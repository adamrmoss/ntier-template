import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, firstValueFrom, map } from 'rxjs';

import { AuthApiService } from './auth-api.service';
import type { ApiErrorBody, AuthSession, StoredTokens, User } from './auth.models';
import { AuthTokenStorage } from './auth-token.storage';

/**
 * Owns visitor authentication state and session restore.
 */
@Injectable({
    providedIn: 'root',
})
export class AuthService
{
    private readonly authApi = inject(AuthApiService);
    private readonly tokenStorage = inject(AuthTokenStorage);

    private readonly sessionSubject = new BehaviorSubject<AuthSession | null>(null);
    private readonly initializedSubject = new BehaviorSubject<boolean>(false);
    private refreshInFlight: Promise<boolean> | null = null;

    public readonly session$: Observable<AuthSession | null> = this.sessionSubject.asObservable();

    public readonly initialized$: Observable<boolean> = this.initializedSubject.asObservable();

    public readonly user$: Observable<User | null> = this.session$.pipe(
        map((session) => session?.user ?? null),
    );

    public readonly isAuthenticated$: Observable<boolean> = this.session$.pipe(
        map((session) => session !== null),
    );

    public get session(): AuthSession | null
    {
        return this.sessionSubject.value;
    }

    public get initialized(): boolean
    {
        return this.initializedSubject.value;
    }

    public get user(): User | null
    {
        return this.sessionSubject.value?.user ?? null;
    }

    public get isAuthenticated(): boolean
    {
        return this.sessionSubject.value !== null;
    }

    public get accessToken(): string | null
    {
        return this.sessionSubject.value?.accessToken ?? null;
    }

    /**
     * Restore a stored session on application startup.
     */
    public async initialize(): Promise<void>
    {
        try
        {
            const stored = this.tokenStorage.read();

            if (!stored)
            {
                return;
            }

            await this.restoreSession(stored);
        }
        catch
        {
            this.clearSession();
        }
        finally
        {
            this.initializedSubject.next(true);
        }
    }

    /**
     * Register a new account and send a confirmation email.
     */
    public async register(
        email: string,
        password: string,
        profile?: {
            displayName?: string;
            firstName?: string;
            lastName?: string;
        },
    ): Promise<string>
    {
        const response = await firstValueFrom(this.authApi.register({
            email,
            password,
            displayName: profile?.displayName,
            firstName: profile?.firstName,
            lastName: profile?.lastName,
        }));

        return response.message;
    }

    /**
     * Sign in with email and password and load the current profile.
     */
    public async login(email: string, password: string): Promise<void>
    {
        const tokens = await firstValueFrom(this.authApi.login({
            email,
            password,
        }));

        await this.applyTokens(tokens);
    }

    /**
     * Confirm an email address from a link and establish a session.
     */
    public async confirmEmail(userId: number, token: string): Promise<void>
    {
        const tokens = await firstValueFrom(this.authApi.confirmEmail({
            userId,
            token,
        }));

        await this.applyTokens(tokens);
    }

    /**
     * Request another confirmation email.
     */
    public async resendConfirmation(email: string): Promise<string>
    {
        const response = await firstValueFrom(this.authApi.resendConfirmation({ email }));

        return response.message;
    }

    /**
     * Request a password reset email.
     */
    public async forgotPassword(email: string): Promise<string>
    {
        const response = await firstValueFrom(this.authApi.forgotPassword({ email }));

        return response.message;
    }

    /**
     * Reset a password from an email link and establish a session.
     */
    public async resetPassword(email: string, token: string, newPassword: string): Promise<void>
    {
        const tokens = await firstValueFrom(this.authApi.resetPassword({
            email,
            token,
            newPassword,
        }));

        await this.applyTokens(tokens);
    }

    /**
     * Revoke the refresh token and clear local session state.
     */
    public async logout(): Promise<void>
    {
        const current = this.session;

        if (current)
        {
            try
            {
                await firstValueFrom(this.authApi.logout({
                    refreshToken: current.refreshToken,
                }));
            }
            catch
            {
                // Clear local state even when the API call fails.
            }
        }

        this.clearSession();
    }

    /**
     * Rotate stored refresh tokens; used by the HTTP interceptor on `401` responses.
     */
    public refreshTokens(): Promise<boolean>
    {
        if (!this.refreshInFlight)
        {
            this.refreshInFlight = this.performTokenRefresh().finally(() =>
            {
                this.refreshInFlight = null;
            });
        }

        return this.refreshInFlight;
    }

    /**
     * Drop in-memory session state and remove stored tokens.
     */
    public clearSession(): void
    {
        this.sessionSubject.next(null);
        this.tokenStorage.clear();
    }

    /**
     * Map a failed auth HTTP response to a user-facing message.
     */
    public authErrorMessage(error: unknown, fallback: string): string
    {
        if (error instanceof HttpErrorResponse)
        {
            const body = error.error as ApiErrorBody | null;

            if (body?.message)
            {
                return body.message;
            }

            if (body?.errors?.length)
            {
                return body.errors[0];
            }
        }

        return fallback;
    }

    /**
     * Map a failed login HTTP response to a user-facing message.
     */
    public loginErrorMessage(error: unknown): string
    {
        return this.authErrorMessage(error, 'Sign in failed. Please try again.');
    }

    private async performTokenRefresh(): Promise<boolean>
    {
        const refreshToken = this.tokenStorage.readRefreshToken();

        if (!refreshToken)
        {
            return false;
        }

        try
        {
            const tokens = await firstValueFrom(this.authApi.refresh({
                refreshToken,
            }));

            this.tokenStorage.write(tokens);

            const current = this.session;

            if (current)
            {
                this.sessionSubject.next({
                    ...current,
                    accessToken: tokens.accessToken,
                    refreshToken: tokens.refreshToken,
                    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
                });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async restoreSession(stored: StoredTokens): Promise<void>
    {
        let tokens: StoredTokens = {
            accessToken: stored.accessToken,
            refreshToken: stored.refreshToken,
            accessTokenExpiresAt: stored.accessTokenExpiresAt,
        };

        if (this.tokenStorage.isAccessTokenExpired(tokens.accessTokenExpiresAt))
        {
            const refreshed = await this.performTokenRefresh();

            if (!refreshed)
            {
                throw new Error('Refresh failed.');
            }

            const reread = this.tokenStorage.read();

            if (!reread)
            {
                throw new Error('Missing tokens after refresh.');
            }

            tokens = reread;
        }

        await this.loadUserProfile(tokens);
    }

    private async applyTokens(tokens: StoredTokens): Promise<void>
    {
        this.tokenStorage.write(tokens);
        await this.loadUserProfile(tokens);
    }

    private async loadUserProfile(tokens: StoredTokens): Promise<void>
    {
        const user = await firstValueFrom(this.authApi.getCurrentUser());

        this.sessionSubject.next({
            user,
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
            accessTokenExpiresAt: tokens.accessTokenExpiresAt,
        });
    }
}

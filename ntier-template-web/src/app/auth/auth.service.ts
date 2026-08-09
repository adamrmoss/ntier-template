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
            // Read stored tokens from local storage.
            const stored = this.tokenStorage.read();

            // Exit when no session is persisted.
            if (!stored)
            {
                return;
            }

            // Restore the in-memory session from stored tokens.
            await this.restoreSession(stored);
        }
        catch
        {
            // Drop invalid or expired stored credentials.
            this.clearSession();
        }
        finally
        {
            // Mark startup restore as complete.
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
        // Submit the registration request to the API.
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
        // Exchange credentials for a token pair.
        const tokens = await firstValueFrom(this.authApi.login({
            email,
            password,
        }));

        // Persist tokens and load the user profile.
        await this.applyTokens(tokens);
    }

    /**
     * Confirm an email address from a link and establish a session.
     */
    public async confirmEmail(userId: number, token: string): Promise<void>
    {
        // Confirm the email address with the API.
        const tokens = await firstValueFrom(this.authApi.confirmEmail({
            userId,
            token,
        }));

        // Persist tokens and load the user profile.
        await this.applyTokens(tokens);
    }

    /**
     * Request another confirmation email.
     */
    public async resendConfirmation(email: string): Promise<string>
    {
        // Request another confirmation email from the API.
        const response = await firstValueFrom(this.authApi.resendConfirmation({ email }));

        return response.message;
    }

    /**
     * Request a password reset email.
     */
    public async forgotPassword(email: string): Promise<string>
    {
        // Request a password reset email from the API.
        const response = await firstValueFrom(this.authApi.forgotPassword({ email }));

        return response.message;
    }

    /**
     * Reset a password from an email link and establish a session.
     */
    public async resetPassword(email: string, token: string, newPassword: string): Promise<void>
    {
        // Submit the new password and reset token to the API.
        const tokens = await firstValueFrom(this.authApi.resetPassword({
            email,
            token,
            newPassword,
        }));

        // Persist tokens and load the user profile.
        await this.applyTokens(tokens);
    }

    /**
     * Revoke the refresh token and clear local session state.
     */
    public async logout(): Promise<void>
    {
        const current = this.session;

        // Revoke the refresh token on the server when a session exists.
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

        // Drop in-memory session state and stored tokens.
        this.clearSession();
    }

    /**
     * Rotate stored refresh tokens; used by the HTTP interceptor on `401` responses.
     */
    public refreshTokens(): Promise<boolean>
    {
        // Reuse a single in-flight refresh for concurrent callers.
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
        // Clear the in-memory session.
        this.sessionSubject.next(null);

        // Remove persisted tokens.
        this.tokenStorage.clear();
    }

    /**
     * Map a failed auth HTTP response to a user-facing message.
     */
    public authErrorMessage(error: unknown, fallback: string): string
    {
        // Extract a message from a failed HTTP response.
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

        // Abort when no refresh token is stored.
        if (!refreshToken)
        {
            return false;
        }

        try
        {
            // Exchange the refresh token for a new pair.
            const tokens = await firstValueFrom(this.authApi.refresh({
                refreshToken,
            }));

            // Persist the rotated token pair.
            this.tokenStorage.write(tokens);

            const current = this.session;

            // Update the in-memory session when one is active.
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

        // Refresh expired access tokens before loading the profile.
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

        // Load the user profile and publish the session.
        await this.loadUserProfile(tokens);
    }

    private async applyTokens(tokens: StoredTokens): Promise<void>
    {
        // Persist the token pair from the API.
        this.tokenStorage.write(tokens);

        // Load the user profile and publish the session.
        await this.loadUserProfile(tokens);
    }

    private async loadUserProfile(tokens: StoredTokens): Promise<void>
    {
        // Fetch the authenticated user's profile.
        const user = await firstValueFrom(this.authApi.getCurrentUser());

        // Publish the session with tokens and profile.
        this.sessionSubject.next({
            user,
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
            accessTokenExpiresAt: tokens.accessTokenExpiresAt,
        });
    }
}

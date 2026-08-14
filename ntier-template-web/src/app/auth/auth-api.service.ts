import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../site/api-config';
import type
    {
        ConfirmEmailRequest,
        ForgotPasswordRequest,
        LoginRequest,
        LogoutRequest,
        MessageResponse,
        RefreshRequest,
        RegisterRequest,
        ResendConfirmationRequest,
        ResetPasswordRequest,
        TokenResponse,
        User,
    } from './auth.models';

/**
 * HTTP calls to authentication and profile endpoints.
 */
@Injectable({
    providedIn: 'root',
})
export class AuthApiService
{
    private readonly http = inject(HttpClient);

    /**
     * Register a new account.
     */
    public register(request: RegisterRequest): Observable<MessageResponse>
    {
        return this.http.post<MessageResponse>(`${API_BASE_URL}/auth/register`, request);
    }

    /**
     * Sign in with email and password.
     */
    public login(request: LoginRequest): Observable<TokenResponse>
    {
        return this.http.post<TokenResponse>(`${API_BASE_URL}/auth/login`, request);
    }

    /**
     * Confirm an email address.
     */
    public confirmEmail(request: ConfirmEmailRequest): Observable<TokenResponse>
    {
        return this.http.post<TokenResponse>(`${API_BASE_URL}/auth/confirm-email`, request);
    }

    /**
     * Resend a confirmation email.
     */
    public resendConfirmation(request: ResendConfirmationRequest): Observable<MessageResponse>
    {
        return this.http.post<MessageResponse>(`${API_BASE_URL}/auth/resend-confirmation`, request);
    }

    /**
     * Request a password reset email.
     */
    public forgotPassword(request: ForgotPasswordRequest): Observable<MessageResponse>
    {
        return this.http.post<MessageResponse>(`${API_BASE_URL}/auth/forgot-password`, request);
    }

    /**
     * Reset a password with a token from email.
     */
    public resetPassword(request: ResetPasswordRequest): Observable<TokenResponse>
    {
        return this.http.post<TokenResponse>(`${API_BASE_URL}/auth/reset-password`, request);
    }

    /**
     * Rotate a refresh token and issue a new access token.
     */
    public refresh(request: RefreshRequest): Observable<TokenResponse>
    {
        return this.http.post<TokenResponse>(`${API_BASE_URL}/auth/refresh`, request);
    }

    /**
     * Revoke the caller's refresh token.
     */
    public logout(request: LogoutRequest): Observable<void>
    {
        return this.http.post<void>(`${API_BASE_URL}/auth/logout`, request);
    }

    /**
     * Load the authenticated user's profile.
     */
    public getCurrentUser(): Observable<User>
    {
        return this.http.get<User>(`${API_BASE_URL}/user/current`);
    }
}

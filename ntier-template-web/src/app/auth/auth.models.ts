/**
 * Signed-in account profile returned by `/user/current`.
 */
export interface User
{
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    displayName: string;
    roles: string[];
    initials: string;
    isAdmin: boolean;
}

/**
 * JWT pair returned from login and refresh.
 */
export interface TokenResponse
{
    accessToken: string;
    refreshToken: string;
    accessTokenExpiresAt: string;
}

/**
 * Token pair read from browser storage.
 */
export interface StoredTokens
{
    accessToken: string;
    refreshToken: string;
    accessTokenExpiresAt: string;
}

/**
 * Login request body for `POST /auth/login`.
 */
export interface LoginRequest
{
    email: string;
    password: string;
}

/**
 * Registration request body for `POST /auth/register`.
 */
export interface RegisterRequest
{
    email: string;
    password: string;
    displayName?: string;
    firstName?: string;
    lastName?: string;
}

/**
 * Email confirmation request body for `POST /auth/confirm-email`.
 */
export interface ConfirmEmailRequest
{
    userId: number;
    token: string;
}

/**
 * Resend confirmation request body for `POST /auth/resend-confirmation`.
 */
export interface ResendConfirmationRequest
{
    email: string;
}

/**
 * Forgot password request body for `POST /auth/forgot-password`.
 */
export interface ForgotPasswordRequest
{
    email: string;
}

/**
 * Reset password request body for `POST /auth/reset-password`.
 */
export interface ResetPasswordRequest
{
    email: string;
    token: string;
    newPassword: string;
}

/**
 * Refresh request body for `POST /auth/refresh`.
 */
export interface RefreshRequest
{
    refreshToken: string;
}

/**
 * Logout request body for `POST /auth/logout`.
 */
export interface LogoutRequest
{
    refreshToken?: string;
}

/**
 * Simple message response from anonymous auth endpoints.
 */
export interface MessageResponse
{
    message: string;
}

/**
 * Common API error payload shape.
 */
export interface ApiErrorBody
{
    message?: string;
    errors?: string[];
}

/**
 * Active authenticated session held in memory by {@link AuthService}.
 */
export interface AuthSession
{
    user: User;
    accessToken: string;
    refreshToken: string;
    accessTokenExpiresAt: string;
}

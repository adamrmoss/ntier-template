import { Injectable } from '@angular/core';

import type { StoredTokens, TokenResponse } from './auth.models';

const ACCESS_TOKEN_KEY = 'ntier.accessToken';
const REFRESH_TOKEN_KEY = 'ntier.refreshToken';
const ACCESS_TOKEN_EXPIRES_KEY = 'ntier.accessTokenExpiresAt';

/**
 * Persist JWT pairs in `localStorage` for session restore across reloads.
 */
@Injectable({
    providedIn: 'root',
})
export class AuthTokenStorage
{
    /**
     * Read the stored token pair, if any.
     */
    public read(): StoredTokens | null
    {
        const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
        const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
        const accessTokenExpiresAt = localStorage.getItem(ACCESS_TOKEN_EXPIRES_KEY);

        if (!accessToken || !refreshToken || !accessTokenExpiresAt)
        {
            return null;
        }

        return {
            accessToken,
            refreshToken,
            accessTokenExpiresAt,
        };
    }

    /**
     * Return the stored access token when present.
     */
    public readAccessToken(): string | null
    {
        return localStorage.getItem(ACCESS_TOKEN_KEY);
    }

    /**
     * Return the stored refresh token when present.
     */
    public readRefreshToken(): string | null
    {
        return localStorage.getItem(REFRESH_TOKEN_KEY);
    }

    /**
     * Persist a token pair from the API.
     */
    public write(tokens: TokenResponse): void
    {
        localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
        localStorage.setItem(ACCESS_TOKEN_EXPIRES_KEY, tokens.accessTokenExpiresAt);
    }

    /**
     * Remove all stored auth tokens.
     */
    public clear(): void
    {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        localStorage.removeItem(ACCESS_TOKEN_EXPIRES_KEY);
    }

    /**
     * Determine whether the access token is expired or within the refresh buffer.
     */
    public isAccessTokenExpired(accessTokenExpiresAt: string, bufferMs: number = 60_000): boolean
    {
        const expiresAtMs = Date.parse(accessTokenExpiresAt);

        if (Number.isNaN(expiresAtMs))
        {
            return true;
        }

        return Date.now() >= expiresAtMs - bufferMs;
    }
}

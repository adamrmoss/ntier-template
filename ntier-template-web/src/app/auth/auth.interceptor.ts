import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';
import { AuthTokenStorage } from './auth-token.storage';

/**
 * Routes that must not receive an `Authorization` header or trigger refresh retries.
 */
function isAuthExemptUrl(url: string): boolean
{
    return url.includes('/auth/login')
        || url.includes('/auth/register')
        || url.includes('/auth/confirm-email')
        || url.includes('/auth/resend-confirmation')
        || url.includes('/auth/forgot-password')
        || url.includes('/auth/reset-password')
        || url.includes('/auth/refresh');
}

/**
 * Attach bearer tokens to API requests and refresh once on `401` responses.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) =>
{
    const auth = inject(AuthService);
    const tokenStorage = inject(AuthTokenStorage);

    // Skip auth endpoints that must not carry bearer tokens.
    if (isAuthExemptUrl(request.url))
    {
        return next(request);
    }

    // Attach the access token when one is available.
    const accessToken = auth.accessToken ?? tokenStorage.readAccessToken();
    const authorizedRequest = accessToken
        ? request.clone({
            setHeaders: {
                Authorization: `Bearer ${accessToken}`,
            },
        })
        : request;

    return next(authorizedRequest).pipe(
        catchError((error: unknown) =>
        {
            // Propagate non-401 errors without retrying.
            if (!(error instanceof HttpErrorResponse) || error.status !== 401)
            {
                return throwError(() => error);
            }

            // Avoid refresh loops on auth endpoints.
            if (isAuthExemptUrl(request.url))
            {
                return throwError(() => error);
            }

            // Refresh tokens once and retry the original request.
            return from(auth.refreshTokens()).pipe(
                switchMap((refreshed) =>
                {
                    // Clear the session when refresh fails.
                    if (!refreshed)
                    {
                        auth.clearSession();
                        return throwError(() => error);
                    }

                    const nextAccessToken = auth.accessToken ?? tokenStorage.readAccessToken();

                    // Clear the session when no access token is available after refresh.
                    if (!nextAccessToken)
                    {
                        auth.clearSession();
                        return throwError(() => error);
                    }

                    // Retry the original request with the new access token.
                    return next(request.clone({
                        setHeaders: {
                            Authorization: `Bearer ${nextAccessToken}`,
                        },
                    }));
                }),
            );
        }),
    );
};

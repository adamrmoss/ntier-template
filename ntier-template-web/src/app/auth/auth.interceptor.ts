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

    if (isAuthExemptUrl(request.url))
    {
        return next(request);
    }

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
            if (!(error instanceof HttpErrorResponse) || error.status !== 401)
            {
                return throwError(() => error);
            }

            if (isAuthExemptUrl(request.url))
            {
                return throwError(() => error);
            }

            return from(auth.refreshTokens()).pipe(
                switchMap((refreshed) =>
                {
                    if (!refreshed)
                    {
                        auth.clearSession();
                        return throwError(() => error);
                    }

                    const nextAccessToken = auth.accessToken ?? tokenStorage.readAccessToken();

                    if (!nextAccessToken)
                    {
                        auth.clearSession();
                        return throwError(() => error);
                    }

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

import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, finalize, map, of, shareReplay } from 'rxjs';

import { AuthService } from './auth.service';
import { LoginDialogComponent } from './login-dialog/login-dialog.component';
import { RegisterDialogComponent } from './register-dialog/register-dialog.component';

/**
 * Opens shared auth dialogs without coupling session logic to dialog components.
 */
@Injectable({
    providedIn: 'root',
})
export class AuthDialogService
{
    private readonly auth = inject(AuthService);
    private readonly dialog = inject(MatDialog);

    private loginDialog$?: Observable<boolean>;
    private registerDialog$?: Observable<boolean>;

    /**
     * Open the login dialog from anywhere in the app.
     */
    public openLogin(): Observable<boolean>
    {
        if (this.auth.isAuthenticated)
        {
            return of(true);
        }

        if (!this.loginDialog$)
        {
            this.loginDialog$ = this.dialog.open(LoginDialogComponent, {
                width: '420px',
            }).afterClosed().pipe(
                map((result) => result === true),
                finalize(() =>
                {
                    this.loginDialog$ = undefined;
                }),
                shareReplay({
                    bufferSize: 1,
                    refCount: true,
                }),
            );
        }

        return this.loginDialog$;
    }

    /**
     * Open the registration dialog from anywhere in the app.
     */
    public openRegister(): Observable<boolean>
    {
        if (this.auth.isAuthenticated)
        {
            return of(true);
        }

        if (!this.registerDialog$)
        {
            this.registerDialog$ = this.dialog.open(RegisterDialogComponent, {
                width: '420px',
            }).afterClosed().pipe(
                map((result) => result === true),
                finalize(() =>
                {
                    this.registerDialog$ = undefined;
                }),
                shareReplay({
                    bufferSize: 1,
                    refCount: true,
                }),
            );
        }

        return this.registerDialog$;
    }

    /**
     * Ensure the visitor is signed in, opening the login dialog when needed.
     */
    public requireAuth(): Observable<boolean>
    {
        if (this.auth.isAuthenticated)
        {
            return of(true);
        }

        return this.openLogin();
    }
}

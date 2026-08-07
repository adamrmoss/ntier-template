import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthDialogService } from '../auth/auth-dialog.service';
import { AuthService } from '../auth/auth.service';
import type { User } from '../auth/auth.models';
import { SITE } from '../site/site.constants';

/**
 * Application shell with a Material toolbar and routed content.
 */
@Component({
    selector: 'ntier-shell',
    standalone: true,
    imports: [
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        MatToolbarModule,
        RouterLink,
        RouterOutlet,
    ],
    templateUrl: './shell.component.html',
    styleUrl: './shell.component.scss',
})
export class ShellComponent
{
    private readonly auth = inject(AuthService);
    private readonly authDialog = inject(AuthDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly appName = SITE.appName;

    private authenticated = false;
    private currentUser: User | null = null;

    constructor()
    {
        this.auth.isAuthenticated$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((authenticated) => {
                this.authenticated = authenticated;
            });

        this.auth.user$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((user) => {
                this.currentUser = user;
            });
    }

    public get isAuthenticated(): boolean
    {
        return this.authenticated;
    }

    public get userLabel(): string
    {
        if (!this.currentUser)
        {
            return 'Account';
        }

        return this.currentUser.displayName || this.currentUser.email;
    }

    public onLogin(): void
    {
        void firstValueFrom(this.authDialog.openLogin());
    }

    public onRegister(): void
    {
        void firstValueFrom(this.authDialog.openRegister());
    }

    public onLogout(): void
    {
        void this.auth.logout();
    }
}

import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';

import type { User } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { SITE, copyrightYear } from '../site/site.constants';

/**
 * Landing page shown at the application root route.
 */
@Component({
    selector: 'ntier-home',
    standalone: true,
    imports: [MatCardModule],
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss',
})
export class HomeComponent
{
    private readonly auth = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly appName = SITE.appName;
    public readonly copyrightYear = copyrightYear();

    private authenticated = false;
    private currentUser: User | null = null;

    constructor()
    {
        // Mirror authentication state into view-ready fields.
        this.auth.isAuthenticated$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((authenticated) => {
                this.authenticated = authenticated;
            });

        // Mirror the signed-in user into view-ready fields.
        this.auth.user$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((user) => {
                this.currentUser = user;
            });
    }

    public get welcomeMessage(): string
    {
        // Greet the signed-in visitor by name.
        if (this.authenticated && this.currentUser)
        {
            const name = this.currentUser.displayName || this.currentUser.email;
            return `Welcome back, ${name}.`;
        }

        return 'Welcome. Sign in from the account menu to get started.';
    }
}

import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, EMPTY, Subject, catchError, from, switchMap, take, takeUntil, tap } from 'rxjs';

import { AuthService } from '../auth.service';

/**
 * Confirms an email address from a link in the registration email.
 */
@Component({
    selector: 'ntier-confirm-email',
    standalone: true,
    templateUrl: './confirm-email.component.html',
    styleUrl: './confirm-email.component.scss',
})
export class ConfirmEmailComponent implements OnInit, OnDestroy
{
    private readonly auth = inject(AuthService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroy$ = new Subject<void>();

    private readonly errorMessageSubject = new BehaviorSubject<string>('');
    private readonly statusMessageSubject = new BehaviorSubject<string>('Confirming your email address…');

    public get errorMessage(): string
    {
        return this.errorMessageSubject.value;
    }

    public get statusMessage(): string
    {
        return this.statusMessageSubject.value;
    }

    public ngOnInit(): void
    {
        // Confirm the email from link query parameters once on load.
        this.route.queryParamMap
            .pipe(
                take(1),
                switchMap((params) => {
                    const userIdValue = params.get('userId');
                    const token = params.get('token') ?? '';
                    const userId = Number(userIdValue);

                    // Reject links with missing or invalid parameters.
                    if (!userIdValue || Number.isNaN(userId) || !token)
                    {
                        this.statusMessageSubject.next('');
                        this.errorMessageSubject.next('This confirmation link is invalid.');
                        return EMPTY;
                    }

                    // Confirm the email and redirect home on success.
                    return from(this.auth.confirmEmail(userId, token)).pipe(
                        tap(() => {
                            void this.router.navigateByUrl('/');
                        }),
                        catchError((error) => {
                            this.errorMessageSubject.next(
                                this.auth.authErrorMessage(error, 'Email confirmation failed.'),
                            );
                            this.statusMessageSubject.next('');
                            return EMPTY;
                        }),
                    );
                }),
                takeUntil(this.destroy$),
            )
            .subscribe();
    }

    public ngOnDestroy(): void
    {
        this.destroy$.next();
        this.destroy$.complete();
    }
}

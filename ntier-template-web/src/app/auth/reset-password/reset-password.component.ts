import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';

import { AuthService } from '../auth.service';
import { passwordsMatch } from '../passwords-match.validator';

/**
 * Resets a password from a link in the reset email.
 */
@Component({
    selector: 'ntier-reset-password',
    standalone: true,
    imports: [
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        ReactiveFormsModule,
    ],
    templateUrl: './reset-password.component.html',
    styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit, OnDestroy
{
    private readonly auth = inject(AuthService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroy$ = new Subject<void>();

    public readonly form = this.formBuilder.nonNullable.group(
        {
            password: ['', [Validators.required]],
            confirmPassword: ['', [Validators.required]],
        },
        { validators: passwordsMatch },
    );

    private email = '';
    private token = '';
    private readonly errorTextSubject = new BehaviorSubject<string>('');
    private readonly isSubmittingSubject = new BehaviorSubject<boolean>(false);
    private readonly linkInvalidSubject = new BehaviorSubject<boolean>(false);

    public get errorMessage(): string
    {
        return this.errorTextSubject.value;
    }

    public get submitting(): boolean
    {
        return this.isSubmittingSubject.value;
    }

    public get invalidLink(): boolean
    {
        return this.linkInvalidSubject.value;
    }

    public get passwordMismatch(): boolean
    {
        return this.form.hasError('passwordMismatch') && this.form.touched;
    }

    public ngOnInit(): void
    {
        // Read reset link parameters and validate the link.
        this.route.queryParamMap
            .pipe(takeUntil(this.destroy$))
            .subscribe((params) => {
                this.email = params.get('email') ?? '';
                this.token = params.get('token') ?? '';
                this.linkInvalidSubject.next(!this.email || !this.token);
            });
    }

    public ngOnDestroy(): void
    {
        this.destroy$.next();
        this.destroy$.complete();
    }

    public async onSubmit(): Promise<void>
    {
        // Block duplicate submits, invalid form state, and bad links.
        if (this.isSubmittingSubject.value || this.form.invalid || this.linkInvalidSubject.value)
        {
            this.form.markAllAsTouched();
            return;
        }

        // Enter the submitting state and clear prior errors.
        this.isSubmittingSubject.next(true);
        this.errorTextSubject.next('');

        const { password } = this.form.getRawValue();

        try
        {
            // Reset the password and redirect home on success.
            await this.auth.resetPassword(this.email, this.token, password);
            await this.router.navigateByUrl('/');
        }
        catch (error)
        {
            this.errorTextSubject.next(this.auth.authErrorMessage(error, 'Password reset failed.'));
        }
        finally
        {
            this.isSubmittingSubject.next(false);
        }
    }
}

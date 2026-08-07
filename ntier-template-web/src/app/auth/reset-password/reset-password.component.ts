import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';

import { AuthService } from '../auth.service';

/**
 * Validate that password and confirm-password fields match.
 */
function passwordsMatch(control: AbstractControl): ValidationErrors | null
{
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (password !== confirmPassword)
    {
        return { passwordMismatch: true };
    }

    return null;
}

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
        if (this.isSubmittingSubject.value || this.form.invalid || this.linkInvalidSubject.value)
        {
            this.form.markAllAsTouched();
            return;
        }

        this.isSubmittingSubject.next(true);
        this.errorTextSubject.next('');

        const { password } = this.form.getRawValue();

        try
        {
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

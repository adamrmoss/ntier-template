import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { BehaviorSubject } from 'rxjs';

import { AuthService } from '../auth.service';
import { passwordsMatch } from '../passwords-match.validator';

/**
 * Registration dialog opened via {@link AuthDialogService.openRegister}.
 */
@Component({
    selector: 'ntier-register-dialog',
    standalone: true,
    imports: [
        MatButtonModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        ReactiveFormsModule,
    ],
    templateUrl: './register-dialog.component.html',
    styleUrl: './register-dialog.component.scss',
})
export class RegisterDialogComponent
{
    private readonly auth = inject(AuthService);
    private readonly dialogRef = inject(MatDialogRef<RegisterDialogComponent, boolean>);
    private readonly formBuilder = inject(FormBuilder);

    public readonly form = this.formBuilder.nonNullable.group(
        {
            email: ['', [Validators.required, Validators.email]],
            password: ['', [Validators.required]],
            confirmPassword: ['', [Validators.required]],
            displayName: [''],
        },
        { validators: passwordsMatch },
    );

    private readonly errorTextSubject = new BehaviorSubject<string>('');
    private readonly isSubmittingSubject = new BehaviorSubject<boolean>(false);
    private readonly successTextSubject = new BehaviorSubject<string>('');

    public get errorMessage(): string
    {
        return this.errorTextSubject.value;
    }

    public get successMessage(): string
    {
        return this.successTextSubject.value;
    }

    public get submitting(): boolean
    {
        return this.isSubmittingSubject.value;
    }

    public get completed(): boolean
    {
        return this.successTextSubject.value.length > 0;
    }

    public get passwordMismatch(): boolean
    {
        return this.form.hasError('passwordMismatch') && this.form.touched;
    }

    public onCancel(): void
    {
        this.dialogRef.close(false);
    }

    public async onSubmit(): Promise<void>
    {
        // Block duplicate submits and invalid form state.
        if (this.isSubmittingSubject.value || this.form.invalid)
        {
            this.form.markAllAsTouched();
            return;
        }

        // Enter the submitting state and clear prior errors.
        this.isSubmittingSubject.next(true);
        this.errorTextSubject.next('');

        const { email, password, displayName } = this.form.getRawValue();

        try
        {
            // Register the account and show the confirmation message.
            const message = await this.auth.register(email, password, {
                displayName: displayName || undefined,
            });
            this.successTextSubject.next(message);
        }
        catch (error)
        {
            this.errorTextSubject.next(this.auth.authErrorMessage(error, 'Registration failed. Please try again.'));
        }
        finally
        {
            this.isSubmittingSubject.next(false);
        }
    }

    public onDone(): void
    {
        this.dialogRef.close(true);
    }
}

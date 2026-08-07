import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { BehaviorSubject, firstValueFrom } from 'rxjs';

import { AuthDialogService } from '../auth-dialog.service';
import { AuthService } from '../auth.service';

/**
 * Email and password login dialog opened via {@link AuthDialogService.openLogin}.
 */
@Component({
    selector: 'ntier-login-dialog',
    standalone: true,
    imports: [
        MatButtonModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        ReactiveFormsModule,
    ],
    templateUrl: './login-dialog.component.html',
    styleUrl: './login-dialog.component.scss',
})
export class LoginDialogComponent
{
    private readonly auth = inject(AuthService);
    private readonly authDialog = inject(AuthDialogService);
    private readonly dialogRef = inject(MatDialogRef<LoginDialogComponent, boolean>);
    private readonly formBuilder = inject(FormBuilder);

    public readonly form = this.formBuilder.nonNullable.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]],
    });

    private readonly errorTextSubject = new BehaviorSubject<string>('');
    private readonly forgotMessageSubject = new BehaviorSubject<string>('');
    private readonly isSubmittingSubject = new BehaviorSubject<boolean>(false);
    private readonly showForgotPasswordSubject = new BehaviorSubject<boolean>(false);

    public get errorMessage(): string
    {
        return this.errorTextSubject.value;
    }

    public get submitting(): boolean
    {
        return this.isSubmittingSubject.value;
    }

    public get forgotPasswordVisible(): boolean
    {
        return this.showForgotPasswordSubject.value;
    }

    public get forgotPasswordMessage(): string
    {
        return this.forgotMessageSubject.value;
    }

    public onCancel(): void
    {
        this.dialogRef.close(false);
    }

    public onOpenRegister(): void
    {
        this.dialogRef.close(false);
        void firstValueFrom(this.authDialog.openRegister());
    }

    public onShowForgotPassword(): void
    {
        this.showForgotPasswordSubject.next(true);
        this.errorTextSubject.next('');
        this.forgotMessageSubject.next('');
    }

    public async onSendForgotPassword(): Promise<void>
    {
        if (this.form.controls.email.invalid)
        {
            this.form.controls.email.markAsTouched();
            return;
        }

        this.isSubmittingSubject.next(true);
        this.forgotMessageSubject.next('');

        try
        {
            const message = await this.auth.forgotPassword(this.form.controls.email.value);
            this.forgotMessageSubject.next(message);
        }
        catch (error)
        {
            this.errorTextSubject.next(this.auth.authErrorMessage(error, 'Could not send reset email.'));
        }
        finally
        {
            this.isSubmittingSubject.next(false);
        }
    }

    public async onSubmit(): Promise<void>
    {
        if (this.isSubmittingSubject.value || this.form.invalid)
        {
            this.form.markAllAsTouched();
            return;
        }

        this.isSubmittingSubject.next(true);
        this.errorTextSubject.next('');

        const { email, password } = this.form.getRawValue();

        try
        {
            await this.auth.login(email, password);
            this.dialogRef.close(true);
        }
        catch (error)
        {
            this.errorTextSubject.next(this.auth.loginErrorMessage(error));
        }
        finally
        {
            this.isSubmittingSubject.next(false);
        }
    }
}

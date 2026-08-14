import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Validate that password and confirm-password fields match.
 */
export function passwordsMatch(control: AbstractControl): ValidationErrors | null
{
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    // Reject mismatched password fields.
    if (password !== confirmPassword)
    {
        return { passwordMismatch: true };
    }

    return null;
}

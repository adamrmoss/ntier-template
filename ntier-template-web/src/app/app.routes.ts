import { Routes } from '@angular/router';

import { ConfirmEmailComponent } from './auth/confirm-email/confirm-email.component';
import { ResetPasswordComponent } from './auth/reset-password/reset-password.component';
import { HomeComponent } from './home/home.component';
import { ShellComponent } from './shell/shell.component';

export const routes: Routes = [
    {
        path: '',
        component: ShellComponent,
        children: [
            {
                path: '',
                component: HomeComponent,
                data: { pageTitle: 'Home | NTier Template' },
            },
            {
                path: 'confirm-email',
                component: ConfirmEmailComponent,
                data: { pageTitle: 'Confirm email | NTier Template' },
            },
            {
                path: 'reset-password',
                component: ResetPasswordComponent,
                data: { pageTitle: 'Reset password | NTier Template' },
            },
        ],
    },
];

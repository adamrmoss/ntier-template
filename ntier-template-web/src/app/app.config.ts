import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig =
{
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes),
        provideStore(),
        provideEffects(),
        provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
    ],
};

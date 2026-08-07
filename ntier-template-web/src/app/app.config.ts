import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { DomSanitizer } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { authInterceptor } from './auth/auth.interceptor';
import { AuthService } from './auth/auth.service';
import { routes } from './app.routes';
import { registerAppIcons } from './site/app-icon.registry';
import { PageMetaService } from './site/page-meta.service';

export const appConfig: ApplicationConfig =
{
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(
            routes,
            withInMemoryScrolling({
                scrollPositionRestoration: 'top',
            }),
        ),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideAnimationsAsync(),
        provideAppInitializer(() => {
            // Restore any persisted session before the app renders.
            return inject(AuthService).initialize();
        }),
        provideAppInitializer(() => {
            // Construct PageMetaService so its router subscription starts.
            inject(PageMetaService);
        }),
        provideAppInitializer(() => {
            // Register self-hosted SVG icons before the first render.
            registerAppIcons(inject(MatIconRegistry), inject(DomSanitizer));
        }),
        provideStore(),
        provideEffects(),
        provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
    ],
};

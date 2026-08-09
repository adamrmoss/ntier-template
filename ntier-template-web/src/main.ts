import { bootstrapApplication } from '@angular/platform-browser';

import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig)
    .catch((err) =>
    {
        // Log bootstrap failures to the console.
        console.error(err);
    });

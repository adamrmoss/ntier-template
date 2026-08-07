import { MatIconRegistry } from '@angular/material/icon';
import { DomSanitizer } from '@angular/platform-browser';

const APP_ICONS = [
    'account_circle',
    'login',
    'logout',
    'person_add',
] as const;

/**
 * Register self-hosted SVG icons with {@link MatIconRegistry}.
 */
export function registerAppIcons(registry: MatIconRegistry, sanitizer: DomSanitizer): void
{
    // Register each self-hosted SVG icon with Material.
    for (const name of APP_ICONS)
    {
        registry.addSvgIcon(
            name,
            sanitizer.bypassSecurityTrustResourceUrl(`icons/${name}.svg`),
        );
    }
}

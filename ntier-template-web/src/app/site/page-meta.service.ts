import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

/**
 * Applies route title and meta description tags on navigation.
 */
@Injectable({
    providedIn: 'root',
})
export class PageMetaService
{
    private readonly destroyRef = inject(DestroyRef);
    private readonly meta = inject(Meta);
    private readonly router = inject(Router);
    private readonly title = inject(Title);

    /**
     * Subscribes to router events and updates document metadata.
     */
    constructor()
    {
        // Update document metadata on each completed navigation.
        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                map(() => this.getDeepestChild(this.router.routerState.root)),
                startWith(this.getDeepestChild(this.router.routerState.root)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe((route) => {
                this.applyRouteMeta(route);
            });
    }

    /**
     * Walks the route tree to the leaf activated route.
     */
    private getDeepestChild(route: ActivatedRoute): ActivatedRoute
    {
        let current = route;

        // Walk to the leaf activated route.
        while (current.firstChild)
        {
            current = current.firstChild;
        }

        return current;
    }

    /**
     * Sets title and description from route data.
     */
    private applyRouteMeta(route: ActivatedRoute): void
    {
        const data = route.snapshot.data;
        const pageTitle = data['pageTitle'] as string | undefined;
        const description = data['description'] as string | undefined;

        // Set the document title when the route defines one.
        if (pageTitle)
        {
            this.title.setTitle(pageTitle);
        }

        // Update description meta tags when the route defines one.
        if (description)
        {
            this.meta.updateTag({ name: 'description', content: description });
            this.meta.updateTag({ property: 'og:description', content: description });
        }

        // Mirror the page title to Open Graph metadata.
        if (pageTitle)
        {
            this.meta.updateTag({ property: 'og:title', content: pageTitle });
        }
    }
}

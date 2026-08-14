/**
 * Site-wide identity strings.
 */
export const SITE = {
    appName: 'NTier Template',
    publisherName: 'NTier Template',
} as const;

/**
 * Returns the current calendar year for copyright notices.
 */
export function copyrightYear(): number
{
    return new Date().getFullYear();
}

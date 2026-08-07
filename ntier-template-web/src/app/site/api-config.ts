/**
 * Resolve the API base URL for the current environment.
 */
export function resolveApiBaseUrl(): string
{
    if (typeof window !== 'undefined' && window.location.hostname === 'localhost')
    {
        return 'http://localhost:5271';
    }

    return 'https://api.example.com';
}

export const API_BASE_URL = resolveApiBaseUrl();

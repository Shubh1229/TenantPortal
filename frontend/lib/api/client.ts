const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

interface RequestOptions {
    method?: string;
    body?: unknown;
    requiresAuth?: boolean;
}

async function refreshAccessToken(): Promise<string | null> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return null;

    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        return null;
    }

    const data = await response.json();
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    return data.accessToken;
}

export async function apiRequest<T>(
    endpoint: string,
    options: RequestOptions = {}
): Promise<T> {
    const { method = 'GET', body, requiresAuth = true } = options;

    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
    };

    if (requiresAuth) {
        let token = localStorage.getItem('accessToken');
        if (!token) {
            token = await refreshAccessToken();
            if (!token) throw new Error('Unauthorized');
        }
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    if (response.status === 401 && requiresAuth) {
        const newToken = await refreshAccessToken();
        if (!newToken) throw new Error('Unauthorized');

        headers['Authorization'] = `Bearer ${newToken}`;
        const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined,
        });

        if (!retryResponse.ok) throw new Error(await retryResponse.text());
        return retryResponse.json();
    }

    if (!response.ok) throw new Error(await response.text());
    return response.json();
}
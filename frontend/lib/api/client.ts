const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

interface RequestOptions {
    method?: string;
    body?: unknown;
    requiresAuth?: boolean;
}

function logApi(method: string, url: string, status: number, durationMs: number, error?: string) {
    const tag = `[API] ${method} ${url} → ${status} (${durationMs}ms)`;
    if (error) {
        console.error(tag, error);
    } else {
        console.log(tag);
    }
}

async function parseResponse<T>(response: Response): Promise<T> {
    const text = await response.text();
    if (!text) return undefined as T;
    try {
        return JSON.parse(text) as T;
    } catch {
        return text as unknown as T;
    }
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
    const url = `${API_BASE_URL}${endpoint}`;
    const start = Date.now();

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

    console.log(`[API] ${method} ${url}`, body ? { body } : '');

    const response = await fetch(url, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    if (response.status === 401 && requiresAuth) {
        const newToken = await refreshAccessToken();
        if (!newToken) throw new Error('Unauthorized');

        headers['Authorization'] = `Bearer ${newToken}`;
        const retryResponse = await fetch(url, {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined,
        });

        const duration = Date.now() - start;
        if (!retryResponse.ok) {
            const errorText = await retryResponse.text();
            logApi(method, url, retryResponse.status, duration, errorText);
            throw new Error(errorText);
        }
        logApi(method, url, retryResponse.status, duration);
        return parseResponse<T>(retryResponse);
    }

    const duration = Date.now() - start;
    if (!response.ok) {
        const errorText = await response.text();
        logApi(method, url, response.status, duration, errorText);
        throw new Error(errorText);
    }

    logApi(method, url, response.status, duration);
    return parseResponse<T>(response);
}
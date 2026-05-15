import { apiRequest } from './client';
import { LoginRequest, LoginResponse, TotpValidationRequest } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const authApi = {
    login: async (data: LoginRequest): Promise<string> => {
        const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        if (!response.ok) throw new Error('Login failed');
        return response.text();
    },

    validateTotp: (data: TotpValidationRequest) =>
        apiRequest<LoginResponse>('/api/auth/login/totp', {
            method: 'POST',
            body: data,
            requiresAuth: false,
        }),

    logout: (refreshToken: string) =>
        apiRequest<void>('/api/auth/logout', {
            method: 'POST',
            body: { refreshToken },
        }),

    invite: (email: string, role: string) =>
        apiRequest<void>('/api/auth/invite', {
            method: 'POST',
            body: { email, role },
        }),
};
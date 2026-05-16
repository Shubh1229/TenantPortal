import { apiRequest } from './client';
import { AdminRegisterResponse, LoginRequest, LoginResponse, TotpValidationRequest } from '@/types';

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

    registerAdmin: (email: string, password: string, returnBaseUrl: string) =>
        apiRequest<AdminRegisterResponse>('/api/auth/register/admin', {
            method: 'POST',
            body: { email, password, returnBaseUrl },
            requiresAuth: false,
        }),

    getSubscriptionStatus: () =>
        apiRequest<{ status: string; isActive: boolean; maxTenants: number | null; currentTenantCount: number }>(
            '/api/auth/subscription/status',
            { method: 'GET' }
        ),

    getBillingPortalUrl: (returnUrl: string) =>
        apiRequest<{ url: string }>('/api/auth/subscription/portal', {
            method: 'POST',
            body: { returnUrl },
        }),
};
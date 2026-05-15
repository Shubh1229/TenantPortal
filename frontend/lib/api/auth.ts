import { apiRequest } from './client';
import { LoginRequest, LoginResponse, TotpValidationRequest } from '@/types';

export const authApi = {
    login: (data: LoginRequest) =>
        apiRequest<string>('/api/auth/login', {
            method: 'POST',
            body: data,
            requiresAuth: false,
        }),

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
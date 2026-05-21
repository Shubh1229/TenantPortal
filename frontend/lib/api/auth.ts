import { apiRequest } from './client';
import { AdminRegisterResponse, ConnectStatus, LoginRequest, LoginResponse, TotpValidationRequest, UserProfile } from '@/types';

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

    getUsers: (role?: string) =>
        apiRequest<import('@/types').User[]>(
            `/api/auth/users${role ? `?role=${role}` : ''}`
        ),

    getPublicProfile: (userId: string) =>
        apiRequest<import('@/types').PublicUserProfile>(`/api/auth/users/${userId}/public-profile`),

    devLogin: (email: string, password: string) =>
        apiRequest<import('@/types').LoginResponse>('/api/auth/dev-login', {
            method: 'POST',
            body: { email, password },
            requiresAuth: false,
        }),

    switchRole: (targetRole: string) =>
        apiRequest<{ accessToken: string }>('/api/auth/switch-role', {
            method: 'POST',
            body: { targetRole },
        }),

    // ── Profile ───────────────────────────────────────────────────────────────────

    getProfile: () =>
        apiRequest<UserProfile>('/api/auth/account/profile'),

    updateProfile: (data: {
        firstName: string;
        lastName: string;
        phoneNumber: string;
        emergencyContactName?: string;
        emergencyContactPhone?: string;
    }) =>
        apiRequest<{ success: boolean }>('/api/auth/account/profile', {
            method: 'PUT',
            body: data,
        }),

    // ── Notification Emails ───────────────────────────────────────────────────────

    addNotificationEmail: (email: string) =>
        apiRequest<{ success: boolean }>('/api/auth/account/notification-emails', {
            method: 'POST',
            body: { email },
        }),

    deleteNotificationEmail: (id: string) =>
        apiRequest<{ success: boolean }>(`/api/auth/account/notification-emails/${id}`, {
            method: 'DELETE',
        }),

    // ── Primary Email ─────────────────────────────────────────────────────────────

    updatePrimaryEmail: (newEmail: string, currentPassword: string) =>
        apiRequest<{ success: boolean; message: string }>('/api/auth/account/primary-email', {
            method: 'PUT',
            body: { newEmail, currentPassword },
        }),

    // ── Password ──────────────────────────────────────────────────────────────────

    changePassword: (currentPassword: string, newPassword: string) =>
        apiRequest<{ success: boolean; message: string }>('/api/auth/account/password', {
            method: 'PUT',
            body: { currentPassword, newPassword },
        }),

    // ── Delete Account ────────────────────────────────────────────────────────────

    deleteAccount: (confirmEmail: string) =>
        apiRequest<{ success: boolean }>('/api/auth/account', {
            method: 'DELETE',
            body: { confirmEmail },
        }),

    // ── Stripe Connect ────────────────────────────────────────────────────────────

    getConnectStatus: () =>
        apiRequest<ConnectStatus>('/api/auth/connect/status'),

    getConnectOnboardingUrl: (returnUrl: string, refreshUrl: string) =>
        apiRequest<{ onboardingUrl: string }>('/api/auth/connect/onboard', {
            method: 'POST',
            body: { returnUrl, refreshUrl },
        }),
};

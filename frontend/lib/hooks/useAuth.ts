'use client';

import { useState, useEffect } from 'react';
import { UserRole } from '@/types';

interface AuthState {
    accessToken: string | null;
    role: UserRole | null;
    userId: string | null;
    isLoading: boolean;
}

function parseJwt(token: string) {
    try {
        const base64 = token.split('.')[1];
        const decoded = JSON.parse(atob(base64));
        return decoded;
    } catch {
        return null;
    }
}

export function useAuth() {
    const [auth, setAuth] = useState<AuthState>({
        accessToken: null,
        role: null,
        userId: null,
        isLoading: true,
    });

    useEffect(() => {
        const token = localStorage.getItem('accessToken');
        if (!token) {
            setAuth({ accessToken: null, role: null, userId: null, isLoading: false });
            return;
        }

        const payload = parseJwt(token);
        if (!payload) {
            setAuth({ accessToken: null, role: null, userId: null, isLoading: false });
            return;
        }

        setAuth({
            accessToken: token,
            role: payload.role as UserRole,
            userId: payload.uid,
            isLoading: false,
        });
    }, []);

    const logout = () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        setAuth({ accessToken: null, role: null, userId: null, isLoading: false });
        window.location.href = '/login';
    };

    return { ...auth, logout };
}
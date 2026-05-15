'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/hooks/useAuth';
import { UserRole } from '@/types';

interface ProtectedRouteProps {
    children: React.ReactNode;
    allowedRoles?: UserRole[];
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
    const { accessToken, role, isLoading } = useAuth();
    const router = useRouter();

    useEffect(() => {
        if (isLoading) return;
        if (!accessToken) {
            router.push('/login');
            return;
        }
        if (allowedRoles && role && !allowedRoles.includes(role)) {
            router.push('/login');
        }
    }, [accessToken, role, isLoading, allowedRoles, router]);

    if (isLoading) return <div className="flex items-center justify-center min-h-screen">Loading...</div>;
    if (!accessToken) return null;

    return <>{children}</>;
}
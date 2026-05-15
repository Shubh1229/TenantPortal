'use client';

import { useAuth } from '@/lib/hooks/useAuth';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
    const { accessToken, role, isLoading, logout } = useAuth();
    const router = useRouter();

    useEffect(() => {
        if (!isLoading && !accessToken) {
            router.push('/login');
        }
    }, [accessToken, isLoading, router]);

    if (isLoading) return <div className="flex items-center justify-center min-h-screen">Loading...</div>;
    if (!accessToken) return null;

    return (
        <div className="min-h-screen bg-slate-50">
            <nav className="bg-white border-b px-6 py-4 flex items-center justify-between">
                <div className="flex items-center gap-6">
                    <h1 className="font-semibold text-lg">Tenant Portal</h1>
                    <Separator orientation="vertical" className="h-6" />
                    <span className="text-sm text-slate-500 capitalize">{role}</span>
                </div>
                <Button variant="ghost" size="sm" onClick={logout}>
                    Sign out
                </Button>
            </nav>
            <main className="container mx-auto px-6 py-8">
                {children}
            </main>
        </div>
    );
}
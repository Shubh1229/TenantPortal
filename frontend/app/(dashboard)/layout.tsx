'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect } from 'react';
import { useAuth } from '@/lib/hooks/useAuth';
import {
    LayoutDashboard,
    CreditCard,
    FileText,
    Bell,
    Calendar,
    LogOut,
    Building2,
} from 'lucide-react';

interface NavItem {
    href: string;
    label: string;
    icon: React.ReactNode;
}

function roleNavItems(role: string | null): NavItem[] {
    if (role === 'SuperAdmin') {
        return [
            { href: '/super-admin', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
        ];
    }
    if (role === 'Admin') {
        return [
            { href: '/admin', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
            { href: '/admin/contracts', label: 'Contracts', icon: <FileText size={18} /> },
            { href: '/admin/rent-schedule', label: 'Rent Schedule', icon: <Calendar size={18} /> },
        ];
    }
    return [
        { href: '/tenant', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
        { href: '/tenant/payment', label: 'Make Payment', icon: <CreditCard size={18} /> },
        { href: '/tenant/contracts', label: 'Contracts', icon: <FileText size={18} /> },
        { href: '/tenant/notifications', label: 'Notifications', icon: <Bell size={18} /> },
    ];
}

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
    const { accessToken, role, isLoading, logout } = useAuth();
    const router = useRouter();
    const pathname = usePathname();

    useEffect(() => {
        if (!isLoading && !accessToken) {
            router.push('/login');
        }
    }, [accessToken, isLoading, router]);

    if (isLoading) {
        return (
            <div className="flex items-center justify-center min-h-screen bg-zinc-950 text-zinc-400">
                Loading...
            </div>
        );
    }
    if (!accessToken) return null;

    const navItems = roleNavItems(role);

    return (
        <div className="flex min-h-screen bg-zinc-950 text-zinc-100">
            {/* Sidebar */}
            <aside className="w-60 shrink-0 flex flex-col bg-zinc-900 border-r border-zinc-800">
                {/* Brand */}
                <div className="px-5 py-5 border-b border-zinc-800 flex items-center gap-2.5">
                    <div className="w-7 h-7 rounded-lg bg-indigo-600 flex items-center justify-center shrink-0">
                        <Building2 size={15} className="text-white" />
                    </div>
                    <span className="font-semibold text-sm tracking-tight text-zinc-100">
                        Tenant Portal
                    </span>
                </div>

                {/* Role badge */}
                <div className="px-5 pt-4 pb-1">
                    <span className="text-[10px] uppercase tracking-widest text-zinc-500 font-medium">
                        {role ?? 'User'}
                    </span>
                </div>

                {/* Nav */}
                <nav className="flex-1 px-3 py-2 space-y-0.5">
                    {navItems.map(item => {
                        const isActive = pathname === item.href;
                        return (
                            <Link
                                key={item.href}
                                href={item.href}
                                className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                                    isActive
                                        ? 'bg-indigo-600/20 text-indigo-400 font-medium'
                                        : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100'
                                }`}
                            >
                                <span className={isActive ? 'text-indigo-400' : 'text-zinc-500'}>
                                    {item.icon}
                                </span>
                                {item.label}
                            </Link>
                        );
                    })}
                </nav>

                {/* Sign out */}
                <div className="px-3 py-4 border-t border-zinc-800">
                    <button
                        onClick={logout}
                        className="flex items-center gap-3 w-full px-3 py-2 rounded-lg text-sm text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100 transition-colors"
                    >
                        <LogOut size={18} className="text-zinc-500" />
                        Sign out
                    </button>
                </div>
            </aside>

            {/* Main content */}
            <main className="flex-1 overflow-auto p-8">
                {children}
            </main>
        </div>
    );
}

'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useAuth } from '@/lib/hooks/useAuth';
import { authApi } from '@/lib/api/auth';
import {
    LayoutDashboard,
    CreditCard,
    FileText,
    Bell,
    Calendar,
    LogOut,
    Building2,
    FlaskConical,
    Home,
    Settings,
    User,
    ArrowLeftRight,
    Landmark,
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
            { href: '/super-admin/tests', label: 'System Tests', icon: <FlaskConical size={18} /> },
            { href: '/profile', label: 'Profile', icon: <User size={18} /> },
            { href: '/settings', label: 'Settings', icon: <Settings size={18} /> },
        ];
    }
    if (role === 'Admin') {
        return [
            { href: '/admin', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
            { href: '/admin/properties', label: 'Properties', icon: <Home size={18} /> },
            { href: '/admin/transactions', label: 'Transactions', icon: <ArrowLeftRight size={18} /> },
            { href: '/admin/contracts', label: 'Contracts', icon: <FileText size={18} /> },
            { href: '/admin/rent-schedule', label: 'Rent Schedule', icon: <Calendar size={18} /> },
            { href: '/admin/payouts', label: 'Payouts', icon: <Landmark size={18} /> },
            { href: '/profile', label: 'Profile', icon: <User size={18} /> },
            { href: '/settings', label: 'Settings', icon: <Settings size={18} /> },
        ];
    }
    return [
        { href: '/tenant', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
        { href: '/tenant/payment', label: 'Make Payment', icon: <CreditCard size={18} /> },
        { href: '/tenant/transactions', label: 'Transactions', icon: <ArrowLeftRight size={18} /> },
        { href: '/tenant/contracts', label: 'Contracts', icon: <FileText size={18} /> },
        { href: '/tenant/notifications', label: 'Notifications', icon: <Bell size={18} /> },
        { href: '/profile', label: 'Profile', icon: <User size={18} /> },
        { href: '/settings', label: 'Settings', icon: <Settings size={18} /> },
    ];
}

const SWITCH_ROLES = ['SuperAdmin', 'Admin', 'Tenant', 'Tester'] as const;

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
    const { accessToken, role, isSuperAdminSwitched, isLoading, logout } = useAuth();
    const router = useRouter();
    const pathname = usePathname();
    const [profileChecked, setProfileChecked] = useState(false);

    const canSwitchRoles = role === 'SuperAdmin' || isSuperAdminSwitched;

    // Redirect to profile-setup if the user hasn't completed their profile yet
    useEffect(() => {
        if (!accessToken || isLoading || pathname === '/profile-setup' || profileChecked) return;
        authApi.getProfile()
            .then(p => {
                setProfileChecked(true);
                if (!p.isProfileComplete) router.push('/profile-setup');
            })
            .catch(() => setProfileChecked(true));
    }, [accessToken, isLoading, pathname, profileChecked, router]);

    async function handleSwitchRole(targetRole: string) {
        try {
            const { authApi } = await import('@/lib/api/auth');
            const { accessToken: newToken } = await authApi.switchRole(targetRole);
            localStorage.setItem('accessToken', newToken);
            if (targetRole === 'SuperAdmin') router.push('/super-admin');
            else if (targetRole === 'Admin') router.push('/admin');
            else if (targetRole === 'Tester') router.push('/super-admin/tests');
            else router.push('/tenant');
            window.location.reload();
        } catch (e) {
            console.error('Role switch failed', e);
        }
    }

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
                        Singh Resident Hub
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

                {/* Role switcher (SuperAdmin only) */}
                {canSwitchRoles && (
                    <div className="px-3 pb-3 border-t border-zinc-800 pt-3">
                        <p className="text-[10px] uppercase tracking-widest text-zinc-600 font-medium px-2 mb-1.5">
                            View as role
                        </p>
                        <div className="grid grid-cols-2 gap-1">
                            {SWITCH_ROLES.map(r => (
                                <button
                                    key={r}
                                    onClick={() => handleSwitchRole(r)}
                                    className={`px-2 py-1.5 rounded-md text-xs transition-colors text-left ${
                                        role === r
                                            ? 'bg-indigo-600/20 text-indigo-400 font-medium'
                                            : 'text-zinc-500 hover:bg-zinc-800 hover:text-zinc-300'
                                    }`}
                                >
                                    {r}
                                </button>
                            ))}
                        </div>
                        {isSuperAdminSwitched && (
                            <p className="text-[10px] text-yellow-500/70 px-2 mt-1.5">
                                Viewing as {role} — your actual account is SuperAdmin
                            </p>
                        )}
                    </div>
                )}

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

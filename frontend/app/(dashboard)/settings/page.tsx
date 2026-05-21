'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api/auth';
import { useAuth } from '@/lib/hooks/useAuth';

type Section = 'primary-email' | 'password' | 'delete';

function extractError(err: unknown, fallback: string): string {
    if (!(err instanceof Error)) return fallback;
    try {
        const parsed = JSON.parse(err.message);
        return parsed?.error ?? parsed?.title ?? fallback;
    } catch {
        return err.message || fallback;
    }
}

export default function SettingsPage() {
    const { logout } = useAuth();
    const router = useRouter();

    const [profile, setProfile] = useState<{ email: string; role: string } | null>(null);
    const [activeSection, setActiveSection] = useState<Section | null>(null);

    // Primary email form
    const [newEmail, setNewEmail] = useState('');
    const [emailPassword, setEmailPassword] = useState('');
    const [emailStatus, setEmailStatus] = useState<string | null>(null);
    const [emailError, setEmailError] = useState<string | null>(null);

    // Password form
    const [currentPw, setCurrentPw] = useState('');
    const [newPw, setNewPw] = useState('');
    const [confirmPw, setConfirmPw] = useState('');
    const [pwStatus, setPwStatus] = useState<string | null>(null);
    const [pwError, setPwError] = useState<string | null>(null);

    // Delete form
    const [deleteConfirm, setDeleteConfirm] = useState('');
    const [deleteError, setDeleteError] = useState<string | null>(null);

    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        authApi.getProfile().then(p => {
            setProfile({ email: p.email, role: p.role });
        }).catch(() => {});
    }, []);

    function openSection(s: Section) {
        setActiveSection(s === activeSection ? null : s);
        // Reset all form state when switching sections
        setEmailStatus(null); setEmailError(null);
        setPwStatus(null); setPwError(null);
        setDeleteError(null);
        setNewEmail(''); setEmailPassword('');
        setCurrentPw(''); setNewPw(''); setConfirmPw('');
        setDeleteConfirm('');
    }

    async function handlePrimaryEmail(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true); setEmailError(null); setEmailStatus(null);
        try {
            const result = await authApi.updatePrimaryEmail(newEmail, emailPassword);
            setEmailStatus(result.message);
            setTimeout(() => { logout(); router.push('/login'); }, 2000);
        } catch (err: unknown) {
            setEmailError(extractError(err, 'Failed to update email.'));
        } finally {
            setSubmitting(false);
        }
    }

    async function handleChangePassword(e: React.FormEvent) {
        e.preventDefault();
        if (newPw !== confirmPw) { setPwError('New passwords do not match.'); return; }
        if (newPw.length < 8) { setPwError('Password must be at least 8 characters.'); return; }
        setSubmitting(true); setPwError(null); setPwStatus(null);
        try {
            const result = await authApi.changePassword(currentPw, newPw);
            setPwStatus(result.message);
            setTimeout(() => { logout(); router.push('/login'); }, 2000);
        } catch (err: unknown) {
            setPwError(extractError(err, 'Failed to change password.'));
        } finally {
            setSubmitting(false);
        }
    }

    async function handleDeleteAccount(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true); setDeleteError(null);
        try {
            await authApi.deleteAccount(deleteConfirm);
            logout();
            router.push('/login');
        } catch (err: unknown) {
            setDeleteError(extractError(err, 'Failed to delete account.'));
        } finally {
            setSubmitting(false);
        }
    }

    if (!profile) {
        return (
            <div className="flex items-center justify-center h-40 text-zinc-500 text-sm">
                Loading...
            </div>
        );
    }

    return (
        <div className="max-w-xl mx-auto space-y-6">
            <div>
                <h1 className="text-xl font-semibold text-zinc-100">Account Settings</h1>
                <p className="text-sm text-zinc-500 mt-1">
                    Signed in as <span className="text-zinc-300">{profile.email}</span>
                    <span className="ml-2 text-zinc-600">· {profile.role}</span>
                </p>
            </div>

            {/* Primary email */}
            <SettingsCard
                title="Change Login Email"
                description="Updates the email used to sign in. You'll be logged out after saving."
                open={activeSection === 'primary-email'}
                onToggle={() => openSection('primary-email')}
            >
                <form onSubmit={handlePrimaryEmail} className="space-y-3">
                    <input
                        type="email"
                        required
                        value={newEmail}
                        onChange={e => setNewEmail(e.target.value)}
                        placeholder="New email address"
                        className="w-full bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    <input
                        type="password"
                        required
                        value={emailPassword}
                        onChange={e => setEmailPassword(e.target.value)}
                        placeholder="Current password to confirm"
                        className="w-full bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    {emailError && <p className="text-red-400 text-xs">{emailError}</p>}
                    {emailStatus && <p className="text-green-400 text-xs">{emailStatus}</p>}
                    <button
                        type="submit"
                        disabled={submitting}
                        className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium disabled:opacity-50 transition-colors"
                    >
                        Update Email
                    </button>
                </form>
            </SettingsCard>

            {/* Change password */}
            <SettingsCard
                title="Change Password"
                description="All active sessions will be logged out after saving."
                open={activeSection === 'password'}
                onToggle={() => openSection('password')}
            >
                <form onSubmit={handleChangePassword} className="space-y-3">
                    <input
                        type="password"
                        required
                        value={currentPw}
                        onChange={e => setCurrentPw(e.target.value)}
                        placeholder="Current password"
                        className="w-full bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    <input
                        type="password"
                        required
                        value={newPw}
                        onChange={e => setNewPw(e.target.value)}
                        placeholder="New password (min 8 characters)"
                        className="w-full bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    <input
                        type="password"
                        required
                        value={confirmPw}
                        onChange={e => setConfirmPw(e.target.value)}
                        placeholder="Confirm new password"
                        className="w-full bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    {pwError && <p className="text-red-400 text-xs">{pwError}</p>}
                    {pwStatus && <p className="text-green-400 text-xs">{pwStatus}</p>}
                    <button
                        type="submit"
                        disabled={submitting}
                        className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-medium disabled:opacity-50 transition-colors"
                    >
                        Change Password
                    </button>
                </form>
            </SettingsCard>

            {/* Delete account */}
            <SettingsCard
                title="Delete Account"
                description="Permanently deletes your account. This cannot be undone."
                open={activeSection === 'delete'}
                onToggle={() => openSection('delete')}
                danger
            >
                <form onSubmit={handleDeleteAccount} className="space-y-3">
                    <p className="text-sm text-zinc-400">
                        Type <span className="font-mono text-zinc-200">{profile.email}</span> to confirm deletion.
                    </p>
                    <input
                        type="email"
                        required
                        value={deleteConfirm}
                        onChange={e => setDeleteConfirm(e.target.value)}
                        placeholder={profile.email}
                        className="w-full bg-zinc-800 border border-red-900/50 rounded-lg px-3 py-2 text-sm text-zinc-100 placeholder-zinc-600 focus:outline-none focus:ring-2 focus:ring-red-600"
                    />
                    {deleteError && <p className="text-red-400 text-xs">{deleteError}</p>}
                    <button
                        type="submit"
                        disabled={submitting || deleteConfirm.toLowerCase() !== profile.email.toLowerCase()}
                        className="px-4 py-2 rounded-lg bg-red-700 hover:bg-red-600 text-white text-sm font-medium disabled:opacity-40 transition-colors"
                    >
                        Delete My Account
                    </button>
                </form>
            </SettingsCard>
        </div>
    );
}

function SettingsCard({
    title,
    description,
    open,
    onToggle,
    danger = false,
    children,
}: {
    title: string;
    description: string;
    open: boolean;
    onToggle: () => void;
    danger?: boolean;
    children: React.ReactNode;
}) {
    return (
        <div className={`rounded-xl border ${danger ? 'border-red-900/40' : 'border-zinc-800'} bg-zinc-900`}>
            <button
                type="button"
                onClick={onToggle}
                className="w-full flex items-center justify-between px-5 py-4 text-left"
            >
                <div>
                    <p className={`text-sm font-medium ${danger ? 'text-red-400' : 'text-zinc-100'}`}>{title}</p>
                    <p className="text-xs text-zinc-500 mt-0.5">{description}</p>
                </div>
                <span className="text-zinc-600 text-xs ml-4">{open ? '▲' : '▼'}</span>
            </button>
            {open && (
                <div className="px-5 pb-5 border-t border-zinc-800">
                    <div className="pt-4">{children}</div>
                </div>
            )}
        </div>
    );
}

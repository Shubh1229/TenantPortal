'use client';

import { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api/auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Building2, CheckCircle2, AlertCircle } from 'lucide-react';

function ResetPasswordForm() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const token = searchParams.get('token') ?? '';

    const [password, setPassword] = useState('');
    const [confirm, setConfirm] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [success, setSuccess] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        if (!token) setError('Missing reset token. Please use the link from your email.');
    }, [token]);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (password !== confirm) {
            setError('Passwords do not match.');
            return;
        }
        if (password.length < 8) {
            setError('Password must be at least 8 characters.');
            return;
        }
        setError('');
        setIsLoading(true);
        try {
            await authApi.resetPassword(token, password);
            setSuccess(true);
            setTimeout(() => router.push('/login'), 3000);
        } catch (err: unknown) {
            const msg = err instanceof Error ? err.message : null;
            setError(msg ?? 'This reset link is invalid or has expired.');
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="flex min-h-screen items-center justify-center bg-zinc-950 px-6">
            <div className="w-full max-w-sm space-y-8">
                <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center">
                        <Building2 size={16} className="text-white" />
                    </div>
                    <span className="font-semibold text-white">Singh Resident Hub</span>
                </div>

                {success ? (
                    <div className="space-y-4 text-center">
                        <div className="flex justify-center">
                            <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center">
                                <CheckCircle2 size={26} className="text-emerald-400" />
                            </div>
                        </div>
                        <div>
                            <h1 className="text-xl font-semibold text-white">Password updated</h1>
                            <p className="mt-2 text-sm text-zinc-400">
                                Your password has been reset. Redirecting you to sign in…
                            </p>
                        </div>
                        <Link href="/login" className="text-sm text-indigo-400 hover:text-indigo-300">
                            Sign in now
                        </Link>
                    </div>
                ) : (
                    <div className="space-y-6">
                        <div>
                            <h1 className="text-2xl font-semibold text-white">Set new password</h1>
                            <p className="mt-1 text-sm text-zinc-400">
                                Choose a strong password for your account.
                            </p>
                        </div>
                        <form onSubmit={handleSubmit} className="space-y-5">
                            <div className="space-y-1.5">
                                <Label htmlFor="password" className="text-zinc-300">New password</Label>
                                <Input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    required
                                    autoComplete="new-password"
                                    className="bg-zinc-900 border-zinc-700 text-white focus:border-indigo-500"
                                />
                            </div>
                            <div className="space-y-1.5">
                                <Label htmlFor="confirm" className="text-zinc-300">Confirm password</Label>
                                <Input
                                    id="confirm"
                                    type="password"
                                    value={confirm}
                                    onChange={e => setConfirm(e.target.value)}
                                    required
                                    autoComplete="new-password"
                                    className="bg-zinc-900 border-zinc-700 text-white focus:border-indigo-500"
                                />
                            </div>
                            {error && (
                                <div className="flex items-start gap-2 text-sm text-red-400">
                                    <AlertCircle size={15} className="mt-0.5 shrink-0" />
                                    {error}
                                </div>
                            )}
                            <Button
                                type="submit"
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                disabled={isLoading || !token}
                            >
                                {isLoading ? 'Updating...' : 'Update password'}
                            </Button>
                            <p className="text-center text-sm text-zinc-500">
                                <Link href="/login" className="text-indigo-400 hover:text-indigo-300">
                                    Back to sign in
                                </Link>
                            </p>
                        </form>
                    </div>
                )}
            </div>
        </div>
    );
}

export default function ResetPasswordPage() {
    return (
        <Suspense>
            <ResetPasswordForm />
        </Suspense>
    );
}

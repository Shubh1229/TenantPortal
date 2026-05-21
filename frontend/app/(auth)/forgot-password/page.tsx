'use client';

import { useState } from 'react';
import Link from 'next/link';
import { authApi } from '@/lib/api/auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Building2, CheckCircle2 } from 'lucide-react';

export default function ForgotPasswordPage() {
    const [email, setEmail] = useState('');
    const [submitted, setSubmitted] = useState(false);
    const [isLoading, setIsLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setIsLoading(true);
        try {
            await authApi.forgotPassword(email, window.location.origin);
        } catch {
            // Swallow — we show the same success message regardless
        } finally {
            setSubmitted(true);
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

                {submitted ? (
                    <div className="space-y-4 text-center">
                        <div className="flex justify-center">
                            <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center">
                                <CheckCircle2 size={26} className="text-emerald-400" />
                            </div>
                        </div>
                        <div>
                            <h1 className="text-xl font-semibold text-white">Check your email</h1>
                            <p className="mt-2 text-sm text-zinc-400">
                                If <strong className="text-zinc-300">{email}</strong> is associated with an account,
                                you will receive a password reset link shortly.
                            </p>
                        </div>
                        <Link href="/login" className="text-sm text-indigo-400 hover:text-indigo-300">
                            Back to sign in
                        </Link>
                    </div>
                ) : (
                    <div className="space-y-6">
                        <div>
                            <h1 className="text-2xl font-semibold text-white">Forgot password</h1>
                            <p className="mt-1 text-sm text-zinc-400">
                                Enter your email and we will send you a reset link.
                            </p>
                        </div>
                        <form onSubmit={handleSubmit} className="space-y-5">
                            <div className="space-y-1.5">
                                <Label htmlFor="email" className="text-zinc-300">Email</Label>
                                <Input
                                    id="email"
                                    type="email"
                                    value={email}
                                    onChange={e => setEmail(e.target.value)}
                                    required
                                    autoComplete="email"
                                    className="bg-zinc-900 border-zinc-700 text-white placeholder:text-zinc-600 focus:border-indigo-500"
                                    placeholder="you@example.com"
                                />
                            </div>
                            <Button
                                type="submit"
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                disabled={isLoading}
                            >
                                {isLoading ? 'Sending...' : 'Send reset link'}
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

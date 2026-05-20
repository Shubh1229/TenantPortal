'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Building2, ShieldCheck, FlaskConical } from 'lucide-react';

const isDev = process.env.NODE_ENV === 'development';

const DEV_ACCOUNTS = [
    { label: 'Admin',   email: 'fakeadmin@email.com',  password: 'DevAdmin123!',  role: 'Admin' },
    { label: 'Tenant',  email: 'faketenant@email.com', password: 'DevTenant123!', role: 'Tenant' },
    { label: 'Tester',  email: 'faketester@email.com', password: 'DevTester123!', role: 'Tester' },
] as const;

type LoginStep = 'credentials' | 'totp';

export default function LoginPage() {
    const router = useRouter();
    const [step, setStep] = useState<LoginStep>('credentials');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [totpCode, setTotpCode] = useState('');
    const [temporaryToken, setTemporaryToken] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    async function handleDevLogin(devEmail: string, devPassword: string, role: string) {
        setIsLoading(true);
        setError('');
        try {
            const response = await authApi.devLogin(devEmail, devPassword);
            localStorage.setItem('accessToken', response.accessToken);
            localStorage.setItem('refreshToken', response.refreshToken);
            if (role === 'Admin') router.push('/admin');
            else if (role === 'Tester') router.push('/super-admin/tests');
            else router.push('/tenant');
        } catch {
            setError('Dev login failed — make sure the server is running in Development mode.');
        } finally {
            setIsLoading(false);
        }
    }

    async function handleCredentials(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setIsLoading(true);
        try {
            const tempToken = await authApi.login({ email, password });
            setTemporaryToken(tempToken);
            setStep('totp');
        } catch {
            setError('Invalid email or password');
        } finally {
            setIsLoading(false);
        }
    }

    async function handleTotp(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setIsLoading(true);
        try {
            const response = await authApi.validateTotp({ temporaryToken, totpCode });
            localStorage.setItem('accessToken', response.accessToken);
            localStorage.setItem('refreshToken', response.refreshToken);

            const payload = JSON.parse(atob(response.accessToken.split('.')[1]));
            const role = payload.role;

            if (role === 'SuperAdmin') router.push('/super-admin');
            else if (role === 'Admin') router.push('/admin');
            else router.push('/tenant');
        } catch {
            setError('Invalid authenticator code');
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="flex min-h-screen bg-zinc-950">
            {/* Left panel — branding */}
            <div className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12 bg-gradient-to-br from-indigo-950 via-zinc-900 to-zinc-950 border-r border-zinc-800">
                <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-xl bg-indigo-600 flex items-center justify-center">
                        <Building2 size={20} className="text-white" />
                    </div>
                    <span className="text-lg font-semibold text-white">Singh Resident Hub</span>
                </div>

                <div>
                    <blockquote className="space-y-3">
                        <p className="text-2xl font-light text-zinc-200 leading-relaxed">
                            Streamline your property management — from rent collection to contract
                            storage, in one secure platform.
                        </p>
                        <footer className="text-sm text-zinc-500">
                            End-to-end encrypted &bull; TOTP 2FA &bull; Role-based access
                        </footer>
                    </blockquote>
                </div>

                <div className="flex items-center gap-2 text-xs text-zinc-600">
                    <ShieldCheck size={14} />
                    <span>All data encrypted at rest and in transit</span>
                </div>
            </div>

            {/* Right panel — form */}
            <div className="flex flex-1 items-center justify-center px-6 py-12">
                <div className="w-full max-w-sm space-y-8">
                    {/* Mobile brand */}
                    <div className="flex items-center gap-3 lg:hidden">
                        <div className="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center">
                            <Building2 size={16} className="text-white" />
                        </div>
                        <span className="font-semibold text-white">Singh Resident Hub</span>
                    </div>

                    <div>
                        <h1 className="text-2xl font-semibold text-white">
                            {step === 'credentials' ? 'Sign in' : 'Two-factor auth'}
                        </h1>
                        <p className="mt-1 text-sm text-zinc-400">
                            {step === 'credentials'
                                ? 'Enter your credentials to continue'
                                : 'Enter the code from your authenticator app'}
                        </p>
                    </div>

                    {isDev && (
                        <div className="rounded-lg border border-yellow-500/20 bg-yellow-500/5 p-4 space-y-3">
                            <div className="flex items-center gap-2 text-yellow-400 text-xs font-medium">
                                <FlaskConical size={13} />
                                Dev shortcuts (bypasses TOTP)
                            </div>
                            <div className="flex flex-wrap gap-2">
                                {DEV_ACCOUNTS.map(acct => (
                                    <Button
                                        key={acct.role}
                                        type="button"
                                        size="sm"
                                        variant="outline"
                                        className="border-yellow-500/30 text-yellow-300 hover:bg-yellow-500/10 text-xs h-7"
                                        disabled={isLoading}
                                        onClick={() => handleDevLogin(acct.email, acct.password, acct.role)}
                                    >
                                        {acct.label}
                                    </Button>
                                ))}
                            </div>
                        </div>
                    )}

                    {step === 'credentials' ? (
                        <form onSubmit={handleCredentials} className="space-y-5">
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
                            <div className="space-y-1.5">
                                <Label htmlFor="password" className="text-zinc-300">Password</Label>
                                <Input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    required
                                    autoComplete="current-password"
                                    className="bg-zinc-900 border-zinc-700 text-white placeholder:text-zinc-600 focus:border-indigo-500"
                                />
                            </div>
                            {error && <p className="text-sm text-red-400">{error}</p>}
                            <Button
                                type="submit"
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                disabled={isLoading}
                            >
                                {isLoading ? 'Signing in...' : 'Sign in'}
                            </Button>
                            <p className="text-center text-sm text-zinc-500">
                                New admin?{' '}
                                <Link href="/register/admin" className="text-indigo-400 hover:text-indigo-300">
                                    Create an account
                                </Link>
                            </p>
                        </form>
                    ) : (
                        <form onSubmit={handleTotp} className="space-y-5">
                            <div className="space-y-1.5">
                                <Label htmlFor="totp" className="text-zinc-300">Authenticator Code</Label>
                                <Input
                                    id="totp"
                                    type="text"
                                    inputMode="numeric"
                                    value={totpCode}
                                    onChange={e => setTotpCode(e.target.value)}
                                    placeholder="000 000"
                                    maxLength={6}
                                    required
                                    autoComplete="one-time-code"
                                    className="bg-zinc-900 border-zinc-700 text-white placeholder:text-zinc-600 focus:border-indigo-500 text-center text-xl tracking-widest"
                                />
                            </div>
                            {error && <p className="text-sm text-red-400">{error}</p>}
                            <Button
                                type="submit"
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                disabled={isLoading}
                            >
                                {isLoading ? 'Verifying...' : 'Verify'}
                            </Button>
                            <Button
                                type="button"
                                variant="ghost"
                                className="w-full text-zinc-400 hover:text-zinc-200"
                                onClick={() => { setStep('credentials'); setError(''); }}
                            >
                                Back to sign in
                            </Button>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}

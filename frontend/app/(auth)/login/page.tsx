'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

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

            // parse role from JWT and redirect accordingly
            const payload = JSON.parse(atob(response.accessToken.split('.')[1]));
            const role = payload.role;

            if (role === 'SuperAdmin') router.push('/super-admin');
            else if (role === 'Admin') router.push('/admin');
            else router.push('/tenant');
        } catch {
            setError('Invalid TOTP code');
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="flex items-center justify-center min-h-screen bg-slate-50">
            <Card className="w-full max-w-md">
                <CardHeader>
                    <CardTitle>Tenant Portal</CardTitle>
                    <CardDescription>
                        {step === 'credentials' ? 'Sign in to your account' : 'Enter your authenticator code'}
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {step === 'credentials' ? (
                        <form onSubmit={handleCredentials} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="email">Email</Label>
                                <Input
                                    id="email"
                                    type="email"
                                    value={email}
                                    onChange={e => setEmail(e.target.value)}
                                    required
                                    autoComplete="email"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="password">Password</Label>
                                <Input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    required
                                    autoComplete="current-password"
                                />
                            </div>
                            {error && <p className="text-sm text-red-500">{error}</p>}
                            <Button type="submit" className="w-full" disabled={isLoading}>
                                {isLoading ? 'Signing in...' : 'Sign in'}
                            </Button>
                        </form>
                    ) : (
                        <form onSubmit={handleTotp} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="totp">Authenticator Code</Label>
                                <Input
                                    id="totp"
                                    type="text"
                                    value={totpCode}
                                    onChange={e => setTotpCode(e.target.value)}
                                    placeholder="000000"
                                    maxLength={6}
                                    required
                                    autoComplete="one-time-code"
                                />
                            </div>
                            {error && <p className="text-sm text-red-500">{error}</p>}
                            <Button type="submit" className="w-full" disabled={isLoading}>
                                {isLoading ? 'Verifying...' : 'Verify'}
                            </Button>
                            <Button
                                type="button"
                                variant="ghost"
                                className="w-full"
                                onClick={() => { setStep('credentials'); setError(''); }}
                            >
                                Back
                            </Button>
                        </form>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
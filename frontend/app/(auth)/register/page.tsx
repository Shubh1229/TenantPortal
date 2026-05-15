'use client';

import { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { apiRequest } from '@/lib/api/client';

type RegisterStep = 'credentials' | 'totp-setup';

function RegisterForm() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const inviteToken = searchParams.get('token') ?? '';

    const [step, setStep] = useState<RegisterStep>('credentials');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [qrCode, setQrCode] = useState('');
    const [manualKey, setManualKey] = useState('');

    useEffect(() => {
        if (!inviteToken) {
            router.push('/login');
        }
    }, [inviteToken, router]);

    async function handleRegister(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        if (password !== confirmPassword) {
            setError('Passwords do not match');
            return;
        }
        if (password.length < 8) {
            setError('Password must be at least 8 characters');
            return;
        }
        setIsLoading(true);
        try {
            const response = await apiRequest<{ manualEntryKey: string; qrCode: string }>(
                '/api/auth/register',
                {
                    method: 'POST',
                    body: { password, confirmPassword, inviteToken },
                    requiresAuth: false,
                }
            );
            setQrCode(response.qrCode);
            setManualKey(response.manualEntryKey);
            setStep('totp-setup');
        } catch {
            setError('Registration failed. Your invite link may be invalid or expired.');
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="flex items-center justify-center min-h-screen bg-black">
            <Card className="w-full max-w-md">
                <CardHeader>
                    <CardTitle>Create Your Account</CardTitle>
                    <CardDescription>
                        {step === 'credentials'
                            ? 'Set your password to get started'
                            : 'Scan the QR code with your authenticator app'}
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {step === 'credentials' ? (
                        <form onSubmit={handleRegister} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="password">Password</Label>
                                <Input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    required
                                    autoComplete="new-password"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="confirmPassword">Confirm Password</Label>
                                <Input
                                    id="confirmPassword"
                                    type="password"
                                    value={confirmPassword}
                                    onChange={e => setConfirmPassword(e.target.value)}
                                    required
                                    autoComplete="new-password"
                                />
                            </div>
                            {error && <p className="text-sm text-red-500">{error}</p>}
                            <Button type="submit" className="w-full" disabled={isLoading}>
                                {isLoading ? 'Creating account...' : 'Create Account'}
                            </Button>
                        </form>
                    ) : (
                        <div className="space-y-4">
                            <p className="text-sm text-slate-500">
                                Scan this QR code with Microsoft Authenticator or any TOTP app.
                                You will need this code every time you log in.
                            </p>
                            {qrCode && (
                                <div className="flex justify-center">
                                    <img
                                        src={`data:image/png;base64,${qrCode}`}
                                        alt="TOTP QR Code"
                                        className="w-48 h-48"
                                    />
                                </div>
                            )}
                            <div className="space-y-1">
                                <p className="text-xs text-slate-500">Can't scan? Enter this key manually:</p>
                                <p className="text-sm font-mono bg-slate-100 px-3 py-2 rounded break-all">
                                    {manualKey}
                                </p>
                            </div>
                            <Button className="w-full" onClick={() => router.push('/login')}>
                                Continue to Login
                            </Button>
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

export default function RegisterPage() {
    return (
        <Suspense fallback={<div className="flex items-center justify-center min-h-screen bg-black text-white">Loading...</div>}>
            <RegisterForm />
        </Suspense>
    );
}
'use client';

import { useState, Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { authApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Building2, Check, AlertTriangle } from 'lucide-react';

type Plan = 'Starter' | 'Standard' | 'Unlimited';
type BillingPeriod = 'Monthly' | 'Yearly';
type Step = 'form' | 'totp-setup';

const PLANS: Record<Plan, {
    label: string;
    tenants: string;
    monthly: number;
    yearly: number;
    yearlySaving: number;
    yearlyMonthlyEquiv: number;
    features: string[];
}> = {
    Starter: {
        label: 'Starter',
        tenants: '5 tenants',
        monthly: 20,
        yearly: 200,
        yearlySaving: 40,
        yearlyMonthlyEquiv: 16.67,
        features: ['Up to 5 tenants', 'Rent collection (card & ACH)', 'Contract storage', 'Overdue reminders'],
    },
    Standard: {
        label: 'Standard',
        tenants: '10 tenants',
        monthly: 30,
        yearly: 300,
        yearlySaving: 60,
        yearlyMonthlyEquiv: 25,
        features: ['Up to 10 tenants', 'Rent collection (card & ACH)', 'Contract storage', 'Overdue reminders'],
    },
    Unlimited: {
        label: 'Unlimited',
        tenants: 'Unlimited tenants',
        monthly: 50,
        yearly: 500,
        yearlySaving: 100,
        yearlyMonthlyEquiv: 41.67,
        features: ['Unlimited tenants', 'Rent collection (card & ACH)', 'Contract storage', 'Overdue reminders'],
    },
};

const PLAN_ORDER: Plan[] = ['Starter', 'Standard', 'Unlimited'];

function AdminRegisterForm() {
    const searchParams = useSearchParams();
    const wasCanceled = searchParams.get('canceled') === 'true';

    const [step, setStep] = useState<Step>('form');
    const [plan, setPlan] = useState<Plan>('Standard');
    const [billingPeriod, setBillingPeriod] = useState<BillingPeriod>('Monthly');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const [qrCode, setQrCode] = useState('');
    const [manualKey, setManualKey] = useState('');
    const [checkoutUrl, setCheckoutUrl] = useState('');

    const selected = PLANS[plan];
    const isYearly = billingPeriod === 'Yearly';
    const displayPrice = isYearly
        ? `$${selected.yearlyMonthlyEquiv % 1 === 0 ? selected.yearlyMonthlyEquiv : selected.yearlyMonthlyEquiv.toFixed(2)}/mo`
        : `$${selected.monthly}/mo`;

    async function handleRegister(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        if (password !== confirmPassword) { setError('Passwords do not match'); return; }
        if (password.length < 8) { setError('Password must be at least 8 characters'); return; }

        setIsLoading(true);
        try {
            const res = await authApi.registerAdmin(
                email,
                password,
                typeof window !== 'undefined' ? window.location.origin : 'http://localhost:3000',
                plan,
                billingPeriod
            );
            setQrCode(res.totpSetup.qrCode);
            setManualKey(res.totpSetup.manualEntryKey);
            setCheckoutUrl(res.checkoutUrl);
            setStep('totp-setup');
        } catch {
            setError('Registration failed. This email may already be registered.');
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="flex min-h-screen bg-zinc-950">
            {/* Left panel */}
            <div className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12 bg-gradient-to-br from-indigo-950 via-zinc-900 to-zinc-950 border-r border-zinc-800">
                <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-xl bg-indigo-600 flex items-center justify-center">
                        <Building2 size={20} className="text-white" />
                    </div>
                    <span className="text-lg font-semibold text-white">Singh Resident Hub</span>
                </div>

                <div className="space-y-6">
                    <div>
                        <p className="text-sm text-indigo-400 font-medium mb-1">{selected.label} Plan · {selected.tenants}</p>
                        <h2 className="text-3xl font-light text-zinc-200 leading-relaxed">
                            <span className="text-indigo-400 font-semibold">7-day free trial</span>,{' '}
                            then {isYearly ? `$${selected.yearly}/year` : `$${selected.monthly}/month`}
                        </h2>
                        {isYearly && (
                            <p className="mt-2 text-sm text-emerald-400">
                                Save ${selected.yearlySaving} — equivalent to ${selected.yearlyMonthlyEquiv % 1 === 0 ? selected.yearlyMonthlyEquiv : selected.yearlyMonthlyEquiv.toFixed(2)}/mo
                            </p>
                        )}
                    </div>
                    <ul className="space-y-3">
                        {[
                            'No credit card required to start',
                            ...selected.features.slice(0),
                            'Cancel anytime',
                        ].map(f => (
                            <li key={f} className="flex items-center gap-3 text-sm text-zinc-300">
                                <div className="w-5 h-5 rounded-full bg-indigo-500/20 flex items-center justify-center shrink-0">
                                    <Check size={11} className="text-indigo-400" />
                                </div>
                                {f}
                            </li>
                        ))}
                    </ul>
                </div>

                <p className="text-xs text-zinc-600">
                    Free for 7 days, then {isYearly ? `$${selected.yearly}/year ($${selected.yearlyMonthlyEquiv % 1 === 0 ? selected.yearlyMonthlyEquiv : selected.yearlyMonthlyEquiv.toFixed(2)}/mo)` : `$${selected.monthly}/month`}. No card required upfront.
                </p>
            </div>

            {/* Right panel */}
            <div className="flex flex-1 items-center justify-center px-6 py-12">
                <div className="w-full max-w-md space-y-7">
                    {step === 'form' && (
                        <>
                            {wasCanceled && (
                                <div className="flex items-start gap-2.5 rounded-lg bg-yellow-500/10 border border-yellow-500/20 px-4 py-3 text-sm text-yellow-300">
                                    <AlertTriangle size={15} className="mt-0.5 shrink-0" />
                                    Billing was not completed. Your account details are saved — click &ldquo;Continue to billing&rdquo; when ready.
                                </div>
                            )}

                            <div>
                                <h1 className="text-2xl font-semibold text-white">Create admin account</h1>
                                <p className="mt-1 text-sm text-zinc-400">
                                    Start your 7-day free trial — no card required.
                                </p>
                            </div>

                            {/* Billing toggle */}
                            <div className="flex items-center gap-1 p-1 rounded-lg bg-zinc-900 border border-zinc-800 w-fit">
                                {(['Monthly', 'Yearly'] as BillingPeriod[]).map(p => (
                                    <button
                                        key={p}
                                        type="button"
                                        onClick={() => setBillingPeriod(p)}
                                        className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
                                            billingPeriod === p
                                                ? 'bg-indigo-600 text-white'
                                                : 'text-zinc-400 hover:text-zinc-200'
                                        }`}
                                    >
                                        {p}
                                        {p === 'Yearly' && (
                                            <span className="ml-1.5 text-xs bg-emerald-500/15 text-emerald-400 px-1.5 py-0.5 rounded-full">
                                                2 months free
                                            </span>
                                        )}
                                    </button>
                                ))}
                            </div>

                            {/* Plan cards */}
                            <div className="grid grid-cols-3 gap-3">
                                {PLAN_ORDER.map(p => {
                                    const info = PLANS[p];
                                    const isSelected = plan === p;
                                    const price = isYearly ? info.yearly : info.monthly;
                                    const unit = isYearly ? '/yr' : '/mo';
                                    return (
                                        <button
                                            key={p}
                                            type="button"
                                            onClick={() => setPlan(p)}
                                            className={`rounded-xl border p-3 text-left transition-all ${
                                                isSelected
                                                    ? 'border-indigo-500 bg-indigo-500/10'
                                                    : 'border-zinc-700 bg-zinc-900 hover:border-zinc-600'
                                            }`}
                                        >
                                            <div className={`text-sm font-semibold mb-1 ${isSelected ? 'text-white' : 'text-zinc-300'}`}>
                                                {info.label}
                                            </div>
                                            <div className={`text-xs mb-2 ${isSelected ? 'text-indigo-300' : 'text-zinc-500'}`}>
                                                {info.tenants}
                                            </div>
                                            <div className={`text-base font-bold ${isSelected ? 'text-white' : 'text-zinc-200'}`}>
                                                ${price}
                                                <span className="text-xs font-normal text-zinc-500">{unit}</span>
                                            </div>
                                            {isYearly && (
                                                <div className="mt-1.5 space-y-0.5">
                                                    <div className="text-xs text-emerald-400">
                                                        Save ${info.yearlySaving}
                                                    </div>
                                                    <div className="text-xs text-zinc-500">
                                                        ~${info.yearlyMonthlyEquiv % 1 === 0 ? info.yearlyMonthlyEquiv : info.yearlyMonthlyEquiv.toFixed(2)}/mo
                                                    </div>
                                                </div>
                                            )}
                                        </button>
                                    );
                                })}
                            </div>

                            <form onSubmit={handleRegister} className="space-y-4">
                                <div className="space-y-1.5">
                                    <Label htmlFor="email" className="text-zinc-300">Email</Label>
                                    <Input
                                        id="email"
                                        type="email"
                                        value={email}
                                        onChange={e => setEmail(e.target.value)}
                                        required
                                        autoComplete="off"
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
                                        autoComplete="new-password"
                                        className="bg-zinc-900 border-zinc-700 text-white focus:border-indigo-500"
                                    />
                                </div>
                                <div className="space-y-1.5">
                                    <Label htmlFor="confirmPassword" className="text-zinc-300">Confirm Password</Label>
                                    <Input
                                        id="confirmPassword"
                                        type="password"
                                        value={confirmPassword}
                                        onChange={e => setConfirmPassword(e.target.value)}
                                        required
                                        autoComplete="new-password"
                                        className="bg-zinc-900 border-zinc-700 text-white focus:border-indigo-500"
                                    />
                                </div>
                                {error && <p className="text-sm text-red-400">{error}</p>}
                                <Button
                                    type="submit"
                                    className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                    disabled={isLoading}
                                >
                                    {isLoading ? 'Creating account...' : `Start free trial — ${selected.label} ${isYearly ? `$${selected.yearly}/yr` : `$${selected.monthly}/mo`}`}
                                </Button>
                                <p className="text-center text-sm text-zinc-500">
                                    Already have an account?{' '}
                                    <Link href="/login" className="text-indigo-400 hover:text-indigo-300">
                                        Sign in
                                    </Link>
                                </p>
                            </form>
                        </>
                    )}

                    {step === 'totp-setup' && (
                        <>
                            <div>
                                <h1 className="text-2xl font-semibold text-white">Set up authenticator</h1>
                                <p className="mt-1 text-sm text-zinc-400">
                                    Scan this QR code with your authenticator app (e.g. Google Authenticator).
                                    You&apos;ll need it every time you sign in.
                                </p>
                            </div>
                            {qrCode && (
                                <div className="flex justify-center">
                                    <img
                                        src={`data:image/png;base64,${qrCode}`}
                                        alt="TOTP QR Code"
                                        className="w-48 h-48 rounded-lg"
                                    />
                                </div>
                            )}
                            <div className="rounded-lg bg-zinc-900 border border-zinc-700 p-4 space-y-1">
                                <p className="text-xs text-zinc-500">Can&apos;t scan? Enter this key manually:</p>
                                <p className="text-sm font-mono text-zinc-200 break-all">{manualKey}</p>
                            </div>
                            <Button
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                onClick={() => { if (checkoutUrl) window.location.href = checkoutUrl; }}
                            >
                                Continue to billing →
                            </Button>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}

export default function AdminRegisterPage() {
    return (
        <Suspense fallback={<div className="min-h-screen bg-zinc-950" />}>
            <AdminRegisterForm />
        </Suspense>
    );
}

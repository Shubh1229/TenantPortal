'use client';

import { useEffect, useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { ConnectStatus } from '@/types';
import { CheckCircle2, Clock, Landmark, ExternalLink, AlertCircle } from 'lucide-react';

export default function PayoutsPage() {
    const [status, setStatus] = useState<ConnectStatus | null>(null);
    const [loading, setLoading] = useState(true);
    const [connecting, setConnecting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        authApi.getConnectStatus()
            .then(setStatus)
            .catch(() => setError('Failed to load payout status.'))
            .finally(() => setLoading(false));
    }, []);

    async function handleConnect() {
        setConnecting(true);
        setError(null);
        try {
            const origin = window.location.origin;
            const { onboardingUrl } = await authApi.getConnectOnboardingUrl(
                `${origin}/stripe-connect/return`,
                `${origin}/admin/payouts`
            );
            window.location.href = onboardingUrl;
        } catch {
            setError('Failed to start onboarding. Please try again.');
            setConnecting(false);
        }
    }

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-[40vh] text-zinc-500">
                Loading payout settings…
            </div>
        );
    }

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-zinc-100">Payouts</h1>
                <p className="text-sm text-zinc-500 mt-1">
                    Connect your bank account to receive rent payments directly from tenants.
                </p>
            </div>

            {error && (
                <div className="flex items-start gap-3 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
                    <AlertCircle size={16} className="mt-0.5 shrink-0" />
                    {error}
                </div>
            )}

            {!status?.isConnected && (
                <div className="rounded-xl border border-zinc-800 bg-zinc-900 p-6 space-y-4">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-indigo-600/20 flex items-center justify-center">
                            <Landmark size={20} className="text-indigo-400" />
                        </div>
                        <div>
                            <p className="font-medium text-zinc-100">Bank Account Not Connected</p>
                            <p className="text-xs text-zinc-500">Tenants cannot pay until you connect your account</p>
                        </div>
                    </div>
                    <p className="text-sm text-zinc-400 leading-relaxed">
                        Singh Resident Hub uses <strong className="text-zinc-300">Stripe Connect</strong> to route tenant
                        payments directly into your bank account. Stripe handles identity verification and secure payouts —
                        you stay in control of when and how you receive funds.
                    </p>
                    <ul className="text-sm text-zinc-400 space-y-1.5 list-none">
                        {[
                            'Rent payments deposited directly to your bank',
                            'Stripe handles identity verification (KYC)',
                            'Manage payouts from the Stripe Express dashboard',
                            'No additional platform fees',
                        ].map(item => (
                            <li key={item} className="flex items-start gap-2">
                                <CheckCircle2 size={14} className="text-indigo-400 mt-0.5 shrink-0" />
                                {item}
                            </li>
                        ))}
                    </ul>
                    <button
                        onClick={handleConnect}
                        disabled={connecting}
                        className="w-full py-2.5 px-4 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-sm font-medium transition-colors"
                    >
                        {connecting ? 'Redirecting to Stripe…' : 'Connect Bank Account'}
                    </button>
                </div>
            )}

            {status?.isConnected && !status.chargesEnabled && (
                <div className="rounded-xl border border-yellow-500/20 bg-zinc-900 p-6 space-y-4">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-yellow-500/10 flex items-center justify-center">
                            <Clock size={20} className="text-yellow-400" />
                        </div>
                        <div>
                            <div className="flex items-center gap-2">
                                <p className="font-medium text-zinc-100">Account Connected</p>
                                <span className="text-[10px] uppercase tracking-widest px-2 py-0.5 rounded-full bg-yellow-500/15 text-yellow-400 font-medium">
                                    Pending Verification
                                </span>
                            </div>
                            <p className="text-xs text-zinc-500 mt-0.5">Stripe is reviewing your information</p>
                        </div>
                    </div>
                    <p className="text-sm text-zinc-400 leading-relaxed">
                        Your Stripe Express account has been created. Stripe typically completes identity verification
                        within 1–2 business days. You will be notified once charges are enabled and tenant payments
                        can flow to your account.
                    </p>
                    <p className="text-sm text-zinc-500">
                        Need to update your details?{' '}
                        <a
                            href="https://express.stripe.com"
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-indigo-400 hover:text-indigo-300 inline-flex items-center gap-1"
                        >
                            Open Stripe Express Dashboard <ExternalLink size={12} />
                        </a>
                    </p>
                </div>
            )}

            {status?.isConnected && status.chargesEnabled && (
                <div className="rounded-xl border border-emerald-500/20 bg-zinc-900 p-6 space-y-4">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center">
                            <CheckCircle2 size={20} className="text-emerald-400" />
                        </div>
                        <div>
                            <div className="flex items-center gap-2">
                                <p className="font-medium text-zinc-100">Payouts Active</p>
                                <span className="text-[10px] uppercase tracking-widest px-2 py-0.5 rounded-full bg-emerald-500/15 text-emerald-400 font-medium">
                                    Active
                                </span>
                            </div>
                            <p className="text-xs text-zinc-500 mt-0.5">
                                Tenant payments are flowing directly to your bank account
                            </p>
                        </div>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                        <div className="rounded-lg bg-zinc-800/60 px-4 py-3">
                            <p className="text-xs text-zinc-500 mb-0.5">Charges</p>
                            <p className="text-sm font-medium text-emerald-400">Enabled</p>
                        </div>
                        <div className="rounded-lg bg-zinc-800/60 px-4 py-3">
                            <p className="text-xs text-zinc-500 mb-0.5">Payouts</p>
                            <p className={`text-sm font-medium ${status.payoutsEnabled ? 'text-emerald-400' : 'text-yellow-400'}`}>
                                {status.payoutsEnabled ? 'Enabled' : 'Pending'}
                            </p>
                        </div>
                    </div>
                    <a
                        href="https://express.stripe.com"
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center justify-center gap-2 w-full py-2.5 px-4 rounded-lg border border-zinc-700 hover:bg-zinc-800 text-zinc-300 text-sm transition-colors"
                    >
                        Manage in Stripe Express Dashboard <ExternalLink size={14} />
                    </a>
                </div>
            )}
        </div>
    );
}

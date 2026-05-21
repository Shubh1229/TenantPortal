'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api/auth';
import { SubscriptionStatusResponse, SubscriptionStatusValue } from '@/types';
import { CreditCard, AlertTriangle, CheckCircle, Clock, XCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';

function StatusBadge({ status }: { status: SubscriptionStatusValue }) {
    const config: Record<SubscriptionStatusValue, { label: string; className: string; icon: React.ReactNode }> = {
        Active:   { label: 'Active',   className: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20', icon: <CheckCircle size={13} /> },
        Trialing: { label: 'Trial',    className: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20',   icon: <Clock size={13} /> },
        PastDue:  { label: 'Past Due', className: 'bg-amber-500/10 text-amber-400 border-amber-500/20',      icon: <AlertTriangle size={13} /> },
        Canceled: { label: 'Canceled', className: 'bg-red-500/10 text-red-400 border-red-500/20',            icon: <XCircle size={13} /> },
        None:     { label: 'Inactive', className: 'bg-zinc-500/10 text-zinc-400 border-zinc-500/20',         icon: <XCircle size={13} /> },
    };
    const c = config[status] ?? config.None;
    return (
        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${c.className}`}>
            {c.icon} {c.label}
        </span>
    );
}

export default function SubscriptionPage() {
    const router = useRouter();
    const [status, setStatus] = useState<SubscriptionStatusResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [portalLoading, setPortalLoading] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        authApi.getSubscriptionStatus()
            .then(setStatus)
            .catch(() => setError('Failed to load subscription details.'))
            .finally(() => setIsLoading(false));
    }, []);

    async function handleManageBilling() {
        setPortalLoading(true);
        try {
            const { url } = await authApi.getBillingPortalUrl(window.location.href);
            window.location.href = url;
        } catch {
            setError('Failed to open billing portal. Please try again.');
        } finally {
            setPortalLoading(false);
        }
    }

    const daysLeft = status?.trialEnd
        ? Math.ceil((new Date(status.trialEnd).getTime() - Date.now()) / 86_400_000)
        : null;

    const tenantUsagePct = status?.maxTenants
        ? Math.min(100, Math.round((status.currentTenantCount / status.maxTenants) * 100))
        : 0;

    if (isLoading) {
        return (
            <div className="flex items-center justify-center h-48 text-zinc-500 text-sm">
                Loading subscription details...
            </div>
        );
    }

    return (
        <div className="max-w-xl space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-white">Subscription</h1>
                <p className="text-sm text-zinc-500 mt-0.5">Manage your Singh Resident Hub plan and billing.</p>
            </div>

            {error && (
                <div className="rounded-lg bg-red-500/10 border border-red-500/20 px-4 py-3 text-sm text-red-400">
                    {error}
                </div>
            )}

            {status && (
                <>
                    {/* Status card */}
                    <div className="rounded-xl bg-zinc-900 border border-zinc-800 p-5 space-y-4">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <CreditCard size={18} className="text-zinc-500" />
                                <span className="text-sm font-medium text-zinc-200">Plan Status</span>
                            </div>
                            <StatusBadge status={status.status} />
                        </div>

                        {/* PastDue warning */}
                        {status.status === 'PastDue' && (
                            <div className="rounded-lg bg-amber-500/10 border border-amber-500/20 px-4 py-3 text-sm text-amber-300 flex items-start gap-2.5">
                                <AlertTriangle size={15} className="mt-0.5 shrink-0" />
                                Your last payment failed. Update your payment method to restore access.
                            </div>
                        )}

                        {/* Trial countdown */}
                        {status.status === 'Trialing' && daysLeft !== null && (
                            <div className={`rounded-lg border px-4 py-3 text-sm flex items-start gap-2.5 ${
                                daysLeft <= 3
                                    ? 'bg-amber-500/10 border-amber-500/20 text-amber-300'
                                    : 'bg-indigo-500/10 border-indigo-500/20 text-indigo-300'
                            }`}>
                                <Clock size={15} className="mt-0.5 shrink-0" />
                                {daysLeft <= 0
                                    ? 'Your trial ends today. Add a payment method now to keep your account active.'
                                    : `Your free trial ends in ${daysLeft === 1 ? '1 day' : `${daysLeft} days`}. Add a payment method before it expires to continue without interruption.`}
                            </div>
                        )}

                        <div className="flex items-center justify-between text-sm">
                            <span className="text-zinc-400">Plan</span>
                            <span className="text-zinc-200">$20 / month</span>
                        </div>
                    </div>

                    {/* Tenant usage */}
                    <div className="rounded-xl bg-zinc-900 border border-zinc-800 p-5 space-y-3">
                        <div className="flex items-center justify-between text-sm">
                            <span className="text-zinc-400 font-medium">Tenant Usage</span>
                            <span className="text-zinc-300">
                                {status.currentTenantCount}
                                {status.maxTenants != null ? ` / ${status.maxTenants}` : ''} active tenants
                            </span>
                        </div>
                        {status.maxTenants != null && (
                            <div className="h-2 rounded-full bg-zinc-800 overflow-hidden">
                                <div
                                    className={`h-full rounded-full transition-all ${
                                        tenantUsagePct >= 90 ? 'bg-amber-500' : 'bg-indigo-500'
                                    }`}
                                    style={{ width: `${tenantUsagePct}%` }}
                                />
                            </div>
                        )}
                        {status.maxTenants != null && tenantUsagePct >= 90 && (
                            <p className="text-xs text-amber-400">
                                You&apos;re near your tenant limit. Contact support to increase your plan.
                            </p>
                        )}
                    </div>

                    {/* Manage billing — only for accounts with an active Stripe subscription */}
                    {status.status !== 'None' ? (
                        <>
                            <Button
                                onClick={handleManageBilling}
                                disabled={portalLoading}
                                className="bg-indigo-600 hover:bg-indigo-500 text-white"
                            >
                                {portalLoading ? 'Opening portal...' : 'Manage Billing'}
                            </Button>
                            <p className="text-xs text-zinc-600">
                                You&apos;ll be redirected to Stripe&apos;s secure billing portal to update your payment method, view invoices, or cancel.
                            </p>
                        </>
                    ) : (
                        <p className="text-sm text-zinc-500">
                            No Stripe subscription on file. Billing management is only available for accounts registered through the sign-up flow.
                        </p>
                    )}
                </>
            )}
        </div>
    );
}

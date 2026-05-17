'use client';

import { Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Building2, CheckCircle } from 'lucide-react';

// This page is the landing point after Stripe Checkout redirects back.
// URL: /subscription/success?session_id=cs_xxx
// The account activation is driven by the webhook (customer.subscription.created),
// which fires asynchronously after checkout. The account may not be active yet
// when this page loads — tell the user to wait a moment and then sign in.

function SuccessContent() {
    const searchParams = useSearchParams();
    const sessionId = searchParams.get('session_id');
    const canceled  = searchParams.get('canceled');

    if (canceled) {
        return (
            <div className="text-center space-y-4">
                <h1 className="text-2xl font-semibold text-white">Checkout canceled</h1>
                <p className="text-sm text-zinc-400 max-w-sm mx-auto">
                    You&apos;ve been returned to the registration page. Your account credentials
                    have been saved — you can complete billing whenever you&apos;re ready.
                </p>
                <Link href="/register/admin">
                    <Button className="mt-2 bg-indigo-600 hover:bg-indigo-500 text-white">
                        Back to registration
                    </Button>
                </Link>
            </div>
        );
    }

    if (!sessionId) {
        return (
            <div className="text-center space-y-3">
                <p className="text-sm text-zinc-400">Invalid or missing session. Please contact support.</p>
                <Link href="/login">
                    <Button variant="ghost" className="text-zinc-400">Go to login</Button>
                </Link>
            </div>
        );
    }

    return (
        <div className="text-center space-y-5">
            <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center mx-auto">
                <CheckCircle size={32} className="text-emerald-400" />
            </div>
            <div>
                <h1 className="text-2xl font-semibold text-white">You&apos;re subscribed!</h1>
                <p className="mt-2 text-sm text-zinc-400 max-w-sm mx-auto">
                    Payment received. Your account is being activated — this usually takes a few seconds.
                    Once active, sign in with the email and password you registered with.
                </p>
            </div>

            {/* What to expect */}
            <div className="rounded-xl bg-zinc-900 border border-zinc-800 p-5 text-left space-y-3 max-w-sm mx-auto">
                <p className="text-xs text-zinc-500 uppercase tracking-wide font-medium">Next steps</p>
                {[
                    'Open your authenticator app',
                    'Scan the QR code you were shown during setup',
                    'Sign in using your email, password, and the 6-digit code',
                    'Start inviting tenants from the dashboard',
                ].map((step, i) => (
                    <div key={i} className="flex items-start gap-3 text-sm text-zinc-300">
                        <span className="w-5 h-5 rounded-full bg-indigo-600/20 text-indigo-400 text-xs flex items-center justify-center shrink-0 font-medium mt-0.5">
                            {i + 1}
                        </span>
                        {step}
                    </div>
                ))}
            </div>

            <Link href="/login">
                <Button className="bg-indigo-600 hover:bg-indigo-500 text-white px-8">
                    Sign in to your account
                </Button>
            </Link>
        </div>
    );
}

export default function SubscriptionSuccessPage() {
    return (
        <div className="min-h-screen bg-zinc-950 flex flex-col items-center justify-center px-6 py-12">
            {/* Brand mark */}
            <div className="flex items-center gap-3 mb-10">
                <div className="w-9 h-9 rounded-xl bg-indigo-600 flex items-center justify-center">
                    <Building2 size={20} className="text-white" />
                </div>
                <span className="text-lg font-semibold text-white">Singh Resident Hub</span>
            </div>

            <Suspense fallback={<div className="text-zinc-400 text-sm">Loading...</div>}>
                <SuccessContent />
            </Suspense>
        </div>
    );
}

'use client';

import { Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { CheckCircle, XCircle, Clock } from 'lucide-react';

function CompleteContent() {
    const searchParams = useSearchParams();
    const status = searchParams.get('redirect_status');
    // Stripe also passes payment_intent and payment_intent_client_secret — unused here

    const isSuccess  = status === 'succeeded';
    const isProcessing = status === 'processing';
    // Any other value (requires_payment_method, requires_action, etc.) is a failure

    if (isSuccess) {
        return (
            <div className="text-center space-y-4">
                <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center mx-auto">
                    <CheckCircle size={32} className="text-emerald-400" />
                </div>
                <h1 className="text-2xl font-semibold text-white">Payment confirmed</h1>
                <p className="text-sm text-zinc-400 max-w-xs mx-auto">
                    Your payment was successful. Your admin will see it as pending confirmation.
                    You&apos;ll receive an in-app notification once it&apos;s approved.
                </p>
                <Link href="/tenant">
                    <Button className="mt-2 bg-indigo-600 hover:bg-indigo-500 text-white">
                        Back to dashboard
                    </Button>
                </Link>
            </div>
        );
    }

    if (isProcessing) {
        return (
            <div className="text-center space-y-4">
                <div className="w-16 h-16 rounded-full bg-yellow-500/10 flex items-center justify-center mx-auto">
                    <Clock size={32} className="text-yellow-400" />
                </div>
                <h1 className="text-2xl font-semibold text-white">Payment processing</h1>
                <p className="text-sm text-zinc-400 max-w-xs mx-auto">
                    Your payment is being processed. ACH payments typically settle in 1–3 business days.
                    You&apos;ll receive a notification once it&apos;s confirmed.
                </p>
                <Link href="/tenant">
                    <Button className="mt-2 bg-indigo-600 hover:bg-indigo-500 text-white">
                        Back to dashboard
                    </Button>
                </Link>
            </div>
        );
    }

    // Failed / unknown status
    return (
        <div className="text-center space-y-4">
            <div className="w-16 h-16 rounded-full bg-red-500/10 flex items-center justify-center mx-auto">
                <XCircle size={32} className="text-red-400" />
            </div>
            <h1 className="text-2xl font-semibold text-white">Payment failed</h1>
            <p className="text-sm text-zinc-400 max-w-xs mx-auto">
                Something went wrong. Please check your payment details and try again.
            </p>
            <Link href="/tenant/payment">
                <Button className="mt-2 bg-indigo-600 hover:bg-indigo-500 text-white">
                    Try again
                </Button>
            </Link>
        </div>
    );
}

export default function PaymentCompletePage() {
    return (
        <div className="flex items-center justify-center min-h-[60vh]">
            <Suspense fallback={<div className="text-zinc-400 text-sm">Loading...</div>}>
                <CompleteContent />
            </Suspense>
        </div>
    );
}

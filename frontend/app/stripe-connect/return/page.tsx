'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { CheckCircle2 } from 'lucide-react';

export default function StripeConnectReturnPage() {
    const router = useRouter();

    useEffect(() => {
        const timer = setTimeout(() => {
            router.push('/admin/payouts');
        }, 2500);
        return () => clearTimeout(timer);
    }, [router]);

    return (
        <div className="flex min-h-screen items-center justify-center bg-zinc-950">
            <div className="text-center space-y-4 px-6">
                <div className="flex justify-center">
                    <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center">
                        <CheckCircle2 size={36} className="text-emerald-400" />
                    </div>
                </div>
                <h1 className="text-xl font-semibold text-zinc-100">Bank Account Connected</h1>
                <p className="text-sm text-zinc-400">
                    Returning you to the portal in a moment…
                </p>
                <p className="text-xs text-zinc-600">
                    Not redirected?{' '}
                    <a href="/admin/payouts" className="text-indigo-400 hover:text-indigo-300">
                        Click here
                    </a>
                </p>
            </div>
        </div>
    );
}

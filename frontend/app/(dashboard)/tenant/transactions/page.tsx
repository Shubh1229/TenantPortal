'use client';

import { useEffect, useState } from 'react';
import { transactionsApi } from '@/lib/api/transactions';
import { Transaction } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

const STATUS_COLORS: Record<string, string> = {
    Confirmed: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
    Pending:   'bg-amber-500/10  text-amber-400  border-amber-500/20',
    Declined:  'bg-red-500/10   text-red-400    border-red-500/20',
    Overdue:   'bg-orange-500/10 text-orange-400 border-orange-500/20',
};

const METHOD_LABELS: Record<string, string> = {
    Stripe:   'Card (Stripe)',
    Ach:      'ACH (Stripe)',
    External: 'External',
    Manual:   'Manual',
};

export default function TenantTransactionsPage() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        transactionsApi.getAll()
            .then(txns => setTransactions([...txns].sort(
                (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
            )))
            .catch(console.error)
            .finally(() => setIsLoading(false));
    }, []);

    const total = transactions
        .filter(t => t.status === 'Confirmed')
        .reduce((sum, t) => sum + t.amount, 0);

    if (isLoading) return <div className="text-zinc-400">Loading transactions…</div>;

    return (
        <div className="space-y-6">
            <div className="flex items-end justify-between">
                <h2 className="text-2xl font-semibold">My Transactions</h2>
                <p className="text-sm text-zinc-500">
                    Total confirmed:{' '}
                    <span className="text-emerald-400 font-medium">
                        ${total.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                    </span>
                </p>
            </div>

            {transactions.length === 0 ? (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardContent className="py-8 text-center text-zinc-500">
                        No transactions yet.
                    </CardContent>
                </Card>
            ) : (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader>
                        <CardTitle className="text-base">Transaction History</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-0">
                            <div className="grid grid-cols-[1fr_110px_110px_100px] gap-3 px-1 pb-2 text-[11px] uppercase tracking-wider text-zinc-600 font-medium border-b border-zinc-800">
                                <span>Date</span>
                                <span>Amount</span>
                                <span>Method</span>
                                <span>Status</span>
                            </div>
                            {transactions.map(t => (
                                <div
                                    key={t.id}
                                    className="grid grid-cols-[1fr_110px_110px_100px] gap-3 px-1 py-3 border-b border-zinc-800/50 last:border-0 items-center"
                                >
                                    <div>
                                        <p className="text-sm text-zinc-200">
                                            {t.type}
                                        </p>
                                        <p className="text-xs text-zinc-500">
                                            {t.paidDate
                                                ? `Paid ${new Date(t.paidDate).toLocaleDateString()}`
                                                : `Submitted ${new Date(t.createdAt).toLocaleDateString()}`}
                                        </p>
                                        {t.externalMethodNote && (
                                            <p className="text-xs text-zinc-600 mt-0.5">{t.externalMethodNote}</p>
                                        )}
                                        {t.status === 'Pending' && t.paymentMethod === 'External' && (
                                            <p className="text-xs text-amber-500/80 mt-0.5">Awaiting admin approval</p>
                                        )}
                                    </div>
                                    <span className="text-sm font-medium text-zinc-100">
                                        ${t.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                                    </span>
                                    <span className="text-sm text-zinc-400">
                                        {METHOD_LABELS[t.paymentMethod] ?? t.paymentMethod}
                                    </span>
                                    <Badge className={`text-xs border ${STATUS_COLORS[t.status] ?? ''}`}>
                                        {t.status}
                                    </Badge>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}

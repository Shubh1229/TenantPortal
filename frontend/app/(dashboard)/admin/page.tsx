'use client';

import { useEffect, useState } from 'react';
import dynamic from 'next/dynamic';
import { transactionsApi } from '@/lib/api/transactions';
import { Transaction } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { DollarSign, Clock, AlertTriangle, TrendingUp } from 'lucide-react';

// Dynamic import keeps recharts out of the SSR bundle
const RevenueChart = dynamic(() => import('./_components/RevenueChart'), { ssr: false });

const STATUS_STYLES: Record<string, string> = {
    Confirmed: 'bg-emerald-500/10 text-emerald-400 ring-1 ring-emerald-500/20',
    Pending:   'bg-yellow-500/10 text-yellow-400 ring-1 ring-yellow-500/20',
    Declined:  'bg-red-500/10 text-red-400 ring-1 ring-red-500/20',
    Overdue:   'bg-red-600/20 text-red-300 ring-1 ring-red-600/30',
};

function StatusBadge({ status }: { status: string }) {
    return (
        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_STYLES[status] ?? 'bg-zinc-700 text-zinc-300'}`}>
            {status}
        </span>
    );
}

interface StatCardProps {
    title: string;
    value: string;
    sub?: string;
    icon: React.ReactNode;
    accent: string;
}

function StatCard({ title, value, sub, icon, accent }: StatCardProps) {
    return (
        <Card className="bg-zinc-900 border-zinc-800">
            <CardContent className="pt-5 pb-5">
                <div className="flex items-start justify-between">
                    <div>
                        <p className="text-xs text-zinc-500 uppercase tracking-wide mb-1">{title}</p>
                        <p className="text-2xl font-bold text-white">{value}</p>
                        {sub && <p className="text-xs text-zinc-500 mt-0.5">{sub}</p>}
                    </div>
                    <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${accent}`}>
                        {icon}
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

export default function AdminDashboard() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        transactionsApi.getAll()
            .then(setTransactions)
            .catch(console.error)
            .finally(() => setIsLoading(false));
    }, []);

    const pendingRequests = transactions.filter(t => t.status === 'Pending');
    const overduePayments = transactions.filter(t => t.status === 'Overdue');
    const confirmedRevenue = transactions
        .filter(t => t.status === 'Confirmed')
        .reduce((sum, t) => sum + t.amount, 0);

    async function handleApprove(id: string) {
        try {
            await transactionsApi.approve(id);
            setTransactions(prev => prev.map(t => t.id === id ? { ...t, status: 'Confirmed' } : t));
        } catch (e) { console.error(e); }
    }

    async function handleDecline(id: string) {
        try {
            await transactionsApi.decline(id);
            setTransactions(prev => prev.map(t => t.id === id ? { ...t, status: 'Declined' } : t));
        } catch (e) { console.error(e); }
    }

    if (isLoading) {
        return <div className="text-zinc-400 text-sm">Loading...</div>;
    }

    return (
        <div className="space-y-7">
            <div>
                <h1 className="text-2xl font-semibold text-white">Admin Dashboard</h1>
                <p className="text-sm text-zinc-500 mt-0.5">Overview of your portfolio</p>
            </div>

            {/* Stat cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <StatCard
                    title="Confirmed Revenue"
                    value={`$${confirmedRevenue.toFixed(0)}`}
                    sub="all time"
                    icon={<DollarSign size={18} className="text-emerald-400" />}
                    accent="bg-emerald-500/10"
                />
                <StatCard
                    title="Total Transactions"
                    value={String(transactions.length)}
                    icon={<TrendingUp size={18} className="text-indigo-400" />}
                    accent="bg-indigo-500/10"
                />
                <StatCard
                    title="Pending Approval"
                    value={String(pendingRequests.length)}
                    icon={<Clock size={18} className="text-yellow-400" />}
                    accent="bg-yellow-500/10"
                />
                <StatCard
                    title="Overdue"
                    value={String(overduePayments.length)}
                    icon={<AlertTriangle size={18} className="text-red-400" />}
                    accent="bg-red-500/10"
                />
            </div>

            {/* Revenue chart */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-zinc-400">Revenue Over Time</CardTitle>
                </CardHeader>
                <CardContent>
                    <RevenueChart transactions={transactions} />
                </CardContent>
            </Card>

            {/* Pending approvals */}
            {pendingRequests.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader>
                        <CardTitle className="text-base text-white">Pending Payment Requests</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-1">
                            {pendingRequests.map(t => (
                                <div
                                    key={t.id}
                                    className="flex items-center justify-between py-3 border-b border-zinc-800 last:border-0"
                                >
                                    <div>
                                        <p className="text-sm font-medium text-zinc-200">{t.type}</p>
                                        <p className="text-xs text-zinc-500">
                                            ${t.amount.toFixed(2)}{t.paidDate ? ` · ${new Date(t.paidDate).toLocaleDateString()}` : ''}
                                        </p>
                                        {t.externalMethodNote && (
                                            <p className="text-xs text-zinc-600 italic">{t.externalMethodNote}</p>
                                        )}
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Button
                                            size="sm"
                                            className="bg-emerald-600 hover:bg-emerald-500 text-white h-7 text-xs"
                                            onClick={() => handleApprove(t.id)}
                                        >
                                            Approve
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="destructive"
                                            className="h-7 text-xs"
                                            onClick={() => handleDecline(t.id)}
                                        >
                                            Decline
                                        </Button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* All transactions */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader>
                    <CardTitle className="text-base text-white">All Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    {transactions.length === 0 ? (
                        <p className="text-sm text-zinc-500">No transactions found.</p>
                    ) : (
                        <div className="space-y-1">
                            {transactions.map(t => (
                                <div
                                    key={t.id}
                                    className="flex items-center justify-between py-3 border-b border-zinc-800 last:border-0"
                                >
                                    <div>
                                        <p className="text-sm font-medium text-zinc-200">{t.type}</p>
                                        <p className="text-xs text-zinc-500">
                                            {t.dueDate ? `Due ${new Date(t.dueDate).toLocaleDateString()}` : 'No due date'}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <StatusBadge status={t.status} />
                                        <span className="text-sm font-semibold text-zinc-200">
                                            ${t.amount.toFixed(2)}
                                        </span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

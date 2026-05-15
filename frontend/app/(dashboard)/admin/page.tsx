'use client';

import { useEffect, useState } from 'react';
import { transactionsApi } from '@/lib/api/transactions';
import { Transaction } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

function StatusBadge({ status }: { status: string }) {
    const variants: Record<string, string> = {
        Confirmed: 'bg-green-100 text-green-800',
        Pending: 'bg-yellow-100 text-yellow-800',
        Declined: 'bg-red-100 text-red-800',
        Overdue: 'bg-red-200 text-red-900',
    };
    return (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${variants[status] ?? 'bg-slate-100'}`}>
            {status}
        </span>
    );
}

export default function AdminDashboard() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        async function load() {
            try {
                const txns = await transactionsApi.getAll();
                setTransactions(txns);
            } catch (e) {
                console.error(e);
            } finally {
                setIsLoading(false);
            }
        }
        load();
    }, []);

    const pendingRequests = transactions.filter(t => t.status === 'Pending');
    const overduePayments = transactions.filter(t => t.status === 'Overdue');

    async function handleApprove(id: string) {
        try {
            await transactionsApi.approve(id);
            setTransactions(prev =>
                prev.map(t => t.id === id ? { ...t, status: 'Confirmed' } : t)
            );
        } catch (e) {
            console.error(e);
        }
    }

    async function handleDecline(id: string) {
        try {
            await transactionsApi.decline(id);
            setTransactions(prev =>
                prev.map(t => t.id === id ? { ...t, status: 'Declined' } : t)
            );
        } catch (e) {
            console.error(e);
        }
    }

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Admin Dashboard</h2>

            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Total Transactions</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <p className="text-2xl font-bold">{transactions.length}</p>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Pending Approval</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <p className="text-2xl font-bold text-yellow-600">{pendingRequests.length}</p>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Overdue</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <p className={`text-2xl font-bold ${overduePayments.length > 0 ? 'text-red-600' : ''}`}>
                            {overduePayments.length}
                        </p>
                    </CardContent>
                </Card>
            </div>

            {/* Pending Approval Requests */}
            {pendingRequests.length > 0 && (
                <Card>
                    <CardHeader>
                        <CardTitle>Pending Payment Requests</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {pendingRequests.map(t => (
                                <div key={t.id} className="flex items-center justify-between py-2 border-b last:border-0">
                                    <div>
                                        <p className="text-sm font-medium">{t.type}</p>
                                        <p className="text-xs text-slate-500">
                                            ${t.amount.toFixed(2)} —{' '}
                                            {t.paidDate ? new Date(t.paidDate).toLocaleDateString() : 'No date'}
                                        </p>
                                        {t.externalMethodNote && (
                                            <p className="text-xs text-slate-400 italic">{t.externalMethodNote}</p>
                                        )}
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Button size="sm" onClick={() => handleApprove(t.id)}>
                                            Approve
                                        </Button>
                                        <Button size="sm" variant="destructive" onClick={() => handleDecline(t.id)}>
                                            Decline
                                        </Button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* All Transactions */}
            <Card>
                <CardHeader>
                    <CardTitle>All Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    {transactions.length === 0 ? (
                        <p className="text-sm text-slate-500">No transactions found.</p>
                    ) : (
                        <div className="space-y-3">
                            {transactions.map(t => (
                                <div key={t.id} className="flex items-center justify-between py-2 border-b last:border-0">
                                    <div>
                                        <p className="text-sm font-medium">{t.type}</p>
                                        <p className="text-xs text-slate-500">
                                            {t.dueDate ? `Due ${new Date(t.dueDate).toLocaleDateString()}` : 'No due date'}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <StatusBadge status={t.status} />
                                        <span className="text-sm font-semibold">${t.amount.toFixed(2)}</span>
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
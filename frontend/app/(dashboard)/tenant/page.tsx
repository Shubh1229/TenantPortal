'use client';

import { useEffect, useState } from 'react';
import { transactionsApi } from '@/lib/api/transactions';
import { notificationsApi } from '@/lib/api/notifications';
import { Transaction, Notification, RentSchedule } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

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

export default function TenantDashboard() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        async function load() {
            try {
                const [txns, notifs] = await Promise.all([
                    transactionsApi.getAll(),
                    notificationsApi.getAll(),
                ]);
                setTransactions(txns);
                setNotifications(notifs.filter(n => !n.isRead).slice(0, 5));
            } catch (e) {
                console.error(e);
            } finally {
                setIsLoading(false);
            }
        }
        load();
    }, []);

    const nextDue = transactions
        .filter(t => t.status !== 'Confirmed' && t.dueDate)
        .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())[0];

    const overdueCount = transactions.filter(t => t.status === 'Overdue').length;
    const pendingCount = transactions.filter(t => t.status === 'Pending').length;

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">My Dashboard</h2>

            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Next Payment Due</CardTitle>
                    </CardHeader>
                    <CardContent>
                        {nextDue ? (
                            <>
                                <p className="text-2xl font-bold">${nextDue.amount.toFixed(2)}</p>
                                <p className="text-sm text-slate-500">
                                    Due {new Date(nextDue.dueDate!).toLocaleDateString()}
                                </p>
                            </>
                        ) : (
                            <p className="text-sm text-slate-500">No upcoming payments</p>
                        )}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Pending Requests</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <p className="text-2xl font-bold">{pendingCount}</p>
                        <p className="text-sm text-slate-500">Awaiting approval</p>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-slate-500">Overdue</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <p className={`text-2xl font-bold ${overdueCount > 0 ? 'text-red-600' : ''}`}>
                            {overdueCount}
                        </p>
                        <p className="text-sm text-slate-500">Payments overdue</p>
                    </CardContent>
                </Card>
            </div>

            {/* Recent Transactions */}
            <Card>
                <CardHeader>
                    <CardTitle>Recent Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    {transactions.length === 0 ? (
                        <p className="text-sm text-slate-500">No transactions found.</p>
                    ) : (
                        <div className="space-y-3">
                            {transactions.slice(0, 10).map(t => (
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

            {/* Unread Notifications */}
            {notifications.length > 0 && (
                <Card>
                    <CardHeader>
                        <CardTitle>Unread Notifications</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {notifications.map(n => (
                                <div key={n.id} className="flex items-start gap-3 py-2 border-b last:border-0">
                                    <div className="w-2 h-2 rounded-full bg-blue-500 mt-1.5 shrink-0" />
                                    <div>
                                        <p className="text-sm">{n.message}</p>
                                        <p className="text-xs text-slate-500">
                                            {new Date(n.createdAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
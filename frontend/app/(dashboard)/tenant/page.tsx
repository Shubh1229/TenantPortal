'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { notificationsApi } from '@/lib/api/notifications';
import { Transaction, Notification, UnitPropertyInfo, PublicUserProfile, RentSchedule } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { AlertTriangle, Building2, CalendarClock, Clock, CreditCard, Home, User2 } from 'lucide-react';

function HomeInfoRow({ label, value }: { label: string; value: string }) {
    return (
        <div className="flex justify-between gap-2">
            <span className="text-zinc-500 shrink-0">{label}</span>
            <span className="text-zinc-200 text-right">{value}</span>
        </div>
    );
}

function ordinalSuffix(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return s[(v - 20) % 10] ?? s[v] ?? s[0];
}

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
    href?: string;
}

function StatCard({ title, value, sub, icon, accent, href }: StatCardProps) {
    const inner = (
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
    );

    return (
        <Card className={`bg-zinc-900 border-zinc-800 ${href ? 'hover:border-zinc-600 transition-colors cursor-pointer' : ''}`}>
            {href ? <Link href={href}>{inner}</Link> : inner}
        </Card>
    );
}

export default function TenantDashboard() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [unitInfo, setUnitInfo] = useState<UnitPropertyInfo | null>(null);
    const [adminProfile, setAdminProfile] = useState<PublicUserProfile | null>(null);
    const [rentSchedule, setRentSchedule] = useState<RentSchedule | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        async function loadAll() {
            try {
                const [txns, notifs, info, schedule] = await Promise.all([
                    transactionsApi.getAll(),
                    notificationsApi.getAll(),
                    transactionsApi.getMyUnitInfo().catch(() => null),
                    transactionsApi.getMyRentSchedule().catch(() => null),
                ]);
                setTransactions(txns);
                setNotifications(notifs.filter(n => !n.isRead).slice(0, 5));
                setUnitInfo(info);
                setRentSchedule(schedule);
                if (info?.adminId) {
                    authApi.getPublicProfile(info.adminId).then(setAdminProfile).catch(() => null);
                }
            } catch (e) { console.error(e); }
            finally { setIsLoading(false); }
        }
        loadAll();
    }, []);

    const nextDue = transactions
        .filter(t => t.status !== 'Confirmed' && t.dueDate)
        .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())[0];

    const overdueCount = transactions.filter(t => t.status === 'Overdue').length;
    const pendingCount = transactions.filter(t => t.status === 'Pending').length;

    if (isLoading) {
        return <div className="text-zinc-400 text-sm">Loading...</div>;
    }

    return (
        <div className="space-y-7">
            <div>
                <h1 className="text-2xl font-semibold text-white">My Dashboard</h1>
                <p className="text-sm text-zinc-500 mt-0.5">Your account at a glance</p>
            </div>

            {/* Stat cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <StatCard
                    title="Next Payment"
                    value={nextDue ? `$${nextDue.amount.toFixed(2)}` : '—'}
                    sub={nextDue ? `Due ${new Date(nextDue.dueDate!).toLocaleDateString()}` : 'All caught up'}
                    icon={<CalendarClock size={18} className="text-indigo-400" />}
                    accent="bg-indigo-500/10"
                    href="/tenant/payment"
                />
                <StatCard
                    title="Pending Requests"
                    value={String(pendingCount)}
                    sub="awaiting approval"
                    icon={<Clock size={18} className="text-yellow-400" />}
                    accent="bg-yellow-500/10"
                />
                <StatCard
                    title="Overdue"
                    value={String(overdueCount)}
                    sub={overdueCount > 0 ? 'action required' : 'nothing overdue'}
                    icon={<AlertTriangle size={18} className={overdueCount > 0 ? 'text-red-400' : 'text-zinc-500'} />}
                    accent={overdueCount > 0 ? 'bg-red-500/10' : 'bg-zinc-800'}
                />
                <StatCard
                    title="Make Payment"
                    value="Pay Now"
                    sub="card or ACH"
                    icon={<CreditCard size={18} className="text-emerald-400" />}
                    accent="bg-emerald-500/10"
                    href="/tenant/payment"
                />
            </div>

            {/* My Home */}
            {(unitInfo || rentSchedule) && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader className="pb-2">
                        <CardTitle className="text-base text-white flex items-center gap-2">
                            <Home size={15} className="text-indigo-400" />
                            My Home
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-5">
                        {unitInfo && (
                            <div className="space-y-2">
                                <p className="text-[11px] uppercase tracking-wider text-zinc-500 font-medium flex items-center gap-1.5">
                                    <Building2 size={11} /> Property &amp; Unit
                                </p>
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1.5 text-sm">
                                    <HomeInfoRow label="Property" value={unitInfo.propertyName} />
                                    <HomeInfoRow label="Address" value={unitInfo.propertyAddress} />
                                    <HomeInfoRow label="Unit" value={`Unit ${unitInfo.unitNumber}`} />
                                    {unitInfo.bedrooms != null && <HomeInfoRow label="Bedrooms" value={String(unitInfo.bedrooms)} />}
                                    {unitInfo.bathrooms != null && <HomeInfoRow label="Bathrooms" value={String(unitInfo.bathrooms)} />}
                                    {unitInfo.squareFeet != null && <HomeInfoRow label="Sq Ft" value={unitInfo.squareFeet.toLocaleString()} />}
                                </div>
                            </div>
                        )}

                        {rentSchedule && (
                            <div className="space-y-2">
                                <p className="text-[11px] uppercase tracking-wider text-zinc-500 font-medium flex items-center gap-1.5">
                                    <CalendarClock size={11} /> Rent Schedule
                                </p>
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1.5 text-sm">
                                    <HomeInfoRow label="Monthly Rent" value={`$${rentSchedule.monthlyAmount.toLocaleString()}`} />
                                    <HomeInfoRow label="Due Day" value={`${rentSchedule.dueDayOfMonth}${ordinalSuffix(rentSchedule.dueDayOfMonth)} of each month`} />
                                    <HomeInfoRow label="Start Date" value={new Date(rentSchedule.startDate).toLocaleDateString()} />
                                    {rentSchedule.endDate && (
                                        <HomeInfoRow label="End Date" value={new Date(rentSchedule.endDate).toLocaleDateString()} />
                                    )}
                                </div>
                            </div>
                        )}

                        {adminProfile && (
                            <div className="space-y-2">
                                <p className="text-[11px] uppercase tracking-wider text-zinc-500 font-medium flex items-center gap-1.5">
                                    <User2 size={11} /> Property Manager
                                </p>
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1.5 text-sm">
                                    {(adminProfile.firstName || adminProfile.lastName) && (
                                        <HomeInfoRow label="Name" value={[adminProfile.firstName, adminProfile.lastName].filter(Boolean).join(' ')} />
                                    )}
                                    <HomeInfoRow label="Email" value={adminProfile.email} />
                                    {adminProfile.phoneNumber && (
                                        <HomeInfoRow label="Phone" value={adminProfile.phoneNumber} />
                                    )}
                                </div>
                            </div>
                        )}
                    </CardContent>
                </Card>
            )}

            {/* Recent transactions */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-base text-white">Recent Transactions</CardTitle>
                    <Link href="/tenant/payment">
                        <Button size="sm" className="bg-indigo-600 hover:bg-indigo-500 text-white h-7 text-xs">
                            Make Payment
                        </Button>
                    </Link>
                </CardHeader>
                <CardContent>
                    {transactions.length === 0 ? (
                        <p className="text-sm text-zinc-500">No transactions found.</p>
                    ) : (
                        <div className="space-y-1">
                            {transactions.slice(0, 10).map(t => (
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

            {/* Unread notifications */}
            {notifications.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader className="pb-2">
                        <CardTitle className="text-base text-white">Unread Notifications</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-1">
                            {notifications.map(n => (
                                <div
                                    key={n.id}
                                    className="flex items-start gap-3 py-3 border-b border-zinc-800 last:border-0"
                                >
                                    <div className="w-1.5 h-1.5 rounded-full bg-indigo-500 mt-2 shrink-0" />
                                    <div>
                                        <p className="text-sm text-zinc-200">{n.message}</p>
                                        <p className="text-xs text-zinc-500">
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

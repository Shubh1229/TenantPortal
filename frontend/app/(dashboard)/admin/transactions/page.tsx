'use client';

import { useEffect, useState, useMemo } from 'react';
import { transactionsApi } from '@/lib/api/transactions';
import { authApi } from '@/lib/api/auth';
import { Transaction, PublicUserProfile, Unit } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Check, X, Search } from 'lucide-react';

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

export default function AdminTransactionsPage() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [profiles, setProfiles] = useState<Record<string, PublicUserProfile>>({});
    const [isLoading, setIsLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        setIsLoading(true);
        try {
            const [txns, unitList] = await Promise.all([
                transactionsApi.getAll(),
                transactionsApi.getUnits(),
            ]);
            setTransactions(txns);
            setUnits(unitList);

            const tenantIds = [...new Set(txns.map(t => t.tenantId))];
            const entries = await Promise.all(
                tenantIds.map(id =>
                    authApi.getPublicProfile(id)
                        .then(p => [id, p] as const)
                        .catch(() => null)
                )
            );
            const map: Record<string, PublicUserProfile> = {};
            for (const e of entries) {
                if (e) map[e[0]] = e[1];
            }
            setProfiles(map);
        } catch (e) {
            console.error(e);
        } finally {
            setIsLoading(false);
        }
    }

    async function handleApprove(id: string) {
        setActionLoading(id);
        try {
            await transactionsApi.approve(id);
            await loadAll();
        } catch {
            alert('Failed to approve transaction.');
        } finally {
            setActionLoading(null);
        }
    }

    async function handleDecline(id: string) {
        if (!confirm('Decline this payment request?')) return;
        setActionLoading(id);
        try {
            await transactionsApi.decline(id);
            await loadAll();
        } catch {
            alert('Failed to decline transaction.');
        } finally {
            setActionLoading(null);
        }
    }

    function tenantLabel(id: string) {
        const p = profiles[id];
        if (!p) return id.slice(0, 8) + '…';
        const name = [p.firstName, p.lastName].filter(Boolean).join(' ');
        return name ? `${name} (${p.email})` : p.email;
    }

    function unitLabel(id: string) {
        const u = units.find(u => u.id === id);
        return u ? `Unit ${u.unitNumber}` : id.slice(0, 8) + '…';
    }

    const pending = transactions.filter(t => t.status === 'Pending' && t.paymentMethod === 'External');

    const filtered = useMemo(() => {
        if (!search.trim()) return transactions;
        const q = search.toLowerCase();
        return transactions.filter(t => {
            const p = profiles[t.tenantId];
            const name = p ? [p.firstName, p.lastName].filter(Boolean).join(' ').toLowerCase() : '';
            const email = p?.email.toLowerCase() ?? '';
            const unit = unitLabel(t.unitId).toLowerCase();
            return name.includes(q) || email.includes(q) || unit.includes(q) || t.status.toLowerCase().includes(q) || t.paymentMethod.toLowerCase().includes(q);
        });
    }, [transactions, profiles, units, search]);

    if (isLoading) return <div className="text-zinc-400">Loading transactions…</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Transactions</h2>

            {/* Pending approvals */}
            {pending.length > 0 && (
                <Card className="bg-zinc-900 border-amber-500/20 border">
                    <CardHeader>
                        <CardTitle className="text-base text-amber-400">
                            Pending Approval — {pending.length} request{pending.length !== 1 ? 's' : ''}
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {pending.map(t => (
                                <div
                                    key={t.id}
                                    className="flex items-center justify-between py-3 border-b border-zinc-800 last:border-0 gap-4"
                                >
                                    <div className="min-w-0 space-y-0.5">
                                        <p className="text-sm font-medium text-zinc-100 truncate">{tenantLabel(t.tenantId)}</p>
                                        <p className="text-xs text-zinc-500">
                                            {unitLabel(t.unitId)} · External · {t.externalMethodNote ?? '—'}
                                        </p>
                                        <p className="text-xs text-zinc-600">
                                            {t.paidDate ? `Paid ${new Date(t.paidDate).toLocaleDateString()}` : `Submitted ${new Date(t.createdAt).toLocaleDateString()}`}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-3 shrink-0">
                                        <span className="text-sm font-semibold text-zinc-100">
                                            ${t.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                                        </span>
                                        <Button
                                            size="sm"
                                            className="bg-emerald-600 hover:bg-emerald-500 text-white h-7 px-3 text-xs"
                                            disabled={actionLoading === t.id}
                                            onClick={() => handleApprove(t.id)}
                                        >
                                            <Check size={12} className="mr-1" />
                                            Approve
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="ghost"
                                            className="text-red-400 hover:text-red-300 h-7 px-3 text-xs"
                                            disabled={actionLoading === t.id}
                                            onClick={() => handleDecline(t.id)}
                                        >
                                            <X size={12} className="mr-1" />
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
                    <div className="flex items-center justify-between gap-4">
                        <CardTitle className="text-base">All Transactions</CardTitle>
                        <div className="relative w-64">
                            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500" />
                            <Input
                                placeholder="Search tenant, unit, status…"
                                value={search}
                                onChange={e => setSearch(e.target.value)}
                                className="pl-8 h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm placeholder:text-zinc-600"
                            />
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    {filtered.length === 0 ? (
                        <p className="text-sm text-zinc-500 py-4 text-center">No transactions found.</p>
                    ) : (
                        <div className="space-y-0">
                            {/* Header row */}
                            <div className="grid grid-cols-[1fr_120px_100px_110px_100px] gap-3 px-1 pb-2 text-[11px] uppercase tracking-wider text-zinc-600 font-medium border-b border-zinc-800">
                                <span>Tenant</span>
                                <span>Unit</span>
                                <span>Amount</span>
                                <span>Method</span>
                                <span>Status</span>
                            </div>
                            {filtered.map(t => (
                                <div
                                    key={t.id}
                                    className="grid grid-cols-[1fr_120px_100px_110px_100px] gap-3 px-1 py-3 border-b border-zinc-800/50 last:border-0 items-center"
                                >
                                    <div className="min-w-0">
                                        <p className="text-sm text-zinc-100 truncate">{tenantLabel(t.tenantId)}</p>
                                        <p className="text-xs text-zinc-600">
                                            {t.paidDate
                                                ? `Paid ${new Date(t.paidDate).toLocaleDateString()}`
                                                : new Date(t.createdAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                    <span className="text-sm text-zinc-400">{unitLabel(t.unitId)}</span>
                                    <span className="text-sm font-medium text-zinc-100">
                                        ${t.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                                    </span>
                                    <div className="space-y-0.5">
                                        <span className="text-xs text-zinc-400">{METHOD_LABELS[t.paymentMethod] ?? t.paymentMethod}</span>
                                        {t.externalMethodNote && (
                                            <p className="text-[11px] text-zinc-600 truncate">{t.externalMethodNote}</p>
                                        )}
                                    </div>
                                    <Badge className={`text-xs border ${STATUS_COLORS[t.status] ?? ''}`}>
                                        {t.status}
                                    </Badge>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

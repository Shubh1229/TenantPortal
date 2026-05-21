'use client';

import { useEffect, useState } from 'react';
import { apiRequest } from '@/lib/api/client';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { PublicUserProfile, RentSchedule, Transaction, Unit, User } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Pencil, Trash2, X, Check, ChevronRight, ArrowLeft, RotateCcw } from 'lucide-react';

type View = 'list' | 'detail';

const STATUS_COLORS: Record<string, string> = {
    Confirmed: 'text-emerald-400',
    Pending:   'text-amber-400',
    Declined:  'text-red-400',
    Overdue:   'text-orange-400',
};

const METHOD_LABELS: Record<string, string> = {
    Stripe: 'Card', Ach: 'ACH', External: 'External', Manual: 'Manual',
};

export default function RentSchedulePage() {
    const [view, setView] = useState<View>('list');
    const [tenants, setTenants] = useState<User[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [schedules, setSchedules] = useState<RentSchedule[]>([]);
    const [deletedSchedules, setDeletedSchedules] = useState<RentSchedule[]>([]);
    const [profiles, setProfiles] = useState<Record<string, PublicUserProfile>>({});
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [showDeleted, setShowDeleted] = useState(false);

    // Detail view
    const [selectedSchedule, setSelectedSchedule] = useState<RentSchedule | null>(null);
    const [detailProfile, setDetailProfile] = useState<PublicUserProfile | null>(null);

    // Create form
    const [unitId, setUnitId] = useState('');
    const [tenantId, setTenantId] = useState('');
    const [monthlyAmount, setMonthlyAmount] = useState('');
    const [dueDayOfMonth, setDueDayOfMonth] = useState('1');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [createStatus, setCreateStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [createError, setCreateError] = useState('');

    // Inline edit state
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editAmount, setEditAmount] = useState('');
    const [editDueDay, setEditDueDay] = useState('');
    const [editEndDate, setEditEndDate] = useState('');
    const [editStatus, setEditStatus] = useState<'idle' | 'saving' | 'error'>('idle');

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        const [t, u, s, d, txns] = await Promise.all([
            authApi.getUsers('Tenant').catch(() => [] as User[]),
            transactionsApi.getUnits().catch(() => [] as Unit[]),
            transactionsApi.getAllRentSchedules().catch(() => [] as RentSchedule[]),
            transactionsApi.getDeletedRentSchedules().catch(() => [] as RentSchedule[]),
            transactionsApi.getAll().catch(() => [] as Transaction[]),
        ]);
        setTenants(t);
        setUnits(u);
        setSchedules(s);
        setDeletedSchedules(d);
        setTransactions(txns);

        const allSchedules = [...s, ...d];
        const tenantIds = [...new Set(allSchedules.filter(x => x.tenantId).map(x => x.tenantId!))];
        const profileEntries = await Promise.all(
            tenantIds.map(id => authApi.getPublicProfile(id).then(p => [id, p] as const).catch(() => null))
        );
        const map: Record<string, PublicUserProfile> = {};
        for (const entry of profileEntries) { if (entry) map[entry[0]] = entry[1]; }
        setProfiles(map);
    }

    const selectedUnit = units.find(u => u.id === unitId);
    const isSharedUnit = selectedUnit?.billingMode === 'SharedUnit';
    const unitTenantIds = selectedUnit?.currentTenantIds ?? [];
    const unitTenants = tenants.filter(t => unitTenantIds.includes(t.id));

    async function openDetail(s: RentSchedule) {
        setSelectedSchedule(s);
        setDetailProfile(null);
        setView('detail');
        if (s.tenantId) {
            const p = profiles[s.tenantId] ?? await authApi.getPublicProfile(s.tenantId).catch(() => null);
            setDetailProfile(p);
        }
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setCreateStatus('loading');
        setCreateError('');
        try {
            await apiRequest('/api/rent-schedule', {
                method: 'POST',
                body: {
                    unitId,
                    tenantId: isSharedUnit ? null : (tenantId || null),
                    monthlyAmount: parseFloat(monthlyAmount),
                    dueDayOfMonth: parseInt(dueDayOfMonth),
                    startDate,
                    endDate: endDate || undefined,
                },
            });
            setCreateStatus('success');
            setUnitId(''); setTenantId(''); setMonthlyAmount(''); setDueDayOfMonth('1');
            setStartDate(''); setEndDate('');
            await loadAll();
        } catch {
            setCreateStatus('error');
            setCreateError('Failed to create rent schedule. Check the details and try again.');
        }
    }

    function startEdit(s: RentSchedule) {
        setEditingId(s.id);
        setEditAmount(s.monthlyAmount.toFixed(2));
        setEditDueDay(s.dueDayOfMonth.toString());
        setEditEndDate(s.endDate ? s.endDate.slice(0, 10) : '');
        setEditStatus('idle');
    }

    function cancelEdit() { setEditingId(null); setEditStatus('idle'); }

    async function saveEdit(id: string) {
        setEditStatus('saving');
        try {
            await transactionsApi.updateRentSchedule(id, {
                monthlyAmount: parseFloat(editAmount),
                dueDayOfMonth: parseInt(editDueDay),
                endDate: editEndDate || undefined,
            });
            setEditingId(null);
            await loadAll();
        } catch { setEditStatus('error'); }
    }

    async function handleDelete(s: RentSchedule) {
        const label = s.tenantId ? tenantLabel(s.tenantId) : unitLabel(s.unitId);
        if (!confirm(`Archive the rent schedule for ${label}? It can be restored later.`)) return;
        try {
            await transactionsApi.deleteRentSchedule(s.id);
            setSchedules(prev => prev.filter(x => x.id !== s.id));
            await loadAll();
        } catch { alert('Failed to archive rent schedule.'); }
    }

    async function handleRestore(s: RentSchedule) {
        try {
            await transactionsApi.restoreRentSchedule(s.id);
            await loadAll();
        } catch { alert('Failed to restore rent schedule.'); }
    }

    const selectClass =
        'flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 shadow-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:opacity-50';

    function tenantLabel(id?: string) {
        if (!id) return 'All tenants (shared)';
        const p = profiles[id];
        if (p) {
            const name = [p.firstName, p.lastName].filter(Boolean).join(' ');
            return name ? `${name} (${p.email})` : p.email;
        }
        return tenants.find(t => t.id === id)?.email ?? id;
    }
    function unitLabel(id: string) {
        const u = units.find(u => u.id === id);
        return u ? `Unit ${u.unitNumber}` : id;
    }

    // ── Detail view ──────────────────────────────────────────────────────────────

    if (view === 'detail' && selectedSchedule) {
        const s = selectedSchedule;
        const unitTxns = transactions
            .filter(t => t.unitId === s.unitId && (s.tenantId ? t.tenantId === s.tenantId : true))
            .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        const p = detailProfile;
        const name = p ? [p.firstName, p.lastName].filter(Boolean).join(' ') : null;

        return (
            <div className="space-y-6">
                <div className="flex items-center gap-3">
                    <button onClick={() => setView('list')} className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-200 transition-colors">
                        <ArrowLeft size={18} />
                    </button>
                    <div>
                        <h2 className="text-2xl font-semibold">{tenantLabel(s.tenantId)}</h2>
                        <p className="text-sm text-zinc-500">{unitLabel(s.unitId)}</p>
                    </div>
                </div>

                {/* Schedule details */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader className="pb-2">
                        <div className="flex items-center justify-between">
                            <CardTitle className="text-base">Rent Schedule</CardTitle>
                            <div className="flex gap-2">
                                <Button size="sm" variant="outline" className="h-7 px-3 text-xs border-zinc-700" onClick={() => startEdit(s)}>
                                    <Pencil size={12} className="mr-1" /> Edit
                                </Button>
                                <Button size="sm" variant="ghost" className="h-7 px-3 text-xs text-red-400 hover:text-red-300" onClick={() => handleDelete(s)}>
                                    <Trash2 size={12} className="mr-1" /> Archive
                                </Button>
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent>
                        {editingId === s.id ? (
                            <div className="space-y-3">
                                <div className="grid grid-cols-3 gap-3">
                                    <div className="space-y-1">
                                        <Label className="text-xs text-zinc-400">Monthly Amount ($)</Label>
                                        <Input type="number" min="0" step="0.01" value={editAmount} onChange={e => setEditAmount(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                    </div>
                                    <div className="space-y-1">
                                        <Label className="text-xs text-zinc-400">Due Day</Label>
                                        <Input type="number" min="1" max="28" value={editDueDay} onChange={e => setEditDueDay(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                    </div>
                                    <div className="space-y-1">
                                        <Label className="text-xs text-zinc-400">End Date (optional)</Label>
                                        <Input type="date" value={editEndDate} onChange={e => setEditEndDate(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                    </div>
                                </div>
                                {editStatus === 'error' && <p className="text-xs text-red-400">Failed to save. Try again.</p>}
                                <div className="flex gap-2">
                                    <Button size="sm" className="bg-indigo-600 hover:bg-indigo-500 text-white h-7 px-3 text-xs" onClick={() => saveEdit(s.id)} disabled={editStatus === 'saving'}>
                                        <Check size={12} className="mr-1" />{editStatus === 'saving' ? 'Saving…' : 'Save'}
                                    </Button>
                                    <Button size="sm" variant="ghost" className="text-zinc-400 h-7 px-3 text-xs" onClick={cancelEdit}>
                                        <X size={12} className="mr-1" />Cancel
                                    </Button>
                                </div>
                            </div>
                        ) : (
                            <div className="space-y-1.5 text-sm">
                                <InfoRow label="Monthly amount" value={`$${s.monthlyAmount.toLocaleString()}/month`} />
                                <InfoRow label="Due day" value={`${s.dueDayOfMonth}${ordinal(s.dueDayOfMonth)} of each month`} />
                                <InfoRow label="Effective" value={new Date(s.startDate).toLocaleDateString()} />
                                {s.endDate && <InfoRow label="Ends" value={new Date(s.endDate).toLocaleDateString()} />}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Tenant profile */}
                {s.tenantId && (
                    <Card className="bg-zinc-900 border-zinc-800">
                        <CardHeader><CardTitle className="text-base">Tenant</CardTitle></CardHeader>
                        <CardContent>
                            {!p ? (
                                <p className="text-sm text-zinc-500">Loading…</p>
                            ) : (
                                <div className="space-y-1.5 text-sm">
                                    {name && <InfoRow label="Name" value={name} />}
                                    <InfoRow label="Email" value={p.email} />
                                    {p.phoneNumber && <InfoRow label="Phone" value={p.phoneNumber} />}
                                    {p.emergencyContactName && (
                                        <InfoRow label="Emergency contact" value={[p.emergencyContactName, p.emergencyContactPhone].filter(Boolean).join(' · ')} />
                                    )}
                                </div>
                            )}
                        </CardContent>
                    </Card>
                )}

                {/* Linked transactions */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Transaction History</CardTitle></CardHeader>
                    <CardContent>
                        {unitTxns.length === 0 ? (
                            <p className="text-sm text-zinc-500">No transactions linked to this schedule.</p>
                        ) : (
                            <div className="space-y-0">
                                <div className="grid grid-cols-[1fr_90px_80px_80px] gap-2 pb-2 text-[11px] uppercase tracking-wider text-zinc-600 border-b border-zinc-800">
                                    <span>Date</span><span>Amount</span><span>Method</span><span>Status</span>
                                </div>
                                {unitTxns.map(t => (
                                    <div key={t.id} className="grid grid-cols-[1fr_90px_80px_80px] gap-2 py-2.5 border-b border-zinc-800/50 last:border-0 items-center">
                                        <div>
                                            <p className="text-sm text-zinc-200">{t.type}</p>
                                            <p className="text-xs text-zinc-500">{t.paidDate ? new Date(t.paidDate).toLocaleDateString() : new Date(t.createdAt).toLocaleDateString()}</p>
                                        </div>
                                        <span className="text-sm font-medium text-zinc-100">${t.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
                                        <span className="text-xs text-zinc-400">{METHOD_LABELS[t.paymentMethod] ?? t.paymentMethod}</span>
                                        <span className={`text-xs font-medium ${STATUS_COLORS[t.status] ?? 'text-zinc-400'}`}>{t.status}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        );
    }

    // ── List view ────────────────────────────────────────────────────────────────

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Rent Schedules</h2>

            {/* Active schedules */}
            {schedules.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Active Schedules</CardTitle></CardHeader>
                    <CardContent>
                        <div className="space-y-1">
                            {schedules.map(s => (
                                <div key={s.id} className="py-3 border-b border-zinc-800 last:border-0">
                                    {editingId === s.id ? (
                                        <div className="space-y-3">
                                            <div className="flex items-center gap-2 text-sm">
                                                <span className="text-zinc-400 font-medium">{tenantLabel(s.tenantId)}</span>
                                                <span className="text-zinc-600">·</span>
                                                <span className="text-zinc-500">{unitLabel(s.unitId)}</span>
                                            </div>
                                            <div className="grid grid-cols-3 gap-3">
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">Monthly Amount ($)</Label>
                                                    <Input type="number" min="0" step="0.01" value={editAmount} onChange={e => setEditAmount(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                                </div>
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">Due Day of Month</Label>
                                                    <Input type="number" min="1" max="28" value={editDueDay} onChange={e => setEditDueDay(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                                </div>
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">End Date (optional)</Label>
                                                    <Input type="date" value={editEndDate} onChange={e => setEditEndDate(e.target.value)} className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm" />
                                                </div>
                                            </div>
                                            {editStatus === 'error' && <p className="text-xs text-red-400">Failed to save. Try again.</p>}
                                            <div className="flex gap-2">
                                                <Button size="sm" className="bg-indigo-600 hover:bg-indigo-500 text-white h-7 px-3 text-xs" onClick={() => saveEdit(s.id)} disabled={editStatus === 'saving'}>
                                                    <Check size={12} className="mr-1" />{editStatus === 'saving' ? 'Saving…' : 'Save'}
                                                </Button>
                                                <Button size="sm" variant="ghost" className="text-zinc-400 h-7 px-3 text-xs" onClick={cancelEdit}>
                                                    <X size={12} className="mr-1" />Cancel
                                                </Button>
                                            </div>
                                        </div>
                                    ) : (
                                        <div className="flex items-center justify-between text-sm">
                                            <button onClick={() => openDetail(s)} className="flex items-center gap-2 text-left hover:opacity-80 transition-opacity min-w-0">
                                                <div className="min-w-0">
                                                    <span className="text-zinc-200 font-medium">{tenantLabel(s.tenantId)}</span>
                                                    <span className="text-zinc-500 ml-2">· {unitLabel(s.unitId)}</span>
                                                </div>
                                                <ChevronRight size={13} className="text-zinc-600 shrink-0" />
                                            </button>
                                            <div className="flex items-center gap-4 shrink-0 ml-4">
                                                <div className="text-right text-xs text-zinc-400 space-y-0.5">
                                                    <div>${s.monthlyAmount.toLocaleString()}/month</div>
                                                    <div>Due {s.dueDayOfMonth}{ordinal(s.dueDayOfMonth)} · {new Date(s.startDate).toLocaleDateString()} – {s.endDate ? new Date(s.endDate).toLocaleDateString() : '—'}</div>
                                                </div>
                                                <div className="flex gap-1.5">
                                                    <button onClick={() => startEdit(s)} className="p-1.5 rounded hover:bg-zinc-800 text-zinc-500 hover:text-zinc-300 transition-colors" title="Edit">
                                                        <Pencil size={13} />
                                                    </button>
                                                    <button onClick={() => handleDelete(s)} className="p-1.5 rounded hover:bg-zinc-800 text-zinc-500 hover:text-red-400 transition-colors" title="Archive">
                                                        <Trash2 size={13} />
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {schedules.length === 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardContent className="py-8 text-center text-zinc-500 text-sm">No active rent schedules.</CardContent>
                </Card>
            )}

            {/* Deleted schedules */}
            {deletedSchedules.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader>
                        <button onClick={() => setShowDeleted(v => !v)} className="flex items-center justify-between w-full text-left">
                            <CardTitle className="text-base text-zinc-400">
                                Archived Schedules
                                <Badge className="ml-2 bg-zinc-800 text-zinc-400 border-zinc-700 text-xs">{deletedSchedules.length}</Badge>
                            </CardTitle>
                            <ChevronRight size={16} className={`text-zinc-600 transition-transform ${showDeleted ? 'rotate-90' : ''}`} />
                        </button>
                    </CardHeader>
                    {showDeleted && (
                        <CardContent>
                            <div className="space-y-1">
                                {deletedSchedules.map(s => (
                                    <div key={s.id} className="flex items-center justify-between py-3 border-b border-zinc-800 last:border-0 text-sm">
                                        <div className="min-w-0">
                                            <span className="text-zinc-400">{tenantLabel(s.tenantId)}</span>
                                            <span className="text-zinc-600 ml-2">· {unitLabel(s.unitId)}</span>
                                            <span className="text-zinc-700 ml-2 text-xs">· archived {s.deletedAt ? new Date(s.deletedAt).toLocaleDateString() : ''}</span>
                                        </div>
                                        <div className="flex items-center gap-3 shrink-0 ml-4">
                                            <span className="text-xs text-zinc-500">${s.monthlyAmount.toLocaleString()}/month</span>
                                            <button onClick={() => handleRestore(s)} className="flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 transition-colors">
                                                <RotateCcw size={12} /> Restore
                                            </button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    )}
                </Card>
            )}

            {/* Create form */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader><CardTitle className="text-base">Create Rent Schedule</CardTitle></CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2 md:col-span-2">
                                <Label htmlFor="unitId" className="text-zinc-300">Unit</Label>
                                <select id="unitId" value={unitId} onChange={e => { setUnitId(e.target.value); setTenantId(''); }} required className={selectClass}>
                                    <option value="">Select a unit…</option>
                                    {units.map(u => (
                                        <option key={u.id} value={u.id}>Unit {u.unitNumber} — {u.billingMode === 'SharedUnit' ? 'Shared billing' : 'Per-tenant billing'}</option>
                                    ))}
                                </select>
                            </div>

                            {selectedUnit && !isSharedUnit && (
                                <div className="space-y-2 md:col-span-2">
                                    <Label htmlFor="tenantId" className="text-zinc-300">Tenant</Label>
                                    <select id="tenantId" value={tenantId} onChange={e => setTenantId(e.target.value)} required className={selectClass}>
                                        <option value="">Select a tenant…</option>
                                        {(unitTenants.length > 0 ? unitTenants : tenants.filter(t => t.isActive)).map(t => (
                                            <option key={t.id} value={t.id}>{t.email}</option>
                                        ))}
                                    </select>
                                </div>
                            )}

                            {selectedUnit && isSharedUnit && (
                                <div className="md:col-span-2 rounded-lg bg-zinc-800/50 border border-zinc-700 px-4 py-3 text-sm text-zinc-400">
                                    This unit uses <strong className="text-zinc-300">shared billing</strong> — one schedule applies to all tenants on the unit.
                                </div>
                            )}

                            <div className="space-y-2">
                                <Label className="text-zinc-300">Monthly Rent Amount ($)</Label>
                                <Input type="number" min="0" step="0.01" value={monthlyAmount} onChange={e => setMonthlyAmount(e.target.value)} placeholder="1850.00" required className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Rent Due Day of Month</Label>
                                <Input type="number" min="1" max="28" value={dueDayOfMonth} onChange={e => setDueDayOfMonth(e.target.value)} required className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                                <p className="text-xs text-zinc-500">Max 28 to avoid month-end issues.</p>
                            </div>
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Start Date</Label>
                                <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} required className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label className="text-zinc-300">End Date <span className="text-zinc-500 font-normal">(optional, defaults to 1 year)</span></Label>
                                <Input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                        </div>

                        {createStatus === 'error' && <p className="text-sm text-red-400">{createError}</p>}
                        {createStatus === 'success' && <p className="text-sm text-emerald-400">Rent schedule created.</p>}

                        <Button type="submit" disabled={createStatus === 'loading'} className="bg-indigo-600 hover:bg-indigo-500 text-white">
                            {createStatus === 'loading' ? 'Creating...' : 'Create Rent Schedule'}
                        </Button>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}

function InfoRow({ label, value }: { label: string; value: string }) {
    return (
        <div className="flex justify-between">
            <span className="text-zinc-400">{label}</span>
            <span className="text-zinc-200">{value}</span>
        </div>
    );
}

function ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return s[(v - 20) % 10] ?? s[v] ?? s[0];
}

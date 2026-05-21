'use client';

import { useEffect, useState } from 'react';
import { apiRequest } from '@/lib/api/client';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { PublicUserProfile, RentSchedule, Unit, User } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Pencil, Trash2, X, Check } from 'lucide-react';

export default function RentSchedulePage() {
    const [tenants, setTenants] = useState<User[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [schedules, setSchedules] = useState<RentSchedule[]>([]);
    const [profiles, setProfiles] = useState<Record<string, PublicUserProfile>>({});

    // Create form
    const [unitId, setUnitId] = useState('');
    const [tenantId, setTenantId] = useState('');
    const [monthlyAmount, setMonthlyAmount] = useState('');
    const [dueDayOfMonth, setDueDayOfMonth] = useState('1');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [error, setError] = useState('');

    // Inline edit state
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editAmount, setEditAmount] = useState('');
    const [editDueDay, setEditDueDay] = useState('');
    const [editEndDate, setEditEndDate] = useState('');
    const [editStatus, setEditStatus] = useState<'idle' | 'saving' | 'error'>('idle');

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        const [t, u, s] = await Promise.all([
            authApi.getUsers('Tenant').catch(() => [] as User[]),
            transactionsApi.getUnits().catch(() => [] as Unit[]),
            transactionsApi.getAllRentSchedules().catch(() => [] as RentSchedule[]),
        ]);
        setTenants(t);
        setUnits(u);
        setSchedules(s);

        // Fetch public profiles for all tenants referenced in schedules
        const tenantIds = [...new Set(s.filter(x => x.tenantId).map(x => x.tenantId!))];
        const profileEntries = await Promise.all(
            tenantIds.map(id => authApi.getPublicProfile(id).then(p => [id, p] as const).catch(() => null))
        );
        const map: Record<string, PublicUserProfile> = {};
        for (const entry of profileEntries) {
            if (entry) map[entry[0]] = entry[1];
        }
        setProfiles(map);
    }

    const selectedUnit = units.find(u => u.id === unitId);
    const isSharedUnit = selectedUnit?.billingMode === 'SharedUnit';
    const unitTenantIds = selectedUnit?.currentTenantIds ?? [];
    const unitTenants = tenants.filter(t => unitTenantIds.includes(t.id));

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setStatus('loading');
        setError('');
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
            setStatus('success');
            setUnitId(''); setTenantId(''); setMonthlyAmount(''); setDueDayOfMonth('1'); setStartDate(''); setEndDate('');
            await loadAll();
        } catch {
            setStatus('error');
            setError('Failed to create rent schedule. Check the details and try again.');
        }
    }

    function startEdit(s: RentSchedule) {
        setEditingId(s.id);
        setEditAmount(s.monthlyAmount.toFixed(2));
        setEditDueDay(s.dueDayOfMonth.toString());
        setEditEndDate(s.endDate ? s.endDate.slice(0, 10) : '');
        setEditStatus('idle');
    }

    function cancelEdit() {
        setEditingId(null);
        setEditStatus('idle');
    }

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
        } catch {
            setEditStatus('error');
        }
    }

    async function handleDelete(s: RentSchedule) {
        const label = s.tenantId ? tenantLabel(s.tenantId) : unitLabel(s.unitId);
        if (!confirm(`Delete the rent schedule for ${label}? This cannot be undone.`)) return;
        try {
            await transactionsApi.deleteRentSchedule(s.id);
            setSchedules(prev => prev.filter(x => x.id !== s.id));
        } catch {
            alert('Failed to delete rent schedule.');
        }
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
                                        /* Inline edit row */
                                        <div className="space-y-3">
                                            <div className="flex items-center gap-2 text-sm">
                                                <span className="text-zinc-400 font-medium">{tenantLabel(s.tenantId)}</span>
                                                <span className="text-zinc-600">·</span>
                                                <span className="text-zinc-500">{unitLabel(s.unitId)}</span>
                                            </div>
                                            <div className="grid grid-cols-3 gap-3">
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">Monthly Amount ($)</Label>
                                                    <Input
                                                        type="number"
                                                        min="0"
                                                        step="0.01"
                                                        value={editAmount}
                                                        onChange={e => setEditAmount(e.target.value)}
                                                        className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm"
                                                    />
                                                </div>
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">Due Day of Month</Label>
                                                    <Input
                                                        type="number"
                                                        min="1"
                                                        max="28"
                                                        value={editDueDay}
                                                        onChange={e => setEditDueDay(e.target.value)}
                                                        className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm"
                                                    />
                                                </div>
                                                <div className="space-y-1">
                                                    <Label className="text-xs text-zinc-400">End Date (optional)</Label>
                                                    <Input
                                                        type="date"
                                                        value={editEndDate}
                                                        onChange={e => setEditEndDate(e.target.value)}
                                                        className="h-8 bg-zinc-800 border-zinc-700 text-zinc-100 text-sm"
                                                    />
                                                </div>
                                            </div>
                                            {editStatus === 'error' && (
                                                <p className="text-xs text-red-400">Failed to save. Try again.</p>
                                            )}
                                            <div className="flex gap-2">
                                                <Button
                                                    size="sm"
                                                    className="bg-indigo-600 hover:bg-indigo-500 text-white h-7 px-3 text-xs"
                                                    onClick={() => saveEdit(s.id)}
                                                    disabled={editStatus === 'saving'}
                                                >
                                                    <Check size={12} className="mr-1" />
                                                    {editStatus === 'saving' ? 'Saving…' : 'Save'}
                                                </Button>
                                                <Button
                                                    size="sm"
                                                    variant="ghost"
                                                    className="text-zinc-400 h-7 px-3 text-xs"
                                                    onClick={cancelEdit}
                                                >
                                                    <X size={12} className="mr-1" />
                                                    Cancel
                                                </Button>
                                            </div>
                                        </div>
                                    ) : (
                                        /* Read-only row */
                                        <div className="flex items-center justify-between text-sm">
                                            <div>
                                                <span className="text-zinc-200 font-medium">{tenantLabel(s.tenantId)}</span>
                                                <span className="text-zinc-500 ml-2">· {unitLabel(s.unitId)}</span>
                                            </div>
                                            <div className="flex items-center gap-4">
                                                <div className="text-right text-xs text-zinc-400 space-y-0.5">
                                                    <div>${s.monthlyAmount.toLocaleString()}/month</div>
                                                    <div>Due {s.dueDayOfMonth}{ordinal(s.dueDayOfMonth)} · {new Date(s.startDate).toLocaleDateString()} – {s.endDate ? new Date(s.endDate).toLocaleDateString() : '—'}</div>
                                                </div>
                                                <div className="flex gap-1.5 shrink-0">
                                                    <button
                                                        onClick={() => startEdit(s)}
                                                        className="p-1.5 rounded hover:bg-zinc-800 text-zinc-500 hover:text-zinc-300 transition-colors"
                                                        title="Edit"
                                                    >
                                                        <Pencil size={13} />
                                                    </button>
                                                    <button
                                                        onClick={() => handleDelete(s)}
                                                        className="p-1.5 rounded hover:bg-zinc-800 text-zinc-500 hover:text-red-400 transition-colors"
                                                        title="Delete"
                                                    >
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

            {/* Create form */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader><CardTitle className="text-base">Create Rent Schedule</CardTitle></CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {/* Unit selector */}
                            <div className="space-y-2 md:col-span-2">
                                <Label htmlFor="unitId" className="text-zinc-300">Unit</Label>
                                <select id="unitId" value={unitId} onChange={e => { setUnitId(e.target.value); setTenantId(''); }} required className={selectClass}>
                                    <option value="">Select a unit…</option>
                                    {units.map(u => (
                                        <option key={u.id} value={u.id}>
                                            Unit {u.unitNumber} — {u.billingMode === 'SharedUnit' ? 'Shared billing' : 'Per-tenant billing'}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            {/* Tenant selector — only for PerTenant units */}
                            {selectedUnit && !isSharedUnit && (
                                <div className="space-y-2 md:col-span-2">
                                    <Label htmlFor="tenantId" className="text-zinc-300">Tenant</Label>
                                    <select id="tenantId" value={tenantId} onChange={e => setTenantId(e.target.value)} required className={selectClass}>
                                        <option value="">Select a tenant…</option>
                                        {(unitTenants.length > 0 ? unitTenants : tenants.filter(t => t.isActive)).map(t => (
                                            <option key={t.id} value={t.id}>{t.email}</option>
                                        ))}
                                    </select>
                                    {unitTenants.length > 0 && (
                                        <p className="text-xs text-zinc-500">Showing tenants currently assigned to this unit.</p>
                                    )}
                                </div>
                            )}

                            {selectedUnit && isSharedUnit && (
                                <div className="md:col-span-2 rounded-lg bg-zinc-800/50 border border-zinc-700 px-4 py-3 text-sm text-zinc-400">
                                    This unit uses <strong className="text-zinc-300">shared billing</strong> — one schedule applies to all tenants on the unit.
                                </div>
                            )}

                            <div className="space-y-2">
                                <Label htmlFor="amount" className="text-zinc-300">Monthly Rent Amount ($)</Label>
                                <Input id="amount" type="number" min="0" step="0.01" value={monthlyAmount} onChange={e => setMonthlyAmount(e.target.value)} placeholder="1850.00" required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="dueDay" className="text-zinc-300">Rent Due Day of Month</Label>
                                <Input id="dueDay" type="number" min="1" max="28" value={dueDayOfMonth} onChange={e => setDueDayOfMonth(e.target.value)} required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                                <p className="text-xs text-zinc-500">Max 28 to avoid month-end issues.</p>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="startDate" className="text-zinc-300">Schedule Start Date</Label>
                                <Input id="startDate" type="date" value={startDate} onChange={e => setStartDate(e.target.value)} required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="endDate" className="text-zinc-300">
                                    End Date <span className="text-zinc-500 font-normal">(optional, defaults to 1 year)</span>
                                </Label>
                                <Input id="endDate" type="date" value={endDate} onChange={e => setEndDate(e.target.value)}
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                        </div>

                        {status === 'error' && <p className="text-sm text-red-400">{error}</p>}
                        {status === 'success' && <p className="text-sm text-emerald-400">Rent schedule created.</p>}

                        <Button type="submit" disabled={status === 'loading'} className="bg-indigo-600 hover:bg-indigo-500 text-white">
                            {status === 'loading' ? 'Creating...' : 'Create Rent Schedule'}
                        </Button>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}

function ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return s[(v - 20) % 10] ?? s[v] ?? s[0];
}

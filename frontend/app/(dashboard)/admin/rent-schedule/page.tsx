'use client';

import { useEffect, useState } from 'react';
import { apiRequest } from '@/lib/api/client';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { RentSchedule, Unit, User } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function RentSchedulePage() {
    const [tenants, setTenants] = useState<User[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [schedules, setSchedules] = useState<RentSchedule[]>([]);

    const [unitId, setUnitId] = useState('');
    const [tenantId, setTenantId] = useState('');
    const [monthlyAmount, setMonthlyAmount] = useState('');
    const [dueDayOfMonth, setDueDayOfMonth] = useState('1');
    const [startDate, setStartDate] = useState('');
    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [error, setError] = useState('');

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
    }

    const selectedUnit = units.find(u => u.id === unitId);
    const isSharedUnit = selectedUnit?.billingMode === 'SharedUnit';

    // Tenants currently assigned to the selected unit (for PerTenant filtering)
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
                },
            });
            setStatus('success');
            setUnitId(''); setTenantId(''); setMonthlyAmount(''); setDueDayOfMonth('1'); setStartDate('');
            await loadAll();
        } catch {
            setStatus('error');
            setError('Failed to create rent schedule. Check the details and try again.');
        }
    }

    const selectClass =
        'flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 shadow-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:opacity-50';

    function tenantLabel(id?: string) {
        if (!id) return 'All tenants (shared)';
        return tenants.find(t => t.id === id)?.email ?? id;
    }
    function unitLabel(id: string) {
        const u = units.find(u => u.id === id);
        return u ? `Unit ${u.unitNumber}` : id;
    }

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Rent Schedule</h2>

            {/* Existing schedules */}
            {schedules.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Active Schedules</CardTitle></CardHeader>
                    <CardContent>
                        <div className="space-y-1">
                            {schedules.map(s => (
                                <div key={s.id} className="flex items-center justify-between py-2 border-b border-zinc-800 last:border-0 text-sm">
                                    <div>
                                        <span className="text-zinc-200 font-medium">{tenantLabel(s.tenantId)}</span>
                                        <span className="text-zinc-500 ml-2">· {unitLabel(s.unitId)}</span>
                                    </div>
                                    <div className="text-right text-xs text-zinc-400 space-y-0.5">
                                        <div>${s.monthlyAmount.toLocaleString()}/month</div>
                                        <div>Due on the {s.dueDayOfMonth}{ordinal(s.dueDayOfMonth)} · starts {new Date(s.startDate).toLocaleDateString()}</div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Create form */}
            <Card>
                <CardHeader><CardTitle>Create Rent Schedule</CardTitle></CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {/* Unit selector */}
                            <div className="space-y-2 md:col-span-2">
                                <Label htmlFor="unitId">Unit</Label>
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
                                    <Label htmlFor="tenantId">Tenant</Label>
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
                                    No specific tenant needs to be selected.
                                </div>
                            )}

                            <div className="space-y-2">
                                <Label htmlFor="amount">Monthly Rent Amount ($)</Label>
                                <Input id="amount" type="number" min="0" step="0.01" value={monthlyAmount} onChange={e => setMonthlyAmount(e.target.value)} placeholder="1850.00" required />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="dueDay">Rent Due Day of Month</Label>
                                <Input id="dueDay" type="number" min="1" max="28" value={dueDayOfMonth} onChange={e => setDueDayOfMonth(e.target.value)} required />
                                <p className="text-xs text-zinc-500">Which day of each month rent is due (e.g. 1 = 1st). Max 28 to avoid month-end issues.</p>
                            </div>
                            <div className="space-y-2 md:col-span-2">
                                <Label htmlFor="startDate">Schedule Start Date</Label>
                                <Input id="startDate" type="date" value={startDate} onChange={e => setStartDate(e.target.value)} required />
                                <p className="text-xs text-zinc-500">The date the first rent cycle begins. Payments will be generated monthly from this date onward.</p>
                            </div>
                        </div>

                        {status === 'error' && <p className="text-sm text-red-500">{error}</p>}
                        {status === 'success' && <p className="text-sm text-green-600">Rent schedule created successfully!</p>}

                        <Button type="submit" disabled={status === 'loading'}>
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

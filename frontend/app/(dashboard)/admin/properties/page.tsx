'use client';

import { useEffect, useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { BillingMode, Property, Unit, User } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';

type View =
    | 'list'
    | 'newProperty'
    | 'editProperty'
    | 'newUnit'
    | 'editUnit'
    | 'assignTenant'
    | 'inviteTenant';

function extractError(err: unknown, fallback = 'An error occurred.'): string {
    if (!(err instanceof Error)) return fallback;
    try { return JSON.parse(err.message)?.error ?? err.message; } catch { return err.message; }
}

export default function PropertiesPage() {
    const [view, setView] = useState<View>('list');
    const [properties, setProperties] = useState<Property[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [tenants, setTenants] = useState<User[]>([]);
    const [selectedProperty, setSelectedProperty] = useState<Property | null>(null);
    const [selectedUnit, setSelectedUnit] = useState<Unit | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [msg, setMsg] = useState('');
    const [msgIsError, setMsgIsError] = useState(false);

    // New / edit property
    const [propName, setPropName] = useState('');
    const [propAddress, setPropAddress] = useState('');

    // New / edit unit
    const [unitNumber, setUnitNumber] = useState('');
    const [unitBeds, setUnitBeds] = useState('');
    const [unitBaths, setUnitBaths] = useState('');
    const [unitSqft, setUnitSqft] = useState('');
    const [unitBillingMode, setUnitBillingMode] = useState<BillingMode>('PerTenant');

    // Assign tenant
    const [assignTenantId, setAssignTenantId] = useState('');
    const [assignStartDate, setAssignStartDate] = useState('');

    // Invite tenant
    const [inviteEmail, setInviteEmail] = useState('');

    const selectClass =
        'flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 shadow-sm focus:outline-none focus:ring-1 focus:ring-indigo-500';

    useEffect(() => { loadAll(); }, []);

    async function loadAll() {
        setIsLoading(true);
        try {
            const [props, allUnits, tList] = await Promise.all([
                transactionsApi.getProperties(),
                transactionsApi.getUnits(),
                authApi.getUsers('Tenant'),
            ]);
            setProperties(props);
            setUnits(allUnits);
            setTenants(tList);
        } catch (e) { console.error(e); }
        finally { setIsLoading(false); }
    }

    function setSuccess(m: string) { setMsg(m); setMsgIsError(false); }
    function setError(m: string)   { setMsg(m); setMsgIsError(true); }
    function goList()              { setView('list'); setMsg(''); }

    // ── Properties ─────────────────────────────────────────────────────────────────

    function openEditProperty(p: Property) {
        setSelectedProperty(p);
        setPropName(p.name);
        setPropAddress(p.address);
        setView('editProperty');
        setMsg('');
    }

    async function handleCreateProperty(e: React.FormEvent) {
        e.preventDefault(); setMsg('');
        try {
            await transactionsApi.createProperty({ name: propName, address: propAddress });
            setPropName(''); setPropAddress('');
            setSuccess('Property created.');
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to create property.')); }
    }

    async function handleUpdateProperty(e: React.FormEvent) {
        e.preventDefault(); setMsg('');
        if (!selectedProperty) return;
        try {
            await transactionsApi.updateProperty(selectedProperty.id, { name: propName, address: propAddress });
            setSuccess('Property updated.');
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to update property.')); }
    }

    async function handleDeleteProperty(p: Property) {
        if (!confirm(`Delete property "${p.name}"? This cannot be undone.`)) return;
        try {
            await transactionsApi.deleteProperty(p.id);
            setSuccess('Property deleted.');
            await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to delete property.')); }
    }

    // ── Units ──────────────────────────────────────────────────────────────────────

    function openNewUnit(p: Property) {
        setSelectedProperty(p);
        setUnitNumber(''); setUnitBeds(''); setUnitBaths(''); setUnitSqft('');
        setUnitBillingMode('PerTenant');
        setView('newUnit'); setMsg('');
    }

    function openEditUnit(u: Unit) {
        setSelectedUnit(u);
        setUnitNumber(u.unitNumber);
        setUnitBeds(u.bedrooms?.toString() ?? '');
        setUnitBaths(u.bathrooms?.toString() ?? '');
        setUnitSqft(u.squareFeet?.toString() ?? '');
        setUnitBillingMode(u.billingMode);
        setView('editUnit'); setMsg('');
    }

    async function handleCreateUnit(e: React.FormEvent) {
        e.preventDefault();
        if (!selectedProperty) return;
        setMsg('');
        try {
            await transactionsApi.createUnit({
                propertyId: selectedProperty.id,
                unitNumber,
                bedrooms: unitBeds ? parseInt(unitBeds) : undefined,
                bathrooms: unitBaths ? parseFloat(unitBaths) : undefined,
                squareFeet: unitSqft ? parseInt(unitSqft) : undefined,
                billingMode: unitBillingMode,
            });
            setSuccess('Unit created.');
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to create unit.')); }
    }

    async function handleUpdateUnit(e: React.FormEvent) {
        e.preventDefault();
        if (!selectedUnit) return;
        setMsg('');
        try {
            await transactionsApi.updateUnit(selectedUnit.id, {
                unitNumber,
                bedrooms: unitBeds ? parseInt(unitBeds) : undefined,
                bathrooms: unitBaths ? parseFloat(unitBaths) : undefined,
                squareFeet: unitSqft ? parseInt(unitSqft) : undefined,
                billingMode: unitBillingMode,
            });
            setSuccess('Unit updated.');
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to update unit.')); }
    }

    async function handleDeleteUnit(u: Unit) {
        if (!confirm(`Delete Unit ${u.unitNumber}? This cannot be undone.`)) return;
        try {
            await transactionsApi.deleteUnit(u.id);
            setSuccess(`Unit ${u.unitNumber} deleted.`);
            await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to delete unit.')); }
    }

    // ── Tenant assignment ──────────────────────────────────────────────────────────

    async function handleAssignTenant(e: React.FormEvent) {
        e.preventDefault();
        if (!selectedUnit) return;
        setMsg('');
        try {
            await transactionsApi.assignTenant(selectedUnit.id, { tenantId: assignTenantId, startDate: assignStartDate });
            setAssignTenantId(''); setAssignStartDate('');
            setSuccess('Tenant assigned.');
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to assign tenant.')); }
    }

    async function handleRemoveTenant(unitId: string, tenantId: string) {
        if (!confirm('Remove this tenant from the unit?')) return;
        try {
            await transactionsApi.removeTenant(unitId, tenantId);
            setSuccess('Tenant removed.');
            await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to remove tenant.')); }
    }

    // ── Invite ─────────────────────────────────────────────────────────────────────

    async function handleInviteTenant(e: React.FormEvent) {
        e.preventDefault(); setMsg('');
        try {
            await authApi.invite(inviteEmail, 'Tenant');
            setInviteEmail('');
            setSuccess(`Invite sent to ${inviteEmail}.`);
            goList(); await loadAll();
        } catch (err) { setError(extractError(err, 'Failed to send invite.')); }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────

    const activeTenants = tenants.filter(t => t.isActive);

    function tenantEmail(id: string) { return tenants.find(t => t.id === id)?.email ?? id; }

    if (isLoading) return <div className="text-zinc-400 text-sm">Loading...</div>;

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h2 className="text-2xl font-semibold">Properties &amp; Units</h2>
                <div className="flex gap-2">
                    <Button variant="outline" onClick={() => { setView('inviteTenant'); setMsg(''); }}>Invite Tenant</Button>
                    <Button onClick={() => { setPropName(''); setPropAddress(''); setView('newProperty'); setMsg(''); }}>+ New Property</Button>
                </div>
            </div>

            {msg && <p className={`text-sm ${msgIsError ? 'text-red-400' : 'text-indigo-400'}`}>{msg}</p>}

            {/* Invite Tenant */}
            {view === 'inviteTenant' && (
                <Card><CardHeader><CardTitle>Invite Tenant</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleInviteTenant} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="inviteEmail">Tenant Email</Label>
                                <Input id="inviteEmail" type="email" value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} placeholder="tenant@example.com" required />
                            </div>
                            <div className="flex gap-2">
                                <Button type="submit">Send Invite</Button>
                                <Button type="button" variant="outline" onClick={goList}>Cancel</Button>
                            </div>
                        </form>
                    </CardContent>
                </Card>
            )}

            {/* New Property */}
            {view === 'newProperty' && (
                <Card><CardHeader><CardTitle>New Property</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleCreateProperty} className="space-y-4">
                            <div className="space-y-2">
                                <Label>Name</Label>
                                <Input value={propName} onChange={e => setPropName(e.target.value)} placeholder="123 Maple Street" required />
                            </div>
                            <div className="space-y-2">
                                <Label>Address</Label>
                                <Input value={propAddress} onChange={e => setPropAddress(e.target.value)} placeholder="123 Maple St, City, State 00000" required />
                            </div>
                            <div className="flex gap-2">
                                <Button type="submit">Create</Button>
                                <Button type="button" variant="outline" onClick={goList}>Cancel</Button>
                            </div>
                        </form>
                    </CardContent>
                </Card>
            )}

            {/* Edit Property */}
            {view === 'editProperty' && selectedProperty && (
                <Card><CardHeader><CardTitle>Edit Property — {selectedProperty.name}</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleUpdateProperty} className="space-y-4">
                            <div className="space-y-2">
                                <Label>Name</Label>
                                <Input value={propName} onChange={e => setPropName(e.target.value)} required />
                            </div>
                            <div className="space-y-2">
                                <Label>Address</Label>
                                <Input value={propAddress} onChange={e => setPropAddress(e.target.value)} required />
                            </div>
                            <div className="flex gap-2">
                                <Button type="submit">Save</Button>
                                <Button type="button" variant="outline" onClick={goList}>Cancel</Button>
                            </div>
                        </form>
                    </CardContent>
                </Card>
            )}

            {/* New Unit */}
            {view === 'newUnit' && selectedProperty && (
                <UnitForm
                    title={`New Unit — ${selectedProperty.name}`}
                    unitNumber={unitNumber} setUnitNumber={setUnitNumber}
                    unitBeds={unitBeds} setUnitBeds={setUnitBeds}
                    unitBaths={unitBaths} setUnitBaths={setUnitBaths}
                    unitSqft={unitSqft} setUnitSqft={setUnitSqft}
                    billingMode={unitBillingMode} setBillingMode={setUnitBillingMode}
                    onSubmit={handleCreateUnit} onCancel={goList}
                    selectClass={selectClass}
                />
            )}

            {/* Edit Unit */}
            {view === 'editUnit' && selectedUnit && (
                <UnitForm
                    title={`Edit Unit ${selectedUnit.unitNumber}`}
                    unitNumber={unitNumber} setUnitNumber={setUnitNumber}
                    unitBeds={unitBeds} setUnitBeds={setUnitBeds}
                    unitBaths={unitBaths} setUnitBaths={setUnitBaths}
                    unitSqft={unitSqft} setUnitSqft={setUnitSqft}
                    billingMode={unitBillingMode} setBillingMode={setUnitBillingMode}
                    onSubmit={handleUpdateUnit} onCancel={goList}
                    selectClass={selectClass}
                />
            )}

            {/* Assign Tenant */}
            {view === 'assignTenant' && selectedUnit && (
                <Card>
                    <CardHeader><CardTitle>Add Tenant — Unit {selectedUnit.unitNumber}</CardTitle></CardHeader>
                    <CardContent>
                        {activeTenants.length === 0 ? (
                            <div className="space-y-3">
                                <p className="text-sm text-zinc-400">No registered tenants yet.</p>
                                <Button type="button" variant="outline" onClick={goList}>Back</Button>
                            </div>
                        ) : (
                            <form onSubmit={handleAssignTenant} className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label>Tenant</Label>
                                        <select value={assignTenantId} onChange={e => setAssignTenantId(e.target.value)} required className={selectClass}>
                                            <option value="">Select a tenant…</option>
                                            {activeTenants
                                                .filter(t => !selectedUnit.currentTenantIds.includes(t.id))
                                                .map(t => <option key={t.id} value={t.id}>{t.email}</option>)}
                                        </select>
                                    </div>
                                    <div className="space-y-2">
                                        <Label>Move-in Date</Label>
                                        <Input type="date" value={assignStartDate} onChange={e => setAssignStartDate(e.target.value)} required />
                                    </div>
                                </div>
                                <div className="flex gap-2">
                                    <Button type="submit">Assign</Button>
                                    <Button type="button" variant="outline" onClick={goList}>Cancel</Button>
                                </div>
                            </form>
                        )}
                    </CardContent>
                </Card>
            )}

            {/* Tenant list */}
            {view === 'list' && tenants.length > 0 && (
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base text-white">Tenants</CardTitle></CardHeader>
                    <CardContent>
                        <div className="space-y-1">
                            {tenants.map(t => (
                                <div key={t.id} className="flex items-center justify-between py-2 border-b border-zinc-800 last:border-0">
                                    <span className="text-sm text-zinc-200">{t.email}</span>
                                    <Badge className={t.isActive
                                        ? 'bg-emerald-500/10 text-emerald-400 ring-1 ring-emerald-500/20'
                                        : 'bg-yellow-500/10 text-yellow-400 ring-1 ring-yellow-500/20'}>
                                        {t.isActive ? 'active' : 'pending'}
                                    </Badge>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Properties list */}
            {view === 'list' && (
                <div className="space-y-4">
                    {properties.length === 0 ? (
                        <Card><CardContent className="pt-6"><p className="text-sm text-zinc-500">No properties yet.</p></CardContent></Card>
                    ) : (
                        properties.map(p => {
                            const propUnits = units.filter(u => u.propertyId === p.id);
                            return (
                                <Card key={p.id} className="bg-zinc-900 border-zinc-800">
                                    <CardHeader className="pb-2">
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <CardTitle className="text-base text-white">{p.name}</CardTitle>
                                                <p className="text-xs text-zinc-500 mt-0.5">{p.address}</p>
                                            </div>
                                            <div className="flex gap-2">
                                                <Button size="sm" variant="outline" onClick={() => openEditProperty(p)}>Edit</Button>
                                                <Button size="sm" variant="outline" onClick={() => handleDeleteProperty(p)} className="text-red-400 hover:text-red-300 border-red-900/40">Delete</Button>
                                                <Button size="sm" variant="outline" onClick={() => openNewUnit(p)}>+ Unit</Button>
                                            </div>
                                        </div>
                                    </CardHeader>
                                    <CardContent>
                                        {propUnits.length === 0 ? (
                                            <p className="text-xs text-zinc-600">No units yet.</p>
                                        ) : (
                                            <div className="space-y-2">
                                                {propUnits.map(u => (
                                                    <div key={u.id} className="py-2 border-b border-zinc-800 last:border-0">
                                                        <div className="flex items-center justify-between">
                                                            <div>
                                                                <span className="text-sm font-medium text-zinc-200">Unit {u.unitNumber}</span>
                                                                <span className="text-xs text-zinc-500 ml-2">
                                                                    {[
                                                                        u.bedrooms != null ? `${u.bedrooms}bd` : null,
                                                                        u.bathrooms != null ? `${u.bathrooms}ba` : null,
                                                                        u.squareFeet != null ? `${u.squareFeet} sqft` : null,
                                                                    ].filter(Boolean).join(' · ')}
                                                                </span>
                                                                <span className="ml-2 text-[10px] uppercase tracking-wide text-zinc-600">
                                                                    {u.billingMode === 'SharedUnit' ? '· shared' : '· per-tenant'}
                                                                </span>
                                                            </div>
                                                            <div className="flex gap-2">
                                                                <Button size="sm" variant="outline" onClick={() => openEditUnit(u)}>Edit</Button>
                                                                <Button size="sm" variant="outline" onClick={() => handleDeleteUnit(u)} className="text-red-400 hover:text-red-300 border-red-900/40">Delete</Button>
                                                                <Button size="sm" variant="outline" onClick={() => { setSelectedUnit(u); setAssignTenantId(''); setAssignStartDate(''); setView('assignTenant'); setMsg(''); }}>
                                                                    + Tenant
                                                                </Button>
                                                            </div>
                                                        </div>

                                                        {/* Current tenants */}
                                                        {u.currentTenantIds.length > 0 && (
                                                            <div className="mt-1.5 space-y-0.5">
                                                                {u.currentTenantIds.map(tid => (
                                                                    <div key={tid} className="flex items-center justify-between">
                                                                        <span className="text-xs text-emerald-400 ml-1">· {tenantEmail(tid)}</span>
                                                                        <button
                                                                            onClick={() => handleRemoveTenant(u.id, tid)}
                                                                            className="text-[10px] text-red-400/60 hover:text-red-400 transition-colors"
                                                                        >
                                                                            remove
                                                                        </button>
                                                                    </div>
                                                                ))}
                                                            </div>
                                                        )}
                                                        {u.currentTenantIds.length === 0 && (
                                                            <p className="text-xs text-zinc-600 mt-1 ml-1">vacant</p>
                                                        )}
                                                    </div>
                                                ))}
                                            </div>
                                        )}
                                    </CardContent>
                                </Card>
                            );
                        })
                    )}
                </div>
            )}
        </div>
    );
}

function UnitForm({
    title,
    unitNumber, setUnitNumber,
    unitBeds, setUnitBeds,
    unitBaths, setUnitBaths,
    unitSqft, setUnitSqft,
    billingMode, setBillingMode,
    onSubmit, onCancel, selectClass,
}: {
    title: string;
    unitNumber: string; setUnitNumber: (v: string) => void;
    unitBeds: string; setUnitBeds: (v: string) => void;
    unitBaths: string; setUnitBaths: (v: string) => void;
    unitSqft: string; setUnitSqft: (v: string) => void;
    billingMode: BillingMode; setBillingMode: (v: BillingMode) => void;
    onSubmit: (e: React.FormEvent) => void;
    onCancel: () => void;
    selectClass: string;
}) {
    return (
        <Card>
            <CardHeader><CardTitle>{title}</CardTitle></CardHeader>
            <CardContent>
                <form onSubmit={onSubmit} className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label>Unit Number</Label>
                            <Input value={unitNumber} onChange={e => setUnitNumber(e.target.value)} placeholder="101" required />
                        </div>
                        <div className="space-y-2">
                            <Label>Bedrooms</Label>
                            <Input type="number" min="0" value={unitBeds} onChange={e => setUnitBeds(e.target.value)} placeholder="2" />
                        </div>
                        <div className="space-y-2">
                            <Label>Bathrooms</Label>
                            <Input type="number" min="0" step="0.5" value={unitBaths} onChange={e => setUnitBaths(e.target.value)} placeholder="1.5" />
                        </div>
                        <div className="space-y-2">
                            <Label>Sq Ft</Label>
                            <Input type="number" min="0" value={unitSqft} onChange={e => setUnitSqft(e.target.value)} placeholder="900" />
                        </div>
                        <div className="space-y-2 md:col-span-2">
                            <Label>Billing Mode</Label>
                            <select value={billingMode} onChange={e => setBillingMode(e.target.value as BillingMode)} className={selectClass}>
                                <option value="PerTenant">Per Tenant — each co-tenant has their own rent schedule</option>
                                <option value="SharedUnit">Shared Unit — one rent schedule for the whole unit</option>
                            </select>
                            <p className="text-xs text-zinc-500">
                                {billingMode === 'PerTenant'
                                    ? 'Use for roommates who each pay their own share independently.'
                                    : 'Use when one party pays the full rent or all tenants split informally.'}
                            </p>
                        </div>
                    </div>
                    <div className="flex gap-2">
                        <Button type="submit">Save</Button>
                        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
                    </div>
                </form>
            </CardContent>
        </Card>
    );
}

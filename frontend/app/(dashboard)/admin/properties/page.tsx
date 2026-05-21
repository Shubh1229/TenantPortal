'use client';

import { useEffect, useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { BillingMode, Property, PublicUserProfile, RentSchedule, Unit, User } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, ChevronRight, User2 } from 'lucide-react';

type View =
    | 'list'
    | 'newProperty'
    | 'editProperty'
    | 'newUnit'
    | 'editUnit'
    | 'assignTenant'
    | 'inviteTenant'
    | 'unitDetail'
    | 'tenantDetail';

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
    const [selectedTenantId, setSelectedTenantId] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [msg, setMsg] = useState('');
    const [msgIsError, setMsgIsError] = useState(false);

    // Profiles and rent schedules loaded on demand for detail views
    const [unitProfiles, setUnitProfiles] = useState<Record<string, PublicUserProfile>>({});
    const [unitRentSchedule, setUnitRentSchedule] = useState<RentSchedule | null | undefined>(undefined);
    const [tenantProfile, setTenantProfile] = useState<PublicUserProfile | null>(null);
    const [tenantUnits, setTenantUnits] = useState<Unit[]>([]);

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

    // ── Unit detail ────────────────────────────────────────────────────────────────

    async function openUnitDetail(u: Unit) {
        setSelectedUnit(u);
        setUnitRentSchedule(undefined);
        setUnitProfiles({});
        setView('unitDetail');
        setMsg('');

        // Load public profiles for all tenants on the unit
        const profileMap: Record<string, PublicUserProfile> = {};
        await Promise.all(
            u.currentTenantIds.map(id =>
                authApi.getPublicProfile(id)
                    .then(p => { profileMap[id] = p; })
                    .catch(() => {})
            )
        );
        setUnitProfiles(profileMap);

        // Load rent schedule(s) for this unit
        try {
            const schedule = await transactionsApi.getUnitRentSchedule(u.id);
            setUnitRentSchedule(schedule);
        } catch {
            setUnitRentSchedule(null);
        }
    }

    // ── Tenant detail ──────────────────────────────────────────────────────────────

    async function openTenantDetail(tenantId: string) {
        setSelectedTenantId(tenantId);
        setTenantProfile(null);
        setTenantUnits([]);
        setView('tenantDetail');

        const [profile] = await Promise.all([
            authApi.getPublicProfile(tenantId).catch(() => null),
        ]);
        setTenantProfile(profile);

        // Find all units this tenant is assigned to
        const assigned = units.filter(u => u.currentTenantIds.includes(tenantId));
        setTenantUnits(assigned);
    }

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
            // Optimistic update for the unit detail view
            if (selectedUnit?.id === unitId) {
                setSelectedUnit(prev => prev ? {
                    ...prev,
                    currentTenantIds: prev.currentTenantIds.filter(id => id !== tenantId),
                } : prev);
            }
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

    function profileName(p: PublicUserProfile | null | undefined) {
        if (!p) return null;
        return [p.firstName, p.lastName].filter(Boolean).join(' ') || null;
    }

    if (isLoading) return <div className="text-zinc-400 text-sm">Loading...</div>;

    // ── Unit detail view ───────────────────────────────────────────────────────────

    if (view === 'unitDetail' && selectedUnit) {
        const property = properties.find(p => p.id === selectedUnit.propertyId);
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-3">
                    <button onClick={goList} className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-200 transition-colors">
                        <ArrowLeft size={18} />
                    </button>
                    <div>
                        <h2 className="text-2xl font-semibold">Unit {selectedUnit.unitNumber}</h2>
                        {property && <p className="text-sm text-zinc-500">{property.name} · {property.address}</p>}
                    </div>
                </div>

                {msg && <p className={`text-sm ${msgIsError ? 'text-red-400' : 'text-indigo-400'}`}>{msg}</p>}

                {/* Unit info */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader className="pb-2">
                        <div className="flex items-center justify-between">
                            <CardTitle className="text-base">Unit Details</CardTitle>
                            <div className="flex gap-2">
                                <Button size="sm" variant="outline" onClick={() => openEditUnit(selectedUnit)}>Edit</Button>
                                <Button size="sm" variant="outline" onClick={() => handleDeleteUnit(selectedUnit)} className="text-red-400 hover:text-red-300 border-red-900/40">Delete</Button>
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                            {selectedUnit.bedrooms != null && (
                                <div><p className="text-zinc-500 text-xs">Bedrooms</p><p className="text-zinc-200">{selectedUnit.bedrooms}</p></div>
                            )}
                            {selectedUnit.bathrooms != null && (
                                <div><p className="text-zinc-500 text-xs">Bathrooms</p><p className="text-zinc-200">{selectedUnit.bathrooms}</p></div>
                            )}
                            {selectedUnit.squareFeet != null && (
                                <div><p className="text-zinc-500 text-xs">Sq Ft</p><p className="text-zinc-200">{selectedUnit.squareFeet.toLocaleString()}</p></div>
                            )}
                            <div>
                                <p className="text-zinc-500 text-xs">Billing</p>
                                <p className="text-zinc-200">{selectedUnit.billingMode === 'SharedUnit' ? 'Shared' : 'Per Tenant'}</p>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/* Tenants */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader className="pb-2">
                        <div className="flex items-center justify-between">
                            <CardTitle className="text-base">Tenants</CardTitle>
                            <Button size="sm" variant="outline" onClick={() => { setAssignTenantId(''); setAssignStartDate(''); setView('assignTenant'); }}>+ Add Tenant</Button>
                        </div>
                    </CardHeader>
                    <CardContent>
                        {selectedUnit.currentTenantIds.length === 0 ? (
                            <p className="text-sm text-zinc-500">No tenants assigned.</p>
                        ) : (
                            <div className="space-y-3">
                                {selectedUnit.currentTenantIds.map(tid => {
                                    const p = unitProfiles[tid];
                                    const name = profileName(p);
                                    return (
                                        <div key={tid} className="flex items-start justify-between py-2 border-b border-zinc-800 last:border-0">
                                            <button
                                                onClick={() => openTenantDetail(tid)}
                                                className="flex items-start gap-3 text-left hover:opacity-80 transition-opacity"
                                            >
                                                <div className="w-8 h-8 rounded-full bg-zinc-800 flex items-center justify-center shrink-0 mt-0.5">
                                                    <User2 size={14} className="text-zinc-400" />
                                                </div>
                                                <div>
                                                    {name && <p className="text-sm font-medium text-zinc-200">{name}</p>}
                                                    <p className="text-sm text-zinc-400">{p?.email ?? tenantEmail(tid)}</p>
                                                    {p?.phoneNumber && <p className="text-xs text-zinc-500 mt-0.5">{p.phoneNumber}</p>}
                                                    {p?.emergencyContactName && (
                                                        <p className="text-xs text-zinc-600 mt-0.5">
                                                            Emergency: {p.emergencyContactName}{p.emergencyContactPhone ? ` · ${p.emergencyContactPhone}` : ''}
                                                        </p>
                                                    )}
                                                </div>
                                            </button>
                                            <div className="flex items-center gap-2 mt-1">
                                                <button
                                                    onClick={() => openTenantDetail(tid)}
                                                    className="text-xs text-indigo-400 hover:text-indigo-300 flex items-center gap-0.5"
                                                >
                                                    Details <ChevronRight size={12} />
                                                </button>
                                                <button
                                                    onClick={() => handleRemoveTenant(selectedUnit.id, tid)}
                                                    className="text-xs text-red-400/60 hover:text-red-400 transition-colors"
                                                >
                                                    remove
                                                </button>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Rent schedule */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Rent Schedule</CardTitle></CardHeader>
                    <CardContent>
                        {unitRentSchedule === undefined ? (
                            <p className="text-sm text-zinc-500">Loading…</p>
                        ) : unitRentSchedule === null ? (
                            <p className="text-sm text-zinc-500">
                                No shared schedule found for this unit.{' '}
                                {selectedUnit.billingMode === 'PerTenant' && 'Per-tenant schedules are managed on the Rent Schedule page.'}
                            </p>
                        ) : (
                            <div className="space-y-1 text-sm">
                                <div className="flex justify-between">
                                    <span className="text-zinc-400">Monthly amount</span>
                                    <span className="text-zinc-200 font-medium">${unitRentSchedule.monthlyAmount.toLocaleString()}</span>
                                </div>
                                <div className="flex justify-between">
                                    <span className="text-zinc-400">Due day</span>
                                    <span className="text-zinc-200">
                                        {unitRentSchedule.dueDayOfMonth}{ordinal(unitRentSchedule.dueDayOfMonth)} of each month
                                    </span>
                                </div>
                                <div className="flex justify-between">
                                    <span className="text-zinc-400">Start date</span>
                                    <span className="text-zinc-200">{new Date(unitRentSchedule.startDate).toLocaleDateString()}</span>
                                </div>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        );
    }

    // ── Tenant detail view ─────────────────────────────────────────────────────────

    if (view === 'tenantDetail' && selectedTenantId) {
        const p = tenantProfile;
        const name = profileName(p);
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-3">
                    <button onClick={() => setView('list')} className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-200 transition-colors">
                        <ArrowLeft size={18} />
                    </button>
                    <div>
                        <h2 className="text-2xl font-semibold">{name ?? p?.email ?? 'Tenant'}</h2>
                        {name && p?.email && <p className="text-sm text-zinc-500">{p.email}</p>}
                    </div>
                </div>

                {/* Profile info */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Profile</CardTitle></CardHeader>
                    <CardContent>
                        {!p ? (
                            <p className="text-sm text-zinc-500">Loading…</p>
                        ) : (
                            <div className="space-y-2 text-sm">
                                <InfoRow label="Email" value={p.email} />
                                {name && <InfoRow label="Name" value={name} />}
                                {p.phoneNumber && <InfoRow label="Phone" value={p.phoneNumber} />}
                                {p.emergencyContactName && (
                                    <InfoRow
                                        label="Emergency contact"
                                        value={[p.emergencyContactName, p.emergencyContactPhone].filter(Boolean).join(' · ')}
                                    />
                                )}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Units */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle className="text-base">Assigned Units</CardTitle></CardHeader>
                    <CardContent>
                        {tenantUnits.length === 0 ? (
                            <p className="text-sm text-zinc-500">Not currently assigned to any unit.</p>
                        ) : (
                            <div className="space-y-2">
                                {tenantUnits.map(u => {
                                    const prop = properties.find(pr => pr.id === u.propertyId);
                                    return (
                                        <button
                                            key={u.id}
                                            onClick={() => openUnitDetail(u)}
                                            className="w-full flex items-center justify-between py-2 border-b border-zinc-800 last:border-0 hover:opacity-80 transition-opacity text-left"
                                        >
                                            <div>
                                                <span className="text-sm font-medium text-zinc-200">Unit {u.unitNumber}</span>
                                                {prop && <span className="text-xs text-zinc-500 ml-2">{prop.name}</span>}
                                            </div>
                                            <ChevronRight size={14} className="text-zinc-500" />
                                        </button>
                                    );
                                })}
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        );
    }

    // ── Standard views ─────────────────────────────────────────────────────────────

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
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle>Invite Tenant</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleInviteTenant} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="inviteEmail" className="text-zinc-300">Tenant Email</Label>
                                <Input id="inviteEmail" type="email" value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} placeholder="tenant@example.com" required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
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
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle>New Property</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleCreateProperty} className="space-y-4">
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Name</Label>
                                <Input value={propName} onChange={e => setPropName(e.target.value)} placeholder="123 Maple Street" required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Address</Label>
                                <Input value={propAddress} onChange={e => setPropAddress(e.target.value)} placeholder="123 Maple St, City, State 00000" required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
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
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader><CardTitle>Edit Property — {selectedProperty.name}</CardTitle></CardHeader>
                    <CardContent>
                        <form onSubmit={handleUpdateProperty} className="space-y-4">
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Name</Label>
                                <Input value={propName} onChange={e => setPropName(e.target.value)} required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="space-y-2">
                                <Label className="text-zinc-300">Address</Label>
                                <Input value={propAddress} onChange={e => setPropAddress(e.target.value)} required
                                    className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                            </div>
                            <div className="flex gap-2">
                                <Button type="submit">Save</Button>
                                <Button type="button" variant="outline" onClick={goList}>Cancel</Button>
                            </div>
                        </form>
                    </CardContent>
                </Card>
            )}

            {/* New / Edit Unit */}
            {(view === 'newUnit' || view === 'editUnit') && (
                <UnitForm
                    title={view === 'newUnit'
                        ? `New Unit — ${selectedProperty?.name}`
                        : `Edit Unit ${selectedUnit?.unitNumber}`}
                    unitNumber={unitNumber} setUnitNumber={setUnitNumber}
                    unitBeds={unitBeds} setUnitBeds={setUnitBeds}
                    unitBaths={unitBaths} setUnitBaths={setUnitBaths}
                    unitSqft={unitSqft} setUnitSqft={setUnitSqft}
                    billingMode={unitBillingMode} setBillingMode={setUnitBillingMode}
                    onSubmit={view === 'newUnit' ? handleCreateUnit : handleUpdateUnit}
                    onCancel={goList}
                    selectClass={selectClass}
                />
            )}

            {/* Assign Tenant */}
            {view === 'assignTenant' && selectedUnit && (
                <Card className="bg-zinc-900 border-zinc-800">
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
                                        <Label className="text-zinc-300">Tenant</Label>
                                        <select value={assignTenantId} onChange={e => setAssignTenantId(e.target.value)} required className={selectClass}>
                                            <option value="">Select a tenant…</option>
                                            {activeTenants
                                                .filter(t => !selectedUnit.currentTenantIds.includes(t.id))
                                                .map(t => <option key={t.id} value={t.id}>{t.email}</option>)}
                                        </select>
                                    </div>
                                    <div className="space-y-2">
                                        <Label className="text-zinc-300">Move-in Date</Label>
                                        <Input type="date" value={assignStartDate} onChange={e => setAssignStartDate(e.target.value)} required
                                            className="bg-zinc-900 border-zinc-700 text-zinc-100" />
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
                        <Card className="bg-zinc-900 border-zinc-800">
                            <CardContent className="pt-6"><p className="text-sm text-zinc-500">No properties yet.</p></CardContent>
                        </Card>
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
                                                            <button
                                                                onClick={() => openUnitDetail(u)}
                                                                className="flex items-center gap-1 hover:opacity-80 transition-opacity text-left"
                                                            >
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
                                                                <ChevronRight size={13} className="text-zinc-600 ml-1" />
                                                            </button>
                                                            <div className="flex gap-2">
                                                                <Button size="sm" variant="outline" onClick={() => openEditUnit(u)}>Edit</Button>
                                                                <Button size="sm" variant="outline" onClick={() => handleDeleteUnit(u)} className="text-red-400 hover:text-red-300 border-red-900/40">Delete</Button>
                                                                <Button size="sm" variant="outline" onClick={() => { setSelectedUnit(u); setAssignTenantId(''); setAssignStartDate(''); setView('assignTenant'); setMsg(''); }}>
                                                                    + Tenant
                                                                </Button>
                                                            </div>
                                                        </div>

                                                        {/* Current tenants — clickable */}
                                                        {u.currentTenantIds.length > 0 && (
                                                            <div className="mt-1.5 space-y-0.5">
                                                                {u.currentTenantIds.map(tid => (
                                                                    <div key={tid} className="flex items-center justify-between">
                                                                        <button
                                                                            onClick={() => openTenantDetail(tid)}
                                                                            className="text-xs text-emerald-400 hover:text-emerald-300 ml-1 flex items-center gap-0.5"
                                                                        >
                                                                            · {tenantEmail(tid)}
                                                                        </button>
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
        <Card className="bg-zinc-900 border-zinc-800">
            <CardHeader><CardTitle>{title}</CardTitle></CardHeader>
            <CardContent>
                <form onSubmit={onSubmit} className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label className="text-zinc-300">Unit Number</Label>
                            <Input value={unitNumber} onChange={e => setUnitNumber(e.target.value)} placeholder="101" required
                                className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                        </div>
                        <div className="space-y-2">
                            <Label className="text-zinc-300">Bedrooms</Label>
                            <Input type="number" min="0" value={unitBeds} onChange={e => setUnitBeds(e.target.value)} placeholder="2"
                                className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                        </div>
                        <div className="space-y-2">
                            <Label className="text-zinc-300">Bathrooms</Label>
                            <Input type="number" min="0" step="0.5" value={unitBaths} onChange={e => setUnitBaths(e.target.value)} placeholder="1.5"
                                className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                        </div>
                        <div className="space-y-2">
                            <Label className="text-zinc-300">Sq Ft</Label>
                            <Input type="number" min="0" value={unitSqft} onChange={e => setUnitSqft(e.target.value)} placeholder="900"
                                className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                        </div>
                        <div className="space-y-2 md:col-span-2">
                            <Label className="text-zinc-300">Billing Mode</Label>
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

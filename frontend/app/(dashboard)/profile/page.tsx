'use client';

import { useEffect, useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { UserProfile } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

function extractError(err: unknown, fallback: string): string {
    if (!(err instanceof Error)) return fallback;
    try {
        const parsed = JSON.parse(err.message);
        return parsed?.error ?? parsed?.title ?? fallback;
    } catch {
        return err.message || fallback;
    }
}

export default function ProfilePage() {
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [editing, setEditing] = useState(false);

    // Edit form state
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [ecName, setEcName] = useState('');
    const [ecPhone, setEcPhone] = useState('');
    const [saveError, setSaveError] = useState('');
    const [saveStatus, setSaveStatus] = useState('');
    const [submitting, setSubmitting] = useState(false);

    // Notification email form
    const [newEmail, setNewEmail] = useState('');
    const [emailError, setEmailError] = useState('');
    const [addingEmail, setAddingEmail] = useState(false);

    useEffect(() => {
        load();
    }, []);

    async function load() {
        try {
            const p = await authApi.getProfile();
            setProfile(p);
            setFirstName(p.firstName ?? '');
            setLastName(p.lastName ?? '');
            setPhoneNumber(p.phoneNumber ?? '');
            setEcName(p.emergencyContactName ?? '');
            setEcPhone(p.emergencyContactPhone ?? '');
        } catch { }
    }

    async function handleSaveProfile(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true); setSaveError(''); setSaveStatus('');
        try {
            await authApi.updateProfile({ firstName, lastName, phoneNumber, emergencyContactName: ecName, emergencyContactPhone: ecPhone });
            setSaveStatus('Profile saved.');
            setEditing(false);
            await load();
        } catch (err) {
            setSaveError(extractError(err, 'Failed to save profile.'));
        } finally {
            setSubmitting(false);
        }
    }

    async function handleAddEmail(e: React.FormEvent) {
        e.preventDefault();
        setAddingEmail(true); setEmailError('');
        try {
            await authApi.addNotificationEmail(newEmail);
            setNewEmail('');
            await load();
        } catch (err) {
            setEmailError(extractError(err, 'Failed to add email.'));
        } finally {
            setAddingEmail(false);
        }
    }

    async function handleDeleteEmail(id: string) {
        try {
            await authApi.deleteNotificationEmail(id);
            await load();
        } catch { }
    }

    if (!profile) {
        return <div className="text-zinc-400 text-sm">Loading...</div>;
    }

    return (
        <div className="max-w-xl space-y-6">
            <h1 className="text-2xl font-semibold text-zinc-100">My Profile</h1>

            {/* Personal info card */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-base">Personal Information</CardTitle>
                    {!editing && (
                        <Button size="sm" variant="outline" onClick={() => { setEditing(true); setSaveStatus(''); setSaveError(''); }}>
                            Edit
                        </Button>
                    )}
                </CardHeader>
                <CardContent>
                    {editing ? (
                        <form onSubmit={handleSaveProfile} className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="fn">First Name</Label>
                                    <Input id="fn" value={firstName} onChange={e => setFirstName(e.target.value)} required />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ln">Last Name</Label>
                                    <Input id="ln" value={lastName} onChange={e => setLastName(e.target.value)} required />
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="ph">Phone Number</Label>
                                <Input id="ph" type="tel" value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} required />
                            </div>
                            <div className="pt-2 border-t border-zinc-800">
                                <p className="text-xs text-zinc-500 mb-3">Emergency Contact</p>
                                <div className="grid grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="ecn">Name</Label>
                                        <Input id="ecn" value={ecName} onChange={e => setEcName(e.target.value)} placeholder="Optional" />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="ecp">Phone</Label>
                                        <Input id="ecp" type="tel" value={ecPhone} onChange={e => setEcPhone(e.target.value)} placeholder="Optional" />
                                    </div>
                                </div>
                            </div>
                            {saveError && <p className="text-sm text-red-400">{saveError}</p>}
                            {saveStatus && <p className="text-sm text-green-400">{saveStatus}</p>}
                            <div className="flex gap-2">
                                <Button type="submit" disabled={submitting}>{submitting ? 'Saving...' : 'Save'}</Button>
                                <Button type="button" variant="outline" onClick={() => setEditing(false)}>Cancel</Button>
                            </div>
                        </form>
                    ) : (
                        <dl className="space-y-3 text-sm">
                            <Row label="Name" value={`${profile.firstName ?? ''} ${profile.lastName ?? ''}`.trim() || '—'} />
                            <Row label="Phone" value={profile.phoneNumber || '—'} />
                            <Row label="Email" value={profile.email} />
                            <Row label="Role" value={profile.role} />
                            {profile.emergencyContactName && (
                                <Row label="Emergency Contact" value={`${profile.emergencyContactName}${profile.emergencyContactPhone ? ` · ${profile.emergencyContactPhone}` : ''}`} />
                            )}
                        </dl>
                    )}
                </CardContent>
            </Card>

            {/* Notification emails card */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader>
                    <CardTitle className="text-base">Notification Emails</CardTitle>
                    <p className="text-xs text-zinc-500 mt-0.5">
                        Notifications are always sent to your primary email. Add extra addresses here.
                    </p>
                </CardHeader>
                <CardContent className="space-y-4">
                    {profile.notificationEmails.length > 0 && (
                        <div className="space-y-1">
                            {profile.notificationEmails.map(e => (
                                <div key={e.id} className="flex items-center justify-between py-1.5 border-b border-zinc-800 last:border-0">
                                    <span className="text-sm text-zinc-300">{e.email}</span>
                                    <button
                                        onClick={() => handleDeleteEmail(e.id)}
                                        className="text-xs text-red-400 hover:text-red-300 transition-colors ml-4"
                                    >
                                        Remove
                                    </button>
                                </div>
                            ))}
                        </div>
                    )}
                    <form onSubmit={handleAddEmail} className="flex gap-2">
                        <Input
                            type="email"
                            value={newEmail}
                            onChange={e => setNewEmail(e.target.value)}
                            placeholder="Add email address"
                            required
                            className="flex-1"
                        />
                        <Button type="submit" disabled={addingEmail} size="sm">
                            {addingEmail ? '...' : 'Add'}
                        </Button>
                    </form>
                    {emailError && <p className="text-xs text-red-400">{emailError}</p>}
                </CardContent>
            </Card>
        </div>
    );
}

function Row({ label, value }: { label: string; value: string }) {
    return (
        <div className="flex justify-between">
            <dt className="text-zinc-500">{label}</dt>
            <dd className="text-zinc-200">{value}</dd>
        </div>
    );
}

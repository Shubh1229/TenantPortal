'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/hooks/useAuth';
import { authApi } from '@/lib/api/auth';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function ProfileSetupPage() {
    const { role } = useAuth();
    const router = useRouter();

    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [emergencyContactName, setEmergencyContactName] = useState('');
    const [emergencyContactPhone, setEmergencyContactPhone] = useState('');
    const [error, setError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setSubmitting(true);
        try {
            await authApi.updateProfile({
                firstName,
                lastName,
                phoneNumber,
                emergencyContactName: emergencyContactName || undefined,
                emergencyContactPhone: emergencyContactPhone || undefined,
            });
            // Redirect to the appropriate dashboard
            if (role === 'Admin') router.push('/admin');
            else if (role === 'SuperAdmin') router.push('/super-admin');
            else router.push('/tenant');
        } catch {
            setError('Failed to save profile. Please try again.');
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <div className="max-w-lg mx-auto mt-8">
            <div className="mb-6">
                <h1 className="text-2xl font-semibold text-zinc-100">Complete Your Profile</h1>
                <p className="text-sm text-zinc-400 mt-1">
                    Just a few details before you get started. This helps your property manager reach you.
                </p>
            </div>

            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader>
                    <CardTitle className="text-base">Personal Information</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="firstName">First Name</Label>
                                <Input
                                    id="firstName"
                                    value={firstName}
                                    onChange={e => setFirstName(e.target.value)}
                                    placeholder="Jordan"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="lastName">Last Name</Label>
                                <Input
                                    id="lastName"
                                    value={lastName}
                                    onChange={e => setLastName(e.target.value)}
                                    placeholder="Rivera"
                                    required
                                />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="phoneNumber">Phone Number</Label>
                            <Input
                                id="phoneNumber"
                                type="tel"
                                value={phoneNumber}
                                onChange={e => setPhoneNumber(e.target.value)}
                                placeholder="412-555-0100"
                                required
                            />
                        </div>

                        <div className="pt-2 border-t border-zinc-800">
                            <p className="text-xs text-zinc-500 mb-3">
                                Emergency contact (optional but recommended for lease purposes)
                            </p>
                            <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="ecName">Contact Name</Label>
                                    <Input
                                        id="ecName"
                                        value={emergencyContactName}
                                        onChange={e => setEmergencyContactName(e.target.value)}
                                        placeholder="Casey Rivera"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ecPhone">Contact Phone</Label>
                                    <Input
                                        id="ecPhone"
                                        type="tel"
                                        value={emergencyContactPhone}
                                        onChange={e => setEmergencyContactPhone(e.target.value)}
                                        placeholder="412-555-0101"
                                    />
                                </div>
                            </div>
                        </div>

                        {error && <p className="text-sm text-red-400">{error}</p>}

                        <Button type="submit" disabled={submitting} className="w-full">
                            {submitting ? 'Saving...' : 'Save & Continue'}
                        </Button>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}

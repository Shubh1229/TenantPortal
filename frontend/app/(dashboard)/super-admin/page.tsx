'use client';

import { useEffect, useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function SuperAdminDashboard() {
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteRole, setInviteRole] = useState<'Admin' | 'Tenant'>('Admin');
    const [inviteStatus, setInviteStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [inviteError, setInviteError] = useState('');

    async function handleInvite(e: React.FormEvent) {
        e.preventDefault();
        setInviteStatus('loading');
        setInviteError('');
        try {
            await authApi.invite(inviteEmail, inviteRole);
            setInviteStatus('success');
            setInviteEmail('');
        } catch {
            setInviteStatus('error');
            setInviteError('Failed to send invite. Please try again.');
        }
    }

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Super Admin Dashboard</h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Invite Admin */}
                <Card>
                    <CardHeader>
                        <CardTitle>Invite User</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <form onSubmit={handleInvite} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="invite-email">Email Address</Label>
                                <Input
                                    id="invite-email"
                                    type="email"
                                    value={inviteEmail}
                                    onChange={e => setInviteEmail(e.target.value)}
                                    placeholder="user@example.com"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="invite-role">Role</Label>
                                <select
                                    id="invite-role"
                                    value={inviteRole}
                                    onChange={e => setInviteRole(e.target.value as 'Admin' | 'Tenant')}
                                    className="w-full border rounded-md px-3 py-2 text-sm bg-white"
                                >
                                    <option value="Admin">Admin</option>
                                    <option value="Tenant">Tenant</option>
                                </select>
                            </div>
                            {inviteStatus === 'error' && (
                                <p className="text-sm text-red-500">{inviteError}</p>
                            )}
                            {inviteStatus === 'success' && (
                                <p className="text-sm text-green-600">Invite sent successfully!</p>
                            )}
                            <Button
                                type="submit"
                                className="w-full"
                                disabled={inviteStatus === 'loading'}
                            >
                                {inviteStatus === 'loading' ? 'Sending...' : 'Send Invite'}
                            </Button>
                        </form>
                    </CardContent>
                </Card>

                {/* System Info */}
                <Card>
                    <CardHeader>
                        <CardTitle>System</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                        <div className="flex justify-between text-sm">
                            <span className="text-slate-500">Role</span>
                            <span className="font-medium">Super Admin</span>
                        </div>
                        <div className="flex justify-between text-sm">
                            <span className="text-slate-500">Access Level</span>
                            <span className="font-medium">Full System</span>
                        </div>
                        <div className="flex justify-between text-sm">
                            <span className="text-slate-500">Notifications</span>
                            <span className="font-medium">Disabled</span>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
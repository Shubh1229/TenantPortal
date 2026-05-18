'use client';

import { useState } from 'react';
import { authApi } from '@/lib/api/auth';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function SuperAdminDashboard() {
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteRole, setInviteRole] = useState<'Admin' | 'Tenant' | 'Tester'>('Admin');
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
        } catch (err) {
            setInviteStatus('error');
            // Parse the error message from the server response body
            let message = 'Failed to send invite. Please try again.';
            if (err instanceof Error && err.message) {
                try {
                    const parsed = JSON.parse(err.message);
                    message = parsed.error ?? parsed.message ?? err.message;
                } catch {
                    message = err.message;
                }
            }
            setInviteError(message);
        }
    }

    return (
        <div className="space-y-7">
            <div>
                <h1 className="text-2xl font-semibold text-white">Super Admin Dashboard</h1>
                <p className="text-sm text-zinc-500 mt-0.5">Full system access</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Invite user */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader>
                        <CardTitle className="text-base text-white">Invite User</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <form onSubmit={handleInvite} className="space-y-4">
                            <div className="space-y-1.5">
                                <Label htmlFor="invite-email" className="text-zinc-300">Email Address</Label>
                                <Input
                                    id="invite-email"
                                    type="email"
                                    value={inviteEmail}
                                    onChange={e => setInviteEmail(e.target.value)}
                                    placeholder="user@example.com"
                                    required
                                    className="bg-zinc-800 border-zinc-700 text-white placeholder:text-zinc-600 focus:border-indigo-500"
                                />
                            </div>
                            <div className="space-y-1.5">
                                <Label htmlFor="invite-role" className="text-zinc-300">Role</Label>
                                <select
                                    id="invite-role"
                                    value={inviteRole}
                                    onChange={e => setInviteRole(e.target.value as 'Admin' | 'Tenant' | 'Tester')}
                                    className="w-full border border-zinc-700 rounded-md px-3 py-2 text-sm bg-zinc-800 text-zinc-200 focus:border-indigo-500 focus:outline-none"
                                >
                                    <option value="Admin">Admin</option>
                                    <option value="Tenant">Tenant</option>
                                    <option value="Tester">Tester</option>
                                </select>
                            </div>
                            {inviteStatus === 'error' && (
                                <p className="text-sm text-red-400">{inviteError}</p>
                            )}
                            {inviteStatus === 'success' && (
                                <p className="text-sm text-emerald-400">Invite sent successfully!</p>
                            )}
                            <Button
                                type="submit"
                                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                                disabled={inviteStatus === 'loading'}
                            >
                                {inviteStatus === 'loading' ? 'Sending...' : 'Send Invite'}
                            </Button>
                        </form>
                    </CardContent>
                </Card>

                {/* System info */}
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardHeader>
                        <CardTitle className="text-base text-white">System</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                        {[
                            ['Role', 'Super Admin'],
                            ['Access Level', 'Full System'],
                            ['Notifications', 'Disabled'],
                        ].map(([label, value]) => (
                            <div key={label} className="flex justify-between text-sm">
                                <span className="text-zinc-500">{label}</span>
                                <span className="font-medium text-zinc-200">{value}</span>
                            </div>
                        ))}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
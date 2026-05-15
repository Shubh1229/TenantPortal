'use client';

import { useState } from 'react';
import { apiRequest } from '@/lib/api/client';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function RentSchedulePage() {
    const [tenantId, setTenantId] = useState('');
    const [unitId, setUnitId] = useState('');
    const [monthlyAmount, setMonthlyAmount] = useState('');
    const [dueDayOfMonth, setDueDayOfMonth] = useState('1');
    const [startDate, setStartDate] = useState('');
    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [error, setError] = useState('');

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setStatus('loading');
        setError('');
        try {
            await apiRequest('/api/rent-schedule', {
                method: 'POST',
                body: {
                    tenantId,
                    unitId,
                    monthlyAmount: parseFloat(monthlyAmount),
                    dueDayOfMonth: parseInt(dueDayOfMonth),
                    startDate,
                },
            });
            setStatus('success');
            setTenantId('');
            setUnitId('');
            setMonthlyAmount('');
            setDueDayOfMonth('1');
            setStartDate('');
        } catch {
            setStatus('error');
            setError('Failed to create rent schedule. Please check the details and try again.');
        }
    }

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Rent Schedule</h2>

            <Card>
                <CardHeader>
                    <CardTitle>Create Rent Schedule</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="tenantId">Tenant ID</Label>
                                <Input
                                    id="tenantId"
                                    value={tenantId}
                                    onChange={e => setTenantId(e.target.value)}
                                    placeholder="Tenant UUID"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="unitId">Unit ID</Label>
                                <Input
                                    id="unitId"
                                    value={unitId}
                                    onChange={e => setUnitId(e.target.value)}
                                    placeholder="Unit UUID"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="amount">Monthly Amount ($)</Label>
                                <Input
                                    id="amount"
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={monthlyAmount}
                                    onChange={e => setMonthlyAmount(e.target.value)}
                                    placeholder="3600.00"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="dueDay">Due Day of Month</Label>
                                <Input
                                    id="dueDay"
                                    type="number"
                                    min="1"
                                    max="31"
                                    value={dueDayOfMonth}
                                    onChange={e => setDueDayOfMonth(e.target.value)}
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="startDate">Start Date</Label>
                                <Input
                                    id="startDate"
                                    type="date"
                                    value={startDate}
                                    onChange={e => setStartDate(e.target.value)}
                                    required
                                />
                            </div>
                        </div>
                        {status === 'error' && <p className="text-sm text-red-500">{error}</p>}
                        {status === 'success' && (
                            <p className="text-sm text-green-600">Rent schedule created successfully!</p>
                        )}
                        <Button type="submit" disabled={status === 'loading'}>
                            {status === 'loading' ? 'Creating...' : 'Create Rent Schedule'}
                        </Button>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
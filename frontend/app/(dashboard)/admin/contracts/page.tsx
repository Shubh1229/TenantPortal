'use client';

import { useEffect, useState, useRef } from 'react';
import { contractsApi } from '@/lib/api/contracts';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { Contract, User, Unit } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function AdminContractsPage() {
    const [contracts, setContracts] = useState<Contract[]>([]);
    const [tenants, setTenants] = useState<User[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isUploading, setIsUploading] = useState(false);
    const [tenantId, setTenantId] = useState('');
    const [unitId, setUnitId] = useState('');
    const [uploadError, setUploadError] = useState('');
    const [uploadSuccess, setUploadSuccess] = useState(false);
    const fileRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        Promise.all([
            load(),
            authApi.getUsers('Tenant').then(setTenants),
            transactionsApi.getUnits().then(setUnits),
        ]).catch(console.error);
    }, []);

    async function load() {
        try {
            const data = await contractsApi.getAll();
            setContracts(data);
        } catch (e) {
            console.error(e);
        } finally {
            setIsLoading(false);
        }
    }

    async function handleUpload(e: React.FormEvent) {
        e.preventDefault();
        if (!fileRef.current?.files?.[0]) return;
        setIsUploading(true);
        setUploadError('');
        setUploadSuccess(false);
        try {
            const formData = new FormData();
            formData.append('TenantId', tenantId);
            formData.append('UnitId', unitId);
            formData.append('File', fileRef.current.files[0]);
            await contractsApi.upload(formData);
            setUploadSuccess(true);
            setTenantId('');
            setUnitId('');
            if (fileRef.current) fileRef.current.value = '';
            await load();
        } catch {
            setUploadError('Failed to upload contract. Please try again.');
        } finally {
            setIsUploading(false);
        }
    }

    async function handleDownload(id: string) {
        try {
            const result = await contractsApi.getDownloadUrl(id);
            window.open(result.downloadUrl, '_blank');
        } catch (e) {
            console.error(e);
        }
    }

    const selectClass =
        'flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 shadow-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:opacity-50';

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Contracts</h2>

            {/* Upload Form */}
            <Card>
                <CardHeader>
                    <CardTitle>Upload Contract</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleUpload} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="tenantId">Tenant</Label>
                                <select
                                    id="tenantId"
                                    value={tenantId}
                                    onChange={e => setTenantId(e.target.value)}
                                    required
                                    className={selectClass}
                                >
                                    <option value="">Select a tenant…</option>
                                    {tenants.map(t => (
                                        <option key={t.id} value={t.id}>{t.email}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="unitId">Unit</Label>
                                <select
                                    id="unitId"
                                    value={unitId}
                                    onChange={e => setUnitId(e.target.value)}
                                    required
                                    className={selectClass}
                                >
                                    <option value="">Select a unit…</option>
                                    {units.map(u => (
                                        <option key={u.id} value={u.id}>{u.unitNumber}</option>
                                    ))}
                                </select>
                            </div>
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="file">Contract PDF</Label>
                            <Input id="file" type="file" accept=".pdf" ref={fileRef} required />
                        </div>
                        {uploadError && <p className="text-sm text-red-500">{uploadError}</p>}
                        {uploadSuccess && <p className="text-sm text-green-600">Contract uploaded successfully!</p>}
                        <Button type="submit" disabled={isUploading}>
                            {isUploading ? 'Uploading...' : 'Upload Contract'}
                        </Button>
                    </form>
                </CardContent>
            </Card>

            {/* All Contracts */}
            <Card>
                <CardHeader>
                    <CardTitle>All Contracts</CardTitle>
                </CardHeader>
                <CardContent>
                    {contracts.length === 0 ? (
                        <p className="text-sm text-slate-500">No contracts found.</p>
                    ) : (
                        <div className="space-y-3">
                            {contracts.map(c => (
                                <div key={c.id} className="flex items-center justify-between py-2 border-b last:border-0">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-2">
                                            <p className="text-sm font-medium">{c.fileName}</p>
                                            {c.isCurrent && <Badge className="bg-green-100 text-green-800">Current</Badge>}
                                        </div>
                                        <p className="text-xs text-slate-500">
                                            Uploaded {new Date(c.uploadedAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                    <Button variant="outline" size="sm" onClick={() => handleDownload(c.id)}>
                                        Download
                                    </Button>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

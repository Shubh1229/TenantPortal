'use client';

import { useEffect, useState, useRef } from 'react';
import { contractsApi } from '@/lib/api/contracts';
import { authApi } from '@/lib/api/auth';
import { transactionsApi } from '@/lib/api/transactions';
import { Contract, PublicUserProfile, Unit } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { X, Eye, Download, Trash2, FileText } from 'lucide-react';

export default function AdminContractsPage() {
    const [contracts, setContracts] = useState<Contract[]>([]);
    const [tenants, setTenants] = useState<PublicUserProfile[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isUploading, setIsUploading] = useState(false);
    const [tenantId, setTenantId] = useState('');
    const [unitId, setUnitId] = useState('');
    const [uploadError, setUploadError] = useState('');
    const [uploadSuccess, setUploadSuccess] = useState(false);
    const fileRef = useRef<HTMLInputElement>(null);

    // PDF preview
    const [previewContract, setPreviewContract] = useState<Contract | null>(null);

    useEffect(() => {
        Promise.all([
            load(),
            authApi.getUsers('Tenant').then(users =>
                Promise.all(users.map(u => authApi.getPublicProfile(u.id).catch(() => ({
                    id: u.id, email: u.email,
                } as PublicUserProfile))))
            ).then(setTenants),
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

    async function handleDelete(c: Contract) {
        if (!confirm(`Delete "${c.fileName}"? This cannot be undone.`)) return;
        try {
            await contractsApi.delete(c.id);
            setContracts(prev => prev.filter(x => x.id !== c.id));
            if (previewContract?.id === c.id) setPreviewContract(null);
        } catch {
            alert('Failed to delete contract.');
        }
    }

    function tenantLabel(id: string) {
        const t = tenants.find(x => x.id === id);
        if (!t) return id;
        const name = [t.firstName, t.lastName].filter(Boolean).join(' ');
        return name ? `${name} (${t.email})` : t.email;
    }

    function unitLabel(id: string) {
        const u = units.find(x => x.id === id);
        return u ? `Unit ${u.unitNumber}` : id;
    }

    const selectClass =
        'flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 shadow-sm focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:opacity-50';

    if (isLoading) return <div className="text-zinc-400 text-sm">Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Contracts</h2>

            {/* PDF preview overlay */}
            {previewContract && (
                <div className="fixed inset-0 z-50 flex flex-col bg-zinc-950/95">
                    <div className="flex items-center justify-between px-4 py-3 border-b border-zinc-800 shrink-0">
                        <div className="flex items-center gap-3">
                            <FileText size={18} className="text-indigo-400" />
                            <span className="text-sm font-medium text-zinc-200">{previewContract.fileName}</span>
                            {previewContract.isCurrent && (
                                <Badge className="bg-emerald-500/10 text-emerald-400 ring-1 ring-emerald-500/20 text-[10px]">Current</Badge>
                            )}
                        </div>
                        <div className="flex items-center gap-2">
                            <Button
                                size="sm"
                                variant="outline"
                                className="border-zinc-700 text-zinc-300 hover:text-white"
                                onClick={() => handleDownload(previewContract.id)}
                            >
                                <Download size={14} className="mr-1.5" />
                                Download
                            </Button>
                            <button
                                onClick={() => setPreviewContract(null)}
                                className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-200 transition-colors"
                            >
                                <X size={18} />
                            </button>
                        </div>
                    </div>
                    <iframe
                        src={previewContract.previewUrl}
                        className="flex-1 w-full border-0"
                        title={previewContract.fileName}
                    />
                </div>
            )}

            {/* Upload Form */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader>
                    <CardTitle className="text-base">Upload Contract</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleUpload} className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="tenantId" className="text-zinc-300">Tenant</Label>
                                <select
                                    id="tenantId"
                                    value={tenantId}
                                    onChange={e => setTenantId(e.target.value)}
                                    required
                                    className={selectClass}
                                >
                                    <option value="">Select a tenant…</option>
                                    {tenants.map(t => {
                                        const name = [t.firstName, t.lastName].filter(Boolean).join(' ');
                                        return (
                                            <option key={t.id} value={t.id}>
                                                {name ? `${name} (${t.email})` : t.email}
                                            </option>
                                        );
                                    })}
                                </select>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="unitId" className="text-zinc-300">Unit</Label>
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
                            <Label htmlFor="file" className="text-zinc-300">Contract PDF</Label>
                            <Input id="file" type="file" accept=".pdf" ref={fileRef} required
                                className="bg-zinc-900 border-zinc-700 text-zinc-100" />
                        </div>
                        {uploadError && <p className="text-sm text-red-400">{uploadError}</p>}
                        {uploadSuccess && <p className="text-sm text-emerald-400">Contract uploaded successfully.</p>}
                        <Button type="submit" disabled={isUploading} className="bg-indigo-600 hover:bg-indigo-500 text-white">
                            {isUploading ? 'Uploading...' : 'Upload Contract'}
                        </Button>
                    </form>
                </CardContent>
            </Card>

            {/* Contract list */}
            <Card className="bg-zinc-900 border-zinc-800">
                <CardHeader>
                    <CardTitle className="text-base">All Contracts</CardTitle>
                </CardHeader>
                <CardContent>
                    {contracts.length === 0 ? (
                        <p className="text-sm text-zinc-500">No contracts found.</p>
                    ) : (
                        <div className="space-y-1">
                            {contracts.map(c => (
                                <div key={c.id} className="flex items-center justify-between py-3 border-b border-zinc-800 last:border-0">
                                    <div className="space-y-0.5 min-w-0 mr-4">
                                        <div className="flex items-center gap-2 flex-wrap">
                                            <p className="text-sm font-medium text-zinc-200 truncate">{c.fileName}</p>
                                            {c.isCurrent && (
                                                <Badge className="bg-emerald-500/10 text-emerald-400 ring-1 ring-emerald-500/20 shrink-0">Current</Badge>
                                            )}
                                        </div>
                                        <p className="text-xs text-zinc-500">
                                            {tenantLabel(c.tenantId)} · Uploaded {new Date(c.uploadedAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-2 shrink-0">
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            className="border-zinc-700 text-zinc-300 hover:text-white"
                                            onClick={() => setPreviewContract(c)}
                                        >
                                            <Eye size={14} className="mr-1.5" />
                                            View
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            className="border-zinc-700 text-zinc-300 hover:text-white"
                                            onClick={() => handleDownload(c.id)}
                                        >
                                            <Download size={14} className="mr-1.5" />
                                            Download
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            className="border-red-900/40 text-red-400 hover:text-red-300"
                                            onClick={() => handleDelete(c)}
                                        >
                                            <Trash2 size={14} />
                                        </Button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

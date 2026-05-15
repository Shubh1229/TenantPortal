'use client';

import { useEffect, useState } from 'react';
import { contractsApi } from '@/lib/api/contracts';
import { Contract } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

export default function TenantContractsPage() {
    const [contracts, setContracts] = useState<Contract[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
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
        load();
    }, []);

    async function handleDownload(id: string) {
        try {
            const result = await contractsApi.getDownloadUrl(id);
            window.open(result.downloadUrl, '_blank');
        } catch (e) {
            console.error(e);
        }
    }

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">My Contracts</h2>
            {contracts.length === 0 ? (
                <Card>
                    <CardContent className="py-8 text-center text-slate-500">
                        No contracts found.
                    </CardContent>
                </Card>
            ) : (
                <div className="space-y-4">
                    {contracts.map(c => (
                        <Card key={c.id}>
                            <CardContent className="flex items-center justify-between py-4">
                                <div className="space-y-1">
                                    <div className="flex items-center gap-2">
                                        <p className="font-medium">{c.fileName}</p>
                                        {c.isCurrent && <Badge className="bg-green-100 text-green-800">Current</Badge>}
                                    </div>
                                    <p className="text-sm text-slate-500">
                                        Uploaded {new Date(c.uploadedAt).toLocaleDateString()}
                                    </p>
                                </div>
                                <Button variant="outline" size="sm" onClick={() => handleDownload(c.id)}>
                                    Download
                                </Button>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
        </div>
    );
}
'use client';

import { useEffect, useState } from 'react';
import { contractsApi } from '@/lib/api/contracts';
import { Contract } from '@/types';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { X, Download, Eye } from 'lucide-react';

export default function TenantContractsPage() {
    const [contracts, setContracts] = useState<Contract[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);
    const [previewName, setPreviewName] = useState('');

    useEffect(() => {
        contractsApi.getAll()
            .then(setContracts)
            .catch(console.error)
            .finally(() => setIsLoading(false));
    }, []);

    async function handleDownload(id: string) {
        try {
            const result = await contractsApi.getDownloadUrl(id);
            window.open(result.downloadUrl, '_blank');
        } catch (e) {
            console.error(e);
        }
    }

    function openPreview(contract: Contract) {
        setPreviewUrl(contract.previewUrl);
        setPreviewName(contract.fileName);
    }

    function closePreview() {
        setPreviewUrl(null);
        setPreviewName('');
    }

    if (isLoading) return <div className="text-zinc-400">Loading...</div>;

    return (
        <>
            {/* Full-screen PDF preview overlay */}
            {previewUrl && (
                <div className="fixed inset-0 z-50 flex flex-col bg-zinc-950">
                    <div className="flex items-center justify-between px-4 py-3 bg-zinc-900 border-b border-zinc-800 shrink-0">
                        <span className="text-sm font-medium text-zinc-200 truncate max-w-[60%]">{previewName}</span>
                        <div className="flex items-center gap-2">
                            <Button
                                size="sm"
                                variant="outline"
                                className="border-zinc-700 text-zinc-300 hover:bg-zinc-800"
                                onClick={() => window.open(previewUrl, '_blank')}
                            >
                                <Download size={14} className="mr-1.5" />
                                Download
                            </Button>
                            <Button
                                size="sm"
                                variant="ghost"
                                className="text-zinc-400 hover:text-zinc-100"
                                onClick={closePreview}
                            >
                                <X size={16} />
                            </Button>
                        </div>
                    </div>
                    <iframe
                        src={previewUrl}
                        className="flex-1 w-full border-0"
                        title={previewName}
                    />
                </div>
            )}

            <div className="space-y-6">
                <h2 className="text-2xl font-semibold">My Contracts</h2>
                {contracts.length === 0 ? (
                    <Card className="bg-zinc-900 border-zinc-800">
                        <CardContent className="py-8 text-center text-zinc-500">
                            No contracts found.
                        </CardContent>
                    </Card>
                ) : (
                    <div className="space-y-3">
                        {contracts.map(c => (
                            <Card key={c.id} className="bg-zinc-900 border-zinc-800">
                                <CardContent className="flex items-center justify-between py-4">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-2">
                                            <p className="font-medium text-zinc-100">{c.fileName}</p>
                                            {c.isCurrent && (
                                                <Badge className="bg-emerald-500/10 text-emerald-400 border-emerald-500/20 text-xs">
                                                    Current
                                                </Badge>
                                            )}
                                        </div>
                                        <p className="text-sm text-zinc-500">
                                            Uploaded {new Date(c.uploadedAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            className="border-zinc-700 text-zinc-300 hover:bg-zinc-800"
                                            onClick={() => openPreview(c)}
                                        >
                                            <Eye size={14} className="mr-1.5" />
                                            View
                                        </Button>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            className="border-zinc-700 text-zinc-300 hover:bg-zinc-800"
                                            onClick={() => handleDownload(c.id)}
                                        >
                                            <Download size={14} className="mr-1.5" />
                                            Download
                                        </Button>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                )}
            </div>
        </>
    );
}

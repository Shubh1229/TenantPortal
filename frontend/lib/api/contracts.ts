import { apiRequest } from './client';
import { Contract } from '@/types';

export const contractsApi = {
    getAll: () =>
        apiRequest<Contract[]>('/api/contracts'),

    getById: (id: string) =>
        apiRequest<Contract>(`/api/contracts/${id}`),

    getDownloadUrl: (id: string) =>
        apiRequest<{ downloadUrl: string; fileName: string; expiresAt: string }>(
            `/api/contracts/${id}/download`
        ),

    upload: (formData: FormData) =>
        fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/contracts/upload`, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
            },
            body: formData,
        }),

    delete: (id: string) =>
        apiRequest<void>(`/api/contracts/${id}`, { method: 'DELETE' }),
};
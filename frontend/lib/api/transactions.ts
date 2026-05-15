import { apiRequest } from './client';
import { Transaction } from '@/types';

export const transactionsApi = {
    getAll: () =>
        apiRequest<Transaction[]>('/api/transactions'),

    getById: (id: string) =>
        apiRequest<Transaction>(`/api/transactions/${id}`),

    createManual: (data: unknown) =>
        apiRequest<void>('/api/transactions', {
            method: 'POST',
            body: data,
        }),

    submitExternal: (data: unknown) =>
        apiRequest<void>('/api/transactions/external', {
            method: 'POST',
            body: data,
        }),

    approve: (id: string) =>
        apiRequest<void>(`/api/transactions/${id}/approve`, {
            method: 'PATCH',
        }),

    decline: (id: string) =>
        apiRequest<void>(`/api/transactions/${id}/decline`, {
            method: 'PATCH',
        }),

    createPaymentIntent: (data: unknown) =>
        apiRequest<string>('/api/transactions/payment-intent', {
            method: 'POST',
            body: data,
        }),
};
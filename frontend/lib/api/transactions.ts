import { apiRequest } from './client';
import { BillingMode, Property, RentSchedule, Transaction, Unit } from '@/types';

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

    getRentSchedule: (tenantId: string) =>
        apiRequest<RentSchedule>(`/api/rent-schedule/${tenantId}`),

    getMyRentSchedule: () =>
        apiRequest<RentSchedule>('/api/rent-schedule/my'),

    createPaymentIntent: (data: unknown) =>
        apiRequest<string>('/api/transactions/payment-intent', {
            method: 'POST',
            body: data,
        }),

    // ── Properties ────────────────────────────────────────────────────────────────

    getProperties: () =>
        apiRequest<Property[]>('/api/properties'),

    createProperty: (data: { name: string; address: string }) =>
        apiRequest<Property>('/api/properties', { method: 'POST', body: data }),

    updateProperty: (id: string, data: { name?: string; address?: string }) =>
        apiRequest<Property>(`/api/properties/${id}`, { method: 'PUT', body: data }),

    deleteProperty: (id: string) =>
        apiRequest<{ success: boolean }>(`/api/properties/${id}`, { method: 'DELETE' }),

    // ── Units ─────────────────────────────────────────────────────────────────────

    getUnits: (propertyId?: string) =>
        apiRequest<Unit[]>(`/api/units${propertyId ? `?propertyId=${propertyId}` : ''}`),

    createUnit: (data: {
        propertyId: string;
        unitNumber: string;
        bedrooms?: number;
        bathrooms?: number;
        squareFeet?: number;
        billingMode?: BillingMode;
    }) =>
        apiRequest<Unit>('/api/units', { method: 'POST', body: data }),

    updateUnit: (id: string, data: {
        unitNumber?: string;
        bedrooms?: number;
        bathrooms?: number;
        squareFeet?: number;
        billingMode?: BillingMode;
    }) =>
        apiRequest<Unit>(`/api/units/${id}`, { method: 'PUT', body: data }),

    deleteUnit: (id: string) =>
        apiRequest<{ success: boolean }>(`/api/units/${id}`, { method: 'DELETE' }),

    assignTenant: (unitId: string, data: { tenantId: string; startDate: string }) =>
        apiRequest<void>(`/api/units/${unitId}/assign-tenant`, { method: 'POST', body: data }),

    removeTenant: (unitId: string, tenantId: string) =>
        apiRequest<void>(`/api/units/${unitId}/remove-tenant/${tenantId}`, { method: 'DELETE' }),

    // ── Rent Schedules ────────────────────────────────────────────────────────────

    getAllRentSchedules: () =>
        apiRequest<RentSchedule[]>('/api/rent-schedules'),

    getUnitRentSchedule: (unitId: string) =>
        apiRequest<RentSchedule>(`/api/units/${unitId}/rent-schedule`),

    updateRentSchedule: (id: string, data: { monthlyAmount?: number; dueDayOfMonth?: number; endDate?: string }) =>
        apiRequest<void>(`/api/rent-schedule/${id}`, { method: 'PATCH', body: data }),

    deleteRentSchedule: (id: string) =>
        apiRequest<void>(`/api/rent-schedule/${id}`, { method: 'DELETE' }),
};

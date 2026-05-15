import { apiRequest } from './client';
import { Notification, NotificationPreference, ReminderSetting } from '@/types';

export const notificationsApi = {
    getAll: () =>
        apiRequest<Notification[]>('/api/notifications'),

    markAsRead: (id: string) =>
        apiRequest<void>(`/api/notifications/${id}/read`, {
            method: 'PATCH',
        }),

    getPreferences: () =>
        apiRequest<NotificationPreference>('/api/notification-preferences'),

    updatePreferences: (data: NotificationPreference) =>
        apiRequest<void>('/api/notification-preferences', {
            method: 'PATCH',
            body: data,
        }),

    getReminders: () =>
        apiRequest<ReminderSetting[]>('/api/reminders'),

    createReminder: (data: unknown) =>
        apiRequest<void>('/api/reminders', {
            method: 'POST',
            body: data,
        }),

    updateReminder: (id: string, data: unknown) =>
        apiRequest<void>(`/api/reminders/${id}`, {
            method: 'PATCH',
            body: data,
        }),

    deleteReminder: (id: string) =>
        apiRequest<void>(`/api/reminders/${id}`, {
            method: 'DELETE',
        }),
};
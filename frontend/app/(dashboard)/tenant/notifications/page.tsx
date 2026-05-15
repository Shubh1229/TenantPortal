'use client';

import { useEffect, useState } from 'react';
import { notificationsApi } from '@/lib/api/notifications';
import { Notification, ReminderSetting, NotificationPreference } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function NotificationsPage() {
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [reminders, setReminders] = useState<ReminderSetting[]>([]);
    const [preference, setPreference] = useState<NotificationPreference | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [daysBefore, setDaysBefore] = useState('7');
    const [sendTime, setSendTime] = useState('09:00');

    useEffect(() => {
        async function load() {
            try {
                const [notifs, rems, pref] = await Promise.all([
                    notificationsApi.getAll(),
                    notificationsApi.getReminders(),
                    notificationsApi.getPreferences(),
                ]);
                setNotifications(notifs);
                setReminders(rems);
                setPreference(pref);
            } catch (e) {
                console.error(e);
            } finally {
                setIsLoading(false);
            }
        }
        load();
    }, []);

    async function handleMarkRead(id: string) {
        try {
            await notificationsApi.markAsRead(id);
            setNotifications(prev =>
                prev.map(n => n.id === id ? { ...n, isRead: true } : n)
            );
        } catch (e) {
            console.error(e);
        }
    }

    async function handleToggleEmail() {
        if (!preference) return;
        try {
            const updated = { emailEnabled: !preference.emailEnabled };
            await notificationsApi.updatePreferences(updated);
            setPreference(updated);
        } catch (e) {
            console.error(e);
        }
    }

    async function handleAddReminder(e: React.FormEvent) {
        e.preventDefault();
        try {
            await notificationsApi.createReminder({ daysBefore: parseInt(daysBefore), sendTime });
            const updated = await notificationsApi.getReminders();
            setReminders(updated);
            setDaysBefore('7');
            setSendTime('09:00');
        } catch (e) {
            console.error(e);
        }
    }

    async function handleDeleteReminder(id: string) {
        try {
            await notificationsApi.deleteReminder(id);
            setReminders(prev => prev.filter(r => r.id !== id));
        } catch (e) {
            console.error(e);
        }
    }

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-semibold">Notifications</h2>

            {/* Email Preference */}
            <Card>
                <CardHeader>
                    <CardTitle>Email Notifications</CardTitle>
                </CardHeader>
                <CardContent className="flex items-center justify-between">
                    <p className="text-sm text-slate-500">
                        {preference?.emailEnabled ? 'Email notifications are enabled' : 'Email notifications are disabled'}
                    </p>
                    <Button variant="outline" size="sm" onClick={handleToggleEmail}>
                        {preference?.emailEnabled ? 'Disable' : 'Enable'}
                    </Button>
                </CardContent>
            </Card>

            {/* Reminders */}
            <Card>
                <CardHeader>
                    <CardTitle>Rent Reminders</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    {reminders.length === 0 ? (
                        <p className="text-sm text-slate-500">No reminders set.</p>
                    ) : (
                        <div className="space-y-2">
                            {reminders.map(r => (
                                <div key={r.id} className="flex items-center justify-between py-2 border-b last:border-0">
                                    <p className="text-sm">
                                        {r.daysBefore} day{r.daysBefore !== 1 ? 's' : ''} before at {r.sendTime}
                                    </p>
                                    <Button
                                        variant="destructive"
                                        size="sm"
                                        onClick={() => handleDeleteReminder(r.id)}
                                    >
                                        Remove
                                    </Button>
                                </div>
                            ))}
                        </div>
                    )}
                    <form onSubmit={handleAddReminder} className="flex items-end gap-3 pt-2">
                        <div className="space-y-2">
                            <Label htmlFor="days">Days Before</Label>
                            <Input
                                id="days"
                                type="number"
                                min="1"
                                max="30"
                                value={daysBefore}
                                onChange={e => setDaysBefore(e.target.value)}
                                className="w-24"
                            />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="time">Send Time</Label>
                            <Input
                                id="time"
                                type="time"
                                value={sendTime}
                                onChange={e => setSendTime(e.target.value)}
                                className="w-32"
                            />
                        </div>
                        <Button type="submit">Add Reminder</Button>
                    </form>
                </CardContent>
            </Card>

            {/* Notification Inbox */}
            <Card>
                <CardHeader>
                    <CardTitle>Inbox</CardTitle>
                </CardHeader>
                <CardContent>
                    {notifications.length === 0 ? (
                        <p className="text-sm text-slate-500">No notifications.</p>
                    ) : (
                        <div className="space-y-3">
                            {notifications.map(n => (
                                <div key={n.id} className="flex items-start justify-between py-2 border-b last:border-0">
                                    <div className="flex items-start gap-3">
                                        {!n.isRead && (
                                            <div className="w-2 h-2 rounded-full bg-blue-500 mt-1.5 shrink-0" />
                                        )}
                                        <div>
                                            <p className={`text-sm ${n.isRead ? 'text-slate-400' : ''}`}>{n.message}</p>
                                            <p className="text-xs text-slate-500">
                                                {new Date(n.createdAt).toLocaleDateString()}
                                            </p>
                                        </div>
                                    </div>
                                    {!n.isRead && (
                                        <Button variant="ghost" size="sm" onClick={() => handleMarkRead(n.id)}>
                                            Mark read
                                        </Button>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
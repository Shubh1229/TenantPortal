'use client';

import { Transaction } from '@/types';
import {
    AreaChart,
    Area,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
} from 'recharts';

interface Props {
    transactions: Transaction[];
}

interface MonthPoint {
    month: string;
    revenue: number;
}

function aggregateByMonth(transactions: Transaction[]): MonthPoint[] {
    const map: Record<string, number> = {};

    for (const t of transactions) {
        if (t.status !== 'Confirmed') continue;
        const date = new Date(t.paidDate ?? t.createdAt);
        const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
        map[key] = (map[key] ?? 0) + t.amount;
    }

    return Object.entries(map)
        .sort(([a], [b]) => a.localeCompare(b))
        .slice(-12)
        .map(([key, revenue]) => ({
            month: new Date(key + '-01').toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
            revenue: Math.round(revenue * 100) / 100,
        }));
}

export default function RevenueChart({ transactions }: Props) {
    const data = aggregateByMonth(transactions);

    if (data.length === 0) {
        return (
            <p className="text-sm text-zinc-500 py-6 text-center">
                No confirmed revenue data yet
            </p>
        );
    }

    return (
        <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={data} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
                <defs>
                    <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                        <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
                <XAxis
                    dataKey="month"
                    tick={{ fill: '#71717a', fontSize: 11 }}
                    axisLine={false}
                    tickLine={false}
                />
                <YAxis
                    tick={{ fill: '#71717a', fontSize: 11 }}
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={v => `$${v}`}
                    width={55}
                />
                <Tooltip
                    contentStyle={{
                        background: '#18181b',
                        border: '1px solid #3f3f46',
                        borderRadius: 8,
                        color: '#e4e4e7',
                        fontSize: 13,
                    }}
                    formatter={(value: number) => [`$${value.toFixed(2)}`, 'Revenue']}
                />
                <Area
                    type="monotone"
                    dataKey="revenue"
                    stroke="#6366f1"
                    strokeWidth={2}
                    fill="url(#revenueGrad)"
                />
            </AreaChart>
        </ResponsiveContainer>
    );
}

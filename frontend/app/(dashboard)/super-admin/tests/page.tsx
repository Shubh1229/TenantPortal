'use client';

import { useState } from 'react';
import { adminApi, TestSuiteResult, TestResult } from '@/lib/api/admin';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
    CheckCircle2,
    XCircle,
    ChevronDown,
    ChevronRight,
    Play,
    RefreshCw,
} from 'lucide-react';

const CATEGORY_ORDER = ['Database', 'Azure', 'Security', 'Connectivity'];

export default function SystemTestsPage() {
    const [results, setResults] = useState<TestSuiteResult | null>(null);
    const [running, setRunning] = useState(false);
    const [error, setError] = useState('');
    const [expanded, setExpanded] = useState<Set<string>>(new Set());

    async function runTests() {
        setRunning(true);
        setError('');
        setResults(null);
        setExpanded(new Set());
        try {
            const data = await adminApi.runTests();
            setResults(data);
            // Auto-expand any failing tests so the user sees the logs immediately
            const failNames = new Set(data.tests.filter(t => !t.passed).map(t => t.name));
            setExpanded(failNames);
        } catch (err) {
            let msg = 'Failed to run tests.';
            if (err instanceof Error) {
                try { msg = JSON.parse(err.message).error ?? err.message; } catch { msg = err.message; }
            }
            setError(msg);
        } finally {
            setRunning(false);
        }
    }

    function toggle(name: string) {
        setExpanded(prev => {
            const next = new Set(prev);
            if (next.has(name)) next.delete(name); else next.add(name);
            return next;
        });
    }

    const categories = CATEGORY_ORDER.filter(cat =>
        results?.tests.some(t => t.category === cat)
    );

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-white">System Tests</h1>
                    <p className="text-sm text-zinc-500 mt-0.5">
                        Live health checks against every service and dependency
                    </p>
                </div>
                <Button
                    onClick={runTests}
                    disabled={running}
                    className="bg-indigo-600 hover:bg-indigo-500 text-white gap-2"
                >
                    {running ? (
                        <><RefreshCw size={15} className="animate-spin" /> Running…</>
                    ) : (
                        <><Play size={15} /> Run Tests</>
                    )}
                </Button>
            </div>

            {/* Error banner */}
            {error && (
                <div className="rounded-lg border border-red-800 bg-red-950/40 px-4 py-3 text-sm text-red-400">
                    {error}
                </div>
            )}

            {/* Loading placeholder */}
            {running && !results && (
                <div className="rounded-lg border border-zinc-800 bg-zinc-900 px-4 py-12 text-center">
                    <RefreshCw size={22} className="animate-spin mx-auto mb-3 text-indigo-500" />
                    <p className="text-sm text-zinc-400">Running system health checks…</p>
                    <p className="text-xs text-zinc-600 mt-1">This may take a few seconds</p>
                </div>
            )}

            {/* Results */}
            {results && (
                <>
                    {/* Summary row */}
                    <div className="grid grid-cols-4 gap-3">
                        <StatCard label="Passed" value={results.passed} valueClass="text-emerald-400" />
                        <StatCard label="Failed" value={results.failed} valueClass={results.failed > 0 ? 'text-red-400' : 'text-zinc-500'} />
                        <StatCard label="Total" value={results.tests.length} valueClass="text-zinc-200" />
                        <StatCard label="Duration" value={`${results.totalDurationMs} ms`} valueClass="text-indigo-400" />
                    </div>

                    {/* Per-category results */}
                    {categories.map(cat => (
                        <section key={cat}>
                            <h2 className="text-[11px] uppercase tracking-widest text-zinc-500 font-medium mb-2 px-1">
                                {cat}
                            </h2>
                            <div className="space-y-1.5">
                                {results.tests.filter(t => t.category === cat).map(test => (
                                    <TestRow
                                        key={test.name}
                                        test={test}
                                        isExpanded={expanded.has(test.name)}
                                        onToggle={() => toggle(test.name)}
                                    />
                                ))}
                            </div>
                        </section>
                    ))}

                    <p className="text-xs text-zinc-700 text-right">
                        Run at {new Date(results.runAt).toLocaleString()}
                    </p>
                </>
            )}
        </div>
    );
}

function StatCard({ label, value, valueClass }: { label: string; value: string | number; valueClass: string }) {
    return (
        <Card className="bg-zinc-900 border-zinc-800">
            <CardContent className="p-4">
                <p className="text-[11px] uppercase tracking-wider text-zinc-500">{label}</p>
                <p className={`text-2xl font-bold mt-0.5 tabular-nums ${valueClass}`}>{value}</p>
            </CardContent>
        </Card>
    );
}

function TestRow({ test, isExpanded, onToggle }: { test: TestResult; isExpanded: boolean; onToggle: () => void }) {
    const hasLogs = test.logs.length > 0;
    return (
        <div className={`rounded-lg border ${test.passed ? 'border-zinc-800' : 'border-red-900/60 bg-red-950/10'} bg-zinc-900 overflow-hidden`}>
            <button
                className="w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-zinc-800/50 transition-colors"
                onClick={hasLogs ? onToggle : undefined}
                style={{ cursor: hasLogs ? 'pointer' : 'default' }}
            >
                {test.passed
                    ? <CheckCircle2 size={15} className="text-emerald-500 shrink-0" />
                    : <XCircle size={15} className="text-red-500 shrink-0" />
                }
                <span className="flex-1 text-sm font-medium text-zinc-100">{test.name}</span>
                <span className={`text-xs mr-4 ${test.passed ? 'text-zinc-400' : 'text-red-400'}`}>
                    {test.message}
                </span>
                <span className="text-xs text-zinc-600 tabular-nums mr-2">{test.durationMs}ms</span>
                {hasLogs && (
                    isExpanded
                        ? <ChevronDown size={13} className="text-zinc-500 shrink-0" />
                        : <ChevronRight size={13} className="text-zinc-500 shrink-0" />
                )}
            </button>

            {isExpanded && hasLogs && (
                <div className="border-t border-zinc-800 bg-zinc-950 px-5 py-3">
                    <pre className="text-xs text-zinc-400 font-mono whitespace-pre-wrap leading-relaxed">
                        {test.logs.join('\n')}
                    </pre>
                </div>
            )}
        </div>
    );
}

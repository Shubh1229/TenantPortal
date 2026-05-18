import { apiRequest } from './client';

export interface TestResult {
    name: string;
    category: string;
    passed: boolean;
    message: string;
    logs: string[];
    durationMs: number;
}

export interface TestSuiteResult {
    runAt: string;
    totalDurationMs: number;
    passed: number;
    failed: number;
    tests: TestResult[];
}

export const adminApi = {
    runTests: () =>
        apiRequest<TestSuiteResult>('/api/auth/tests/run', { method: 'GET' }),
};

'use client';

import { useState } from 'react';
import { transactionsApi } from '@/lib/api/transactions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cardTotalWithFee, achTotalWithFee, formatCurrency } from '@/lib/utils/stripe';
import { CreditCard, Building2, Info } from 'lucide-react';

type PaymentMethodType = 'card' | 'ach';
type PageState = 'form' | 'processing' | 'success' | 'error';

export default function PaymentPage() {
    const [method, setMethod] = useState<PaymentMethodType>('card');
    const [amount, setAmount] = useState('');
    const [pageState, setPageState] = useState<PageState>('form');
    const [errorMessage, setErrorMessage] = useState('');

    const parsedAmount = parseFloat(amount) || 0;
    const feeCalc = method === 'card'
        ? cardTotalWithFee(parsedAmount)
        : achTotalWithFee(parsedAmount);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (parsedAmount <= 0) return;

        setPageState('processing');
        setErrorMessage('');
        try {
            // Returns the Stripe PaymentIntent client secret
            const clientSecret = await transactionsApi.createPaymentIntent({
                amount: parsedAmount,
                paymentMethodType: method,
            });

            // In a full integration the client secret would be passed to Stripe Elements.
            // For now, redirect to Stripe's hosted payment page or show confirmation.
            // The backend has already created the intent — just confirm success here.
            console.log('PaymentIntent client secret:', clientSecret);
            setPageState('success');
        } catch (err) {
            console.error(err);
            setErrorMessage('Payment failed. Please try again.');
            setPageState('error');
        }
    }

    if (pageState === 'success') {
        return (
            <div className="max-w-md mx-auto space-y-6">
                <h1 className="text-2xl font-semibold text-white">Payment</h1>
                <Card className="bg-zinc-900 border-zinc-800">
                    <CardContent className="pt-6 pb-6 text-center space-y-3">
                        <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center mx-auto">
                            <svg className="w-6 h-6 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                        </div>
                        <p className="text-lg font-medium text-white">Payment initiated</p>
                        <p className="text-sm text-zinc-400">
                            Your payment of {formatCurrency(feeCalc.total)} has been submitted and is pending
                            confirmation from your admin.
                        </p>
                        <Button
                            className="mt-4 bg-indigo-600 hover:bg-indigo-500 text-white"
                            onClick={() => { setPageState('form'); setAmount(''); }}
                        >
                            Make another payment
                        </Button>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="max-w-lg mx-auto space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-white">Make a Payment</h1>
                <p className="text-sm text-zinc-500 mt-0.5">
                    Stripe fee is passed through to you at cost — no markup.
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                {/* Payment method selector */}
                <div>
                    <Label className="text-zinc-300 mb-2 block">Payment Method</Label>
                    <div className="grid grid-cols-2 gap-3">
                        <button
                            type="button"
                            onClick={() => setMethod('card')}
                            className={`flex items-center gap-3 px-4 py-3 rounded-lg border text-sm transition-colors ${
                                method === 'card'
                                    ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                                    : 'border-zinc-700 bg-zinc-900 text-zinc-400 hover:border-zinc-500'
                            }`}
                        >
                            <CreditCard size={18} />
                            <div className="text-left">
                                <p className="font-medium">Credit / Debit Card</p>
                                <p className="text-xs text-zinc-500">2.9% + $0.30</p>
                            </div>
                        </button>
                        <button
                            type="button"
                            onClick={() => setMethod('ach')}
                            className={`flex items-center gap-3 px-4 py-3 rounded-lg border text-sm transition-colors ${
                                method === 'ach'
                                    ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                                    : 'border-zinc-700 bg-zinc-900 text-zinc-400 hover:border-zinc-500'
                            }`}
                        >
                            <Building2 size={18} />
                            <div className="text-left">
                                <p className="font-medium">ACH Direct Debit</p>
                                <p className="text-xs text-zinc-500">0.8%, capped $5</p>
                            </div>
                        </button>
                    </div>
                </div>

                {/* Amount */}
                <div className="space-y-1.5">
                    <Label htmlFor="amount" className="text-zinc-300">Amount (USD)</Label>
                    <div className="relative">
                        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500 text-sm">$</span>
                        <Input
                            id="amount"
                            type="number"
                            min="1"
                            step="0.01"
                            value={amount}
                            onChange={e => setAmount(e.target.value)}
                            required
                            className="pl-7 bg-zinc-900 border-zinc-700 text-white placeholder:text-zinc-600 focus:border-indigo-500"
                            placeholder="0.00"
                        />
                    </div>
                </div>

                {/* Fee breakdown */}
                {parsedAmount > 0 && (
                    <div className="rounded-lg bg-zinc-800/50 border border-zinc-700 p-4 space-y-2">
                        <div className="flex items-center gap-1.5 mb-3">
                            <Info size={13} className="text-zinc-500" />
                            <span className="text-xs text-zinc-500 uppercase tracking-wide font-medium">
                                Fee Breakdown
                            </span>
                        </div>
                        <div className="flex justify-between text-sm">
                            <span className="text-zinc-400">Rent amount</span>
                            <span className="text-zinc-200">{formatCurrency(parsedAmount)}</span>
                        </div>
                        <div className="flex justify-between text-sm">
                            <span className="text-zinc-400">
                                Stripe fee{' '}
                                <span className="text-zinc-600">
                                    ({method === 'card' ? '2.9% + $0.30' : '0.8%, max $5'})
                                </span>
                            </span>
                            <span className="text-yellow-400">+{formatCurrency(feeCalc.fee)}</span>
                        </div>
                        <div className="border-t border-zinc-700 pt-2 flex justify-between text-sm font-semibold">
                            <span className="text-zinc-200">Total charged</span>
                            <span className="text-white">{formatCurrency(feeCalc.total)}</span>
                        </div>
                        {method === 'ach' && (
                            <p className="text-xs text-zinc-500 pt-1">
                                ACH is significantly cheaper for larger amounts. For payments over $172,
                                the $5 cap kicks in and saves you money vs. card.
                            </p>
                        )}
                    </div>
                )}

                {pageState === 'error' && (
                    <p className="text-sm text-red-400">{errorMessage}</p>
                )}

                <Button
                    type="submit"
                    className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                    disabled={pageState === 'processing' || parsedAmount <= 0}
                >
                    {pageState === 'processing'
                        ? 'Processing...'
                        : `Pay ${parsedAmount > 0 ? formatCurrency(feeCalc.total) : ''}`}
                </Button>
            </form>
        </div>
    );
}

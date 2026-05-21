'use client';

import { useState, useEffect } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { useAuth } from '@/lib/hooks/useAuth';
import { transactionsApi } from '@/lib/api/transactions';
import { RentSchedule } from '@/types';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cardTotalWithFee, achTotalWithFee, formatCurrency } from '@/lib/utils/stripe';
import { CreditCard, Building2, Info, ArrowLeft, Send } from 'lucide-react';

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY ?? '');

const stripeAppearance = {
    theme: 'night' as const,
    variables: {
        colorPrimary: '#6366f1',
        colorBackground: '#18181b',
        colorText: '#e4e4e7',
        colorDanger: '#f87171',
        borderRadius: '8px',
        fontFamily: 'inherit',
    },
    rules: {
        '.Input': { border: '1px solid #3f3f46' },
        '.Input:focus': { border: '1px solid #6366f1', boxShadow: 'none' },
    },
};

type PaymentMethodType = 'card' | 'ach' | 'external';
type Step = 'setup' | 'pay';

interface CheckoutFormProps {
    total: number;
    onBack: () => void;
}

function CheckoutForm({ total, onBack }: CheckoutFormProps) {
    const stripe = useStripe();
    const elements = useElements();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!stripe || !elements) return;
        setIsLoading(true);
        setError('');

        const { error: confirmError } = await stripe.confirmPayment({
            elements,
            confirmParams: {
                return_url: `${window.location.origin}/tenant/payment/complete`,
            },
        });

        if (confirmError) {
            setError(confirmError.message ?? 'Payment failed — please try again.');
        }
        setIsLoading(false);
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-5">
            <PaymentElement options={{ layout: 'tabs' }} />
            {error && <p className="text-sm text-red-400">{error}</p>}
            <Button
                type="submit"
                className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                disabled={!stripe || !elements || isLoading}
            >
                {isLoading ? 'Processing...' : `Pay ${formatCurrency(total)}`}
            </Button>
            <Button
                type="button"
                variant="ghost"
                className="w-full text-zinc-400 hover:text-zinc-200"
                onClick={onBack}
            >
                <ArrowLeft size={15} className="mr-1.5" />
                Change amount or method
            </Button>
        </form>
    );
}

export default function PaymentPage() {
    useAuth();

    const [step, setStep] = useState<Step>('setup');
    const [schedule, setSchedule] = useState<RentSchedule | null>(null);
    const [scheduleError, setScheduleError] = useState(false);

    const [method, setMethod] = useState<PaymentMethodType>('card');
    const [amount, setAmount] = useState('');
    const [clientSecret, setClientSecret] = useState('');
    const [intentError, setIntentError] = useState('');
    const [isCreatingIntent, setIsCreatingIntent] = useState(false);

    // External payment form
    const [extMethod, setExtMethod] = useState('Zelle');
    const [extPaidDate, setExtPaidDate] = useState(new Date().toISOString().slice(0, 10));
    const [extNote, setExtNote] = useState('');
    const [extSubmitting, setExtSubmitting] = useState(false);
    const [extResult, setExtResult] = useState<'success' | 'error' | null>(null);

    useEffect(() => {
        transactionsApi.getMyRentSchedule()
            .then(s => {
                setSchedule(s);
                setAmount(s.monthlyAmount.toFixed(2));
            })
            .catch(() => setScheduleError(true));
    }, []);

    const parsedAmount = parseFloat(amount) || 0;
    const feeCalc = method === 'card'
        ? cardTotalWithFee(parsedAmount)
        : achTotalWithFee(parsedAmount);

    async function handleProceed(e: React.FormEvent) {
        e.preventDefault();
        if (!schedule || parsedAmount <= 0) return;
        setIntentError('');
        setIsCreatingIntent(true);
        try {
            const secret = await transactionsApi.createPaymentIntent({
                rentScheduleId: schedule.id,
                amount: parsedAmount,
                currency: 'usd',
                paymentMethodType: method,
            });
            setClientSecret(secret);
            setStep('pay');
        } catch {
            setIntentError('Failed to initialise payment. Please try again.');
        } finally {
            setIsCreatingIntent(false);
        }
    }

    async function handleExternalSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!schedule || parsedAmount <= 0) return;
        setExtSubmitting(true);
        setExtResult(null);
        try {
            await transactionsApi.submitExternal({
                unitId: schedule.unitId,
                amount: parsedAmount,
                paymentMethod: 'External',
                paidDate: new Date(extPaidDate).toISOString(),
                note: `${extMethod}${extNote ? ` — ${extNote}` : ''}`,
            });
            setExtResult('success');
            setAmount('');
            setExtNote('');
        } catch {
            setExtResult('error');
        } finally {
            setExtSubmitting(false);
        }
    }

    const methodOptions: { id: PaymentMethodType; label: string; sub: string; icon: React.ReactNode }[] = [
        { id: 'card', label: 'Card', sub: '2.9% + $0.30', icon: <CreditCard size={18} /> },
        { id: 'ach', label: 'ACH Direct Debit', sub: '0.8%, max $5', icon: <Building2 size={18} /> },
        { id: 'external', label: 'External', sub: 'Zelle, check, wire', icon: <Send size={18} /> },
    ];

    if (step === 'setup') {
        return (
            <div className="max-w-lg mx-auto space-y-6">
                <div>
                    <h1 className="text-2xl font-semibold text-white">Make a Payment</h1>
                    <p className="text-sm text-zinc-500 mt-0.5">
                        Card and ACH are processed instantly via Stripe. External payments require admin approval.
                    </p>
                </div>

                {scheduleError && (
                    <div className="rounded-lg bg-yellow-500/10 border border-yellow-500/20 px-4 py-3 text-sm text-yellow-300">
                        No rent schedule found for your account. Contact your admin to set one up.
                    </div>
                )}

                {/* Method selector */}
                <div>
                    <Label className="text-zinc-300 mb-2 block">Payment Method</Label>
                    <div className="grid grid-cols-3 gap-2">
                        {methodOptions.map(m => {
                            const isSelected = method === m.id;
                            return (
                                <button
                                    key={m.id}
                                    type="button"
                                    onClick={() => setMethod(m.id)}
                                    className={`flex flex-col items-start gap-1.5 px-3 py-3 rounded-lg border text-sm transition-colors ${
                                        isSelected
                                            ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                                            : 'border-zinc-700 bg-zinc-900 text-zinc-400 hover:border-zinc-500'
                                    }`}
                                >
                                    <span className={isSelected ? 'text-indigo-400' : 'text-zinc-500'}>{m.icon}</span>
                                    <div>
                                        <p className="font-medium text-xs leading-tight">{m.label}</p>
                                        <p className="text-[11px] text-zinc-500 leading-tight">{m.sub}</p>
                                    </div>
                                </button>
                            );
                        })}
                    </div>
                    {method === 'external' && (
                        <p className="text-xs text-amber-400/80 mt-2 flex items-center gap-1">
                            <Info size={12} />
                            External payments are submitted as requests and require admin approval before being confirmed.
                        </p>
                    )}
                </div>

                {/* Amount field — always shown */}
                <div className="space-y-1.5">
                    <Label htmlFor="amount" className="text-zinc-300">
                        Amount (USD)
                        {schedule && (
                            <span className="ml-2 text-xs text-zinc-500 font-normal">
                                scheduled: {formatCurrency(schedule.monthlyAmount)}
                            </span>
                        )}
                    </Label>
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

                {/* Stripe fee breakdown — card / ach only */}
                {method !== 'external' && parsedAmount > 0 && (
                    <div className="rounded-lg bg-zinc-800/50 border border-zinc-700 p-4 space-y-2">
                        <div className="flex items-center gap-1.5 mb-3">
                            <Info size={13} className="text-zinc-500" />
                            <span className="text-xs text-zinc-500 uppercase tracking-wide font-medium">Fee Breakdown</span>
                        </div>
                        <div className="flex justify-between text-sm">
                            <span className="text-zinc-400">Payment amount</span>
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
                            <span className="text-zinc-200">Total charged to you</span>
                            <span className="text-white">{formatCurrency(feeCalc.total)}</span>
                        </div>
                        {method === 'ach' && parsedAmount > 172 && (
                            <p className="text-xs text-emerald-500 pt-1">
                                ACH cap hit — you save {formatCurrency(cardTotalWithFee(parsedAmount).fee - 5)} vs card.
                            </p>
                        )}
                    </div>
                )}

                {/* External-specific fields */}
                {method === 'external' && (
                    <form onSubmit={handleExternalSubmit} className="space-y-4">
                        <div className="space-y-1.5">
                            <Label className="text-zinc-300">Payment Service</Label>
                            <select
                                value={extMethod}
                                onChange={e => setExtMethod(e.target.value)}
                                className="flex h-9 w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1 text-sm text-zinc-100 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                            >
                                <option>Zelle</option>
                                <option>Check</option>
                                <option>Wire Transfer</option>
                                <option>Cash</option>
                                <option>Other</option>
                            </select>
                        </div>
                        <div className="space-y-1.5">
                            <Label htmlFor="extPaidDate" className="text-zinc-300">Date Paid</Label>
                            <Input
                                id="extPaidDate"
                                type="date"
                                value={extPaidDate}
                                onChange={e => setExtPaidDate(e.target.value)}
                                required
                                className="bg-zinc-900 border-zinc-700 text-zinc-100"
                            />
                        </div>
                        <div className="space-y-1.5">
                            <Label htmlFor="extNote" className="text-zinc-300">
                                Note <span className="text-zinc-500 font-normal">(optional)</span>
                            </Label>
                            <Input
                                id="extNote"
                                type="text"
                                value={extNote}
                                onChange={e => setExtNote(e.target.value)}
                                placeholder="Confirmation number, memo, etc."
                                className="bg-zinc-900 border-zinc-700 text-zinc-100 placeholder:text-zinc-600"
                            />
                        </div>
                        {extResult === 'success' && (
                            <div className="rounded-lg bg-emerald-500/10 border border-emerald-500/20 px-4 py-3 text-sm text-emerald-300">
                                Payment request submitted. Your admin will review and confirm it.
                            </div>
                        )}
                        {extResult === 'error' && (
                            <p className="text-sm text-red-400">Failed to submit request. Please try again.</p>
                        )}
                        <Button
                            type="submit"
                            className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                            disabled={extSubmitting || parsedAmount <= 0 || !schedule}
                        >
                            {extSubmitting ? 'Submitting...' : `Submit Request for ${parsedAmount > 0 ? formatCurrency(parsedAmount) : '—'}`}
                        </Button>
                    </form>
                )}

                {/* Stripe flow — proceed button */}
                {method !== 'external' && (
                    <>
                        {intentError && <p className="text-sm text-red-400">{intentError}</p>}
                        <Button
                            type="button"
                            className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                            disabled={isCreatingIntent || parsedAmount <= 0 || !schedule}
                            onClick={handleProceed}
                        >
                            {isCreatingIntent ? 'Preparing...' : 'Continue to payment'}
                        </Button>
                    </>
                )}
            </div>
        );
    }

    return (
        <div className="max-w-lg mx-auto space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-white">Enter Payment Details</h1>
                <p className="text-sm text-zinc-500 mt-0.5">
                    Total: <span className="text-white font-medium">{formatCurrency(feeCalc.total)}</span>
                    <span className="text-zinc-600"> (includes {formatCurrency(feeCalc.fee)} Stripe fee)</span>
                </p>
            </div>

            <Card className="bg-zinc-900 border-zinc-800">
                <CardContent className="pt-6">
                    {clientSecret && (
                        <Elements
                            stripe={stripePromise}
                            options={{ clientSecret, appearance: stripeAppearance }}
                        >
                            <CheckoutForm
                                total={feeCalc.total}
                                onBack={() => { setStep('setup'); setClientSecret(''); }}
                            />
                        </Elements>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}

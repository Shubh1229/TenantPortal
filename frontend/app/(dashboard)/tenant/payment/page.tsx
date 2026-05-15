'use client';

import { useState, useEffect } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { useAuth } from '@/lib/hooks/useAuth';
import { transactionsApi } from '@/lib/api/transactions';
import { RentSchedule } from '@/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cardTotalWithFee, achTotalWithFee, formatCurrency } from '@/lib/utils/stripe';
import { CreditCard, Building2, Info, ArrowLeft } from 'lucide-react';

// Stripe instance — created once outside the component tree
const stripePromise = loadStripe(
    process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY ?? ''
);

// Dark theme appearance for Stripe Elements, matching our zinc palette
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

type PaymentMethodType = 'card' | 'ach';
type Step = 'setup' | 'pay';

// ── Inner checkout form — must live inside <Elements> ─────────────────────────

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
                // Stripe redirects here after 3DS / bank auth flows
                return_url: `${window.location.origin}/tenant/payment/complete`,
            },
        });

        // confirmPayment only returns here if there's an error or the redirect didn't happen
        if (confirmError) {
            setError(confirmError.message ?? 'Payment failed — please try again.');
        }
        setIsLoading(false);
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-5">
            <PaymentElement
                options={{
                    layout: 'tabs',
                }}
            />
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

// ── Main page ─────────────────────────────────────────────────────────────────

export default function PaymentPage() {
    const { userId } = useAuth();

    const [step, setStep] = useState<Step>('setup');
    const [schedule, setSchedule] = useState<RentSchedule | null>(null);
    const [scheduleError, setScheduleError] = useState(false);

    const [method, setMethod] = useState<PaymentMethodType>('card');
    const [amount, setAmount] = useState('');
    const [clientSecret, setClientSecret] = useState('');
    const [intentError, setIntentError] = useState('');
    const [isCreatingIntent, setIsCreatingIntent] = useState(false);

    // Load the tenant's rent schedule to pre-fill the amount and get the scheduleId
    useEffect(() => {
        if (!userId) return;
        transactionsApi.getRentSchedule(userId)
            .then(s => {
                setSchedule(s);
                setAmount(s.monthlyAmount.toFixed(2));
            })
            .catch(() => setScheduleError(true));
    }, [userId]);

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

    // ── Setup step ────────────────────────────────────────────────────────────
    if (step === 'setup') {
        return (
            <div className="max-w-lg mx-auto space-y-6">
                <div>
                    <h1 className="text-2xl font-semibold text-white">Make a Payment</h1>
                    <p className="text-sm text-zinc-500 mt-0.5">
                        Stripe processing fee passed through at cost — no markup.
                    </p>
                </div>

                {scheduleError && (
                    <div className="rounded-lg bg-yellow-500/10 border border-yellow-500/20 px-4 py-3 text-sm text-yellow-300">
                        No rent schedule found for your account. Contact your admin to set one up.
                    </div>
                )}

                <form onSubmit={handleProceed} className="space-y-5">
                    {/* Method selector */}
                    <div>
                        <Label className="text-zinc-300 mb-2 block">Payment Method</Label>
                        <div className="grid grid-cols-2 gap-3">
                            {(['card', 'ach'] as const).map(m => {
                                const isSelected = method === m;
                                return (
                                    <button
                                        key={m}
                                        type="button"
                                        onClick={() => setMethod(m)}
                                        className={`flex items-center gap-3 px-4 py-3 rounded-lg border text-sm transition-colors ${
                                            isSelected
                                                ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300'
                                                : 'border-zinc-700 bg-zinc-900 text-zinc-400 hover:border-zinc-500'
                                        }`}
                                    >
                                        {m === 'card'
                                            ? <CreditCard size={18} />
                                            : <Building2 size={18} />}
                                        <div className="text-left">
                                            <p className="font-medium">
                                                {m === 'card' ? 'Card' : 'ACH Direct Debit'}
                                            </p>
                                            <p className="text-xs text-zinc-500">
                                                {m === 'card' ? '2.9% + $0.30' : '0.8%, max $5'}
                                            </p>
                                        </div>
                                    </button>
                                );
                            })}
                        </div>
                    </div>

                    {/* Amount */}
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

                    {/* Fee breakdown */}
                    {parsedAmount > 0 && (
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

                    {intentError && <p className="text-sm text-red-400">{intentError}</p>}

                    <Button
                        type="submit"
                        className="w-full bg-indigo-600 hover:bg-indigo-500 text-white"
                        disabled={isCreatingIntent || parsedAmount <= 0 || !schedule}
                    >
                        {isCreatingIntent ? 'Preparing...' : 'Continue to payment'}
                    </Button>
                </form>
            </div>
        );
    }

    // ── Pay step — only rendered once we have a client secret ─────────────────
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

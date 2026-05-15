// Stripe fee pass-through calculations.
// Card: 2.9% + $0.30 flat. Solve for charge = amount / (1 - 0.029) + 0.30/(1-0.029) (algebraic passthrough).
// ACH:  0.8% capped at $5.00 (Stripe us_bank_account pricing).

export function cardTotalWithFee(amount: number): { total: number; fee: number } {
    const total = (amount + 0.3) / (1 - 0.029);
    return { total, fee: total - amount };
}

export function achTotalWithFee(amount: number): { total: number; fee: number } {
    const fee = Math.min(amount * 0.008, 5.0);
    return { total: amount + fee, fee };
}

export function formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
}

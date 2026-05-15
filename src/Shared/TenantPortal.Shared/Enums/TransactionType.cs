namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// Categories of charges and credits that can be recorded as transactions.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>Monthly rent payment.</summary>
        Rent,

        /// <summary>Security deposit collected at lease start.</summary>
        Deposit,

        /// <summary>Penalty applied when rent is paid after the due date.</summary>
        LateFee,

        /// <summary>Charge for a maintenance or repair service.</summary>
        Maintenance,

        /// <summary>Utility cost passed through to the tenant.</summary>
        Utility,

        /// <summary>Parking space fee.</summary>
        Parking,

        /// <summary>Monthly pet fee.</summary>
        PetFee,

        /// <summary>Refund or credit returned to the tenant.</summary>
        Refund,

        /// <summary>Any charge or credit not covered by the above categories.</summary>
        Other
    }
}

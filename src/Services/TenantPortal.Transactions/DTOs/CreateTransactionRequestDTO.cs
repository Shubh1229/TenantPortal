using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    /// <summary>Request body for creating a manual or historical backfill transaction.</summary>
    public class CreateTransactionRequestDTO
    {
        /// <summary>The tenant the transaction belongs to.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>The unit associated with this charge.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Category of the charge.</summary>
        public required TransactionType Type { get; set; }

        /// <summary>Dollar amount of the transaction.</summary>
        public required decimal Amount { get; set; }

        /// <summary>How the payment was made.</summary>
        public required PaymentMethod PaymentMethod { get; set; }

        /// <summary>Optional description of the external payment method used.</summary>
        public string? ExternalMethodNote { get; set; }

        /// <summary>The date payment was originally due. <c>null</c> for entries without a scheduled due date.</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>The date payment was actually received. <c>null</c> if not yet paid.</summary>
        public DateTime? PaidDate { get; set; }
    }
}

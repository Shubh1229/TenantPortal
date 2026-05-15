using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.Models
{
    public class Transaction
    {
        public required Guid Id { get; set; }
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required TransactionType Type { get; set; }
        public required decimal Amount { get; set; }
        public required TransactionStatus Status { get; set; }
        public required PaymentMethod PaymentMethod { get; set; }
        public string? ExternalMethodNote { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public required Guid CreatedBy { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}

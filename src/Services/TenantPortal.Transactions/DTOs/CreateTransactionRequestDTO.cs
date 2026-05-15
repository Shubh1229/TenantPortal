using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    public class CreateTransactionRequestDTO
    {
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required TransactionType Type { get; set; }
        public required decimal Amount { get; set; }
        public required PaymentMethod PaymentMethod { get; set; }
        public string? ExternalMethodNote { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
    }
}

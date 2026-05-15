using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAllTransactionsAsync(Guid userId, UserRole role);
        Task<Transaction?> GetTransactionAsync(Guid transactionId, Guid userId, UserRole role);
        Task<bool> CreateTransactionAsync(CreateTransactionRequestDTO request, Guid createdBy);
        Task<bool> SubmitExternalPaymentRequestAsync(ExternalPaymentRequestDTO request, Guid tenantId);
        Task<bool> ApproveExternalPaymentRequestAsync(Guid transactionId);
        Task<bool> DeclineExternalPaymentRequestAsync(Guid transactionId);
        Task<bool> SoftDeleteTransactionAsync(Guid transactionId, Guid userId);
    }
}

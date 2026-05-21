using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Interfaces
{
    /// <summary>
    /// Manages transaction CRUD operations, external payment request approvals/declines, and soft deletes.
    /// All reads are scoped by role: tenants see only their own transactions.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Returns all visible transactions for the caller.
        /// Tenants receive only their own; Admins and Super Admins receive all.
        /// </summary>
        Task<List<Transaction>> GetAllTransactionsAsync(Guid userId, UserRole role);

        /// <summary>
        /// Returns a single transaction by ID, enforcing tenant ownership for <see cref="UserRole.Tenant"/> callers.
        /// </summary>
        Task<Transaction?> GetTransactionAsync(Guid transactionId, Guid userId, UserRole role);

        /// <summary>Creates a manual or historical backfill transaction as <c>Confirmed</c>.</summary>
        Task<bool> CreateTransactionAsync(CreateTransactionRequestDTO request, Guid createdBy);

        /// <summary>
        /// Records an external payment claim from a tenant, creating a <c>Pending</c> transaction
        /// that awaits admin approval.
        /// </summary>
        Task<bool> SubmitExternalPaymentRequestAsync(ExternalPaymentRequestDTO request, Guid tenantId);

        /// <summary>Transitions a <c>Pending</c> transaction to <c>Confirmed</c>.</summary>
        Task<bool> ApproveExternalPaymentRequestAsync(Guid transactionId);

        /// <summary>Transitions a <c>Pending</c> transaction to <c>Declined</c>.</summary>
        Task<bool> DeclineExternalPaymentRequestAsync(Guid transactionId);

        /// <summary>Soft-deletes a transaction by setting <c>IsDeleted = true</c>.</summary>
        Task<bool> SoftDeleteTransactionAsync(Guid transactionId, Guid userId);

        /// <summary>Seeds demo rent schedule and transaction history for a Tester account. Idempotent.</summary>
        Task<bool> SeedTesterDataAsync(Guid tenantId);

        /// <summary>
        /// Returns unit and property info for the calling tenant's active assignment,
        /// or <c>null</c> if the tenant is not currently assigned to any unit.
        /// </summary>
        Task<UnitPropertyInfoDTO?> GetMyUnitInfoAsync(Guid tenantId);
    }
}

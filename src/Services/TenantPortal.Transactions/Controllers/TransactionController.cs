using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Interfaces;

namespace TenantPortal.Transactions.Controllers
{
    /// <summary>
    /// Handles transaction management, external payment requests, Stripe payments,
    /// and rent schedule operations.
    /// </summary>
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IStripeService _stripeService;
        private readonly IRentScheduleService _rentScheduleService;

        public TransactionController(
            ITransactionService transactionService,
            IStripeService stripeService,
            IRentScheduleService rentScheduleService)
        {
            _transactionService = transactionService;
            _stripeService = stripeService;
            _rentScheduleService = rentScheduleService;
        }

        /// <summary>
        /// Returns all transactions visible to the caller.
        /// Tenants see only their own; Admins and Super Admins see all.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet]
        public async Task<IActionResult> GetAllTransactionsAsync()
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            var transactions = await _transactionService.GetAllTransactionsAsync(userId.Value, role.Value);
            return Ok(transactions);
        }

        /// <summary>Returns a single transaction by ID. Tenants may only access their own.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            var transaction = await _transactionService.GetTransactionAsync(id, userId.Value, role.Value);
            if (transaction == null) return NotFound("Transaction not found.");
            return Ok(transaction);
        }

        /// <summary>Creates a manual or backfill transaction. Admin and Super Admin only.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost]
        public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var result = await _transactionService.CreateTransactionAsync(request, userId.Value);
            if (!result) return BadRequest("Failed to create transaction.");
            return Ok("Created transaction successfully");
        }

        /// <summary>
        /// Submits an external payment request (Zelle, cheque, bank transfer, etc.).
        /// Creates a <c>Pending</c> transaction awaiting admin approval.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("external")]
        public async Task<IActionResult> SubmitExternalPaymentRequestAsync([FromBody] ExternalPaymentRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var result = await _transactionService.SubmitExternalPaymentRequestAsync(request, userId.Value);
            if (!result) return BadRequest("Failed to submit external payment request.");
            return Ok("Submitted external payment request successfully");
        }

        /// <summary>
        /// Approves a pending external payment request, marking it <c>Confirmed</c>.
        /// Admin and Super Admin only.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveExternalPaymentRequestAsync([FromRoute] Guid id)
        {
            var result = await _transactionService.ApproveExternalPaymentRequestAsync(id);
            if (!result) return BadRequest("Failed to approve external payment request.");
            return Ok("Approved external payment request successfully");
        }

        /// <summary>
        /// Declines a pending external payment request, marking it <c>Declined</c>.
        /// Admin and Super Admin only.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPatch("{id}/decline")]
        public async Task<IActionResult> DeclineExternalPaymentRequestAsync([FromRoute] Guid id)
        {
            var result = await _transactionService.DeclineExternalPaymentRequestAsync(id);
            if (!result) return BadRequest("Failed to decline external payment request.");
            return Ok("Declined external payment request successfully");
        }

        /// <summary>Soft-deletes a transaction. Admin and Super Admin only.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransactionAsync([FromRoute] Guid id)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var result = await _transactionService.SoftDeleteTransactionAsync(id, userId.Value);
            if (!result) return BadRequest("Could not delete transaction");
            return Ok("Deleted transaction successfully");
        }

        /// <summary>
        /// Creates a Stripe PaymentIntent and returns the client secret for frontend completion.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("payment-intent")]
        public async Task<IActionResult> CreateStripePaymentIntentAsync([FromBody] PaymentIntentRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var clientSecret = await _stripeService.CreatePaymentIntentAsync(request, userId.Value);
            if (clientSecret == null) return BadRequest("Failed to create payment intent.");
            return Ok(clientSecret);
        }

        /// <summary>
        /// Receives and processes Stripe webhook events.
        /// Verified via the <c>Stripe-Signature</c> header — does not require a JWT.
        /// </summary>
        [HttpPost("/api/webhooks/stripe")]
        public async Task<IActionResult> HandleStripeWebhookAsync()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(stripeSignature)) return BadRequest("Missing Stripe signature");

            var result = await _stripeService.HandleWebhookEventAsync(json, stripeSignature);
            if (!result) return BadRequest();
            return Ok();
        }

        // ── Rent Schedule Endpoints ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the rent schedule for the calling tenant — handles both PerTenant and SharedUnit billing modes.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/rent-schedule/my")]
        public async Task<IActionResult> GetMyRentScheduleAsync()
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var schedule = await _rentScheduleService.GetMyRentScheduleAsync(userId.Value);
            if (schedule == null) return NotFound("Rent schedule not found.");
            return Ok(schedule);
        }

        /// <summary>
        /// Returns the rent schedule for the specified tenant.
        /// Tenants may only retrieve their own schedule.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/rent-schedule/{tenantId}")]
        public async Task<IActionResult> GetRentScheduleAsync([FromRoute] Guid tenantId)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            // Tenants may only view their own schedule
            if (role == UserRole.Tenant && userId.Value != tenantId)
                return Forbid();

            var schedule = await _rentScheduleService.GetRentScheduleAsync(tenantId);
            if (schedule == null) return NotFound("Rent schedule not found.");
            return Ok(schedule);
        }

        /// <summary>Returns all rent schedules scoped to the caller. Admins see only schedules they created.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpGet("/api/rent-schedules")]
        public async Task<IActionResult> GetAllRentSchedulesAsync()
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");
            var schedules = await _rentScheduleService.GetAllRentSchedulesAsync(userId.Value, role.Value);
            return Ok(schedules);
        }

        /// <summary>Creates a rent schedule for a tenant. Admin and Super Admin only.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("/api/rent-schedule")]
        public async Task<IActionResult> CreateRentScheduleAsync([FromBody] CreateRentScheduleRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            var result = await _rentScheduleService.CreateRentScheduleAsync(request, userId.Value, role.Value);
            if (!result) return BadRequest("Failed to create rent schedule.");
            return Ok("Rent schedule created successfully");
        }

        /// <summary>Updates an existing rent schedule. Admin and Super Admin only.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPatch("/api/rent-schedule/{id}")]
        public async Task<IActionResult> UpdateRentScheduleAsync([FromRoute] Guid id, [FromBody] UpdateRentScheduleRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            // Route id is authoritative — override any value in the request body
            request.RentScheduleId = id;
            var result = await _rentScheduleService.UpdateRentScheduleAsync(request, userId.Value, role.Value);
            if (!result) return BadRequest("Failed to update rent schedule.");
            return Ok("Rent schedule updated successfully");
        }

        /// <summary>Deletes a rent schedule. Admin and Super Admin only.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpDelete("/api/rent-schedule/{id}")]
        public async Task<IActionResult> DeleteRentScheduleAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid user ID or role in token");

            var result = await _rentScheduleService.DeleteRentScheduleAsync(id, userId.Value, role.Value);
            if (!result) return BadRequest("Failed to delete rent schedule.");
            return Ok("Rent schedule deleted successfully");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private (Guid? userId, UserRole? role) GetUserIdAndRole()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
                return (null, null);
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(AppConstants.Claims.UserRole), out UserRole role))
                return (null, null);
            return (userId, role);
        }
    }
}

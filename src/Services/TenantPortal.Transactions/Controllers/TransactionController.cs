using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Interfaces;

namespace TenantPortal.Transactions.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IStripeService _stripeService;
        private readonly IRentScheduleService _rentScheduleService;
        public TransactionController(ITransactionService transactionService, IStripeService stripeService, IRentScheduleService rentScheduleService)
        {
            _transactionService = transactionService;
            _stripeService = stripeService;
            _rentScheduleService = rentScheduleService;
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet]
        public async Task<IActionResult> GetAllTransactionsAsync()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(AppConstants.Claims.UserRole), out UserRole role))
            {
                return BadRequest("Invalid user ID in token");
            } 
            var transactions = await _transactionService.GetAllTransactionsAsync(userId, role);
            if (transactions == null || !transactions.Any())
            {
                return NotFound("No transactions found for the specified user.");
            }
            return Ok(transactions);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionAsync([FromRoute] Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(AppConstants.Claims.UserRole), out UserRole role))
            {
                return BadRequest("Invalid user ID in token");
            }
            var transaction = await _transactionService.GetTransactionAsync(id, userId, role);
            if (transaction == null)
            {
                return NotFound("Transaction not found for the specified user.");
            }
            return Ok(transaction);
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost]
        public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequestDTO request)
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            var result = await _transactionService.CreateTransactionAsync(request, userId);
            if (!result)
            {
                return BadRequest("Failed to create transaction. Please check the request data and try again.");
            }
            return Ok("Created Transaction successfully");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("external")]
        public async Task<IActionResult> SubmitExternalPaymentRequestAsync([FromBody] ExternalPaymentRequestDTO request)
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            var result = await _transactionService.SubmitExternalPaymentRequestAsync(request, userId);
            if (!result)
            {
                return BadRequest("Failed to submit external payment request. Please check the request data and try again.");
            }
            return Ok("Submitted external payment request successfully");
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveExternalPaymentRequestAsync([FromRoute] Guid id)
        {
            var result = await _transactionService.ApproveExternalPaymentRequestAsync(id);
            if (!result)
            {
                return BadRequest("Failed to approve external payment request. Please check the transaction ID and try again.");
            }
            return Ok("Approved external payment request successfully");
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPatch("{id}/decline")]
        public async Task<IActionResult> DeclineExternalPaymentRequestAsync([FromRoute] Guid id)
        {
            var result = await _transactionService.DeclineExternalPaymentRequestAsync(id);
            if (!result)
            {
                return BadRequest("Failed to decline external payment request. Please check the transaction ID and try again.");
            }
            return Ok("Declined external payment request successfully");
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransactionAsync([FromRoute] Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            var result = await _transactionService.SoftDeleteTransactionAsync(id, userId);
            if (!result)
            {
                return BadRequest("Could not delete transaction");
            }
            return Ok("Deleted transaction");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("payment-intent")]
        public async Task<IActionResult> CreateStripePaymentIntent([FromBody] PaymentIntentRequestDTO request)
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return BadRequest("Invalid user ID in token");
            }
            var result = await _stripeService.CreatePaymentIntentAsync(request, userId);
            if (result == null)
            {
                return BadRequest("Failed to create payment intent. Please check the request data and try again.");
            }
            return Ok(result);
        }


        [HttpPost("/api/webhooks/stripe")]
        public async Task<IActionResult> HandleStripeWebhookAsync()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(stripeSignature))
                return BadRequest("Missing Stripe signature");
            var result = await _stripeService.HandleWebhookEventAsync(json, stripeSignature);
            if (!result) return BadRequest();
            return Ok();
        }

    }
}

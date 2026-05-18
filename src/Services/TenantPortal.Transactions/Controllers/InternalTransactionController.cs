using Microsoft.AspNetCore.Mvc;
using TenantPortal.Transactions.Interfaces;

namespace TenantPortal.Transactions.Controllers
{
    /// <summary>
    /// Internal-only endpoints called by other services on the Docker network.
    /// These paths are blocked by the gateway and must never be publicly reachable.
    /// </summary>
    [ApiController]
    [Route("api/transactions/internal")]
    public class InternalTransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public InternalTransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>Seeds demo rent schedule and transaction history for a newly registered Tester account.</summary>
        [HttpPost("seed-tester")]
        public async Task<IActionResult> SeedTesterAsync([FromBody] SeedTesterRequest request)
        {
            if (request.TenantId == Guid.Empty) return BadRequest("TenantId required.");
            var ok = await _transactionService.SeedTesterDataAsync(request.TenantId);
            return ok ? Ok() : StatusCode(500, "Seed failed.");
        }
    }

    public record SeedTesterRequest(Guid TenantId);
}

using Microsoft.AspNetCore.Mvc;
using TenantPortal.Contracts.Interfaces;

namespace TenantPortal.Contracts.Controllers
{
    /// <summary>
    /// Internal-only endpoints called by other services on the Docker network.
    /// These paths are blocked by the gateway and must never be publicly reachable.
    /// </summary>
    [ApiController]
    [Route("api/contracts/internal")]
    public class InternalContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public InternalContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        /// <summary>Seeds demo contract data for a newly registered Tester account.</summary>
        [HttpPost("seed-tester")]
        public async Task<IActionResult> SeedTesterAsync([FromBody] SeedTesterRequest request)
        {
            if (request.TenantId == Guid.Empty) return BadRequest("TenantId required.");
            var ok = await _contractService.SeedTesterDataAsync(request.TenantId);
            return ok ? Ok() : StatusCode(500, "Seed failed.");
        }
    }

    public record SeedTesterRequest(Guid TenantId);
}

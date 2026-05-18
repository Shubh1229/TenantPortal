using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantPortal.Auth.Services;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Auth.Controllers
{
    [ApiController]
    [Route("api/auth/tests")]
    [Authorize(Policy = AppConstants.Policies.RequireSuperAdmin)]
    public class TestController : ControllerBase
    {
        private readonly SystemTestRunner _runner;

        public TestController(SystemTestRunner runner) => _runner = runner;

        [HttpGet("run")]
        public async Task<IActionResult> RunAsync()
        {
            var result = await _runner.RunAllAsync();
            return Ok(result);
        }
    }
}

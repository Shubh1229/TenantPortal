using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Contracts.DTOs;
using TenantPortal.Contracts.Interfaces;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Contracts.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;
        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet]
        public async Task<IActionResult> GetAllContractsAsync()
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null)
            {
                return BadRequest("Invalid user ID or role in token");
            }
            var response = await _contractService.GetAllContractsAsync(userId.Value, role.Value);
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContractAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null)
            {
                return BadRequest("Invalid user ID or role in token");
            }
            var response = await _contractService.GetContractAsync(id, userId.Value, role.Value);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadContractAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null)
            {
                return BadRequest("Invalid user ID or role in token");
            }
            var fileResult = await _contractService.DownloadContractAsync(id, userId.Value, role.Value);
            if (fileResult == null)
            {
                return NotFound();
            }
            return Ok(fileResult);
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadContractAsync([FromForm] UploadContractRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null)
            {
                return BadRequest("Invalid user ID or role in token");
            }
            var response = await _contractService.UploadContractAsync(request, userId.Value, role.Value);
            if (!response)
            {
                return BadRequest("Failed to upload contract. Please check the provided data and try again.");
            }
            return Ok("Uploaded contract successfully");
        }


        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContractAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null)
            {
                return BadRequest("Invalid user ID or role in token");
            }
            var response = await _contractService.DeleteContractAsync(id, userId.Value, role.Value);
            if (!response)
            {
                return BadRequest("Failed to delete contract. Please check the contract ID and try again.");
            }
            return Ok("Deleted contract successfully");
        }

        private (Guid? userId, UserRole? role) GetUserIdAndRole()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return (null, null);
            }
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(AppConstants.Claims.UserRole), out UserRole role))
            {
                return (null, null);
            }
            return (userId, role);
        }
    }
}

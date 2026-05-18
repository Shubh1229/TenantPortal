using Azure.Communication.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Notifications.Controllers
{
    /// <summary>
    /// SuperAdmin-only diagnostics endpoints for verifying external service connectivity.
    /// </summary>
    [ApiController]
    [Route("api/notifications/diagnostics")]
    [Authorize]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ISecretsProvider _secretsProvider;

        public DiagnosticsController(ISecretsProvider secretsProvider)
        {
            _secretsProvider = secretsProvider;
        }

        /// <summary>
        /// Sends a test email via Azure Communication Services to verify the ACS connection string
        /// and sender address are correctly loaded from Key Vault.
        /// </summary>
        [HttpPost("test-email")]
        public async Task<IActionResult> SendTestEmail([FromQuery] string to)
        {
            if (string.IsNullOrWhiteSpace(to))
                return BadRequest("Query parameter 'to' is required.");

            try
            {
                var connectionString = await _secretsProvider.GetSecretAsync(SecretKeys.AzureCommunicationServices);
                var senderAddress = await _secretsProvider.GetSecretAsync(SecretKeys.AzureEmailSenderAddress);

                var emailClient = new EmailClient(connectionString);
                var operation = await emailClient.SendAsync(
                    Azure.WaitUntil.Completed,
                    senderAddress: senderAddress,
                    recipientAddress: to,
                    subject: "TEST",
                    htmlContent: "<p>TEST WORKED CONGRATS!</p>");

                return Ok(new
                {
                    success = true,
                    status = operation.Value.Status.ToString(),
                    sender = senderAddress,
                    recipient = to
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}

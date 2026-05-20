using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.Data;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Controllers
{
    [ApiController]
    [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
    public class PropertyController : ControllerBase
    {
        private readonly TransactionDbContext _context;

        public PropertyController(TransactionDbContext context)
        {
            _context = context;
        }

        // ── Properties ───────────────────────────────────────────────────────────────

        [HttpGet("api/properties")]
        public async Task<IActionResult> GetPropertiesAsync()
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var query = _context.Properties.Where(p => !p.IsDeleted);
            if (role == UserRole.Admin)
                query = query.Where(p => p.AdminId == userId);

            var properties = await query
                .Select(p => new { p.Id, p.Name, p.Address, p.IsActive, p.CreatedAt })
                .ToListAsync();

            return Ok(properties);
        }

        [HttpPost("api/properties")]
        public async Task<IActionResult> CreatePropertyAsync([FromBody] CreatePropertyRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid token");

            var property = new Property
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Address = request.Address,
                AdminId = userId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return Ok(new { property.Id, property.Name, property.Address, property.IsActive, property.CreatedAt });
        }

        [HttpPut("api/properties/{id}")]
        public async Task<IActionResult> UpdatePropertyAsync([FromRoute] Guid id, [FromBody] UpdatePropertyRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (property == null) return NotFound("Property not found.");

            if (role == UserRole.Admin && property.AdminId != userId) return Forbid();

            if (!string.IsNullOrWhiteSpace(request.Name)) property.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Address)) property.Address = request.Address.Trim();
            property.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { property.Id, property.Name, property.Address });
        }

        [HttpDelete("api/properties/{id}")]
        public async Task<IActionResult> DeletePropertyAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (property == null) return NotFound("Property not found.");

            if (role == UserRole.Admin && property.AdminId != userId) return Forbid();

            var hasActiveUnits = await _context.Units.AnyAsync(u => u.PropertyId == id && !u.IsDeleted);
            if (hasActiveUnits)
                return BadRequest(new { error = "Cannot delete a property that still has units. Remove all units first." });

            property.IsDeleted = true;
            property.IsActive = false;
            property.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ── Units ────────────────────────────────────────────────────────────────────

        [HttpGet("api/units")]
        public async Task<IActionResult> GetUnitsAsync([FromQuery] Guid? propertyId = null)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var propQuery = _context.Properties.Where(p => !p.IsDeleted);
            if (role == UserRole.Admin)
                propQuery = propQuery.Where(p => p.AdminId == userId);

            var ownedPropertyIds = await propQuery.Select(p => p.Id).ToListAsync();

            var unitQuery = _context.Units
                .Where(u => !u.IsDeleted && ownedPropertyIds.Contains(u.PropertyId));

            if (propertyId.HasValue)
                unitQuery = unitQuery.Where(u => u.PropertyId == propertyId.Value);

            var units = await unitQuery.ToListAsync();

            // Fetch all active assignments for these units in one query
            var unitIds = units.Select(u => u.Id).ToList();
            var activeAssignments = await _context.TenantUnitAssignments
                .Where(a => unitIds.Contains(a.UnitId) && a.EndDate == null)
                .ToListAsync();

            var assignmentsByUnit = activeAssignments
                .GroupBy(a => a.UnitId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.TenantId).ToList());

            var result = units.Select(u => new
            {
                u.Id,
                u.PropertyId,
                u.UnitNumber,
                u.Bedrooms,
                u.Bathrooms,
                u.SquareFeet,
                u.IsActive,
                u.BillingMode,
                CurrentTenantIds = assignmentsByUnit.TryGetValue(u.Id, out var ids) ? ids : new List<Guid>()
            });

            return Ok(result);
        }

        [HttpPost("api/units")]
        public async Task<IActionResult> CreateUnitAsync([FromBody] CreateUnitRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId && !p.IsDeleted);

            if (property == null) return NotFound("Property not found.");

            if (role == UserRole.Admin && property.AdminId != userId)
                return Forbid();

            // Prevent duplicate unit numbers within the same property
            var duplicate = await _context.Units.AnyAsync(u =>
                u.PropertyId == request.PropertyId &&
                u.UnitNumber == request.UnitNumber &&
                !u.IsDeleted);

            if (duplicate)
                return Conflict(new { error = $"Unit \"{request.UnitNumber}\" already exists on this property." });

            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                PropertyId = request.PropertyId,
                UnitNumber = request.UnitNumber,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFeet = request.SquareFeet,
                BillingMode = request.BillingMode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            return Ok(new { unit.Id, unit.PropertyId, unit.UnitNumber, unit.BillingMode });
        }

        [HttpPut("api/units/{id}")]
        public async Task<IActionResult> UpdateUnitAsync([FromRoute] Guid id, [FromBody] UpdateUnitRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (unit == null) return NotFound("Unit not found.");

            if (role == UserRole.Admin)
            {
                var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted);
                if (property?.AdminId != userId) return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(request.UnitNumber) && request.UnitNumber != unit.UnitNumber)
            {
                var duplicate = await _context.Units.AnyAsync(u =>
                    u.PropertyId == unit.PropertyId &&
                    u.UnitNumber == request.UnitNumber &&
                    u.Id != id &&
                    !u.IsDeleted);

                if (duplicate)
                    return Conflict(new { error = $"Unit \"{request.UnitNumber}\" already exists on this property." });

                unit.UnitNumber = request.UnitNumber.Trim();
            }

            if (request.Bedrooms.HasValue) unit.Bedrooms = request.Bedrooms;
            if (request.Bathrooms.HasValue) unit.Bathrooms = request.Bathrooms;
            if (request.SquareFeet.HasValue) unit.SquareFeet = request.SquareFeet;
            if (request.BillingMode.HasValue) unit.BillingMode = request.BillingMode.Value;
            unit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { unit.Id, unit.UnitNumber, unit.Bedrooms, unit.Bathrooms, unit.SquareFeet, unit.BillingMode });
        }

        [HttpDelete("api/units/{id}")]
        public async Task<IActionResult> DeleteUnitAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (unit == null) return NotFound("Unit not found.");

            if (role == UserRole.Admin)
            {
                var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted);
                if (property?.AdminId != userId) return Forbid();
            }

            var hasActiveTenants = await _context.TenantUnitAssignments
                .AnyAsync(a => a.UnitId == id && a.EndDate == null);

            if (hasActiveTenants)
                return BadRequest(new { error = "Cannot delete a unit with active tenants. End all tenant assignments first." });

            unit.IsDeleted = true;
            unit.IsActive = false;
            unit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ── Tenant Assignment ────────────────────────────────────────────────────────

        [HttpPost("api/units/{id}/assign-tenant")]
        public async Task<IActionResult> AssignTenantAsync([FromRoute] Guid id, [FromBody] AssignTenantRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (unit == null) return NotFound("Unit not found.");

            if (role == UserRole.Admin)
            {
                var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted);
                if (property?.AdminId != userId) return Forbid();
            }

            var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);

            // Reject if this tenant is already active on any unit
            var existingAssignment = await _context.TenantUnitAssignments
                .FirstOrDefaultAsync(a => a.TenantId == request.TenantId && a.EndDate == null);

            if (existingAssignment != null)
            {
                var msg = existingAssignment.UnitId == id
                    ? "This tenant is already assigned to this unit."
                    : "This tenant is currently assigned to another unit. End their current assignment before reassigning them.";
                return BadRequest(new { error = msg });
            }

            // Allow multiple active tenants on the same unit — just add the new assignment
            _context.TenantUnitAssignments.Add(new TenantUnitAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UnitId = id,
                StartDate = startDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok("Tenant assigned successfully.");
        }

        [HttpDelete("api/units/{unitId}/remove-tenant/{tenantId}")]
        public async Task<IActionResult> RemoveTenantAsync([FromRoute] Guid unitId, [FromRoute] Guid tenantId)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            if (role == UserRole.Admin)
            {
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == unitId && !u.IsDeleted);
                var property = unit != null
                    ? await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted)
                    : null;
                if (property?.AdminId != userId) return Forbid();
            }

            var assignment = await _context.TenantUnitAssignments
                .FirstOrDefaultAsync(a => a.UnitId == unitId && a.TenantId == tenantId && a.EndDate == null);

            if (assignment == null) return NotFound("No active assignment found for this tenant on this unit.");

            assignment.EndDate = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Tenant removed from unit.");
        }

        // ── Rent schedule for a specific unit (SharedUnit mode) ──────────────────────

        [HttpGet("api/units/{id}/rent-schedule")]
        public async Task<IActionResult> GetUnitRentScheduleAsync([FromRoute] Guid id)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            if (role == UserRole.Admin)
            {
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
                var property = unit != null
                    ? await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted)
                    : null;
                if (property?.AdminId != userId) return Forbid();
            }

            var schedule = await _context.RentSchedules
                .FirstOrDefaultAsync(s => s.UnitId == id && s.TenantId == null);

            if (schedule == null) return NotFound("No shared rent schedule for this unit.");
            return Ok(schedule);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

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

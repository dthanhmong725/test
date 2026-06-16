using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.Entities;
using DOAN_LAPTRINHWEB.Authorization;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    private readonly IRoleService _roleService;

    public AdminController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("users")]
    [Authorize(Policy = AuthorizationPolicies.RequireModerator)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isBanned = null)
    {
        var result = await _roleService.GetUsersForManagementAsync(page, pageSize, search, role, isBanned);
        return Ok(result);
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromQuery] UserRole newRole)
    {
        var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _roleService.ChangeRoleAsync(adminId, id, newRole);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(int id, [FromQuery] string reason = "Vi phạm nội quy")
    {
        var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _roleService.BanUserAsync(adminId, id, reason);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(int id)
    {
        var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _roleService.UnbanUserAsync(adminId, id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int days = 14)
    {
        var result = await _roleService.GetDashboardStatsAsync(days);
        return Ok(result);
    }

    [HttpGet("security-logs")]
    [Authorize(Policy = AuthorizationPolicies.RequireModerator)]
    public async Task<IActionResult> GetSecurityLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? userId = null)
    {
        var result = await _roleService.GetSecurityLogsAsync(page, pageSize, userId);
        return Ok(result);
    }
}

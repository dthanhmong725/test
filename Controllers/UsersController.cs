using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;
using DOAN_LAPTRINHWEB.Authorization;
using DOAN_LAPTRINHWEB.Data;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly AppDbContext _context;

    public UsersController(IUserService userService, IRoleService roleService, AppDbContext context)
    {
        _userService = userService;
        _roleService = roleService;
        _context = context;
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("profile/{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(string username)
    {
        var result = await _userService.GetPublicProfileAsync(username);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{targetUserId}/follow-status")]
    public async Task<IActionResult> GetFollowStatus(int targetUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var isFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);
        var isFollowedByTarget = await _context.Follows.AnyAsync(f => f.FollowerId == targetUserId && f.FollowingId == currentUserId);

        return Ok(new { success = true, isFollowing, isMutual = (isFollowing && isFollowedByTarget) });
    }

    [HttpPost("{targetUserId}/follow")]
    public async Task<IActionResult> ToggleFollow(int targetUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (currentUserId == targetUserId)
            return BadRequest(new { success = false, message = "Bạn không thể tự theo dõi chính mình." });

        var existingFollow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

        bool dynamicStatus;
        if (existingFollow != null)
        {
            _context.Follows.Remove(existingFollow);
            dynamicStatus = false;
        }
        else
        {
            _context.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = targetUserId });
            dynamicStatus = true;
        }

        await _context.SaveChangesAsync();
        var isMutual = dynamicStatus && await _context.Follows.AnyAsync(f => f.FollowerId == targetUserId && f.FollowingId == currentUserId);

        return Ok(new
        {
            success = true,
            isFollowing = dynamicStatus,
            isMutual,
            message = dynamicStatus ? "Đã theo dõi chuyên gia thành công" : "Đã hủy theo dõi chuyên gia"
        });
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;
using DOAN_LAPTRINHWEB.Authorization;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IBookmarkService _bookmarkService;
    private readonly ISecurityLogService _securityLogService;

    public PostsController(IPostService postService, IBookmarkService bookmarkService, ISecurityLogService securityLogService)
    {
        _postService = postService;
        _bookmarkService = bookmarkService;
        _securityLogService = securityLogService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null, [FromQuery] string? search = null, [FromQuery] string? sortBy = null)
    {
        var result = await _postService.GetAllAsync(page, pageSize, categoryId, search, sortBy);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        }

        var result = await _postService.GetByIdAsync(id, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostWithAttachmentDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        // Gọi Service tạo bài viết (Gợi ý: Nếu chưa sửa IPPostService, bạn hãy lưu tạm link file vào cuối Content 
        // hoặc bổ sung thuộc tính FileUrl vào DTO tùy thuộc vào Service của bạn)
        var result = await _postService.CreateAsync(dto, userId);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }
    // Định nghĩa DTO nhận dữ liệu mới ở cuối file Controllers/PostsController.cs
    public class CreatePostWithAttachmentDto : CreatePostDto
    {
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _postService.UpdateAsync(id, dto, userId);

        if (!result.Success)
            return BadRequest(result);

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        await _securityLogService.LogAsync(userId, SecurityAction.EditPost, ipAddress, userAgent, $"Sửa bài viết ID: {id}", true);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var role = Enum.Parse<UserRole>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User");
        var result = await _postService.DeleteAsync(id, userId, role);

        if (!result.Success)
            return BadRequest(result);

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        await _securityLogService.LogAsync(userId, SecurityAction.DeletePost, ipAddress, userAgent, $"Xóa bài viết ID: {id}", true);

        return Ok(result);
    }

    [HttpPut("{id}/pin")]
    [Authorize(Policy = AuthorizationPolicies.RequireModerator)]
    public async Task<IActionResult> PinPost(int id, [FromQuery] bool isPinned = true)
    {
        var role = Enum.Parse<UserRole>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User");
        var result = await _postService.PinPostAsync(id, isPinned, role);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}/lock")]
    [Authorize(Policy = AuthorizationPolicies.RequireModerator)]
    public async Task<IActionResult> LockPost(int id, [FromQuery] bool isLocked = true)
    {
        var role = Enum.Parse<UserRole>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User");
        var result = await _postService.LockPostAsync(id, isLocked, role);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/vote")]
    public async Task<IActionResult> Vote(int id, [FromBody] VoteDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _postService.VoteAsync(id, userId, dto.IsUpvote);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}/vote")]
    public async Task<IActionResult> RemoveVote(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _postService.RemoveVoteAsync(id, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> IncrementView(int id)
    {
        var result = await _postService.IncrementViewAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/bookmark")]
    public async Task<IActionResult> AddBookmark(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _bookmarkService.AddBookmarkAsync(userId, id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}/bookmark")]
    public async Task<IActionResult> RemoveBookmark(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _bookmarkService.RemoveBookmarkAsync(userId, id);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

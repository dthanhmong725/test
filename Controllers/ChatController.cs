using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DOAN_LAPTRINHWEB.Hubs;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, AppDbContext context, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var count = await _chatService.GetTotalUnreadCountAsync(userId);
        return Ok(new { success = true, unreadCount = count });
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetChatRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.GetUserChatRoomsAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetChatRoom(int roomId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.GetChatRoomAsync(roomId, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.CreateChatRoomAsync(dto, userId);

        if (!result.Success)
            return BadRequest(result);

        return Created($"/api/chat/rooms/{result.Data?.Id}", result);
    }

    [HttpPost("rooms/{roomId}/join")]
    public async Task<IActionResult> JoinChatRoom(int roomId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.JoinChatRoomAsync(roomId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/leave")]
    public async Task<IActionResult> LeaveChatRoom(int roomId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.LeaveChatRoomAsync(roomId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessages(int roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.GetMessagesAsync(roomId, page, pageSize, userId);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> SendMessage(int roomId, [FromBody] SendMessageRequestDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.SendMessageAsync(roomId, userId, dto.Content, dto.AttachmentUrl, dto.AttachmentType, dto.ReplyToId);

        if (!result.Success)
            return BadRequest(result);

        if (result.Data != null)
        {
            await _hubContext.Clients.Group($"room_{roomId}").SendAsync("NewMessage", result.Data);
        }

        return Ok(result);
    }

    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.EditMessageAsync(messageId, userId, dto.Content);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.DeleteMessageAsync(messageId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("messages/{messageId}/reactions")]
    public async Task<IActionResult> ToggleReaction(int messageId, [FromBody] ReactionRequestDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.ToggleReactionAsync(messageId, userId, dto.Emoji);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("messages/{messageId}/pin")]
    public async Task<IActionResult> TogglePinMessage(int messageId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.TogglePinMessageAsync(messageId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("rooms/{roomId}/pinned")]
    public async Task<IActionResult> GetPinnedMessage(int roomId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.GetPinnedMessageAsync(roomId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("rooms/{roomId}/search")]
    public async Task<IActionResult> SearchMessages(int roomId, [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { success = false, message = "Từ khóa tìm kiếm trống" });

        var result = await _chatService.SearchMessagesAsync(roomId, userId, q, page, pageSize);
        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/members")]
    public async Task<IActionResult> AddMember(int roomId, [FromBody] AddMemberRequestDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.AddMemberAsync(roomId, userId, dto.Username);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/read")]
    public async Task<IActionResult> MarkAsRead(int roomId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _chatService.MarkAsReadAsync(roomId, userId);
        return Ok(result);
    }
    // ── TÍNH NĂNG: TỰ ĐỘNG KHỞI TẠO PHÒNG CHAT DIRECT GIỮA 2 USER ──
    // ── THÊM TÍNH NĂNG: TỰ ĐỘNG KHỞI TẠO PHÒNG CHAT DIRECT GIỮA 2 USER ──
    [HttpPost("rooms/initiate/{targetUserId}")]
    public async Task<IActionResult> InitiateDirectChat(int targetUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        if (currentUserId == targetUserId)
            return BadRequest(new { success = false, message = "Bạn không thể tự tạo cuộc trò chuyện với chính mình." });

        // 1. Kiểm tra điều kiện Follow chéo trong Database
        var isFollowingTarget = await _context.Follows
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

        var isTargetFollowingMe = await _context.Follows
            .AnyAsync(f => f.FollowerId == targetUserId && f.FollowingId == currentUserId);

        if (!isFollowingTarget && !isTargetFollowingMe)
        {
            return BadRequest(new { success = false, message = "Cả hai bên phải theo dõi nhau mới có thể nhắn tin!" });
        }
        else if (!isFollowingTarget)
        {
            return BadRequest(new { success = false, message = "Bạn phải theo dõi đối phương trước khi nhắn tin!" });
        }
        else if (!isTargetFollowingMe)
        {
            return BadRequest(new { success = false, message = "Đối phương chưa theo dõi bạn. Cần theo dõi chéo mới có thể nhắn tin!" });
        }

        // 2. Tìm xem trước đó đã có phòng chat Direct nào giữa 2 người này chưa
        var existingRoom = await _context.ChatRooms
            .Include(r => r.Members)
            .Where(r => r.Type == ChatRoomType.Direct && r.IsActive)
            .FirstOrDefaultAsync(r => r.Members.Any(m => m.UserId == currentUserId) && r.Members.Any(m => m.UserId == targetUserId));

        if (existingRoom != null)
        {
            return Ok(new { success = true, chatId = existingRoom.Id, message = "Kết nối đến phòng chat cũ thành công." });
        }

        // 3. Nếu chưa có, tiến hành tạo mới một ChatRoom dạng Direct
        var targetUser = await _context.Users.FindAsync(targetUserId);
        var currentUser = await _context.Users.FindAsync(currentUserId);

        var newRoom = new ChatRoom
        {
            Name = $"{currentUser?.DisplayName ?? currentUser?.Username} & {targetUser?.DisplayName ?? targetUser?.Username}",
            Type = ChatRoomType.Direct,
            CreatedById = currentUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ChatRooms.Add(newRoom);
        await _context.SaveChangesAsync();

        // Thêm 2 User thành thành viên của phòng chat mới tạo
        _context.ChatRoomMembers.Add(new ChatRoomMember { ChatRoomId = newRoom.Id, UserId = currentUserId, JoinedAt = DateTime.UtcNow });
        _context.ChatRoomMembers.Add(new ChatRoomMember { ChatRoomId = newRoom.Id, UserId = targetUserId, JoinedAt = DateTime.UtcNow });

        await _context.SaveChangesAsync();

        return Ok(new { success = true, chatId = newRoom.Id, message = "Thiết lập phòng chat thành công!" });
    }
}

public class SendMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public int? ReplyToId { get; set; }
}

public class EditMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class ReactionRequestDto
{
    public string Emoji { get; set; } = string.Empty;
}

public class AddMemberRequestDto
{
    public string Username { get; set; } = string.Empty;
}

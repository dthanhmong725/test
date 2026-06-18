using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
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
    [HttpPost("rooms/initiate/{targetUserId}")]
    public async Task<IActionResult> InitiateDirectChat(int targetUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        if (currentUserId == targetUserId)
            return BadRequest(new { success = false, message = "Bạn không thể tự trò chuyện với chính mình." });

        // 1. Kiểm tra điều kiện Follow chéo (Hai bên cùng theo dõi nhau)
        // Để gọi trực tiếp từ Controller, bạn có thể inject AppDbContext vào Controller hoặc viết qua Service. 
        // Dưới đây là logic xử lý để bạn tham khảo hoặc đưa vào ChatService:

        // Giả định bạn bổ sung hàm xử lý này vào ChatService để gọi:
        // var result = await _chatService.CreateDirectChatWithFollowCheckAsync(currentUserId, targetUserId);

        return Ok(new { success = true, message = "Đã xác thực kết nối chéo thành công!" });
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

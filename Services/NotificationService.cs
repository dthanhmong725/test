using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateAsync(int recipientId, int actorId, NotificationType type, int? postId = null, int? commentId = null)
    {
        // Không tạo thông báo tự gửi cho chính mình
        if (recipientId == actorId) return;

        // Kiểm tra đã có thông báo tương tự chưa đọc trong vòng 1 giờ qua để tránh spam
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var existing = await _db.Notifications.AnyAsync(n =>
            n.RecipientId == recipientId &&
            n.ActorId == actorId &&
            n.Type == type &&
            n.PostId == postId &&
            n.CommentId == commentId &&
            !n.IsRead &&
            n.CreatedAt >= oneHourAgo);

        if (existing) return;

        var notification = new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId,
            CommentId = commentId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetAllAsync(int userId, int page = 1, int pageSize = 20)
    {
        var notifications = await _db.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(n => n.Actor)
            .Include(n => n.Post)
            .Include(n => n.Comment)
            .ToListAsync();

        var dtos = notifications.Select(n => MapToDto(n)).ToList();

        return new ApiResponse<List<NotificationDto>>
        {
            Success = true,
            Data = dtos
        };
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

        if (notification == null)
            return new ApiResponse<bool> { Success = false, Message = "Không tìm thấy thông báo" };

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return new ApiResponse<bool> { Success = true, Data = true };
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();

        return new ApiResponse<bool> { Success = true, Data = true };
    }

    private static NotificationDto MapToDto(Notification n)
    {
        var actorName = n.Actor?.DisplayName ?? n.Actor?.Username ?? "Ai đó";
        var postTitle = n.Post?.Title;

        string message = n.Type switch
        {
            NotificationType.PostUpvote   => $"{actorName} đã upvote bài viết của bạn",
            NotificationType.PostDownvote => $"{actorName} đã downvote bài viết của bạn",
            NotificationType.Comment      => $"{actorName} đã bình luận vào bài viết của bạn",
            NotificationType.CommentUpvote   => $"{actorName} đã upvote bình luận của bạn",
            NotificationType.CommentDownvote => $"{actorName} đã downvote bình luận của bạn",
            NotificationType.Mention      => $"{actorName} đã nhắc đến bạn trong một bình luận",
            _ => "Bạn có thông báo mới"
        };

        if (postTitle != null && n.Type != NotificationType.Mention)
            message += $": \"{Truncate(postTitle, 50)}\"";

        return new NotificationDto
        {
            Id            = n.Id,
            Type          = n.Type,
            TypeLabel     = n.Type.ToString(),
            Message       = message,
            ActorUsername = n.Actor?.Username,
            ActorAvatar   = n.Actor?.AvatarUrl,
            PostId        = n.PostId,
            PostTitle     = postTitle,
            CommentId     = n.CommentId,
            IsRead        = n.IsRead,
            CreatedAt     = n.CreatedAt,
            TimeAgo       = GetTimeAgo(n.CreatedAt)
        };
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private static string GetTimeAgo(DateTime dt)
    {
        var seconds = (int)(DateTime.UtcNow - dt).TotalSeconds;
        if (seconds < 60)   return "vừa xong";
        if (seconds < 3600) return $"{seconds / 60} phút trước";
        if (seconds < 86400) return $"{seconds / 3600} giờ trước";
        if (seconds < 604800) return $"{seconds / 86400} ngày trước";
        return dt.ToLocalTime().ToString("dd/MM/yyyy");
    }
}

using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<PublicProfileDto>> GetPublicProfileAsync(string username, int? currentUserId = null);
    Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<PaginatedResponse<PublicProfileDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, int? currentUserId = null);
    Task<ApiResponse<ReputationDto>> GetReputationAsync(int userId);
    Task<ApiResponse<List<LeaderboardEntryDto>>> GetLeaderboardAsync(int page, int pageSize);
}

public interface ICategoryService
{
    Task<ApiResponse<List<CategoryDto>>> GetAllAsync();
    Task<ApiResponse<CategoryDto>> GetBySlugAsync(string slug);
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto, int createdById);
    Task<ApiResponse<CategoryDto>> UpdateAsync(int id, CreateCategoryDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

public interface IPostService
{
    Task<PaginatedResponse<PostListDto>> GetAllAsync(int page, int pageSize, int? categoryId, string? search, string? sortBy);
    Task<ApiResponse<PostDto>> GetByIdAsync(int id, int? userId);
    Task<ApiResponse<PostDto>> CreateAsync(CreatePostDto dto, int authorId);
    Task<ApiResponse<PostDto>> UpdateAsync(int id, UpdatePostDto dto, int userId);
    Task<ApiResponse<bool>> DeleteAsync(int id, int userId, UserRole userRole);
    Task<ApiResponse<bool>> PinPostAsync(int id, bool isPinned, UserRole userRole);
    Task<ApiResponse<bool>> LockPostAsync(int id, bool isLocked, UserRole userRole);
    Task<ApiResponse<bool>> VoteAsync(int postId, int userId, bool isUpvote);
    Task<ApiResponse<bool>> RemoveVoteAsync(int postId, int userId);
    Task<ApiResponse<bool>> IncrementViewAsync(int id);
}

public interface ICommentService
{
    Task<PaginatedResponse<CommentDto>> GetByPostAsync(int postId, int page, int pageSize, int? userId);
    Task<ApiResponse<CommentDto>> CreateAsync(int postId, CreateCommentDto dto, int authorId);
    Task<ApiResponse<CommentDto>> UpdateAsync(int commentId, UpdateCommentDto dto, int userId);
    Task<ApiResponse<bool>> DeleteAsync(int commentId, int userId, UserRole userRole);
    Task<ApiResponse<bool>> VoteAsync(int commentId, int userId, bool isUpvote);
    Task<ApiResponse<bool>> RemoveVoteAsync(int commentId, int userId);
}

public interface IBookmarkService
{
    Task<PaginatedResponse<BookmarkDto>> GetUserBookmarksAsync(int userId, int page, int pageSize);
    Task<ApiResponse<bool>> AddBookmarkAsync(int userId, int postId);
    Task<ApiResponse<bool>> RemoveBookmarkAsync(int userId, int postId);
    Task<ApiResponse<bool>> IsBookmarkedAsync(int userId, int postId);
    Task<ApiResponse<bool>> ToggleBookmarkAsync(int userId, int postId);
}

public interface IBadgeService
{
    Task<ApiResponse<List<BadgeDto>>> GetAllBadgesAsync(int? userId);
    Task<ApiResponse<List<BadgeDto>>> GetUserBadgesAsync(int userId);
    Task CheckAndAwardBadgesAsync(int userId);
}

public interface IActivityLogService
{
    Task LogAsync(ActivityType type, int userId, string? ipAddress, string? userAgent, string? details, bool isSuccess = true);
    Task<PaginatedResponse<ActivityLogDto>> GetUserActivityAsync(int userId, int page, int pageSize);
    Task<PaginatedResponse<ActivityLogDto>> GetAllActivityAsync(int page, int pageSize, int? userId, ActivityType? type);
}

public interface IRateLimitService
{
    Task<bool> IsRateLimitedAsync(string userId, string endpoint);
    Task IncrementAsync(string userId, string endpoint);
}

public interface IChatService
{
    Task<PaginatedResponse<ChatRoomDto>> GetUserChatRoomsAsync(int userId, int page, int pageSize);
    Task<ApiResponse<ChatRoomDto>> GetChatRoomAsync(int roomId, int userId);
    Task<ApiResponse<ChatRoomDto>> CreateChatRoomAsync(CreateChatRoomDto dto, int creatorId);
    Task<ApiResponse<bool>> JoinChatRoomAsync(int roomId, int userId);
    Task<ApiResponse<bool>> LeaveChatRoomAsync(int roomId, int userId);
    Task<PaginatedResponse<ChatMessageDto>> GetMessagesAsync(int roomId, int page, int pageSize, int userId);
    Task<ApiResponse<ChatMessageDto>> SendMessageAsync(int roomId, int userId, string content, string? attachmentUrl, string? attachmentType, int? replyToId);
    Task<ApiResponse<bool>> EditMessageAsync(int messageId, int userId, string newContent);
    Task<ApiResponse<bool>> DeleteMessageAsync(int messageId, int userId);
    Task<ApiResponse<bool>> ToggleReactionAsync(int messageId, int userId, string emoji);
    Task<ApiResponse<bool>> TogglePinMessageAsync(int messageId, int userId);
    Task<ApiResponse<ChatMessageDto?>> GetPinnedMessageAsync(int roomId, int userId);
    Task<PaginatedResponse<ChatMessageDto>> SearchMessagesAsync(int roomId, int userId, string term, int page, int pageSize);
    Task<ApiResponse<bool>> AddMemberAsync(int roomId, int adderUserId, string username);
    Task<ApiResponse<bool>> MarkAsReadAsync(int roomId, int userId);
    Task<ApiResponse<bool>> UpdateLastReadAsync(int roomId, int userId);
    Task<int> GetTotalUnreadCountAsync(int userId);
}

public interface IPasswordStrengthService
{
    PasswordStrengthDto CheckStrength(string password);
}

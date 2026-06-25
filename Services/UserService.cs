using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<UserDto>.ErrorResponse("User not found");

        return ApiResponse<UserDto>.SuccessResponse(MapToUserDto(user));
    }

    public async Task<ApiResponse<PublicProfileDto>> GetPublicProfileAsync(string username, int? currentUserId = null)
    {
        var user = await _context.Users
            .Include(u => u.Badges).ThenInclude(ub => ub.Badge)
            .Include(u => u.Posts)
            .Include(u => u.Comments)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return ApiResponse<PublicProfileDto>.ErrorResponse("User not found");

        var isFollowing = false;
        if (currentUserId.HasValue)
        {
            isFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == user.Id);
        }

        return ApiResponse<PublicProfileDto>.SuccessResponse(new PublicProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CoverPhotoUrl = user.CoverPhotoUrl,
            Bio = user.Bio,
            Role = user.Role.ToString(),
            Rank = user.Rank.ToString(),
            ReputationPoints = user.ReputationPoints,
            PostCount = user.Posts.Count(p => !p.IsDeleted),
            CommentCount = user.Comments.Count(c => !c.IsDeleted),
            CreatedAt = user.CreatedAt,
            Badges = user.Badges.Select(ub => new BadgeDto
            {
                Id = ub.Badge.Id,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                Icon = ub.Badge.Icon,
                Color = ub.Badge.Color,
                Type = ub.Badge.Type,
                ReputationRequired = ub.Badge.ReputationRequired,
                IsEarned = true,
                EarnedAt = ub.EarnedAt
            }).ToList(),
            IsFollowing = isFollowing
        });
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse<UserDto>.ErrorResponse("User not found");

        if (!string.IsNullOrEmpty(dto.DisplayName))
            user.DisplayName = dto.DisplayName;

        if (dto.Bio != null)
            user.Bio = dto.Bio;

        if (dto.AvatarUrl != null)
            user.AvatarUrl = dto.AvatarUrl;

        if (dto.CoverPhotoUrl != null)
            user.CoverPhotoUrl = dto.CoverPhotoUrl;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<UserDto>.SuccessResponse(MapToUserDto(user), "Profile updated");
    }

    public async Task<PaginatedResponse<PublicProfileDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, int? currentUserId = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) || (u.DisplayName != null && u.DisplayName.Contains(search)));

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var userRole))
            query = query.Where(u => u.Role == userRole);

        if (currentUserId.HasValue)
            query = query.Where(u => u.Id != currentUserId.Value);

        var totalItems = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.ReputationPoints)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.Badges).ThenInclude(ub => ub.Badge)
            .Select(u => new PublicProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                Role = u.Role.ToString(),
                Rank = u.Rank.ToString(),
                ReputationPoints = u.ReputationPoints,
                PostCount = u.Posts.Count(p => !p.IsDeleted),
                CommentCount = u.Comments.Count(c => !c.IsDeleted),
                CreatedAt = u.CreatedAt,
                Badges = u.Badges.Select(ub => new BadgeDto
                {
                    Id = ub.Badge.Id,
                    Name = ub.Badge.Name,
                    Description = ub.Badge.Description,
                    Icon = ub.Badge.Icon,
                    Color = ub.Badge.Color,
                    Type = ub.Badge.Type,
                    ReputationRequired = ub.Badge.ReputationRequired,
                    IsEarned = true,
                    EarnedAt = ub.EarnedAt
                }).ToList()
            })
            .ToListAsync();

        if (currentUserId.HasValue && users.Count > 0)
        {
            var userIds = users.Select(u => u.Id).ToList();
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == currentUserId.Value && userIds.Contains(f.FollowingId))
                .Select(f => f.FollowingId)
                .ToListAsync();

            foreach (var u in users)
                u.IsFollowing = followingIds.Contains(u.Id);
        }

        return new PaginatedResponse<PublicProfileDto>
        {
            Success = true,
            Data = users,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ApiResponse<ReputationDto>> GetReputationAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Posts).ThenInclude(p => p.Votes)
            .Include(u => u.Comments)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return ApiResponse<ReputationDto>.ErrorResponse("User not found");

        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return ApiResponse<ReputationDto>.SuccessResponse(new ReputationDto
        {
            UserId = user.Id,
            Username = user.Username,
            TotalPoints = user.ReputationPoints,
            Rank = user.Rank.ToString(),
            PostsThisMonth = user.Posts.Count(p => p.CreatedAt >= thisMonth && !p.IsDeleted),
            CommentsThisMonth = user.Comments.Count(c => c.CreatedAt >= thisMonth && !c.IsDeleted),
            UpvotesReceived = user.Posts.Where(p => !p.IsDeleted).Sum(p => p.UpvoteCount) +
                             user.Comments.Where(c => !c.IsDeleted).Sum(c => c.UpvoteCount),
            DownvotesReceived = user.Posts.Where(p => !p.IsDeleted).Sum(p => p.DownvoteCount) +
                                user.Comments.Where(c => !c.IsDeleted).Sum(c => c.DownvoteCount)
        });
    }

    public async Task<ApiResponse<List<LeaderboardEntryDto>>> GetLeaderboardAsync(int page, int pageSize)
    {
        var users = await _context.Users
            .OrderByDescending(u => u.ReputationPoints)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.Posts)
            .Include(u => u.Comments)
            .ToListAsync();

        var startRank = (page - 1) * pageSize + 1;
        var entries = users.Select((u, index) => new LeaderboardEntryDto
        {
            Rank = startRank + index,
            User = MapToUserDto(u),
            ReputationPoints = u.ReputationPoints,
            PostCount = u.Posts.Count(p => !p.IsDeleted),
            CommentCount = u.Comments.Count(c => !c.IsDeleted)
        }).ToList();

        return ApiResponse<List<LeaderboardEntryDto>>.SuccessResponse(entries);
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        CoverPhotoUrl = user.CoverPhotoUrl,
        Bio = user.Bio,
        Role = user.Role.ToString(),
        Rank = user.Rank.ToString(),
        ReputationPoints = user.ReputationPoints,
        CreatedAt = user.CreatedAt
    };
}

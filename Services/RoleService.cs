using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.Entities;
using DOAN_LAPTRINHWEB.Models.DTOs;

namespace DOAN_LAPTRINHWEB.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;
    private readonly ISecurityLogService _securityLogService;

    public RoleService(AppDbContext context, ISecurityLogService securityLogService)
    {
        _context = context;
        _securityLogService = securityLogService;
    }

    public async Task<ApiResponse<bool>> ChangeRoleAsync(int adminId, int targetUserId, UserRole newRole)
    {
        var admin = await _context.Users.FindAsync(adminId);
        if (admin == null || admin.Role != UserRole.Admin)
            return ApiResponse<bool>.ErrorResponse("Chỉ quản trị viên mới có quyền thay đổi vai trò.");

        if (adminId == targetUserId)
            return ApiResponse<bool>.ErrorResponse("Bạn không thể thay đổi vai trò của chính mình.");

        var user = await _context.Users.FindAsync(targetUserId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin)
            return ApiResponse<bool>.ErrorResponse("Không thể thay đổi vai trò của quản trị viên khác.");

        var oldRole = user.Role;
        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogSecurityEventAsync(adminId, SecurityAction.RoleChange,
            $"Thay đổi vai trò người dùng {user.Username} từ {oldRole} thành {newRole}");

        return ApiResponse<bool>.SuccessResponse(true, $"Đã thay đổi vai trò thành {newRole}");
    }

    public async Task<ApiResponse<bool>> BanUserAsync(int adminId, int targetUserId, string reason)
    {
        var admin = await _context.Users.FindAsync(adminId);
        if (admin == null || admin.Role != UserRole.Admin)
            return ApiResponse<bool>.ErrorResponse("Chỉ quản trị viên mới có quyền khóa tài khoản.");

        if (adminId == targetUserId)
            return ApiResponse<bool>.ErrorResponse("Bạn không thể tự khóa tài khoản của mình.");

        var user = await _context.Users.FindAsync(targetUserId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin)
            return ApiResponse<bool>.ErrorResponse("Không thể khóa tài khoản quản trị viên.");

        if (user.IsBanned)
            return ApiResponse<bool>.ErrorResponse("Tài khoản đã bị khóa trước đó.");

        user.IsBanned = true;
        user.BanReason = reason;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogSecurityEventAsync(adminId, SecurityAction.BanUser,
            $"Khóa tài khoản {user.Username}: {reason}");

        return ApiResponse<bool>.SuccessResponse(true, "Đã khóa tài khoản");
    }

    public async Task<ApiResponse<bool>> UnbanUserAsync(int adminId, int targetUserId)
    {
        var admin = await _context.Users.FindAsync(adminId);
        if (admin == null || admin.Role != UserRole.Admin)
            return ApiResponse<bool>.ErrorResponse("Chỉ quản trị viên mới có quyền mở khóa tài khoản.");

        var user = await _context.Users.FindAsync(targetUserId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng.");

        if (!user.IsBanned)
            return ApiResponse<bool>.ErrorResponse("Tài khoản không bị khóa.");

        user.IsBanned = false;
        user.BanReason = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogSecurityEventAsync(adminId, SecurityAction.UnbanUser,
            $"Mở khóa tài khoản {user.Username}");

        return ApiResponse<bool>.SuccessResponse(true, "Đã mở khóa tài khoản");
    }

    public async Task<PaginatedResponse<UserManagementDto>> GetUsersForManagementAsync(
        int page, int pageSize, string? search, string? role, bool? isBanned)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Username.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.DisplayName != null && u.DisplayName.Contains(search)));

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var userRole))
            query = query.Where(u => u.Role == userRole);

        if (isBanned.HasValue)
            query = query.Where(u => u.IsBanned == isBanned.Value);

        var totalItems = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.Role)
            .ThenByDescending(u => u.ReputationPoints)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserManagementDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role.ToString(),
                Rank = u.Rank.ToString(),
                ReputationPoints = u.ReputationPoints,
                IsBanned = u.IsBanned,
                BanReason = u.BanReason,
                IsEmailVerified = u.IsEmailVerified,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<UserManagementDto>
        {
            Success = true,
            Data = users,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ApiResponse<List<Models.DTOs.SecurityLogDto>>> GetSecurityLogsAsync(int page, int pageSize, int? userId)
    {
        var query = _context.SecurityLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(s => s.UserId == userId.Value);

        var logs = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.User)
            .Select(s => new Models.DTOs.SecurityLogDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User != null ? s.User.Username : null,
                IpAddress = s.IpAddress,
                Action = s.Action.ToString(),
                Description = s.Description,
                IsSuccess = s.IsSuccess,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<SecurityLogDto>>.SuccessResponse(logs);
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(int days = 14)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var rangeStart = todayStart.AddDays(-(days - 1));

        var totalUsers = await _context.Users.CountAsync();
        var totalPosts = await _context.Posts.CountAsync(p => !p.IsDeleted);
        var totalComments = await _context.Comments.CountAsync(c => !c.IsDeleted);
        var totalCategories = await _context.Categories.CountAsync();
        var bannedUsers = await _context.Users.CountAsync(u => u.IsBanned);

        var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= todayStart);
        var newPostsToday = await _context.Posts.CountAsync(p => !p.IsDeleted && p.CreatedAt >= todayStart);
        var newCommentsToday = await _context.Comments.CountAsync(c => !c.IsDeleted && c.CreatedAt >= todayStart);

        var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
        var moderatorCount = await _context.Users.CountAsync(u => u.Role == UserRole.Moderator);
        var regularUserCount = totalUsers - adminCount - moderatorCount;

        // Dữ liệu thô trong khoảng ngày cần thống kê, gom nhóm theo ngày tại phía client (C#)
        // để tránh phụ thuộc vào hàm SQL cụ thể (SQLite/SQL Server khác nhau khi group theo Date)
        var rawUsers = await _context.Users
            .Where(u => u.CreatedAt >= rangeStart)
            .Select(u => u.CreatedAt)
            .ToListAsync();

        var rawPosts = await _context.Posts
            .Where(p => !p.IsDeleted && p.CreatedAt >= rangeStart)
            .Select(p => p.CreatedAt)
            .ToListAsync();

        var rawComments = await _context.Comments
            .Where(c => !c.IsDeleted && c.CreatedAt >= rangeStart)
            .Select(c => c.CreatedAt)
            .ToListAsync();

        var userGrowth = BuildDailyCounts(rawUsers, rangeStart, days);
        var postGrowth = BuildDailyCounts(rawPosts, rangeStart, days);
        var commentGrowth = BuildDailyCounts(rawComments, rangeStart, days);

        var topCategories = await _context.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryStatDto
            {
                Name = c.Name,
                Color = c.Color,
                PostCount = c.Posts.Count(p => !p.IsDeleted)
            })
            .OrderByDescending(c => c.PostCount)
            .Take(5)
            .ToListAsync();

        var topUsers = await _context.Users
            .OrderByDescending(u => u.ReputationPoints)
            .Take(5)
            .Select(u => new TopUserStatDto
            {
                Id = u.Id,
                Username = u.Username,
                AvatarUrl = u.AvatarUrl,
                ReputationPoints = u.ReputationPoints,
                Role = u.Role.ToString()
            })
            .ToListAsync();

        var stats = new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            TotalPosts = totalPosts,
            TotalComments = totalComments,
            TotalCategories = totalCategories,
            BannedUsers = bannedUsers,
            NewUsersToday = newUsersToday,
            NewPostsToday = newPostsToday,
            NewCommentsToday = newCommentsToday,
            UserGrowth = userGrowth,
            PostGrowth = postGrowth,
            CommentGrowth = commentGrowth,
            AdminCount = adminCount,
            ModeratorCount = moderatorCount,
            RegularUserCount = regularUserCount,
            TopCategories = topCategories,
            TopUsers = topUsers
        };

        return ApiResponse<DashboardStatsDto>.SuccessResponse(stats);
    }

    private static List<DailyCountDto> BuildDailyCounts(List<DateTime> timestamps, DateTime rangeStart, int days)
    {
        var counts = timestamps
            .GroupBy(t => t.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<DailyCountDto>();
        for (int i = 0; i < days; i++)
        {
            var day = rangeStart.AddDays(i);
            result.Add(new DailyCountDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = counts.TryGetValue(day, out var c) ? c : 0
            });
        }

        return result;
    }

    private async Task LogSecurityEventAsync(int userId, SecurityAction action, string details, bool isSuccess = true)
    {
        await _securityLogService.LogAsync(userId, action, "system", null, details, isSuccess);
    }
}

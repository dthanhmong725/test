using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Interfaces;

public interface IRoleService
{
    Task<ApiResponse<bool>> ChangeRoleAsync(int adminId, int targetUserId, UserRole newRole);
    Task<ApiResponse<bool>> BanUserAsync(int adminId, int targetUserId, string reason);
    Task<ApiResponse<bool>> UnbanUserAsync(int adminId, int targetUserId);
    Task<PaginatedResponse<UserManagementDto>> GetUsersForManagementAsync(int page, int pageSize, string? search, string? role, bool? isBanned);
    Task<ApiResponse<List<SecurityLogDto>>> GetSecurityLogsAsync(int page, int pageSize, int? userId);
    Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(int days = 14);
}

using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.Entities;
using DOAN_LAPTRINHWEB.Models.DTOs;

namespace DOAN_LAPTRINHWEB.Services;

public class SecurityLogService : ISecurityLogService
{
    private readonly AppDbContext _context;

    public SecurityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, SecurityAction action, string? ipAddress, string? userAgent, string? description, bool isSuccess = true)
    {
        var log = new SecurityLog
        {
            UserId = userId,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = description,
            IsSuccess = isSuccess,
            CreatedAt = DateTime.UtcNow
        };

        _context.SecurityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<SecurityLogDto>> GetUserLogsAsync(int userId, int page, int pageSize, DateTime? dateFrom, DateTime? dateTo, SecurityAction? action, bool? isSuccess)
    {
        var query = _context.SecurityLogs
            .Include(s => s.User)
            .Where(s => s.UserId == userId);

        if (dateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= dateTo.Value);
        if (action.HasValue)
            query = query.Where(s => s.Action == action.Value);
        if (isSuccess.HasValue)
            query = query.Where(s => s.IsSuccess == isSuccess.Value);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SecurityLogDto
            {
                Id = s.Id,
                Action = s.Action.ToString(),
                IpAddress = s.IpAddress ?? string.Empty,
                UserAgent = s.UserAgent,
                Description = s.Description,
                IsSuccess = s.IsSuccess,
                CreatedAt = s.CreatedAt,
                UserId = s.UserId,
                Username = s.User.Username
            })
            .ToListAsync();

        return new PaginatedResponse<SecurityLogDto>
        {
            Success = true,
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<PaginatedResponse<SecurityLogDto>> GetAllLogsAsync(int page, int pageSize, int? userId, DateTime? dateFrom, DateTime? dateTo, SecurityAction? action, bool? isSuccess)
    {
        var query = _context.SecurityLogs
            .Include(s => s.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(s => s.UserId == userId.Value);
        if (dateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= dateTo.Value);
        if (action.HasValue)
            query = query.Where(s => s.Action == action.Value);
        if (isSuccess.HasValue)
            query = query.Where(s => s.IsSuccess == isSuccess.Value);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SecurityLogDto
            {
                Id = s.Id,
                Action = s.Action.ToString(),
                IpAddress = s.IpAddress ?? string.Empty,
                UserAgent = s.UserAgent,
                Description = s.Description,
                IsSuccess = s.IsSuccess,
                CreatedAt = s.CreatedAt,
                UserId = s.UserId,
                Username = s.User.Username
            })
            .ToListAsync();

        return new PaginatedResponse<SecurityLogDto>
        {
            Success = true,
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}

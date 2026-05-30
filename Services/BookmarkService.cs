using Microsoft.EntityFrameworkCore;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Services;

public class BookmarkService : IBookmarkService
{
    private readonly AppDbContext _context;

    public BookmarkService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<BookmarkDto>> GetUserBookmarksAsync(int userId, int page, int pageSize)
    {
        var query = _context.Bookmarks
            .Include(b => b.Post)
            .ThenInclude(p => p.Author)
            .Include(b => b.Post)
            .ThenInclude(p => p.Category)
            .Include(b => b.Post)
            .ThenInclude(p => p.PostTags)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt);

        var totalItems = await query.CountAsync();
        var bookmarks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = bookmarks.Select(b => new BookmarkDto
        {
            Id = b.Id,
            PostId = b.PostId,
            CreatedAt = b.CreatedAt,
            Post = new PostListDto
            {
                Id = b.Post.Id,
                Title = b.Post.Title,
                Slug = b.Post.Slug,
                IsPinned = b.Post.IsPinned,
                IsLocked = b.Post.IsLocked,
                ViewCount = b.Post.ViewCount,
                UpvoteCount = b.Post.UpvoteCount,
                DownvoteCount = b.Post.DownvoteCount,
                CommentCount = b.Post.CommentCount,
                CreatedAt = b.Post.CreatedAt,
                AuthorUsername = b.Post.Author.Username,
                AuthorAvatar = b.Post.Author.AvatarUrl,
                AuthorRole = b.Post.Author.Role.ToString(),
                CategoryName = b.Post.Category!.Name,
                CategorySlug = b.Post.Category!.Slug,
                Tags = b.Post.PostTags.Select(pt => pt.Name).ToList()
            }
        }).ToList();

        return new PaginatedResponse<BookmarkDto>
        {
            Success = true,
            Data = result,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ApiResponse<bool>> AddBookmarkAsync(int userId, int postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        var existing = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

        if (existing != null)
            return ApiResponse<bool>.SuccessResponse(true, "Already bookmarked");

        _context.Bookmarks.Add(new Bookmark
        {
            UserId = userId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Bookmark added");
    }

    public async Task<ApiResponse<bool>> RemoveBookmarkAsync(int userId, int postId)
    {
        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

        if (bookmark == null)
            return ApiResponse<bool>.ErrorResponse("Bookmark not found");

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Bookmark removed");
    }

    public async Task<ApiResponse<bool>> IsBookmarkedAsync(int userId, int postId)
    {
        var isBookmarked = await _context.Bookmarks
            .AnyAsync(b => b.UserId == userId && b.PostId == postId);

        return ApiResponse<bool>.SuccessResponse(isBookmarked);
    }

    public async Task<ApiResponse<bool>> ToggleBookmarkAsync(int userId, int postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Bài viết không tồn tại");

        var existing = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

        if (existing != null)
        {
            _context.Bookmarks.Remove(existing);
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(false, "Đã bỏ lưu bài viết");
        }
        else
        {
            _context.Bookmarks.Add(new Bookmark
            {
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Đã lưu bài viết");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Ganss.Xss;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _context;
    private readonly IActivityLogService _activityLog;
    private readonly IRoleService _roleService;
    private readonly IBadgeService _badgeService;
    private readonly ISecurityLogService _securityLogService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly INotificationService _notificationService;
    private readonly IReputationService _reputationService;

    public PostService(
        AppDbContext context,
        IActivityLogService activityLog,
        IBadgeService badgeService,
        IRoleService roleService,
        ISecurityLogService securityLogService,
        INotificationService notificationService,
        IReputationService reputationService)
    {
        _context = context;
        _activityLog = activityLog;
        _badgeService = badgeService;
        _htmlSanitizer = new HtmlSanitizer();
        _roleService = roleService;
        _securityLogService = securityLogService;
        _notificationService = notificationService;
        _reputationService = reputationService;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = Regex.Replace(html, "<.*?>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static bool IsValidBodyLength(string content, int minChars = 50)
    {
        return StripHtml(content).Length >= minChars;
    }

    public async Task<PaginatedResponse<PostListDto>> GetAllAsync(int page, int pageSize, int? categoryId, string? search, string? sortBy)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));

        query = sortBy?.ToLower() switch
        {
            "views" => query.OrderByDescending(p => p.ViewCount),
            "comments" => query.OrderByDescending(p => p.CommentCount),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.LastActivityAt ?? p.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPinned = p.IsPinned,
                IsLocked = p.IsLocked,
                ViewCount = p.ViewCount,
                UpvoteCount = p.UpvoteCount,
                DownvoteCount = p.DownvoteCount,
                CommentCount = p.CommentCount,
                CreatedAt = p.CreatedAt,
                AuthorUsername = p.Author.Username,
                AuthorAvatar = p.Author.AvatarUrl,
                AuthorRole = p.Author.Role.ToString(),
                AuthorReputation = p.Author.ReputationPoints,
                CategoryName = p.Category!.Name,
                CategorySlug = p.Category!.Slug,
                Tags = p.PostTags.Select(pt => pt.Name).ToList()
            })
            .ToListAsync();

        return new PaginatedResponse<PostListDto>
        {
            Success = true,
            Data = posts,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ApiResponse<PostDto>> GetByIdAsync(int id, int? userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
            .Include(p => p.Attachments)
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (post == null)
            return ApiResponse<PostDto>.ErrorResponse("Post not found");

        // Increment view count
        post.ViewCount++;

        // Check if user bookmarked
        bool isBookmarked = false;
        int? userVote = null;

        if (userId.HasValue)
        {
            isBookmarked = await _context.Bookmarks.AnyAsync(b => b.UserId == userId.Value && b.PostId == id);
            var vote = post.Votes.FirstOrDefault(v => v.UserId == userId.Value);
            if (vote != null)
                userVote = vote.IsUpvote ? 1 : -1;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<PostDto>.SuccessResponse(new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = _htmlSanitizer.Sanitize(post.Content),
            Slug = post.Slug,
            IsPinned = post.IsPinned,
            IsLocked = post.IsLocked,
            ViewCount = post.ViewCount,
            UpvoteCount = post.UpvoteCount,
            DownvoteCount = post.DownvoteCount,
            CommentCount = post.CommentCount,
            LastActivityAt = post.LastActivityAt,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Author = new UserDto
            {
                Id = post.Author.Id,
                Username = post.Author.Username,
                Email = post.Author.Email,
                DisplayName = post.Author.DisplayName,
                AvatarUrl = post.Author.AvatarUrl,
                Bio = post.Author.Bio,
                Role = post.Author.Role.ToString(),
                Rank = post.Author.Rank.ToString(),
                ReputationPoints = post.Author.ReputationPoints,
                CreatedAt = post.Author.CreatedAt
            },
            Category = new CategoryDto
            {
                Id = post.Category!.Id,
                Name = post.Category!.Name,
                Description = post.Category!.Description,
                Icon = post.Category!.Icon,
                Color = post.Category!.Color,
                Slug = post.Category!.Slug,
                DisplayOrder = post.Category!.DisplayOrder,
                IsActive = post.Category!.IsActive,
                PostCount = post.Category!.PostCount
            },
            Tags = post.PostTags.Select(pt => pt.Name).ToList(),
            Attachments = post.Attachments.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                ContentType = a.ContentType,
                FileSize = a.FileSize
            }).ToList(),
            UserVote = userVote,
            IsBookmarked = isBookmarked
        });
    }

    public async Task<ApiResponse<PostDto>> CreateAsync(CreatePostDto dto, int authorId)
    {
        if (!IsValidBodyLength(dto.Content, 50))
            return ApiResponse<PostDto>.ErrorResponse("Nội dung phải có ít nhất 50 ký tự sau khi loại bỏ thẻ HTML");

        var author = await _context.Users.FindAsync(authorId);
        if (author == null)
            return ApiResponse<PostDto>.ErrorResponse("User not found");

        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null)
            return ApiResponse<PostDto>.ErrorResponse("Danh mục không tồn tại");

        var sanitizedContent = _htmlSanitizer.Sanitize(dto.Content);

        var post = new Post
        {
            Title = dto.Title,
            Content = sanitizedContent,
            Slug = GenerateSlug(dto.Title),
            AuthorId = authorId,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Add attachments
        if (dto.Attachments != null && dto.Attachments.Any())
        {
            foreach (var att in dto.Attachments)
            {
                _context.PostAttachments.Add(new PostAttachment
                {
                    FileName = att.FileName,
                    FileUrl = att.FileUrl ?? "",
                    FileSize = att.FileSize,
                    ContentType = att.ContentType ?? "application/octet-stream",
                    PostId = post.Id,
                    UploadedById = authorId,
                    UploadedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        // Add tags
        if (dto.Tags != null && dto.Tags.Any())
        {
            foreach (var tag in dto.Tags.Take(5))
            {
                _context.PostTags.Add(new PostTag { Name = tag.ToLower(), PostId = post.Id });
            }
        }

        // Update category post count
        category.PostCount++;
        post.Category = category;
        post.Author = author;

        await _context.SaveChangesAsync();
        await _activityLog.LogAsync(ActivityType.PostCreate, authorId, null, null, $"Post created: {post.Title}");

        // ===== TÍCH HỢP REPUTATION: Cộng điểm khi đăng bài =====
        await _reputationService.ApplyChangeAsync(
            userId: authorId,
            action: ReputationAction.PostCreated,
            actorId: authorId,
            postId: post.Id,
            description: $"Đã đăng bài viết: {post.Title}");

        // Check for badges
        await _badgeService.CheckAndAwardBadgesAsync(authorId);

        return ApiResponse<PostDto>.SuccessResponse(new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Slug = post.Slug,
            IsPinned = post.IsPinned,
            IsLocked = post.IsLocked,
            ViewCount = post.ViewCount,
            UpvoteCount = post.UpvoteCount,
            DownvoteCount = post.DownvoteCount,
            CommentCount = post.CommentCount,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Author = new UserDto
            {
                Id = author.Id,
                Username = author.Username,
                DisplayName = author.DisplayName,
                AvatarUrl = author.AvatarUrl,
                Role = author.Role.ToString(),
                Rank = author.Rank.ToString(),
                ReputationPoints = author.ReputationPoints
            },
            Category = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Color = category.Color
            },
            Tags = dto.Tags ?? new List<string>()
        }, "Post created");
    }

    public async Task<ApiResponse<PostDto>> UpdateAsync(int id, UpdatePostDto dto, int userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (post == null)
            return ApiResponse<PostDto>.ErrorResponse("Không tìm thấy bài viết");

        if (post.AuthorId != userId)
            return ApiResponse<PostDto>.ErrorResponse("Bạn chỉ có thể sửa bài viết của mình");

        if (dto.Title != null)
        {
            post.Title = dto.Title;
            post.Slug = GenerateSlug(dto.Title);
        }

        if (dto.Content != null)
        {
            if (!IsValidBodyLength(dto.Content, 50))
                return ApiResponse<PostDto>.ErrorResponse("Nội dung phải có ít nhất 50 ký tự sau khi loại bỏ thẻ HTML");
            post.Content = _htmlSanitizer.Sanitize(dto.Content);
        }

        if (dto.CategoryId.HasValue)
        {
            var newCategory = await _context.Categories.FindAsync(dto.CategoryId.Value);
            if (newCategory == null)
                return ApiResponse<PostDto>.ErrorResponse("Danh mục không tồn tại");

            post.Category!.PostCount--;
            newCategory.PostCount++;
            post.Category = newCategory;
            post.CategoryId = dto.CategoryId.Value;
        }

        // Update tags
        if (dto.Tags != null)
        {
            var existingTags = await _context.PostTags.Where(pt => pt.PostId == id).ToListAsync();
            _context.PostTags.RemoveRange(existingTags);

            foreach (var tag in dto.Tags.Take(5))
            {
                _context.PostTags.Add(new PostTag { Name = tag.ToLower(), PostId = id });
            }
        }

        post.UpdatedAt = DateTime.UtcNow;
        post.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _securityLogService.LogAsync(userId, SecurityAction.EditPost, null, null, $"Sửa bài viết: {post.Title}", true);

        return ApiResponse<PostDto>.SuccessResponse(new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = _htmlSanitizer.Sanitize(post.Content),
            Slug = post.Slug,
            IsPinned = post.IsPinned,
            IsLocked = post.IsLocked,
            UpdatedAt = post.UpdatedAt,
            Author = new UserDto
            {
                Id = post.Author.Id,
                Username = post.Author.Username,
                DisplayName = post.Author.DisplayName,
                AvatarUrl = post.Author.AvatarUrl,
                Role = post.Author.Role.ToString()
            },
            Category = new CategoryDto
            {
                Id = post.Category!.Id,
                Name = post.Category!.Name,
                Slug = post.Category!.Slug
            },
            Tags = dto.Tags ?? new List<string>()
        }, "Bài viết đã được cập nhật");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, UserRole userRole)
    {
        var post = await _context.Posts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy bài viết");

        if (post.AuthorId != userId && userRole == UserRole.User)
            return ApiResponse<bool>.ErrorResponse("Bạn chỉ có thể xóa bài viết của mình");

        post.IsDeleted = true;
        post.Category!.PostCount--;
        post.Category = null;

        await _context.SaveChangesAsync();
        await _securityLogService.LogAsync(userId, SecurityAction.DeletePost, null, null, $"Xóa bài viết: {post.Title}", true);

        // ===== TÍCH HỢP REPUTATION: Trừ điểm khi bài bị xóa =====
        await _reputationService.ApplyChangeAsync(
            userId: post.AuthorId,
            action: ReputationAction.PostDeleted,
            actorId: userId,
            postId: post.Id,
            description: $"Bài viết bị xóa: {post.Title}");

        return ApiResponse<bool>.SuccessResponse(true, "Bài viết đã được xóa");
    }

    public async Task<ApiResponse<bool>> PinPostAsync(int id, bool isPinned, UserRole userRole)
    {
        if (userRole == UserRole.User)
            return ApiResponse<bool>.ErrorResponse("Chỉ moderator và admin mới có thể ghim bài viết");

        var post = await _context.Posts.FindAsync(id);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        post.IsPinned = isPinned;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, isPinned ? "Post pinned" : "Post unpinned");
    }

    public async Task<ApiResponse<bool>> LockPostAsync(int id, bool isLocked, UserRole userRole)
    {
        if (userRole == UserRole.User)
            return ApiResponse<bool>.ErrorResponse("Only moderators and admins can lock posts");

        var post = await _context.Posts.FindAsync(id);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        post.IsLocked = isLocked;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, isLocked ? "Post locked" : "Post unlocked");
    }

    public async Task<ApiResponse<bool>> VoteAsync(int postId, int userId, bool isUpvote)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        // Không vote bài của chính mình
        if (post.AuthorId == userId)
            return ApiResponse<bool>.ErrorResponse("Bạn không thể vote bài viết của chính mình");

        var existingVote = await _context.PostVotes
            .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId);

        if (existingVote != null)
        {
            if (existingVote.IsUpvote == isUpvote)
            {
                // Bỏ vote
                _context.PostVotes.Remove(existingVote);
                if (isUpvote) post.UpvoteCount--;
                else post.DownvoteCount--;

                // Hoàn điểm
                await _reputationService.ApplyChangeAsync(
                    userId: post.AuthorId,
                    action: isUpvote ? ReputationAction.UpvoteRemoved : ReputationAction.DownvoteRemoved,
                    actorId: userId,
                    postId: postId);
            }
            else
            {
                // Đổi chiều vote
                if (existingVote.IsUpvote)
                {
                    post.UpvoteCount--;
                    post.DownvoteCount++;
                }
                else
                {
                    post.DownvoteCount--;
                    post.UpvoteCount++;
                }
                existingVote.IsUpvote = isUpvote;

                // Áp dụng thay đổi điểm: trừ cũ + cộng mới
                var reverseAction = existingVote.IsUpvote ? ReputationAction.PostDownvoted : ReputationAction.PostUpvoted;
                var newAction = existingVote.IsUpvote ? ReputationAction.PostUpvoted : ReputationAction.PostDownvoted;
                await _reputationService.ApplyChangeAsync(post.AuthorId, reverseAction, userId, postId);
                await _reputationService.ApplyChangeAsync(post.AuthorId, newAction, userId, postId);

                await _notificationService.CreateAsync(post.AuthorId, userId, isUpvote ? NotificationType.PostUpvote : NotificationType.PostDownvote, postId);
            }
        }
        else
        {
            // Vote mới
            _context.PostVotes.Add(new PostVote
            {
                PostId = postId,
                UserId = userId,
                IsUpvote = isUpvote
            });

            if (isUpvote) post.UpvoteCount++;
            else post.DownvoteCount++;

            // ===== TÍCH HỢP REPUTATION: Cộng/trừ điểm cho tác giả =====
            var action = isUpvote ? ReputationAction.PostUpvoted : ReputationAction.PostDownvoted;
            await _reputationService.ApplyChangeAsync(
                userId: post.AuthorId,
                action: action,
                actorId: userId,
                postId: postId);

            await _notificationService.CreateAsync(post.AuthorId, userId, isUpvote ? NotificationType.PostUpvote : NotificationType.PostDownvote, postId);
        }

        await _context.SaveChangesAsync();
        await _badgeService.CheckAndAwardBadgesAsync(post.AuthorId);

        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<bool>> RemoveVoteAsync(int postId, int userId)
    {
        var vote = await _context.PostVotes
            .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId);

        if (vote == null)
            return ApiResponse<bool>.ErrorResponse("Vote not found");

        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        if (vote.IsUpvote) post.UpvoteCount--;
        else post.DownvoteCount--;

        _context.PostVotes.Remove(vote);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<bool>> IncrementViewAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
            return ApiResponse<bool>.ErrorResponse("Post not found");

        post.ViewCount++;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("+", "plus")
            .Replace("--", "-");

        return $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}

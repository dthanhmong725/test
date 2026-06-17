using System.ComponentModel.DataAnnotations;

namespace DOAN_LAPTRINHWEB.Models.DTOs;

// Category DTOs
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "folder";
    public string Color { get; set; } = "#0d6efd";
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int PostCount { get; set; }
}

public class CreateCategoryDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string Icon { get; set; } = "folder";

    [StringLength(50)]
    public string Color { get; set; } = "#0d6efd";

    public int DisplayOrder { get; set; } = 0;
}

// Post DTOs
public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto Author { get; set; } = null!;
    public CategoryDto Category { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
    public int? UserVote { get; set; }
    public bool IsBookmarked { get; set; }
}

public class CreatePostDto
{
    [Required]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề phải từ 10 đến 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(50, ErrorMessage = "Nội dung phải có ít nhất 50 ký tự sau khi loại bỏ thẻ HTML")]
    public string Content { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    public List<string>? Tags { get; set; }

    public List<AttachmentDto>? Attachments { get; set; }
}

public class UpdatePostDto
{
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề phải từ 10 đến 200 ký tự")]
    public string? Title { get; set; }

    [MinLength(50, ErrorMessage = "Nội dung phải có ít nhất 50 ký tự sau khi loại bỏ thẻ HTML")]
    public string? Content { get; set; }

    public int? CategoryId { get; set; }

    public List<string>? Tags { get; set; }
}

public class PostListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string AuthorRole { get; set; } = "User";
    public string AuthorRank { get; set; } = "Newbie";
    public int AuthorReputation { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

// Comment DTOs
public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto Author { get; set; } = null!;
    public int? ParentCommentId { get; set; }
    public int? UserVote { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}

public class CreateCommentDto
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public int? ParentCommentId { get; set; }
}

public class UpdateCommentDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
}

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

// Vote DTOs
public class VoteDto
{
    [Required]
    public bool IsUpvote { get; set; }
}

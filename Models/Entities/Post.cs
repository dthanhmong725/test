using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOAN_LAPTRINHWEB.Models.Entities;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    public bool IsPinned { get; set; } = false;

    public bool IsLocked { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public int ViewCount { get; set; } = 0;

    public int UpvoteCount { get; set; } = 0;

    public int DownvoteCount { get; set; } = 0;

    public int CommentCount { get; set; } = 0;

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public virtual User Author { get; set; } = null!;

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    // Navigation properties
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public virtual ICollection<PostVote> Votes { get; set; } = new List<PostVote>();
    public virtual ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();
}

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;

    public bool IsEdited { get; set; } = false;

    public DateTime? EditedAt { get; set; }

    public int UpvoteCount { get; set; } = 0;

    public int DownvoteCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public virtual User Author { get; set; } = null!;

    public int PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public virtual Post Post { get; set; } = null!;

    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public virtual Comment? ParentComment { get; set; }

    // Navigation properties
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public virtual ICollection<CommentVote> Votes { get; set; } = new List<CommentVote>();
}

public class PostVote
{
    [Key]
    public int Id { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public int PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public virtual Post Post { get; set; } = null!;
}

public class CommentVote
{
    [Key]
    public int Id { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public int CommentId { get; set; }

    [ForeignKey(nameof(CommentId))]
    public virtual Comment Comment { get; set; } = null!;
}

public class PostTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public int PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public virtual Post Post { get; set; } = null!;
}

public class PostAttachment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? FileUrl { get; set; }

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public virtual Post Post { get; set; } = null!;

    public int UploadedById { get; set; }

    [ForeignKey(nameof(UploadedById))]
    public virtual User UploadedBy { get; set; } = null!;
}

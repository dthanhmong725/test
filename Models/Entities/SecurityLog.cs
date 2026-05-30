using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOAN_LAPTRINHWEB.Models.Entities;

public enum SecurityAction
{
    Login = 1,
    Logout = 2,
    FailedLogin = 3,
    ChangePassword = 4,
    EditPost = 5,
    DeletePost = 6,
    BanUser = 7,
    UnbanUser = 8,
    SuspiciousActivity = 9,
    RateLimitExceeded = 10,
    RoleChange = 11
}

public class SecurityLog
{
    [Key]
    public int Id { get; set; }

    public SecurityAction Action { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsSuccess { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

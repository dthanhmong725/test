using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOAN_LAPTRINHWEB.Models.Entities;

public class Follow
{
    [Key]
    public int Id { get; set; }

    public int FollowerId { get; set; }
    [ForeignKey(nameof(FollowerId))]
    public virtual User Follower { get; set; } = null!;

    public int FollowingId { get; set; }
    [ForeignKey(nameof(FollowingId))]
    public virtual User Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
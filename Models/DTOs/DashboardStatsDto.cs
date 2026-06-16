namespace DOAN_LAPTRINHWEB.Models.DTOs;

public class DashboardStatsDto
{
    // Tổng số liệu hiện tại
    public int TotalUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int TotalCategories { get; set; }
    public int BannedUsers { get; set; }

    // Số liệu mới trong 24h gần nhất (để hiển thị "+N hôm nay")
    public int NewUsersToday { get; set; }
    public int NewPostsToday { get; set; }
    public int NewCommentsToday { get; set; }

    // Dữ liệu cho biểu đồ tăng trưởng (mặc định 14 ngày gần nhất)
    public List<DailyCountDto> UserGrowth { get; set; } = new();
    public List<DailyCountDto> PostGrowth { get; set; } = new();
    public List<DailyCountDto> CommentGrowth { get; set; } = new();

    // Phân bổ vai trò người dùng
    public int AdminCount { get; set; }
    public int ModeratorCount { get; set; }
    public int RegularUserCount { get; set; }

    // Top danh mục theo số bài viết
    public List<CategoryStatDto> TopCategories { get; set; } = new();

    // Top người dùng theo điểm reputation
    public List<TopUserStatDto> TopUsers { get; set; } = new();
}

public class DailyCountDto
{
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
    public int Count { get; set; }
}

public class CategoryStatDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int PostCount { get; set; }
}

public class TopUserStatDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int ReputationPoints { get; set; }
    public string Role { get; set; } = string.Empty;
}

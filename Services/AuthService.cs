using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MailKit.Net.Smtp;
using DOAN_LAPTRINHWEB.Data;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;
using DOAN_LAPTRINHWEB.Models.Entities;

namespace DOAN_LAPTRINHWEB.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly ISecurityLogService _securityLogService;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;

    public AuthService(AppDbContext context, IConfiguration config, ILogger<AuthService> logger, ISecurityLogService securityLogService)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _securityLogService = securityLogService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, string? ipAddress, string? userAgent)
    {
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
        {
            await LogSecurityEventAsync(null, ipAddress, userAgent, "REGISTER_FAILED", "Username already exists", false, dto.Username);
            return ApiResponse<AuthResponseDto>.ErrorResponse("Tên đăng nhập đã được sử dụng");
        }

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
        {
            await LogSecurityEventAsync(null, ipAddress, userAgent, "REGISTER_FAILED", "Email already exists", false, dto.Email);
            return ApiResponse<AuthResponseDto>.ErrorResponse("Email đã được sử dụng");
        }

        var verificationToken = GenerateSecureToken(6);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            DisplayName = dto.Username,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "REGISTER_SUCCESS", $"User registered: {user.Username}", true);

        var (accessToken, refreshToken, expiresAt) = await GenerateTokensAsync(user);
        await SaveRefreshTokenAsync(user.Id, refreshToken, ipAddress);

        // Send verification email
        await SendVerificationEmailAsync(user.Email, verificationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        }, "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Username);

        if (user == null)
        {
            await LogSecurityEventAsync(null, ipAddress, userAgent, "LOGIN_FAILED", "User not found", false, dto.Username);
            return ApiResponse<AuthResponseDto>.ErrorResponse("Tên đăng nhập hoặc mật khẩu không đúng");
        }

        // Check lockout
        if (user.IsLockedOut)
        {
            var remaining = user.LockoutEnd!.Value - DateTime.UtcNow;
            await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_LOCKED", $"Account locked until {user.LockoutEnd}", false);
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                $"Tài khoản bị khóa tạm thời. Vui lòng thử lại sau {remaining.TotalMinutes:F0} phút.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_LOCKOUT",
                    $"Account locked after {MaxFailedAttempts} failed attempts", false);
            }
            else
            {
                await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_FAILED",
                    $"Invalid password. {MaxFailedAttempts - user.FailedLoginAttempts} attempts remaining", false);
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<AuthResponseDto>.ErrorResponse(
                $"Tên đăng nhập hoặc mật khẩu không đúng. Còn {MaxFailedAttempts - user.FailedLoginAttempts} lần thử.");
        }

        if (!user.IsActive)
        {
            await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_DISABLED", "Account is disabled", false);
            return ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa");
        }

        if (user.IsBanned)
        {
            await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_BANNED", $"Account banned: {user.BanReason}", false);
            return ApiResponse<AuthResponseDto>.ErrorResponse($"Tài khoản đã bị cấm. Lý do: {user.BanReason}");
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "LOGIN_SUCCESS", "Successful login", true);

        var (accessToken, refreshToken, expiresAt) = await GenerateTokensAsync(user);
        await SaveRefreshTokenAsync(user.Id, refreshToken, ipAddress);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        }, "Đăng nhập thành công!");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(int userId, string? ipAddress, string? userAgent)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(userId, ipAddress, userAgent, "LOGOUT", "User logged out", true);

        return ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công");
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto dto, string? ipAddress, string? userAgent)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null)
        {
            // Don't reveal if email exists
            return ApiResponse<bool>.SuccessResponse(true,
                "Nếu email tồn tại trong hệ thống, một liên kết đặt lại mật khẩu đã được gửi.");
        }

        var resetToken = GenerateSecureToken(6);
        user.ResetToken = resetToken;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "PASSWORD_RESET_REQUEST", "Password reset requested", true);

        await SendPasswordResetEmailAsync(user.Email, resetToken, user.Username);

        return ApiResponse<bool>.SuccessResponse(true,
            "Nếu email tồn tại trong hệ thống, một liên kết đặt lại mật khẩu đã được gửi.");
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto, string? ipAddress, string? userAgent)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == dto.Token && u.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            return ApiResponse<bool>.ErrorResponse("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all existing refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(user.Id, ipAddress, userAgent, "PASSWORD_RESET_COMPLETE", "Password reset completed", true);

        return ApiResponse<bool>.SuccessResponse(true, "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return ApiResponse<bool>.ErrorResponse("Mật khẩu hiện tại không đúng");

        // Check password strength
        var strengthResult = ValidatePasswordStrength(dto.NewPassword);
        if (!strengthResult.IsValid)
            return ApiResponse<bool>.ErrorResponse(string.Join("; ", strengthResult.Errors));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all existing refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(userId, null, null, "PASSWORD_CHANGE", $"Password changed", true);

        return ApiResponse<bool>.SuccessResponse(true, "Đổi mật khẩu thành công!");
    }

    public async Task<ApiResponse<bool>> VerifyEmailAsync(string token, string? ipAddress)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Liên kết xác thực không hợp lệ hoặc đã hết hạn");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogSecurityEventAsync(user.Id, ipAddress, null, "EMAIL_VERIFIED", "Email verified successfully", true);

        return ApiResponse<bool>.SuccessResponse(true, "Xác thực email thành công!");
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (token == null)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Refresh token không hợp lệ");

        if (token.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Refresh token đã hết hạn");

        var user = token.User;
        if (user.IsBanned || !user.IsActive)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản bị cấm hoặc vô hiệu hóa");

        // Revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = GenerateSecureToken(64);

        var (accessToken, newRefreshToken, expiresAt) = await GenerateTokensAsync(user);
        await SaveRefreshTokenAsync(user.Id, newRefreshToken, ipAddress);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        });
    }

    public string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DefaultSecureKey123456789012345678901234567890"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("rank", user.Rank.ToString()),
            new Claim("emailVerified", user.IsEmailVerified.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "CyberForum",
            audience: _config["Jwt:Audience"] ?? "CyberForum",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateSecureToken(int length)
    {
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, length);
    }

    private Task<(string accessToken, string refreshToken, DateTime expiresAt)> GenerateTokensAsync(User user)
    {
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateSecureToken(64);
        var expiresAt = DateTime.UtcNow.AddDays(7);
        return Task.FromResult((accessToken, refreshToken, expiresAt));
    }

    private async Task SaveRefreshTokenAsync(int userId, string token, string? ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    private async Task LogSecurityEventAsync(int? userId, string? ipAddress, string? userAgent, string eventType, string details, bool isSuccess, string? username = null)
    {
        if (!userId.HasValue) return;
        try
        {
            var action = eventType switch
            {
                "LOGIN_SUCCESS" => SecurityAction.Login,
                "LOGOUT" => SecurityAction.Logout,
                "LOGIN_FAILED" => SecurityAction.FailedLogin,
                "LOGIN_LOCKOUT" or "LOGIN_LOCKED" => SecurityAction.SuspiciousActivity,
                "LOGIN_DISABLED" => SecurityAction.SuspiciousActivity,
                "LOGIN_BANNED" => SecurityAction.SuspiciousActivity,
                "PASSWORD_CHANGE" => SecurityAction.ChangePassword,
                "REGISTER_SUCCESS" => SecurityAction.Login,
                _ => SecurityAction.Login
            };
            await _securityLogService.LogAsync(userId.Value, action, ipAddress, userAgent, details, isSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
        }
    }

    private (bool IsValid, List<string> Errors) ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (password.Length < 8)
            errors.Add("Mật khẩu phải có ít nhất 8 ký tự");
        if (!password.Any(char.IsUpper))
            errors.Add("Mật khẩu phải chứa ít nhất 1 chữ HOA");
        if (!password.Any(char.IsDigit))
            errors.Add("Mật khẩu phải chứa ít nhất 1 số");
        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
            errors.Add("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (!@#$%^&*...)");

        return (errors.Count == 0, errors);
    }

    private async Task SendVerificationEmailAsync(string email, string token)
    {
        try
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:Username"];
            var smtpPass = _config["Smtp:Password"];
            var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@cyberforum.local";
            var fromName = _config["Smtp:FromName"] ?? "CyberForum";

            if (string.IsNullOrEmpty(smtpHost))
            {
                _logger.LogWarning("SMTP not configured. Verification token: {Token}", token);
                return;
            }

            var verifyUrl = $"{_config["App:Url"] ?? "http://localhost:5000"}/verify-email.html?token={token}";

            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: 'Segoe UI', Arial, sans-serif; background: #0d1117; color: #e6edf3; padding: 20px;'>
<div style='max-width: 600px; margin: 0 auto; background: #161b22; border: 1px solid #30363d; border-radius: 10px; padding: 30px;'>
<h2 style='color: #00e5a0; margin-bottom: 20px;'>Xác thực email của bạn</h2>
<p>Xin chào,</p>
<p>Cảm ơn bạn đã đăng ký CyberForum. Vui lòng nhấp vào nút bên dưới để xác thực địa chỉ email của bạn:</p>
<div style='text-align: center; margin: 30px 0;'>
<a href='{verifyUrl}' style='display: inline-block; background: #00e5a0; color: #0d1117; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: 600;'>Xác thực email</a>
</div>
<p style='color: #8b949e; font-size: 12px;'>Liên kết này sẽ hết hạn sau 24 giờ.</p>
<hr style='border: none; border-top: 1px solid #30363d; margin: 20px 0;'>
<p style='color: #8b949e; font-size: 12px;'>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
</div>
</body>
</html>";

            await SendEmailAsync(email, "Xác thực email - CyberForum", body, smtpUser!, smtpPass!, smtpHost, smtpPort, fromEmail, fromName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
        }
    }

    private async Task SendPasswordResetEmailAsync(string email, string token, string username)
    {
        try
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:Username"];
            var smtpPass = _config["Smtp:Password"];
            var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@cyberforum.local";
            var fromName = _config["Smtp:FromName"] ?? "CyberForum";

            if (string.IsNullOrEmpty(smtpHost))
            {
                _logger.LogWarning("SMTP not configured. Reset token: {Token}", token);
                return;
            }

            var resetUrl = $"{_config["App:Url"] ?? "http://localhost:5000"}/reset-password.html?token={token}";

            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: 'Segoe UI', Arial, sans-serif; background: #0d1117; color: #e6edf3; padding: 20px;'>
<div style='max-width: 600px; margin: 0 auto; background: #161b22; border: 1px solid #30363d; border-radius: 10px; padding: 30px;'>
<h2 style='color: #f85149; margin-bottom: 20px;'>Đặt lại mật khẩu</h2>
<p>Xin chào {username},</p>
<p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhấp vào nút bên dưới để đặt lại mật khẩu:</p>
<div style='text-align: center; margin: 30px 0;'>
<a href='{resetUrl}' style='display: inline-block; background: #f85149; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: 600;'>Đặt lại mật khẩu</a>
</div>
<p style='color: #8b949e; font-size: 12px;'>Liên kết này sẽ hết hạn sau 1 giờ. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
<hr style='border: none; border-top: 1px solid #30363d; margin: 20px 0;'>
<p style='color: #8b949e; font-size: 12px;'>Đây là email tự động từ CyberForum. Không trả lời email này.</p>
</div>
</body>
</html>";

            await SendEmailAsync(email, "Đặt lại mật khẩu - CyberForum", body, smtpUser!, smtpPass!, smtpHost, smtpPort, fromEmail, fromName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, string smtpUser, string smtpPass, string smtpHost, int smtpPort, string fromEmail, string fromName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = subject;

        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        Bio = user.Bio,
        Role = user.Role.ToString(),
        Rank = user.Rank.ToString(),
        ReputationPoints = user.ReputationPoints,
        CreatedAt = user.CreatedAt
    };
}

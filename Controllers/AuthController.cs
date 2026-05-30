using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DOAN_LAPTRINHWEB.Interfaces;
using DOAN_LAPTRINHWEB.Models.DTOs;

namespace DOAN_LAPTRINHWEB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordStrengthService _passwordStrengthService;

    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    public AuthController(IAuthService authService, IPasswordStrengthService passwordStrengthService)
    {
        _authService = authService;
        _passwordStrengthService = passwordStrengthService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
        }

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.RegisterAsync(dto, ipAddress, userAgent);

        if (!result.Success)
            return BadRequest(result);

        SetAuthCookies(result.Data!.AccessToken, result.Data.RefreshToken, 1);

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
        }

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

        if (!result.Success)
            return BadRequest(result);

        SetAuthCookies(result.Data!.AccessToken, result.Data.RefreshToken, dto.RememberMe ? 7 : 1);

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _authService.LogoutAsync(userId, ipAddress, userAgent);

        ClearAuthCookies();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công"));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
        }

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.ForgotPasswordAsync(dto, ipAddress, userAgent);

        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
        }

        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.ResetPasswordAsync(dto, ipAddress, userAgent);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.ChangePasswordAsync(userId, dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var ipAddress = GetClientIp();
        var result = await _authService.VerifyEmailAsync(dto.Token, ipAddress);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrEmpty(refreshToken))
        {
            var bodyToken = Request.Headers["X-Refresh-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(bodyToken))
                refreshToken = bodyToken;
        }

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(ApiResponse<object>.ErrorResponse("Refresh token không được tìm thấy"));

        var ipAddress = GetClientIp();
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        if (!result.Success)
        {
            ClearAuthCookies();
            return Unauthorized(result);
        }

        var cookieExpiry = result.Data!.ExpiresAt > DateTime.UtcNow.AddDays(1)
            ? (int)(result.Data.ExpiresAt - DateTime.UtcNow).TotalDays
            : 1;

        SetAuthCookies(result.Data.AccessToken, result.Data.RefreshToken, cookieExpiry);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var accessToken = _authService.GenerateJwtToken(new Models.Entities.User
        {
            Id = userId,
            Username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
            Role = Enum.Parse<Models.Entities.UserRole>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User"),
            Rank = Enum.Parse<Models.Entities.UserRank>(User.FindFirst("rank")?.Value ?? "Newbie")
        });

        return Ok(ApiResponse<object>.SuccessResponse(new { accessToken }));
    }

    [HttpPost("check-password-strength")]
    [AllowAnonymous]
    public IActionResult CheckPasswordStrength([FromBody] CheckPasswordDto dto)
    {
        var result = _passwordStrengthService.CheckStrength(dto.Password);
        return Ok(ApiResponse<PasswordStrengthDto>.SuccessResponse(result));
    }

    private void SetAuthCookies(string accessToken, string refreshToken, int expiryDays)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expiryDays)
        };

        Response.Cookies.Append(AccessTokenCookie, accessToken, cookieOptions);

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expiryDays)
        };

        Response.Cookies.Append(RefreshTokenCookie, refreshToken, refreshOptions);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps });
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

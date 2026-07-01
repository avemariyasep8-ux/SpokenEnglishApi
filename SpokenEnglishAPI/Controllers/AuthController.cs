using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Application.Services;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly IUserService _userService;

        private readonly IAuditLogService _audit;

        public AuthController(IAuthService authService, IUserService userService, IAuditLogService audit)
        {
            _authService = authService;
            _userService = userService;
            _audit = audit;
        }

        private string? ClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? UserAgent() =>
            Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            try
            {
                var result = _authService.Login(dto);
                await _audit.LogAsync("LOGIN_SUCCESS", result.UserID, result.Email, ClientIp(), UserAgent(), true);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _audit.LogAsync("LOGIN_FAILURE", null, dto.EmailOrMob, ClientIp(), UserAgent(), false, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterUserRequestDto dto)
        {
            var result = _userService.Register(dto);
            await _audit.LogAsync("REGISTER", null, dto.Email, ClientIp(), UserAgent(), true);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminData()
        {
            return Ok("Admin Access");
        }

        [HttpPost("refresh")]
        public IActionResult Refresh(RefreshTokenRequestDto dto)
       => Ok(_authService.RefreshToken(dto));

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordDto dto)
        {
            _authService.ResetPassword(dto);
            return Ok("Password reset successful");
        }

        [HttpPost("otp-login")]
        public IActionResult OtpLogin(OtpLoginDto dto)
        {
            return Ok("OTP verified");
        }

        //[HttpPost("reset-password")]
        //public IActionResult ResetPassword(ResetPasswordDto dto)
        //{
        //    _userService.ResetPassword(dto.Token, dto.NewPassword);
        //    return Ok("Password reset successful");
        //}

        //[HttpPost("send-otp")]
        //public IActionResult SendOtp(string mobileOrEmail)
        //{
        //    var otp = new Random().Next(100000, 999999).ToString();
        //    _userRepo.SaveOtp(mobileOrEmail, otp);

        //    // Send OTP via SMS/Email
        //    return Ok("OTP sent");
        //}

        //[HttpPost("verify-otp")]
        //public IActionResult VerifyOtp(OtpLoginDto dto)
        //{
        //    var user = _userRepo.ValidateOtp(dto.Input, dto.Otp);
        //    var token = _jwt.Generate(user);

        //    return Ok(new { Token = token });
        //}

    }



}

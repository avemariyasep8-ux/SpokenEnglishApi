using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Domain.Entities;
using SpokenEnglishAPI.Infrastructure.Repositories;
using SpokenEnglishAPI.Infrastructure.Security;

namespace SpokenEnglishAPI.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserRepository _userRepo;
    private readonly JwtTokenGenerator _jwt;

    public AuthService(UserRepository userRepo, JwtTokenGenerator jwt)
    {
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public LoginResponseDto Login(LoginRequestDto dto)
    {
        var user = _userRepo.GetByEmail(dto.EmailOrMob);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = _jwt.Generate(user);
        var refresh = _jwt.GenerateRefreshToken();

        _userRepo.SaveRefreshToken(user.ID, refresh, DateTime.UtcNow.AddDays(7));

        return new LoginResponseDto
        {
            UserID = user.ID,
            Email = user.Email,
            Token = token,
            RefreshToken = refresh,
            ApiKey = user.ApiKey
        };
    }

    public LoginResponseDto RefreshToken(RefreshTokenRequestDto dto)
    {
        var user = _userRepo.ValidateRefreshToken(dto.RefreshToken);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var newRefreshToken = _jwt.GenerateRefreshToken();
        _userRepo.SaveRefreshToken(user.ID, newRefreshToken, DateTime.UtcNow.AddDays(7));

        return new LoginResponseDto
        {
            Token = _jwt.Generate(user),
            RefreshToken = newRefreshToken,
            ApiKey = user.ApiKey
        };
    }

    public LoginResponseDto ValidateRefreshToken(string refreshToken)
    {
        var user = _userRepo.ValidateRefreshToken(refreshToken);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var newRefreshToken = _jwt.GenerateRefreshToken();
        _userRepo.SaveRefreshToken(user.ID, newRefreshToken, DateTime.UtcNow.AddDays(7));

        return new LoginResponseDto
        {
            Token = _jwt.Generate(user),
            RefreshToken = newRefreshToken,
            ApiKey = user.ApiKey
        };
    }

    public void ResetPassword(ResetPasswordDto dto)
    {
        var user = _userRepo.GetByEmail(dto.EmailOrMobile);

        if (user == null)
            throw new Exception("User not found");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        _userRepo.UpdatePassword(user.ID, hash);
    }
}

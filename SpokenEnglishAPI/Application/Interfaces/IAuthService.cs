using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Application.Interfaces;

public interface IAuthService
{
    LoginResponseDto Login(LoginRequestDto dto);
    LoginResponseDto RefreshToken(RefreshTokenRequestDto dto);
    void ResetPassword(ResetPasswordDto dto);

    LoginResponseDto ValidateRefreshToken(string refreshToken);
}


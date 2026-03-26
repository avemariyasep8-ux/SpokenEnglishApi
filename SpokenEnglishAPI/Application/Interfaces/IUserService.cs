using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Application.Interfaces
{
    public interface IUserService
    {
        RegisterUserResponseDto Register(RegisterUserRequestDto dto);

        void ResetPassword(ResetPasswordDto dto);
    }
}

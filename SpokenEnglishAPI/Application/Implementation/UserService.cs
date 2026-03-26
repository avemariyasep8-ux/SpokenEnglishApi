using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Application.Implementation;

public class UserService : IUserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public RegisterUserResponseDto Register(RegisterUserRequestDto dto)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var apiKey = Guid.NewGuid().ToString("N");

        _userRepository.CreateUser(dto.Email, passwordHash, apiKey);

        return new RegisterUserResponseDto
        {
            Email = dto.Email,
            mobnumber = dto.mobnumber,
            ApiKey = apiKey,
            Message = "User registered successfully"
        };
    }

    public void ResetPassword(ResetPasswordDto dto)
    {
        var user = _userRepository.GetByEmail(dto.EmailOrMobile);

        if (user == null)
            throw new Exception("User not found");

        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        _userRepository.UpdatePassword(user.ID, newHash);
    }
}

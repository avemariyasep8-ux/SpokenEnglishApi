using Dapper;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Application.Implementation;

public class UserService : IUserService
{
    private readonly UserRepository _userRepository;
    private readonly DbContext _db;

    public UserService(UserRepository userRepository, DbContext db)
    {
        _userRepository = userRepository;
        _db = db;
    }

    public RegisterUserResponseDto Register(RegisterUserRequestDto dto)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var apiKey = Guid.NewGuid().ToString("N");

        _userRepository.CreateUser(dto.Email, passwordHash, apiKey);

        // Set full_name, level, and school_role on the newly created user
        if (!string.IsNullOrEmpty(dto.FullName) || dto.SchoolId.HasValue || !string.IsNullOrEmpty(dto.Level))
        {
            using var con = _db.CreateConnection();
            var level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level;
            con.Execute(
                "UPDATE users SET full_name=@fn, school_id=@sid, school_role=@sr, level=@lvl WHERE email=@email",
                new { fn = dto.FullName, sid = dto.SchoolId, sr = dto.SchoolRole, lvl = level, email = dto.Email });

            // Auto-assign the matching learning package based on the chosen level.
            var packageLevel = SpokenEnglishAPI.Controllers.PackageController.LevelToPackageLevel(level);
            var packageId = con.ExecuteScalar<int?>(
                "SELECT package_id FROM learning_package WHERE level=@lvl AND is_active=true ORDER BY display_order LIMIT 1",
                new { lvl = packageLevel });
            if (packageId.HasValue)
                con.Execute("UPDATE users SET package_id=@pid WHERE email=@email",
                    new { pid = packageId.Value, email = dto.Email });

            // If school selected, add to school_users pending approval
            if (dto.SchoolId.HasValue && !string.IsNullOrEmpty(dto.SchoolRole))
            {
                var userId = con.ExecuteScalar<int>("SELECT id FROM users WHERE email=@email", new { email = dto.Email });
                con.Execute(
                    @"INSERT INTO school_users (school_id, user_id, school_role, class_name, is_approved)
                      VALUES (@sid, @uid, @role, @cls, false)
                      ON CONFLICT (school_id, user_id) DO NOTHING",
                    new { sid = dto.SchoolId, uid = userId, role = dto.SchoolRole, cls = dto.ClassName });
            }
        }

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
        if (user == null) throw new Exception("User not found");
        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        _userRepository.UpdatePassword(user.ID, newHash);
    }
}

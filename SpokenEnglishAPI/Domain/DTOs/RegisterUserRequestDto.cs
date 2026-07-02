using System.ComponentModel.DataAnnotations;

namespace SpokenEnglishAPI.Domain.DTOs
{
    public class RegisterUserRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = "";

        [Required]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Mobile must be 10–15 digits.")]
        public string mobnumber { get; set; } = "";

        [MaxLength(200)]
        public string? FullName { get; set; }

        [Range(1, int.MaxValue)]
        public int? SchoolId { get; set; }

        // Restrict role to known values
        [RegularExpression("^(Student|Teacher|Headmaster)$", ErrorMessage = "Invalid school role.")]
        public string? SchoolRole { get; set; }

        [MaxLength(50)]
        public string? ClassName { get; set; }

        // 3 primary levels (Beginner/Intermediate/Advanced); legacy 5-level values still accepted.
        [RegularExpression("^(Beginner|Intermediate|Advanced|Elementary|College|Professional)$", ErrorMessage = "Invalid level.")]
        [MaxLength(50)]
        public string? Level { get; set; }
    }

    public class RegisterUserResponseDto
    {
        public string Email { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string mobnumber { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class ResetPasswordDto
    {
        [Required]
        [MaxLength(200)]
        public string EmailOrMobile { get; set; } = "";

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string NewPassword { get; set; } = "";
    }
}

namespace SpokenEnglishAPI.Domain.DTOs
{
    public class RegisterUserRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string mobnumber { get; set; }
        public string? FullName { get; set; }
        public int? SchoolId { get; set; }
        public string? SchoolRole { get; set; } // Student | Teacher | Headmaster
        public string? ClassName { get; set; }
    }

    public class RegisterUserResponseDto
    {
        public string Email { get; set; }
        public string ApiKey { get; set; }
        public string mobnumber { get; set; }
        public string Message { get; set; }
    }

    public class ResetPasswordDto
    {
        public string EmailOrMobile { get; set; }
        public string NewPassword { get; set; }
    }
 
}

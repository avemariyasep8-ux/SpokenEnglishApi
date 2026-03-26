namespace SpokenEnglishAPI.Domain.DTOs
{
    public class RegisterUserRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string mobnumber { get; set; }
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

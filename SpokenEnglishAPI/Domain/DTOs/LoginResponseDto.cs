namespace SpokenEnglishAPI.Domain.DTOs
{
    public class LoginResponseDto
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ApiKey { get; set; }
        public string Role { get; set; }
        public string Level { get; set; }
    }
    public class RefreshTokenRequestDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class OtpLoginDto
    {
        public string MobileNumber { get; set; }
        public string OTP { get; set; }
    }

             
}

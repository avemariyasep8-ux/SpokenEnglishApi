using System.ComponentModel.DataAnnotations;

namespace SpokenEnglishAPI.Domain.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string EmailOrMob { get; set; } = "";

        [Required]
        [MaxLength(128)]
        public string Password { get; set; } = "";
    }
}

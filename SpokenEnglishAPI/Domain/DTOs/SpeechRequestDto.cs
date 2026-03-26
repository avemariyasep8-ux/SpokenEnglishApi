namespace SpokenEnglishAPI.Domain.DTOs
{
    public class SpeechRequestDto
    {
        public string AudioBase64 { get; set; }
        public string LanguageCode { get; set; }
    }
}

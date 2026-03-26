using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Application.Interfaces
{
    public interface ISpeechService
    {
        Task<string> ConvertAsync(SpeechRequestDto dto);
    }
}

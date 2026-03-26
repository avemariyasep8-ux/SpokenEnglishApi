using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Application.Services;

public class SpeechService : ISpeechService
{
    public Task<string> ConvertAsync(SpeechRequestDto dto)
    {
        return Task.FromResult("Converted text (mock)");
    }
}

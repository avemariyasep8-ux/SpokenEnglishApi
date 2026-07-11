namespace SpokenEnglishAPI.Application.Interfaces
{
    public record AiChatTurn(string Role, string Text); // Role: "user" | "ai"

    public record AiChatResult(string Reply, bool HasMistake, string? CorrectedText, string? Explanation);

    public interface IAiConversationService
    {
        Task<AiChatResult> ChatAsync(string scenario, string level, List<AiChatTurn> history, string userMessage);
    }
}

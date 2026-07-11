using System.Text;
using System.Text.Json;
using SpokenEnglishAPI.Application.Interfaces;

namespace SpokenEnglishAPI.Application.Implementation
{
    /// <summary>
    /// Real-time AI conversation partner backed by Google Gemini. Each turn: the model plays
    /// a natural human role in the given scenario AND grades the learner's last message for
    /// grammar/word-choice mistakes, returning a corrected version + short explanation when needed.
    /// </summary>
    public class GeminiAiConversationService : IAiConversationService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;
        private const string Model = "gemini-2.0-flash";

        public GeminiAiConversationService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? config["Gemini:ApiKey"];
        }

        public async Task<AiChatResult> ChatAsync(string scenario, string level, List<AiChatTurn> history, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("GEMINI_API_KEY is not configured.");

            var systemPrompt =
                $"You are a friendly, patient English conversation partner helping a {level}-level Tamil-speaking " +
                $"learner practice spoken English. Scenario: {scenario}. " +
                "Reply naturally and briefly (1-3 sentences) like a real person having this conversation, and ask a " +
                "follow-up question to keep the conversation going. Keep vocabulary appropriate for the learner's level. " +
                "Also check the learner's LAST message for grammar or word-choice mistakes. " +
                "Respond ONLY as JSON matching this shape: " +
                "{\"reply\": string, \"hasMistake\": boolean, \"correctedText\": string|null, \"explanation\": string|null}. " +
                "If there is no mistake, set hasMistake to false and correctedText/explanation to null. " +
                "Keep explanation to one short simple sentence a beginner can understand.";

            var contents = new List<object>();
            foreach (var turn in history)
                contents.Add(new { role = turn.Role == "ai" ? "model" : "user", parts = new[] { new { text = turn.Text } } });
            contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

            var requestBody = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                generationConfig = new { responseMimeType = "application/json", temperature = 0.7 }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";
            using var resp = await _http.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini API error ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

            using var parsed = JsonDocument.Parse(text);
            var root = parsed.RootElement;
            var reply = root.TryGetProperty("reply", out var r) ? r.GetString() ?? "" : "";
            var hasMistake = root.TryGetProperty("hasMistake", out var hm) && hm.ValueKind == JsonValueKind.True;
            var correctedText = root.TryGetProperty("correctedText", out var ct) && ct.ValueKind == JsonValueKind.String ? ct.GetString() : null;
            var explanation = root.TryGetProperty("explanation", out var ex) && ex.ValueKind == JsonValueKind.String ? ex.GetString() : null;

            return new AiChatResult(reply, hasMistake, correctedText, explanation);
        }
    }
}

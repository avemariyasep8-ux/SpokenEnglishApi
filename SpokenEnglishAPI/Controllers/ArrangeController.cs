using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArrangeController : ControllerBase
    {
        private readonly IArrangeService _service;
        private readonly DbContext _db;
        public ArrangeController(IArrangeService service, DbContext db) { _service = service; _db = db; }

        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetSentences(int lessonId, int languageId)
        {
            var sentences = await _service.GetArrangeSentences(lessonId, languageId);
            return Ok(sentences);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSentence([FromBody] AddArrangeSentenceDto dto)
        {
            using var con = _db.CreateConnection();
            var sentenceId = await con.ExecuteScalarAsync<int>(
                "INSERT INTO arrangesentence (lessonid) VALUES (@lid) RETURNING arrangesentenceid",
                new { lid = dto.LessonId });
            await con.ExecuteAsync(
                "INSERT INTO arrangesentence_lang (arrangesentenceid, languageid, correctsentence) VALUES (@sid, @lang, @cs)",
                new { sid = sentenceId, lang = dto.LanguageId, cs = dto.CorrectSentence });
            var words = dto.CorrectSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var wordId = await con.ExecuteScalarAsync<int>(
                    "INSERT INTO arrangesentenceword (arrangesentenceid, correctorder) VALUES (@sid, @ord) RETURNING wordid",
                    new { sid = sentenceId, ord = i + 1 });
                await con.ExecuteAsync(
                    "INSERT INTO arrangesentenceword_lang (wordid, languageid, wordtext) VALUES (@wid, @lang, @wt)",
                    new { wid = wordId, lang = dto.LanguageId, wt = words[i] });
            }
            return Ok(new { arrangeSentenceId = sentenceId });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSentence(int id)
        {
            using var con = _db.CreateConnection();
            // cascade order: word_lang → word → sentence_lang → sentence
            var wordIds = await con.QueryAsync<int>(
                "SELECT wordid FROM arrangesentenceword WHERE arrangesentenceid=@id", new { id });
            foreach (var wid in wordIds)
                await con.ExecuteAsync("DELETE FROM arrangesentenceword_lang WHERE wordid=@wid", new { wid });
            await con.ExecuteAsync("DELETE FROM arrangesentenceword WHERE arrangesentenceid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentence_lang WHERE arrangesentenceid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentence WHERE arrangesentenceid=@id", new { id });
            return Ok(new { message = "Deleted" });
        }
    }

    public record AddArrangeSentenceDto(int LessonId, int LanguageId, string CorrectSentence);
}

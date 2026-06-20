using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _service;
        private readonly DbContext _db;
        public ReadingController(IReadingService service, DbContext db) { _service = service; _db = db; }

        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetSentences(int lessonId, int languageId)
        {
            var sentences = await _service.GetReadingSentences(lessonId, languageId);
            return Ok(sentences);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] AddReadingDto dto)
        {
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO readingsentence (lessonid, sentencetext, languageid, displayorder)
                  VALUES (@lid, @st, @lang, (SELECT COALESCE(MAX(displayorder),0)+1 FROM readingsentence WHERE lessonid=@lid))
                  RETURNING readingsentenceid",
                new { lid = dto.LessonId, st = dto.SentenceText, lang = dto.LanguageId });
            return Ok(new { readingId = id });
        }
    }

    public class AddReadingDto
    {
        public int LessonId { get; set; }
        public string SentenceText { get; set; } = "";
        public int LanguageId { get; set; } = 1;
    }
}

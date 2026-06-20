using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WordContentController : ControllerBase
    {
        private readonly WordContentRepository _repo;
        private readonly DbContext _db;
        public WordContentController(WordContentRepository repo, DbContext db) { _repo = repo; _db = db; }

        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> Get(int lessonId, int languageId)
        {
            var content = await _repo.GetWordContent(lessonId, languageId);
            return Ok(content);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] AddWordContentDto dto)
        {
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO lesson_word_content
                    (lesson_id, word_name, sentence_pattern, definition_en, definition_ta, example_en, example_ta, display_order)
                  VALUES (@lid, @wn, @sp, @den, @dta, @een, @eta, @ord)
                  RETURNING content_id",
                new { lid=dto.LessonId, wn=dto.WordName, sp=dto.SentencePattern, den=dto.DefinitionEn,
                      dta=dto.DefinitionTa, een=dto.ExampleEn, eta=dto.ExampleTa, ord=dto.DisplayOrder });
            return Ok(new { contentId = id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] AddWordContentDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE lesson_word_content SET
                    word_name=@wn, sentence_pattern=@sp,
                    definition_en=@den, definition_ta=@dta,
                    example_en=@een, example_ta=@eta, display_order=@ord
                  WHERE content_id=@id",
                new { id, wn=dto.WordName, sp=dto.SentencePattern, den=dto.DefinitionEn,
                      dta=dto.DefinitionTa, een=dto.ExampleEn, eta=dto.ExampleTa, ord=dto.DisplayOrder });
            return Ok(new { message = "Updated" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM lesson_word_content WHERE content_id=@id", new { id });
            return Ok(new { message = "Deleted" });
        }
    }

    public class AddWordContentDto
    {
        public int LessonId { get; set; }
        public string WordName { get; set; } = "";
        public string? SentencePattern { get; set; }
        public string? DefinitionEn { get; set; }
        public string? DefinitionTa { get; set; }
        public string? ExampleEn { get; set; }
        public string? ExampleTa { get; set; }
        public int DisplayOrder { get; set; }
    }
}

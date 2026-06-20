using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StreakController : ControllerBase
    {
        private readonly StreakRepository _repo;
        public StreakController(StreakRepository repo) => _repo = repo;

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(int userId)
        {
            var streak = await _repo.GetStreak(userId);
            return Ok(streak ?? new UserStreakDto());
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateStreakDto dto)
        {
            var streak = await _repo.UpdateStreak(dto.UserId, dto.XpEarned);
            return Ok(streak);
        }
    }
}

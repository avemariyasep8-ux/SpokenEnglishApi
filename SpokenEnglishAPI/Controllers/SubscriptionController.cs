using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly SubscriptionRepository _repo;
        public SubscriptionController(SubscriptionRepository repo) => _repo = repo;

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _repo.GetPlans();
            return Ok(plans);
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto dto)
        {
            var id = await _repo.Subscribe(dto);
            return Ok(new { SubscriptionId = id, Message = "Subscription activated successfully!" });
        }

        [HttpGet("my/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetMySubscription(int userId)
        {
            var sub = await _repo.GetUserSubscription(userId);
            if (sub == null) return Ok(new { Status = "none" });
            return Ok(sub);
        }
    }
}

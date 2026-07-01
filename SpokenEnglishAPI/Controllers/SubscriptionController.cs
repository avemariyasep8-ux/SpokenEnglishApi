using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;
using SpokenEnglishAPI.Infrastructure.Security;

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
            if (!OwnershipGuard.CanAccess(User, dto.UserId)) return Forbid();
            var id = await _repo.Subscribe(dto);
            return Ok(new { SubscriptionId = id, Message = "Subscription activated successfully!" });
        }

        [HttpGet("my/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetMySubscription(int userId)
        {
            if (!OwnershipGuard.CanAccess(User, userId)) return Forbid();
            var sub = await _repo.GetUserSubscription(userId);
            if (sub == null) return Ok(new { Status = "none" });
            return Ok(sub);
        }
    }
}

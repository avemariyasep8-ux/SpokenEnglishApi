using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpokenEnglishAPI.Controllers;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Tests;

public class SubscriptionControllerTests
{
    private readonly Mock<SubscriptionRepository> _mockRepo;
    private readonly SubscriptionController _controller;

    public SubscriptionControllerTests()
    {
        _mockRepo   = new Mock<SubscriptionRepository>(null!);
        _controller = new SubscriptionController(_mockRepo.Object);

        // Admin principal so the ownership guard passes in unit tests.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("uid", "1"),
        }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task GetPlans_ReturnsThreePlans()
    {
        var plans = new List<SubscriptionPlanDto>
        {
            new() { PlanId=1, PlanName="Monthly",  DurationMonths=1,  PriceInr=199 },
            new() { PlanId=2, PlanName="Yearly",   DurationMonths=12, PriceInr=999 },
            new() { PlanId=3, PlanName="2-Year",   DurationMonths=24, PriceInr=1499 },
        };
        _mockRepo.Setup(r => r.GetPlans()).ReturnsAsync(plans);

        var result = await _controller.GetPlans() as OkObjectResult;

        result!.StatusCode.Should().Be(200);
        (result.Value as IEnumerable<SubscriptionPlanDto>).Should().HaveCount(3);
    }

    [Fact]
    public async Task Subscribe_ReturnsSuccessMessage()
    {
        _mockRepo.Setup(r => r.Subscribe(It.IsAny<SubscribeRequestDto>())).ReturnsAsync(42);

        var result = await _controller.Subscribe(new SubscribeRequestDto { UserId = 1, PlanId = 2 }) as OkObjectResult;

        result!.StatusCode.Should().Be(200);
        result.Value!.ToString().Should().Contain("42");
    }

    [Fact]
    public async Task GetMySubscription_ReturnsNone_WhenNoSubscription()
    {
        _mockRepo.Setup(r => r.GetUserSubscription(99)).ReturnsAsync((UserSubscriptionDto?)null);

        var result = await _controller.GetMySubscription(99) as OkObjectResult;

        result!.Value!.ToString().Should().Contain("none");
    }

    [Fact]
    public async Task GetMySubscription_ReturnsActiveSub_WhenExists()
    {
        var sub = new UserSubscriptionDto
        {
            SubscriptionId = 1, PlanName = "Yearly",
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1),
            Status = "active", DaysRemaining = 365
        };
        _mockRepo.Setup(r => r.GetUserSubscription(5)).ReturnsAsync(sub);

        var result = await _controller.GetMySubscription(5) as OkObjectResult;
        var data   = result!.Value as UserSubscriptionDto;

        data!.Status.Should().Be("active");
        data.DaysRemaining.Should().Be(365);
    }
}

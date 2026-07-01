using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Controllers;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for user progress tracking — sequential lesson unlock,
/// answer submission, and progress summary.
/// </summary>
public class ProgressTests
{
    private readonly Mock<IProgressService> _mockProgress;
    private readonly ProgressController     _controller;

    public ProgressTests()
    {
        _mockProgress = new Mock<IProgressService>();
        _controller   = new ProgressController(_mockProgress.Object, null!);

        // Provide an authenticated Admin principal so the ownership guard passes
        // (Admins may act on any userId).
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

    // ── SaveAnswer ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAnswer_ReturnsOk_OnSuccess()
    {
        _mockProgress.Setup(s => s.SaveAnswer(It.IsAny<SubmitAnswerDto>())).Returns(Task.CompletedTask);

        var dto    = new SubmitAnswerDto { UserID = 1, LessonID = 1, ActivityType = "Meaning", IsCorrect = true };
        var result = await _controller.SaveAnswer(dto) as OkObjectResult;

        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SaveAnswer_CallsService_WithCorrectDto()
    {
        SubmitAnswerDto? captured = null;
        _mockProgress.Setup(s => s.SaveAnswer(It.IsAny<SubmitAnswerDto>()))
                     .Callback<SubmitAnswerDto>(dto => captured = dto)
                     .Returns(Task.CompletedTask);

        var input = new SubmitAnswerDto { UserID = 7, LessonID = 3, ActivityType = "Arrange", IsCorrect = false };
        await _controller.SaveAnswer(input);

        captured.Should().NotBeNull();
        captured!.UserID.Should().Be(7);
        captured.LessonID.Should().Be(3);
        captured.ActivityType.Should().Be("Arrange");
        captured.IsCorrect.Should().BeFalse();
    }

    // ── GetProgress ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProgress_ReturnsOk_WithProgressList()
    {
        var progress = new List<UserProgressDto>
        {
            new() { LessonID = 1, TotalAttempt = 10, CorrectCount = 8 },
            new() { LessonID = 2, TotalAttempt = 5,  CorrectCount = 5 },
        };
        _mockProgress.Setup(s => s.GetUserProgress(1, 1)).ReturnsAsync(progress);

        var result = await _controller.GetProgress(1, 1) as OkObjectResult;
        var data   = result!.Value as IEnumerable<UserProgressDto>;

        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProgress_CalculatesAccuracy_Correctly()
    {
        var progress = new List<UserProgressDto>
        {
            new() { LessonID = 1, TotalAttempt = 10, CorrectCount = 7 },
        };
        _mockProgress.Setup(s => s.GetUserProgress(1, 1)).ReturnsAsync(progress);

        var result = await _controller.GetProgress(1, 1) as OkObjectResult;
        var data   = (result!.Value as IEnumerable<UserProgressDto>)!.First();

        data.Accuracy.Should().Be(70); // 7/10 = 70%
    }

    [Fact]
    public async Task GetProgress_ReturnsZeroAccuracy_WhenNoAttempts()
    {
        var progress = new List<UserProgressDto>
        {
            new() { LessonID = 1, TotalAttempt = 0, CorrectCount = 0 },
        };
        _mockProgress.Setup(s => s.GetUserProgress(5, 1)).ReturnsAsync(progress);

        var result = await _controller.GetProgress(5, 1) as OkObjectResult;
        var data   = (result!.Value as IEnumerable<UserProgressDto>)!.First();

        data.Accuracy.Should().Be(0);
    }

    [Fact]
    public async Task GetProgress_ReturnsEmpty_WhenNoHistory()
    {
        _mockProgress.Setup(s => s.GetUserProgress(99, 1)).ReturnsAsync(new List<UserProgressDto>());

        var result = await _controller.GetProgress(99, 1) as OkObjectResult;
        (result!.Value as IEnumerable<UserProgressDto>).Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for sequential lesson unlock logic.
/// Rule: a user can only access lesson N+1 after completing lesson N.
/// </summary>
public class SequentialUnlockTests
{
    // Mirror of the frontend unlock logic
    private static bool IsUnlocked(int lessonIndex, IReadOnlyList<int> completedLessonIds, IReadOnlyList<int> allLessonIds)
    {
        if (lessonIndex == 0) return true;                          // first lesson always unlocked
        var prevId = allLessonIds[lessonIndex - 1];
        return completedLessonIds.Contains(prevId);
    }

    [Fact]
    public void FirstLesson_AlwaysUnlocked()
    {
        var all       = new[] { 1, 2, 3, 4, 5 };
        var completed = Array.Empty<int>();

        IsUnlocked(0, completed, all).Should().BeTrue();
    }

    [Fact]
    public void SecondLesson_LockedUntilFirstComplete()
    {
        var all       = new[] { 1, 2, 3 };
        var completed = Array.Empty<int>();

        IsUnlocked(1, completed, all).Should().BeFalse();
    }

    [Fact]
    public void SecondLesson_UnlockedAfterFirstComplete()
    {
        var all       = new[] { 1, 2, 3 };
        var completed = new[] { 1 };

        IsUnlocked(1, completed, all).Should().BeTrue();
    }

    [Fact]
    public void ThirdLesson_LockedIfOnlyFirstComplete()
    {
        var all       = new[] { 1, 2, 3 };
        var completed = new[] { 1 };

        IsUnlocked(2, completed, all).Should().BeFalse();
    }

    [Fact]
    public void ThirdLesson_UnlockedAfterBothPreviousComplete()
    {
        var all       = new[] { 1, 2, 3 };
        var completed = new[] { 1, 2 };

        IsUnlocked(2, completed, all).Should().BeTrue();
    }

    [Fact]
    public void AllLessonsUnlocked_WhenAllPreviousComplete()
    {
        var all       = new[] { 1, 2, 3, 4, 5 };
        var completed = new[] { 1, 2, 3, 4 };

        for (int i = 0; i < all.Length; i++)
            IsUnlocked(i, completed, all).Should().BeTrue($"lesson index {i} should be unlocked");
    }
}

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Controllers;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for lesson retrieval and sequential lesson flow.
/// </summary>
public class LessonContentTests
{
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly LessonsController   _controller;

    public LessonContentTests()
    {
        _mockLessonService = new Mock<ILessonService>();
        // LessonsController now requires DbContext; pass null — content endpoint not tested here
        _controller        = new LessonsController(_mockLessonService.Object, null!);
    }

    // ── GetLessons ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLessons_ReturnsOk_WithLessonList()
    {
        var lessons = new List<LessonDto>
        {
            new() { LessonID = 1, LessonName = "Greetings",    LessonOrder = 1, IsActive = true },
            new() { LessonID = 2, LessonName = "Daily Phrases", LessonOrder = 2, IsActive = true },
        };
        _mockLessonService.Setup(s => s.GetLessons(1)).ReturnsAsync(lessons);

        var result = await _controller.GetLessons(1) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var data = result.Value as IEnumerable<LessonDto>;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLessons_ReturnsEmptyList_WhenNoLessons()
    {
        _mockLessonService.Setup(s => s.GetLessons(99)).ReturnsAsync(new List<LessonDto>());

        var result = await _controller.GetLessons(99) as OkObjectResult;
        var data   = result!.Value as IEnumerable<LessonDto>;

        data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLessons_ReturnsLessonsInOrder()
    {
        var lessons = new List<LessonDto>
        {
            new() { LessonID = 1, LessonName = "A", LessonOrder = 1 },
            new() { LessonID = 2, LessonName = "B", LessonOrder = 2 },
            new() { LessonID = 3, LessonName = "C", LessonOrder = 3 },
        };
        _mockLessonService.Setup(s => s.GetLessons(1)).ReturnsAsync(lessons);

        var result = await _controller.GetLessons(1) as OkObjectResult;
        var data   = (result!.Value as IEnumerable<LessonDto>)!.ToList();

        data[0].LessonOrder.Should().Be(1);
        data[1].LessonOrder.Should().Be(2);
        data[2].LessonOrder.Should().Be(3);
    }

    // ── GetLessonDetail ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetLessonDetail_ReturnsOk_WhenLessonExists()
    {
        var lesson = new LessonDto { LessonID = 5, LessonName = "Shopping", LessonOrder = 5, IsActive = true };
        _mockLessonService.Setup(s => s.GetLessonDetail(5)).ReturnsAsync(lesson);

        var result = await _controller.GetLessonDetail(5) as OkObjectResult;

        result!.StatusCode.Should().Be(200);
        (result.Value as LessonDto)!.LessonName.Should().Be("Shopping");
    }

    [Fact]
    public async Task GetLessonDetail_ReturnsNotFound_WhenLessonMissing()
    {
        _mockLessonService.Setup(s => s.GetLessonDetail(999)).ReturnsAsync((LessonDto?)null);

        var result = await _controller.GetLessonDetail(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── Sequential lesson flow ────────────────────────────────────────────────

    [Fact]
    public async Task GetSequentialLesson_ReturnsNotFound_WhenLessonMissing()
    {
        _mockLessonService.Setup(s => s.GetSequentialLesson(999, 1)).ReturnsAsync((SequentialLessonDto?)null);

        var result = await _controller.GetSequentialLesson(999, 1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSequentialLesson_ReturnsSteps_WhenLessonExists()
    {
        var flow = new SequentialLessonDto
        {
            LessonID   = 1,
            LessonName = "Greetings",
            Steps      = new List<LessonStepDto>
            {
                new() { StepType = "meaning",          Order = 1 },
                new() { StepType = "example",          Order = 2 },
                new() { StepType = "practice_arrange", Order = 3 },
                new() { StepType = "practice_speak",   Order = 4 },
            }
        };
        _mockLessonService.Setup(s => s.GetSequentialLesson(1, 1)).ReturnsAsync(flow);

        var result = await _controller.GetSequentialLesson(1, 1) as OkObjectResult;
        var data   = result!.Value as SequentialLessonDto;

        data!.Steps.Should().HaveCount(4);
        data.Steps.Select(s => s.StepType).Should().Contain("practice_arrange");
    }
}

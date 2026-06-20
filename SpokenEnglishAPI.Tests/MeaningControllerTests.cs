using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Controllers;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Tests;

public class MeaningControllerTests
{
    private readonly Mock<IMeaningService> _mockService;
    private readonly MeaningController _controller;

    public MeaningControllerTests()
    {
        _mockService = new Mock<IMeaningService>();
        _controller  = new MeaningController(_mockService.Object);
    }

    [Fact]
    public async Task GetQuestions_ReturnsOk_WithQuestions()
    {
        var expected = new List<MeaningQuizDto>
        {
            new() { QuestionID = 1, QuestionText = "Test Q?", Options = new List<MeaningOptionDto>
            {
                new() { OptionID = 1, OptionText = "Correct", IsCorrect = true },
                new() { OptionID = 2, OptionText = "Wrong",   IsCorrect = false }
            }}
        };
        _mockService.Setup(s => s.GetMeaningQuestionsWithAnswers(1, 1)).ReturnsAsync(expected);

        var result = await _controller.GetQuestions(1, 1) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var data = result.Value as IEnumerable<MeaningQuizDto>;
        data.Should().HaveCount(1);
        data!.First().Options.Should().Contain(o => o.IsCorrect);
    }

    [Fact]
    public async Task GetQuestions_ReturnsIsCorrect_True_ForCorrectOption()
    {
        // This is the critical test — the bug was IsCorrect always being false
        var expected = new List<MeaningQuizDto>
        {
            new() { QuestionID = 5, QuestionText = "Which is correct?", Options = new()
            {
                new() { OptionID = 10, OptionText = "Right",  IsCorrect = true  },
                new() { OptionID = 11, OptionText = "Wrong1", IsCorrect = false },
                new() { OptionID = 12, OptionText = "Wrong2", IsCorrect = false },
                new() { OptionID = 13, OptionText = "Wrong3", IsCorrect = false },
            }}
        };
        _mockService.Setup(s => s.GetMeaningQuestionsWithAnswers(5, 1)).ReturnsAsync(expected);

        var result = await _controller.GetQuestions(5, 1) as OkObjectResult;
        var data   = (result!.Value as IEnumerable<MeaningQuizDto>)!.ToList();

        data[0].Options.Count(o => o.IsCorrect).Should().Be(1);
        data[0].Options.First(o => o.IsCorrect).OptionText.Should().Be("Right");
        data[0].Options.Where(o => !o.IsCorrect).Should().HaveCount(3);
    }

    [Fact]
    public async Task GetQuestions_ReturnsEmptyList_WhenNoQuestions()
    {
        _mockService.Setup(s => s.GetMeaningQuestionsWithAnswers(99, 1)).ReturnsAsync(new List<MeaningQuizDto>());

        var result = await _controller.GetQuestions(99, 1) as OkObjectResult;
        var data   = result!.Value as IEnumerable<MeaningQuizDto>;

        data.Should().BeEmpty();
    }
}

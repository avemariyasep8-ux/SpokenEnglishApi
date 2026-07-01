using MiniExcelLibs;
using Microsoft.AspNetCore.Http;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Infrastructure.Repositories;
using SpokenEnglishAPI.Domain.DTOs;
using System.Data;

namespace SpokenEnglishAPI.Application.Implementation
{
    public class BulkUploadService : IBulkUploadService
    {
        private readonly LessonRepository _lessonRepo;
        private readonly MeaningRepository _meaningRepo;
        private readonly ArrangeRepository _arrangeRepo;

        public BulkUploadService(LessonRepository lessonRepo, MeaningRepository meaningRepo, ArrangeRepository arrangeRepo)
        {
            _lessonRepo = lessonRepo;
            _meaningRepo = meaningRepo;
            _arrangeRepo = arrangeRepo;
        }

        public async Task<byte[]> DownloadTemplate()
        {
            var memoryStream = new MemoryStream();
            var sheets = new Dictionary<string, object>
            {
                ["Lessons"] = new[] { 
                    new { LessonID = "", LanguageID = 1, LessonTypeID = 1, LessonOrder = 1, LessonName = "Present Tense", Description = "Learn basic present tense sentences.", Action = "ADD" },
                    new { LessonID = "", LanguageID = 1, LessonTypeID = 1, LessonOrder = 2, LessonName = "Past Tense", Description = "Learn basic past tense sentences.", Action = "ADD" }
                },
                ["Meanings"] = new[] { 
                    new { LessonID = 1, LanguageID = 1, QuestionText = "Choose the correct meaning for 'Delicious'", Option1 = "சுவையான", Option2 = "வேகமான", Option3 = "மெதுவான", Option4 = "பெரிய", CorrectOptionIndex = 1, Action = "ADD" },
                    new { LessonID = 1, LanguageID = 1, QuestionText = "I ___ to school every day.", Option1 = "Go", Option2 = "Went", Option3 = "Going", Option4 = "Gone", CorrectOptionIndex = 1, Action = "ADD" }
                },
                ["ArrangeSentences"] = new[] { 
                    new { LessonID = 1, LanguageID = 1, CorrectSentence = "She eats rice every day", Words = "She, eats, rice, every, day", Action = "ADD" },
                    new { LessonID = 1, LanguageID = 1, CorrectSentence = "They play football on weekends", Words = "They, play, football, on, weekends", Action = "ADD" }
                }
            };
            memoryStream.SaveAs(sheets);
            return memoryStream.ToArray();
        }

        public async Task<string> UploadBulkData(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var sheetNames = stream.GetSheetNames();

            int lessonCount = 0, meaningCount = 0, arrangeCount = 0;

            foreach (var name in sheetNames)
            {
                var rows = stream.Query(sheetName: name).Cast<IDictionary<string, object>>();

                if (name == "Lessons")
                {
                    foreach (var row in rows)
                    {
                        var action = row["Action"]?.ToString()?.ToUpper();
                        if (action == "ADD")
                        {
                            await _lessonRepo.AddLesson(new AddLessonDto {
                                LanguageID = Convert.ToInt32(row["LanguageID"]),
                                LessonTypeID = Convert.ToInt32(row["LessonTypeID"]),
                                LessonOrder = Convert.ToInt32(row["LessonOrder"]),
                                LessonName = row["LessonName"]?.ToString() ?? "",
                                Description = row["Description"]?.ToString()
                            });
                            lessonCount++;
                        }
                        // Add Edit/Delete logic as needed
                    }
                }
                else if (name == "Meanings")
                {
                    foreach (var row in rows)
                    {
                        var action = row["Action"]?.ToString()?.ToUpper();
                        if (action == "ADD")
                        {
                            var qid = await _meaningRepo.AddQuestion(new AddMeaningQuestionDto {
                                LessonID = Convert.ToInt32(row["LessonID"]),
                                LanguageID = Convert.ToInt32(row["LanguageID"]),
                                QuestionText = row["QuestionText"]?.ToString() ?? ""
                            });

                            for (int i = 1; i <= 4; i++)
                            {
                                var optText = row[$"Option{i}"]?.ToString();
                                if (!string.IsNullOrEmpty(optText))
                                {
                                    await _meaningRepo.AddOption(new AddMeaningOptionDto {
                                        QuestionID = qid,
                                        LanguageID = Convert.ToInt32(row["LanguageID"]),
                                        OptionText = optText,
                                        IsCorrect = i == Convert.ToInt32(row["CorrectOptionIndex"])
                                    });
                                }
                            }
                            meaningCount++;
                        }
                    }
                }
                else if (name == "ArrangeSentences")
                {
                    foreach (var row in rows)
                    {
                        var action = row["Action"]?.ToString()?.ToUpper();
                        if (action == "ADD")
                        {
                            var sid = await _arrangeRepo.AddSentence(
                                Convert.ToInt32(row["LessonID"]),
                                Convert.ToInt32(row["LanguageID"]),
                                row["CorrectSentence"]?.ToString() ?? "");

                            var wordsStr = row["Words"]?.ToString() ?? "";
                            var words = wordsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < words.Length; i++)
                            {
                                await _arrangeRepo.AddWord(sid, 
                                    Convert.ToInt32(row["LanguageID"]), 
                                    words[i].Trim(), 
                                    i + 1);
                            }
                            arrangeCount++;
                        }
                    }
                }
            }

            return $"Processed {lessonCount} lessons, {meaningCount} meanings, {arrangeCount} sentences.";
        }
    }
}

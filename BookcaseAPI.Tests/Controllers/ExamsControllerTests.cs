using BookcaseAPI.Controllers;
using BookcaseAPI.Data;
using BookcaseAPI.Models;
using BookcaseAPI.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace BookcaseAPI.Tests.Controllers
{
    public class ExamsControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static ClaimsPrincipal CreateUser(int userId, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static ExamsController CreateController(ApplicationDbContext context, int userId, bool isAdmin)
        {
            return new ExamsController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateUser(userId, isAdmin)
                    }
                }
            };
        }

        [Fact]
        public async Task GetExams_ReturnsAllExams_WhenAdmin()
        {
            using var context = CreateContext();
            context.Exams.AddRange(
                new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" },
                new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "B", TestName = "T2" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);

            var result = await controller.GetExams();

            Assert.NotNull(result.Value);
            var exams = result.Value;
            Assert.Equal(2, exams.Count());
        }

        [Fact]
        public async Task GetExams_ReturnsOnlyUserExams_WhenNotAdmin()
        {
            using var context = CreateContext();
            context.Exams.AddRange(
                new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" },
                new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "B", TestName = "T2" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var result = await controller.GetExams();

            Assert.NotNull(result.Value);
            var exams = result.Value;
            Assert.Single(exams);
            Assert.Equal(1, exams.First().ClientId);
        }

        [Fact]
        public async Task GetExam_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.GetExam(42);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetExam_ReturnsForbid_WhenNotOwnerAndNotAdmin()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);
            var examId = context.Exams.Select(e => e.Id).Single();

            var result = await controller.GetExam(examId);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetExam_ReturnsExam_WhenAdmin()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);
            var examId = context.Exams.Select(e => e.Id).Single();

            var result = await controller.GetExam(examId);

            Assert.NotNull(result.Value);
            var exam = result.Value;
            Assert.Equal(examId, exam.Id);
        }

        [Fact]
        public async Task CreateExam_CreatesExamAndReturnsCreated()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 5, isAdmin: false);

            var dto = new CreateExamDto
            {
                Date = DateTime.UtcNow,
                Address = "Address",
                TestName = "Test"
            };

            var result = await controller.CreateExam(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdExam = Assert.IsType<Exam>(createdResult.Value);
            Assert.Equal(5, createdExam.ClientId);

            var savedExam = await context.Exams.SingleAsync();
            Assert.Equal(createdExam.Id, savedExam.Id);
        }

        [Fact]
        public async Task UpdateExam_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var dto = new UpdateExamDto
            {
                Date = DateTime.UtcNow,
                Address = "Address",
                TestName = "Test"
            };

            var result = await controller.UpdateExam(42, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateExam_ReturnsForbid_WhenNotOwnerAndNotAdmin()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);
            var examId = context.Exams.Select(e => e.Id).Single();

            var dto = new UpdateExamDto
            {
                Date = DateTime.UtcNow.AddDays(1),
                Address = "Updated",
                TestName = "Updated"
            };

            var result = await controller.UpdateExam(examId, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateExam_UpdatesExam_WhenOwner()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);
            var examId = context.Exams.Select(e => e.Id).Single();

            var dto = new UpdateExamDto
            {
                Date = DateTime.UtcNow.AddDays(2),
                Address = "Updated",
                TestName = "Updated"
            };

            var result = await controller.UpdateExam(examId, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await context.Exams.SingleAsync();
            Assert.Equal(dto.Address, updated.Address);
            Assert.Equal(dto.TestName, updated.TestName);
            Assert.Equal(dto.Date, updated.Date);
        }

        [Fact]
        public async Task DeleteExam_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.DeleteExam(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteExam_ReturnsForbid_WhenNotOwnerAndNotAdmin()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);
            var examId = context.Exams.Select(e => e.Id).Single();

            var result = await controller.DeleteExam(examId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteExam_RemovesExam_WhenOwner()
        {
            using var context = CreateContext();
            context.Exams.Add(new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);
            var examId = context.Exams.Select(e => e.Id).Single();

            var result = await controller.DeleteExam(examId);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Exams);
        }
    }
}
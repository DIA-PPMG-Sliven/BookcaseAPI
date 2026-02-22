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
    public class MajorsControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static MajorsController CreateController(ApplicationDbContext context, int userId, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var controller = new MajorsController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                    }
                }
            };

            return controller;
        }

        [Fact]
        public async Task GetMajors_ReturnsAll_WhenAdmin()
        {
            using var context = CreateContext();
            context.Majors.AddRange(
                new Major { Name = "M1", ClientId = 1 },
                new Major { Name = "M2", ClientId = 2 });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);

            var result = await controller.GetMajors();

            Assert.NotNull(result.Value);
            var majors = result.Value;
            Assert.Equal(2, majors.Count());
        }

        [Fact]
        public async Task GetMajors_ReturnsOnlyUserMajors_WhenNotAdmin()
        {
            using var context = CreateContext();
            context.Majors.AddRange(
                new Major { Name = "M1", ClientId = 1 },
                new Major { Name = "M2", ClientId = 2 });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var result = await controller.GetMajors();

            Assert.NotNull(result.Value);
            var majors = result.Value;
            Assert.Single(majors);
            Assert.Equal(1, majors.First().ClientId);
        }

        [Fact]
        public async Task GetMajor_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.GetMajor(42);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateMajor_SetsClientIdAndStatus()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 7, isAdmin: false);

            var dto = new CreateMajorDto
            {
                Name = "M1",
                UniversityName = "U",
                Status = "Applied"
            };

            var result = await controller.CreateMajor(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var major = Assert.IsType<Major>(created.Value);
            Assert.Equal(7, major.ClientId);
            Assert.Equal(MajorStatus.Applied, major.Status);
        }

        [Fact]
        public async Task UpdateMajor_ReturnsBadRequest_WhenIdMismatch()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.UpdateMajor(1, new UpdateMajorDto { Id = 2 });

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateMajor_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.UpdateMajor(1, new UpdateMajorDto { Id = 1 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateMajor_ReturnsForbid_WhenNotOwner()
        {
            using var context = CreateContext();
            var major = new Major { Name = "M1", ClientId = 1 };
            context.Majors.Add(major);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: false);

            var dto = new UpdateMajorDto { Id = major.Id, Name = "Updated" };

            var result = await controller.UpdateMajor(major.Id, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateMajor_ReturnsBadRequest_WhenExamNotOwned()
        {
            using var context = CreateContext();
            var major = new Major { Name = "M1", ClientId = 1 };
            var examOwned = new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" };
            var examOther = new Exam { ClientId = 2, Date = DateTime.UtcNow, Address = "B", TestName = "T2" };

            context.Majors.Add(major);
            context.Exams.AddRange(examOwned, examOther);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var dto = new UpdateMajorDto
            {
                Id = major.Id,
                Name = "Updated",
                ExamIds = new List<int> { examOwned.Id, examOther.Id }
            };

            var result = await controller.UpdateMajor(major.Id, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateMajor_UpdatesMajorAndLinks_WhenOwner()
        {
            using var context = CreateContext();
            var major = new Major { Name = "M1", ClientId = 1 };
            var exam1 = new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" };
            var exam2 = new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "B", TestName = "T2" };

            context.Majors.Add(major);
            context.Exams.AddRange(exam1, exam2);
            await context.SaveChangesAsync();

            context.MajorExams.Add(new MajorExam { MajorId = major.Id, ExamId = exam1.Id });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var dto = new UpdateMajorDto
            {
                Id = major.Id,
                Name = "Updated",
                UniversityName = "U",
                Address = "Addr",
                Duration = "4y",
                Language = "EN",
                GradingSystem = "G",
                Notes = "Notes",
                Status = "ApplyTo",
                ExamIds = new List<int> { exam2.Id }
            };

            var result = await controller.UpdateMajor(major.Id, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await context.Majors.Include(m => m.MajorExams).SingleAsync();
            Assert.Equal("Updated", updated.Name);
            Assert.Equal(MajorStatus.ApplyTo, updated.Status);
            Assert.Single(updated.MajorExams);
            Assert.Equal(exam2.Id, updated.MajorExams.First().ExamId);
        }

        [Fact]
        public async Task DeleteMajor_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.DeleteMajor(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteMajor_ReturnsForbid_WhenNotOwner()
        {
            using var context = CreateContext();
            var major = new Major { Name = "M1", ClientId = 1 };
            context.Majors.Add(major);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: false);

            var result = await controller.DeleteMajor(major.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteMajor_RemovesMajorAndLinks_WhenOwner()
        {
            using var context = CreateContext();
            var major = new Major { Name = "M1", ClientId = 1 };
            var exam = new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T1" };

            context.Majors.Add(major);
            context.Exams.Add(exam);
            await context.SaveChangesAsync();

            context.MajorExams.Add(new MajorExam { MajorId = major.Id, ExamId = exam.Id });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var result = await controller.DeleteMajor(major.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Majors);
            Assert.Empty(context.MajorExams);
        }
    }
}
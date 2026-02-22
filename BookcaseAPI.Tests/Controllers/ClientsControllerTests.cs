using BookcaseAPI.Controllers;
using BookcaseAPI.Data;
using BookcaseAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace BookcaseAPI.Tests.Controllers
{
    public class ClientsControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static ClientsController CreateController(ApplicationDbContext context, int userId, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var controller = new ClientsController(context)
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
        public async Task GetClients_ReturnsAll_WhenAdmin()
        {
            using var context = CreateContext();
            context.Clients.AddRange(
                new Client { Username = "u1", PasswordHash = "p1", Role = "User" },
                new Client { Username = "u2", PasswordHash = "p2", Role = "User" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);

            var result = await controller.GetClients();

            Assert.NotNull(result.Value);
            var clients = result.Value;
            Assert.Equal(2, clients.Count());
        }

        [Fact]
        public async Task GetClient_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.GetClient(42);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetClient_ReturnsClient_WhenExists()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);

            var result = await controller.GetClient(client.Id);

            Assert.NotNull(result.Value);
            var found = result.Value;
            Assert.Equal(client.Id, found.Id);
        }

        [Fact]
        public async Task DeleteClient_ReturnsForbid_WhenNotOwnerOrAdmin()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 2, isAdmin: false);

            var result = await controller.DeleteClient(client.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteClient_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.DeleteClient(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteClient_RemovesClientAndRelatedEntities()
        {
            using var context = CreateContext();

            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "Major", ClientId = 1 };
            var exam = new Exam { ClientId = 1, Date = DateTime.UtcNow, Address = "A", TestName = "T" };
            var application = new Application
            {
                Major = major,
                Student = client,
                MajorId = 1,
                StudentId = 1,
                Deadline = DateTime.UtcNow.AddDays(10),
                Stage = "Stage"
            };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Exams.Add(exam);
            context.Applications.Add(application);
            await context.SaveChangesAsync();

            context.ApplicationExams.Add(new ApplicationExam { ApplicationId = application.Id, ExamId = exam.Id });
            context.MajorExams.Add(new MajorExam { MajorId = major.Id, ExamId = exam.Id });
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 1, isAdmin: false);

            var result = await controller.DeleteClient(client.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Clients);
            Assert.Empty(context.Applications);
            Assert.Empty(context.Majors);
            Assert.Empty(context.Exams);
            Assert.Empty(context.ApplicationExams);
            Assert.Empty(context.MajorExams);
        }
    }
}
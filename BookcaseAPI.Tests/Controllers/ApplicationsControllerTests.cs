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
    public class ApplicationsControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static ApplicationsController CreateController(ApplicationDbContext context, int userId, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var controller = new ApplicationsController(context)
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
        public async Task GetApplications_ReturnsAll_WhenAdmin()
        {
            using var context = CreateContext();
            var client1 = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var client2 = new Client { Username = "u2", PasswordHash = "p2", Role = "User" };
            var major1 = new Major { Name = "M1", ClientId = 1 };
            var major2 = new Major { Name = "M2", ClientId = 2 };

            context.Clients.AddRange(client1, client2);
            context.Majors.AddRange(major1, major2);

            context.Applications.AddRange(
                new Application { Major = major1, Student = client1, MajorId = major1.Id, StudentId = client1.Id, Deadline = DateTime.UtcNow, Stage = "S1" },
                new Application { Major = major2, Student = client2, MajorId = major2.Id, StudentId = client2.Id, Deadline = DateTime.UtcNow, Stage = "S2" });

            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: true);

            var result = await controller.GetApplications();

            Assert.NotNull(result.Value);
            var apps = result.Value;
            Assert.Equal(2, apps.Count());
        }

        [Fact]
        public async Task GetApplications_ReturnsOnlyUserApps_WhenNotAdmin()
        {
            using var context = CreateContext();
            var client1 = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var client2 = new Client { Username = "u2", PasswordHash = "p2", Role = "User" };
            var major1 = new Major { Name = "M1", ClientId = 1 };
            var major2 = new Major { Name = "M2", ClientId = 2 };

            context.Clients.AddRange(client1, client2);
            context.Majors.AddRange(major1, major2);

            context.Applications.AddRange(
                new Application { Major = major1, Student = client1, MajorId = major1.Id, StudentId = client1.Id, Deadline = DateTime.UtcNow, Stage = "S1" },
                new Application { Major = major2, Student = client2, MajorId = major2.Id, StudentId = client2.Id, Deadline = DateTime.UtcNow, Stage = "S2" });

            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: client1.Id, isAdmin: false);

            var result = await controller.GetApplications();

            Assert.NotNull(result.Value);
            var apps = result.Value;
            Assert.Single(apps);
            Assert.Equal(client1.Id, apps.First().StudentId);
        }

        [Fact]
        public async Task GetApplication_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.GetApplication(42);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetApplication_ReturnsForbid_WhenNotOwner()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            var app = new Application { Major = major, Student = client, MajorId = major.Id, StudentId = client.Id, Deadline = DateTime.UtcNow, Stage = "S1" };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: false);

            var result = await controller.GetApplication(app.Id);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreateApplication_SetsStudentId_WhenNotAdmin()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            context.Clients.Add(client);
            context.Majors.Add(major);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: client.Id, isAdmin: false);

            var app = new Application
            {
                MajorId = major.Id,
                StudentId = 999,
                Deadline = DateTime.UtcNow.AddDays(1),
                Stage = "New"
            };

            var result = await controller.CreateApplication(app);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdApp = Assert.IsType<Application>(created.Value);
            Assert.Equal(client.Id, createdApp.StudentId);
        }

        [Fact]
        public async Task UpdateApplication_ReturnsBadRequest_WhenIdMismatch()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.UpdateApplication(1, new Application { Id = 2 });

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateApplication_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.UpdateApplication(1, new Application { Id = 1 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateApplication_ReturnsForbid_WhenNotOwner()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            var app = new Application { Major = major, Student = client, MajorId = major.Id, StudentId = client.Id, Deadline = DateTime.UtcNow, Stage = "S1" };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: false);

            var updated = new Application { Id = app.Id, MajorId = major.Id, StudentId = client.Id, Deadline = app.Deadline, Stage = "Updated" };
            var result = await controller.UpdateApplication(app.Id, updated);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateApplication_Updates_WhenOwner()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            var app = new Application { Major = major, Student = client, MajorId = major.Id, StudentId = client.Id, Deadline = DateTime.UtcNow, Stage = "S1" };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: client.Id, isAdmin: false);

            app.Stage = "Updated";
            app.Notes = "Notes";

            var result = await controller.UpdateApplication(app.Id, app);

            Assert.IsType<NoContentResult>(result);

            var saved = await context.Applications.SingleAsync();
            Assert.Equal("Updated", saved.Stage);
            Assert.Equal("Notes", saved.Notes);
        }

        [Fact]
        public async Task DeleteApplication_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1, isAdmin: true);

            var result = await controller.DeleteApplication(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteApplication_ReturnsForbid_WhenNotOwner()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            var app = new Application { Major = major, Student = client, MajorId = major.Id, StudentId = client.Id, Deadline = DateTime.UtcNow, Stage = "S1" };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: 99, isAdmin: false);

            var result = await controller.DeleteApplication(app.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteApplication_Removes_WhenOwner()
        {
            using var context = CreateContext();
            var client = new Client { Username = "u1", PasswordHash = "p1", Role = "User" };
            var major = new Major { Name = "M1", ClientId = 1 };
            var app = new Application { Major = major, Student = client, MajorId = major.Id, StudentId = client.Id, Deadline = DateTime.UtcNow, Stage = "S1" };

            context.Clients.Add(client);
            context.Majors.Add(major);
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: client.Id, isAdmin: false);

            var result = await controller.DeleteApplication(app.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Applications);
        }
    }
}
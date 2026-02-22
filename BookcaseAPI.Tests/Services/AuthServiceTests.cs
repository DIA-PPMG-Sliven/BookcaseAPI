using BookcaseAPI.Data;
using BookcaseAPI.DTOs;
using BookcaseAPI.Models;
using BookcaseAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookcaseAPI.Tests.Services
{
    public class AuthServiceTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_key_1234567890_1234567890",
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static IConfiguration CreateConfigurationWithoutKey()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        [Fact]
        public void Service_Implements_IAuthService()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            Assert.IsAssignableFrom<IAuthService>(service);
        }

        [Fact]
        public async Task Register_ReturnsNull_WhenUsernameExists()
        {
            using var context = CreateContext();
            context.Clients.Add(new Client { Username = "user", PasswordHash = "hash", Role = "User" });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateConfiguration());

            var result = await service.Register(new RegisterRequest { Username = "user", Password = "pass" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Register_CreatesClientAndReturnsResponse()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            var result = await service.Register(new RegisterRequest { Username = "user", Password = "pass", Role = "Admin" });

            Assert.NotNull(result);
            Assert.Equal("user", result.Username);
            Assert.Equal("Admin", result.Role);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            var client = await context.Clients.SingleAsync();
            Assert.Equal("user", client.Username);
            Assert.Equal("Admin", client.Role);
            Assert.NotEqual("pass", client.PasswordHash);
        }

        [Fact]
        public async Task Register_AssignsUserRole_WhenRoleNotAdmin()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            var result = await service.Register(new RegisterRequest { Username = "user", Password = "pass", Role = "Other" });

            Assert.NotNull(result);
            Assert.Equal("User", result.Role);
        }

        [Fact]
        public async Task Login_ReturnsNull_WhenUserMissing()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            var result = await service.Login(new LoginRequest { Username = "user", Password = "pass" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_ReturnsNull_WhenPasswordInvalid()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            await service.Register(new RegisterRequest { Username = "user", Password = "pass" });

            var result = await service.Login(new LoginRequest { Username = "user", Password = "wrong" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_ReturnsResponse_WhenValid()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            await service.Register(new RegisterRequest { Username = "user", Password = "pass" });

            var result = await service.Login(new LoginRequest { Username = "user", Password = "pass" });

            Assert.NotNull(result);
            Assert.Equal("user", result.Username);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
        }

        [Fact]
        public void GenerateJwtToken_ReturnsToken()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfiguration());

            var token = service.GenerateJwtToken("user", "User", 1);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateJwtToken_Throws_WhenKeyMissing()
        {
            using var context = CreateContext();
            var service = new AuthService(context, CreateConfigurationWithoutKey());

            var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateJwtToken("user", "User", 1));
            Assert.Equal("JWT Key not configured", ex.Message);
        }
    }
}
using BookcaseAPI.DTOs;
using Xunit;

namespace BookcaseAPI.Tests.DTOs
{
    public class DtoTests
    {
        [Fact]
        public void AuthResponse_Defaults_AreEmptyStrings()
        {
            var dto = new AuthResponse();

            Assert.Equal(string.Empty, dto.Token);
            Assert.Equal(string.Empty, dto.Username);
            Assert.Equal(string.Empty, dto.Role);
        }

        [Fact]
        public void AuthResponse_Allows_SettingProperties()
        {
            var dto = new AuthResponse
            {
                Token = "token",
                Username = "user",
                Role = "Admin"
            };

            Assert.Equal("token", dto.Token);
            Assert.Equal("user", dto.Username);
            Assert.Equal("Admin", dto.Role);
        }

        [Fact]
        public void LoginRequest_Defaults_AreEmptyStrings()
        {
            var dto = new LoginRequest();

            Assert.Equal(string.Empty, dto.Username);
            Assert.Equal(string.Empty, dto.Password);
        }

        [Fact]
        public void LoginRequest_Allows_SettingProperties()
        {
            var dto = new LoginRequest
            {
                Username = "user",
                Password = "pass"
            };

            Assert.Equal("user", dto.Username);
            Assert.Equal("pass", dto.Password);
        }

        [Fact]
        public void RegisterRequest_Defaults_AreExpected()
        {
            var dto = new RegisterRequest();

            Assert.Equal(string.Empty, dto.Username);
            Assert.Equal(string.Empty, dto.Password);
            Assert.Equal("User", dto.Role);
        }

        [Fact]
        public void RegisterRequest_Allows_SettingProperties()
        {
            var dto = new RegisterRequest
            {
                Username = "user",
                Password = "pass",
                Role = "Admin"
            };

            Assert.Equal("user", dto.Username);
            Assert.Equal("pass", dto.Password);
            Assert.Equal("Admin", dto.Role);
        }
    }
}
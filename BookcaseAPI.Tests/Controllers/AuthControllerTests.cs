using BookcaseAPI.Controllers;
using BookcaseAPI.DTOs;
using BookcaseAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BookcaseAPI.Tests.Controllers
{
    public class AuthControllerTests
    {
        private sealed class FakeAuthService : IAuthService
        {
            private readonly AuthResponse? _registerResponse;
            private readonly AuthResponse? _loginResponse;

            public FakeAuthService(AuthResponse? registerResponse, AuthResponse? loginResponse)
            {
                _registerResponse = registerResponse;
                _loginResponse = loginResponse;
            }

            public Task<AuthResponse?> Register(RegisterRequest request) => Task.FromResult(_registerResponse);

            public Task<AuthResponse?> Login(LoginRequest request) => Task.FromResult(_loginResponse);

            public string GenerateJwtToken(string username, string role, int userId) => "token";
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserExists()
        {
            var controller = new AuthController(new FakeAuthService(registerResponse: null, loginResponse: null));

            var result = await controller.Register(new RegisterRequest { Username = "u", Password = "p" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenCreated()
        {
            var response = new AuthResponse { Username = "u", Role = "User", Token = "t" };
            var controller = new AuthController(new FakeAuthService(response, loginResponse: null));

            var result = await controller.Register(new RegisterRequest { Username = "u", Password = "p" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalid()
        {
            var controller = new AuthController(new FakeAuthService(registerResponse: null, loginResponse: null));

            var result = await controller.Login(new LoginRequest { Username = "u", Password = "p" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenValid()
        {
            var response = new AuthResponse { Username = "u", Role = "User", Token = "t" };
            var controller = new AuthController(new FakeAuthService(registerResponse: null, loginResponse: response));

            var result = await controller.Login(new LoginRequest { Username = "u", Password = "p" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }
    }
}
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Systeminfo;
using Api.Infrastructure;
using Bookmarks.Tests.Api.Integration;
using FluentAssertions;
using Xunit;

namespace Bookmarks.Tests.Api.Controller
{
    public class AppInfoControllerTests
    {
        const string AppInfoUrl = "/api/v1/appinfo";
        const string JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE4Nzc1MzIyMjEsImp0aSI6IjAxYzNiZTllLWVmZTItNGViMy04ZjUyLTQxMWRmZDI0NDFjNyIsImlhdCI6MTU3NjkyNzQyMSwiaXNzIjoibG9naW4uYmluZ2dsLm5ldCIsInN1YiI6ImEuYkBjLmRlIiwiVHlwZSI6ImxvZ2luLlVzZXIiLCJEaXNwbGF5TmFtZSI6IkRpc3BsYXlOYW1lIiwiRW1haWwiOiJhLmJAYy5kZSIsIlVzZXJJZCI6IlVzZXJJZCIsIlVzZXJOYW1lIjoiVXNlck5hbWUiLCJHaXZlbk5hbWUiOiJVc2VyIiwiU3VybmFtZSI6Ik5hbWUiLCJDbGFpbXMiOlsiYm9va21hcmtzfGh0dHA6Ly9sb2NhbGhvc3Q6MzAwMHxBZG1pbjtVc2VyIl19.phhEJYyFIpNioH-68ypphKYS3gC373U1duHNhcupH2w";

        JsonSerializerOptions jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Fact]
        public async Task TestGetAppInfo()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory<Startup>();
            factory.Registrations = services => {
                // services
            };
            var client = factory.CreateClient();

            // Act
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {JwtToken}");
            var response = await client.GetAsync(AppInfoUrl);

            // Assert
            response
                .Should()
                .NotBeNull();
            response.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            var responseString = await response.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<AppInfo>(responseString, jsonOpts);
            item.UserInfo.DisplayName
                .Should()
                .Be("DisplayName");
            item.UserInfo.UserName
                .Should()
                .Be("UserName");

        }

         [Fact]
        public async Task TestGetAppInfo_NoAuth()
        {
            // Arrange
            var factory = new CustomWebApplicationFactory<Startup>();
            factory.Registrations = services => {
                // services
            };
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(AppInfoUrl);

            // Assert
            response
                .Should()
                .NotBeNull();
            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Unauthorized);

        }
    }
}

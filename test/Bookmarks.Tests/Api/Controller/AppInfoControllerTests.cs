using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Controllers.Systeminfo;
using Api.Infrastructure;
using Bookmarks.Tests.Api.Controller.Fixtures;
using Bookmarks.Tests.Api.Integration;
using FluentAssertions;
using Xunit;

namespace Bookmarks.Tests.Api.Controller
{
    public class AppInfoControllerTests : IClassFixture<JwtFixtures>, IClassFixture<JsonFixtures>
    {
        readonly JwtFixtures _jwt;
        readonly JsonFixtures _json;
        const string AppInfoUrl = "/api/v1/appinfo";

        public AppInfoControllerTests(JwtFixtures jwt, JsonFixtures json)
        {
            _jwt = jwt;
            _json = json;
        }

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
            client.DefaultRequestHeaders.Authorization = _jwt.AuthHeader;
            var response = await client.GetAsync(AppInfoUrl);

            // Assert
            response
                .Should()
                .NotBeNull();
            response.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            var responseString = await response.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<AppInfo>(responseString, _json.JsonOpts);
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

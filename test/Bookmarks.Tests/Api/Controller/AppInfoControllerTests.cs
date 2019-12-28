using System.Net;
using System.Threading.Tasks;
using Api.Controllers.Systeminfo;
using Api.Infrastructure;
using Bookmarks.Tests.Api.Controller.Fixtures;
using Bookmarks.Tests.Api.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Store;
using Xunit;

namespace Bookmarks.Tests.Api.Controller
{
    public class AppInfoControllerTests : IClassFixture<ControllerFixtures>
    {
        readonly ControllerFixtures _controller;
        const string AppInfoUrl = "/api/v1/appinfo";

        public AppInfoControllerTests(ControllerFixtures controller)
        {
            _controller = controller;
        }

        ILogger<AppInfoController> Logger => Mock.Of<ILogger<AppInfoController>>();

        [Fact]
        public void TestGetAppInfo()
        {
            // Arrange
            var controller = new AppInfoController(Logger);
            controller.ControllerContext = _controller.Context;

            // Act
            var appInfo = controller.Get();

            // Assert
            appInfo
                .Should()
                .NotBeNull();
            appInfo.UserInfo.DisplayName
                .Should()
                .Be("DisplayName");
            appInfo.UserInfo.UserName
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
                services.AddDbContextPool<BookmarkContext>(options => {
                    options.UseSqlite("Data Source=:memory:");
                });
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

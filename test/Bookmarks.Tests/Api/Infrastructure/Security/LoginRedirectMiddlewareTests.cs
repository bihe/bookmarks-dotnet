using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Api.Infrastructure.Security.Middleware;
using Microsoft.Extensions.Options;
using Api.Infrastructure.Security;
using Api.Infrastructure.Security.Exceptions;
using FluentAssertions;
using System.Net;

namespace Bookmarks.Tests.Api.Infrastructure.Security
{
    public class LoginRedirectMiddlewareTests
    {
        [Theory]
        [InlineData("LoginChallengeException")]
        [InlineData("AuthorizationException")]
        public async Task TestMiddleware_StandardBehavior(string exceptionType)
        {
            // Arrange
            var logger = Mock.Of<ILogger<LoginRedirectMiddleware>>();
            JwtSettings jwt = new JwtSettings {
                CookieName = "cookiename",
                Issuer = "issuer",
                LoginRedirect = "http://redirect",
                Secret = "secret",
                Claims = new Claim{
                    Name = "claim",
                    Url = "http:/claim",
                    Roles = new string[]{"role1"}
                }
            };
            IOptions<JwtSettings> settings = Options.Create<JwtSettings>(jwt);
            var middleware = new LoginRedirectMiddleware((innerHttpContext) =>
            {
                switch(exceptionType)
                {
                    case "LoginChallengeException": throw new LoginChallengeException("error");
                    case "AuthorizationException": throw new AuthorizationException("error");
                }
                throw new System.Exception("error");

            }, logger, settings);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            // Assert
            context.Response.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Redirect);

            context.Response.Headers["Location"]
                .Should().Contain(x => x == jwt.LoginRedirect);
        }
    }
}

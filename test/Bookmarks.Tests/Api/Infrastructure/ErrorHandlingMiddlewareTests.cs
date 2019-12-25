using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Infrastructure;
using Api.Infrastructure.Security.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Bookmarks.Tests.Api.Infrastructure
{
    public class ErrorHandlingMiddlewareTests
    {
        [Fact]
        public async Task TestMiddleware_StandardBehavior()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ErrorHandlingMiddleware>>();
            var middleware = new ErrorHandlingMiddleware((innerHttpContext) =>
            {
                throw new System.Exception("Error", new System.Exception("inner"));

            }, logger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var streamText = reader.ReadToEnd();
            var pd = JsonSerializer.Deserialize<ProblemDetails>(streamText);

            // Assert
            pd.Type.Should().Be("about:blank");
            pd.Title.Should().StartWith("error during request: ");
            pd.Status.Should().Be(500);
            pd.Detail.Should().Be("Error, inner");

            context.Response.StatusCode
            .Should()
            .Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task TestMiddleware_HttpAcceptHtml()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ErrorHandlingMiddleware>>();
            var middleware = new ErrorHandlingMiddleware((innerHttpContext) =>
            {
                throw new System.Exception("Error", new System.Exception("inner"));

            }, logger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var headerDict = new HeaderDictionary();
            var accept = @"text/html; q=0.5, application/json, text/x-dvi; q=0.8, text/x-c";
            context.Request.Headers.Add(KeyValuePair.Create<string,StringValues>("Accept", accept));

            // Act
            await middleware.Invoke(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var streamText = reader.ReadToEnd();

            // Assert
            context.Response.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Redirect);

            context.Response.Headers["Location"]
                .Should().Contain(x => x == "/Error");
        }

        [Fact]
        public async Task TestMiddleware_LoginChallengeException()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ErrorHandlingMiddleware>>();
            var middleware = new ErrorHandlingMiddleware((innerHttpContext) =>
            {
                throw new LoginChallengeException("login");

            }, logger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act && Assert
            await Assert.ThrowsAsync<LoginChallengeException>(() => {
                return middleware.Invoke(context);
            });
        }

        [Fact]
        public async Task TestMiddleware_AuthorizationException()
        {
            // Arrange
            var logger = Mock.Of<ILogger<ErrorHandlingMiddleware>>();
            var middleware = new ErrorHandlingMiddleware((innerHttpContext) =>
            {
                throw new AuthorizationException("login");

            }, logger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act && Assert
            await Assert.ThrowsAsync<AuthorizationException>(() => {
                return middleware.Invoke(context);
            });
        }

    }
}

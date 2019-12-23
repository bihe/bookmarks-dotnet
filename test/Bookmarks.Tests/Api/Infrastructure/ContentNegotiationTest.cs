using System.Collections.Generic;
using Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Bookmarks.Tests.Api.Infrastructure
{
    public class ContentNegotiationTest
    {
        [Theory]
        [InlineData(@"text/html; q=0.5, application/json, text/x-dvi; q=0.8, text/x-c", "text/html", true)]
        [InlineData(@"text/html", "text/html", true)]
        [InlineData(@"application/json", "text/html", false)]
        [InlineData(@"text/plain", "text/html", false)]
        public void TestContentNegotiation(string accept, string acceptable, bool result)
        {
            var headerDict = new HeaderDictionary();
            headerDict.Add(KeyValuePair.Create<string,StringValues>("Accept", accept));
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Headers).Returns(headerDict);

            var acc = ContentNegotiation.IsAcceptable(mockRequest.Object, acceptable);
            Assert.Equal(result, acc);
        }
    }
}

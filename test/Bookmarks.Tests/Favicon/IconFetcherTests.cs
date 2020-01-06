using System.Net.Http;
using System.Threading.Tasks;
using Api.Favicon;
using FluentAssertions;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookmarks.Tests.Favicon
{
    public class IconFetcherTests
    {
        const string TransparentPixelBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+P+/HgAFhAJ/wlseKgAAAABJRU5ErkJggg==";

        ILogger<IconFetcher> Logger => Mock.Of<ILogger<IconFetcher>>();

        byte[] Image => System.Text.Encoding.UTF8.GetBytes(TransparentPixelBase64);

        [Fact]
        public async Task TestGetFaviconFromUrl()
        {
            string html = @"
            <html>
                <head>
                    <meta charset=""utf-8"">
                    <link rel=""shortcut icon"" href=""http://a.b.c.de/img/favicon.png"">
                </head>
                <body>html</body>
            </html>
            ";

            using (var httpTest = new HttpTest())
            {
                // arrange
                var fetcher = new IconFetcher(Logger);

                httpTest
                    .RespondWith(html)                      // first call to fetch the html
                    .RespondWith(buildContent: () => {      // second call to get the payload
                        return new ByteArrayContent(Image);
                    });

                // act
                var result = await fetcher.GetFaviconFromUrl("http://a.b.c.de");

                // assert
                result.filename
                    .Should().Be("favicon.png");
                result.payload
                    .Should().NotBeNull();
                result.payload.Length
                    .Should().Be(Image.Length);
            }
        }

        [Fact]
        public async Task TestGetFaviconFromUrl_MissingScheme()
        {
            string html = @"
            <html>
                <head>
                    <meta charset=""utf-8"">
                    <link rel=""shortcut icon"" href=""//a.b.c.de/img/favicon.png"">
                </head>
                <body>html</body>
            </html>
            ";

            using (var httpTest = new HttpTest())
            {
                // arrange
                var fetcher = new IconFetcher(Logger);

                httpTest
                    .RespondWith(html)                      // first call to fetch the html
                    .RespondWith(buildContent: () => {      // second call to get the payload
                        return new ByteArrayContent(Image);
                    });

                // act
                var result = await fetcher.GetFaviconFromUrl("https://a.b.c.de");

                // assert
                httpTest.ShouldHaveCalled("https://a.b.c.de");
                httpTest.ShouldHaveCalled("https://a.b.c.de/img/favicon.png");

                result.filename
                    .Should().Be("favicon.png");
                result.payload
                    .Should().NotBeNull();
                result.payload.Length
                    .Should().Be(Image.Length);
            }
        }

        [Fact]
        public async Task TestGetFaviconFromUrl_MissingBaseUrl()
        {
            string html = @"
            <html>
                <head>
                    <meta charset=""utf-8"">
                    <link rel=""shortcut icon"" href=""/img/favicon.png"">
                </head>
                <body>html</body>
            </html>
            ";

            using (var httpTest = new HttpTest())
            {
                // arrange
                var fetcher = new IconFetcher(Logger);

                httpTest
                    .RespondWith(html)                      // first call to fetch the html
                    .RespondWith(buildContent: () => {      // second call to get the payload
                        return new ByteArrayContent(Image);
                    });

                // act
                var result = await fetcher.GetFaviconFromUrl("https://a.b.c.de");

                // assert
                httpTest.ShouldHaveCalled("https://a.b.c.de");
                httpTest.ShouldHaveCalled("https://a.b.c.de/img/favicon.png");

                result.filename
                    .Should().Be("favicon.png");
                result.payload
                    .Should().NotBeNull();
                result.payload.Length
                    .Should().Be(Image.Length);
            }
        }

        [Fact]
        public async Task TestGetFaviconFromUrl_NoHtmlLinkUseBaseFavicon()
        {
            string html = @"
            <html>
                <head>
                    <meta charset=""utf-8"">
                </head>
                <body>html</body>
            </html>
            ";

            using (var httpTest = new HttpTest())
            {
                // arrange
                var fetcher = new IconFetcher(Logger);

                httpTest
                    .RespondWith(html)                      // first call to fetch the html
                    .RespondWith(buildContent: () => {      // second call to get the payload
                        return new ByteArrayContent(Image);
                    });

                // act
                var result = await fetcher.GetFaviconFromUrl("https://a.b.c.de/a/b/c");

                // assert
                httpTest.ShouldHaveCalled("https://a.b.c.de");
                httpTest.ShouldHaveCalled("https://a.b.c.de/favicon.ico");

                result.filename
                    .Should().Be("favicon.ico");
                result.payload
                    .Should().NotBeNull();
                result.payload.Length
                    .Should().Be(Image.Length);
            }
        }

        [Fact]
        public async Task TestGetFaviconFromUrl_RelativePath()
        {
            string html = @"
            <html>
                <head>
                    <meta charset=""utf-8"">
                    <link rel=""shortcut icon"" href=""./img/favicon.png"">
                </head>
                <body>html</body>
            </html>
            ";

            using (var httpTest = new HttpTest())
            {
                // arrange
                var fetcher = new IconFetcher(Logger);

                httpTest
                    .RespondWith(html)                      // first call to fetch the html
                    .RespondWith(buildContent: () => {      // second call to get the payload
                        return new ByteArrayContent(Image);
                    });

                // act
                var result = await fetcher.GetFaviconFromUrl("https://a.b.c.de");

                // assert
                httpTest.ShouldHaveCalled("https://a.b.c.de");
                httpTest.ShouldHaveCalled("https://a.b.c.de/img/favicon.png");

                result.filename
                    .Should().Be("favicon.png");
                result.payload
                    .Should().NotBeNull();
                result.payload.Length
                    .Should().Be(Image.Length);
            }
        }

    }
}

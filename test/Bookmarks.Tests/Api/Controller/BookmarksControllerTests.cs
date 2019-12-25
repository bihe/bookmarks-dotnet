using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Controllers.Bookmarks;
using Api.Infrastructure;
using Bookmarks.Tests.Api.Controller.Fixtures;
using Bookmarks.Tests.Api.Integration;
using Bookmarks.Tests.Store.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using persistence = Store;

namespace Bookmarks.Tests.Api.Controller
{
    public class BookmarksControllerTests : TestDbProvider, IClassFixture<ControllerFixtures>
     {
        persistence.BookmarkContext _context = null;
        persistence.IBookmarkRepository _repo = null;
        readonly ControllerFixtures _fixtures;
        const string BookmarksBaseUrl = "/api/v1/bookmarks";

        public BookmarksControllerTests(ControllerFixtures fixtures)
        {
            _context = SetupDbContext(nameof(BookmarksControllerTests));
            _context.Database.EnsureCreated();
            _context.Bookmarks.RemoveRange(_context.Bookmarks);
            _context.SaveChanges();

            var logger = Mock.Of<ILogger<persistence.DbBookmarkRepository>>();
            _repo = new persistence.DbBookmarkRepository(_context, logger);

            _fixtures = fixtures;
        }

        [Fact]
        public async Task TestCreateBookmarks()
        {
            // Arrange
            var logger = Mock.Of<ILogger<BookmarksController>>();
            var controller = new BookmarksController(logger, _repo);

            var bookmark = new BookmarkModel{
                DisplayName = "Test",
                Path = "/",
                Type = ItemType.Node,
                Url = "http://a.b.c.de"
            };

            // arrange
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Create(bookmark);

            // Assert
            result
                .Should()
                .NotBeNull();
            var created = result.As<CreatedResult>();

            created.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Created);
        }
     }
}

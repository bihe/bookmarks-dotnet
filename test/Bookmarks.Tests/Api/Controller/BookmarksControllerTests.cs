using System;
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
using Store;
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

        BookmarkModel BookMark => new BookmarkModel{
                DisplayName = "Test",
                Path = "/",
                Type = global::Api.Controllers.Bookmarks.ItemType.Node,
                Url = "http://a.b.c.de"
            };

        ILogger<BookmarksController> Logger => Mock.Of<ILogger<BookmarksController>>();


        [Fact]
        public async Task TestCreateBookmarks()
        {
            // Arrange
            var controller = new BookmarksController(Logger, _repo);
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Create(BookMark);

            // Assert
            result
                .Should()
                .NotBeNull();
            var created = result.As<CreatedResult>();

            created.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Created);
        }

        [Fact]
        public async Task TestCreateBookmarks_MissingValues()
        {
            // Arrange
            var controller = new BookmarksController(Logger, _repo);
            controller.ControllerContext = _fixtures.Context;
            var bookmark = BookMark;
            bookmark.Path = string.Empty;

            // Act
            var result = await controller.Create(bookmark);

            // Assert
            result
                .Should()
                .NotBeNull();
            var created = result.As<ObjectResult>();
            var problem = (ProblemDetails)created.Value;
            problem
                .Should()
                .NotBeNull();
            problem.Status
                .Should()
                .Be((int)HttpStatusCode.BadRequest);
            problem.Title
                .Should()
                .Be(Errors.InvalidRequestError);
        }

         [Fact]
        public async Task TestCreateBookmarks_Exception()
        {
            // Arrange
            var repo = new MockDBRepoException();

            var controller = new BookmarksController(Logger, repo);
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Create(BookMark);

            // Assert
            result
                .Should()
                .NotBeNull();
            var created = result.As<ObjectResult>();
            var problem = (ProblemDetails)created.Value;
            problem
                .Should()
                .NotBeNull();
            problem.Status
                .Should()
                .Be((int)HttpStatusCode.InternalServerError);
            problem.Title
                .Should()
                .Be(Errors.CreateBookmarksError);
        }
    }

    internal class MockDBRepoException : MockDBRepo
    {
        public override Task<BookmarkEntity> Create(BookmarkEntity item)
        {
            throw new Exception("error!");
        }
    }
}

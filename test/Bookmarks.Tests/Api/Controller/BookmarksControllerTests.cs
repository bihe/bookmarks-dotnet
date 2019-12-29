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

        [Fact]
        public async Task TestGetBookmarks()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetById("id");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<BookmarkModel>();

            bm.DisplayName
                .Should()
                .Be("DisplayName");
            bm.Path
                .Should()
                .Be("/");
            bm.Url
                .Should()
                .Be("http://a.b.c.de");
        }

        [Fact]
        public async Task TestGetBookmarks_EmptyId()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetById("");

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
        public async Task TestGetBookmarks_NotFound()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetById("_unknown_");

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
                .Be((int)HttpStatusCode.NotFound);
            problem.Title
                .Should()
                .Be(Errors.NotFoundError);
        }

        [Fact]
        public async Task TestUpdateBookmarks()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;
            var bm = BookMark;
            bm.Id = "id";
            bm.DisplayName = bm.DisplayName + "_updated";

            // Act
            var result = await controller.Update(bm);

            // Assert
            result
                .Should()
                .NotBeNull();
            var update = result.As<OkObjectResult>();

            update.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);

            var bookmarkResult = update.Value.As<Result<string>>();
            bookmarkResult.Value
                .Should()
                .Be("id");
        }

        [Fact]
        public async Task TestUpdateBookmarks_MissingId()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;
            var bm = BookMark;
            bm.Id = "";
            bm.DisplayName = bm.DisplayName + "_updated";

            // Act
            var result = await controller.Update(bm);

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
        public async Task TestUpdateBookmarks_Exception()
        {
            // Arrange
            var controller = new BookmarksController(Logger, new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;
            var bm = BookMark;
            bm.Id = "exception";
            bm.DisplayName = bm.DisplayName + "_updated";

            // Act
            var result = await controller.Update(bm);

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
                .Be(Errors.UpdateBookmarksError);
        }

    }

    internal class MockDBRepoException : MockDBRepo
    {
        public override Task<BookmarkEntity> Create(BookmarkEntity item)
        {
            throw new Exception("error!");
        }
    }

    internal class MockDbRepo : MockDBRepo
    {
        public override async Task<(bool result, T value)> InUnitOfWorkAsync<T>(Func<Task<(bool result, T value)>> atomicOperation)
        {
            return await Task.Run(() => {
                return atomicOperation();
            });
        }

        public override async Task<BookmarkEntity> GetBookmarkById(string id, string username)
        {
            return await Task.Run(() => {
                if (id == "id" || id == "exception")
                {
                    return new BookmarkEntity{
                        ChildCount = 0,
                        Created = DateTime.UtcNow,
                        DisplayName = "DisplayName",
                        Id = "id",
                        Path = "/",
                        SortOrder = 0,
                        Type = persistence.ItemType.Node,
                        Url = "http://a.b.c.de",
                        UserName = username
                    };
                }
                return null;
            });
        }

        public override async Task<BookmarkEntity> Update(BookmarkEntity item)
        {
            return await Task.Run(() => {
                if (item.Id == "id")
                {
                    return new BookmarkEntity{
                        ChildCount = 0,
                        Created = DateTime.UtcNow,
                        Modified = DateTime.UtcNow,
                        Id = "id",
                        Type = persistence.ItemType.Node,
                        Url = item.Url,
                        SortOrder = item.SortOrder,
                        Path = item.Path,
                        DisplayName = item.DisplayName,
                        UserName = item.UserName
                    };
                }
                else if (item.Id == "exception")
                {
                    throw new Exception("error");
                }
                return null;
            });
        }
    }
}

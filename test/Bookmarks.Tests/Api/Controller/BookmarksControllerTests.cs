using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Api.Controllers.Bookmarks;
using Bookmarks.Tests.Api.Controller.Fixtures;
using Bookmarks.Tests.Store.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

        BookmarksController CreateController(IBookmarkRepository repository)
        {
            return new BookmarksController(Logger, repository, null, null, null, null);
        }


        [Fact]
        public async Task TestCreateBookmarks()
        {
            // Arrange
            var controller = CreateController(_repo);
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
            var controller = CreateController(_repo);
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
            var controller = CreateController(new MockDBRepoException());
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
            var controller = CreateController(new MockDbRepo());
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
            var controller = CreateController(new MockDbRepo());
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
            var controller = CreateController(new MockDbRepo());
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
            var controller = CreateController(new MockDbRepo());
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
            var controller = CreateController(new MockDbRepo());
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
            var controller = CreateController(new MockDbRepo());
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

        [Fact]
        public async Task TestUpdateBookmarks_RenameBug()
        {
            // Arrange
            var controller = CreateController(_repo);
            controller.ControllerContext = _fixtures.Context;

            // 1) create a folder in root
            var rootFolder = new BookmarkModel{
                DisplayName = "Folder",
                Path = "/",
                Type = global::Api.Controllers.Bookmarks.ItemType.Folder,
            };
            var result = await controller.Create(rootFolder);
            var create = result.As<CreatedResult>();
            create.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Created);
            var bookmarkResult = create.Value.As<Result<string>>();
            var folderId = bookmarkResult.Value;

            // 2) create a node in the folder
            var node = new BookmarkModel{
                DisplayName = "Node",
                Path = "/Folder",
                Type = global::Api.Controllers.Bookmarks.ItemType.Node,
                Url = "url"
            };
            result = await controller.Create(node);
            create = result.As<CreatedResult>();
            create.StatusCode
                .Should()
                .Be((int)HttpStatusCode.Created);

            // 3) rename the root-folder
            result = await controller.Update(new BookmarkModel{
                Id = folderId,
                DisplayName = "Folder_renamed",
                Path = "/",
                Type = global::Api.Controllers.Bookmarks.ItemType.Folder,
            });

            result
                .Should()
                .NotBeNull();
            var update = result.As<OkObjectResult>();

            update.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);

            bookmarkResult = update.Value.As<Result<string>>();
            bookmarkResult.Value
                .Should()
                .Be(folderId);

            // 4) get the list of nodes within the folder -- it should return the node from above
            result = await controller.GetBookmarksByPath("/Folder_renamed");
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();
            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(1);
            bm.Value[0].DisplayName
                .Should()
                .Be("Node");
        }

        [Fact]
        public async Task TestDeleteBookmarks()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Delete("id");

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
        public async Task TestDeleteBookmarks_MissingId()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Delete("");

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
        public async Task TestDeleteBookmarks_Exception()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.Delete("exception");

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
                .Be(Errors.DeleteBookmarkError);
        }

        [Fact]
        public async Task TestFindBookmarksByPath()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByPath("/");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task TestFindBookmarksByPath_NoResult()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByPath("no");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(0);
        }

        [Fact]
        public async Task TestFindBookmarksByPath_MissingPath()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByPath("");

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
        public async Task TestFindBookmarksByName()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByName("a");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task TestFindBookmarksByName_NoResult()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByName("no");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(0);
        }

        [Fact]
        public async Task TestFindBookmarksByName_MissingName()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarksByName("");

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
        public async Task TestGetBookmarkFolderByPath()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarkFolderByPath("/path");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<Result<BookmarkModel>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Value.DisplayName
                .Should()
                .Be("Folder");
        }

        [Fact]
        public async Task TestGetBookmarkFolderByPath_IsRoot()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarkFolderByPath("/");

            // Assert
            result
                .Should()
                .NotBeNull();
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<Result<BookmarkModel>>();

            bm.Success
                .Should()
                .Be(true);
            bm.Value.DisplayName
                .Should()
                .Be("Root");
        }

        [Fact]
        public async Task TestGetBookmarkFolderByPath_MissingPath()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarkFolderByPath("");

            // Assert
            result
                .Should()
                .NotBeNull();
            var getfolderResult = result.As<ObjectResult>();
            var problem = (ProblemDetails)getfolderResult.Value;
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
        public async Task TestGetBookmarkFolderByPath_NotFond()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.GetBookmarkFolderByPath("notfound");

           // Assert
            result
                .Should()
                .NotBeNull();
            var getfolderResult = result.As<ObjectResult>();
            var problem = (ProblemDetails)getfolderResult.Value;
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
        public async Task TestFetchAndForward()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.FetchAndForward("id");

            // Assert
            result
                .Should()
                .NotBeNull();
            var redirect = result.As<RedirectResult>();
            redirect.Url
                .Should()
                .Be("http://a.b.c.de");
        }

        [Fact]
        public async Task TestFetchAndForward_InvalidArgument()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.FetchAndForward("");

            // Assert
            result
                .Should()
                .NotBeNull();
            var getfolderResult = result.As<ObjectResult>();
            var problem = (ProblemDetails)getfolderResult.Value;
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
        public async Task TestFetchAndForward_Exception()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.FetchAndForward("exception");

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

        [Fact]
        public async Task TestUpdateSortOrder()
        {
            IBookmarkRepository _CreateRepo()
            {
                var ctxt = SetupDbContext(nameof(BookmarksControllerTests));
                ctxt.Database.EnsureCreated();
                ctxt.Bookmarks.RemoveRange(ctxt.Bookmarks);
                ctxt.SaveChanges();

                var logger = Mock.Of<ILogger<persistence.DbBookmarkRepository>>();
                return new persistence.DbBookmarkRepository(ctxt, logger);
            }

            // Arrange
            var controller = CreateController(_CreateRepo());
            controller.ControllerContext = _fixtures.Context;

            // 1) create 3 items
            for(int i=0; i<3; i++)
            {
                var item = new BookmarkModel{
                    Id = $"id{i}",
                    DisplayName = $"item{i}",
                    Path = "/",
                    Type = global::Api.Controllers.Bookmarks.ItemType.Folder,
                };
                var r = await controller.Create(item);
                var c = r.As<CreatedResult>();
                c.StatusCode
                    .Should()
                    .Be((int)HttpStatusCode.Created);
            }

            // 2) get the items for path '/'
            var result = await controller.GetBookmarksByPath("/");
            var ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var bm = ok.Value.As<ListResult<List<BookmarkModel>>>();
            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(3);
            bm.Value[0].DisplayName
                .Should()
                .Be("item0");
            bm.Value[2].DisplayName
                .Should()
                .Be("item2");

            // 3) change the sort-order
            var sorting = new BookmarksSortOrderModel {
                Ids = new List<string> {
                    "id2",
                    "id0",
                    "id1"
                },
                SortOrder = new List<int> {
                    1,
                    2,
                    3
                }
            };

            result = await controller.UpdateSortOrder(sorting);
            ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            var updateResult = ok.Value.As<Result<string>>();
            updateResult.Success
                .Should()
                .Be(true);

            // 4) get the bookmark list again
            result = await controller.GetBookmarksByPath("/");
            ok = result.As<OkObjectResult>();
            ok.StatusCode
                .Should()
                .Be((int)HttpStatusCode.OK);
            bm = ok.Value.As<ListResult<List<BookmarkModel>>>();
            bm.Success
                .Should()
                .Be(true);
            bm.Count
                .Should()
                .Be(3);
            bm.Value[0].DisplayName
                .Should()
                .Be("item2");
            bm.Value[2].DisplayName
                .Should()
                .Be("item1");
        }

        [Fact]
        public async Task TestUpdateSortOrder_MissingParameters()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.UpdateSortOrder(new BookmarksSortOrderModel {
                Ids = new List<string>(),
                SortOrder = new List<int>()
            });

            // Assert
            result
                .Should()
                .NotBeNull();
            var updateSortOrderResult = result.As<ObjectResult>();
            var problem = (ProblemDetails)updateSortOrderResult.Value;
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
        public async Task TestUpdateSortOrder_UnbalancedParameters()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.UpdateSortOrder(new BookmarksSortOrderModel {
                Ids = new List<string>{
                    "id1", "id2"
                },
                SortOrder = new List<int>{
                    1
                }
            });

            // Assert
            result
                .Should()
                .NotBeNull();
            var updateSortOrderResult = result.As<ObjectResult>();
            var problem = (ProblemDetails)updateSortOrderResult.Value;
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
        public async Task TestUpdateSortOrder_Exception()
        {
            // Arrange
            var controller = CreateController(new MockDbRepo());
            controller.ControllerContext = _fixtures.Context;

            // Act
            var result = await controller.UpdateSortOrder(new BookmarksSortOrderModel {
                Ids = new List<string>{
                    "exception"
                },
                SortOrder = new List<int>{
                    1
                }
            });

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

        async Task<List<BookmarkEntity>> GetBookmarkList(string input, string username)
        {
            return await Task.Run(() => {
                if (input == "no")
                {
                    return null;
                }

                return new List<BookmarkEntity> {
                    new BookmarkEntity{
                        ChildCount = 0,
                        Created = DateTime.UtcNow,
                        DisplayName = "DisplayName",
                        Id = "id1",
                        Path = "/",
                        SortOrder = 0,
                        Type = persistence.ItemType.Node,
                        Url = "http://a.b.c.de",
                        UserName = username
                    },
                     new BookmarkEntity{
                        ChildCount = 0,
                        Created = DateTime.UtcNow,
                        DisplayName = "DisplayName",
                        Id = "id2",
                        Path = "/",
                        SortOrder = 0,
                        Type = persistence.ItemType.Node,
                        Url = "http://a.b.c.de",
                        UserName = username
                    }
                };
            });
        }

        public override async Task<List<BookmarkEntity>> GetBookmarksByPath(string path, string username)
        {
            return await GetBookmarkList(path, username);
        }

        public override async Task<List<BookmarkEntity>> GetBookmarksByName(string name, string username)
        {
            return await GetBookmarkList(name, username);
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
                        Id = id,
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

        public override async Task<BookmarkEntity> GetFolderByPath(string path, string username)
        {
            return await Task.Run(() => {
                if (path == "notfound")
                    return null;
                return new BookmarkEntity{
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "Folder",
                    Id = Guid.NewGuid().ToString(),
                    Path = path,
                    SortOrder = 0,
                    Type = persistence.ItemType.Folder,
                    UserName = username
                };
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

        public override async Task<bool> Delete(BookmarkEntity item)
        {
             return await Task.Run(() => {
                 if (item.Id == "exception")
                 {
                    throw new Exception("error");
                 }
                 return true;
             });
        }
    }
}

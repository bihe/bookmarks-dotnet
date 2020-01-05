using System;
using System.Threading.Tasks;
using Bookmarks.Tests.Store.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Store;
using Xunit;

namespace Bookmarks.Tests.Store
{
    public class StoreTest : TestDbProvider
    {
        BookmarkContext context = null;
        IBookmarkRepository repo = null;

        const string Username = "user";

        public StoreTest()
        {
            context = SetupDbContext(nameof(StoreTest));
            context.Database.EnsureCreated();

            context.Bookmarks.RemoveRange(context.Bookmarks);
            context.SaveChanges();

            var logger = Mock.Of<ILogger<DbBookmarkRepository>>();
            repo = new DbBookmarkRepository(context, logger);
        }

        string NewId
        {
            get
            {
                return Guid.NewGuid().ToString();
            }
        }

        [Fact]
        public async Task TestCreateBookmarkAndGetById()
        {
            Assert.NotNull(context);

            // create one item
            var itemId = NewId;
            var item = await repo.Create(new BookmarkEntity{
                Id = itemId,
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "displayName",
                Path = "/",
                SortOrder = 0,
                Type = ItemType.Node,
                Url = "http://url",
                UserName = Username
            });

            Assert.NotNull(item);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("displayName", item.DisplayName);
            Assert.Equal(Username, item.UserName);

            // get item by id
            var bm = await repo.GetBookmarkById(itemId, Username);
            Assert.NotNull(bm);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("displayName", bm.DisplayName);
            Assert.Equal(Username, bm.UserName);
        }

        [Fact]
        public async Task TestUpdateBookmark()
        {
            Assert.NotNull(context);

            // create one item
            var itemId = NewId;
            var item = await repo.Create(new BookmarkEntity{
                Id = itemId,
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "displayName",
                Path = "/",
                SortOrder = 0,
                Type = ItemType.Node,
                Url = "http://url",
                UserName = Username,
                Favicon = "favicon.ico"
            });

            Assert.NotNull(item);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("displayName", item.DisplayName);
            Assert.Equal(Username, item.UserName);

            // get item by id
            var bm = await repo.GetBookmarkById(itemId, Username);
            Assert.NotNull(bm);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("displayName", bm.DisplayName);
            Assert.Equal(Username, bm.UserName);

            await repo.Update(new BookmarkEntity{
                Id = bm.Id,
                DisplayName = bm.DisplayName + "_changed",
                Path = bm.Path,
                SortOrder = 10,
                Url = "http://new-url",
                UserName = bm.UserName,
                AccessCount = 99,
                Favicon = "favicon1.ico"
            });

            bm = await repo.GetBookmarkById(itemId, Username);
            Assert.NotNull(bm);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("displayName_changed", bm.DisplayName);
            Assert.Equal(Username, bm.UserName);
            Assert.Equal(10, bm.SortOrder);
            Assert.Equal("http://new-url", bm.Url);
            Assert.Equal(99, bm.AccessCount);
            Assert.Equal("favicon1.ico", bm.Favicon);


            // failed update - empty path
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return repo.Update(new BookmarkEntity{
                    Id = bm.Id,
                    DisplayName = bm.DisplayName + "_changed",
                    Path = "",
                    SortOrder = 10,
                    Url = "http://new-url",
                    UserName = bm.UserName
                });
            });

            // failed update - unavalable path
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return repo.Update(new BookmarkEntity{
                    Id = bm.Id,
                    DisplayName = bm.DisplayName + "_changed",
                    Path = "/this/path/is/not/known",
                    SortOrder = 10,
                    Url = "http://new-url",
                    UserName = bm.UserName
                });
            });

            // bookmark not found
            bm = await repo.Update(new BookmarkEntity{
                Id = "some-id",
                DisplayName = bm.DisplayName + "_changed",
                Path = bm.Path,
                SortOrder = 10,
                Url = "http://new-url",
                UserName = bm.UserName
            });
            Assert.Null(bm);

        }

        [Fact]
        public async Task TestCreateFolderHierarchy()
        {
            Assert.NotNull(context);

            // create a folder-hierarchy /A/B/C
            await repo.Create(new BookmarkEntity{
                Id = NewId,
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "A",
                Path = "/",
                SortOrder = 0,
                Type = ItemType.Folder,
                Url = "http://url",
                UserName = Username
            });

            await repo.Create(new BookmarkEntity{
                Id = NewId,
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "B",
                Path = "/A",
                SortOrder = 0,
                Type = ItemType.Folder,
                Url = "http://url",
                UserName = Username
            });

            await repo.Create(new BookmarkEntity{
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "C",
                Path = "/A/B",
                SortOrder = 0,
                Type = ItemType.Folder,
                Url = "http://url",
                UserName = Username
            });

            var bms = await repo.GetBookmarksByPathStart("/A", Username);
            Assert.NotNull(bms);
            Assert.Equal(2, bms.Count);

            bms = await repo.GetAllBookmarks(Username);
            Assert.NotNull(bms);
            Assert.Equal(3, bms.Count);
            Assert.Equal("A", bms[0].DisplayName);
            Assert.Equal("B", bms[1].DisplayName);
            Assert.Equal("C", bms[2].DisplayName);

            bms = await repo.GetBookmarksByName("B",Username);
            Assert.NotNull(bms);
            Assert.Single(bms);
            Assert.Equal("/A", bms[0].Path);

            bms = await repo.GetBookmarksByPath("/A/B",Username);
            Assert.NotNull(bms);
            Assert.Single(bms);
            Assert.Equal("C", bms[0].DisplayName);

            var bm = await repo.GetFolderByPath("/A/B", Username);
            Assert.NotNull(bm);
            Assert.Equal("B", bm.DisplayName);
            Assert.Equal("/A", bm.Path);

            // invalid folder
            bm = await repo.GetFolderByPath("|A|B", Username);
            Assert.Null(bm);

            // use the structure from above and get the child-count of nodes
            // this is just a folder-structure of /A/B/C
            var nodes = await repo.GetChildCountOfPath("/A", Username);
            Assert.NotNull(nodes);
            Assert.Single(nodes);
            Assert.Equal(1, nodes[0].Count);
            Assert.Equal("/A", nodes[0].Path);

            nodes = await repo.GetChildCountOfPath("", Username);
            Assert.NotNull(nodes);
            Assert.Equal(3, nodes.Count);
            Assert.Equal(1, nodes[0].Count);
            Assert.Equal("/", nodes[0].Path);

            // create a node to an existing path
            var nodeID = NewId;
            bm = await repo.Create(new BookmarkEntity {
                Id = nodeID,
                ChildCount = 0,
                Created = DateTime.UtcNow,
                DisplayName = "URL",
                Path = "/A/B",
                SortOrder = 0,
                Type = ItemType.Node,
                Url = "http://url",
                UserName = Username
            });
            Assert.NotNull(bm);

            // get the folder
            bm = await repo.GetFolderByPath("/A/B", Username);
            Assert.NotNull(bm);
            Assert.Equal("B", bm.DisplayName);
            Assert.Equal("/A", bm.Path);
            Assert.Equal(2, bm.ChildCount); // sub-folder and the newly created node

            nodes = await repo.GetChildCountOfPath("/A/B", Username);
            Assert.NotNull(nodes);
            Assert.Single(nodes);
            Assert.Equal(2, nodes[0].Count);
            Assert.Equal("/A/B", nodes[0].Path);

            // unknown path
            nodes = await repo.GetChildCountOfPath("/A/B/C/D/E", Username);
            Assert.Empty(nodes);

            // remove a node
            Assert.True(await repo.Delete(new BookmarkEntity{Id = nodeID, UserName = Username}));

            bm = await repo.GetFolderByPath("/A/B", Username);
            Assert.NotNull(bm);
            Assert.Equal("B", bm.DisplayName);
            Assert.Equal("/A", bm.Path);
            Assert.Equal(1, bm.ChildCount); // sub-folder only

            Assert.False(await repo.Delete(new BookmarkEntity{Id = "-1", UserName = Username}));

            // we have the path /A/B/C
            // if we delete /A/B the only thing left will be /A
            Assert.True(await repo.DeletePath("/A/B", Username));

            bms = await repo.GetAllBookmarks(Username);
            Assert.NotNull(bms);
            Assert.Single(bms);

            await Assert.ThrowsAsync<ArgumentException>(() => {
                return repo.DeletePath("", Username);
            });

            await Assert.ThrowsAsync<ArgumentException>(() => {
                return repo.DeletePath("/", Username);
            });

            Assert.False(await repo.DeletePath("/D/E", Username));
        }

        [Fact]
        public async Task TestInvalidFolderHierarchy()
        {
            // no path at all!
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return repo.Create(new BookmarkEntity{
                    Id = NewId,
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "A",
                    Path = "",
                    SortOrder = 0,
                    Type = ItemType.Folder,
                    Url = "http://url",
                    UserName = Username
                });
            });

            // cannot get the parent path
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return repo.Create(new BookmarkEntity{
                    Id = NewId,
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "A",
                    Path = "/A/B",
                    SortOrder = 0,
                    Type = ItemType.Folder,
                    Url = "http://url",
                    UserName = Username
                });
            });

            // invalid path
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return repo.Create(new BookmarkEntity{
                    Id = NewId,
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "A",
                    Path = "-",
                    SortOrder = 0,
                    Type = ItemType.Folder,
                    Url = "http://url",
                    UserName = Username
                });
            });
        }

        [Fact]
        public async Task TestUnitOfWork()
        {
            Assert.NotNull(context);

            var itemId = NewId;
            await repo.InUnitOfWorkAsync(async () => {
                // create a folder-hierarchy /A/B/C
                var item = await repo.Create(new BookmarkEntity{
                    Id = itemId,
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "A",
                    Path = "/",
                    SortOrder = 0,
                    Type = ItemType.Folder,
                    Url = "http://url",
                    UserName = Username
                });
                // rollback the transaction
                return (false, item);
            });

            var bm = await repo.GetBookmarkById(itemId, Username);
            Assert.Null(bm);

            itemId = NewId;
            await Assert.ThrowsAsync<Exception>( () => {
                return repo.InUnitOfWorkAsync<BookmarkEntity>(async () => {
                    // create a folder-hierarchy /A/B/C
                    await repo.Create(new BookmarkEntity{
                        Id = itemId,
                        ChildCount = 0,
                        Created = DateTime.UtcNow,
                        DisplayName = "A",
                        Path = "/",
                        SortOrder = 0,
                        Type = ItemType.Folder,
                        Url = "http://url",
                        UserName = Username
                    });
                    // rollback the transaction
                    throw new Exception("error");
                });
            });

            bm = await repo.GetBookmarkById(itemId, Username);
            Assert.Null(bm);

            itemId = NewId;
            await repo.InUnitOfWorkAsync(async () => {
                // create a folder-hierarchy /A/B/C
                var item = await repo.Create(new BookmarkEntity{
                    Id = itemId,
                    ChildCount = 0,
                    Created = DateTime.UtcNow,
                    DisplayName = "A",
                    Path = "/",
                    SortOrder = 0,
                    Type = ItemType.Folder,
                    Url = "http://url",
                    UserName = Username
                });
                return (true, item);
            });

            bm = await repo.GetBookmarkById(itemId, Username);
            Assert.NotNull(bm);
        }
    }
}

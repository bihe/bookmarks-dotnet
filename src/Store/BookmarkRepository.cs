using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Store
{
    /// <summary>
    /// IBookmarkRepository is responsible for storing and retrieving boomarks from a store
    /// </summary>
    public interface IBookmarkRepository
    {
        Task<(bool result, T value)> InUnitOfWorkAsync<T>(Func<Task<(bool result,T value)>> atomicOperation);

        Task<List<BookmarkEntity>> GetAllBookmarks(string username);
        Task<List<BookmarkEntity>> GetBookmarksByPath(string path, string username);
        Task<List<BookmarkEntity>> GetBookmarksByPathStart(string startPath, string username);
        Task<List<BookmarkEntity>> GetBookmarksByName(string name, string username);
        Task<List<NodeCount>> GetChildCountOfPath(string path, string username);

        Task<BookmarkEntity> GetFolderByPath(string path, string username);
        Task<BookmarkEntity> GetBookmarkById(string id, string username);
        Task<BookmarkEntity> Create(BookmarkEntity item);
        Task<BookmarkEntity> Update(BookmarkEntity item);

        Task<bool> Delete(BookmarkEntity item);
        Task<bool> DeletePath(string path, string username);
    }

    // implement the Repository with a relational database
    public class DbBookmarkRepository : IBookmarkRepository
    {
        readonly ILogger<DbBookmarkRepository> _logger;

        BookmarkContext _context;

        public DbBookmarkRepository(BookmarkContext context, ILogger<DbBookmarkRepository> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<(bool result, T value)> InUnitOfWorkAsync<T>(Func<Task<(bool result,T value)>> atomicOperation)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    var opOutcome = await atomicOperation();
                    if (!opOutcome.result)
                    {
                        _logger.LogInformation($"The operation outcome was {opOutcome}; therefore a rollback was initiated!");
                        tx.Rollback();
                        return opOutcome;
                    }
                    tx.Commit();
                    return opOutcome;
                }
                catch (Exception EX)
                {
                    tx.Rollback();
                    _logger.LogError($"Could not perform atomic operation: {EX.Message}; Stack: {EX.StackTrace}");
                    throw;
                }
            }
        }

        public async Task<BookmarkEntity> Create(BookmarkEntity item)
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                throw new ArgumentException("path is empty", nameof(item.Path));
            }

            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();

            _logger.LogDebug($"create new bookmark {item}");

            // if we create a new bookmark item using a specific path we need to ensure that
            // the parent-path is available. as this is a hierarchical structure this is quite tedious
            // the solution is to query the whole hierarchy and check if the given path is there

            if (item.Path != "/")
            {
                var hierarchy = await this.AvailablePaths(item.UserName);
                if (!hierarchy.Where(path => path == item.Path).Any())
                {
                    _logger.LogWarning($"cannot create the bookmark {item} because the parent path {item.Path} is not available!");
                    throw new InvalidOperationException($"cannot create item because of missing path hierarchy '{item.Path}'!");
                }
            }
            _context.Bookmarks.Add(item);

            // this entry (either node or folder) was created with a given path. increment the number of child-elements
            // for this given path, and update the "parent" directory entry.
            // exception: if the path is ROOT, '/' no update needs to be done, because no dedicated ROOT, '/' entry
            if (item.Path != "/")
            {
                if (!await this.IncrementChildCount(item.Path, item.UserName))
                {
                    throw new InvalidOperationException($"could not update the child-count for the new item {item}");
                }
            }

            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<BookmarkEntity> Update(BookmarkEntity item)
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                throw new ArgumentException("path is empty", nameof(item.Path));
            }

            var bm = await this.GetBookmarkById(item.Id, item.UserName);
            if (bm == null)
            {
                _logger.LogWarning($"could not find a bookmark the bookmark to update {item}");
                return null;
            }

            // check that the parent-path is available. as this is a hierarchical structure this is quite tedious
            // the solution is to query the whole hierarchy and check if the given path is there
            if (item.Path != "/")
            {
                var hierarchy = await this.AvailablePaths(item.UserName);
                if (!hierarchy.Where(path => path == item.Path).Any())
                {
                    _logger.LogWarning($"cannot create the bookmark {item} because the parent path {item.Path} is not available!");
                    throw new InvalidOperationException($"cannot create item because of missing path hierarchy '{item.Path}'!");
                }
            }

            // use the found bookmark and update it with the supplied values
            bm.DisplayName = item.DisplayName;
            bm.Modified = DateTime.UtcNow;
            bm.Path = item.Path;
            bm.SortOrder = item.SortOrder;
            bm.Url = item.Url;

            // the properties ChildCount, Created, Type, UserName are not touched!

            await _context.SaveChangesAsync();

            // no need to update the child-count as the existing bookmark is just changed, without any
            // oder updates

            return bm;
        }

        public async Task<bool> Delete(BookmarkEntity item)
        {
            var bm = await this.GetBookmarkById(item.Id, item.UserName);
            if (bm == null)
            {
                _logger.LogWarning($"could not find a bookmark the bookmark to update {item}");
                return false;
            }

            // one item is removed from a given path, decrement the child-count for
            // the folder / path this item is located in
            if (bm.Path != "/")
            {
                if (!await this.DecrementChildCount(bm.Path, bm.UserName))
                {
                    throw new InvalidOperationException($"could not update the child-count for the new item {item}");
                }
            }

            _context.Remove(bm);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePath(string path, string username)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path is empty", nameof(path));
            }

            if (path == "/")
            {
                throw new ArgumentException("cannot delete root path '/'", nameof(path));
            }

            var q = from b in _context.Bookmarks where
                b.UserName.ToLower() == username.ToLower() &&
                b.Path.StartsWith(path)
                select b;

            if (!await q.AnyAsync())
            {
                _logger.LogInformation($"no bookmarks available for path {path}");
                return false;
            }

            _context.RemoveRange(q);

            // also the folder needs to be deleted
            var folder = await GetFolderByPath(path, username);
            if (folder != null)
            {
                _context.Remove(folder);
            }

            await _context.SaveChangesAsync();

            var (parentPath, folderName, ok) = PathAndFolder(path);
            if (!ok)
            {
                _logger.LogWarning($"could not get parent-path/folder of given path {path}");
            }

            // if we "reached" the root path no need to update anything
            if (parentPath != "/")
            {
                var parentFolder = await GetFolderByPath(parentPath, username);
                var childCount = await GetChildCountOfPath(parentPath, username);
                if (childCount.Count > 0)
                    parentFolder.ChildCount = childCount[0].Count;
                else
                    parentFolder.ChildCount = 0;

                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<BookmarkEntity> GetBookmarkById(string id, string username)
        {
            var q = from b in _context.Bookmarks where b.UserName.ToLower() == username.ToLower() && b.Id == id select b;
            return await q.FirstOrDefaultAsync();
        }

        public async Task<BookmarkEntity> GetFolderByPath(string path, string username)
        {
            var (parent, folder, ok) = PathAndFolder(path);
            if (!ok)
            {
                _logger.LogWarning($"could not get parent/folder of path {path}");
                return null;
            }

            var q = from b in _context.Bookmarks where
                b.UserName.ToLower() == username.ToLower() &&
                b.Path == parent &&
                b.DisplayName == folder &&
                b.Type == ItemType.Folder
                select b;

            return await q.FirstOrDefaultAsync();
        }

        public async Task<List<BookmarkEntity>> GetBookmarksByPathStart(string startPath, string username)
        {
            var q = from b in _context.Bookmarks where b.UserName.ToLower() == username.ToLower() && b.Path.StartsWith(startPath) select b;
            q = q.OrderBy(b => b.SortOrder).OrderBy(b => b.DisplayName);
            return await q.ToListAsync();
        }

        public async Task<List<BookmarkEntity>> GetAllBookmarks(string username)
        {
            var q = from b in _context.Bookmarks where b.UserName.ToLower() == username.ToLower() select b;
            q = q.OrderBy(b => b.Path).OrderBy(b => b.SortOrder).OrderBy(b => b.DisplayName);
            return await q.ToListAsync();
        }

        public async Task<List<BookmarkEntity>> GetBookmarksByName(string name, string username)
        {
            var q = from b in _context.Bookmarks where b.UserName.ToLower() == username.ToLower() && b.DisplayName == name select b;
            q = q.OrderBy(b => b.SortOrder).OrderBy(b => b.DisplayName);
            return await q.ToListAsync();
        }

        public async Task<List<BookmarkEntity>> GetBookmarksByPath(string path, string username)
        {
            var q = from b in _context.Bookmarks where b.UserName.ToLower() == username.ToLower() && b.Path == path select b;
            q = q.OrderBy(b => b.SortOrder).OrderBy(b => b.DisplayName);
            return await q.ToListAsync();
        }

        public async Task<List<NodeCount>> GetChildCountOfPath(string path, string username)
        {
            var nodes = await GetPathChildCount(path, username);
            if (nodes == null || !nodes.Any())
                return new List<NodeCount>();
            return nodes.ToList();
        }

        // internal logic && helpers

        async Task<bool> IncrementChildCount(string path, string username)
        {
            return await CalcChildCount(path, username, () => +1);
        }

        async Task<bool> DecrementChildCount(string path, string username)
        {
            return await CalcChildCount(path, username, () => -1);
        }

        async Task<bool> CalcChildCount(string path, string username, Func<int> fn)
        {
            // the supplied path is of the form
            // /A/B/C => get the entry C (which is a folder) and increment the child-count
            var (parentPath, parentName, ok) = PathAndFolder(path);
            if (!ok)
            {
                _logger.LogWarning($"invalid path encountered {path}");
                return false;
            }

            var q = from b in _context.Bookmarks where
                b.UserName.ToLower() == username.ToLower() &&
                b.Path == parentPath &&
                b.Type == ItemType.Folder &&
                b.DisplayName == parentName select b;

            if (!await q.AnyAsync())
            {
                _logger.LogWarning($"could not get parent item {parentPath}, {parentName}!");
                return false;
            }
            var parent = await q.FirstAsync();

            // increment the parent item
            parent.ChildCount += fn();
            await _context.SaveChangesAsync();
            return true;
        }

        // decompose the path to path and name
        (string path, string folder, bool valid) PathAndFolder(string path)
        {
            var i = path.LastIndexOf("/");
            if (i == -1)
            {
                return ("","", false);
            }

            var parent = path.Substring(0, i);
            if (i == 0 || parent == "")
            {
                parent = "/";
            }
            var name = path.Substring(i+1);
            return (parent, name, true);
        }

        // query for all available paths by combining parent path and directory name
        const string hierarchyQuery = @"SELECT '/' as path

                UNION ALL

                SELECT
                    CASE ii.path
                        WHEN '/' THEN ''
                        ELSE ii.path
                    END || '/' || ii.display_name AS path
                FROM BOOKMARKS ii WHERE
                    ii.type = 1 AND lower(ii.user_name) = @UserName
                GROUP BY ii.path || '/' || ii.display_name";

        async Task<IEnumerable<string>> AvailablePaths(string username)
        {
            // a native SQL query is utilized here by means of Dapper!
            var conn = _context.Database.GetDbConnection();
            // the current tx might by null which is OK, when passed to dapper
            var currTx = _context.Database.CurrentTransaction?.GetDbTransaction();

            return await conn.QueryAsync<string>(hierarchyQuery, new {
                UserName = username.ToLower()
            }, currTx);
        }

        async Task<IEnumerable<NodeCount>> GetPathChildCount(string path, string username)
        {
            var conn = _context.Database.GetDbConnection();
            var currTx = _context.Database.CurrentTransaction?.GetDbTransaction();

            var q = @"SELECT i.path as path, count(i.id) as count FROM BOOKMARKS i WHERE i.path IN (
                {0}
                ) GROUP BY i.path {1} ORDER BY i.path";

            if (string.IsNullOrEmpty(path))
            {
                q = string.Format(q, hierarchyQuery, "");
            }
            else
            {
                q = string.Format(q, hierarchyQuery, "HAVING upper(i.path) = @Path");
            }

            return await conn.QueryAsync<NodeCount>(q, new {
                UserName = username.ToLower(),
                Path = path.ToUpper()
            }, currTx);
        }

    }
}

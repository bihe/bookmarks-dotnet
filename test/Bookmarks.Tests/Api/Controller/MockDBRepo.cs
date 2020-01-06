using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Store;

namespace Bookmarks.Tests.Api.Controller
{
    internal abstract class MockDBRepo : IBookmarkRepository
    {
        public virtual Task<BookmarkEntity> Create(BookmarkEntity item)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> Delete(BookmarkEntity item)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> DeletePath(string path, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<BookmarkEntity>> GetAllBookmarks(string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<BookmarkEntity> GetBookmarkById(string id, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<BookmarkEntity>> GetBookmarksByName(string name, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<BookmarkEntity>> GetBookmarksByPath(string path, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<BookmarkEntity>> GetBookmarksByPathStart(string startPath, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<NodeCount>> GetChildCountOfPath(string path, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<BookmarkEntity> GetFolderByPath(string path, string username)
        {
            throw new NotImplementedException();
        }

        public virtual Task<(bool result, T value)> InUnitOfWorkAsync<T>(Func<Task<(bool result, T value)>> atomicOperation)
        {
            throw new NotImplementedException();
        }

        public virtual Task<BookmarkEntity> Update(BookmarkEntity item)
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<BookmarkEntity>> GetMostRecentBookmarks(string username, int limit)
        {
            throw new NotImplementedException();
        }
    }
}

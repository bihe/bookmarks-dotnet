using System;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Store.Tests
{
    public abstract class TestDbProvider : IDisposable
    {
        SqliteConnection _conn;

        protected BookmarkContext SetupDbContext([CallerMemberName]string caller = "")
        {
            _conn = new SqliteConnection("DataSource=/tmp/test.sql");
            _conn.Open();

            var options = new DbContextOptionsBuilder<BookmarkContext>()
                .UseSqlite(_conn)
                .EnableSensitiveDataLogging(true)
                .Options;

            return new BookmarkContext(options);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _conn.Close();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

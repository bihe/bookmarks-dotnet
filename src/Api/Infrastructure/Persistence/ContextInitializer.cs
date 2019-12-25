using System.Linq;
using Store;

namespace Api.Infrastructure.Persistence
{
    public static class ContextInitializer
    {
        public static void InitialData(BookmarkContext context)
        {
            context.Database.EnsureCreated();
            if(context.Bookmarks.Any())
            {
                return;
            }
            context.SaveChanges();
        }
    }
}

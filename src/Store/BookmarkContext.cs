using System;
using Microsoft.EntityFrameworkCore;

namespace Store
{
    public class BookmarkContext : DbContext
    {
        public Guid ContextID { get; set; } = Guid.NewGuid();

        public BookmarkContext(DbContextOptions<BookmarkContext> options) : base(options)
        {}

        public DbSet<BookmarkEntity> Bookmarks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BookmarkEntity>(e => {
                e.ToTable("BOOKMARKS");
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).HasColumnName("id").IsRequired();
                e.Property(p => p.Path).HasColumnName("path").IsRequired().HasMaxLength(255);
                e.Property(p => p.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(128);
                e.Property(p => p.Url).HasColumnName("url").IsRequired().HasMaxLength(512);
                e.Property(p => p.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue(0);
                e.Property(p => p.Type).HasColumnName("type").IsRequired();
                e.Property(p => p.UserName).HasColumnName("user_name").IsRequired().HasMaxLength(128);
                e.Property(p => p.Created).HasColumnName("created").IsRequired();
                e.Property(p => p.Modified).HasColumnName("modified");
                e.Property(p => p.ChildCount).HasColumnName("child_count").IsRequired().HasDefaultValue(0);
                e.Property(p => p.AccessCount).HasColumnName("access_count").IsRequired().HasDefaultValue(0);

                e.HasIndex(i => new { i.Path }).HasName("IX_PATH");
                e.HasIndex(i => new { i.SortOrder }).HasName("IX_SORT_ORDER");
                e.HasIndex(i => new { i.UserName }).HasName("IX_USER");
            });
        }
    }
}

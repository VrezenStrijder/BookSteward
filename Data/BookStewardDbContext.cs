using BookSteward.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.IO;

namespace BookSteward.Data
{
    public class BookStewardDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Category> Categories { get; set; }
        
        public string DbPath { get; private set; }

        public BookStewardDbContext(DbContextOptions<BookStewardDbContext> options)
            : base(options)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var basePath = Environment.GetFolderPath(folder);
            var appFolder = Path.Join(basePath, "BookSteward");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            DbPath = Path.Join(appFolder, "BookSteward.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置多对多关系
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Tags)
                .WithMany(t => t.Books)
                .UsingEntity(j => j.ToTable("BookTags")); 

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Books)
                .WithMany();

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Children)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={DbPath}");
            }
            
            base.OnConfiguring(optionsBuilder);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using BookSteward.Models;
using BookSteward.Data;

namespace BookSteward.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly BookStewardDbContext context;

        public CategoryService(BookStewardDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await context.Categories
                .Include(c => c.Children)
                .Include(c => c.Books)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await context.Categories
                .Include(c => c.Children)
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category> CreateCategoryAsync(string name, int? parentId = null)
        {
            var category = new Category
            {
                Name = name,
                ParentId = parentId
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> UpdateCategoryAsync(int id, string name)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null) return null;

            category.Name = name;
            await context.SaveChangesAsync();
            return category;
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category != null)
            {
                context.Categories.Remove(category);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Category>> GetRootCategoriesAsync()
        {
            return await context.Categories
                .Include(c => c.Children)
                    .ThenInclude(child => child.Children)
                        .ThenInclude(grandchild => grandchild.Children)
                .Include(c => c.Books)
                .Where(c => c.ParentId == null)
                .ToListAsync();
        }

        public async Task<Category> GetOrCreateDefaultCategoryAsync()
        {
            var defaultCategory = await context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Name == "默认分类");

            if (defaultCategory == null)
            {
                defaultCategory = new Category
                {
                    Name = "默认分类",
                    ParentId = null
                };
                context.Categories.Add(defaultCategory);
                await context.SaveChangesAsync();
            }

            return defaultCategory;
        }
        
        public async Task<Category> UpdateCategoryBooksAsync(int categoryId, List<Book> books)
        {
            var category = await context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == categoryId);
                
            if (category == null)
                throw new ArgumentException($"分类ID {categoryId} 不存在");
                
            // 清空并重新添加书籍
            category.Books.Clear();
            foreach (var book in books)
            {
                category.Books.Add(book);
            }
            
            await context.SaveChangesAsync();
            return category;
        }
    }
}
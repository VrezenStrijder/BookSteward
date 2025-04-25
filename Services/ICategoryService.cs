using BookSteward.Models;

namespace BookSteward.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();

        Task<Category> GetCategoryByIdAsync(int id);

        Task<Category> CreateCategoryAsync(string name, int? parentId = null);

        Task<Category?> UpdateCategoryAsync(int id, string name);

        Task DeleteCategoryAsync(int id);

        Task<List<Category>> GetRootCategoriesAsync();

        /// <summary>
        /// 获取或创建默认分类
        /// </summary>
        /// <returns>默认分类对象</returns>
        Task<Category> GetOrCreateDefaultCategoryAsync();
        
        /// <summary>
        /// 更新分类中的书籍列表
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="books">书籍列表</param>
        /// <returns>更新后的分类</returns>
        Task<Category> UpdateCategoryBooksAsync(int categoryId, List<Book> books);
    }
}
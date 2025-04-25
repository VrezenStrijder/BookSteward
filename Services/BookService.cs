using BookSteward.Data;
using BookSteward.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSteward.Services;

public class BookService : IBookService
{
    private readonly BookStewardDbContext dbContext;

    public BookService(BookStewardDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IEnumerable<Book>> GetAllBooksAsync()
    {
        Log.Debug("获取所有书籍.");
        try
        {
            return await dbContext.Books.Include(b => b.Tags).ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取所有书籍失败.");
            throw; 
        }
    }

    public async Task<Book?> GetBookByIdAsync(int id)
    {
        Log.Debug("获取书籍: {BookId}", id);
        try
        {
            return await dbContext.Books.Include(b => b.Tags).FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取书籍: {BookId}", id);
            throw;
        }
    }

    public async Task<Book> AddBookAsync(Book book)
    {
        if (book == null) throw new ArgumentNullException(nameof(book));
        Log.Information("添加书籍: {BookTitle}", book.Title);
        try
        {
            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync();
            Log.Information("添加书籍 {BookId} 成功.", book.Id);
            return book;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "添加书籍 {BookTitle} 时失败.", book.Title);
            throw;
        }
    }

    
    public async Task<bool> UpdateBookAsync(Book book)
    {
        if (book == null) throw new ArgumentNullException(nameof(book));
        try
        {
            // Check if the book exists
            var existingBook = await dbContext.Books.FindAsync(book.Id);
            if (existingBook == null)
            {
                Log.Warning("不存在Id为 {BookId} 的书籍.", book.Id);
                return false;
            }

            dbContext.Entry(existingBook).CurrentValues.SetValues(book);

            await dbContext.SaveChangesAsync();
            Log.Information("更新书籍 {BookId} 信息成功", book.Id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Error(ex, "更新书籍 {BookId} 信息时发生并发错误.", book.Id);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新书籍 {BookId} 信息时发生错误", book.Id);
            throw;
        }
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        try
        {
            var bookToDelete = await dbContext.Books.FindAsync(id);
            if (bookToDelete == null)
            {
                Log.Warning("不存在Id为 {BookId} 的书籍.", id);
                return false;
            }

            dbContext.Books.Remove(bookToDelete);
            await dbContext.SaveChangesAsync();
            Log.Information("删除书籍 {BookId} 成功.", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除书籍 {BookId} 失败.", id);
            throw;
        }
    }

    
    public async Task<IEnumerable<Book>> SearchBooksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllBooksAsync(); // Return all books if query is empty
        }

        var lowerCaseQuery = query.ToLower();

        try
        {
            return await dbContext.Books
                .Where(b =>
                    (b.Title != null && b.Title.ToLower().Contains(lowerCaseQuery)) ||
                    (b.Author != null && b.Author.ToLower().Contains(lowerCaseQuery)) ||
                    (b.Description != null && b.Description.ToLower().Contains(lowerCaseQuery))
                 )
                .ToListAsync();

            //return await dbContext.Books
            //    .Include(b => b.Tags)
            //    .Where(b =>
            //        (b.Title != null && b.Title.ToLower().Contains(lowerCaseQuery)) ||
            //        (b.Author != null && b.Author.ToLower().Contains(lowerCaseQuery)) ||
            //        (b.Description != null && b.Description.ToLower().Contains(lowerCaseQuery)) || 
            //        (b.Tags != null && b.Tags.Any(t => t.Name != null && t.Name.ToLower().Contains(lowerCaseQuery)))
            //     )
            //    .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "搜索书籍失败.(查询语句: {Query}", query);
            throw;
        }
    }
}
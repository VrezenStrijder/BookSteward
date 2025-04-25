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
        Log.Debug("��ȡ�����鼮.");
        try
        {
            return await dbContext.Books.Include(b => b.Tags).ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "��ȡ�����鼮ʧ��.");
            throw; 
        }
    }

    public async Task<Book?> GetBookByIdAsync(int id)
    {
        Log.Debug("��ȡ�鼮: {BookId}", id);
        try
        {
            return await dbContext.Books.Include(b => b.Tags).FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "��ȡ�鼮: {BookId}", id);
            throw;
        }
    }

    public async Task<Book> AddBookAsync(Book book)
    {
        if (book == null) throw new ArgumentNullException(nameof(book));
        Log.Information("����鼮: {BookTitle}", book.Title);
        try
        {
            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync();
            Log.Information("����鼮 {BookId} �ɹ�.", book.Id);
            return book;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "����鼮 {BookTitle} ʱʧ��.", book.Title);
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
                Log.Warning("������IdΪ {BookId} ���鼮.", book.Id);
                return false;
            }

            dbContext.Entry(existingBook).CurrentValues.SetValues(book);

            await dbContext.SaveChangesAsync();
            Log.Information("�����鼮 {BookId} ��Ϣ�ɹ�", book.Id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Error(ex, "�����鼮 {BookId} ��Ϣʱ������������.", book.Id);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "�����鼮 {BookId} ��Ϣʱ��������", book.Id);
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
                Log.Warning("������IdΪ {BookId} ���鼮.", id);
                return false;
            }

            dbContext.Books.Remove(bookToDelete);
            await dbContext.SaveChangesAsync();
            Log.Information("ɾ���鼮 {BookId} �ɹ�.", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ɾ���鼮 {BookId} ʧ��.", id);
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
            Log.Error(ex, "�����鼮ʧ��.(��ѯ���: {Query}", query);
            throw;
        }
    }
}
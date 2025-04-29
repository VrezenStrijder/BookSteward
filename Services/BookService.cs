using BookSteward.Data;
using BookSteward.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        try
        {
            return await dbContext.Books.Include(b => b.Tags).FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "��ȡ�鼮ʧ��: {BookId}", id);
            throw;
        }
    }

    public async Task<Book> AddBookAsync(Book book)
    {
        if (book == null) throw new ArgumentNullException(nameof(book));
        try
        {
            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync();
            Log.Information("�ѳɹ������鼮:  {BookId} .", book.Id);
            return book;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "�����鼮 {BookTitle} ʧ�� .", book.Title);
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
                Log.Warning("������IDΪ {BookId} ���鼮.", book.Id);
                return false;
            }

            dbContext.Entry(existingBook).CurrentValues.SetValues(book);

            await dbContext.SaveChangesAsync();
            Log.Information("�����鼮 {BookId} �ɹ�.", book.Id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Error(ex, "�����鼮 {BookId} ��Ϣʱ��������ʧ��.", book.Id);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "�����鼮 {BookId} ��Ϣʱʧ��.", book.Id);
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
                Log.Warning("������IDΪ {BookId} ���鼮.", id);
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
            // 分两步执行查询，避免复杂的LINQ表达式无法被翻译的问题
            // 1. 先查询基本属性匹配的书籍
            var basicPropertyMatches = await dbContext.Books
                .Include(b => b.Tags)
                .Where(b =>
                    (b.Title != null && b.Title.ToLower().Contains(lowerCaseQuery)) ||
                    (b.Author != null && b.Author.ToLower().Contains(lowerCaseQuery)) ||
                    (b.Description != null && b.Description.ToLower().Contains(lowerCaseQuery))
                )
                .ToListAsync();

            // 2. 再查询标签匹配的书籍
            var tagMatches = await dbContext.Books
                .Include(b => b.Tags)
                .Where(b => b.Tags.Any(t => t.Name != null && t.Name.ToLower().Contains(lowerCaseQuery)))
                .ToListAsync();

            // 合并结果并去重
            return basicPropertyMatches.Union(tagMatches).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "搜索书籍失败.(查询词: {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<Book>> SearchBooksByTagAsync(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return await GetAllBooksAsync(); // Return all books if tag name is empty
        }

        var lowerCaseTagName = tagName.ToLower();

        try
        {
            // ʹ�����Ӳ�ѯ���渴�ӵĵ������Բ�ѯ
            // ���Ȳ���ƥ��ı�ǩ
            var matchingTagIds = await dbContext.Tags
                .Where(t => t.Name != null && t.Name.ToLower() == lowerCaseTagName)
                .Select(t => t.Id)
                .ToListAsync();

            if (!matchingTagIds.Any())
            {
                return new List<Book>(); // ���û��ƥ��ı�ǩ�����ؿ��б�
            }

            // Ȼ����Ұ�����Щ��ǩ���鼮
            return await dbContext.Books
                .Where(b => b.Tags.Any(t => matchingTagIds.Contains(t.Id)))
                .Include(b => b.Tags)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "����ǩ�����鼮ʧ��.(��ǩ��: {TagName}", tagName);
            throw;
        }
    }

    public IEnumerable<Tag> GetAllTags()
    {
        try
        {
            return dbContext.Tags.ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "��ȡ���б�ǩʧ��.");
            throw;
        }
    }

    public async Task<bool> RemoveTagFromBookAsync(int bookId, int tagId)
    {
        try
        {
            // ��ȡ�鼮
            var book = await dbContext.Books
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
            {
                Log.Warning("������IDΪ {BookId} ���鼮.", bookId);
                return false;
            }

            // ��ȡ��ǩ
            var tag = await dbContext.Tags.FindAsync(tagId);
            if (tag == null)
            {
                Log.Warning("������IDΪ {TagId} �ı�ǩ.", tagId);
                return false;
            }

            // �Ƴ��鼮���ǩ�Ĺ���
            if (book.Tags.Any(t => t.Id == tagId))
            {
                book.Tags.Remove(tag);
                await dbContext.SaveChangesAsync();
                Log.Information("�Ѵ��鼮 {BookId} �Ƴ���ǩ {TagId}", bookId, tagId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "���鼮 {BookId} �Ƴ���ǩ {TagId} ʧ��", bookId, tagId);
            throw;
        }
    }

    public async Task<bool> DeleteTagAsync(int tagId)
    {
        try
        {
            // ��ȡ��ǩ
            var tag = await dbContext.Tags
                .Include(t => t.Books)
                .FirstOrDefaultAsync(t => t.Id == tagId);

            if (tag == null)
            {
                Log.Warning("������IDΪ {TagId} �ı�ǩ.", tagId);
                return false;
            }

            // �Ƴ������鼮��ñ�ǩ�Ĺ���
            foreach (var book in tag.Books.ToList())
            {
                book.Tags.Remove(tag);
            }

            // ɾ����ǩ
            dbContext.Tags.Remove(tag);
            await dbContext.SaveChangesAsync();
            Log.Information("��ɾ����ǩ {TagId} �������й���", tagId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ɾ����ǩ {TagId} ʧ��", tagId);
            throw;
        }
    }

    public async Task AddTagsToBooksAsync(List<int> bookIds, List<string> tagNames)
    {
        if (bookIds == null || !bookIds.Any())
        {
            throw new ArgumentException("�鼮ID�б�����Ϊ��", nameof(bookIds));
        }

        if (tagNames == null || !tagNames.Any())
        {
            throw new ArgumentException("��ǩ�����б�����Ϊ��", nameof(tagNames));
        }

        try
        {
            // ��ȡ��������鼮
            var books = await dbContext.Books
                .Include(b => b.Tags)
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync();

            if (!books.Any())
            {
                Log.Warning("δ�ҵ�ָ��ID���鼮: {BookIds}", string.Join(", ", bookIds));
                return;
            }

            // ����ÿ����ǩ
            foreach (var tagName in tagNames)
            {
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    continue;
                }

                // ���һ򴴽���ǩ
                var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                if (tag == null)
                {
                    tag = new Tag { Name = tagName };
                    dbContext.Tags.Add(tag);
                    await dbContext.SaveChangesAsync();
                }

                // Ϊÿ�������ӱ�ǩ
                foreach (var book in books)
                {
                    // ����鼮�Ƿ����д˱�ǩ
                    if (!book.Tags.Any(t => t.Id == tag.Id))
                    {
                        book.Tags.Add(tag);
                    }
                }
            }

            // �������
            await dbContext.SaveChangesAsync();
            Log.Information("�ɹ�Ϊ {BookCount} ���鼮���� {TagCount} ����ǩ", books.Count, tagNames.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ϊ�鼮���ӱ�ǩʧ��");
            throw;
        }
    }

}
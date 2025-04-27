using BookSteward.Models;
using System.Collections.Generic;

namespace BookSteward.Services;

public interface IBookService
{
    Task<IEnumerable<Book>> GetAllBooksAsync();

    Task<Book?> GetBookByIdAsync(int id);

    Task<Book> AddBookAsync(Book book);

    Task<bool> UpdateBookAsync(Book book);

    Task<bool> DeleteBookAsync(int id);

    Task<IEnumerable<Book>> SearchBooksAsync(string query);
    
    Task<IEnumerable<Book>> SearchBooksByTagAsync(string tagName);
    
    IEnumerable<Tag> GetAllTags();
    
    Task<bool> RemoveTagFromBookAsync(int bookId, int tagId);
    
    Task<bool> DeleteTagAsync(int tagId);
    
    Task AddTagsToBooksAsync(List<int> bookIds, List<string> tagNames);
}
using BookSteward.Models;

namespace BookSteward.Services;

public interface IBookService
{
    Task<IEnumerable<Book>> GetAllBooksAsync();

    Task<Book?> GetBookByIdAsync(int id);

    Task<Book> AddBookAsync(Book book);

    Task<bool> UpdateBookAsync(Book book);

    Task<bool> DeleteBookAsync(int id);

    Task<IEnumerable<Book>> SearchBooksAsync(string query);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BookSteward.Crawl
{
    public class OpenLibraryProvider : IBookCrawlProvider
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<CrawlBookInfo?> SearchByTitleAsync(string title)
        {
            var query = HttpUtility.UrlEncode(title);
            var url = $"https://openlibrary.org/search.json?title={query}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<OpenLibrarySearchResult>();
            if (result?.Docs == null || result.Docs.Length == 0) return null;

            var book = result.Docs[0];

            return new CrawlBookInfo
            {
                Title = book.Title,
                Author = string.Join(", ", book.AuthorName),
                Isbn = book.Isbn?.FirstOrDefault(),
                Publisher = book.Publisher?.FirstOrDefault(),
                CoverUrl = book.CoverI != null ? $"https://covers.openlibrary.org/b/id/{book.CoverI}-L.jpg" : null,
                Source = "OpenLibrary"
            };
        }

        // DTO 用于反序列化 JSON
        private class OpenLibrarySearchResult
        {
            public Doc[] Docs { get; set; }
        }

        private class Doc
        {
            public string Title { get; set; }
            public string[] AuthorName { get; set; }
            public string[] Publisher { get; set; }
            public string[] Isbn { get; set; }
            public int? CoverI { get; set; }
        }
    }
}

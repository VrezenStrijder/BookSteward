using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace BookSteward.Crawl
{
    public class AnnasArchiveProvider : IBookCrawlProvider
    {
        private readonly HttpClient httpClient = new HttpClient();

        public AnnasArchiveProvider()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

        public async Task<CrawlBookInfo?> SearchByTitleAsync(string title)
        {
            var searchUrl = $"https://annas-archive.org/search?q={Uri.EscapeDataString(title)}";
            var html = await httpClient.GetStringAsync(searchUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'results')]//a[contains(@href, '/md5')]");
            if (resultNode == null) return null;

            var bookTitle = resultNode.InnerText.Trim();
            var bookLink = resultNode.GetAttributeValue("href", "");

            // 可以进一步访问详情页获取更多信息（可选）
            return new CrawlBookInfo
            {
                Title = bookTitle,
                Source = "Anna's Archive"
                // Todo
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSteward.Crawl
{
    public interface IBookCrawlProvider
    {
        Task<CrawlBookInfo?> SearchByTitleAsync(string title);
    }

}

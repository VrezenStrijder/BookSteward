using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSteward.Crawl
{
    public class CrawlBookInfo
    {
        public string Title { get; set; }

        public string Author { get; set; }

        public string Publisher { get; set; }

        public string Isbn { get; set; }

        public string CoverUrl { get; set; }

        public string Source { get; set; }
    }
}

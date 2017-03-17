using System;
using System.Text.RegularExpressions;

namespace WebCrawlerUtil
{
    public class PageSearcher
    {
        private Regex[] titleRegex;
        private Regex linkRegex;
        private Regex[] dateRegex;

        public PageSearcher()
        {
            // Links: <a .*?href=\"(.+?)\".*?>.*?<\/a>
            // Title: (1/2 bleacher report, 3/4 cnn)
            // 1. <meta name="og:title" property="og:title" content="(.*?)".*?>
            // 2. <meta name="twitter:title" property="twitter:title" content="(.*?)".*?>
            // 3. <meta content="([^<]*?)" name="twitter:title".*?>
            // 4: <meta content="([^<]*?)" property="og:title".*?>
            // 5. <title>(.*?)<\/title>
            // Date:
            // 1. <meta content="([^<]*?)" property="og:pubdate".*?>
            // 2. <meta content="([^<]*?)" name="pubdate".*?>
            // 3. <meta name="pubdate" .*?content="(.*?)".*?>
            // 4. <meta pd="([^<]*?)".*?>
            // 5. <meta name="date" content="([^<]*?)".*?>
            this.titleRegex = new Regex[] 
            {
                new Regex("<meta name=\"og:title\" property=\"og:title\" content=\"(.*?)\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta name=\"twitter:title\" property=\"twitter:title\" content=\"(.*?)\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta content=\"([^<]*?)\" name=\"twitter:title\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta content=\"([^<]*?)\" property=\"og:title\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<title>(.*?)<\\/title>", RegexOptions.Compiled | RegexOptions.IgnoreCase) // generic title tag comes last as it's got other annonying info like cnn.com or bleacher report
            };
            this.linkRegex = new Regex("<a .*?href=\"(.+?)\".*?>.*?<\\/a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            dateRegex = new Regex[] 
            {
                new Regex("<meta content=\"([^<]*?)\" property=\"og:pubdate\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta content=\"([^<]*?)\" name=\"pubdate\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta name=\"pubdate\" .*?content=\"(.*?)\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta pd=\"([^<]*?)\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex("<meta name=\"date\" content=\"([^<]*?)\".*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            };
        }

        public string GetTitle(string pageContent)
        {
            string foundTitle = string.Empty;
            for (int i = 0; i < titleRegex.Length; i++)
            {
                Match m = titleRegex[i].Match(pageContent);
                if (m.Groups[1].Value != string.Empty)
                {
                    foundTitle = m.Groups[1].Value;
                    break;
                }
            }
            System.Diagnostics.Trace.WriteLine(foundTitle);
            return foundTitle;
        }

        public MatchCollection GetAllLinks(string pageContent)
        {
            return linkRegex.Matches(pageContent);
        }

        public DateTime GetPubDate(string pageContent)
        {
            // MinValue crashes table inserts, azure table requires dates be > 1601
            DateTime? foundDate = null;
            for (int i = 0; i < dateRegex.Length; i++)
            {
                Match m = dateRegex[i].Match(pageContent);
                if (m.Groups[1].Value != string.Empty)
                {
                    DateTime temp = DateTime.Parse(m.Groups[1].Value);
                    // only store the most recent date
                    if (!foundDate.HasValue || foundDate.Value.CompareTo(temp) < 0)
                    {
                        foundDate = temp;
                    }
                }
            }
            return foundDate ?? DateTime.UtcNow;
        }
    }
}

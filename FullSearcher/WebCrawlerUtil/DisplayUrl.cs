using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebCrawlerUtil
{
    // stripped down version of UrlEntity to return to users
    public class DisplayUrl
    {
        public DisplayUrl() { }
        public DisplayUrl(string url, string title, DateTime date)
        {
            this.URL = url;
            this.Title = title;
            this.Date = date;
        }

        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string URL { get; set; }
    }
}
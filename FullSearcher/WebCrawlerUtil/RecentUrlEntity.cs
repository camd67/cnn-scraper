using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebCrawlerUtil
{
    class RecentUrlEntity : TableEntity
    {
        public const string RECENT_PARTITION_KEY = "recent";
        public const string RECENT_ROW_KEY = "urlListing";
        private const int MAX_URLS = 10;

        private Queue<string> urls;

        public RecentUrlEntity()
        {
            this.PartitionKey = RECENT_PARTITION_KEY;
            this.RowKey = RECENT_ROW_KEY;

            urls = new Queue<string>();
        }

        public string Urls
        {
            get { return string.Join(">", urls.ToArray()); }
            set { urls = new Queue<string>(value.Split('>')); }
        }

        public void AddUrl(string url)
        {
            if(urls.Count >= MAX_URLS)
            {
                urls.Dequeue();
            }
            urls.Enqueue(url);
        }
    }
}

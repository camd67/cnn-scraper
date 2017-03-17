using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace WebCrawlerUtil
{
    public class ErrorEntity : TableEntity
    {
        public const string ERROR_PARTITION = "errors";
        public ErrorEntity() { }
        public ErrorEntity(string errorName, string url)
        {
            this.PartitionKey = ERROR_PARTITION;
            this.RowKey = Guid.NewGuid().ToString();

            this.Error = errorName;
            this.URL = url;
        }

        public string Error { get; set; }
        public string URL { get; set; }
    }
}

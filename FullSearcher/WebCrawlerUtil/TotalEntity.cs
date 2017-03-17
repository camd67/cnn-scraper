using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerUtil
{
    public class TotalEntity : TableEntity
    {
        private const string TOTAL_PARTITION = "totals";

        public TotalEntity() { }
        public TotalEntity(string totalType, int start)
        {
            this.PartitionKey = TOTAL_PARTITION;
            this.RowKey = totalType;

            this.ItemCount = start;
        }

        public void IncrementCount()
        {
            this.ItemCount++;
        }

        public int ItemCount { get; set; }
    }
}

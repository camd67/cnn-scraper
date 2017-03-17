using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebCrawlerUtil
{
    class WorkerCommandEntity : TableEntity
    {
        public const string WORKER_COMMAND_PARTITION = "WorkerCommand";
        public const string WORKER_COMMAND_ROW = "WorkerShouldRun";

        public WorkerCommandEntity() { }
        public WorkerCommandEntity(bool toRun)
        {
            this.PartitionKey = WORKER_COMMAND_PARTITION;
            this.RowKey = WORKER_COMMAND_ROW;

            this.ShouldRun = toRun;
        }

        public bool ShouldRun { get; set; }
    }
}

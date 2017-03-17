using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace WebCrawlerUtil
{
    public class CloudConnection
    {
        // All URLs that are waiting to be processed go here
        private const string URL_QUEUE_NAME = "urlprocesslist";
        // All XML urls that are waiting to be processed go here
        private const string XML_QUEUE_NAME = "xmlprocesslist";
        // All statistics go here - worker stats, url count, errors, etc.
        private const string STATS_TABLE_NAME = "statistics";
        // All URLs that have been processed go here
        private const string URL_TABLE_NAME = "processedurls";
        private const string BLOB_NAME = "wiki";
        private TimeSpan normalMessageTime = TimeSpan.FromSeconds(30);

        private CloudStorageAccount storageAccount;
        private CloudBlobContainer blobContainer;
        private CloudTable statsTable;
        private CloudTable urlTable;
        private CloudQueue urlQueue;
        private CloudQueue xmlQueue;

        private static TotalEntity urlTotalCount;
        private static TotalEntity allUrlsCrawled;
        private static WorkerCommandEntity command;
        private static RecentUrlEntity recentUrls;

        public CloudConnection()
        {
            storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            urlQueue = queueClient.GetQueueReference(URL_QUEUE_NAME);
            urlQueue.CreateIfNotExists();

            xmlQueue = queueClient.GetQueueReference(XML_QUEUE_NAME);
            xmlQueue.CreateIfNotExists();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            urlTable = tableClient.GetTableReference(URL_TABLE_NAME);
            urlTable.CreateIfNotExists();

            statsTable = tableClient.GetTableReference(STATS_TABLE_NAME);
            statsTable.CreateIfNotExists();

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(BLOB_NAME);

            InitEntities();
        }

        private void InitEntities()
        {
            // all of the following create a new local entity, try to get the remote one
            // only assigning to the new one if the remote doesn't exist
            // Tried making a generic function for this, doesn't work well
            TotalEntity newUrlTotal = new TotalEntity("urlsIndexed", 0);
            TotalEntity remoteUrlTotal = (TotalEntity)statsTable.Execute(TableOperation
                .Retrieve<TotalEntity>(newUrlTotal.PartitionKey, newUrlTotal.RowKey)).Result;
            if (remoteUrlTotal != null)
            {
                urlTotalCount = remoteUrlTotal;
            }
            else
            {
                urlTotalCount = newUrlTotal;
                statsTable.Execute(TableOperation.InsertOrReplace(urlTotalCount));
            }

            TotalEntity newAllUrl = new TotalEntity("allUrls", 0);
            TotalEntity remoteAllUrl = (TotalEntity)statsTable.Execute(TableOperation
                .Retrieve<TotalEntity>(newAllUrl.PartitionKey, newAllUrl.RowKey)).Result;
            if (remoteAllUrl != null)
            {
                allUrlsCrawled = remoteAllUrl;
            }
            else
            {
                allUrlsCrawled = newAllUrl;
                statsTable.Execute(TableOperation.InsertOrReplace(allUrlsCrawled));
            }

            WorkerCommandEntity newCommand = new WorkerCommandEntity(false);
            WorkerCommandEntity remoteCommand = (WorkerCommandEntity)statsTable.Execute(TableOperation
                .Retrieve<WorkerCommandEntity>(newCommand.PartitionKey, newCommand.RowKey)).Result;
            if (remoteCommand != null)
            {
                command = remoteCommand;
            }
            else
            {
                command = newCommand;
                statsTable.Execute(TableOperation.InsertOrReplace(command));
            }

            RecentUrlEntity newRecents = new RecentUrlEntity();
            RecentUrlEntity remoteRecents = (RecentUrlEntity)statsTable.Execute(TableOperation
                .Retrieve<RecentUrlEntity>(newRecents.PartitionKey, newRecents.RowKey)).Result;
            if (remoteRecents != null)
            {
                recentUrls = remoteRecents;
            }
            else
            {
                recentUrls = newRecents;
                statsTable.Execute(TableOperation.InsertOrReplace(recentUrls));
            }
        }

        public string GetPageTitle(string url)
        {
            TableQuery<UrlEntity> query = new TableQuery<UrlEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, UrlEntity.EncodeToTableKey(url))
                );
            var result = urlTable.ExecuteQuery(query);
            if(result.Count() == 0)
            {
                return "No results found";
            }
            else
            {
                return result.First().Title;
            }
        }

        public CloudQueueMessage GetNextXmlMessage()
        {
            return xmlQueue.GetMessage(normalMessageTime);
        }
        public CloudQueueMessage GetNextUrlMessage()
        {
            return urlQueue.GetMessage(normalMessageTime);
        }

        public void DeleteUrlMessage(CloudQueueMessage message)
        {
            urlQueue.DeleteMessage(message);
        }
        public void DeleteXmlMessage(CloudQueueMessage message)
        {
            xmlQueue.DeleteMessage(message);
        }

        public void ResumeWorkers()
        {
            urlTable.CreateIfNotExists();
            Trace.WriteLine("Resuming all workers");
            command.ShouldRun = true;
            statsTable.Execute(TableOperation.InsertOrReplace(command));
        }

        public void StartWorkers(string url)
        {
            xmlQueue.AddMessage(new CloudQueueMessage(url));
            urlTable.CreateIfNotExists();
            Trace.WriteLine("Starting all workers");
            command.ShouldRun = true;
            statsTable.Execute(TableOperation.InsertOrReplace(command));
        }

        public void StartWorkers(string url1, string url2)
        {
            xmlQueue.AddMessage(new CloudQueueMessage(url1));
            xmlQueue.AddMessage(new CloudQueueMessage(url2));
            urlTable.CreateIfNotExists();
            command.ShouldRun = true;
            statsTable.Execute(TableOperation.InsertOrReplace(command));
            Trace.WriteLine("Starting all workers");
        }
        public void StopWorkers()
        {
            Trace.WriteLine("Stopping all workers");
            command.ShouldRun = false;
            statsTable.Execute(TableOperation.InsertOrReplace(command));
        }

        public void ClearTables()
        {
            urlTable.DeleteIfExists();

            urlTotalCount = new TotalEntity("urlsIndexed", 0);
            statsTable.Execute(TableOperation.InsertOrReplace(urlTotalCount));

            allUrlsCrawled = new TotalEntity("allUrls", 0);
            statsTable.Execute(TableOperation.InsertOrReplace(allUrlsCrawled));

            command = new WorkerCommandEntity(false);
            statsTable.Execute(TableOperation.InsertOrReplace(command));

            recentUrls = new RecentUrlEntity();
            statsTable.Execute(TableOperation.InsertOrReplace(recentUrls));

            TableQuery query = new TableQuery().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ErrorEntity.ERROR_PARTITION));
            foreach (var row in statsTable.ExecuteQuery(query))
            {
                statsTable.Execute(TableOperation.Delete(row));
            }
        }

        public bool WorkerShouldRun()
        {
            WorkerCommandEntity wce = (WorkerCommandEntity)statsTable.Execute(
                TableOperation.Retrieve<WorkerCommandEntity>(command.PartitionKey, command.RowKey)).Result;
            return wce.ShouldRun;
        }

        public int GetUrlTableLength()
        {
            return GetTotalEntity(urlTotalCount);
        }

        public void IncrementUrlTableLength()
        {
            TotalEntity temp = (TotalEntity)statsTable.Execute(TableOperation
                .Retrieve<TotalEntity>(urlTotalCount.PartitionKey, urlTotalCount.RowKey)).Result;
            if(Math.Abs(temp.ItemCount - urlTotalCount.ItemCount) > 10)
            {
                // de-sync noticed, update our local to the remote
                urlTotalCount = temp;
            }
            urlTotalCount.IncrementCount();
            statsTable.Execute(TableOperation.InsertOrReplace(urlTotalCount));
        }

        public int GetUrlQueueLength()
        {
            urlQueue.FetchAttributes();
            return urlQueue.ApproximateMessageCount ?? -1;
        }

        public int GetTotalUrlsCrawled()
        {
            return GetTotalEntity(allUrlsCrawled);
        }

        public TrieStatEntity GetTrieStats()
        {
            return (TrieStatEntity)statsTable.Execute(TableOperation.Retrieve<TrieStatEntity>(TrieStatEntity.TRIE_STATS_PKEY, TrieStatEntity.TRIE_STATS_RKEY)).Result;
        }

        private int GetTotalEntity(TotalEntity toGet)
        {
            // don't want to just return the in-memory one since it could be out of date
            TotalEntity result = (TotalEntity)statsTable.Execute(TableOperation.Retrieve<TotalEntity>(toGet.PartitionKey, toGet.RowKey)).Result;
            toGet.ItemCount = result.ItemCount;
            return result.ItemCount;

        }
        public void IncrementTotalUrlsCrawled()
        {
            TotalEntity temp = (TotalEntity)statsTable.Execute(TableOperation
                    .Retrieve<TotalEntity>(allUrlsCrawled.PartitionKey, allUrlsCrawled.RowKey)).Result;
            // allow for some wiggle room, in case the request takes a while
            if (Math.Abs(temp.ItemCount - allUrlsCrawled.ItemCount) > 10)
            {
                // de-sync noticed, update our local to the remote
                allUrlsCrawled = temp;
            }
            allUrlsCrawled.IncrementCount();
            statsTable.Execute(TableOperation.InsertOrReplace(allUrlsCrawled));
        }

        public List<ErrorEntity> GetErrors()
        {
            int maxErrors = 30;
            int currentErrors = 0;
            List<ErrorEntity> errors = new List<ErrorEntity>();
            TableQuery<ErrorEntity> q = new TableQuery<ErrorEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ErrorEntity.ERROR_PARTITION));
            foreach(ErrorEntity entity in statsTable.ExecuteQuery(q))
            {
                if(currentErrors > maxErrors) { break; }
                errors.Add(entity);
                currentErrors++;
            }
            return errors;
        }

        public void AddError(string error, string url)
        {
            ErrorEntity toAdd = new ErrorEntity(error, url);
            statsTable.Execute(TableOperation.InsertOrReplace(toAdd));
        }

        public string[] GetRecentUrls()
        {
            RecentUrlEntity entity = (RecentUrlEntity)statsTable.Execute(
                TableOperation.Retrieve<RecentUrlEntity>(RecentUrlEntity.RECENT_PARTITION_KEY, RecentUrlEntity.RECENT_ROW_KEY)).Result;
            return entity.Urls.Split('>');
        }

        public void FlushQueue()
        {
            //xmlQueue.Clear();
            urlQueue.Clear();
            
            CloudQueueMessage message = xmlQueue.GetMessage(normalMessageTime);
            while (message != null)
            {
                xmlQueue.DeleteMessage(message);
                message = xmlQueue.GetMessage(normalMessageTime);
            }
            xmlQueue.Clear();
        }

        public void AddToUrlTable(UrlEntity url)
        {
            urlTable.Execute(TableOperation.InsertOrReplace(url));
            recentUrls.AddUrl(UrlEntity.DecodeFromTableKey(url.PartitionKey) + ": " + UrlEntity.DecodeFromTableKey(url.RowKey));
            statsTable.Execute(TableOperation.InsertOrReplace(recentUrls));
            IncrementUrlTableLength();
        }

        public void AddToUrlTable(UrlEntity[] urls)
        {
            // Can't use a batch op since partition keys are different
            foreach(UrlEntity entity in urls)
            {
                AddToUrlTable(entity);
            }
        }

        public void AddToUrlQueue(string url)
        {
            urlQueue.AddMessage(new CloudQueueMessage(url), TimeSpan.FromDays(5));
        }

        public void AddToXmlQueue(string xml)
        {
            xmlQueue.AddMessage(new CloudQueueMessage(xml), TimeSpan.FromDays(5));
        }

        public void UpdateWorkerInfo(WorkerInfoEntity update)
        {
            statsTable.Execute(TableOperation.InsertOrReplace(update));
        }
        public WorkerInfoEntity[] GetWorkerInfo()
        {
            TableQuery<WorkerInfoEntity> query = new TableQuery<WorkerInfoEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, WorkerInfoEntity.PARTITION_KEY)
                );
            return statsTable.ExecuteQuery(query).ToArray();
        }

        public DisplayUrl[] SearchForPhrase(string phrase)
        {
            if(phrase == null || phrase == string.Empty) { return null; }
            // remove non-alphanumeric and split before using linq (get us a collection)
            string[] words = UrlEntity.CleanFullTitle(phrase).ToLower().Split(' ');
            if(words.Length <= 0)
            {
                return null;
            }
            // query the db. This should be in multiple parts due to the maximum limit on filter params for azure tables
            // also since we're using different partition keys it doesn't really speed anything up
            List<UrlEntity> allEntites = new List<UrlEntity>();
            foreach(string param in words)
            {
                var results = urlTable.ExecuteQuery<UrlEntity>(new TableQuery<UrlEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, param)
                    ));
                allEntites.AddRange(results);
            }
            // group by full title, then order by the number of matches for that title
            // ending with a date sort and making new display URLs for the items
            var res = allEntites
                .GroupBy(x => x.Title)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.First().Date)
                .Select(x => new DisplayUrl(x.First().URL, x.First().Title, x.First().Date))
                .Take(20);
                ;
            return res.ToArray();
        }

        public void UpdateTrieStats(TrieStatEntity stat)
        {
            statsTable.Execute(TableOperation.InsertOrReplace(stat));
        }
        public CloudBlockBlob GetFromBlob(string filename)
        {
            if (blobContainer.Exists())
            {
                foreach (IListBlobItem item in blobContainer.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        if (blob.Name == filename)
                        {
                            return blob;
                        }
                    }
                }
            }
            return null;
        }
    }
}

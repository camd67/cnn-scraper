using System.Collections.Generic;
using System.Web.Script.Services;
using System.Web.Services;
using WebCrawlerUtil;

namespace WebDisplay
{
    [WebService(Namespace = "http://doane-assignment3.cloudapp.net/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ScriptService]
    public class Dashboard : WebService
    {
        private static CloudConnection connection = new CloudConnection();

#if DEBUG
        // helper functions to do debug stuff, like adding a single XML or URL to the queue
        [WebMethod]
        public void AddXmlDebug(string url)
        {
            connection.AddToXmlQueue(url);
            connection.ResumeWorkers();
        }
        [WebMethod]
        public void AddUrlDebug(string url)
        {
            connection.AddToUrlQueue(url);
            connection.ResumeWorkers();
        }
#endif

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public WorkerInfoEntity[] GetAllWorkers()
        {
            return connection.GetWorkerInfo();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string[] GetRecentUrls()
        {
            return connection.GetRecentUrls();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<int> GetLengthsAndTotals()
        {
            List<int> totals = new List<int>();
            totals.Add(connection.GetTotalUrlsCrawled());
            totals.Add(connection.GetUrlQueueLength());
            totals.Add(connection.GetUrlTableLength());
            return totals;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public TrieStatEntity GetTrieStats()
        {
            return connection.GetTrieStats();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StopCrawlers()
        {
            connection.StopWorkers();
            connection.FlushQueue();
            connection.ClearTables();
            return "Sent stop command to workers, cleared tables, cleared queues";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<ErrorEntity> GetAllErrors()
        {
            return connection.GetErrors();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StartCrawlersUrl(string url)
        {
            // restrict to just cnn + bleacherreport for this assignment
            if (!(url.EndsWith("http://www.cnn.com/robots.txt") || url.EndsWith("http://bleacherreport.com/robots.txt")))
            {
                return "Url must be to a valid site";
            }
            connection.StartWorkers(url);
            return "Started Crawlers with url: " + url;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StartWorkers()
        {
            connection.StartWorkers("http://www.cnn.com/robots.txt", "http://bleacherreport.com/robots.txt");
            return "Started workers with cnn and bleacherreport";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string PauseWorkers()
        {
            connection.StopWorkers();
            return "Paused workers";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ResumeWorkers()
        {
            connection.ResumeWorkers();
            return "Resumed workers";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPageInformation(string url)
        {
            return connection.GetPageTitle(url);
        }
    }
}

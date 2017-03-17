using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;
using System.Net;
using System.Threading;
using WebCrawlerUtil;

namespace CrawlerWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static WebCrawler crawler;
        private static CloudConnection connection;

        public override void Run()
        {
            Trace.TraceInformation("CrawlerWorker is running");
            crawler.Crawl();
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            connection = new CloudConnection();
            crawler = new WebCrawler(connection);
            bool result = base.OnStart();
            Trace.TraceInformation("CrawlerWorker has been started");
            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("CrawlerWorker is stopping");
            this.cancellationTokenSource.Cancel();
            base.OnStop();
            Trace.TraceInformation("CrawlerWorker has stopped");
        }
    }
}

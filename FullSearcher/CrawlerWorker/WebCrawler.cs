using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using WebCrawlerUtil;

namespace CrawlerWorker
{
    class WebCrawler
    {
        private WorkerInfoEntity worker;
        private PerformanceCounter ramPerformance;
        private PerformanceCounter cpuPerformance;
        private WorkerState currentState;
        private List<string> disallowedUrls;
        private List<string> allowedDomains;
        private HashSet<string> visitedUrls;
        private HashSet<string> seenUrls; // seen URLs are ones we've added to the queue before, visited is processed

        private HttpClient httpClient;
        private PageSearcher searcher;
        private CloudConnection connection;

        private int workerStatusCounter;
        // max number of messages to process before updating worker status
        private const int MAX_STATUS_COUNTER = 4;
        private const int SLEEP_TIME = 50;
        private const int WORKER_STATS_UPDATE = 3;
        private const string WORKER_NAME = "MainWorker";

        public WebCrawler(CloudConnection conn)
        {
            this.connection = conn;
            this.ramPerformance = new PerformanceCounter("Memory", "Available MBytes");
            this.cpuPerformance = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            this.currentState = WorkerState.IDLE;
            this.worker = new WorkerInfoEntity(WORKER_NAME, cpuPerformance.NextValue() / Environment.ProcessorCount, ramPerformance.NextValue(), currentState);
            this.connection.UpdateWorkerInfo(this.worker);
            this.workerStatusCounter = 0;
            this.disallowedUrls = new List<string>();
            this.allowedDomains = new List<string>() { "cnn.com", "bleacherreport.com/articles", "bleacherreport.com/nba/archives", "bleacherreport.com/nba" };
            this.visitedUrls = new HashSet<string>();
            this.seenUrls = new HashSet<string>();
            this.httpClient = new HttpClient();
            this.searcher = new PageSearcher();
        }

        ~WebCrawler()
        {
            if(httpClient != null)
            {
                httpClient.Dispose();
            }
        }

        public void Crawl()
        {
            while (true)
            {
                CheckForCancelMessage();
                switch (currentState)
                {
                    case WorkerState.IDLE:
                        Idle();
                        break;
                    case WorkerState.LOADING:
                        ProcessXml();
                        break;
                    case WorkerState.CRAWLING:
                        ProcessUrl();
                        break;
                    default:
                        Idle();
                        break;
                }
                Thread.Sleep(SLEEP_TIME);
            }
        }

        private bool CheckForCancelMessage()
        {
            if (currentState != WorkerState.IDLE && !connection.WorkerShouldRun())
            {
                Trace.WriteLine("Recieved stop command, Switching worker to Idle");
                currentState = WorkerState.IDLE;
                worker.UpdateWorkerState(cpuPerformance, ramPerformance, currentState);
                return true;
            }
            return false;
        }

        private void Idle()
        {
            UpdateWorkerStatusCounter();
            if (connection.WorkerShouldRun())
            {
                Trace.WriteLine("Switching worker to Loading");
                currentState = WorkerState.LOADING;
                workerStatusCounter = 0;
                visitedUrls.Clear();
            }
        }

        private void ProcessXml()
        {
            CloudQueueMessage message = connection.GetNextXmlMessage();
            if(message != null)
            {
                string xmlUrl = message.AsString;
                try
                {
                    if (!visitedUrls.Contains(xmlUrl))
                    {
                        visitedUrls.Add(xmlUrl);
                        // Process XML
                        if (xmlUrl.EndsWith("robots.txt"))
                        {
                            ParseRobotsTxt(xmlUrl);
                        }
                        else
                        {
                            ParseXml(xmlUrl);
                        }
                    }
                }
                catch(XmlException ex)
                {
                    AddError("Xml error", xmlUrl);
                    Trace.WriteLine(ex);
                }
                catch(Exception ex)
                {
                    AddError(ex.GetType().Name + " processing XML", xmlUrl);
                    Trace.WriteLine(ex);
                }
                // Deleting here instead of inside the try {} block since we don't want to re-process broken URLs
                connection.DeleteXmlMessage(message);
                UpdateWorkerStatusCounter();
            }
            else
            {
                // No more XML messages
                Trace.WriteLine("Switching worker to Crawling");
                currentState = WorkerState.CRAWLING;
                
            }
        }

        private void ProcessUrl()
        {
            CloudQueueMessage message = connection.GetNextUrlMessage();
            if(message != null)
            {
                string url = message.AsString;
                // Process URL
                try
                {
                    if (!visitedUrls.Contains(url) && IsValidLink(url))
                    {
                        visitedUrls.Add(url);
                        ParseUrl(url);
                    }
                }
                catch(RegexMatchTimeoutException ex)
                {
                    AddError("Regex error", url);
                    Trace.WriteLine(ex);
                }
                catch(Exception ex)
                {
                    AddError(ex.GetType().Name + " processing url", url);
                    Trace.WriteLine(ex);
                }
                connection.DeleteUrlMessage(message);
                connection.IncrementTotalUrlsCrawled();
                UpdateWorkerStatusCounter();
            }
            else
            {
                // No more url messages
                Trace.WriteLine("Switching worker to Idle");
                connection.StopWorkers();
                currentState = WorkerState.IDLE;
            }
        }

        private void UpdateWorkerStatusCounter()
        {
            if(workerStatusCounter >= MAX_STATUS_COUNTER)
            {
                workerStatusCounter = 0;
                worker.UpdateWorkerState(cpuPerformance, ramPerformance, currentState);
                connection.UpdateWorkerInfo(worker);
            }
            else
            {
                workerStatusCounter++;
            }
        }

        private void ParseUrl(string url)
        {
            // if url fails to get title, ignore page
            string pageContent = DownloadText(url);
            string title = searcher.GetTitle(pageContent);
            if(title == string.Empty) { return; }
            DateTime date = searcher.GetPubDate(pageContent);
            connection.AddToUrlTable(UrlEntity.CreateEntitiesFromString(title, url, date));
            foreach (Match match in searcher.GetAllLinks(pageContent))
            {
                string linkToAdd = match.Groups[1].Value;
                if (linkToAdd.StartsWith("//"))
                {
                    // assume http
                    linkToAdd = "http:" + linkToAdd;
                }
                if (linkToAdd.StartsWith("/"))
                {
                    // relative url, add in root based on current domain
                    if (url.Contains("cnn.com"))
                    {
                        linkToAdd = "http://cnn.com" + linkToAdd;
                    }
                    else if (url.Contains("blearcherreport.com"))
                    {
                        linkToAdd = "http://bleacherreport.com" + linkToAdd;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (!visitedUrls.Contains(linkToAdd))
                {
                    AddToUrlQueue(linkToAdd);
                }
            }
        }

        private void ParseXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(DownloadText(xml));

            XmlNodeList sitemaps = doc.GetElementsByTagName("sitemap");
            if(sitemaps.Count <= 0)
            {
                // xml doc is a list of urls, not sitemaps
                XmlNodeList sites = doc.GetElementsByTagName("url");
                foreach (XmlNode node in sites)
                {
                    string urlToAdd = node["loc"].InnerText.Trim();
                    string lastMod = string.Empty;
                    if(node["lastmod"] != null)
                    {
                        lastMod = node["lastmod"].InnerText.Trim();
                    }
                    else if(node["news:news"] != null && node["news:news"]["news:publication_date"] != null)
                    { 
                        lastMod = node["news:news"]["news:publication_date"].InnerText.Trim();
                    }
                    else
                    {// generic url
                        AddToUrlQueue(urlToAdd);
                    }

                    if (IsRecentUrl(urlToAdd, lastMod))
                    {
                        AddToUrlQueue(urlToAdd);
                    }
                }
            }
            else
            {
                // xml is sitemaps
                foreach(XmlNode node in sitemaps)
                {
                    string xmlToAdd = node["loc"].InnerText.Trim();
                    string lastMod = node["lastmod"].InnerText.Trim();
                    if (IsRecentUrl(xmlToAdd, lastMod))
                    {
                        connection.AddToXmlQueue(xmlToAdd);
                    }
                }
            }
        }

        private void ParseRobotsTxt(string robots)
        {
            string robotsList = DownloadText(robots);
            string[] allLines = robotsList.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            const string DISALLOW = "Disallow: ";
            const string SITEMAP = "Sitemap: ";
            foreach(string line in allLines)
            {
                if (line.StartsWith(DISALLOW))
                {
                    disallowedUrls.Add(line.Substring(DISALLOW.Length));
                }
                else if (line.StartsWith(SITEMAP)
                    && !line.EndsWith("sitemap-interactive.xml"))
                {
                    if((line.Contains("bleacherreport.com") && (line.Contains("nba") || line.EndsWith("bleacherreport.com/sitemap.xml")))
                        || (line.Contains("cnn.com"))){
                        // make sure we're either on cnn and in time (checked later) or on bleacherreport + nba
                        connection.AddToXmlQueue(line.Substring(SITEMAP.Length));
                    }
                }
            }
        }

        private string DownloadText(string url)
        {
            // gross force sync
            using (HttpResponseMessage message = httpClient.GetAsync(url).Result)
            {
                using (HttpContent content = message.Content)
                {
                    return content.ReadAsStringAsync().Result;
                }
            }
        }

        /// <summary>
        /// Checks to see if a given URL is HTML by making a HEAD request.
        /// This is expensive so IsHtmlPartial should be used for quick checks to remove JS, internal links, etc.
        /// </summary>
        /// <param name="url">The URL to check</param>
        /// <returns>True if the URL points to an HTML resource</returns>
        private bool IsHtmlFull(string url)
        {
            using(HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Head, url))
            {
                using (HttpResponseMessage message = httpClient.SendAsync(req).Result)
                {
                    return message.Content.Headers.ContentType.MediaType == "text/html";
                }
            }
        }
        
        /// <summary>
        /// Weak but fast check to see if a URL is HTML.
        /// This is innaccurate, but filters out stuff like "#" and "javascript:void(0);"
        /// </summary>
        /// <param name="url">The URL to check</param>
        /// <returns>True if the URL possibly points to HTML</returns>
        private bool IsHtmlPartial(string url)
        {
            return url.StartsWith("http://");
        }

        private bool IsValidLink(string url)
        {
            if (url.StartsWith("https"))
            {
                url = "http" + url.Substring(5);
            }
            return IsAllowedUrl(url) && IsHtmlPartial(url);
        }

        private void AddToUrlQueue(string url)
        {
            if (!seenUrls.Contains(url) && IsValidLink(url))
            {
                seenUrls.Add(url);
                connection.AddToUrlQueue(url);
            }
        }

        private void AddError(string errorMessage, string url)
        {
            connection.AddError(errorMessage, url);
            Trace.WriteLine("Adding error: " + url);
        }

        private bool IsAllowedUrl(string url)
        {
            // check for allowed domains
            bool allowed = false;
            for(int i = 0; i < allowedDomains.Count; i++)
            {
                if (url.Contains(allowedDomains[i]))
                {
                    allowed = true;
                }
            }
            if(!allowed) { return false; }
            // at this point, assume it's allowed until proven otherwise
            allowed = true;
            for(int k = 0; k < disallowedUrls.Count; k++)
            {
                if (url.Contains(disallowedUrls[k]))
                {
                    allowed = false;
                }
            }
            return allowed;
        }

        private bool IsRecentUrl(string url, string lastMod)
        {
            return url.Contains("2017") || lastMod.StartsWith("2017");
        }
    }
}

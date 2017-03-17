using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Services;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WebCrawlerUtil;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WebDisplay
{
    [WebService(Namespace = "http://doane-assignment3.cloudapp.net/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ScriptService]
    public class SearchEngine : WebService
    {
        private const string LOCAL_STORAGE_NAME = "wikidata";
        private static LocalResource localResource = RoleEnvironment.GetLocalResource(LOCAL_STORAGE_NAME);
        private const string WIKI_TITLES = "titles_lower.txt";
        private const int MAX_WORDS = 10;
        private const int MIN_MB_FOR_TRIE = 50;
        private const int MAX_CACHE_SIZE = 100;

        private static PerformanceCounter ramPerformance = new PerformanceCounter("Memory", "Available MBytes");
        private static CloudConnection connection = new CloudConnection();
        private static Trie trie;
        private static Dictionary<string, DisplayUrl[]> cache = new Dictionary<string, DisplayUrl[]>(MAX_CACHE_SIZE);

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string[] Search(string q)
        {
            if(trie != null)
            {
                return trie.FindWords(q, MAX_WORDS);
            }
            else
            {
                return null;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public DisplayUrl[] SearchFromScraped(string q)
        {
            if (cache.ContainsKey(q))
            {
                return cache[q];
            }
            var results = connection.SearchForPhrase(q);
            if (cache.Count >= MAX_CACHE_SIZE - 1)
            {
                cache.Clear();
            }
            cache.Add(q, results);
            return results;
        }

        [WebMethod]
        public string DownloadWiki()
        {
            var fullPath = localResource.RootPath + WIKI_TITLES;
            // if the file already exists, remove it
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            CloudBlockBlob blob = connection.GetFromBlob(WIKI_TITLES);
            if (blob == null)
            {
                return "Error downloading blob - file doesn't exist in blob";
            }
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                blob.DownloadToStream(stream);
            }

            // simple check to make sure the file downloaded properly
            FileInfo downloadedBlob = new FileInfo(fullPath);
            if (downloadedBlob.Exists && downloadedBlob.Length > 0)
            {
                return "Successfully downloaded wiki titles. Resulting file size: " + downloadedBlob.Length + " bytes";
            }
            else
            {
                return "Error downloading blob, file appears to either not exist or is 0 bytes";
            }
        }

        [WebMethod]
        public string BuildTrie(byte trieListSize, int updateTime)
        {
            trie = new Trie(trieListSize);
            // Manually force a GC so we have maximum memory for the trie
            // NOTE that this is pretty slow, but since this function is only called once (and manually) it's no problem
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var fullPath = localResource.RootPath + WIKI_TITLES;
            TrieStatEntity stats = new TrieStatEntity(0, "");
            const int MAX_GC_TIME = 100000;
            int timeTillUpdate = updateTime;
            int timeTillGC = MAX_GC_TIME;
            float startMemory = ramPerformance.NextValue();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            try
            {
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    while (!reader.EndOfStream)
                    {
                        string title = reader.ReadLine();
                        trie.Add(title);
                        stats.UpdateLastTitle(title);
                        timeTillUpdate--;
                        timeTillGC--;
                        if (timeTillGC <= 0)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            timeTillGC = MAX_GC_TIME;
                        }
                        if (timeTillUpdate <= 0)
                        {
                            timeTillUpdate = updateTime;
                            if (ramPerformance.NextValue() <= MIN_MB_FOR_TRIE)
                            {
                                // always update on error
                                connection.UpdateTrieStats(stats);
                                return "Trie had to stop building due to memory constraints. Last word added: " + stats.LastTitle;
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                return "Couldn't build trie due to IOException. Make sure to download the data file before building the trie!";
            }
            watch.Stop();
            long buildTime = watch.ElapsedMilliseconds;
            watch.Reset();
            watch.Start();
            string[] found = trie.FindWords("test", 5);
            watch.Stop();
            long findTime = watch.ElapsedTicks;
            string output = string.Join(",", found);
            connection.UpdateTrieStats(stats);
            float memoryDiff = startMemory - ramPerformance.NextValue();
            return "Built trie in " + buildTime + "ms ||| searching for 5 words starting with 'test': " + output + " in " + findTime + " ticks ||| Ram used: " + memoryDiff;
        }
    }
}

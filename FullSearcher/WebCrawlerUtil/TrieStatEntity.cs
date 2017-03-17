using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebCrawlerUtil
{
    public class TrieStatEntity : TableEntity
    {
        public const string TRIE_STATS_PKEY = "triestats";
        public const string TRIE_STATS_RKEY = "uniquestats";

        public TrieStatEntity() { }
        public TrieStatEntity(int titleCount, string lastTitle)
        {
            this.PartitionKey = TRIE_STATS_PKEY;
            this.RowKey = TRIE_STATS_RKEY;

            this.TitleCount = titleCount;
            this.LastTitle = lastTitle;
        }

        public void UpdateLastTitle(string title)
        {
            LastTitle = title;
            TitleCount++;
        }

        public string LastTitle { get; set; }
        public int TitleCount { get; set; }
    }
}

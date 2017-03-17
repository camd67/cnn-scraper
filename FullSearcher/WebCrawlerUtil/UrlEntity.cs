using System;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace WebCrawlerUtil
{
    public class UrlEntity : TableEntity
    {
        private static Regex alphaNumeric = new Regex("[^a-zA-Z0-9 ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UrlEntity() { }
        public UrlEntity(string url, string title, string fullTitle, DateTime accessDate)
        {
            this.PartitionKey = CleanSearchWord(EncodeToTableKey(title));
            this.RowKey = EncodeToTableKey(url);
            System.Diagnostics.Trace.WriteLine(fullTitle);
            this.Title = DecodeFromHtml(fullTitle); ;
            this.Date = accessDate;
        }

        public string URL { get { return DecodeFromTableKey(this.RowKey); } }
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string SearchWord { get { return DecodeFromTableKey(this.PartitionKey); } }

        public static string CleanSearchWord(string searchWord)
        {
            return alphaNumeric.Replace(searchWord, string.Empty);
        }
        public static string CleanFullTitle(string fullTitle)
        {
            return alphaNumeric.Replace(fullTitle, " ");
        }

        public override string ToString()
        {
            return "PK{" + PartitionKey + "}RK:{" + RowKey + "}FullTitle:" + Title;
        }

        public static UrlEntity[] CreateEntitiesFromString(string fullTitle, string url, DateTime access)
        {
            List<UrlEntity> toReturn = new List<UrlEntity>();
            fullTitle = CleanFullTitle(fullTitle);
            string[] words = fullTitle.Split(' ');
            foreach(string word in words)
            {
                string wordToAdd = word.Trim().ToLower();
                if(wordToAdd.Length <= 0) { continue; }
                toReturn.Add(new UrlEntity(url, wordToAdd, fullTitle, access));
            }

            return toReturn.ToArray();
        }

        private string DecodeFromHtml(string s)
        {
            return s.Replace("  ", " ").Replace(" x27 ", "'").Replace(" 39 ", "'");
        }

        // https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/Understanding-the-Table-Service-Data-Model
        private static char[] illegalChars = {REPLACE_CHAR, '/', '#', '?', '\\'};
        //           encode/decode with { <0 , <1 , <2, etc  } REPLACE_CHAR has to go first so it doesn't replace others
        private const char REPLACE_CHAR = '<';

        public static string EncodeToTableKey(string s)
        {
            s = WebUtility.HtmlDecode(s);
            StringBuilder output = new StringBuilder();
            for(int i = 0; i < s.Length; i++)
            {
                bool foundIllegalChar = false;
                for(int k = 0; k < illegalChars.Length; k++)
                {
                    if(s[i] == illegalChars[k])
                    {
                        // output <0, <1, or <2, etc
                        output.Append(REPLACE_CHAR.ToString() + k);
                        foundIllegalChar = true;
                        break;
                    }
                }
                if (!foundIllegalChar)
                { // no illegal char? Just output
                    output.Append(s[i]);
                }
            }
            return output.ToString().ToLower();
        }

        public static string DecodeFromTableKey(string s)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if(s[i] == REPLACE_CHAR)
                {
                    int replaceIndex = (int)(s[i + 1] - '0');
                    output.Append(illegalChars[replaceIndex]);
                    i++;
                }
                else
                {
                    output.Append(s[i]);
                }
            }
            return output.ToString();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebCrawlerUtil
{
    public class Trie
    {
        private TrieNode root;

        public Trie() : this(10) { }
        public Trie(byte wordArraySize)
        {
            root = new TrieNode();
            TrieNode.MAX_WORD_ARRAY_SIZE = wordArraySize;
        }

        /// <summary>
        /// Adds a given string to the Trie
        /// </summary>
        /// <param name="value">The string to add</param>
        public void Add(string value)
        {
            root.Add(value);
        }

        /// <summary>
        /// Finds strings from the given prefix
        /// </summary>
        /// <param name="prefix">The prefix to search for</param>
        /// <param name="wordLimit">The max number of words to return from the search</param>
        /// <returns>The words found from the given prefix</returns>
        public string[] FindWords(string prefix, int wordLimit)
        {
            List<string> words = new List<string>();
            root.FindFromPrefix(prefix, "", words, wordLimit);
            return words.ToArray();
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            root.BuildWords(s, "");
            return s.ToString();
        }
        
    }// End of Trie
}
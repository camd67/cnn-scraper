using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerUtil
{
    class TrieNode
    {
        private Dictionary<char, TrieNode> children;
        private List<string> wordEndings;
        public static byte MAX_WORD_ARRAY_SIZE = 20;
        private const char END_OF_WORD = '%';

        public TrieNode() : this("") { }
        public TrieNode(string value)
        {
            wordEndings = new List<string>(MAX_WORD_ARRAY_SIZE);
            if (value != null && value != "")
            {
                wordEndings.Add(value);
            }
        }

        public void Add(string toAdd)
        {
            // First, check if we're using the dictionary or the array
            if (children != null)
            {
                if ((toAdd == null || toAdd.Length == 0))
                {
                    if (!children.ContainsKey(END_OF_WORD))
                        children.Add(END_OF_WORD, null);
                }
                else
                {
                    char firstLetter = toAdd[0];
                    toAdd = (toAdd.Length > 1 ? toAdd.Substring(1) : null);
                    if (children.ContainsKey(firstLetter))
                    {
                        children[firstLetter].Add(toAdd);
                    }
                    else
                    {
                        children.Add(firstLetter, new TrieNode(toAdd));
                    }
                }
            }
            else // using array
            {
                if (wordEndings.Count < MAX_WORD_ARRAY_SIZE)
                {
                    // still space for the word, add it
                    toAdd = toAdd == null ? "" : toAdd; // convert to empty string if null
                    if (!wordEndings.Contains(toAdd))
                    {
                        wordEndings.Add(toAdd);

                    }
                }
                else
                {
                    // no space for word! Remove array and add to children
                    children = new Dictionary<char, TrieNode>();
                    this.Add(toAdd);
                    for (int i = 0; i < wordEndings.Count; i++)
                    {
                        this.Add(wordEndings[i]);
                    }
                    wordEndings = null;
                }
            }
        }

        private bool EndingsContains(string ending)
        {
            for (int i = 0; i < wordEndings.Count; i++)
            {
                if (ending == wordEndings[i]) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Searches for words given a prefix
        /// </summary>
        /// <param name="prefix">Prefix to search for</param>
        /// <param name="currentWord">Current word buildup so far</param>
        /// <param name="output">Words to output</param>
        /// <param name="limit">Max number of words</param>
        public void FindFromPrefix(string prefix, string currentWord, List<string> output, int limit)
        {
            if (output.Count >= limit) { return; }

            if (prefix == "") // done looking for prefix, build up words
            {
                bool limitReached = NoPrefixSearch(currentWord, output, limit);
                if (limitReached) { return; }
            }
            else // Still looking for the end of the prefix
            {
                bool limitReached = PrefixSearch(prefix, currentWord, output, limit);
                if (limitReached) { return; }
            }
        }

        /// <summary>
        /// Helper function to buildup words assuming there's still a prefix to search for
        /// </summary>
        /// <param name="prefix">Prefix to search for</param>
        /// <param name="currentWord">The word so far</param>
        /// <param name="output">List of all words to output</param>
        /// <param name="limit">Max number of words to output</param>
        /// <returns></returns>
        private bool PrefixSearch(string prefix, string currentWord, List<string> output, int limit)
        {
            if (wordEndings != null)
            {
                // note that at this point, not all word endings may match
                for (int i = 0; i < wordEndings.Count; i++)
                {
                    if (output.Count >= limit) { return false; }
                    if (wordEndings[i].StartsWith(prefix))
                    {
                        output.Add(currentWord + wordEndings[i]);
                    }
                }
            }
            else
            {
                char current = prefix[0];
                string remaining = prefix.Length > 1 ? prefix.Substring(1) : "";
                if (children.ContainsKey(current))
                {
                    children[current].FindFromPrefix(remaining, currentWord + current, output, limit);
                }
            }
            return true;
        }

        /// <summary>
        /// Helper function that finds words assuming that there is no more prefix to search
        /// </summary>
        /// <param name="currentWord">The word so far</param>
        /// <param name="output">List of all words to output</param>
        /// <param name="limit">Max number of words to output</param>
        /// <returns>True if the limit has been reached, false otherwise</returns>
        private bool NoPrefixSearch(string currentWord, List<string> output, int limit)
        {
            if (wordEndings != null)
            {
                // If we have word endings + no prefix, add everything
                for (int i = 0; i < wordEndings.Count; i++)
                {
                    if (output.Count >= limit) { return false; }
                    output.Add(currentWord + wordEndings[i]);
                }
            }
            else
            {
                // Still need to traverse downwards to buildup the rest of the words
                if (children.ContainsKey(END_OF_WORD))
                {
                    output.Add(currentWord);
                }
                // Kinda slow to sort the keys, but it gives the right results
                foreach (var pair in children.OrderBy(x => x.Key))
                {
                    if (output.Count >= limit) { return false; }

                    if (pair.Key != END_OF_WORD)
                    {
                        pair.Value.FindFromPrefix("", currentWord + pair.Key, output, limit);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Builds up an output of all the words that are a child to this node
        /// </summary>
        /// <param name="output">The current output of all nodes up to this point</param>
        /// <param name="buildup">The current string buildup, should start at "" </param>
        public void BuildWords(StringBuilder output, string buildup)
        {
            if (wordEndings != null)
            {
                for (int i = 0; i < wordEndings.Count; i++)
                {
                    output.Append(buildup);
                    output.Append(wordEndings[i]);
                    output.Append(", ");
                }
            }
            else
            {
                if (this.IsEndOfWord())
                {
                    output.Append(buildup);
                    output.Append(", ");
                }
                foreach (var pair in children.OrderBy(x => x.Key))
                {
                    if (pair.Key != END_OF_WORD)
                    {
                        pair.Value.BuildWords(output, buildup + pair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to determine if the given node is the end of a word (leaf).
        /// NOTE that this function assumes that there are child nodes
        /// </summary>
        /// <returns>True if at the end of a word, false otherwise</returns>
        public bool IsEndOfWord()
        {
            return children.ContainsKey(END_OF_WORD) && children.Count == 1;
        }

    }
}

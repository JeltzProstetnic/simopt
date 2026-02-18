using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Utilities.IO
{
    public class StringWordsEnumerator : IEnumerator, IEnumerator<string>
    {
        #region cvar

        private string source;
        private Queue<string> currentLine;
        private string currentWord;
        private int minWordLength;
        private string[] splitStrings = { " ", "  ", "   ", "    " };

        private List<string> caseInvariantStopWords;
        private List<string> caseSensitiveStopWords;
        private Dictionary<string, string> sourceReplacements;
        private Dictionary<string, string> targetReplacements;

        #endregion
        #region prop

        #region IEnumerator

        public object Current { get { return currentWord; } }

        #endregion
        #region IEnumerator<string>

        string IEnumerator<string>.Current { get { return currentWord; } }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="caseInvariantStopWords">Must be lower case!</param>
        /// <param name="caseSensitiveStopWords"></param>
        /// <param name="sourceReplacements">These replacements are done on the raw data.</param>
        /// <param name="targetReplacements">These replacements are done on the processed words.</param>
        public StringWordsEnumerator(
            string source,
            int minWordLength = 5,
            List<string> caseInvariantStopWords = null,
            List<string> caseSensitiveStopWords = null, 
            Dictionary<string, string> sourceReplacements = null,
            Dictionary<string, string> targetReplacements = null)
        {
            currentLine = new Queue<string>();
         
            this.source = source;
            this.minWordLength = minWordLength;
            this.caseInvariantStopWords = caseInvariantStopWords;
            this.caseSensitiveStopWords = caseSensitiveStopWords;
            this.sourceReplacements = sourceReplacements;
            this.targetReplacements = targetReplacements;
        }

        #endregion
        #region impl

        #region IEnumerator

        public bool MoveNext()
        {
            // read next line if the current line queue is empty
            if (currentLine.Count == 0)
            {
                string line = ProcessLine(source);

                // split into single words
                string[] words = line.Split(splitStrings, StringSplitOptions.RemoveEmptyEntries);

                // process and enqueue words
                foreach (string word in words) ProcessWord(word);
            }

            currentWord = currentLine.Dequeue();

            return true;
        }

        public void Reset()
        {
            currentLine.Clear();
            currentWord = "";
        }

        #endregion
        #region IDisposable

        public void Dispose() { }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// apply source replacement, replace non letters and trim.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>value if value is null or empty</returns>
        private string ProcessLine(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string line = value;

            // apply source replacements
            foreach (KeyValuePair<string, string> kvp in sourceReplacements)
                line = line.Replace(kvp.Key, kvp.Value);

            // convert to plain text without interpunctation
            line = line.ReplaceNonLetters(' ').Trim();

            return line;
        }

        /// <summary>
        /// filter stop words, apply target replacements and enqueue
        /// </summary>
        /// <param name="word"></param>
        private void ProcessWord(string word)
        {
            // skip stop words
            if (!(caseSensitiveStopWords.Contains(word) || caseInvariantStopWords.Contains(word.ToLower())))
            {
                string finalWord = word;

                // apply target replacements
                foreach (KeyValuePair<string, string> kvp in targetReplacements)
                    finalWord = finalWord.Replace(kvp.Key, kvp.Value);

                // enqueue
                if (finalWord.Length >= minWordLength) currentLine.Enqueue(finalWord);
            }
        }

        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Utilities.IO
{
    /// <summary>
    /// Returns single words from a binary file using a <code>BinaryFileInfoStringsEnumerator</code>. 
    /// Similar to the <code>BinaryFileStringsEnumerator</code>, the words enumerator enumerates 
    /// strings within a binary data stream. (It actually uses a <code>BinaryFileStringsEnumerator</code>
    /// internally) But in contrast to the above the strings are split at all non-letters. Furthermore 
    /// a list of case sensitive and a list of case insensitive stop words is applied and final 
    /// replacements can be configured to take place on the words before they are returned.
    /// </summary>
    public class BinaryFileWordsEnumerator : IEnumerator, IEnumerator<string>
    {
        #region cvar
        
        private Queue<string> currentLine;
        private int minWordLength;
        private string currentWord;
        private string[] splitStrings = { " ", "  ", "   ", "    " };

        #endregion
        #region prop

        /// <summary>
        /// A dictionary of strings which were found next to the given field names.
        /// </summary>
        public Dictionary<string, List<string>> IdentifiedFields { get { return StringsEnumerator.IdentifiedFields; } }

        /// <summary>
        /// The strings enumerator which is used internally.
        /// </summary>
        public BinaryFileStringsEnumerator StringsEnumerator { get; set; }

        /// <summary>
        /// Words which are to be skipped independent of casing.
        /// Caution: must be lower case!
        /// </summary>
        public List<string> CaseInvariantStopWords { get; set; }
        
        /// <summary>
        /// Words which are to be skipped.
        /// </summary>
        public List<string> CaseSensitiveStopWords { get; set; }

        /// <summary>
        /// These replacements are done on the processed words.
        /// </summary>
        public Dictionary<string, string> TargetReplacements { get; set; }

        #region IEnumerator

        public object Current { get { return currentWord; } }

        #endregion
        #region IEnumerator<string>

        string IEnumerator<string>.Current { get { return currentWord; } }

        #endregion

        #endregion
        #region ctor

        public BinaryFileWordsEnumerator()
        {
            this.CaseInvariantStopWords = new List<string>();
            this.CaseSensitiveStopWords = new List<string>();
            this.TargetReplacements = new Dictionary<string,string>();
        }

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="file">The file to process.</param>
        /// <param name="minWordLength">The shortest possible word to return. This is checked after all other processing is done.</param>
        /// <param name="minTextLength">The shortest possible string to process. This is measured after the source replacements have been applied.</param>
        /// <param name="maxTextLength">The maximum length for a string to be identified and returned. Longer strings - if found - will be truncated.</param>
        /// <param name="nonLetterTolerance">If set to zero, every string will be terminated at the first occurrence of a non-legal character byte.</param>
        /// <param name="nonLetterReplacement">If null, the non letter itself will be appended. Use empty string to skip or any other string to replace.</param>
        /// <param name="wordStartCandidates">
        /// A number of bytes which may indicate the beginning of a text string. If none are provided the class will use
        /// the characters from <code>SystemTools.ValidLetters</code>.
        /// </param>
        /// <param name="legalCharacterBytes">
        /// A number of bytes which are to be treated as valid within a text string. If none are provided the class will use
        /// the characters from <code>SystemTools.ValidLetters</code>.</param>
        /// <param name="caseInvariantStopWords">Words which are to be skipped independent of casing. Must be lower case!</param>
        /// <param name="caseSensitiveStopWords">Words which are to be skipped.</param>
        /// <param name="byteReplacements">These replacements are done on the raw data.</param>
        /// <param name="sourceReplacements">These replacements are done on the preprocessed strings.</param>
        /// <param name="targetReplacements">These replacements are done on the processed words.</param>
        public BinaryFileWordsEnumerator(
            FileInfo file, 
            int minWordLength = 5,
            int minTextLength = 3,
            int maxTextLength = 256, 
            int nonLetterTolerance = 1,
            string nonLetterReplacement = null,
            Dictionary<string, List<string>> fieldIdentifiers = null,
            List<byte> wordStartCandidates = null,
            List<byte> legalCharacterBytes = null,
            List<string> caseInvariantStopWords = null,
            List<string> caseSensitiveStopWords = null,
            Dictionary<byte, byte> byteReplacements = null, 
            Dictionary<string, string> sourceReplacements = null,
            Dictionary<string, string> targetReplacements = null,
            List<byte> blackListCharacterBytes = null) 
            : this()
        {
            StringsEnumerator = new BinaryFileStringsEnumerator(
                file,
                minTextLength,
                maxTextLength,
                nonLetterTolerance,
                nonLetterReplacement,
                fieldIdentifiers,
                wordStartCandidates,
                legalCharacterBytes,
                byteReplacements,
                sourceReplacements,
                blackListCharacterBytes);

            currentLine = new Queue<string>();

            this.minWordLength = minWordLength;

            if (targetReplacements != null) this.TargetReplacements = targetReplacements;
            if (caseInvariantStopWords != null) this.CaseInvariantStopWords = caseInvariantStopWords;
            if (caseSensitiveStopWords != null) this.CaseSensitiveStopWords = caseSensitiveStopWords;
        }

        #endregion
        #region impl

        #region IEnumerator

        /// <summary>
        /// Process the next word in the stream.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            // read next line if the current line queue is empty
            while (currentLine.Count == 0)
            {
                // try to get strings from the file
                if (!StringsEnumerator.MoveNext()) return false;

                // read next line if the current line is empty
                if (!string.IsNullOrEmpty(StringsEnumerator.Current as string))
                {
                    // convert to plain text without interpunctation
                    string line = (StringsEnumerator.Current as string).ReplaceNonLetters(' ').Trim();

                    // split into single words
                    string[] words = line.Split(splitStrings, StringSplitOptions.RemoveEmptyEntries);

                    // process and enqueue words
                    foreach (string word in words) ProcessWord(word);
                }
            }

            currentWord = currentLine.Dequeue();

            return true;
        }

        /// <summary>
        /// Reset this instance.
        /// </summary>
        public void Reset()
        {
            StringsEnumerator.Reset();
            currentLine.Clear();
            currentWord = "";
        }

        #endregion
        #region IDisposable

        public void Dispose()
        {
            StringsEnumerator.Dispose();
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// filter stop words, apply target replacements, check length and enqueue
        /// </summary>
        /// <param name="word"></param>
        private void ProcessWord(string word)
        {
            // skip stop words
            if (!(CaseSensitiveStopWords.Contains(word) || CaseInvariantStopWords.Contains(word.ToLower())))
            {
                string finalWord = word;

                // apply target replacements
                foreach (KeyValuePair<string, string> kvp in TargetReplacements)
                    finalWord = finalWord.Replace(kvp.Key, kvp.Value);

                // enqueue
                if(finalWord.Length >= minWordLength) currentLine.Enqueue(finalWord);
            }
        }

        #endregion
    }
}
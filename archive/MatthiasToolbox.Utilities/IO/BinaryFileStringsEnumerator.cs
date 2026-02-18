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
    /// Strings enumerator for a binary file. This class enumerates strings within a binary stream. 
    /// Strings will be identified within the range of a sliding stream window of configurable size
    /// by a list of word start candidate bytes and a minimum number of subsequent bytes from the list
    /// <code>LegalCharacterBytes</code>.
    /// Prior to comparison with the <code>LegalCharacterBytes</code> list the bytes are transformed
    /// as defined in the dictionary <code>SourceReplacements</code>. A tolerance (in bytes) for non
    /// legal characters can be defined. These will either be skipped (if <code>AppendForToleratedNonLetter</code>
    /// is set to an empty string), appended as they are (if <code>AppendForToleratedNonLetter</code> is set to null) 
    /// or replaced by the value in <code>AppendForToleratedNonLetter</code>.
    /// After this step and whitespace trimming the replacements as defined in <code>TargetReplacements</code>
    /// are applied. If the result's length is greater than or equal to the defined minimum length it
    /// will be returned.
    /// </summary>
    public class BinaryFileStringsEnumerator : IEnumerator, IEnumerator<string>
    {
        #region cvar

        private SlidingStreamWindow swin;
        private FileStream sr;
        private FileInfo file;
        private bool isOpen;
        private string currentLine;
        
        #endregion
        #region prop

        /// <summary>
        /// A dictionary of named fields with possible match strings which precede field values.
        /// </summary>
        public Dictionary<string, List<string>> FieldIdentifiers { get; set; }

        /// <summary>
        /// A dictionary of strings which were found next to the FieldIdentifiers.
        /// </summary>
        public Dictionary<string, List<string>> IdentifiedFields { get; set; }

        /// <summary>
        /// These replacements are done on the raw data.
        /// </summary>
        public Dictionary<byte, byte> SourceReplacements { get; set; }
        
        /// <summary>
        /// These replacements are done on the processed words.
        /// </summary>
        public Dictionary<string, string> TargetReplacements { get; set; }

        /// <summary>
        /// The number of subsequent bytes not in the legal characters list to ignore when terminating strings.
        /// </summary>
        public int NonLetterTolerance { get; set; }

        /// <summary>
        /// If null, the non letter itself will be appended. Use empty string to skip or any other string to replace.
        /// </summary>
        public string AppendForToleratedNonLetter { get; set; }

        /// <summary>
        /// The shortest possible string to return. This is measured after target replacements have been applied.
        /// </summary>
        public int MinimumTextLength { get; set; }

        /// <summary>
        /// The maximum length for a string to be identified and returned. Longer strings - if found - will be truncated.
        /// </summary>
        public int MaximumTextLength { get; set; }

        /// <summary>
        /// A number of bytes which may indicate the beginning of a text string.
        /// </summary>
        public List<byte> WordStartCandidates { get; set; }

        /// <summary>
        /// A number of bytes which are to be treated as valid within a text string.
        /// </summary>
        public List<byte> LegalCharacterBytes { get; set; }

        /// <summary>
        /// A number of bytes which will result in skipping the current window.
        /// </summary>
        public List<byte> BlackListCharacterBytes { get; set; }

        #region IEnumerator

        public object Current { get { return currentLine; } }

        #endregion
        #region IEnumerator<string>

        string IEnumerator<string>.Current { get { return currentLine; } }

        #endregion

        #endregion
        #region ctor
        
        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="file">The file to process.</param>
        /// <param name="minTextLength">The shortest possible string to return. This is measured after target replacements have been applied.</param>
        /// <param name="maxTextLength">The maximum length for a string to be identified and returned. Longer strings - if found - will be truncated.</param>
        /// <param name="nonLetterTolerance">If set to zero, every string will be terminated at the first occurrence of a non-legal character byte.</param>
        /// <param name="nonLetterReplacement">If null, the non letter itself will be appended. Use empty string to skip or any other string to replace.</param>
        /// <param name="wordStartCandidates">
        /// A number of bytes which may indicate the beginning of a text string. If none are provided the class will use
        /// the characters from <code>SystemTools.ValidLetters</code>.
        /// </param>
        /// <param name="legalCharacterBytes">
        /// A number of bytes which are to be treated as valid within a text string. If none are provided the class will use
        /// the characters from <code>SystemTools.ValidLetters</code>.
        /// </param>
        /// <param name="sourceReplacements">These replacements are done on the raw data.</param>
        /// <param name="targetReplacements">These replacements are done on the processed strings.</param>
        public BinaryFileStringsEnumerator(
            FileInfo file,
            int minTextLength = 3,
            int maxTextLength = 256, 
            int nonLetterTolerance = 1,
            string nonLetterReplacement = null,
            Dictionary<string, List<string>> fieldIdentifiers = null,
            List<byte> wordStartCandidates = null, 
            List<byte> legalCharacterBytes = null,
            Dictionary<byte, byte> sourceReplacements = null,
            Dictionary<string, string> targetReplacements = null,
            List<byte> blackListCharacterBytes = null)
        {
            if (wordStartCandidates == null)
            {
                this.WordStartCandidates = new List<byte>();
                foreach (char c in SystemTools.ValidLetters)
                    wordStartCandidates.Add((byte)c);
            }
            else
            {
                this.WordStartCandidates = wordStartCandidates;
            }

            if (legalCharacterBytes == null)
            {
                this.LegalCharacterBytes = new List<byte>();
                foreach (char c in SystemTools.ValidLetters)
                    legalCharacterBytes.Add((byte)c);
            }
            else
            {
                this.LegalCharacterBytes = legalCharacterBytes;
            }

            if (blackListCharacterBytes == null)
                BlackListCharacterBytes = new List<byte>();
            else 
                BlackListCharacterBytes = blackListCharacterBytes;

            this.file = file;
            this.MinimumTextLength = minTextLength;
            this.MaximumTextLength = maxTextLength;
            this.NonLetterTolerance = nonLetterTolerance;
            this.AppendForToleratedNonLetter = nonLetterReplacement;
            this.IdentifiedFields = new Dictionary<string, List<string>>();
            this.FieldIdentifiers = fieldIdentifiers != null ? fieldIdentifiers : new Dictionary<string, List<string>>();
            this.SourceReplacements = sourceReplacements != null ? sourceReplacements : new Dictionary<byte, byte>();
            this.TargetReplacements = targetReplacements != null ? targetReplacements : new Dictionary<string, string>();

            foreach (string key in FieldIdentifiers.Keys) IdentifiedFields[key] = new List<string>();
        }

        #endregion
        #region impl

        #region IEnumerator

        /// <summary>
        /// Process the stream for the next string.
        /// </summary>
        /// <returns>True if a further string is available or false if the end of the file has been reached.</returns>
        public bool MoveNext()
        {
            // open file if not already done
            if(!Open()) return false;

            // go bytewise over the file 
            foreach (byte[] b in swin)
            {
                // maybe found beginning of a string
                if(WordStartCandidates.Contains(b[0]))
                {
                    // process the current window
                    string candidate = ProcessBytes(b);
                    string processedCandidate = candidate.Trim();

                    if (!string.IsNullOrEmpty(processedCandidate))
                    {
                        // apply target replacements
                        foreach (KeyValuePair<string, string> kvp in TargetReplacements)
                            processedCandidate = processedCandidate.Replace(kvp.Key, kvp.Value);

                        // find field values
                        FindFieldValuesIn(processedCandidate);
                    }

                    // advance window to after candidate
                    swin.Skip(candidate.Length);

                    // accept string if length fits
                    if (processedCandidate.Length > MinimumTextLength)
                    {
                        currentLine = processedCandidate;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Reset this instance.
        /// </summary>
        public void Reset()
        {
            if (sr != null)
            {
                try
                {
                    sr.Close();
                }
                catch { /* NO RECOVERY POSSIBLE */ }
            }
            currentLine = "";
            isOpen = false;
        }

        #endregion
        #region IDisposable

        public void Dispose()
        {
            if (sr != null)
            {
                try
                {
                    sr.Close();
                }
                catch { /* NO RECOVERY POSSIBLE */ }
            }
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// 
        /// </summary>
        /// <returns>false only in case of an exception</returns>
        private bool Open()
        {
            if (!isOpen)
            {
                try
                {
                    sr = file.OpenRead();
                    swin = new SlidingStreamWindow(sr, MaximumTextLength, 2);
                }
                catch (Exception ex)
                {
                    this.Log<ERROR>("The BinaryFileInfoWordEnumerator was unable to open the file <" + file.FullName + ">.", ex);
                    return false;
                }
                isOpen = true;
            }
            return true;
        }

        /// <summary>
        /// apply source replacements and find delimiter (with respect to nonLetterTolerance)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string ProcessBytes(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            byte bResult;
            int garbageCount = 0;

            foreach (byte b in bytes)
            {
                bResult = b;
                foreach (KeyValuePair<byte, byte> kvp in SourceReplacements)
                    if (kvp.Key == b) bResult = kvp.Value;

                if (!LegalCharacterBytes.Contains(bResult))
                {
                    if (BlackListCharacterBytes.Contains(bResult)) break;
                    garbageCount += 1;
                    if (garbageCount <= NonLetterTolerance)
                    {
                        if (AppendForToleratedNonLetter == null) sb.Append((char)bResult);
                        else if (AppendForToleratedNonLetter != "") sb.Append(AppendForToleratedNonLetter);
                    } 
                    else break;
                }
                else sb.Append((char)bResult);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Try to match the string with a field name.
        /// </summary>
        /// <param name="processedCandidate"></param>
        private void FindFieldValuesIn(string processedCandidate)
        {
            foreach (KeyValuePair<string, List<string>> kvp in FieldIdentifiers)
            {
                foreach (string match in kvp.Value)
                {
                    if (processedCandidate.ToLower().StartsWith(match.ToLower()))
                        IdentifiedFields[kvp.Key].Insert(0, processedCandidate.Substring(match.Length));
                    else if (processedCandidate.Contains(match))
                        IdentifiedFields[kvp.Key].Add(processedCandidate);
                    else continue;
                    return;
                }
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MatthiasToolbox.Semantics.Dictionary
{
    public class EnglishEnumerator : IEnumerator<string>
    {
        private static FileInfo file = new FileInfo("Resources\\english.txt");
        private TextReader textReader;
        private string current;

        public EnglishEnumerator()
        {
            textReader = file.OpenText();
        }

        #region IEnumerator<string>

        public string Current
        {
            get { return current; }
        }

        #endregion
        #region IDisposable

        public void Dispose()
        {
            try
            {
                if (textReader != null)
                {
                    textReader.Close();
                    textReader.Dispose();
                }
            }
            finally
            {
                textReader = null;
            }
        }

        #endregion
        #region IEnumerator

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            try
            {
                current = textReader.ReadLine();
                if (current == null) return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void Reset()
        {
            current = null;

            try
            {
                textReader.Close();
            }
            catch { /* Whatever */ }

            textReader = file.OpenText();
        }

        #endregion
    }
}

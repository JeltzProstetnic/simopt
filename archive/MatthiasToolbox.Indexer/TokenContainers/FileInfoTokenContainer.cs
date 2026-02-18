using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Utilities.IO;
using System.IO;

namespace MatthiasToolbox.Indexer.TokenContainers
{
    public class FileInfoTokenContainer : IEnumerable<string>
    {
        private IEnumerator<string> enumerator;

        public TextFileWordsEnumerator TextFileEnumerator { get { return enumerator as TextFileWordsEnumerator; } }
        public BinaryFileWordsEnumerator BinaryFileEnumerator { get { return enumerator as BinaryFileWordsEnumerator; } }

        public FileInfoTokenContainer(string path, bool isUTF8) 
        {
            if (isUTF8) enumerator = new TextFileWordsEnumerator(new FileInfo(path));
            else enumerator = new BinaryFileWordsEnumerator(new FileInfo(path));
        }

        #region IEnumerable<string> Member

        public IEnumerator<string> GetEnumerator()
        {
            return enumerator;
        }

        #endregion

        #region IEnumerable Member

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return enumerator;
        }

        #endregion
    }
}
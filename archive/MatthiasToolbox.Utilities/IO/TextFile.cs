using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MatthiasToolbox.Utilities.IO
{
    public class TextFile : IEnumerable<string>
    {
        private FileInfo file;

        public TextFile(FileInfo file)
        {
            this.file = file;
        }

        public TextFile(String file)
        {
            this.file = new FileInfo(file);
        }

        #region IEnumerable<string>

        public IEnumerator<string> GetEnumerator()
        {
            return new TextFileLinesEnumerator(file);
        }

        #endregion
        #region IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TextFileLinesEnumerator(file);
        }

        #endregion
    }
}
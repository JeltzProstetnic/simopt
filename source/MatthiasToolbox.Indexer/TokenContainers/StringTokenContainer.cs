using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Interfaces;

namespace MatthiasToolbox.Indexer.TokenContainers
{
    /// <summary>
    /// TODO: implement equivalent of TextFileInfoWordEnumerator
    /// </summary>
    public class StringTokenContainer : IEnumerable<string>
    {
        #region IEnumerable<string> Member

        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Member

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

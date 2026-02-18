using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Database;

namespace MatthiasToolbox.Indexer.Interfaces
{
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Process a document (update tokens and create token occurances) and return the token count.
        /// </summary>
        int ProcessDocument(Document document);
    }
}

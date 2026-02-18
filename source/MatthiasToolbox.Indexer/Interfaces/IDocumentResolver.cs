using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Database;
using System.IO;

namespace MatthiasToolbox.Indexer.Interfaces
{
    public interface IDocumentResolver
    {
        double Priority { get; }
        bool CanResolve(Document document);
        IEnumerable<IVariableContainer<string, string>> CurrentMetaData { get; }
        IEnumerable<string> Resolve(Document document);
    }
}
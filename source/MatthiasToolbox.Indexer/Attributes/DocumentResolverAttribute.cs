using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Indexer.Attributes
{
    /// <summary>
    /// An attribute to mark a resolver to be used by the indexing service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DocumentResolverAttribute : Attribute
    {
        /// <summary>
        /// Default constructor. This will make the marked class the only document resolver. 
        /// If a file is not resolvable using this resolver it will be ignored.
        /// </summary>
        public DocumentResolverAttribute() { }
    }
}
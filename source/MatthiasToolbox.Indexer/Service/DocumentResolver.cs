using System;
using System.Collections.Generic;

using MatthiasToolbox.Indexer.Database;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Indexer.Service
{
    /// <summary>
    /// A default document resolver implementation as fallback.
    /// </summary>
    public class DocumentResolver : IDocumentResolver
    {
        #region cvar

        private static int instanceCounter = 0;
        private Predicate<Document> resolvablePredicate;
        private Func<Document, IEnumerable<string>> resolverMethod;

        #endregion
        #region prop

        public double Priority { get; set; }
        public IEnumerable<IVariableContainer<string, string>> CurrentMetaData { get; set; }

        #endregion
        #region ctor

        public DocumentResolver(Predicate<Document> resolvablePredicate, Func<Document, IEnumerable<string>> resolverMethod)
        {
            this.Priority = -double.MaxValue + instanceCounter;
            this.resolvablePredicate = resolvablePredicate;
            this.resolverMethod = resolverMethod;

            instanceCounter++;
        }

        #endregion
        #region impl

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public bool CanResolve(Document document)
        {
            return resolvablePredicate.Invoke(document);
        }

        /// <summary>
        /// Returns the content data for the given document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns>Null if an exception occurs. Make sure that this resolver can 
        /// resolve the document using IsResolvable first to avoid this.</returns>
        public IEnumerable<string> Resolve(Document document)
        {
            try
            {
                return resolverMethod.Invoke(document);
            }
            catch (Exception ex)
            {
                this.Log<ERROR>("The document resolver was unable to resolve the document at <" + document.Path + ">.", ex);
                return null; 
            }
        }

        #endregion
    }
}
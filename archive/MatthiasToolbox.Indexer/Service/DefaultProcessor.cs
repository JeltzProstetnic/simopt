using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Database;
using MatthiasToolbox.Indexer.Enumerations;
using System.IO;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Indexer.TokenContainers;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Indexer.Service
{
    /// <summary>
    /// TODO: for really huge documents: process without memorizing (requires a WorkInProgress flag in the token occurrence table though)
    /// </summary>
    public class DefaultProcessor : IDocumentProcessor
    {
        #region cvar

        private IndexDatabase database;
        private bool useDictionary = true;
        private List<TokenOccurrence> temporaryOccurrences;
        private Dictionary<string, List<int>> temporaryDocumentData;

        #endregion
        #region prop

        public List<IDocumentResolver> DocumentResolvers { get; set; }

        public Action<Document> PathTypeResolver { get; set; }

        public Action<Document> DocumentTypeResolver { get; set; }

        /// <summary>
        /// If set to true, a dictionary will be used for the processing of documents. This is faster but consumes a lot of memory.
        /// Otherwise the document tokens will be processed sequentially using less memory but more db communication. Default is true.
        /// </summary>
        public bool UseDictionary { get { return useDictionary; } set { useDictionary = value; } }

        /// <summary>
        /// Leave this empty if you prefer to process all strings.
        /// </summary>
        public List<string> StopWords { get; set; }

        /// <summary>
        /// Default is false!
        /// </summary>
        public bool UseStopWords { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// Empty constructor. You need to set the database and add resolvers to use this.
        /// </summary>
        public DefaultProcessor() 
        {
            DocumentResolvers = new List<IDocumentResolver>();
            temporaryDocumentData = new Dictionary<string, List<int>>();
            temporaryOccurrences = new List<TokenOccurrence>();
            StopWords = new List<string>();

            PathTypeResolver = DefaultPathTypeResolver;
            DocumentTypeResolver = DefaultDocumentTypeResolver;
        }

        /// <summary>
        /// Default constructor. You need to add resolvers to use this.
        /// </summary>
        /// <param name="database"></param>
        public DefaultProcessor(IndexDatabase database, bool loadStopWords = true) : this()
        {
            this.database = database;
            if (loadStopWords) LoadStopWords();
        }

        #endregion
        #region impl

        /// <summary>
        /// Set the database to use for index building.
        /// </summary>
        /// <param name="database"></param>
        public void SetDatabase(IndexDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// Set simple resolvers for plaintext and binary files.
        /// </summary>
        /// <param name="idResolver">If not null, the first resolver will be looking for identifiers.</param>
        public void SetDefaultResolvers(Func<string, IEnumerable<string>> idResolver = null)
        {
            if (idResolver != null)
            {
                DocumentResolvers.Add(
                    new DocumentResolver(
                        d => d.PathType == PathType.Identifier && d.DocumentType == DocumentType.PlainText,
                        d => idResolver.Invoke(d.Path)));
            }

            DocumentResolvers.Add(
                new DocumentResolver(
                    d => d.PathType == PathType.FilePath && d.DocumentType == DocumentType.PlainText, 
                    d => new FileInfoTokenContainer(d.Path, true)));

            DocumentResolvers.Add(
                new DocumentResolver(
                    d => d.PathType == PathType.FilePath && d.DocumentType == DocumentType.Binary,
                    d => new FileInfoTokenContainer(d.Path, false)));
        }

        /// <summary>
        /// Load the stop words from the IndexDatabase
        /// </summary>
        public void LoadStopWords()
        {
            try
            {
                foreach (StopWord s in database.StopWordTable)
                    StopWords.Add(s.Word);
            }
            catch (Exception e)
            {
                this.Log<ERROR>("Error loading the stop-words. Check db connection.", e);
            }
        }

        /// <summary>
        /// Retrieve the token data container for the provided document
        /// trying to use the document resolvers subsequently. If no
        /// resolution is found this returns null.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public IEnumerable<string> GetNativeDocument(Document document)
        {
            if (document.PathType == PathType.Unknown) PathTypeResolver.Invoke(document);
            
            if (document.DocumentType == DocumentType.Unknown) DocumentTypeResolver.Invoke(document);
            
            foreach (IDocumentResolver resolver in DocumentResolvers)
            {
                if (resolver.CanResolve(document))
                {
                    return resolver.Resolve(document);
                }
            }

            return null;
        }

        #region IDocumentProcessor

        /// <summary>
        /// Process the document. If the document has no data
        /// it will be retrieved using the resolvers.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public int ProcessDocument(Document document)
        {
            IEnumerable<string> data;

            if (!document.HasData)
                data = GetNativeDocument(document);
            else
                data = document.Data;

            if (data == null) return 0;

            if (useDictionary)
                return ProcessUsingDictionary(data, document);
            else
                return ProcessUsingList(data, document);
        }

        private int ProcessUsingDictionary(IEnumerable<string> data, Document document)
        {
            int count = 0;
            int currentPosition = 1;

            temporaryDocumentData.Clear();

            foreach (string item in data)
            {
                if (!UseStopWords || !StopWords.Contains(item))
                {
                    if (!temporaryDocumentData.ContainsKey(item))
                        temporaryDocumentData[item] = new List<int>();
                    temporaryDocumentData[item].Add(currentPosition);
                    currentPosition += 1;
                    count += 1;
                }
            }

            foreach (KeyValuePair<string, List<int>> kvp in temporaryDocumentData)
            {
                Token t = database.GetToken(kvp.Key);
                TokenOccurrence o = database.GetOccurrence(t, document);
                o.ProcessPositionList(kvp.Value, count, database.CalculateTokenPositionVariance);
            }

            return count;
        }

        private int ProcessUsingList(IEnumerable<string> data, Document document)
        {
            int count = 0;
            int currentPosition = 1;
            
            temporaryOccurrences.Clear();

            foreach (string item in data)
            {
                if (!StopWords.Contains(item))
                {
                    Token t = database.GetToken(item);
                    TokenOccurrence o = database.GetOccurrence(t, document);
                    o.AppendPosition(currentPosition);
                    temporaryOccurrences.Add(o);
                    currentPosition += 1;
                    count += 1;
                }
            }

            foreach (TokenOccurrence o in temporaryOccurrences)
            {
                o.ProcessPositionList(count, database.CalculateTokenPositionVariance);
            }

            return count;
        }

        #endregion

        #endregion
        #region util

        /// <summary>
        /// This will store the naively resolved document type into the document's respective property.
        /// 
        /// CAUTION: this is not completely reliable. Any document not containing a "." in it's path 
        /// will be classified as plain text. Other types will be resolved solely based on lower case 
        /// extensions. e.g. if the path ends with ".htm" it will be resolved as html file. If the
        /// extension is not recognized but a "." is contained in the path it will be resolved as binary.
        /// </summary>
        /// <param name="document">
        /// A document of which the DocumentType property is to be determined.
        /// </param>
        public void DefaultDocumentTypeResolver(Document document)
        {
            if (document.DocumentType != DocumentType.Unknown) return;

            if (document.Path.EndsWith(".htm") ||
                document.Path.StartsWith(".html"))
                document.DocumentType = DocumentType.Html;
            else if (document.Path.EndsWith(".xml") ||
                document.Path.EndsWith(".xaml"))
                document.DocumentType = DocumentType.Xml;
            else if (document.Path.EndsWith(".rtf"))
                document.DocumentType = DocumentType.Rtf;
            else if (document.Path.EndsWith(".pdf"))
                document.DocumentType = DocumentType.Pdf;
            else if (document.Path.EndsWith(".doc"))
                document.DocumentType = DocumentType.Doc;
            else if (document.Path.EndsWith(".docx"))
                document.DocumentType = DocumentType.DocX;
            else if (document.Path.EndsWith(".csv"))
                document.DocumentType = DocumentType.Csv;
            else if (document.Path.EndsWith(".xls"))
                document.DocumentType = DocumentType.Xls;
            else if (document.Path.EndsWith(".xlsx"))
                document.DocumentType = DocumentType.XlsX;
            else if (document.Path.EndsWith(".ppt"))
                document.DocumentType = DocumentType.Ppt;
            else if (document.Path.EndsWith(".pptx"))
                document.DocumentType = DocumentType.PptX;
            else if (document.Path.EndsWith(".lit"))
                document.DocumentType = DocumentType.Lit;
            else if (!document.Path.Contains('.'))
                document.DocumentType = DocumentType.PlainText;
            else
                document.DocumentType = DocumentType.Binary;
        }

        /// <summary>
        /// This will store the naively resolved path type into the document's respective property.
        /// 
        /// CAUTION: this is not completely reliable. If the path starts with "http:", "https:"
        /// or "www." it will be identified as web path, if it starts with ftp: -> ftp path. 
        /// If a file exists with the given path, it will be identified as file path and in 
        /// all other cases as "identifier". Pitfalls: if you use "1" as identifier and a file 
        /// named "1" without extension exists at the local path this will fail. If you use 
        /// relative paths and your filename starts with "www." this will also fail. And if 
        /// your web address is relative or starts with "www2." or something alike this will 
        /// also fail. 
        /// 
        /// Do not use this resolver if such cases are possible. Write your own resolver 
        /// based on your actual usage of the library.
        /// </summary>
        /// <param name="document">
        /// A document of which the PathType property is to be determined.
        /// </param>
        public void DefaultPathTypeResolver(Document document)
        {
            if (document.PathType != PathType.Unknown) return;

            if (document.Path.StartsWith("http:") ||
                document.Path.StartsWith("https:") ||
                document.Path.StartsWith("www."))
                document.PathType = PathType.WebPath;
            else if (document.Path.StartsWith("ftp:"))
                document.PathType = PathType.FtpPath;
            else if (File.Exists(document.Path))
                document.PathType = PathType.FilePath;
            else
                document.PathType = PathType.Identifier;
        }

        #endregion
    }
}
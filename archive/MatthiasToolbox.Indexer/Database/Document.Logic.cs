using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Indexer.Enumerations;
using System.IO;
using MatthiasToolbox.Indexer.Service;
using MatthiasToolbox.Indexer.Interfaces;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Indexer.Database
{
    /// <summary>
    /// TODO: throw exceptions / log if datasource was not set and datasource dependend properties are queried.
    /// </summary>
    public partial class Document
    {
        #region cvar

        private string metaDataBlock;
        private DateTime now = DateTime.Now;
        private int? distinctTokenCount;
        private Dictionary<int, Token> contents = new Dictionary<int, Token>();

        #endregion
        #region prop

        /// <summary>
        /// Set this to the IndexDatabase to which this instance relates.
        /// </summary>
        public static IndexDatabase DataSource { get; set; }

        public DocumentType DocumentType { get; set; }
        public PathType PathType { get; set; }
        public bool HasData { get; set; }
        public IEnumerable<string> Data { get; set; }

        public string FileName { get { return File.Name; } }
        public string Name { get { return File.FileNameWithoutExtension(); } }
        public long Size { get { return File.Length; } }
        public DateTime CreationDate { get { return File.CreationTime; } }
        
        public FileInfo File
        {
            get
            {
                if (file == null) file = new FileInfo(Path);
                return file;
            }
        }

        public int DistinctTokenCount 
        { 
            get 
            {
                if (!distinctTokenCount.HasValue)
                    distinctTokenCount = DataSource.TokenOccurrenceTable.Where(t => t.DocumentID == ID).Count();
                
                return distinctTokenCount.Value;
            } 
        }

        public string MetaDataBlock
        {
            get
            {
                if (metaDataBlock == null)
                {
                    metaDataBlock = "";
                    foreach (MetaData md in this.MetaData)
                    {
                        metaDataBlock += md.FieldName + "=" + md.Data + "\n";
                    }
                    metaDataBlock = metaDataBlock.Substring(0, metaDataBlock.Length - 1);
                }
                return metaDataBlock;
            }
        }

        /// <summary>
        /// Between 0 and 100, default is 50.
        /// </summary>
        public float AverageRating
        {
            get
            {
                var q = from row in DataSource.ResultEvaluationTable
                        where row.DocumentID == ID
                        select row.Rating;
                if (!q.Any()) return 50;
                else return q.Average();
            }
        }

        public TimeSpan Age
        {
            get
            {
                return now.Subtract(ModificationDate);
            }
        }

        public double AgeDays
        {
            get
            {
                return now.Subtract(ModificationDate).TotalDays;
            }
        }

        public SearchQuery CurrentQuery { get; set; }

        public int ApproximateHitsForCurrentQuery { get { return CurrentQuery == null ? 0 : CountApproximateHits(CurrentQuery.OptionalTerms); } }

        public int ExactHitsForCurrentQuery { get { return CurrentQuery == null ? 0 : CountExactHits(CurrentQuery.OptionalTerms); } }

        public string CurrentQueryTermsNeighbourhood
        {
            get
            {
                string result = "";
                if (CurrentQuery != null && DisplayNeighbourhood)
                {
                    foreach (Token t in CurrentQuery.AllSearchedTokens)
                    {
                        string n = GetFirstNeighbourhoodBlock(t, 20);
                        if (!string.IsNullOrEmpty(n)) result += n + "\n";
                    }
                }
                return result.TrimEnd('\n');
            }
        }

        public bool DisplayNeighbourhood { get; set; }

        #endregion
        #region ctor

        /// <summary>
        /// CAUTION: using this constructor instantiates a Document without checksum!
        /// </summary>
        /// <param name="file"></param>
        /// <param name="pathType"></param>
        /// <param name="documentType"></param>
        public Document(FileInfo file, PathType pathType = PathType.Unknown, DocumentType documentType = DocumentType.Unknown) : this()
        {
            this.file = file;
            this.Path = file.FullName;
            this.DocumentType = documentType;
            this.PathType = pathType;
            this.TokenCount = -1;
            this.ModificationDate = File.LastWriteTime;
        }

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <param name="checksum"></param>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        /// <param name="documentType"></param>
        /// <param name="tokenCount"></param>
        public Document(int checksum, 
            string path, 
            PathType pathType = PathType.Unknown, 
            DocumentType documentType = DocumentType.Unknown, 
            int tokenCount = -1) 
            : this(checksum, path, tokenCount)
        {
            this.DocumentType = documentType;
            this.PathType = pathType;
        }

        #endregion
        #region impl

        public void AddMetaData(string propertyName, string value)
        {
            MetaData m = new MetaData(this, propertyName, "unknown", value);
            this.MetaData.Add(m);
            DataSource.SubmitChanges();
        }

        public void AddMetaData(IVariableContainer<string, string> value, bool doSubmit, IndexDatabase db)
        {
            if (db == null) db = DataSource;
            MetaData m = new MetaData(this, value.Name, value.DataType, value.Value);
            this.MetaData.Add(m);
            if (doSubmit) db.SubmitChanges();
        }

        public string GetMetaData(string propertyName)
        {
            IEnumerable<MetaData> md = MetaData.Where(m => m.FieldName == propertyName);
            if (md.Any()) return md.First().Data;
            return null;
        }

        /// <summary>
        /// Deletes the document and all related data from the database in this.DataSource
        /// </summary>
        public void Delete(bool doSubmitChanges = true)
        {
            // retrieve token occurrences
            var qTokenOccurrences = (from row in DataSource.TokenOccurrenceTable 
                                     where row.DocumentID == ID 
                                     select row);

            // process token occurrences
            if (qTokenOccurrences.Any())
            {
                foreach (TokenOccurrence to in qTokenOccurrences)
                {
                    var qToken = (from row in DataSource.TokenTable 
                                   where row.ID == to.TokenID
                                   select row);

                    if(qToken.Any())
                    {
                        Token t = qToken.First();
                        if (t.NumberOfDocuments == 1) DataSource.TokenTable.DeleteOnSubmit(t);
                        else t.ClearStatistics();
                    }

                    DataSource.TokenOccurrenceTable.DeleteOnSubmit(to);
                }
            }

            // retrieve and process result evaluations
            var qResultEvaluations = (from row in DataSource.ResultEvaluationTable
                                    where row.DocumentID == ID
                                    select row);

            if (qResultEvaluations.Any())
                DataSource.ResultEvaluationTable.DeleteAllOnSubmit(qResultEvaluations.ToList());

            // delete the document itself
            DataSource.DocumentTable.DeleteOnSubmit(this);

            if (doSubmitChanges) DataSource.SubmitChanges();
        }

        /// <summary>
        /// Resets the document and all related data from the database in this.DataSource
        /// CAUTION: the Checksum will not be reset.
        /// </summary>
        public void Reset(IndexDatabase db = null)
        {
            if (db == null) db = DataSource;

            // retrieve token occurrences
            var qTokenOccurrences = (from row in db.TokenOccurrenceTable
                                     where row.DocumentID == ID
                                     select row);

            // process token occurrences
            if (qTokenOccurrences.Any())
            {
                foreach (TokenOccurrence to in qTokenOccurrences)
                {
                    var qToken = (from row in db.TokenTable
                                  where row.ID == to.TokenID
                                  select row);

                    if (qToken.Any())
                    {
                        Token t = qToken.First();
                        if (t.NumberOfDocuments == 1) db.TokenTable.DeleteOnSubmit(t);
                        else t.ClearStatistics();
                    }

                    db.TokenOccurrenceTable.DeleteOnSubmit(to);
                }
            }

            db.MetaDataTable.DeleteAllOnSubmit(db.MetaDataTable.Where(m => m.DocumentID == ID));
            metaDataBlock = null;

            db.SubmitChanges();
        }

        public bool Contains(Token token)
        {
            return TokenOccurrence.Exists(this, token);
        }

        /// <summary>
        /// Determine if this document has any of the given tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool ContainsAny(IEnumerable<Token> tokens)
        {
            foreach (Token t in tokens)
            {
                if (TokenOccurrence.Exists(this, t)) return true;
            }

            return false;
        }

        public bool ContainsAll(IEnumerable<Token> tokens)
        {
            foreach (Token t in tokens)
            {
                if (!TokenOccurrence.Exists(this, t)) return false;
            }

            return true;
        }

        public bool ContainsOneOfEach(IEnumerable<IEnumerable<Token>> tokens)
        {
            bool found;

            foreach (IEnumerable<Token> tokenGroup in tokens)
            {
                found = false;
                foreach (Token token in tokenGroup)
                {
                    if (TokenOccurrence.Exists(this, token))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        public bool HasExactHit(string word)
        {
            IQueryable<Token> q = DataSource.FindFullTokenIgnoreCase(word);
            if (q.Any())
            {
                foreach (Token t in q)
                {
                    if (this.Contains(t)) return true;
                }
            }
            return false;
        }

        public bool HasApproximateHit(string word)
        {
            IQueryable<Token> q = DataSource.FindPartTokenIgnoreCase(word);
            if (q.Any())
            {
                foreach (Token t in q)
                {
                    if (this.Contains(t)) return true;
                }
            }
            return false;
        }

        public int CountExactHits(IEnumerable<string> words)
        {
            int result = 0;
            foreach (string s in words)
            {
                if (this.HasExactHit(s)) result++;
            }
            return result;
        }

        public int CountApproximateHits(IEnumerable<string> words)
        {
            int result = 0;
            foreach (string s in words)
            {
                if (this.HasApproximateHit(s)) result++;
            }
            return result;
        }

        public double CalculateRank(SearchQuery query)
        {
            return DataSource.RankingFunction.Invoke(this, query);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="rating">a number from 0 to 100</param>
        /// <param name="doSubmit"></param>
        public void Rate(SearchQuery query, float rating, bool doSubmit = false)
        {
            this.RatingCount += 1;
            this.RatingSum += rating;
            DataSource.ResultEvaluationTable.InsertOnSubmit(new ResultEvaluation(this, query.ToString(), rating));
            if (doSubmit) DataSource.SubmitChanges();
        }

        public void LoadContents()
        {
            var q = from row in DataSource.TokenOccurrenceTable
                    where row.DocumentID == ID
                    select row;

            foreach (TokenOccurrence to in q)
            {
                foreach (int i in to.Positions)
                {
                    contents[i] = to.Token;
                }
            }
        }

        /// <summary>
        /// returns a list of predecessors, then the token itself and then the successors.
        /// If the token is not contained in the document this returns an empty enumeration.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="maxNeighboursBefore"></param>
        /// <param name="maxNeighboursAfter"></param>
        /// <returns>a list of predecessors, then the token itself and then the successors.</returns>
        public IEnumerable<List<Token>> GetNeighbourhoodBlocks(Token token, int maxNumberOfResults = 3, int maxNeighboursBefore = 10, int maxNeighboursAfter = 10)
        {
            Token current = token;
            if (!Contains(token)) yield break;

            int pre;
            int post;
            int preCount;
            int postCount;
            int count = 0;
            List<Token> currentBlock;

            foreach (int pos in token.PositionsIn(this))
            {
                if (count >= maxNumberOfResults) break; // returned enough results

                if (contents.ContainsKey(pos - 1) || contents.ContainsKey(pos + 1))
                {
                    // the token has predecessor or successor
                    count++;
                    currentBlock = new List<Token>();
                    preCount = 0;
                    postCount = 0;
                    pre = pos;
                    post = pos;

                    while (contents.ContainsKey(pre - 1) && preCount < maxNeighboursBefore)
                    {
                        pre -= 1;
                        currentBlock.Insert(0, contents[pre]);
                        preCount++;
                    }

                    currentBlock.Add(token);

                    while (contents.ContainsKey(post + 1) && postCount < maxNeighboursAfter)
                    {
                        post += 1;
                        currentBlock.Insert(0, contents[post]);
                        postCount++;
                    }

                    yield return currentBlock;
                }
            }
        }

        public string GetFirstNeighbourhoodBlock(Token token, int maxSize) 
        {
            string result = "";
            var n = GetNeighbourhoodBlocks(token, 1, maxSize / 2, maxSize / 2);
            if (n.Any())
            {
                List<Token> block = n.First();
                foreach (Token t in block) result += t.TokenData + " ";
            }
            return result.Trim();
        }

        #endregion
    }
}
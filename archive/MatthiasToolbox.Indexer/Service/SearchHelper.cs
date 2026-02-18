using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Indexer.Database;
using System.Threading;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Indexer.Service
{
    public class SearchHelper
    {
        #region prop

        /// <summary>
        /// Default is space and comma.
        /// </summary>
        public static List<char> QuerySplitChars { get; set; }

        /// <summary>
        /// Index database for searching
        /// </summary>
        public IndexDatabase DataSource { get; set; }

        /// <summary>
        /// Number of results to skip 
        /// (for Search, all other Search functions like SearchQ or SearchNOT will ignore this)
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Number of results to return (0 for all)
        /// (for Search, all other Search functions like SearchQ or SearchNOT will ignore this)
        /// </summary>
        public int Take { get; set; }

        #endregion
        #region ctor

        static SearchHelper()
        {
            QuerySplitChars = new List<char>();

            foreach (char c in SystemTools.NonLetterList)
            {
                if (!"1234567890".Contains(c)) QuerySplitChars.Add(c);
            }
        }

        /// <summary>
        /// using the default querySplitChars (all non letter non number characters)
        /// </summary>
        /// <param name="dataSource"></param>
        public SearchHelper(IndexDatabase dataSource)
        {
            this.DataSource = dataSource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="querySplitChars">default: all characters which are neither letters nor numbers</param>
        public SearchHelper(IndexDatabase dataSource, char[] querySplitChars) 
            : this(dataSource)
        {
            if (querySplitChars != null) QuerySplitChars = querySplitChars.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="querySplitChars">default: all characters which are neither letters nor numbers</param>
        public SearchHelper(IndexDatabase dataSource, List<char> querySplitChars)
            : this(dataSource)
        {
            if (querySplitChars != null) QuerySplitChars = querySplitChars;
        }

        #endregion
        #region impl

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// </summary>
        /// <param name="mustHaveExact"></param>
        /// <param name="mustHave"></param>
        /// <param name="mustNotHave"></param>
        /// <param name="canHave"></param>
        /// <returns></returns>
        public IQueryable<Document> SearchQ(string mustHaveExact, string mustHave, string mustNotHave, string canHave, bool updateStatistics = false)
        {
            return SearchQ(
                new SearchQuery(
                    PrepareQuery(mustHaveExact),
                    PrepareQuery(mustHave),
                    PrepareQuery(canHave),
                    PrepareQuery(mustNotHave)
                )
                , updateStatistics);
        }

        public IQueryable<Document> SearchQ(SearchQuery query, bool updateStatistics = false)
        {
            IQueryable<Document> results = DataSource.DocumentTable;

            // search exact terms (usually in "")
            if (query.PrepareExactTokens(DataSource))
                results = DataSource.FindAllExact(query.ExactTokens, results);

            // remove not-terms from results
            if (query.PrepareForbiddenTokens(DataSource))
                results = DataSource.FindNot(query.ForbiddenTokens, results);

            // search and-terms
            if (query.PrepareRequiredTokens(DataSource, true, false))
                results = DataSource.FindOneOfEach(query.RequiredTokenGroups, results);

            // search optional terms
            if (query.PrepareOptionalTokens(DataSource))
                results = DataSource.FindAny(query.OptionalTokens, results);

            // update statistics
            // if (updateStatistics) UpdateStatisticsAsync(query);
            if (updateStatistics) DataSource.UpdateStatistics(query, true);

            return results;
        }

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// use Skip and Take properties to extract partial results
        /// </summary>
        /// <param name="mustHaveExact"></param>
        /// <param name="mustHave"></param>
        /// <param name="mustNotHave"></param>
        /// <param name="canHave"></param>
        /// <returns></returns>
        public IEnumerable<Document> Search(string mustHaveExact, string mustHave, string mustNotHave, string canHave, bool updateStatistics = false)
        {
            return Search(
                new SearchQuery(
                    PrepareQuery(mustHaveExact),
                    PrepareQuery(mustHave),
                    PrepareQuery(canHave),
                    PrepareQuery(mustNotHave)
                )
                , updateStatistics);
        }

        public IEnumerable<Document> Search(SearchQuery query, bool updateStatistics = false)
        {
            IQueryable<Document> results = DataSource.DocumentTable;

            // search exact terms (usually in "")
            if (query.PrepareExactTokens(DataSource)) 
                results = DataSource.FindAllExact(query.ExactTokens, results);

            // remove not-terms from results
            if (query.PrepareForbiddenTokens(DataSource)) 
                results = DataSource.FindNot(query.ForbiddenTokens, results);
            
            // search and-terms
            if (query.PrepareRequiredTokens(DataSource, true, false)) 
                results = DataSource.FindOneOfEach(query.RequiredTokenGroups, results);
            
            // search optional terms
            if (query.PrepareOptionalTokens(DataSource)) 
                results = DataSource.FindAny(query.OptionalTokens, results);

            // update statistics
            // if (updateStatistics) UpdateStatisticsAsync(query);
            if (updateStatistics) DataSource.UpdateStatistics(query, true);

            if (Take != 0 && Skip != 0) return results.Skip(Skip).Take(Take);
            else if (Take != 0) return results.Take(Take);
            else if (Skip != null) return results.Skip(Skip);
            else return results;
        }

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IQueryable<Document> SearchNOT(string query)
        {
            return DataSource.FindNot(SearchQuery.GetTokens(DataSource, PrepareQuery(query), true));
        }

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IQueryable<Document> SearchAND(string query, bool updateSearchCount = true) 
        {
            List<string> queryParts = PrepareQuery(query);
            if (updateSearchCount) DataSource.IncrementSearchCount(SearchQuery.GetTokens(DataSource, queryParts, true));
            return DataSource.FindOneOfEach(SearchQuery.GetTokenGroups(DataSource, queryParts));
        }

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IQueryable<Document> SearchExact(string query, bool updateSearchCount = true)
        {
            List<Token> tokens = SearchQuery.GetTokens(DataSource, PrepareQuery(query), true);
            if (updateSearchCount) DataSource.IncrementSearchCount(tokens);
            return DataSource.FindAllExact(tokens);
        }

        /// <summary>
        /// use QuerySplitChars to determine how the search strings will be cut
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IQueryable<Document> SearchOR(string query, bool updateSearchCount = true)
        {
            List<string> queryParts = PrepareQuery(query);

            if (updateSearchCount) DataSource.IncrementSearchCount(SearchQuery.GetTokens(DataSource, queryParts));

            return DataSource.FindAny(SearchQuery.GetTokens(DataSource, queryParts));
        }

        /// <summary>
        /// Calls DataSource.UpdateStatistics(query, true); in a new thread.
        /// </summary>
        /// <param name="query"></param>
        public void UpdateStatisticsAsync(SearchQuery query)
        {
            new Thread(
                    new ThreadStart(
                        delegate
                        {
                            try
                            {
                                DataSource.UpdateStatistics(query, true);
                            }
                            catch { }
                        }
                    )
                ).Start();
        }

        #endregion
        #region util

        /// <summary>
        /// uses QuerySplitChars and removes empty entries
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        internal static List<string> PrepareQuery(string query)
        {
            string[] queryParts = query.Split(QuerySplitChars.ToArray(), StringSplitOptions.RemoveEmptyEntries); 
            return queryParts.ToList();
        }

        #endregion
    }
}
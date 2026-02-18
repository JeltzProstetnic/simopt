using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Indexer.Database;

namespace MatthiasToolbox.Indexer.Service
{
    public class SearchQuery
    {
        #region over

        /// <summary>
        /// Provides a string of the following format:
        /// "exactTerm1,exactTerm2,...,exactTerm3|requiredTerm1...n|optionalTerm1..n|forbiddenTerm1..n"
        /// If no items are available for one of the four groups there will not be anything between the two | signs.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string result = "";
            if (ExactTerms.Count > 0) result = ExactTerms.ToSeparatedString();
            result += "|";
            if (RequiredTerms.Count > 0) result += RequiredTerms.ToSeparatedString();
            result += "|";
            if (OptionalTerms.Count > 0) result += OptionalTerms.ToSeparatedString();
            result += "|";
            if (ForbiddenTerms.Count > 0) result += ForbiddenTerms.ToSeparatedString();
            return result;
        }

        #endregion
        #region prop

        public bool HasExactTerms { get { return (ExactTerms != null && ExactTerms.Count > 0); } }
        public bool HasRequiredTerms { get { return (RequiredTerms != null && RequiredTerms.Count > 0); } }
        public bool HasOptionalTerms { get { return (OptionalTerms != null && OptionalTerms.Count > 0); } }
        public bool HasForbiddenTerms { get { return (ForbiddenTerms != null && ForbiddenTerms.Count > 0); } }

        public List<string> ExactTerms { get; set; }
        public List<string> RequiredTerms { get; set; }
        public List<string> OptionalTerms { get; set; }
        public List<string> ForbiddenTerms { get; set; }

        public bool HasExactTokens { get { return (ExactTokens != null && ExactTokens.Count > 0); } }
        public bool HasRequiredTokens { get { return (RequiredTokens != null && RequiredTokens.Count > 0); } }
        public bool HasOptionalTokens { get { return (OptionalTokens != null && OptionalTokens.Count > 0); } }
        public bool HasForbiddenTokens { get { return (ForbiddenTokens != null && ForbiddenTokens.Count > 0); } }
        public bool HasRequiredTokenGroups { get { return (RequiredTokenGroups != null && RequiredTokenGroups.Count > 0); } }

        public List<Token> ExactTokens { get; set; }
        public List<Token> RequiredTokens { get; set; }
        public List<Token> OptionalTokens { get; set; }
        public List<Token> ForbiddenTokens { get; set; }
        public List<List<Token>> RequiredTokenGroups { get; set; }

        /// <summary>
        /// NOT-Tokens are not included!
        /// </summary>
        public IEnumerable<Token> AllSearchedTokens
        {
            get
            {
                // exact tokens
                if(ExactTokens != null ) 
                    foreach (Token t in ExactTokens) 
                        yield return t;

                // AND tokens
                if (RequiredTokenGroups.Count == 0)
                {
                    if (RequiredTokens != null)
                        foreach (Token t in RequiredTokens) 
                            yield return t;
                }
                else
                {
                    foreach (List<Token> l in RequiredTokenGroups)
                    {
                        foreach (Token t in l) 
                            yield return t;
                    }
                }

                // OR tokens
                if(OptionalTokens != null)
                    foreach (Token t in OptionalTokens) 
                        yield return t;
            }
        }

        #endregion
        #region ctor

        public SearchQuery()
        {
            ExactTerms = new List<string>();
            RequiredTerms = new List<string>();
            OptionalTerms = new List<string>();
            ForbiddenTerms = new List<string>();
            RequiredTokenGroups = new List<List<Token>>();
        }

        /// <summary>
        /// Requires the following format:
        /// "exactTerm1,exactTerm2,...,exactTerm3|requiredTerm1...n|optionalTerm1..n|forbiddenTerm1..n"
        /// If no items are available for one of the four groups there must not be anything between the two | signs.
        /// </summary>
        /// <param name="formattedQuery"></param>
        public SearchQuery(string formattedQuery) : this()
        {
            string[] parts = formattedQuery.Split('|');
            if (!String.IsNullOrEmpty(parts[0])) ExactTerms.AddRange(parts[0].Split(','));
            if (!String.IsNullOrEmpty(parts[1])) RequiredTerms.AddRange(parts[1].Split(','));
            if (!String.IsNullOrEmpty(parts[2])) OptionalTerms.AddRange(parts[2].Split(','));
            if (!String.IsNullOrEmpty(parts[3])) ForbiddenTerms.AddRange(parts[3].Split(','));
        }

        public SearchQuery(string mustHaveExact, string mustHave, string mustNotHave, string canHave)
        {
            this.ExactTerms = SearchHelper.PrepareQuery(mustHaveExact);
            this.RequiredTerms = SearchHelper.PrepareQuery(mustHave);
            this.OptionalTerms = SearchHelper.PrepareQuery(canHave);
            this.ForbiddenTerms = SearchHelper.PrepareQuery(mustNotHave);
        }

        public SearchQuery(List<string> exactTerms, 
            List<string> requiredTerms, 
            List<string> optionalTerms, 
            List<string> forbiddenTerms)
        {
            this.ExactTerms = exactTerms;
            this.RequiredTerms = requiredTerms;
            this.OptionalTerms = optionalTerms;
            this.ForbiddenTerms = forbiddenTerms;
        }

        #endregion
        #region impl

        /// <summary>
        /// Fills the OptionalTokens list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="exactTokensOnly"></param>
        /// <returns>true if optional tokens were found.</returns>
        public bool PrepareOptionalTokens(IndexDatabase index, bool exactTokensOnly = false)
        {
            if (!HasOptionalTerms) return false;
            OptionalTokens = PrepareTokens(OptionalTerms, index, exactTokensOnly);
            return OptionalTokens.Count > 0;
        }

        /// <summary>
        /// Fills the ForbiddenTokens list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="exactTokensOnly"></param>
        /// <returns>true if forbidden tokens were found.</returns>
        public bool PrepareForbiddenTokens(IndexDatabase index, bool exactTokensOnly = true)
        {
            if (!HasForbiddenTerms) return false;
            ForbiddenTokens = PrepareTokens(ForbiddenTerms, index, exactTokensOnly);
            return ForbiddenTokens.Count > 0;
        }

        /// <summary>
        /// Fills the ExactTokens list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="exactTokensOnly"></param>
        /// <returns>true if exact tokens were found.</returns>
        public bool PrepareExactTokens(IndexDatabase index, bool exactTokensOnly = true)
        {
            if (!HasExactTerms) return false;
            ExactTokens = PrepareTokens(ExactTerms, index, exactTokensOnly);
            return ExactTokens.Count > 0;
        }

        /// <summary>
        /// Fills the RequiredTokens list and the RequiredTokenGroups list depending on createTokens and createTokenGroups.
        /// If both flags are set to false the method will do nothing and return false.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="createTokenGroups"></param>
        /// <param name="createTokens"></param>
        /// <returns>true if tokens were found</returns>
        public bool PrepareRequiredTokens(IndexDatabase index, bool createTokenGroups = false, bool createTokens = true)
        {
            if (!HasRequiredTerms || (!createTokenGroups && !createTokens)) return false;
            if(createTokens) RequiredTokens = PrepareTokens(RequiredTerms, index, true);
            if(createTokenGroups) RequiredTokenGroups = PrepareTokenGroups(RequiredTerms, index);
            return createTokenGroups ? RequiredTokenGroups.Count > 0 : RequiredTokens.Count > 0;
        }

        /// <summary>
        /// retrieve tokens from the data source
        /// </summary>
        /// <param name="index"></param>
        /// <param name="queryParts"></param>
        /// <param name="exactTokensOnly"></param>
        /// <returns></returns>
        public static List<Token> GetTokens(IndexDatabase index, List<string> queryParts, bool exactTokensOnly = false)
        {
            List<Token> tokens = new List<Token>();
            foreach (string q in queryParts)
            {
                if (exactTokensOnly) tokens.AddRange(index.FindFullTokenIgnoreCase(q));
                else tokens.AddRange(index.FindPartTokenIgnoreCase(q));
            }
            return tokens;
        }

        /// <summary>
        /// retrieve tokens for each query part
        /// </summary>
        /// <param name="index"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static List<List<Token>> GetTokenGroups(IndexDatabase index, List<string> queryParts)
        {
            List<List<Token>> result = new List<List<Token>>();
            foreach (string q in queryParts)
            {
                List<Token> t = index.FindPartTokenIgnoreCase(q).ToList();
                if (t.Count > 0) result.Add(t);
            }
            return result;
        }

        private List<Token> PrepareTokens(List<string> terms, IndexDatabase index, bool exactTokensOnly = true)
        {
            List<Token> results = new List<Token>();
            foreach (string q in terms)
            {
                if (exactTokensOnly) results.AddRange(index.FindFullTokenIgnoreCase(q));
                else results.AddRange(index.FindPartTokenIgnoreCase(q));
            }
            return results;
        }

        private List<List<Token>> PrepareTokenGroups(List<string> terms, IndexDatabase index)
        {
            List<List<Token>> result = new List<List<Token>>();
            foreach (string q in terms)
            {
                List<Token> t = Global.IndexDatabase.FindPartTokenIgnoreCase(q).ToList();
                if (t.Count > 0) result.Add(t);
            }
            return result;
        }

        #endregion
    }
}
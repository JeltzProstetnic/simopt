using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Mathematics;

namespace MatthiasToolbox.Indexer.Database
{
    public partial class ResultEvaluation
    {
        private List<Token> searchTokens;

        /// <summary>
        /// Set this to the IndexDatabase to which this instance relates.
        /// </summary>
        public static IndexDatabase DataSource { get; set; }

        public IEnumerable<string> SearchStrings
        {
            get
            {
                foreach (Token t in SearchTokens) yield return t.TokenData;
            }
        }

        public IEnumerable<Token> SearchTokens
        {
            get
            {
                if (searchTokens == null)
                {
                    searchTokens = new List<Token>();
                    foreach (string s in Search.Split('|'))
                    {
                        int id = int.Parse(s);
                        searchTokens.Add((from q in DataSource.TokenTable
                                          where q.ID == id
                                          select q).First());
                    }
                }
                return searchTokens.AsEnumerable<Token>();
            }
        }
    }
}
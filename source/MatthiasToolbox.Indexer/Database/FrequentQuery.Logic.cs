using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Indexer.Service;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.Indexer.Database
{
    public partial class FrequentQuery
    {
        #region cvar
        
        private SearchQuery query;

        #endregion
        #region prop

        public SearchQuery Query
        {
            get
            {
                if (query == null) query = new SearchQuery(SearchString);
                return query;
            }
            set
            {
                query = value;
                SearchString = value.ToString();
            }
        }

        #endregion
        #region ctor

        public FrequentQuery(SearchQuery value, int count = 1)
        {
            this.Query = value;
            this.SearchCount = 1;
        }

        #endregion
    }
}
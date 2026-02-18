using System;

namespace MatthiasToolbox.Geography
{
	/// <summary>
	/// contains Data for a Capital 
	/// </summary>
	public class CapitalData
    {
        #region over

        /// <summary>
        /// overriden: will return the name of the Capital
        /// </summary>
        public override string ToString()
        {
            return CapitalName;
        }

        #endregion
        #region prop

        /// <summary>
        /// the (Climate) StationID of the Capital
        /// </summary>
		public string StationID{
			get; set;
		}
		
        /// <summary>
        /// the name of the country to which the capital belongs
        /// </summary>
		public string CountryName{
			get; set;
		}
		
        /// <summary>
        /// the full name of the country to which the capital belongs
        /// </summary>
		public string FullCountryName{
			get; set;
		}
		
        /// <summary>
        /// the CountryCode of the country to which the capital belongs
        /// </summary>
		public string CountryCode{
			get; set;
		}
		
        /// <summary>
        /// the name of the Capital
        /// </summary>
		public string CapitalName{
			get; set;
		}
		
        /// <summary>
        /// the Citizenship of the country to which the capital belongs
        /// </summary>
		public string Citizenship{
			get; set;
		}
		
        /// <summary>
        /// the adjective of the Citizenship of the country to which the capital belongs
        /// </summary>
		public string Adjective{
			get; set;
		}
		
        /// <summary>
        /// the Currency of the country to which the capital belongs
        /// </summary>
		public string Currency{
			get; set;
		}
		
        /// <summary>
        ///  the CurrencyCode of the country to which the capital belongs
        /// </summary>
		public string CurrencyCode{
			get; set;
		}
		
        /// <summary>
        ///  the CurrencySubunit of the country to which the capital belongs
        /// </summary>
		public string CurrencySubunit{
			get; set;
		}

        #endregion
        #region ctor

        /// <summary>
        /// constructor for CapitalData class requiring 
        /// the name of the Capital's country, 
        /// the full name of the Capital's country,
        /// the countrycode of the Capital's country, 
        /// the name of the Capital,
        /// the citizenship of the Capital's country,
        /// the adjective of the citizenship of the Capital's country,
        /// the currency of the Capital's country,
        /// the currencyCode of the Capital's country and
        /// the currencySubunit of the Capital's country
        /// </summary>
        public CapitalData(string countryName, string fullCountryName, string countryCode, string capitalName,
		               string citizenship, string adjective, string currency, string currencyCode, string currencySubunit)
        {
			this.CountryName = countryName;
			this.FullCountryName = fullCountryName;
			this.CountryCode = countryCode;
			this.CapitalName = capitalName;
			this.Citizenship = citizenship;
			this.Adjective = adjective;
			this.Currency = currency;
			this.CurrencyCode = currencyCode;
			this.CurrencySubunit = currencySubunit;

        }

        #endregion
    }
}

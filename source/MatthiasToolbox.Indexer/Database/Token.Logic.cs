using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Semantics;
using System.Text.RegularExpressions;

namespace MatthiasToolbox.Indexer.Database
{
    /// <summary>
    /// TODO: throw exceptions / log if datasource was not set and datasource dependend properties are queried.
    /// </summary>
    public partial class Token
    {
        #region cvar

        private int? totalCount;
        private int? numberOfDocuments;
        private double? inverseDocumentFrequency;
        
        private double? garbageProbability;
        private double? umlautDistributionLikelihood;
        private double? casingDistributionLikelihood;
        private double? phoneticDistributionLikelihood;

        #endregion
        #region prop

        /// <summary>
        /// Set this to the IndexDatabase to which this instance relates.
        /// </summary>
        public static IndexDatabase DataSource { get; set; }

        #region statistics

        /// <summary>
        /// Will be calculated and stored on demand. Use ClearStatistics 
        /// after a change in the database to update this value.
        /// </summary>
        public int TotalCount 
        { 
            get 
            {
                if (!totalCount.HasValue) 
                    totalCount = DataSource.TotalCount(this);
                
                return totalCount.Value;
            } 
        }

        /// <summary>
        /// Will be calculated and stored on demand. Use ClearStatistics 
        /// after a change in the database to update this value.
        /// Returns the number of documents in which this token occurs.
        /// </summary>
        public int NumberOfDocuments
        {
            get
            {
                if (!numberOfDocuments.HasValue)
                {
                    numberOfDocuments = DataSource.CountDocumentsContaining(this);
                }
                return numberOfDocuments.Value;
            }
        }

        /// <summary>
        /// Will be calculated and stored on demand. Use ClearStatistics 
        /// after a change in the database to update this value.
        /// Returns log(N/(occurences)) or zero if the token doesn't occur in any document.
        /// </summary>
        public double InverseDocumentFrequency
        {
            get
            {
                if (!inverseDocumentFrequency.HasValue)
                {
                    if (NumberOfDocuments == 0) inverseDocumentFrequency = 0;
                    else inverseDocumentFrequency = Math.Log((double)DataSource.DocumentCount / (double)NumberOfDocuments);
                }
                return inverseDocumentFrequency.Value;
            }
        }

        /// <summary>
        /// GarbageProbability divided by TotalCount
        /// </summary>
        public double RelativeGarbageProbability
        {
            get { if (totalCount == 0) return double.NaN; else return GarbageProbability / TotalCount; }
        }

        /// <summary>
        /// Get a value indicating the probability that this is garbage.
        /// </summary>
        public double GarbageProbability
        {
            get
            {
                if (!garbageProbability.HasValue) 
                    garbageProbability = TokenData.GarbageProbability();

                return garbageProbability.Value;
            }
        }

        /// <summary>
        /// If exactly one or all letters are uppercase this returns 1. Otherwise it returns 1 - the upper case fraction.
        /// </summary>
        public double CasingDistributionLikelihood
        {
            get
            {
                if (!casingDistributionLikelihood.HasValue)
                {
                    if (TokenData.UpperCount() == 1 || TokenData.UpperCount() == TokenData.Length)
                        casingDistributionLikelihood = 1;
                    else
                        casingDistributionLikelihood = 1 - TokenData.UpperCaseFraction();
                }

                return casingDistributionLikelihood.Value;
            }
        }

        public double PhoneticDistributionLikelihood
        {
            get
            {
                if (!phoneticDistributionLikelihood.HasValue)
                    phoneticDistributionLikelihood = TokenData.PhoneticDistributionLikelihood();

                return phoneticDistributionLikelihood.Value;
            }
        }

        /// <summary>
        /// Returns 1 - the umlaut fraction.
        /// </summary>
        public double UmlautDistributionLikelihood
        {
            get
            {
                if (!umlautDistributionLikelihood.HasValue)
                    umlautDistributionLikelihood = 1 - TokenData.UmlautFraction();

                return umlautDistributionLikelihood.Value;
            }
        }

        #endregion

        #endregion
        #region rset

        /// <summary>
        /// Causes documant related statistics (TotalCount, 
        /// NumberOfDocuments and InverseDocumentFrequency) 
        /// to be recalculated on demand.
        /// </summary>
        public void ClearStatistics()
        {
            totalCount = null;
            numberOfDocuments = null;
            inverseDocumentFrequency = null;
        }

        #endregion
        #region impl

        public List<int> PositionsIn(Document document)
        {
            TokenOccurrence to = (from row in DataSource.TokenOccurrenceTable
                                  where row.DocumentID == document.ID && row.TokenID == ID
                                  select row).First();
            return to.Positions;
        }

        #endregion
    }
}
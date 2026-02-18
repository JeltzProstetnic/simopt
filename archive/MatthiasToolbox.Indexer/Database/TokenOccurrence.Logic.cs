using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Mathematics;

namespace MatthiasToolbox.Indexer.Database
{
    /// <summary>
    /// TODO: throw exceptions / log if datasource was not set and datasource dependend properties are queried.
    /// </summary>
    public partial class TokenOccurrence
    {
        #region cvar

        private double? tfidf;
        private Token token;
        private List<int> positions;
        private Document document;

        #endregion
        #region prop

        /// <summary>
        /// Set this to the IndexDatabase to which this instance relates.
        /// </summary>
        public static IndexDatabase DataSource { get; set; }

        /// <summary>
        /// Will be cached after first retrieval
        /// </summary>
        public Token Token
        {
            get
            {
                if (token == null) 
                    token = DataSource.TokenTable.Where(t => t.ID == TokenID).First();

                return token;
            }
        }

        /// <summary>
        /// Will be cached after first retrieval
        /// </summary>
        public Document Document
        {
            get
            {
                if (document == null)
                    document = DataSource.DocumentTable.Where(d => d.ID == DocumentID).First();

                return document;
            }
        }

        /// <summary>
        /// Returns the first n occurence positions of the token as list.
        /// The value of n depends on the current database configuration 
        /// and will be cached after the first retrieval.
        /// </summary>
        public List<int> Positions 
        {
            get 
            {
                if (positions == null)
                {
                    positions = new List<int>();
                    string[] tmp = PositionList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in tmp)
                    {
                        positions.Add(int.Parse(s));
                    }
                }
                return positions;
            }
        }

        /// <summary>
        /// TF-IDF weight (term frequency * inverse document frequency)
        /// This may be cached. Use ClearStatistics to force a re-calculation.
        /// </summary>
        public double TFIDF
        {
            get
            {
                if (!tfidf.HasValue)
                {
                    if (token == null) 
                        token = DataSource.TokenTable.Where(t => t.ID == TokenID).First();
                    tfidf = Density * token.InverseDocumentFrequency;
                }
                return tfidf.Value;
            }
        }

        #endregion
        #region ctor

        /// <summary>
        /// CAUTION: this will calculate average and median position as well as steepness and density
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        /// <param name="positions"></param>
        /// <param name="calculateVariance"></param>
        public TokenOccurrence(
            Token token,
            Document document,
            List<int> positions,
            bool calculateVariance = false)
        {
            this.TokenID = token.ID;
            this.DocumentID = document.ID;
            this.Count = positions.Count;

            string pos = "";
            double sum1 = 0;
            foreach (int i in positions)
            {
                pos += i.ToString() + ";";
                sum1 += i;
            }
            if (pos.Length > 0) pos.TrimEnd(';');

            this.PositionList = pos;

            double positionAverageAbsolute = sum1 / (double)positions.Count;
            this.PositionAverage = positionAverageAbsolute / (double)document.TokenCount;
            this.PositionMedian = positions.Median() / (double)document.TokenCount;
            this.Steepness = PositionAverage - PositionMedian;
            this.Density = (double)positions.Count / (double)document.TokenCount;

            if (calculateVariance) // σ² = (1/(n-1))(Σ(xi-µ)²)
            {
                double f = 1d / (double)(positions.Count - 1);
                double sum2 = 0;
                foreach (int i in positions)
                {
                    sum2 += Math.Pow((double)i - positionAverageAbsolute, 2d);
                }
                this.PositionVariance = f * sum2;
            }
        }

        #endregion
        #region rset

        /// <summary>
        /// Clears the data for the TF-IDF so that the value will be re-calculated on demand.
        /// </summary>
        public void ClearStatistics()
        {
            tfidf = null;
            positions = null;
        }

        #endregion
        #region impl

        public static bool Exists(Document doc, Token token)
        {
            return (from row in DataSource.TokenOccurrenceTable
                    where row.DocumentID == doc.ID && row.TokenID == token.ID
                    select row).Any();
        }

        public void ProcessPositionList(List<int> positions, double documentTokenCount, bool calculateVariance = false)
        {
            this.Count = positions.Count;

            string pos = "";
            double sum1 = 0;
            foreach (int i in positions)
            {
                pos += i.ToString() + ";";
                sum1 += i;
            }
            if (pos.Length > 0) pos = pos.TrimEnd(';');

            this.PositionList = pos;

            double positionAverageAbsolute = sum1 / (double)positions.Count;
            this.PositionAverage = positionAverageAbsolute / documentTokenCount;
            this.PositionMedian = positions.Median() / documentTokenCount;
            this.Steepness = PositionAverage - PositionMedian;
            this.Density = (double)positions.Count / documentTokenCount;

            if (calculateVariance) // σ² = (1/(n-1))(Σ(xi-µ)²)
            {
                double f = 1d / (double)(positions.Count - 1);
                double sum2 = 0;
                foreach (int i in positions)
                {
                    sum2 += Math.Pow((double)i - positionAverageAbsolute, 2d);
                }
                this.PositionVariance = f * sum2;
            }
        }

        public void ProcessPositionList(double documentTokenCount, bool calculateVariance = false)
        {
            ProcessPositionList(Positions, documentTokenCount, calculateVariance);
        }

        public void AppendPosition(int position)
        {
            this.PositionList += ";" + position.ToString();
        }

        #endregion
    }
}
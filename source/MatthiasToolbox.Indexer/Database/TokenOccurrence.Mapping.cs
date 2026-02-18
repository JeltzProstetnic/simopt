using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Mathematics;

namespace MatthiasToolbox.Indexer.Database
{
    [Table(Name = "tblTokenOccurrences")]
    public partial class TokenOccurrence
    {
        #region ctor

        /// <summary>
        /// Empty constructor for deserialization (LINQ)
        /// </summary>
        public TokenOccurrence() { }

        /// <summary>
        /// Minimal constructor, no occurrences or other data (yet)
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        public TokenOccurrence(
            Token token,
            Document document)
        {
            this.TokenID = token.ID;
            this.token = token;
            this.DocumentID = document.ID;
        }

        /// <summary>
        /// Positions will be transformed into a ; sepatated string.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        /// <param name="positions"></param>
        /// <param name="avgPosition">Provide a normalized value (0..1).</param>
        /// <param name="medPosition">Provide a normalized value (0..1).</param>
        /// <param name="varPosition">Provide absolute value.</param>
        /// <param name="distributionSteepness">Provide a normalized value (-1..1).</param>
        public TokenOccurrence(
            Token token, 
            Document document, 
            List<int> positions, 
            double avgPosition, 
            double medPosition,
            double varPosition,
            double distributionSteepness)
        {
            this.TokenID = token.ID;
            this.token = token;
            this.DocumentID = document.ID;
            this.Count = positions.Count;
            this.PositionAverage = avgPosition;
            this.PositionMedian = medPosition;
            this.PositionVariance = varPosition;
            this.Steepness = distributionSteepness;
            this.Density = (double)positions.Count / (double)document.TokenCount;

            string pos = "";
            foreach (int i in positions) 
            {
                pos += i.ToString() + ";";
            }
            if (pos.Length > 0) pos.TrimEnd(';');

            this.PositionList = pos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="document"></param>
        /// <param name="count">The number of occurances of the given token in the given document.</param>
        /// <param name="positions">Provide a ; separated string.</param>
        /// <param name="avgPosition">Provide a normalized value (0..1).</param>
        /// <param name="medPosition">Provide a normalized value (0..1).</param>
        /// <param name="varPosition">Provide absolute value.</param>
        /// <param name="distributionSteepness">Provide a normalized value (-1..1).</param>
        public TokenOccurrence(
            Token token,
            Document document,
            int count,
            string positions,
            double avgPosition = 0.5,
            double medPosition = 0.5,
            double varPosition = 0,
            double distributionSteepness = 0)
        {
            this.TokenID = token.ID;
            this.token = token;
            this.DocumentID = document.ID;
            this.Count = count;
            this.PositionAverage = avgPosition;
            this.PositionMedian = medPosition;
            this.PositionVariance = varPosition;
            this.Steepness = distributionSteepness;
            this.PositionList = positions;
            this.Density = (double)count / (double)document.TokenCount;
        }

        #endregion
        #region data

        [Column(AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID { get; private set; }

        [Column]
        public int TokenID { get; set; }

        [Column]
        public int DocumentID { get; set; }

        /// <summary>
        /// Total number of occurence of the token in the document
        /// </summary>
        [Column]
        public int Count { get; set; }

        /// <summary>
        /// The first n occurence positions of the token as semicolon-separated string.
        /// The value of n depends on the current database configuration
        /// </summary>
        [Column]
        public string PositionList { get; set; }

        /// <summary>
        /// Average value µ of the term's distribution, 0..1
        /// </summary>
        [Column]
        public double PositionAverage { get; set; }

        /// <summary>
        /// Median value of the term's distribution, 0..1
        /// </summary>
        [Column]
        public double PositionMedian { get; set; }

        /// <summary>
        /// Variance σ² value of the term's distribution. (Absolute value)
        /// σ² = (1/(n-1))(Σ(xi-µ)²)
        /// </summary>
        [Column]
        public double PositionVariance { get; set; }

        /// <summary>
        /// The density of occurence, 0..1 (count / length)
        /// </summary>
        [Column]
        public double Density { get; set; }

        /// <summary>
        /// Steepness of the term's distribution, -1..1 (average - median)
        /// Negative value: term frequency tends to decrease towards the end.
        /// Positive value: term frequency tends to increase towards the end.
        /// </summary>
        [Column]
        public double Steepness { get; set; }

        #endregion
    }
}

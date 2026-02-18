using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Algorithms
{
    public class LevenshteinDistance
    {
        /// <summary>
        /// Compare two words.
        /// Returns 0 if both strings are null or empty.
        /// Memory: O(m*n)
        /// Processor: O(m*n)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Get(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 0;
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int t;
            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i; // del
            for (int j = 0; j <= b.Length; j++) d[0, j] = j; // ins

            for (int j = 1; j <= b.Length; j++)
            {
                for (int i = 1; i <= a.Length; i++)
                {
                    if (a[i - 1] == b[j - 1]) d[i, j] = d[i - 1, j - 1];
                    else
                    {
                        t = Math.Min(d[i - 1, j], d[i, j - 1]);
                        d[i, j] = Math.Min(t, d[i - 1, j - 1]) + 1;
                    }
                }
            }

            return d[a.Length, b.Length];
        }

        /// <summary>
        /// Compare two lists of items (like a sentence).
        /// Returns 0 if both strings are null or empty.
        /// Memory: O(m*n)
        /// Processor: O(m*n)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Get<T>(List<T> a, List<T> b) where T : IEquatable<T>
        {
            if (a.Count == 0 && b.Count == 0) return 0;
            if (a.Count == 0) return b.Count;
            if (b.Count == 0) return a.Count;

            int t;
            int[,] d = new int[a.Count + 1, b.Count + 1];

            for (int i = 0; i <= a.Count; i++) d[i, 0] = i; // del
            for (int j = 0; j <= b.Count; j++) d[0, j] = j; // ins

            for (int j = 1; j <= b.Count; j++)
            {
                for (int i = 1; i <= a.Count; i++)
                {
                    if (a[i - 1].Equals(b[j - 1])) d[i, j] = d[i - 1, j - 1];
                    else
                    {
                        t = Math.Min(d[i - 1, j], d[i, j - 1]);
                        d[i, j] = Math.Min(t, d[i - 1, j - 1]) + 1;
                    }
                }
            }

            return d[a.Count, b.Count];
        }
    }
}
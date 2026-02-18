using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Semantics.Metamodel;
using System.Reflection;
using System.Data.Linq;
using MatthiasToolbox.Delta.Diff;

namespace MatthiasToolbox.Semantics
{
    public static class Extensions
    {
        #region cvar

        private static Regex multipleUmlauts = new Regex("[ä-üÄ-Ü][ä-üÄ-Ü]");
        private static Regex qWithoutU = new Regex("q[^u]|Q[^uU]");
        private static Regex umlautsAndS = new Regex("[ä-üÄ-Üß]");
        private static Regex upperCase = new Regex("[A-ZÄ-Ü]");
        private static Regex lowerCase = new Regex("[a-zä-ü]");
        private static Regex capitalizedWord = new Regex("^[A-ZÄ-Ü][a-zä-ü]*$");
        private static Regex invalidStart = new Regex("^[ßyY]");
        private static Regex invalidEnd = new Regex("[qcyQCY]$");
        private static Regex vowels = new Regex("[aeiouAEIOU]");
        private static Regex vowelsAndY = new Regex("[aeiouyAEIOUY]");
        private static Regex nonVowels = new Regex("[^aeiouAEIOU]");
        private static Regex nonVowelsNorY = new Regex("[^aeiouyAEIOUY]");
        private static Regex schOnly = new Regex("(sch)");
        private static Regex chOnly = new Regex("[^s](ch)");
        private static Regex ckOnly = new Regex("(ck)");
        private static Regex vowelsAndUmlauts = new Regex("[aeiouäöüAEIOUÄÖÜ]");
        private static Regex vowelsAndYAndUmlauts = new Regex("[aeiouäöüyAEIOUÄÖÜY]");
        private static Regex nonVowelsNorUmlauts = new Regex("[^aeiouäöüAEIOUÄÖÜ]");
        private static Regex nonVowelsNorYNorUmlauts = new Regex("[^aeiouäöüyAEIOUÄÖÜY]");

        private static Regex invalidLetterCombinations = new Regex("xx|yy|ii|jj|cc|sß|lß|mxh");
        private static Regex improbableLetterCombinations = new Regex("zz|hh|uu|vv");
        private static Regex probableSyllablesGerman = new Regex("^Bau|bau|ertrag|akuum|[Bb]uch|[Gg]roß|[Mm]aß");
        
        #endregion
        #region impl

        #region string

        /// <summary>
        /// Calculate the Edit Script distance to the given string. (use for multiple paragraphs)
        /// the result can take values from 0 (equal) to min(paragraphcount)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="other"></param>
        /// <param name="ignoreSpace">flag if subsequent whitespaces are to be treated as only one space</param>
        /// <returns></returns>
        public static int DistanceTo(this string a, string other, bool ignoreSpace)
        {
            MyersDiff m = new MyersDiff();
            return m.GetDistance(a, other, ignoreSpace);
        }

        #region check

        /// <summary>
        /// Returns true only if all letters are upper case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsUpperCase(this string value)
        {
            return !lowerCase.IsMatch(value);
        }

        /// <summary>
        /// Returns true only if all letters are lower case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsLowerCase(this string value)
        {
            return !upperCase.IsMatch(value);
        }

        /// <summary>
        /// Returns true only if the word starts with a capital letter and contains no further capitals.
        /// Test => true
        /// tEst => false
        /// test => false
        /// TEst => false
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsCapitalized(this string value)
        {
            return capitalizedWord.IsMatch(value);
        }

        #endregion
        #region count

        /// <summary>
        /// Count how many occurrences of the given strings are found in this instance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int Count(this string value, params char[] data)
        {
            int result = 0;
            for (int i = 0; i < value.Length; i++)
            {
                foreach (char test in data)
                {
                    if (value.Substring(i, 1) == test.ToString()) result += 1;
                }
            }
            return result;
        }

        /// <summary>
        /// Counts number of upper case letters.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int UpperCount(this string value)
        {
            return upperCase.Matches(value).Count;
        }

        /// <summary>
        /// Counts umlaute and ß.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int UmlautCount(this string value)
        {
            return umlautsAndS.Matches(value).Count;
        }

        /// <summary>
        /// Returns number of vowels (a, e, i, o, u, ä, ö, ü) in the string. Y is not counted.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int VowelCountWithoutY(this string value)
        {
            return vowelsAndUmlauts.Matches(value).Count;
        }

        /// <summary>
        /// Returns number of vowels (a, e, i, o, u, ä, ö, ü) and y.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int VowelCountWithY(this string value)
        {
            return vowelsAndYAndUmlauts.Matches(value).Count;
        }

        /// <summary>
        /// Returns number of consonants except for y.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ConsonantCountWithoutY(this string value)
        {
            return nonVowelsNorYNorUmlauts.Matches(value).Count;
        }

        /// <summary>
        /// Returns number of consonants including y.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ConsonantCountWithY(this string value)
        {
            return nonVowelsNorUmlauts.Matches(value).Count;
        }

        /// <summary>
        /// Number of "ck", "ch" and "sch" occurrences.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int SchChCkCount(this string value)
        {
            return schOnly.Matches(value).Count + chOnly.Matches(value).Count + ckOnly.Matches(value).Count;
        }

        #endregion
        #region fractions

        /// <summary>
        /// ck, sch and ch are counted as only one letter
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double UmlautFraction(this string value)
        {
            return (double)value.UmlautCount() / (double)(value.Length - value.SchChCkCount());
        }

        /// <summary>
        /// Number of uppercase letters divided by the length of the string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double UpperCaseFraction(this string value)
        {
            return (double)value.UpperCount() / (double)value.Length;
        }

        /// <summary>
        /// ck, sch and ch are counted as only one letter
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double VowelFractionWithY(this string value)
        {
            return (double)value.VowelCountWithY() / (double)(value.Length - value.SchChCkCount());
        }

        #endregion
        #region probabilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="umlautFactorWeight"></param>
        /// <param name="casingFactorWeight"></param>
        /// <param name="phoneticFactorWeight"></param>
        /// <returns></returns>
        public static double GarbageProbability(
            this string value, 
            double umlautFactorWeight = 2.7d, 
            double casingFactorWeight = 2.5d,
            double phoneticFactorWeight = 1.1d,
            int ignoredUpperCaseLetters = 1,
            int toleratedLowerCaseInUpper = 0)
        {
            double ucFactor = 0;

            if (invalidStart.IsMatch(value) ||
                invalidEnd.IsMatch(value) ||
                multipleUmlauts.IsMatch(value) ||
                qWithoutU.IsMatch(value) ||
                value.VowelCountWithoutY() == 0 ||
                value.ConsonantCountWithoutY() == 0 ||
                invalidLetterCombinations.IsMatch(value)) return 1d;

            int ucCount = value.UpperCount();
            if (ucCount > ignoredUpperCaseLetters && ucCount < (value.Length - toleratedLowerCaseInUpper))
            {
                ucFactor = value.UpperCaseFraction();
            }

            double result = (umlautFactorWeight * value.UmlautFraction()) + 
                (casingFactorWeight * ucFactor) + 
                (phoneticFactorWeight * value.PhoneticDistributionImprobability());

            if (probableSyllablesGerman.IsMatch(value)) result -= 0.4;
            else if (improbableLetterCombinations.IsMatch(value)) result += 0.4;

            if (result > 1d) return 1; else if (result < 0d) return 0; else return result;
        }

        public static double GarbageProbabilityROC(
            this string value,
            double umlautFactorWeight = 0.43d,
            double casingFactorWeight = 0.4d,
            double phoneticFactorWeight = 0.17d,
            int ignoredUpperCaseLetters = 1,
            int toleratedLowerCaseInUpper = 0)
        {
            double ucFactor = 0;
            double n = 1 / (casingFactorWeight + umlautFactorWeight + phoneticFactorWeight);
            double casingWeight = casingFactorWeight * n;
            double umlautWeight = umlautFactorWeight * n;
            double phoneticWeight = phoneticFactorWeight * n;

            int ucCount = value.UpperCount();
            if (ucCount > ignoredUpperCaseLetters && ucCount < (value.Length - toleratedLowerCaseInUpper))
            {
                ucFactor = value.UpperCaseFraction();
            }

            double result = (umlautWeight * value.UmlautFraction()) +
                (casingWeight * ucFactor) +
                (phoneticWeight * value.PhoneticDistributionImprobability());

            return result;
        }

        public static double GarbageProbabilityROCPlus(
            this string value,
            double umlautFactorWeight = 0.43d,
            double casingFactorWeight = 0.4d,
            double phoneticFactorWeight = 0.17d,
            int ignoredUpperCaseLetters = 1,
            int toleratedLowerCaseInUpper = 0)
        {
            double ucFactor = 0;
            double n = 1 / (casingFactorWeight + umlautFactorWeight + phoneticFactorWeight);
            double casingWeight = casingFactorWeight * n;
            double umlautWeight = umlautFactorWeight * n;
            double phoneticWeight = phoneticFactorWeight * n;

            if (invalidStart.IsMatch(value) ||
                invalidEnd.IsMatch(value) ||
                multipleUmlauts.IsMatch(value) ||
                qWithoutU.IsMatch(value) ||
                value.VowelCountWithoutY() == 0 ||
                value.ConsonantCountWithoutY() == 0 ||
                invalidLetterCombinations.IsMatch(value)) return 1d;

            int ucCount = value.UpperCount();
            if (ucCount > ignoredUpperCaseLetters && ucCount < (value.Length - toleratedLowerCaseInUpper))
            {
                ucFactor = value.UpperCaseFraction();
            }

            double result = (umlautWeight * value.UmlautFraction()) +
                (casingWeight * ucFactor) +
                (phoneticWeight * value.PhoneticDistributionImprobability());

            if (probableSyllablesGerman.IsMatch(value)) result -= Math.Min(0.4, result);
            else if (improbableLetterCombinations.IsMatch(value)) result += Math.Min(0.4, 1d - result);

            return result;
        }
        // Unfortunately the following is not realistic. High numbers of capital 
        // letters are more improbable (STRANgE) than low numbers (CamelCase).

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static double CasingDistributionLikelihood(this string value)
        //{
        //    return Math.Abs(value.UpperCaseFraction() - 0.5d) * 2d;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static double CasingDistributionImprobability(this string value)
        //{
        //    return 1d - (Math.Abs(value.UpperCaseFraction() - 0.5d) * 2d);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double PhoneticDistributionLikelihood(this string value)
        {
            // n konsonanten (Angstschweiß - 6 konsonanten wenn sch nur als 1 zählt)
            // n vokale (Treueeid, Spreeauen - je 5 vokale)
            return 1d - PhoneticDistributionImprobability(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double PhoneticDistributionImprobability(this string value)
        {
            // n konsonanten (Angstschweiß - 6 konsonanten wenn sch nur als 1 zählt)
            // n vokale (Treueeid, Spreeauen - je 5 vokale)
            return Math.Abs(value.VowelFractionWithY() - 0.5d) * 2d;
        }

        #endregion
        #region ILINQTable

        public static Ontology GetOntology(this ILINQTable table)
        {
            try
            {
                FieldInfo fEvent = table.GetType().GetField("PropertyChanging", BindingFlags.NonPublic | BindingFlags.Instance);
                MulticastDelegate dEvent = (MulticastDelegate)fEvent.GetValue(table);
                Delegate[] onChangingHandlers = dEvent.GetInvocationList();

                // Obtain the ChangeTracker
                foreach (Delegate handler in onChangingHandlers)
                {
                    if (handler.Target.GetType().Name == "StandardChangeTracker")
                    {
                        // Obtain the 'services' private field of the 'tracker'
                        object tracker = handler.Target;
                        object services = tracker.GetType().GetField("services", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tracker);

                        // Get the Context
                        DataContext context = services.GetType().GetProperty("Context").GetValue(services, null) as DataContext;
                        return context as Ontology;
                    }
                }

                // Not found
                throw new Exception("Error reflecting object");
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
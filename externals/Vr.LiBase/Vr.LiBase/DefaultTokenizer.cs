using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vr.LiBase
{
    public class DefaultTokenizer
    {
        public static IEnumerable<string> Split(string rawString) 
        {
            char[] splitChars = { };
            string[] result = rawString.Split(splitChars);
            foreach (string subString in result) yield return subString;
        }

        public static Dictionary<string, int> Process(IEnumerable<string> subStrings)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (string s in subStrings)
            {
                if (!result.ContainsKey(s)) result[s] = 1;
                else result[s] += 1;
            }

            return result;
        }
    }
}
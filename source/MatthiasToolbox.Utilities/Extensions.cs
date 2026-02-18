using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MatthiasToolbox.Cryptography.Checksums;
using System.Text.RegularExpressions;
using MatthiasToolbox.Utilities.Enumerations;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Utilities.Conversion.Points;
using MatthiasToolbox.Logging;
using System.Data.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MatthiasToolbox.Basics.Algorithms;

namespace MatthiasToolbox.Utilities
{
    public static class Extensions
    {
        #region dele

        public delegate void CopyCallbackDelegate(string source, string target, bool isFolder, bool wasOverwritten);

        #endregion
        #region cvar

        private static CRC32 crc32;

        #endregion
        #region ctor

        static Extensions()
        {
            crc32 = new CRC32();
        }

        #endregion
        #region impl

        #region List

        /// <summary>
        /// Create a string from the given list and a separator string.
        /// Example: "A,B,C"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToSeparatedString<T>(this List<T> list, string separator = ",")
        {
            string result = "";
            int count = list.Count;
            if (count > 0)
            {
                for (int i = 0; i < count - 1; i++)
                {
                    result += list[i].ToString() + separator;
                }
                result += list[count - 1].ToString();
            }
            return result;
        }

        /// <summary>
        /// Calculate the Levenshtein distance to the given string. (use for sentences)
        /// Set the correct language for the system tools first!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int DistanceTo<T>(this List<T> list, List<T> other) where T : IEquatable<T>
        {
            return LevenshteinDistance.Get(list, other);
        }

        #endregion
        #region Type

        /// <summary>
        /// Gets all types which implement the given interface / inherit the given class.
        /// This includes derived interfaces, abstract classes and other things. Use
        /// Type.IsRealClass() to identify concrete implementations.
        /// </summary>
        /// <param name="desiredType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetDerivedTypes(this Type desiredType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => desiredType.IsAssignableFrom(type));
        }

        /// <summary>
        /// Returns true only if the type represents a non abstract class which is not a generic type definition.
        /// </summary>
        /// <param name="testType"></param>
        /// <returns></returns>
        public static bool IsRealClass(this Type testType)
        {
            return testType.IsAbstract == false
                && testType.IsGenericTypeDefinition == false
                && testType.IsInterface == false;
        }

        #endregion
        #region Point

        public static IPoint2D<int> ToIPoint(this System.Drawing.Point value)
        {
            return new DrawingPointWrapper(value);
        }

        #endregion
        #region ITable

        /// <summary>
        /// Obtain the DataContext providing this entity
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static DataContext GetContext(this ILINQTable table)
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
                    return context;
                }
            }

            // Not found
            throw new Exception("Error reflecting object");
        }

        #endregion
        #region String

        #region basic

        /// <summary>
        /// Extract words. Set the correct language for the system tools first!
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<string> Tokenize(this string source)
        {
            return source.Split(SystemTools.NonLettersNonNumbers, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in De serialization
        /// </summary>
        /// <param name="pXmlString"></param>
        /// <returns></returns>
        public static Byte[] ToUTF8ByteArray(this String pXmlString)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        #endregion
        #region culture

        /// <summary>
        /// Returns value.ReplaceNonEnglishSpecialChars() if the culture is "en", 
        /// value.ReplaceNonGermanSpecialChars() if it is "de" and value otherwise.
        /// Non-alphabet special characters (interpunctation and others) will not be touched.
        /// See <see cref="ReplaceNonEnglishSpecialChars"/> and
        /// <see cref="ReplaceNonGermanSpecialChars"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AdjustToCulture(this string value)
        {
            if (SystemTools.Culture.TwoLetterISOLanguageName == "en")
            {
                return value.ReplaceNonEnglishSpecialChars();
            }
            else if (SystemTools.Culture.TwoLetterISOLanguageName == "de")
            {
                return value.ReplaceNonGermanSpecialChars();
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// replace letters which are not common in German to 
        /// their closest German approximations.
        /// examples: ñ will be replaced with with nj and
        /// the letter ø will be replaced with ö
        /// </summary>
        /// <param name="source"></param>
        /// <returns>the string with replaced special characters</returns>
        public static string ReplaceNonGermanSpecialChars(this string source)
        {
            string result = source;

            string[,] smallLettersToReplace = new string[17, 2];
            smallLettersToReplace[0, 0] = "á"; smallLettersToReplace[0, 1] = "a";
            smallLettersToReplace[1, 0] = "é"; smallLettersToReplace[1, 1] = "e";
            smallLettersToReplace[2, 0] = "ó"; smallLettersToReplace[2, 1] = "o";
            smallLettersToReplace[3, 0] = "è"; smallLettersToReplace[3, 1] = "e";
            smallLettersToReplace[4, 0] = "ñ"; smallLettersToReplace[4, 1] = "nj";
            smallLettersToReplace[5, 0] = "à"; smallLettersToReplace[5, 1] = "a";
            smallLettersToReplace[6, 0] = "í"; smallLettersToReplace[6, 1] = "i";
            smallLettersToReplace[7, 0] = "ú"; smallLettersToReplace[7, 1] = "u";
            smallLettersToReplace[8, 0] = "ã"; smallLettersToReplace[8, 1] = "a";
            smallLettersToReplace[9, 0] = "ç"; smallLettersToReplace[9, 1] = "c";
            smallLettersToReplace[10, 0] = "ô"; smallLettersToReplace[10, 1] = "o";
            smallLettersToReplace[11, 0] = "ê"; smallLettersToReplace[11, 1] = "e";
            smallLettersToReplace[12, 0] = "â"; smallLettersToReplace[12, 1] = "a";
            smallLettersToReplace[13, 0] = "î"; smallLettersToReplace[13, 1] = "i";
            smallLettersToReplace[14, 0] = "û"; smallLettersToReplace[14, 1] = "u";
            smallLettersToReplace[15, 0] = "å"; smallLettersToReplace[15, 1] = "a";
            smallLettersToReplace[16, 0] = "ø"; smallLettersToReplace[16, 1] = "ö";

            string[,] bigLettersToReplace = new string[17, 2];
            bigLettersToReplace[0, 0] = "Á"; bigLettersToReplace[0, 1] = "A";
            bigLettersToReplace[1, 0] = "É"; bigLettersToReplace[1, 1] = "E";
            bigLettersToReplace[2, 0] = "Ó"; bigLettersToReplace[2, 1] = "O";
            bigLettersToReplace[3, 0] = "È"; bigLettersToReplace[3, 1] = "E";
            bigLettersToReplace[4, 0] = "Ñ"; bigLettersToReplace[4, 1] = "Nj";
            bigLettersToReplace[5, 0] = "À"; bigLettersToReplace[5, 1] = "A";
            bigLettersToReplace[6, 0] = "Í"; bigLettersToReplace[6, 1] = "I";
            bigLettersToReplace[7, 0] = "Ú"; bigLettersToReplace[7, 1] = "U";
            bigLettersToReplace[8, 0] = "Ã"; bigLettersToReplace[8, 1] = "A";
            bigLettersToReplace[9, 0] = "Ç"; bigLettersToReplace[9, 1] = "C";
            bigLettersToReplace[10, 0] = "Ô"; bigLettersToReplace[10, 1] = "O";
            bigLettersToReplace[11, 0] = "Ê"; bigLettersToReplace[11, 1] = "E";
            bigLettersToReplace[12, 0] = "Â"; bigLettersToReplace[12, 1] = "A";
            bigLettersToReplace[13, 0] = "Î"; bigLettersToReplace[13, 1] = "I";
            bigLettersToReplace[14, 0] = "Û"; bigLettersToReplace[14, 1] = "U";
            bigLettersToReplace[15, 0] = "Å"; bigLettersToReplace[15, 1] = "A";
            bigLettersToReplace[16, 0] = "Ø"; bigLettersToReplace[16, 1] = "Ö";

            for (int i = 0; i <= smallLettersToReplace.GetUpperBound(0); i += 1) result = result.Replace(smallLettersToReplace[i, 0], smallLettersToReplace[i, 1]);
            for (int i = 0; i <= bigLettersToReplace.GetUpperBound(0); i += 1) result = result.Replace(bigLettersToReplace[i, 0], bigLettersToReplace[i, 1]);

            return result;
        }

        /// <summary>
        /// replace letters which are not common in English to 
        /// their closest English approximations.
        /// examples: ñ will be replaced with with nj and
        /// the letters ø and ö will be replaced with oe
        /// </summary>
        /// <param name="source"></param>
        /// <returns>the string with replaced special characters</returns>
        public static string ReplaceNonEnglishSpecialChars(this string source)
        {
            string result = source;

            string[,] smallLettersToReplace = new string[20, 2];
            smallLettersToReplace[0, 0] = "á"; smallLettersToReplace[0, 1] = "a";
            smallLettersToReplace[1, 0] = "é"; smallLettersToReplace[1, 1] = "e";
            smallLettersToReplace[2, 0] = "ó"; smallLettersToReplace[2, 1] = "o";
            smallLettersToReplace[3, 0] = "è"; smallLettersToReplace[3, 1] = "e";
            smallLettersToReplace[4, 0] = "ñ"; smallLettersToReplace[4, 1] = "nj";
            smallLettersToReplace[5, 0] = "à"; smallLettersToReplace[5, 1] = "a";
            smallLettersToReplace[6, 0] = "í"; smallLettersToReplace[6, 1] = "i";
            smallLettersToReplace[7, 0] = "ú"; smallLettersToReplace[7, 1] = "u";
            smallLettersToReplace[8, 0] = "ã"; smallLettersToReplace[8, 1] = "a";
            smallLettersToReplace[9, 0] = "ç"; smallLettersToReplace[9, 1] = "c";
            smallLettersToReplace[10, 0] = "ô"; smallLettersToReplace[10, 1] = "o";
            smallLettersToReplace[11, 0] = "ê"; smallLettersToReplace[11, 1] = "e";
            smallLettersToReplace[12, 0] = "â"; smallLettersToReplace[12, 1] = "a";
            smallLettersToReplace[13, 0] = "î"; smallLettersToReplace[13, 1] = "i";
            smallLettersToReplace[14, 0] = "û"; smallLettersToReplace[14, 1] = "u";
            smallLettersToReplace[15, 0] = "å"; smallLettersToReplace[15, 1] = "a";
            smallLettersToReplace[16, 0] = "ø"; smallLettersToReplace[16, 1] = "oe";

            smallLettersToReplace[17, 0] = "ä"; smallLettersToReplace[17, 1] = "ae";
            smallLettersToReplace[18, 0] = "ö"; smallLettersToReplace[18, 1] = "oe";
            smallLettersToReplace[19, 0] = "ü"; smallLettersToReplace[19, 1] = "ue";

            string[,] bigLettersToReplace = new string[20, 2];
            bigLettersToReplace[0, 0] = "Á"; bigLettersToReplace[0, 1] = "A";
            bigLettersToReplace[1, 0] = "É"; bigLettersToReplace[1, 1] = "E";
            bigLettersToReplace[2, 0] = "Ó"; bigLettersToReplace[2, 1] = "O";
            bigLettersToReplace[3, 0] = "È"; bigLettersToReplace[3, 1] = "E";
            bigLettersToReplace[4, 0] = "Ñ"; bigLettersToReplace[4, 1] = "Nj";
            bigLettersToReplace[5, 0] = "À"; bigLettersToReplace[5, 1] = "A";
            bigLettersToReplace[6, 0] = "Í"; bigLettersToReplace[6, 1] = "I";
            bigLettersToReplace[7, 0] = "Ú"; bigLettersToReplace[7, 1] = "U";
            bigLettersToReplace[8, 0] = "Ã"; bigLettersToReplace[8, 1] = "A";
            bigLettersToReplace[9, 0] = "Ç"; bigLettersToReplace[9, 1] = "C";
            bigLettersToReplace[10, 0] = "Ô"; bigLettersToReplace[10, 1] = "O";
            bigLettersToReplace[11, 0] = "Ê"; bigLettersToReplace[11, 1] = "E";
            bigLettersToReplace[12, 0] = "Â"; bigLettersToReplace[12, 1] = "A";
            bigLettersToReplace[13, 0] = "Î"; bigLettersToReplace[13, 1] = "I";
            bigLettersToReplace[14, 0] = "Û"; bigLettersToReplace[14, 1] = "U";
            bigLettersToReplace[15, 0] = "Å"; bigLettersToReplace[15, 1] = "A";
            bigLettersToReplace[16, 0] = "Ø"; bigLettersToReplace[16, 1] = "Oe";

            smallLettersToReplace[17, 0] = "Ä"; smallLettersToReplace[17, 1] = "Ae";
            smallLettersToReplace[18, 0] = "Ö"; smallLettersToReplace[18, 1] = "Oe";
            smallLettersToReplace[19, 0] = "Ü"; smallLettersToReplace[19, 1] = "Ue";

            for (int i = 0; i <= smallLettersToReplace.GetUpperBound(0); i += 1) result = result.Replace(smallLettersToReplace[i, 0], smallLettersToReplace[i, 1]);
            for (int i = 0; i <= bigLettersToReplace.GetUpperBound(0); i += 1) result = result.Replace(bigLettersToReplace[i, 0], bigLettersToReplace[i, 1]);

            return result;
        }

        #endregion
        #region clean

        /// <summary>
        /// Replace all non valid letters (see <see cref="SystemTools.NonLetterList"/>)
        /// Numbers will also be removed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string ReplaceNonLetters(this string source, char replacement)
        {
            string result = source;
            foreach (char c in SystemTools.NonLetterList) 
                result = result.Replace(c, replacement);
            return result;
        }

        /// <summary>
        /// Replace all non valid letters (see <see cref="SystemTools.NonLetterList"/>)
        /// Numbers will also be removed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string ReplaceNonLetters(this string source, string replacement)
        {
            string result = source;
            foreach (char c in SystemTools.NonLetterList)
                result = result.Replace(c.ToString(), replacement);
            return result;
        }

        /// <summary>
        /// Replace all non valid letters (see <see cref="SystemTools.NonLetterList"/>)
        /// Numbers will also be removed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string ReplaceNonLettersNonNumbers(this string source, char replacement)
        {
            string result = source;
            foreach (char c in SystemTools.NonLetterList)
                result = result.Replace(c, replacement);
            return result;
        }

        /// <summary>
        /// Replace all non valid letters (see <see cref="SystemTools.NonLetterList"/>)
        /// Numbers will also be removed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string ReplaceNonLettersNonNumbers(this string source, string replacement)
        {
            string result = source;
            foreach (char c in SystemTools.NonLetterList)
                result = result.Replace(c.ToString(), replacement);
            return result;
        }

        #endregion
        #region distance

        /// <summary>
        /// Calculate the Levenshtein distance to the given string. (use for words)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int DistanceTo(this string a, string other)
        {
            return LevenshteinDistance.Get(a, other);
        }

        /// <summary>
        /// Calculate the Levenshtein distance to the given string. (use for sentences)
        /// Set the correct language for the system tools first!
        /// </summary>
        /// <param name="a"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int DistanceToSentence(this string a, string other)
        {
            return LevenshteinDistance.Get(a.Tokenize(), other.Tokenize());
        }

        /// <summary>
        /// Calculate the Levenshtein distance to the given string. (use for sentences)
        /// Set the correct language for the system tools first!
        /// </summary>
        /// <param name="a"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int DistanceToSentence(this string a, string other, Func<string, List<string>> tokenizer)
        {
            return LevenshteinDistance.Get(tokenizer.Invoke(a), tokenizer.Invoke(other));
        }

        

        #endregion

        #endregion
        #region Byte[]

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        public static String UTF8ToString(this Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        #endregion
        #region DateTime

        /// <summary>
        /// Fuzzy comparison of two dates.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timeWithWindow"></param>
        /// <param name="windowInSeconds"></param>
        /// <returns></returns>
        public static bool RoughlyEquals(this DateTime time, DateTime timeWithWindow, int windowInSeconds = 1) //, int frequencyInSeconds)
        {
            long delta = (long)((TimeSpan)(timeWithWindow - time)).TotalSeconds; // % frequencyInSeconds;
            // delta = delta > windowInSeconds ? frequencyInSeconds - delta : delta;
            return Math.Abs(delta) < windowInSeconds;
        }

        /// <summary>
        /// Fuzzy comparison of two times. (the date will be ignored, only the time of day will be compared!)
        /// (2001 01 01 12:30:15).TimeRoughlyEquals(2002 01 01 12:30:15) => true;
        /// TODO: test this!
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timeWithWindow"></param>
        /// <param name="windowInSeconds"></param>
        /// <returns></returns>
        public static bool TimeRoughlyEquals(this DateTime time, DateTime timeWithWindow, int windowInSeconds = 1, int frequencyInSeconds = 60 * 60 * 24)
        {
            long delta = (long)((TimeSpan)(timeWithWindow - time)).TotalSeconds % frequencyInSeconds;
            delta = delta > windowInSeconds ? frequencyInSeconds - delta : delta;
            return Math.Abs(delta) < windowInSeconds;
        }

        #endregion
        #region FileInfo

        /// <summary>
        /// returns the filename without including the extension.
        /// if the filename is null or empty this will return 
        /// null or an empty string. if no "." is found this 
        /// will return the name property as it is found.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static string FileNameWithoutExtension(this FileInfo fi)
        {
            if (!string.IsNullOrEmpty(fi.Name) && fi.Name.Contains("."))
                return fi.Name.Substring(0, fi.Name.LastIndexOf('.'));
            else return fi.Name;
        }

        /// <summary>
        /// Return only the last extension of a file. e.g. for the
        /// file "Name.ext1.ext2.txt" only "txt" will be returned.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static string LastExtension(this FileInfo fi)
        {
            if (!string.IsNullOrEmpty(fi.Name) && fi.Name.Contains("."))
                return fi.Name.Substring(fi.Name.LastIndexOf('.') + 1);
            else return fi.Extension.Replace(".", "");
        }

        /// <summary>
        /// returns the filename and path without including the extension.
        /// if the filename is null or empty this will return 
        /// null or an empty string. if no "." is found this 
        /// will return the name property as it is found.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static string FullNameWithoutExtension(this FileInfo fi)
        {
            if (!string.IsNullOrEmpty(fi.Name) && fi.Name.Contains("."))
                return fi.FullName.Substring(0, fi.FullName.LastIndexOf('.'));
            else return fi.FullName;
        }

        /// <summary>
        /// If the file exists and the length property can be accessed 
        /// this will return true if the length is zero, false otherwise.
        /// In all other cases (file not found, access error) this will return null.
        /// </summary>
        /// <param name="fi">A file info instance to check.</param>
        /// <returns>True or false depending on the length, null in case of error / non existing file.</returns>
        public static bool? IsEmpty(this FileInfo fi)
        {
            try
            {
                if (fi.Exists) return fi.Length == 0;
                return null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Checks if the file can be read from by actually trying to read a byte. 
        /// This will also return false if the file is empty.
        /// </summary>
        /// <param name="fi">A file info instance to check.</param>
        /// <returns>True only if it is actually possible to read from this file.</returns>
        public static bool CanRead(this FileInfo fi, bool log = false)
        {
            FileStream sr = null;
            bool result = false;

            try
            {
                if (!fi.Exists || fi.Length == 0) return false;
                sr = fi.OpenRead();
                sr.ReadByte();
                result = true;
            }
            catch (Exception ex) 
            {
                if (log) Logger.Log<INFO>("MatthiasToolbox.Utilities.Extensions", ex);
            }
            finally 
            {
                if(sr != null) sr.Close();
            }

            return result;
        }

        /// <summary>
        /// Checks if the file can be written to. 
        /// </summary>
        /// <param name="fi">A file info instance to check.</param>
        /// <returns>True only if it is actually possible to read from this file.</returns>
        public static bool CanWrite(this FileInfo fi, bool log = false)
        {
            FileStream sr = null;
            bool result = false;

            try
            {
                sr = fi.OpenWrite();
                result = true;
            }
            catch (Exception ex) 
            {
                if (log) Logger.Log<INFO>("MatthiasToolbox.Utilities.Extensions", ex);
            }
            finally
            {
                if (sr != null) sr.Close();
            }

            return result;
        }

        /// <summary>
        /// Calculate the CRC32 checksum for this instance.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns>The CRC32 checksum for this instance.</returns>
        public static int GetCRC32(this FileInfo fi)
        {
            return crc32.GetCrc32(fi);
        }

        public static Uri ToURI(this FileInfo fi)
        {
            return new Uri(fi.FullName);
        }

        #endregion
        #region StackTrace

        /// <summary>
        /// Retrieve the declaring type of the method which called the method from which you are calling this.
        /// </summary>
        /// <param name="frameOffset">
        /// -1 = you
        /// 0 = your caller
        /// 1 = caller of your caller
        /// ...
        /// </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type GetCaller(this StackTrace stackTrace, int frameOffset = 0)
        {
            return stackTrace.GetFrame(2 + frameOffset).GetMethod().DeclaringType;
        }

        #endregion
        #region DirectoryInfo

        /// <summary>
        /// Copy all directory contents to the given target folder. If the folder doesn't exist it will be created.
        /// If overwrite is set to false all existing files will be skipped.
        /// If skipExistingFolders is set to false all existing folders will be skipped completely.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="overwrite"></param>
        /// <param name="skipExistingFolders"></param>
        /// <param name="callBack"></param>
        public static void CopyTo(this DirectoryInfo source, DirectoryInfo target, bool overwrite = true, bool skipExistingFolders = false, CopyCallbackDelegate callBack = null)
        {
            bool call = callBack != null;

            // Check if the target directory exists, if not, create it.
            if (!Directory.Exists(target.FullName)) Directory.CreateDirectory(target.FullName);

            // Copy each file into its new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                string trg = Path.Combine(target.FullName, fi.Name);
                bool over = File.Exists(trg);
                fi.CopyTo(trg, overwrite);
                if (call) callBack.Invoke(fi.FullName, trg, false, over);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo sourceSub in source.GetDirectories())
            {
                DirectoryInfo targetSub = new DirectoryInfo(Path.Combine(target.FullName, sourceSub.Name));
                bool over = targetSub.Exists;
                if (over && skipExistingFolders) continue;
                if (!over) target.CreateSubdirectory(sourceSub.Name);
                sourceSub.CopyTo(targetSub, overwrite, skipExistingFolders, callBack);
                if (call) callBack.Invoke(sourceSub.FullName, targetSub.FullName, true, over);
            }
        }

        /// <summary>
        /// Copy all directory contents to the given target folder. If the folder doesn't exist it will be created.
        /// If overwrite is set to false all existing files will be skipped.
        /// If skipExistingFolders is set to false all existing folders will be skipped completely.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="overwrite"></param>
        /// <param name="skipExistingFolders"></param>
        /// <param name="callBack"></param>
        public static void CopyTo(this DirectoryInfo source, string target, bool overwrite = true, bool skipExistingFolders = false, CopyCallbackDelegate callBack = null)
        {
            source.CopyTo(new DirectoryInfo(target), overwrite, skipExistingFolders);
        }

        /// <summary>
        /// Determine if a sub folder with the given name exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="subFolderName"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        public static bool HasSubFolder(this DirectoryInfo source, string subFolderName, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return source.GetDirectories(subFolderName, searchOption).Length > 0;
        }

        /// <summary>
        /// returns the first match
        /// </summary>
        /// <param name="source"></param>
        /// <param name="subFolderName"></param>
        /// <param name="searchOption"></param>
        /// <returns>null if no sub dir with the given name was found</returns>
        public static DirectoryInfo GetSubFolder(this DirectoryInfo source, string subFolderName, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!source.HasSubFolder(subFolderName, searchOption)) return null;
            return source.GetDirectories(subFolderName, searchOption)[0];
        }

        /// <summary>
        /// Returns all files in the given directory and subdirectories.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> AllFiles(this DirectoryInfo source)
        {
            return source.EnumerateFiles("*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Returns all directories in the given directory and subdirectories.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<DirectoryInfo> AllDirectories(this DirectoryInfo source)
        {
            return source.EnumerateDirectories("*", SearchOption.AllDirectories);
        }

        #endregion
        #region OperatingSystem

        public static WindowsVersion WindowsVersion(this OperatingSystem os)
        {
            // Determine the platform.
            switch (os.Platform)
            {
                // Platform is Windows 95, Windows 98, Windows 98 Second Edition, or Windows Me.
                case System.PlatformID.Win32Windows:

                    switch (os.Version.Minor)
                    {
                        case 0:
                            return Enumerations.WindowsVersion.Windows_95;
                        case 10:
                            if (os.Version.Revision.ToString() == "2222A")
                            {
                                return Enumerations.WindowsVersion.Windows_98_SE;
                            }
                            else
                            {
                                return Enumerations.WindowsVersion.Windows_98;
                            }
                        case 90:
                            return Enumerations.WindowsVersion.Windows_Me;
                    }

                    break;

                // Platform is Windows NT 3.51, Windows NT 4.0, Windows 2000, or Windows XP.
                case System.PlatformID.Win32NT:

                    switch (os.Version.Major)
                    {
                        case 3:
                            return Enumerations.WindowsVersion.Windows_NT_3_51;
                        case 4:
                            return Enumerations.WindowsVersion.Windows_NT_4_0;
                        case 5:
                            if (os.Version.Minor == 0)
                            {
                                return Enumerations.WindowsVersion.Windows_2000;
                            }
                            else if (os.Version.Minor == 1)
                            {
                                return Enumerations.WindowsVersion.Windows_XP;
                            }
                            else if (os.Version.Minor == 2)
                            {
                                return Enumerations.WindowsVersion.Windows_2003;
                            }
                            else return Enumerations.WindowsVersion.Unknown;
                        case 6:
                            return Enumerations.WindowsVersion.Windows_Vista;
                        case 7:
                            return Enumerations.WindowsVersion.Windows_7;
                    }

                    break;
            }

            return Enumerations.WindowsVersion.Unknown;
        }

        public static string WindowsVersionName(this OperatingSystem os)
        {
            // Determine the platform.
            switch (os.Platform)
            {
                // Platform is Windows 95, Windows 98, Windows 98 Second Edition, or Windows Me.
                case System.PlatformID.Win32Windows:

                    switch (os.Version.Minor)
                    {
                        case 0:
                            return "Windows 95";
                        case 10:
                            if (os.Version.Revision.ToString() == "2222A")
                            {
                                return "Windows 98 Second Edition";
                            }
                            else
                            {
                                return "Windows 98";
                            }
                        case 90:
                            return "Windows Me";
                    }
                    
                    break;

                // Platform is Windows NT 3.51, Windows NT 4.0, Windows 2000, or Windows XP.
                case System.PlatformID.Win32NT:

                    switch (os.Version.Major)
                    {
                        case 3:
                            return "Windows NT 3.51";
                        case 4:
                            return "Windows NT 4.0";
                        case 5:
                            if (os.Version.Minor == 0)
                            {
                                return "Windows 2000";
                            }
                            else if (os.Version.Minor == 1)
                            {
                                return "Windows XP";
                            }
                            else if (os.Version.Minor == 2)
                            {
                                return "Windows Server 2003";
                            }
                            else return "Unknown";
                        case 6:
                            return "Windows Vista";
                        case 7:
                            return "Windows 7";
                    }

                    break;
            }

            return "Unknown";
        }

        /// <summary>
        /// True for XP, 2003, Vista, Windows 7 and above
        /// </summary>
        public static bool IsAtLeastWindowsXP(this OperatingSystem os)
        {
            return (int)os.WindowsVersion() >= (int)Enumerations.WindowsVersion.Windows_XP;
        }

        /// <summary>
        /// True for Vista, Windows 7 and above
        /// </summary>
        public static bool IsAtLeastWindowsVista(this OperatingSystem os)
        {
            return (int)os.WindowsVersion() >= (int)Enumerations.WindowsVersion.Windows_Vista;
        }

        /// <summary>
        /// True only for Windows 7 and above
        /// </summary>
        public static bool IsAtLeastWindows7(this OperatingSystem os)
        {
            return (int)os.WindowsVersion() >= (int)Enumerations.WindowsVersion.Windows_7;
        }

        #endregion

        #endregion
    }
}
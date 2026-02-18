///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: Myers Text Delta Algorithm - non generic wrapper
//    Status:      RELEASE
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Donnerstag, 29. März 2007 Matthias Gruber original version
//      Dienstag, 08. Mai  2007 Matthias Gruber first working version
//       Samstag, 12. Mai  2007 Matthias Gruber major refractoring
//       Sonntag, 13. Mai  2007 Matthias Gruber further refractoring, unit tests
//      Dienstag, 29. Mai  2007 Matthias Gruber final testing & comments
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using MatthiasToolbox.Delta.Utilities;

namespace MatthiasToolbox.Delta.Diff
{
    /// <summary>
    /// non generic wrapper for MyersDiff&lt;T&gt; for use with multi line text
    /// </summary>
    public class MyersDiff
    {
        #region cvar

        private MyersDiff<int> myers;   // the actual algorithm is initialized to int for performance

        /// <summary>
        /// temporary variables
        /// </summary>
        private String[] tmpLines1;
        private String[] tmpLines2;
        private int[] indices1;
        private int[] indices2;
        private Hashtable<String, int> tmpDict;
        private int length1;
        private int length2;

        #endregion
        #region prop

        /// <summary>
        /// length of the first collection
        /// </summary>
        public int Length1
        {
            get { return length1; }
        }

        /// <summary>
        /// length of the second collection
        /// </summary>
        public int Length2
        {
            get { return length2; }
        }

        /// <summary>
        /// first index collection
        /// </summary>
        public int[] Indices1()
        {
            return indices1;
        }

        /// <summary>
        /// second index collection
        /// </summary>
        public int[] Indices2()
        {
            return indices2;
        }

        #endregion
        #region impl

        #region public

        /// <summary>
        /// calculate line by line distance between two texts
        /// </summary>
        /// <param name="text1">
        /// source text
        /// </param>
        /// <param name="text2">
        /// target text
        /// </param>
        /// <param name="ignoreSpace">
        /// flag if subsequent whitespaces are to be treated as only one space
        /// </param>
        /// <returns>
        /// min(paragraphcount) minus the length of the shortest edit script
        /// </returns>
        public int GetDistance(String text1, String text2, bool ignoreSpace)
        {
            tmpLines1 = Converter.Dedicated.Convert(text1, ignoreSpace);
            tmpLines2 = Converter.Dedicated.Convert(text2, ignoreSpace);

            mergeCodes(ignoreSpace);

            tmpLines1 = null;
            tmpLines2 = null;
            length1 = indices1.Length;
            length2 = indices2.Length;

            myers = new MyersDiff<int>(indices1, indices2);

            return Math.Min(length1, length2) - myers.CalculateLength();
        } // Trace

        /// <summary>
        /// calculate line by line difference between two texts
        /// </summary>
        /// <param name="text1">
        /// source text
        /// </param>
        /// <param name="text2">
        /// target text
        /// </param>
        /// <param name="ignoreSpace">
        /// flag if subsequent whitespaces are to be treated as only one space
        /// </param>
        /// <returns>
        /// the trace which contains the traversed match points in the found shortest edit script
        /// </returns>
        public Trace GetDiff(String text1, String text2, bool ignoreSpace)
        {
            tmpLines1 = Converter.Dedicated.Convert(text1, ignoreSpace);
            tmpLines2 = Converter.Dedicated.Convert(text2, ignoreSpace);

            mergeCodes(ignoreSpace);

            tmpLines1 = null;
            tmpLines2 = null;
            length1 = indices1.Length;
            length2 = indices2.Length;

            myers = new MyersDiff<int>(indices1, indices2);

            myers.MakeEditScript();

            return myers.Trace;
        } // Trace

        /// <summary>
        /// calculate line by line difference between two texts
        /// </summary>
        /// <param name="text1">
        /// source text
        /// </param>
        /// <param name="text2">
        /// target text
        /// </param>
        /// <param name="ignoreSpace">
        /// flag if subsequent whitespaces are to be treated as only one space
        /// </param>
        /// <returns>
        /// the trace which contains the traversed match points in the found shortest edit script
        /// </returns>
        public Trace GetDiff(FileInfo text1, FileInfo text2, bool ignoreSpace)
        {
            tmpLines1 = Converter.Dedicated.Convert(text1, ignoreSpace);
            tmpLines2 = Converter.Dedicated.Convert(text2, ignoreSpace);

            mergeCodes(ignoreSpace);

            tmpLines1 = null;
            tmpLines2 = null;
            length1 = indices1.Length;
            length2 = indices2.Length;

            myers = new MyersDiff<int>(indices1, indices2);

            myers.MakeEditScript();

            return myers.Trace;
        } // Trace

        #endregion
        #region private

        /// <summary>
        /// combined indexer for source and target textlines
        /// </summary>
        /// <param name="trimSpace">
        /// fags if leading and trailing spaces are to be ignored
        /// </param>
        private void mergeCodes(bool trimSpace)
        {
            tmpDict = new Hashtable<String, int>(tmpLines1.Length + tmpLines2.Length);
            int increment = 0;
            String tmp;

            indices1 = new int[tmpLines1.Length];
            indices2 = new int[tmpLines2.Length];

            if (trimSpace)
            {
                for (int i = 0; i < tmpLines1.Length; ++i)
                {
                    tmp = tmpLines1[i].Trim();
                    if (tmpDict[tmp] == null)
                    {
                        increment += 1;
                        tmpDict[tmp] = increment;
                        indices1[i] = increment;
                    }
                    else
                    {
                        indices1[i] = (int)tmpDict[tmp];
                    }
                }

                for (int i = 0; i < tmpLines2.Length; ++i)
                {
                    tmp = tmpLines2[i].Trim();
                    if (tmpDict[tmp] == null)
                    {
                        increment += 1;
                        tmpDict[tmp] = increment;
                        indices2[i] = increment;
                    }
                    else
                    {
                        indices2[i] = (int)tmpDict[tmp];
                    }
                }
            }
            else
            {
                for (int i = 0; i < tmpLines1.Length; ++i)
                {
                    tmp = tmpLines1[i];
                    if (tmpDict[tmp] == null)
                    {
                        increment += 1;
                        tmpDict[tmp] = increment;
                        indices1[i] = increment;
                    }
                    else
                    {
                        indices1[i] = (int)tmpDict[tmp];
                    }
                }

                for (int i = 0; i < tmpLines2.Length; ++i)
                {
                    tmp = tmpLines2[i];
                    if (tmpDict[tmp] == null)
                    {
                        increment += 1;
                        tmpDict[tmp] = increment;
                        indices2[i] = increment;
                    }
                    else
                    {
                        indices2[i] = (int)tmpDict[tmp];
                    }
                }
            }

            tmpDict = null;
        } // void

        #endregion

        #endregion
    } // class
}
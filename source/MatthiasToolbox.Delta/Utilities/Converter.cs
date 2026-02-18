///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: Converter class implementation.
//    Status:      FINAL/RELEASE?
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Mittwoch, 10. Mai 2006 BernhardGlueck Original Version.
//    Mittwoch, 09. Mai 2007 Matthias Gruber comments and formatting
//     Freitag, 11. Mai 2007 added dedicated String[](FileInfo)
//     Samstag, 12. Mai 2007 added dedicated String[](String)
//     Sonntag, 13. Mai 2007 added dedicated String(List<TxtCommand>)
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// Converter class implementation.
    /// </summary>
    public static class Converter
    {
        #region Generic conversion methods
        
        /// <summary>
        /// generic conversion
        /// </summary>
        /// <typeparam name="S">source type</typeparam>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="source">source object</param>
        /// <returns>converted object</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static T Convert<S,T>( S source )
        {
            return Convert< S, T >( source, default( T ) );
        }

        /// <summary>
        /// generic conversion
        /// </summary>
        /// <typeparam name="S">source type</typeparam>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="source">source object</param>
        /// <param name="errorValue">the value to return if the conversion fails</param>
        /// <returns>converted object</returns>
        public static T Convert<S,T>( S source, T errorValue )
        {
            bool success = false;
            return Convert< S, T >( source, errorValue, null, ref success );
        }

        /// <summary>
        /// generic conversion
        /// </summary>
        /// <typeparam name="S">source type</typeparam>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="source">source object</param>
        /// <param name="errorValue">the value to return if the conversion fails</param>
        /// <param name="formatter">an IFormatProvider for the conversion</param>
        /// <param name="success">success flag</param>
        /// <returns>converted object</returns>
        public static T Convert<S,T>( S source, T errorValue, IFormatProvider formatter, ref bool success )
        {
            return (T) ConvertObject( source, typeof ( T ), errorValue, formatter, ref success );
        }
        
        #endregion
        #region Non generic conversion methods
        
        /// <summary>
        /// collection converter
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="target">target object</param>
        /// <param name="errorValue">value to return if conversion fails</param>
        /// <returns>false in case errorValue is null, conversion success else</returns>
        public static bool      ConvertCollection( IEnumerable source,
                                                   IList target,
                                                   Object errorValue )
        {
            if ( errorValue != null )
            {
                return ConvertCollection( source, target, errorValue.GetType(), errorValue, null,true );
            }
            return false;
        }

        /// <summary>
        /// collection converter
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="target">target object</param>
        /// <param name="targetType">the type of the elements</param>
        /// <param name="errorValue">value to return if conversion fails</param>
        /// <param name="formatter">an IFormatProvider for the conversion</param>
        /// <param name="skipNullValues">flags if null entries should be skipped</param>
        /// <returns>false in case the convrsion fails</returns>
        public static bool      ConvertCollection( IEnumerable source,
                                                   IList target,
                                                   Type targetType,
                                                   Object errorValue,
                                                   IFormatProvider formatter,
                                                   bool skipNullValues )
        {
            bool useIndexAssignment = false;
            // Check if the List supports adding
            try
            {
                target.Add( null );
            }
            catch( NotSupportedException )
            {
                useIndexAssignment = true;
            }
            
            int index = 0;
            bool overallSuccess = true;
            
            foreach( Object elementObject in source )
            {
                bool success = false;
                Object convertedObject = ConvertObject( elementObject, targetType, errorValue, formatter, ref success );
                
                if ( success )
                {
                    if ( useIndexAssignment )
                    {
                        if( index < target.Count )
                        {
                            if ( skipNullValues )
                            {
                                if ( convertedObject != null )
                                {
                                    target[ index ] = convertedObject;
                                }
                                else
                                {
                                    index --;
                                }
                            }
                            else
                            {
                                target[ index ] = convertedObject;
                            }
                        }
                    }
                    else
                    {
                        if ( skipNullValues )
                        {
                            if ( convertedObject != null )
                            {
                                target.Add( convertedObject );
                            }
                        }
                        else
                        {
                            target.Add( convertedObject );
                        }
                    }
                }
                else
                {
                    overallSuccess = false;
                }
                
                index ++;
            }
            return overallSuccess;
        }
        
        /// <summary>
        /// convert an object to a given type
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="targetType">the target type</param>
        /// <returns>false in case the convrsion fails</returns>
        public static Object    ConvertObject( Object source,Type targetType )
        {
            bool success = false;
            return ConvertObject( source, targetType, null, null, ref success );
        }
        
        /// <summary>
        /// convert an object
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="errorValue">value to return in case of error</param>
        /// <returns>converted object</returns>
        public static Object    ConvertObject( Object source,Object errorValue )
        {
            bool success = false;
            return ConvertObject( source, errorValue, null, ref success );
        }
        
        /// <summary>
        /// convert an object
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="errorValue">value to return in case of error</param>
        /// <param name="formatter">an IFormatProvider for the conversion</param>
        /// <param name="success">success flag</param>
        /// <returns>converted object</returns>
        public static Object    ConvertObject( Object source,Object errorValue,IFormatProvider formatter,ref bool success )
        {
            if ( errorValue != null )
            {
                return ConvertObject( source, errorValue.GetType(),errorValue, formatter, ref success );
            }
            return errorValue;
        }
        
        /// <summary>
        /// convert an object to a given type
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="targetType">the target type</param>
        /// <param name="errorValue">value to return in case of error</param>
        /// <param name="formatter">an IFormatProvider for the conversion</param>
        /// <param name="success">success flag</param>
        /// <returns>converted object</returns>
        public static Object    ConvertObject( Object source, Type targetType,Object errorValue,IFormatProvider formatter, ref bool success )
        {
            if ( formatter == null )
            {
                formatter = System.Threading.Thread.CurrentThread.CurrentCulture;
            }

            // Assert that all parameters are correct.
            if ( source == null )
            {
                success = false;
                return errorValue;
            }
            if ( targetType == null )
            {
                success = false;
                return errorValue;
            }

            // Take a shortcut if the same type.
            Type sourceType = source.GetType();

            if ( sourceType.Equals( targetType ) )
            {
                success = true;
                return source;
            }

            // Fast basic type conversion.
            if ( targetType.IsPrimitive )
            {
                if ( sourceType.IsPrimitive )
                {
                    Object result = System.Convert.ChangeType( source, targetType,formatter );
                
                    if ( result != null )
                    {
                        success = true;
                        return result;
                    }
                }
                else
                {
                    IConvertible sourceConvertible = source as IConvertible;
                    if ( sourceConvertible != null )
                    {
                        Object result = ConvertByInterface( sourceConvertible,targetType,formatter );
                        if ( result != null )
                        {
                            success = true;
                            return result;
                        }
                    }
                }
            }

            // Now we go with the slower reflection based TypeConverter way.
            TypeConverter sourceConverter = TypeDescriptor.GetConverter( sourceType );

            if ( sourceConverter != null )
            {
                if ( sourceConverter.CanConvertTo( targetType ) )
                {
                    Object targetResult = sourceConverter.ConvertTo( source, targetType );

                    if ( targetResult != null )
                    {
                        success = true;
                        return targetResult;
                    }
                }
            }

            TypeConverter targetConverter = TypeDescriptor.GetConverter( targetType );

            if ( targetConverter != null )
            {
                if ( targetConverter.CanConvertFrom( sourceType ) )
                {
                    Object targetResult = targetConverter.ConvertFrom( source );
                    if ( targetResult != null )
                    {
                        success = true;
                        return targetResult;
                    }
                }
            }

            // Fallback for strings placed last since ToString is often not
            // the correct path for full type conversion.
            if ( targetType.Equals( typeof ( String ) ) )
            {
                success = true;
                return source.ToString();
            }
            
            success = false;
            return errorValue;
        }

        /// <summary>
        /// convert an IConvertible object to a given type
        /// </summary>
        /// <param name="source">source object</param>
        /// <param name="targetType">the target type</param>
        /// <param name="formatter">an IFormatProvider for the conversion</param>
        /// <returns>converted object</returns>
        private static Object ConvertByInterface( IConvertible source, Type targetType, IFormatProvider formatter )
        {
            return source.ToType( targetType, formatter );
        }
        
        #endregion
        #region specialized conversion methods
        
        /// <summary>
        /// container for converters dedicated especially to the SDelta library
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public static class Dedicated
        {
            /// <summary>
            /// convert text from a file into an array of lines
            /// </summary>
            /// <param name="textFile">the text file</param>
            /// <returns>an array of lines or null on error</returns>
            internal static String[] Convert(FileInfo textFile)
            {
                StreamReader r = textFile.OpenText();
                string text = r.ReadToEnd();
                r.Close();
                return text.Replace("\r", "").Split('\n');
            }

            /// <summary>
            /// convert text from a file into an array of lines
            /// </summary>
            /// <param name="textFile">the text file</param>
            /// <param name="ignoreSpace">
            /// if true any number of subsequent spaces is 
            /// converted to only one space character
            /// </param>
            /// <returns>an array of lines or null on error</returns>
            internal static String[] Convert(FileInfo textFile, bool ignoreSpace)
            {
                if (!ignoreSpace) return Convert(textFile);
               
                String[] lines = File.ReadAllLines(textFile.FullName); // text.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = Regex.Replace(lines[i], "\\s+", " ");
                }
                return lines;
            }
            
            /// <summary>
            /// convert text into an array of lines
            /// </summary>
            /// <param name="text">the text</param>
            /// <returns>an array of lines</returns>
            internal static String[] Convert(String text)
            {
                return text.Replace("\r", "").Split('\n');
            }
            
            /// <summary>
            /// convert text into an array of lines
            /// </summary>
            /// <param name="text">the text</param>
            /// <param name="ignoreSpace">
            /// if true any number of subsequent spaces is 
            /// converted to only one space character
            /// </param>
            /// <returns>an array of lines</returns>
            internal static String[] Convert(String text, bool ignoreSpace)
            {
                if (ignoreSpace)
                {
                    String[] lines = text.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
                    for(int i = 0; i < lines.Length; i++ )
                    {
                        lines[i] = Regex.Replace(lines[i], "\\s+", " ");
                    }
                    return lines;
                }
                else return Convert(text);
            } // String[]
        } // class

        #endregion
    } // class
} // namespace
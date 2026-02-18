using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using MatthiasToolbox.Logging;
using Microsoft.Win32;

namespace MatthiasToolbox.Utilities
{
    public class SystemTools
    {
        #region cvar

        // for convenient logging
        private static SystemTools instance;

        private static char[] alphabetEN = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        private static char[] alphabetDE = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ü', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Ä', 'Ö', 'Ü', 'ß' };
        private static char[] nonAlphabetEN;
        private static char[] nonAlphabetDE;
        private static char[] nonAlphabetNumEN;
        private static char[] nonAlphabetNumDE;

        private static List<char> alphabetListEN = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        private static List<char> alphabetListDE = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ü', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'Ä', 'Ö', 'Ü', 'ß' };
        private static List<char> nonAlphabetListEN = new List<char>();
        private static List<char> nonAlphabetListDE = new List<char>();
        private static List<char> nonAlphabetNumListEN = new List<char>();
        private static List<char> nonAlphabetNumListDE = new List<char>();

        #endregion
        #region prop

        /// <summary>
        /// Culture base for all system utility classes.
        /// </summary>
        public static CultureInfo Culture { get; set; }

        /// <summary>
        /// Depending on the culture this will return valid letters for the language.
        /// Currently only "de" and "en" are implemented, for other cultures only 26 
        /// ASCII letters will be returned
        /// </summary>
        public static char[] ValidLetters 
        { 
            get 
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return alphabetEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return alphabetDE;
                }
                else
                {
                    return alphabetEN;
                }
            } 
        }

        /// <summary>
        /// Depending on the culture this will return valid letters for the language.
        /// Currently only "de" and "en" are implemented, for other cultures only 26 
        /// ASCII letters will be returned
        /// </summary>
        public static List<char> ValidLetterList
        {
            get
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return alphabetListEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return alphabetListDE;
                }
                else
                {
                    return alphabetListEN;
                }
            } 
        }

        /// <summary>
        /// Depending on the culture this will return all characters which are not in 
        /// the valid letters list for the language. Currently only "de" and "en" are 
        /// implemented. All other languages will return the same as in "en".
        /// </summary>
        public static char[] NonLetters
        {
            get
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return nonAlphabetEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return nonAlphabetDE;
                }
                else
                {
                    return nonAlphabetEN;
                }
            }
        }

        /// <summary>
        /// Depending on the culture this will return all characters which are not in 
        /// the valid letters list for the language. Currently only "de" and "en" are 
        /// implemented. All other languages will return the same as in "en".
        /// </summary>
        public static List<char> NonLetterList
        {
            get
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return nonAlphabetListEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return nonAlphabetListDE;
                }
                else
                {
                    return nonAlphabetListEN;
                }
            }
        }

        /// <summary>
        /// Depending on the culture this will return all characters which are not in 
        /// the valid letters list for the language. Currently only "de" and "en" are 
        /// implemented. All other languages will return the same as in "en".
        /// </summary>
        public static char[] NonLettersNonNumbers
        {
            get
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return nonAlphabetNumEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return nonAlphabetNumDE;
                }
                else
                {
                    return nonAlphabetNumEN;
                }
            }
        }

        /// <summary>
        /// Depending on the culture this will return all characters which are not in 
        /// the valid letters list for the language. Currently only "de" and "en" are 
        /// implemented. All other languages will return the same as in "en".
        /// </summary>
        public static List<char> NonLetterNonNumberList
        {
            get
            {
                if (Culture.TwoLetterISOLanguageName == "en")
                {
                    return nonAlphabetNumListEN;
                }
                else if (Culture.TwoLetterISOLanguageName == "de")
                {
                    return nonAlphabetNumListDE;
                }
                else
                {
                    return nonAlphabetNumListEN;
                }
            }
        }

        #endregion
        #region ctor

        static SystemTools()
        {
            // for convenient logging
            instance = new SystemTools();
            Culture = CultureInfo.CurrentCulture;

            // english
            for (int b = 0; b <= 255; b += 1)
            {
                if (!alphabetListEN.Contains((char)b)) nonAlphabetListEN.Add((char)b);
            }
            nonAlphabetEN = nonAlphabetListEN.ToArray();
            nonAlphabetNumListEN.AddRange(nonAlphabetListEN);
            nonAlphabetNumListEN.RemoveAll(c => "1234567890".Contains(c));
            nonAlphabetNumEN = nonAlphabetNumListEN.ToArray();

            // german
            for (int b = 0; b <= 255; b += 1)
            {
                if (!alphabetListDE.Contains((char)b)) nonAlphabetListDE.Add((char)b);
            }
            nonAlphabetDE = nonAlphabetListDE.ToArray();
            nonAlphabetNumListDE.AddRange(nonAlphabetListDE);
            nonAlphabetNumListDE.RemoveAll(c => "1234567890".Contains(c));
            nonAlphabetNumDE = nonAlphabetNumListDE.ToArray();
        }

        private SystemTools() { }

        #endregion
        #region impl

        #region registry

        /// <summary>
        /// try to find the installation path for an 
        /// installed windows application by its
        /// display name. Unfortunately many apps have 
        /// no registered display name and many apps
        /// do not register their install location.
        /// However, if no install location is registered
        /// this will try to use the display icon or
        /// (todo) uninstall string to come to a path. be aware
        /// though that the uninstall path is not necessarily
        /// the same as the install path.
        /// </summary>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static DirectoryInfo FindInstalledApp(string displayName)
        {
            try
            {
                string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey);
                foreach (string skName in rk.GetSubKeyNames())
                {
                    RegistryKey sk = rk.OpenSubKey(skName);
                    if (sk.GetValue("DisplayName") != null)
                    {
                        if (sk.GetValue("DisplayName").ToString().ToLower().Contains(displayName.ToLower()))
                        {
                            if (sk.GetValue("InstallLocation") != null)
                            {
                                return new DirectoryInfo(sk.GetValue("InstallLocation").ToString());
                            }
                            else if (sk.GetValue("DisplayIcon") != null)
                            {
                                string icon = sk.GetValue("DisplayIcon").ToString();
                                int exePos = icon.LastIndexOf("\\");
                                string path = icon.Substring(0, exePos);
                                return new DirectoryInfo(path);
                            }
                            else if (sk.GetValue("UninstallString") != null)
                            {
#if DEBUG
                                instance.Log<WARN>("Unable to parse UninstallString.");
#endif
                                // TODO: parse from uninstall string
                            }
                            else
                            {
#if DEBUG
                                instance.Log<WARN>("No useful data found in the registry key.");
#endif
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                instance.Log<ERROR>("Problem reading the registry key.", ex);
            }
            return null;
        }

        #endregion
        #region processes

        #region info

        public static string ExecutablePath()
        {
            String codeBase = Assembly.GetExecutingAssembly().CodeBase;
            return codeBase.Substring(8, codeBase.LastIndexOf('/') - 8).Replace("/", "\\");
        }
        
        public static DirectoryInfo ExecutablePathInfo() 
        {
        	String codeBase = Assembly.GetExecutingAssembly().CodeBase;
        	return new DirectoryInfo(codeBase.Substring(8, codeBase.LastIndexOf('/') - 8).Replace("/", "\\"));
        }
        
        public static Uri ExecutablePathUri() 
        {
        	String codeBase = Assembly.GetExecutingAssembly().CodeBase;
        	return new Uri(codeBase.Substring(0, codeBase.LastIndexOf('/')));
        }

        #endregion
        #region running

        /// <summary>
        /// check if a process with the given filename 
        /// (case insensitive) is currently running.
        /// usually this runs with normal user privileges,
        /// so services and other higher-privileged apps
        /// will not be found.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsProcessRunning(string fileName)
        {
            bool result = false;

            Process[] runningProcesses = Process.GetProcesses();
            
            foreach (Process proc in runningProcesses)
            {
                try
                {
                    if (proc.MainModule.FileName.ToLower().EndsWith(fileName.ToLower()))
                    {
                        result = true;
                        break;
                    }
                }
                catch (Exception ex) // usually access error
                {
                    string n = "";
                    try { n = proc.ProcessName; }
                    catch { /* CANNOT RECOVER */ }
#if DEBUG
                    instance.Log<ERROR>("Error investigating process " + n + ".", ex);
#endif
                } 
            }

            return result;
        }

        /// <summary>
        /// find the process with the given filename 
        /// (case insensitive) if it is currently running.
        /// usually this runs with normal user privileges,
        /// so services and other higher-privileged apps
        /// will not be found.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>null if the process wasn't found</returns>
        public static Process GetRunningProcess(string fileName)
        {
            Process[] runningProcesses = Process.GetProcesses();

            foreach (Process proc in runningProcesses)
            {
                try
                {
                    if (proc.MainModule.FileName.ToLower().EndsWith(fileName.ToLower()))
                    {
                        return proc;
                    }
                }
                catch (Exception ex) // usually access error
                {
                    string n = "";
                    try { n = proc.ProcessName; }
                    catch { /* CANNOT RECOVER */ }
#if DEBUG
                    instance.Log<ERROR>("Error investigating process " + n + ".", ex);
#endif
                } 
            }

            return null;
        }

        #endregion
        #region cmd

        public static int ExecuteShellCommand(string command, int timeout = int.MaxValue, bool showOutput = false)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            // /C => exit after execution
            processInfo = new ProcessStartInfo("cmd.exe", "/C " + command);

            processInfo.CreateNoWindow = !showOutput;
            processInfo.UseShellExecute = false;
            //ProcessInfo.RedirectStandardOutput = true;
            process = Process.Start(processInfo);

            // Console.WriteLine(Process.StandardOutput.ReadToEnd());
            process.WaitForExit(timeout);

            exitCode = process.ExitCode;
            process.Close();

            return exitCode;
        }

        public static int ExecuteShellCommand(string command, out string output, int timeout = int.MaxValue, bool showOutput = false)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            // /C => exit after execution
            processInfo = new ProcessStartInfo("cmd.exe", "/C " + command);

            processInfo.CreateNoWindow = !showOutput;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            process = Process.Start(processInfo);
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(timeout);

            exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        #endregion
        #region bat

        public static int ExecuteBatchFile(string file, out string output)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = file;
            p.Start();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            int exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

        public static int ExecuteBatchFile(string file, bool showOutput = false)
        {
            Process p = new Process();
            p.StartInfo.CreateNoWindow = !showOutput;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = file;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            int exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

        #endregion

        #endregion
        #region http

        /// <summary>
        /// Check validity by trying to get the dns host entry.
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public static bool UrlIsValid(string hostNameOrAddress)
        {
            try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(hostNameOrAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the given url responds with a http OK status code meaning the web adress exists.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsUrlAvailable(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UseDefaultCredentials = true;
                request.Proxy.Credentials = request.Credentials;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch (WebException) { /* NO RESOLUTION REQUIRED */ }

            return false;
        }

        /// <summary>
        /// Check if the given uri responds with a http OK status code meaning the web adress exists.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsUrlAvailable(Uri uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.UseDefaultCredentials = true;
                request.Proxy.Credentials = request.Credentials;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch (WebException) { /* NO RESOLUTION REQUIRED */ }

            return false;
        }

        #endregion

        #endregion
    }
}
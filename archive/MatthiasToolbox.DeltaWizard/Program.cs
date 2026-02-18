using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using MatthiasToolbox.Delta;
using MatthiasToolbox.Delta.Delta;
using MatthiasToolbox.Delta.Utilities;
using MatthiasToolbox.Cryptography.Interfaces;

namespace MatthiasToolbox.DeltaWizard
{
    static class Program
    {
        #region classvar
        private static Form1 f1;
        private static Form2 f2;
        private static Form3 f3;
        private static Form4 f4;
        private static Form5 f5;
        
        private static int PageIndex = 1;
        private static int x = -1;
        private static int y = -1;
        
        private static int Action = 0;

        private static bool w2Multi;
        private static String[] w2Files = new string[8];
        private static bool w2Checkbox;
        
        private static int w3BlockSize = 0;
        private static int w3Digest = -1;
        private static int w3MinSize = 2048;
        private static int w3MaxSize = 2048;
        
        private static bool w3BlockSizeTest = true;
        private static int w3bsFrom = 0;
        private static int w3bsTo = 0;
        private static int w3bsStep = 0;
        private static bool w3Statistics = true;
        private static List<Statistics> myStatistics;
        private static Hashtable<long, double> cRatios;
        private static Hashtable<long, double> throughputs;
        private static Hashtable<long, int> digestErrors;
        private static double avgSpeed = 0;
        private static long avgSize = 0;
        private static double avgCR = 0;
        private static int skippedSpeedMeasurements;
        private static Hashtable<String, double> exts = new Hashtable<string, double>();
        private static Hashtable<String, int> anzs = new Hashtable<string, int>();

        private static List<Type> digestProviderTypes;
        private static IDigestProvider theDigest;
        
        private delegate void Action4Delegate();
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            String summary = "";
            String log = "";
            f1 = new Form1();
            f2 = new Form2();
            f3 = new Form3();

            loadDigestAlgorithms();
            
            while(PageIndex<100)
            {
                switch(PageIndex)
                {
                    case 1:
                        // select action
                        if (!Wizard1()) return;
                        break;
                    case 2:
                        // select file(s)
                        if (!Wizard2()) return;
                        
                        break;
                    case 3:
                        // config
                        if (!Wizard3()) return;
                        break;
                    case 4:
                        // confirmation
                        summary = Wizard4();
                        if (summary == "") return;
                        break;
                    case 5:
                        // display progress
                        f5 = new Form5();
                        f5.Left = x;
                        f5.Top = y;
                        f5.Show();
                        f5.AppendText(summary);
                        switch(Action)
                        {
                            case 1: // create delta(s)
                                Action1(f2.F1, f2.F2, w2Multi, f2.F3, f2.F4, w3BlockSize, w3MinSize);
                                break;
                            case 2: // create Hashset(s)
                                Action2(f2.F1, w2Multi, f2.F2, w3BlockSize, w3MinSize);
                                break;
                            case 3: // apply delta(s)
                                Action3(f2.F2, f2.F1, f2.F3, w2Multi, 0);
                                break;
                            case 4: // testing, validation, performance analysis, statistics
                                if(w3Statistics) myStatistics = new List<Statistics>();
                                Action4Delegate a4 = new Action4Delegate(Action4);
                                // todo: a4.BeginInvoke() , im callback: EndInvoke
                                Action4();
                                break;
                        } // switch

                        log = f5.GetText();
                        f5.Close();
                        f5 = new Form5();
                        f5.Left = x;
                        f5.Top = y;
                        f5.Release();
                        f5.SetText(log);
                        f5.ShowDialog();
                        PageIndex += 1;
                        break;
                    default:
                         // display results or success message
                        PageIndex = 100;
                        if(Action == 4 && (w3Statistics || w3BlockSizeTest))
                        {
                            FormStats ff = new FormStats();
                            if(w3Statistics)
                            {
                                ff.Init(summary, myStatistics.Count);
                                ff.SetStats(avgSpeed, avgSize, avgCR, exts);
                                ff.SetBStats(digestErrors, cRatios, throughputs);
                                avgSpeed = 0;
                                avgSize = 0;
                                avgCR = 0;
                                exts = new Hashtable<String, double>();
                            }
                            else
                            {
                                if(digestErrors[w3BlockSize] != null) ff.SetFails((int)digestErrors[w3BlockSize]);
                            }
                            ff.ShowDialog();
                        }
                        else
                        {
                            if (MessageBox.Show("Operation abgeschlossen. Wollen Sie einen weiteren Job erledigen?", "Success", MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Information) == DialogResult.Yes) PageIndex = 1;
                        }
                        break;
                }
            } // while

        } // void

        #region wizards
        
        private static bool Wizard1()
        {
            int i = f1.GetState();
            f1 = new Form1();
            f1.SetState(i);
            if(x == -1 && y == -1)
            {
                f1.StartPosition = FormStartPosition.CenterScreen;
            }
            else
            {
                f1.StartPosition = FormStartPosition.Manual;
                f1.Left = x;
                f1.Top = y;
            }
            Application.Run(f1);
            if (f1.DialogResult != DialogResult.OK) return false;
            PageIndex += 1;
            Action = f1.GetState();
            x = f1.Left;
            y = f1.Top;
            return true;
        } // bool
        
        private static bool Wizard2()
        {
            int i = f2.GetState(ref w2Files, ref w2Checkbox);
            f2 = new Form2();

            // TODO: remove in final
            //if (Action == 4)
            //{
            //    w2Files[0] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\v1";
            //    w2Files[1] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\v2";
            //    w2Files[2] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\d1";
            //    w2Files[3] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\d2";
            //    w2Files[4] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\h1";
            //    w2Files[5] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\h2";
            //    w2Files[6] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\r1";
            //    w2Files[7] = "C:\\Users\\Matthias\\Desktop\\Development\\test\\r2";
            //}

            f2.SetState(i, Action, w2Files, w2Checkbox);
            f2.Left = x;
            f2.Top = y;
            Application.Run(f2);
            f2.GetState(ref w2Files, ref w2Checkbox);
            x = f2.Left;
            y = f2.Top;
            switch (f2.DialogResult)
            {
                case DialogResult.OK:
                    PageIndex += 1;
                    if (Action == 3) PageIndex += 1;
                    w2Multi = (f2.GetState() == 1);
                    break;
                case DialogResult.Retry:
                    PageIndex -= 1;
                    break;
                default:
                    return false;
            } // switch
            return true;
        } // bool
        
        private static bool Wizard3()
        {
            f3 = new Form3();
            f3.Left = x;
            f3.Top = y;
            f3.SetState(Action, digestProviderTypes);
            Application.Run(f3);
            x = f3.Left;
            y = f3.Top;
            switch (f3.DialogResult)
            {
                case DialogResult.OK:
                    PageIndex += 1;
                    w3BlockSize = f3.GetBlocksize();
                    w3Statistics = f3.GetStatistics();
                    w3BlockSizeTest = f3.GetBlockSizeTest();
                    w3bsFrom = f3.GetBSfrom();
                    w3bsTo = f3.GetBSto();
                    w3bsStep = f3.GetBSstep();
                    w3MinSize = f3.GetMinSize();
                    w3MaxSize = f3.GetMaxSize()*1024*1024;
                    w3Digest = f3.GetDigest();
                    ConstructorInfo ci = digestProviderTypes[w3Digest].GetConstructor(Type.EmptyTypes);
                    theDigest = (IDigestProvider)ci.Invoke(null);
                    break;
                case DialogResult.Retry:
                    PageIndex -= 1;
                    break;
                default:
                    return false;
            } // switch
            return true;
        } // bool
        
        private static String Wizard4()
        {
            String dummy = "";
            String blockString = w3BlockSize.ToString() + " bytes.";
            switch (Action)
            {
                case 1:
                    if (w2Multi) dummy = "Erstelle Deltas";
                    else dummy = "Erstelle Delta";
                    dummy += " (Blockgröße " + blockString + ")";
                    dummy += Environment.NewLine;
                    dummy += "von <" + f2.F1 + "> nach <" + f2.F2 + ">";
                    dummy += Environment.NewLine;
                    if (w2Multi) dummy += "Deltas speichern nach ";
                    else dummy += "Delta speichern als ";
                    dummy += "<" + f2.F3 + ">";
                    dummy += Environment.NewLine;
                    if (f2.F4!="")
                    {
                        if (w2Multi) dummy += "Hashsets speichern nach ";
                        else dummy += "Hashset speichern als ";
                        dummy += "<" + f2.F4 + ">";
                    }
                    break;
                case 2:
                    if (w2Multi) dummy = "Erstelle Hashsets";
                    else dummy = "Erstelle Hashset";
                    dummy += " (Blockgröße " + blockString + ")";
                    dummy += Environment.NewLine;
                    dummy += "für <" + f2.F1 + ">";
                    dummy += Environment.NewLine;
                    if (w2Multi) dummy += "Hashsets speichern nach ";
                    else dummy += "Hashset speichern als ";
                    dummy += "<" + f2.F2 + ">";
                    break;
                case 3:
                    if (w2Multi) dummy = "Verwende Deltas ";
                    else dummy = "Verwende Delta ";
                    dummy += "(" + f2.F2 + ") mit Hashset (" + f2.F3 + ")";
                    dummy += Environment.NewLine;
                    dummy += "anwenden auf <" + f2.F1 + ">";
                    dummy += Environment.NewLine;
                    if (w2Multi) dummy += "Rekonstruierte Versionen speichern unter <";
                    else dummy += "Rekonstruierte Version speichern als <";
                    dummy += f2.F4 + ">";
                    break;
                case 4:
                    if (w2Multi) dummy = "Testlauf mit ";
                    else dummy = "Testlauf mit ";
                    if (w3BlockSizeTest) dummy += " verschiedenen Blockgrößen.";
                    else dummy += " Blockgröße " + blockString;
                    dummy += Environment.NewLine;
                    dummy += "Datendateien: <" + f2.F1 + ">, <" + f2.F2 + ">";
                    dummy += Environment.NewLine;
                    dummy += "Delta Dateien: <" + f2.F3 + ">, <" + f2.F4 + ">";
                    dummy += Environment.NewLine;
                    dummy += "Hashsets: <" + f2.F5 + ">, <" + f2.F6 + ">";
                    dummy += Environment.NewLine;
                    dummy += "Rekonstruierte Dateien: <" + f2.F7 + ">, <" + f2.F8 + ">";
                    break;
            } // switch
            f4 = new Form4();
            f4.setText(dummy);
            f4.Left = x;
            f4.Top = y;
            Application.Run(f4);
            x = f4.Left;
            y = f4.Top;
            switch (f4.DialogResult)
            {
                case DialogResult.OK:
                    PageIndex += 1;
                    break;
                case DialogResult.Retry:
                    PageIndex -= 1;
                    break;
                default:
                    return "";
            } // switch
            return dummy;
        } // bool
        
        #endregion
        #region actions
     
        /// <summary>
        /// create delta
        /// </summary>
        /// <param name="src">old file / folder</param>
        /// <param name="trg">new file / folder</param>
        /// <param name="multi">
        /// true...folder
        /// false...file
        /// </param>
        /// <param name="saveDelta">folder to save deltas to</param>
        /// <param name="saveHash">folder to save hashsets to, an empty string means the hashsets are discarded</param>
        /// <param name="block">block size for binary delta algorithm</param>
        /// <param name="minbin">minimum size for binary delta in bytes</param>
        /// <returns>true for success</returns>
        private static bool Action1(String src, String trg, bool multi, String saveDelta, String saveHash, int block, int minbin)
        {
            if(multi)
            {
                DirectoryInfo ds = new DirectoryInfo(src);
                foreach(FileInfo fi in ds.GetFiles())
                {
                    String fsrc = fi.FullName;
                    String ftrg = trg + "\\" + fi.Name;
                    String fdelta = saveDelta + "\\" + fi.Name + ".delta";
                    String fhash = saveHash + "\\" + fi.Name + ".hashset";
                    if (File.Exists(ftrg))
                        Action1(fsrc, ftrg, false, fdelta, fhash, block, minbin);
                    else
                    {
                        f5.AppendText("    <" + fsrc + "> was not processed because <" + ftrg + "> was not found.");
                        Application.DoEvents();
                    }
                    Application.DoEvents();
                }
                foreach(DirectoryInfo di in ds.GetDirectories())
                {
                    String dsrc = di.FullName;
                    String dtrg = trg + "\\" + di.Name;
                    String ddelta = saveDelta + "\\" + di.Name;
                    String dhash = saveHash + "\\" + di.Name;
                    try
                    {
                        if (!Directory.Exists(ddelta)) Directory.CreateDirectory(ddelta);
                        if (!Directory.Exists(dhash)) Directory.CreateDirectory(dhash);
                    }
                    catch (Exception ex)
                    {
                        f5.AppendText(ex.Message);
                        Application.DoEvents();
                        continue;
                    }
                    if (Directory.Exists(dtrg)) Action1(dsrc, dtrg, true, ddelta, dhash, block, minbin);
                    else
                    {
                        f5.AppendText("    <" + dsrc + "> was not processed because <" + dtrg + "> was not found.");
                        Application.DoEvents();
                    }
                }
            }
            else
            {
                if(w3BlockSizeTest)
                {
                    saveDelta = FileExtPrefix(saveDelta, block.ToString());
                    saveHash = FileExtPrefix(saveHash, block.ToString());
                }
                
                FileStream old = File.Open(src, FileMode.Open, FileAccess.Read);
                FileStream neu = File.Open(trg, FileMode.Open, FileAccess.Read);
                
                
                if (old.Length < minbin || neu.Length < minbin)
                {
                    f5.AppendText("<" + src + "> is too small -> skipped.");
                    Application.DoEvents();
                    old.Close();
                    neu.Close();
                    return true;
                }
                else if (old.Length > w3MaxSize || neu.Length > w3MaxSize)
                {
                    f5.AppendText("<" + src + "> is too big -> skipped.");
                    Application.DoEvents();
                    old.Close();
                    neu.Close();
                    return true;
                }
                
                f5.AppendText("processing <" + src + "> ...");
                Application.DoEvents();
                
                    if(block==0)
                    {
                        throw new Exception("something went wrong");
                    }
                    getBinD(old, neu, block, saveDelta, saveHash);
                
                
                old.Close();
                neu.Close();
                
            } // if
            return true;
        } // bool

        private static bool Action2(String src, bool multi, String saveHash, int block, int minbin)
        {
            if(multi)
            {
                DirectoryInfo ds = new DirectoryInfo(src);
                foreach(FileInfo fi in ds.GetFiles())
                {
                    String fsrc = fi.FullName;
                    String saveH = saveHash + "\\" + fi.Name + ".hashset";
                    Action2(fsrc, false, saveH, block, minbin);
                }
                foreach(DirectoryInfo di in ds.GetDirectories())
                {
                    String dhash = saveHash + "\\" + di.Name;
                    try
                    {
                        if(!Directory.Exists(dhash)) Directory.CreateDirectory(dhash);
                    }
                    catch (Exception ex)
                    {
                        f5.AppendText(ex.Message);
                        Application.DoEvents();
                        continue;
                    }
                    Action2(di.FullName, true, dhash, block, minbin);
                }
            }
            else
            {
                FileStream data = File.Open(src, FileMode.Open, FileAccess.Read);
                if (data.Length < minbin)
                {
                    f5.AppendText("<" + src + "> is too small -> skipped.");
                    Application.DoEvents();
                    return true;
                }
                else if (data.Length > w3MaxSize)
                {
                    f5.AppendText("<" + src + "> is too big -> skipped.");
                    Application.DoEvents();
                    return true;
                }
                
                f5.AppendText("processing <" + src + "> ...");
                Application.DoEvents();
                if (block == 0)
                {
                    block = Math.Max(((int) (((double) data.Length/1024)*0.1)), 2048);
                }
                getHashset(data, block, saveHash);
                data.Close();
            }
            return true;
        }

        private static bool Action3(String delta, String data, String target, bool multi, int bs)
        {
            if(multi)
            {
                DirectoryInfo ds = new DirectoryInfo(data);
                foreach(FileInfo fi in ds.GetFiles())
                {
                    String fdata = fi.FullName;
                    String fdelta = delta + "\\" + fi.Name + ".delta";
                    String bfd = fdelta;
                    if (w3BlockSizeTest) bfd = FileExtPrefix(bfd, bs.ToString());
                    String ftarget = target + "\\" + fi.Name;
                    if(File.Exists(bfd)) Action3(fdelta, fdata, ftarget, false, bs);
                    else
                    {
                        f5.AppendText("    <" + fdata + "> was not processed because <" + bfd + "> was not found.");
                        Application.DoEvents();
                    }
                }
                foreach(DirectoryInfo di in ds.GetDirectories())
                {
                    String ddelta = delta + "\\" + di.Name;
                    String ddata = di.FullName;
                    String dtarget = target + "\\" + di.Name;
                    try
                    {
                        if (!Directory.Exists(dtarget)) Directory.CreateDirectory(dtarget);
                    }
                    catch(Exception ex)
                    {
                        f5.AppendText(ex.Message);
                        Application.DoEvents();
                        continue;
                    }
                    if(Directory.Exists(ddelta)) Action3(ddelta, ddata, dtarget, true, bs);
                    else
                    {
                        f5.AppendText("    <" + ddata + "> was not processed because <" + ddelta + "> was not found.");
                        Application.DoEvents();
                    }
                }
            }
            else
            {
                if (w3BlockSizeTest)
                {
                    target = FileExtPrefix(target, bs.ToString());
                    delta = FileExtPrefix(delta, bs.ToString());
                }
                
                FileStream fdata = null;
                FileStream fdelta = null;
                FileStream fresult = null;
                try
                {
                    fdata = File.Open(data, FileMode.Open, FileAccess.Read);
                    fdelta = File.Open(delta, FileMode.Open, FileAccess.Read);
                    fresult = File.Open(target, FileMode.CreateNew, FileAccess.Write);
                }
                catch (Exception ex)
                {
                    f5.AppendText(ex.Message);
                    Application.DoEvents();
                    if (fdata != null) fdata.Close();
                    if (fdelta != null) fdelta.Close();
                    if (fresult != null) fresult.Close();
                    return false;
                }

                // TridgellDelta<String, ulong> td = new TridgellDelta<String, ulong>(w3BlockSize, theDigest, new MD4(), new Adler32());
                TridgellDelta td = new TridgellDelta(w3BlockSize, theDigest);
                
                f5.AppendText("processing <" + delta + ">...");
                Application.DoEvents();

                String dig;
                int newBlockSize = td.ApplyDelta(fdelta, fdata, fresult, out dig);
                fdelta.Close();
                fdata.Close();
                fresult.Close();
                
                f5.AppendText("verifying...");
                Application.DoEvents();
                
                FileStream fcheck = File.Open(target, FileMode.Open, FileAccess.Read);
                String ssha = theDigest.GetHash(fcheck, Encoding.UTF8.GetBytes("BluLogic"));
                fcheck.Close();
                if(ssha != dig) 
                {
                    f5.AppendText("    ### ERROR! " + theDigest.Name + " mismatch. ###");
                    Application.DoEvents();
                    if(digestErrors[newBlockSize] == null)
                    {
                        digestErrors[newBlockSize] = 1;
                    }
                    else
                    {
                        int i = (int) digestErrors[newBlockSize];
                        digestErrors[newBlockSize] = i + 1;
                    }
                }
            }
            return true;
        }
        
        private static void Action4()
        {
            digestErrors = new Hashtable<long, int>();

            // TODO: remove in final
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\d1"));
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\d2"));
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\h1"));
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\h2"));
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\r1"));
            //kill(new DirectoryInfo("C:\\Users\\Matthias\\Desktop\\Development\\test\\r2"));

            if(!w3BlockSizeTest)
            {
                skippedSpeedMeasurements = 0;
                Action1(f2.F1, f2.F2, w2Multi, f2.F3, f2.F5, w3BlockSize, w3MinSize);
                Action1(f2.F2, f2.F1, w2Multi, f2.F4, f2.F6, w3BlockSize, w3MinSize);
                Action3(f2.F3, f2.F1, f2.F8, w2Multi, 0);
                Action3(f2.F4, f2.F2, f2.F7, w2Multi, 0);
                if(w3Statistics)
                {
                    avgSpeed = avgSpeed / (myStatistics.Count - skippedSpeedMeasurements);
                    avgSize /= myStatistics.Count;
                    avgCR /= myStatistics.Count;
                }
            }
            else
            {
                int sumSkipped = 0;
                int fileAnz = 0;
                cRatios = new Hashtable<long, double>();
                throughputs = new Hashtable<long, double>();
                for(int i = w3bsFrom; i<= w3bsTo; i+=w3bsStep)
                {
                    skippedSpeedMeasurements = 0;
                    Action1(f2.F1, f2.F2, w2Multi, f2.F3, f2.F5, i, w3MinSize);
                    Action1(f2.F2, f2.F1, w2Multi, f2.F4, f2.F6, i, w3MinSize);
                    Action3(f2.F3, f2.F1, f2.F8, w2Multi, i);
                    Action3(f2.F4, f2.F2, f2.F7, w2Multi, i);
                    if (i == w3bsFrom) fileAnz = myStatistics.Count;
                    sumSkipped += skippedSpeedMeasurements;
                    if(fileAnz > 0) cRatios[i] = (double)cRatios[i] / fileAnz;
                    if(fileAnz - skippedSpeedMeasurements > 0) throughputs[i] = (double)cRatios[i] / (fileAnz - skippedSpeedMeasurements);
                }
                if (myStatistics.Count - sumSkipped > 0) avgSpeed = avgSpeed / (myStatistics.Count - sumSkipped);
                if (myStatistics.Count > 0) avgSize /= myStatistics.Count;
                if (myStatistics.Count > 0) avgCR /= myStatistics.Count;
            }

            String[] sext = new string[exts.Values.Count];
            
            int ii = 0;
            foreach (string s in exts.Keys)
            {
                sext[ii] = s;
                ii += 1;
            }
            
            foreach(String s in sext)
            {
                exts[s] = (double)exts[s]/(int)anzs[s];
            }
            
        }
        
        #endregion
        #region helpers
        
        private static void getHashset(FileStream data, int blockSize, String toFile)
        {
            TridgellDelta td = new TridgellDelta(blockSize, theDigest);
            Stream output = File.Open(toFile, FileMode.Create, FileAccess.Write);
            td.GetBlockSignatures(data, output);
            output.Close();
        }
        
        private static void getBinD(Stream src, FileStream trg, int blockSize, String saveDelta, String saveHash)
        {
            Statistics s = new Statistics();
            Stopwatch s1 = new Stopwatch();
            String tmpHashset;
            
            BlockedHashset<String, ulong> bhs;
            
            TridgellDelta td = new TridgellDelta(blockSize, theDigest);
            
            if (saveHash == "") tmpHashset = getTempFile();
            else tmpHashset = saveHash;

            Stream fs1 = new FileStream(tmpHashset, FileMode.Create);
            s1.Start();
            bhs = td.GetBlockSignatures(src, fs1);
            s1.Stop();
            if (w3Statistics)
            {
                s.HashTime = (s1.ElapsedMilliseconds / 1000.0);
                s.HashSize = fs1.Length;
                s1.Reset();
            }
            fs1.Close();
            
            Stream fs = new FileStream(saveDelta, FileMode.Create);
            
            s1.Start();
            List<BinaryCommand> D = td.GetDelta(trg, bhs, ref fs);
            s1.Stop();
            if(saveHash == "") File.Delete(tmpHashset);
            if (w3Statistics)
            {
                s.BlockSize = blockSize;
                s.DeltaTime = (s1.ElapsedMilliseconds / 1000.0);
                s.DeltaSize = fs.Length;
                s.FileSize1 = src.Length;
                s.FileSize2 = trg.Length;
                myStatistics.Add(s);
                s1.Reset();
                if (w3BlockSizeTest)
                {
                    if (cRatios[blockSize] == null)
                    {
                        cRatios[blockSize] = s.CRdelta;
                    }
                    else
                    {
                        double d = (double)cRatios[blockSize];
                        cRatios[blockSize] = s.CRdelta + d;
                    }
                }
                if(s.DeltaTime + s.HashTime != 0)
                {
                    if(avgSpeed==0)
                    {
                        avgSpeed = ((((double)s.combinedSize + (double)s.FileSize1 + (double)s.FileSize2) / (1024 * 1024))) /
                            (s.DeltaTime + s.HashTime);
                    }
                    else
                    {
                        avgSpeed += ((((double)s.combinedSize + (double)s.FileSize1 + (double)s.FileSize2) / (1024 * 1024))) / 
                            (s.DeltaTime + s.HashTime);
                    }
                    if(w3BlockSizeTest)
                    {
                        if(throughputs[blockSize] == null)
                        {
                            throughputs[blockSize] = ((((double)s.combinedSize + (double)s.FileSize1 + (double)s.FileSize2) / (1024 * 1024))) /
                                (s.DeltaTime + s.HashTime);
                        }
                        else
                        {
                            double d = (double)throughputs[blockSize];
                            throughputs[blockSize] = d + ((((double)s.combinedSize + (double)s.FileSize1 + (double)s.FileSize2) / (1024 * 1024))) /
                                (s.DeltaTime + s.HashTime);
                        }
                    }
                }
                else
                {
                    skippedSpeedMeasurements += 1;
                }
                avgSize += s.FileSize;
                avgCR += s.CRdelta;

                String ext = getFileExtension(trg.Name).ToLower();
                if (ext == "") ext = "<none>";
               
                if(exts[ext] == null)
                {
                    exts[ext] = s.CRdelta;
                    anzs[ext] = 1;
                }
                else
                {
                    exts[ext] = (double)exts[ext] + s.CRdelta;
                    anzs[ext] = (int)anzs[ext] + 1;
                }
            }
            src.Close();
            trg.Close();
            fs.Close();
        }

        //private static void kill(DirectoryInfo di)
        //{
        //    foreach (FileInfo fi in di.GetFiles())
        //    {
        //        try { fi.Delete(); }
        //        catch { }
        //    }
        //}
        
        private static String getFileExtension(String file)
        {
            String fileonly = file;
            if (file.Contains("\\"))
            {
                int j = file.LastIndexOf("\\");
                fileonly = file.Substring(j + 1);
            }
            if (!fileonly.Contains(".")) return "";
            int i = file.LastIndexOf('.');
            return file.Substring(i + 1);
        }

        private static String FileExtPrefix(String file, String prefix)
        {
            String ext = getFileExtension(file);
            if (ext == "") return file + "." + prefix;
            int i = file.LastIndexOf('.') + 1;
            return file.Substring(0, i) + prefix + "." + ext;
        }
        
        private static void loadDigestAlgorithms()
        {
            Type[] allTypes = typeof(CommandType).Assembly.GetExportedTypes();
            digestProviderTypes = new List<Type>();
            foreach (Type t in allTypes)
            {
                if (t.GetInterface(typeof(IDigestProvider).FullName) != null)
                {
                    digestProviderTypes.Add(t);
                }
            }
        }
        
        private static String getTempFile()
        {
            return Path.GetTempFileName();
        }
        #endregion
    } // class
} // namespace


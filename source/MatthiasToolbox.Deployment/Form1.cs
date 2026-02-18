using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Utilities;
using MatthiasToolbox.Logging.Loggers;

namespace MatthiasToolbox.Deployment
{
    public partial class Form1 : Form
    {
        #region cvar

        private string conf;
        private int fileCounter;
        private DirectoryInfo srcBase;
        private Dictionary<DirectoryInfo, DirectoryInfo> x86dirs = new Dictionary<DirectoryInfo, DirectoryInfo>();
        private Dictionary<DirectoryInfo, DirectoryInfo> x64dirs = new Dictionary<DirectoryInfo, DirectoryInfo>();
        private Dictionary<DirectoryInfo, FileInfo> x86files = new Dictionary<DirectoryInfo, FileInfo>();
        private Dictionary<DirectoryInfo, FileInfo> x64files = new Dictionary<DirectoryInfo, FileInfo>();

        #endregion
        #region ctor

        public Form1()
        {
            InitializeComponent();

#if DEBUG
            conf = "Debug";
            this.Text = "CAUTION: DEBUG VERSION";
#else
            conf = "Release";
#endif
            Logger.Add(new RichTextBoxLogger(richTextBox1));
            Logger.Add<STATUS>(new StatusBarLogger(toolStripStatusLabel1));

            string myPath = SystemTools.ExecutablePath();
            string basePath = myPath.Substring(0, myPath.LastIndexOf("\\bin"));
            basePath = basePath.Substring(0, basePath.LastIndexOf("\\"));
            srcBase = new DirectoryInfo(basePath);

            this.Log<STATUS>("Sources located at " + basePath + ".");
        }

        #endregion
        #region hand

        #region form

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (DirectoryInfo dir in srcBase.EnumerateDirectories())
            {
                if (dir.Name == ".svn") continue;
                bool include = dir.Name != "MatthiasToolbox.Deployment";
                if (HasRelease("x86", dir)) checkedListBox1.Items.Add(dir, include);
                if (HasRelease("x64", dir)) checkedListBox2.Items.Add(dir, include);
            }
            textBoxDate.Text = DateTime.Now.ToString("yyyyMMdd");
            textBoxTargetPath.Text = srcBase.Parent.FullName + "\\binaries";
        }

        #endregion
        #region buttons

        private void buttonOK_Click(object sender, EventArgs e)
        {
            fileCounter = 0;
            int i = 0;
            int executables = 0;
            int libraries = 0;
            int configfiles = 0;
            int xmlfiles = 0;
            int pdbfiles = 0;

            string folder = textBoxDate.Text + ".v" + textBoxMajor.Text + "." + textBoxMinor.Text + "." + textBoxTag.Text + "." + textBoxStage.Text;
            DirectoryInfo x86targetDirectory = new DirectoryInfo(Path.Combine(textBoxTargetPath.Text, folder, "x86"));
            DirectoryInfo x64targetDirectory = new DirectoryInfo(Path.Combine(textBoxTargetPath.Text, folder, "x64"));
            List<FileInfo> files86 = new List<FileInfo>();
            List<DirectoryInfo> directories86 = new List<DirectoryInfo>();
            List<FileInfo> files64 = new List<FileInfo>();
            List<DirectoryInfo> directories64 = new List<DirectoryInfo>();
            FileInfo tmpFile;
            DirectoryInfo tmpFolder;

            #region prepare x86

            foreach (object o in checkedListBox1.CheckedItems)
            {
                tmpFile = x86files[o as DirectoryInfo];
                if (tmpFile.Extension.ToLower() == ".exe") executables++;
                else libraries++;
                files86.Add(tmpFile);
                if (checkBoxXML1.Checked && HasFile("xml", ref tmpFile))
                {
                    files86.Add(tmpFile);
                    xmlfiles++;
                }
                if (checkBoxPDB1.Checked && HasFile("pdb", ref tmpFile))
                {
                    files86.Add(tmpFile);
                    pdbfiles++;
                }
                if (checkBoxConfig1.Checked && HasFile("config", ref tmpFile))
                {
                    files86.Add(tmpFile);
                    configfiles++;
                }
                tmpFolder = x86dirs[o as DirectoryInfo];
                if (checkBoxResources1.Checked && tmpFolder.HasSubFolder("Resources")) directories86.Add(tmpFolder.GetSubFolder("Resources"));
            }

            #endregion
            #region prepare x64

            foreach (object o in checkedListBox2.CheckedItems)
            {
                tmpFile = x64files[o as DirectoryInfo];
                if (tmpFile.Extension.ToLower() == ".exe") executables++;
                else libraries++;
                files64.Add(tmpFile);
                if (checkBoxXML2.Checked && HasFile("xml", ref tmpFile))
                {
                    files64.Add(tmpFile);
                    xmlfiles++;
                }
                if (checkBoxPDB2.Checked && HasFile("pdb", ref tmpFile))
                {
                    files64.Add(tmpFile);
                    pdbfiles++;
                }
                if (checkBoxConfig2.Checked && HasFile("config", ref tmpFile))
                {
                    files64.Add(tmpFile);
                    configfiles++;
                }
                tmpFolder = x64dirs[o as DirectoryInfo];
                if (checkBoxResources2.Checked && tmpFolder.HasSubFolder("Resources")) directories64.Add(tmpFolder.GetSubFolder("Resources"));
            }

            #endregion

            if (MessageBox.Show("Copy " + executables.ToString() + " executables, " + 
                libraries.ToString() + " libraries, " + 
                configfiles.ToString() + " configurations, " +
                xmlfiles.ToString() + " xml files and " + 
                pdbfiles.ToString() + "pdb files?", "Check", MessageBoxButtons.OKCancel) != System.Windows.Forms.DialogResult.OK) return;

            #region copy x86

            if (files86.Count > 0 || directories86.Count > 0) x86targetDirectory.Create();

            foreach (FileInfo file in files86)
            {
                string target = Path.Combine(x86targetDirectory.FullName, file.Name);
                if (File.Exists(target)) this.Log<INFO>("Overwriting file: [" + file.FullName + "] => [" + target + "]");
                else this.Log<INFO>("Copying file: [" + file.FullName + "] => [" + target + "]");
                file.CopyTo(target, true);
                fileCounter++;
                i++;
            }

            foreach (DirectoryInfo dir in directories86)
            {
                string target = Path.Combine(x86targetDirectory.FullName, "Resources");
                this.Log<INFO>("Copying folder: [" + dir.FullName + "] => [" + target + "]");
                dir.CopyTo(target, callBack: CopyCallback);
            }

            #endregion
            #region copy x64

            if (files64.Count > 0 || directories64.Count > 0) x64targetDirectory.Create();

            foreach (FileInfo file in files64)
            {
                string target = Path.Combine(x64targetDirectory.FullName, file.Name);
                if (File.Exists(target)) this.Log<INFO>("Overwriting file: [" + file.FullName + "] => [" + target + "]");
                else this.Log<INFO>("Copying file: [" + file.FullName + "] => [" + target + "]");
                file.CopyTo(target, true);
                fileCounter++;
                i++;
            }

            foreach (DirectoryInfo dir in directories64)
            {
                string target = Path.Combine(x64targetDirectory.FullName, "Resources");
                this.Log<INFO>("Copying folder: [" + dir.FullName + "] => [" + target + "]");
                dir.CopyTo(target, callBack: CopyCallback);
            }

            #endregion

            this.Log<STATUS>(fileCounter.ToString() + " files copied (" + i.ToString() + " of " + 
                (executables + libraries + configfiles + xmlfiles + pdbfiles).ToString() + " release files).");
        }
        
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = textBoxTargetPath.Text;
            fb.ShowNewFolderButton = true;
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBoxTargetPath.Text = fb.SelectedPath;
        }

        #region selection

        private void buttonSelect1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++) checkedListBox1.SetItemChecked(i, true);
        }

        private void buttonUnselect1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++) checkedListBox1.SetItemChecked(i, false);
        }

        private void buttonInvert1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++) checkedListBox1.SetItemChecked(i, !checkedListBox1.GetItemChecked(i));
        }

        private void buttonSelect2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox2.Items.Count; i++) checkedListBox2.SetItemChecked(i, true);
        }

        private void buttonUnselect2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox2.Items.Count; i++) checkedListBox2.SetItemChecked(i, false);
        }

        private void buttonInvert2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox2.Items.Count; i++) checkedListBox2.SetItemChecked(i, !checkedListBox2.GetItemChecked(i));
        }

        #endregion
        #region io

        private void buttonSave1_Click(object sender, EventArgs e)
        {
            SaveSelection("temp.selection");
        }

        private void buttonLoad1_Click(object sender, EventArgs e)
        {
            LoadSelection("temp.selection");
        }

        private void buttonSave2_Click(object sender, EventArgs e)
        {
            SaveSelection("temp.selection");
        }

        private void buttonLoad2_Click(object sender, EventArgs e)
        {
            LoadSelection("temp.selection");
        }

        #endregion

        #endregion
        #region io

        private void SaveSelection(string targetFile)
        {
            List<string> x86selectionNames = new List<string>();
            List<bool> x86selectionChecks = new List<bool>();

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                DirectoryInfo dir = checkedListBox1.Items[i] as DirectoryInfo;
                // x86selection[dir.Name] = checkedListBox1.GetItemChecked(i);
                x86selectionNames.Add(dir.Name);
                x86selectionChecks.Add(checkedListBox1.GetItemChecked(i));
            }

            List<string> x64selectionNames = new List<string>();
            List<bool> x64selectionChecks = new List<bool>();

            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                DirectoryInfo dir = checkedListBox2.Items[i] as DirectoryInfo;
                x64selectionNames.Add(dir.Name);
                x64selectionChecks.Add(checkedListBox2.GetItemChecked(i));
            }

            XmlSerializer s = new XmlSerializer(typeof(List<string>));
            TextWriter w1 = new StreamWriter(targetFile + ".x86n");
            s.Serialize(w1, x86selectionNames);
            w1.Close();

            TextWriter w2 = new StreamWriter(targetFile + ".x64n");
            s.Serialize(w2, x64selectionNames);
            w2.Close();

            XmlSerializer s2 = new XmlSerializer(typeof(List<bool>));
            TextWriter w3 = new StreamWriter(targetFile + ".x86c");
            s2.Serialize(w3, x86selectionChecks);
            w3.Close();

            TextWriter w4 = new StreamWriter(targetFile + ".x64c");
            s2.Serialize(w4, x64selectionChecks);
            w4.Close();
        }

        private void LoadSelection(string sourceFile)
        {
            List<string> x86selectionNames = new List<string>();
            List<bool> x86selectionChecks = new List<bool>();

            List<string> x64selectionNames = new List<string>();
            List<bool> x64selectionChecks = new List<bool>();

            Dictionary<string, bool> x86selection = new Dictionary<string, bool>();
            Dictionary<string, bool> x64selection = new Dictionary<string, bool>();

            XmlSerializer s = new XmlSerializer(typeof(List<string>));
            TextReader r1 = new StreamReader(sourceFile + ".x86n");
            x86selectionNames = (List<string>)s.Deserialize(r1);
            r1.Close();

            TextReader r2 = new StreamReader(sourceFile + ".x64n");
            x64selectionNames = (List<string>)s.Deserialize(r2);
            r2.Close();

            XmlSerializer s2 = new XmlSerializer(typeof(List<bool>));
            TextReader r3 = new StreamReader(sourceFile + ".x86c");
            x86selectionChecks = (List<bool>)s2.Deserialize(r3);
            r3.Close();

            TextReader r4 = new StreamReader(sourceFile + ".x64c");
            x64selectionChecks = (List<bool>)s2.Deserialize(r4);
            r4.Close();

            for (int i = 0; i < x86selectionChecks.Count; i++)
                x86selection[x86selectionNames[i]] = x86selectionChecks[i];

            for (int i = 0; i < x64selectionChecks.Count; i++)
                x64selection[x64selectionNames[i]] = x64selectionChecks[i];

            foreach (KeyValuePair<string, bool> kvp in x86selection)
            {
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    DirectoryInfo dir = checkedListBox1.Items[i] as DirectoryInfo;
                    if (dir.Name == kvp.Key) checkedListBox1.SetItemChecked(i, kvp.Value);
                }
            }

            foreach (KeyValuePair<string, bool> kvp in x64selection)
            {
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                {
                    DirectoryInfo dir = checkedListBox2.Items[i] as DirectoryInfo;
                    if (dir.Name == kvp.Key) checkedListBox2.SetItemChecked(i, kvp.Value);
                }
            }
        }

        #endregion

        #endregion
        #region impl

        private bool HasFile(string extension, ref FileInfo file)
        {
            string test = file.FullNameWithoutExtension() + "." + extension;
            if (!File.Exists(test))
            {
                if (extension.ToLower() == "config")
                {
                    test = "app." + extension;
                    if (!File.Exists(test)) return false;
                } else return false;
            }
            file = new FileInfo(test);
            return true;
        }

        private bool HasRelease(string platform, DirectoryInfo dir)
        {
            if (dir.GetDirectories("bin").Length > 0)
            {
                DirectoryInfo bin = dir.GetDirectories("bin")[0];
                if (bin.GetDirectories(platform).Length > 0)
                {
                    DirectoryInfo platformFolder = bin.GetDirectories(platform)[0];
                    if (platformFolder.GetDirectories(conf).Length > 0)
                    {
                        DirectoryInfo release = platformFolder.GetDirectories(conf)[0];
                        FileInfo mainFile;

                        #region get file

                        if (release.GetFiles(dir.Name + ".exe").Length > 0)
                        {
                            mainFile = release.GetFiles(dir.Name + ".exe")[0];
                        }
                        else if (release.GetFiles(dir.Name + ".dll").Length > 0)
                        {
                            mainFile = release.GetFiles(dir.Name + ".dll")[0];
                        }
                        else
                        {
                            this.Log<INFO>("No " + platform + " dll or exe file found for " + dir.Name + ".");
                            return false;
                        }

                        #endregion
                        #region check date

                        if (mainFile.LastWriteTime.Date != DateTime.Now.Date)
                        {
                            this.Log<WARN>("No current " + platform + " release found for " + dir.Name + ".");
                            return false;
                        }

                        #endregion
                        #region store data

                        if (platform == "x86")
                        {
                            x86files[dir] = mainFile;
                            x86dirs[dir] = release;
                        }
                        else
                        {
                            x64files[dir] = mainFile;
                            x64dirs[dir] = release;
                        }

                        #endregion

                        return true;
                    }
                }
            }
            this.Log<WARN>("No " + platform + " release found for " + dir.Name + ".");
            return false;
        }

        #endregion
        #region util

        private void CopyCallback(string src, string trg, bool dir, bool over)
        {
            if (!dir) fileCounter++;
            if (!dir && !over) this.Log<INFO>("Copying file: [" + src + "] => [" + trg + "]");
            else if (!dir) this.Log<INFO>("Overwriting file: [" + src + "] => [" + trg + "]");
            if (dir && over) this.Log<INFO>("Merged folders: [" + src + "] => [" + trg + "]");
        }

        #endregion
    }
}
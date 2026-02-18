using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using MatthiasToolbox.Cryptography.Enumerations;
using MatthiasToolbox.Cryptography.Utilities;
using MatthiasToolbox.Utilities;

namespace MatthiasToolbox.HashUtil
{
    public partial class Form1 : Form
    {
        #region classvar

        HashEncoding Coding = HashEncoding.Base64;
        HashMode Mode = HashMode.Default;
        
        string cpu;
        string hd;
        string mac;

        byte[] IV;
        private string userIV = "";
        private string userFile = "";

        #endregion
        #region constructor
        
        public Form1()
        {
            InitializeComponent();

            cpu = HardwareInfo.GetCPUId();
            hd = HardwareInfo.GetVolumeSerial("C");
            mac = HardwareInfo.GetMACAddress();
        }

        #endregion
        #region main events

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                FirstRun();

                if (!String.IsNullOrEmpty(userIV)) textBox2.Text = userIV;

                comboBox2.SelectedIndex = 0;
                comboBox3.SelectedIndex = 0;
                
                comboBox1.Items.Add("Prozessor ID (0) = " + cpu);
                comboBox1.Items.Add("MAC Adresse (0) = " + mac);
                comboBox1.Items.Add("Festplatten ID (C:) = " + hd);
                comboBox1.Items.Add("Custom IV = " + userIV);

                comboBox1.Text = textBox2.Text;

                textBox1.Text = Environment.GetCommandLineArgs()[0].Replace(".vshost", "");

                Show();
                button1_Click(sender, e);

#if DEBUG
#else
                if (textBoxH1.Text.Substring(3, 1) != "r") Close();
#endif
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private bool CheckMySelf()
        {
#if DEBUG
            return true;
#else
            return ("r" == getH1(Environment.GetCommandLineArgs()[0].Replace(".vshost", "")).Substring(3, 1));
#endif

        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            IV = Encoding.UTF8.GetBytes(textBox2.Text);

            if (File.Exists(textBox1.Text))
            {
                Clear();
                label11.Visible = false;
                button1.Visible = true;
            }
            else
            {
                label11.Visible = true;
                button1.Visible = false;

                if (textBox1.Text == "")
                {
                    Clear();
                    return;
                }

                textBoxH1.Enabled = true;
                textBoxH2.Enabled = true;
                textBoxH3.Enabled = true;
                textBoxH4.Enabled = true;
                textBoxH5.Enabled = true;
                textBoxH6.Enabled = true;
                textBoxH7.Enabled = true;
                textBoxH8.Enabled = true;
                textBoxH9.Enabled = true;
                if (checkBox1.Checked)
                    textBoxH1.Text = HashProvider.Get(
                                        Algorithm.Adler32,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                if (checkBox2.Checked) 
                    textBoxH2.Text = HashProvider.Get(
                                        Algorithm.CRC32,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                if (checkBox3.Checked) 
                    textBoxH3.Text = HashProvider.Get(
                                        Algorithm.MD4,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                if (checkBox4.Checked) 
                    textBoxH4.Text = HashProvider.Get(
                                        Algorithm.MD5,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                if (checkBox5.Checked) 
                    textBoxH5.Text = HashProvider.Get(
                                         Algorithm.SHA1,
                                         Coding, FromString(textBox1.Text),
                                         Mode, IV);
                if (checkBox6.Checked) 
                    textBoxH6.Text = HashProvider.Get(
                                         Algorithm.RIPEMED160,
                                         Coding, FromString(textBox1.Text),
                                         Mode, IV);
                if (checkBox7.Checked) 
                    textBoxH7.Text = HashProvider.Get(
                                         Algorithm.SHA256,
                                         Coding, FromString(textBox1.Text),
                                         Mode, IV);
                if (checkBox8.Checked) 
                    textBoxH8.Text = HashProvider.Get(
                                         Algorithm.SHA384,
                                         Coding, FromString(textBox1.Text),
                                         Mode, IV);
                if (checkBox9.Checked) 
                    textBoxH9.Text = HashProvider.Get(
                                         Algorithm.SHA512,
                                         Coding, FromString(textBox1.Text),
                                         Mode, IV);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if(!button1.Visible) textBox1_TextChanged(sender, e);
            else GreyOut();
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.CheckFileExists = true;
            if(of.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = of.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IV = Encoding.UTF8.GetBytes(textBox2.Text);
            try
            {
                FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);

                makeH1(fs);
                makeH2(fs);
                makeH3(fs);
                makeH4(fs);
                makeH5(fs);
                makeH6(fs);
                makeH7(fs);
                makeH8(fs);
                makeH9(fs);

                textBoxH1.Enabled = true;
                textBoxH2.Enabled = true;
                textBoxH3.Enabled = true;
                textBoxH4.Enabled = true;
                textBoxH5.Enabled = true;
                textBoxH6.Enabled = true;
                textBoxH7.Enabled = true;
                textBoxH8.Enabled = true;
                textBoxH9.Enabled = true;
                
                fs.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error processing file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0: // cpu
                    textBox2.Text = cpu;
                    break;
                case 1: // mac
                    textBox2.Text = mac;
                    break;
                case 2: // hd
                    textBox2.Text = hd;
                    break;
                case 3: // user
                    textBox2.Text = userIV;
                    break;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox2.SelectedIndex)
            {
                case 0:     // base64
                    if (Coding == HashEncoding.Base64) return;
                    Coding = HashEncoding.Base64;
                    if (!button1.Visible) textBox1_TextChanged(sender, e);
                    else GreyOut();
                    break;
                case 1:     // hex
                    if (Coding == HashEncoding.Hexadecimal) return;
                    Coding = HashEncoding.Hexadecimal;
                    if (!button1.Visible) textBox1_TextChanged(sender, e);
                    else GreyOut();
                    break;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = (comboBox3.SelectedIndex != 0);
            textBox2.Enabled = comboBox1.Enabled;

            switch (comboBox3.SelectedIndex)
            {
                case 0:     // default
                    if (Mode == HashMode.Default) return;
                    Mode = HashMode.Default;
                    if (!button1.Visible) textBox1_TextChanged(sender, e);
                    else GreyOut();
                    break;
                case 1:     // iv
                    if (Mode == HashMode.IV) return;
                    Mode = HashMode.IV;
                    if (!button1.Visible) textBox1_TextChanged(sender, e);
                    else GreyOut();
                    break;
                case 2:     // rmx
                    if (Mode == HashMode.RMX) return;
                    Mode = HashMode.RMX;
                    if (!button1.Visible) textBox1_TextChanged(sender, e);
                    else GreyOut();
                    break;
            }
        }
        
        private void Form1_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Info i = new Info();
            i.ShowDialog();
            e.Cancel = true;
        }
        
        #endregion
        #region loading & saving

        private void SaveHash(String data, String ext, bool sfv)
        {
            SaveFileDialog sf = new SaveFileDialog();
            if (sfv) sf.Filter =
                 "Simple File Verification Format (*.sfv)|*.sfv|XML File Verification Format (*.xfv)|*.xfv|Plain Text (*.crc32)|*.crc32|Plain Text (*.*)|*.*";
            else sf.Filter =
                "XML File Verification Format (*.xfv)|*.xfv|Plain Text (*." + ext + ")|*." + ext + "|Plain Text (*.*)|*.*";
            if(sf.ShowDialog() == DialogResult.OK)
            {
                string xt = "";
                if (sf.FileName.LastIndexOf('.') > 0 && sf.FileName.LastIndexOf('.') < sf.FileName.Length)
                    xt = sf.FileName.Substring(sf.FileName.LastIndexOf('.') + 1);
                switch(xt)
                {
                    case("sfv"):
                        string fn = textBox1.Text.Substring(textBox1.Text.LastIndexOf('\\') + 1);
                        try
                        {
                            FileStream fs = new FileStream(sf.FileName, FileMode.CreateNew, FileAccess.Write);
                            
                            // write filename
                            byte[] bdata = Encoding.UTF8.GetBytes(fn + " ");
                            fs.Write(bdata, 0, bdata.Length);
                            
                            // write crc32
                            bdata = Encoding.UTF8.GetBytes(data);
                            fs.Write(bdata, 0, bdata.Length);
                            
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error writing to file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    case("xfv"):
                        FileVerification fv = new FileVerification();
                        int del = textBox1.Text.LastIndexOf('\\');
                        fv.algorithm = ext;
                        fv.iv = Encoding.UTF8.GetString(IV);
                        fv.path = textBox1.Text.Substring(0, del);
                        fv.filename = textBox1.Text.Substring(del + 1);
                        fv.hash = data;
                        
                        XmlSerializer xs = new XmlSerializer(typeof(FileVerification));
                        try
                        {
                            XmlWriterSettings ws = new XmlWriterSettings();
                            ws.Indent = true;
                            XmlWriter xw = XmlWriter.Create(sf.FileName, ws);
                            xs.Serialize(xw, fv);
                            xw.Flush();
                            xw.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error writing to file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                    default:
                        try
                        {
                            FileStream fs = new FileStream(sf.FileName, FileMode.CreateNew, FileAccess.Write);
                            byte[] bdata = Encoding.UTF8.GetBytes(data);
                            fs.Write(bdata, 0, bdata.Length);
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error writing to file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;
                }
            }
        }

        private bool SaveText(String data, String file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
                byte[] bdata = Encoding.UTF8.GetBytes(data);
                fs.Write(bdata, 0, bdata.Length);
                fs.Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error writing to file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private String LoadText(String file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                byte[] bdata = new byte[fs.Length];
                fs.Read(bdata, 0, (int)fs.Length);
                fs.Close();
                return Encoding.UTF8.GetString(bdata);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error writing to file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        #endregion
        #region save buttons

        private void button3_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(userIV) && userIV != textBox2.Text)
            {
                // overwrite warning
                if (MessageBox.Show("old = (" + userIV +
                                    ") \nnew = (" + textBox2.Text +
                                    ")\n\nOverwrite User IV?",
                                    "User IV Überschreiben?",
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Question) != DialogResult.OK)
                    return;
            }
            if (SaveText(textBox2.Text, userFile))
            {
                userIV = textBox2.Text;
                comboBox1.Items[3] = "Custom IV = " + userIV;
                MessageBox.Show("User IV saved.", "IV saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH1.Text, "adler32", false);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH2.Text, "crc32", true);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH3.Text, "md4", false);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH4.Text, "md5", false);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH5.Text, "sha1", false);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH6.Text, "ripemed160", false);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH7.Text, "sha256", false);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH8.Text, "sha384", false);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            SaveHash(textBoxH9.Text, "sha512", false);
        }

        #endregion
        #region checkboxes

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            button4.Enabled = checkBox1.Checked;
            button21.Enabled = checkBox1.Checked;
            if (!checkBox1.Checked)
            {
                textBoxH1.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH1.Text = HashProvider.Get(
                                        Algorithm.Adler32,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV); 
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH1(fs);
            fs.Close();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            button5.Enabled = checkBox2.Checked;
            button20.Enabled = checkBox2.Checked;
            if (!checkBox2.Checked)
            {
                textBoxH2.Text = "";
                return;
            }
            else if (!File.Exists(textBox1.Text))
            {
                textBoxH2.Text = HashProvider.Get(
                                        Algorithm.CRC32,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH2(fs);
            fs.Close();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            button6.Enabled = checkBox3.Checked;
            button19.Enabled = checkBox3.Checked;
            if (!checkBox3.Checked)
            {
                textBoxH3.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH3.Text = HashProvider.Get(
                                        Algorithm.MD4,
                                        Coding, FromString(textBox1.Text),
                                        Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH3(fs);
            fs.Close();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            button7.Enabled = checkBox4.Checked;
            button18.Enabled = checkBox4.Checked;
            if (!checkBox4.Checked)
            {
                textBoxH4.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH4.Text = HashProvider.Get(
                    Algorithm.MD5,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH4(fs);
            fs.Close();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            button8.Enabled = checkBox5.Checked;
            button17.Enabled = checkBox5.Checked;
            if (!checkBox5.Checked)
            {
                textBoxH5.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH5.Text = HashProvider.Get(
                    Algorithm.SHA1,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH5(fs);
            fs.Close();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            button9.Enabled = checkBox6.Checked;
            button16.Enabled = checkBox6.Checked;
            if (!checkBox6.Checked)
            {
                textBoxH6.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH6.Text = HashProvider.Get(
                    Algorithm.RIPEMED160,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH6(fs);
            fs.Close();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            button10.Enabled = checkBox7.Checked;
            button15.Enabled = checkBox7.Checked;
            if (!checkBox7.Checked)
            {
                textBoxH7.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH7.Text = HashProvider.Get(
                    Algorithm.SHA256,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH7(fs);
            fs.Close();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            button11.Enabled = checkBox8.Checked;
            button14.Enabled = checkBox8.Checked;
            if (!checkBox8.Checked)
            {
                textBoxH8.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH8.Text = HashProvider.Get(
                    Algorithm.SHA384,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH8(fs);
            fs.Close();
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            button12.Enabled = checkBox9.Checked;
            button13.Enabled = checkBox9.Checked;
            if (!checkBox9.Checked)
            {
                textBoxH9.Text = "";
                return;
            }
            else if(!File.Exists(textBox1.Text))
            {
                textBoxH9.Text = HashProvider.Get(
                    Algorithm.SHA512,
                    Coding, FromString(textBox1.Text),
                    Mode, IV);
                return;
            }
            FileStream fs = new FileStream(textBox1.Text, FileMode.Open, FileAccess.Read);
            makeH9(fs);
            fs.Close();
        }
        
        #endregion
        #region clipboard buttons

        private void button21_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH1.Text)) return;
            Clipboard.SetDataObject(textBoxH1.Text, true);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH2.Text)) return;
            Clipboard.SetDataObject(textBoxH2.Text, true);
        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH3.Text)) return;
            Clipboard.SetDataObject(textBoxH3.Text, true);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH4.Text)) return;
            Clipboard.SetDataObject(textBoxH4.Text, true);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH5.Text)) return;
            Clipboard.SetDataObject(textBoxH5.Text, true);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH6.Text)) return;
            Clipboard.SetDataObject(textBoxH6.Text, true);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH7.Text)) return;
            Clipboard.SetDataObject(textBoxH7.Text, true);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH8.Text)) return;
            Clipboard.SetDataObject(textBoxH8.Text, true);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxH9.Text)) return;
            Clipboard.SetDataObject(textBoxH9.Text, true);
        }

        #endregion
        #region utilities

        private Stream FromString(String data)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(data));
        }
        
        private void Clear()
        {
            textBoxH1.Text = "";
            textBoxH2.Text = "";
            textBoxH3.Text = "";
            textBoxH4.Text = "";
            textBoxH5.Text = "";
            textBoxH6.Text = "";
            textBoxH7.Text = "";
            textBoxH8.Text = "";
            textBoxH9.Text = "";
        }

        private void GreyOut()
        {
            textBoxH1.Enabled = false;
            textBoxH2.Enabled = false;
            textBoxH3.Enabled = false;
            textBoxH4.Enabled = false;
            textBoxH5.Enabled = false;
            textBoxH6.Enabled = false;
            textBoxH7.Enabled = false;
            textBoxH8.Enabled = false;
            textBoxH9.Enabled = false;
        }
        
        private void FirstRun()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BlueLogic"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BlueLogic");
            }

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BlueLogic\\HashProvider"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BlueLogic\\HashProvider");
            }

            userFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BlueLogic\\HashProvider\\user.iv";
            if (File.Exists(userFile)) userIV = LoadText(userFile);

            if (!CheckMySelf())
            {
                MessageBox.Show("Application corrupted!\n" + getH1(Environment.GetCommandLineArgs()[0].Replace(".vshost", "")));
                Close();
            }
        }
        
        private void makeH1(FileStream fs)
        {
            if (checkBox1.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH1.Text = HashProvider.Get(
                                        Algorithm.Adler32,
                                        Coding, fs, Mode, IV);
            }
        }

        private string getH1(String file)
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.Begin);
            string result = HashProvider.Get(
                                        Algorithm.Adler32,
                                        Coding, fs, Mode, IV);
            fs.Close();
            return result;
        }
        
        private void makeH2(FileStream fs)
        {
            if (checkBox2.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH2.Text = HashProvider.Get(
                                         Algorithm.CRC32,
                                         Coding, fs,
                                         Mode, IV);
            }
        }

        private void makeH3(FileStream fs)
        {
            if (checkBox3.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH3.Text = HashProvider.Get(
                                        Algorithm.MD4,
                                        Coding, fs, Mode, IV);
            }
        }

        private void makeH4(FileStream fs)
        {
            if (checkBox4.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH4.Text = HashProvider.Get(
                                        Algorithm.MD5,
                                        Coding, fs, Mode, IV);
            }
        }

        private void makeH5(FileStream fs)
        {
            if (checkBox5.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH5.Text = HashProvider.Get(
                                        Algorithm.SHA1,
                                        Coding, fs, Mode, IV);
            }
        }

        private void makeH6(FileStream fs)
        {
            if (checkBox6.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH6.Text = HashProvider.Get(
                                        Algorithm.RIPEMED160,
                                        Coding, fs, Mode, IV);
            }
        }

        private void makeH7(FileStream fs)
        {
            if (checkBox7.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH7.Text = HashProvider.Get(
                                         Algorithm.SHA256,
                                         Coding, fs,
                                         Mode, IV);
            }
        }

        private void makeH8(FileStream fs)
        {
            if (checkBox8.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH8.Text = HashProvider.Get(
                                        Algorithm.SHA384,
                                        Coding, fs, Mode, IV);
            }
        }

        private void makeH9(FileStream fs)
        {
            if (checkBox9.Checked)
            {
                fs.Seek(0, SeekOrigin.Begin);
                textBoxH9.Text = HashProvider.Get(
                                        Algorithm.SHA512,
                                        Coding, fs, Mode, IV);
            }
        }
        
        #endregion
    } // class
} // namespace
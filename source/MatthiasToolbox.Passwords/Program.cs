using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using MatthiasToolbox.Passwords.Utilities;

// TODO: Encryption Key Rotation + Generation PKS5?
// TODO: clear pwd from mem!

namespace MatthiasToolbox.Passwords
{
    /// <summary>
    /// the main application entry point
    /// </summary>
    static class Program
    {
        #region classvars
        
        internal static SecureString passUser;      // the user password
        // internal static SecureString passCode;   // the code password

        internal static UserData userData;          // user accounts data
        internal static Config confData;            // program configuration data
        
        // program configuration file
        internal static String confFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords\\Secret Words.conf";

        internal static XmlReaderSettings setReader; // xml reader settings
        internal static XmlWriterSettings setWriter; // xml writer settings
        
        private static MainWindow mainForm;         // main application window

        private enum UserResult { OK, WrongPassword, FileNotFound, Error }
        
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool ok;
            Mutex m = new Mutex(true, "MatthiasToolbox.Passwords", out ok);
            if (!ok)
            {
                MessageBox.Show("Another instance is already running.");
                return;
            }

            if(!Initialize()) return;
            
            if (FirstRun()) // Setup
            {
                Application.Run(new FirstStartInfo()); // show usage warning
                if ((new FirstStart()).ShowDialog() != DialogResult.OK) return;
            } 
            else // get password
            {
                Login f2 = new Login();
                UserResult result;
                int wrong = 0;
                do
                {
                    if (f2.ShowDialog() != DialogResult.OK) return;
                    result = getUser();
                    if(result == UserResult.WrongPassword)
                    {
                        wrong += 1;
                        if(wrong > 4) 
                        {
                            MessageBox.Show("Wrong again. This program will be disabled for a while now.", "Wrong Password",
                                        MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            Thread.Sleep(1000 * 200);
                            return;
                        }
                        else if(wrong > 3)
                        {
                            MessageBox.Show("Wrong Password. You can try one more time after a certain delay.", "Wrong Password",
                                        MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            Thread.Sleep(30000);    
                        }
                        else if(wrong > 1)
                        {
                            MessageBox.Show("Wrong Password. You can try again after a short delay.", "Wrong Password",
                                        MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            Thread.Sleep(5000 * wrong);
                        }
                        else
                        {
                            MessageBox.Show("Wrong Password. Please try again.", "Wrong Password",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else if(result == UserResult.FileNotFound)
                    {
                        MessageBox.Show("User file <" + userData.UserFile + "> not found!", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else if(result == UserResult.Error)
                    {
                        return;
                    }
                    
                } while (result != UserResult.OK);
            } 
            
            // TODO: self test
            
            // load data
            userData.Load();
            
            // open
            mainForm = new MainWindow();
            LoadSettings();
            Application.Run(mainForm);
            SaveSettings(); 
            GC.KeepAlive(m);
        }

        #region initialization

        private static byte[] getIV()
        {
            if(!File.Exists(confFile.Replace(' ',' ')))
            {
                confData.IV = Crypto.GetRSAIV();
                confData.Save();
            }

            string usb = "";
            if (confData.Protection != "")
            {
                while (usb == "")
                {
                    usb = HardwareInfo.GetVolumeSerialByHash(confData.Protection);
                    if (usb == "")
                    {
                        if (MessageBox.Show("Protection device not found!", "Error", MessageBoxButtons.RetryCancel,
                                        MessageBoxIcon.Warning) == DialogResult.Cancel) return null;
                    }
                }
            }
            Crypto.IVP = Encoding.UTF8.GetBytes(usb);
            
            string hd = "";
            if(confData.Drive != "" && confData.IVmode == Config.IVMethod.hd)
            {
                while(hd=="")
                {
                    hd = HardwareInfo.GetVolumeSerialByHash(confData.Drive);
                    if (hd == "")
                    {
                        if(MessageBox.Show("IV device not found!", "Error", MessageBoxButtons.RetryCancel,
                                        MessageBoxIcon.Warning) == DialogResult.Cancel) return null;
                    }
                }
            }
            
            string mac = "";
            if (confData.Adapter != "" && confData.IVmode == Config.IVMethod.mac)
            {
                while(mac=="")
                {
                    mac = HardwareInfo.GetMACAddress(confData.Adapter);
                    if (mac == "")
                    {
                        if(MessageBox.Show("IV device not found!", "Error", MessageBoxButtons.RetryCancel,
                                        MessageBoxIcon.Warning) == DialogResult.Cancel) return null;
                    }
                }
            }
            
            switch (confData.IVmode)
            {
                case Config.IVMethod.file:
                    return confData.IV;
                case Config.IVMethod.cpu:
                    return Pad(Encoding.UTF8.GetBytes(HardwareInfo.GetCPUId()), confData.IV);
                case Config.IVMethod.mac:
                    return Pad(Encoding.UTF8.GetBytes(mac), confData.IV);
                case Config.IVMethod.hd:
                    return Pad(Encoding.UTF8.GetBytes(hd), confData.IV);
                default:
                    return confData.IV;
            }
        }
        
        private static byte[] Pad(byte[] data, byte[] canvas)
        {
            byte[] result = (byte[])canvas.Clone();
            int max = Math.Min(data.Length, canvas.Length);
            for(int i = 0; i < max; i++)
            {
                result[i] = (byte)(((int)canvas[i] + (int)data[i]) % 255);
            }
            return result;
        }

        private static bool Initialize()
        {
            setReader = new XmlReaderSettings();
            setReader.ConformanceLevel = ConformanceLevel.Fragment;
            setReader.IgnoreWhitespace = true;

            setWriter = new XmlWriterSettings();
            setWriter.CloseOutput = false;
            setWriter.ConformanceLevel = ConformanceLevel.Fragment;
            setWriter.Indent = true;

            // TODO: debugger warning
#if Release
            if(System.Diagnostics.Debugger.IsAttached)
            {
                
            }
#endif
            
            userData = new UserData();
            confData = Config.Load();
            
            Crypto.IV = getIV();
            if (Crypto.IV == null) return false;
            return true;
        }

        /// <summary>
        /// creates BlueLogic.SecretWords folder and checks for conf file
        /// </summary>
        /// <returns>
        /// true if no conf file exists
        /// </returns>
        private static bool FirstRun()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox");
            }

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords");
            }

            return !File.Exists(confFile);
        }

        private static UserResult getUser()
        {
            try
            {
                FileStream fs = File.OpenRead(confFile);
                byte[] btemp = new byte[fs.Length];
                fs.Read(btemp, 0, (int)fs.Length);
                SecureString userPath;
                try
                {
                    userPath = Crypto.DecryptRSA(Encoding.ASCII.GetString(btemp), passUser);
                }
                catch(Exception ex)
                {
                    return UserResult.WrongPassword;
                }
                userData.UserFile = Crypto.DecryptSecureString(userPath);
                if(File.Exists(userData.UserFile)) return UserResult.OK;
                return UserResult.FileNotFound;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load user data - " + ex.Message, "Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return UserResult.Error;
            }
        }

        private static void LoadSettings()
        {
            String ini = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords\\gui.ini";
            if (!File.Exists(ini)) return;

            FileStream load = new FileStream(ini, FileMode.Open, FileAccess.Read);
            XmlReader reader = XmlReader.Create(load, setReader);
            XmlSerializer intSerializer = new XmlSerializer(typeof(int));
            // XmlSerializer stringSerializer = new XmlSerializer(typeof(string));
            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();
            if (wasEmpty) return;
            if (reader.NodeType == XmlNodeType.None) return;

            reader.ReadStartElement("X1");
            mainForm.Left = (int)intSerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadStartElement("X2");
            mainForm.Width = (int)intSerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadStartElement("Y1");
            mainForm.Top = (int)intSerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadStartElement("Y2");
            mainForm.Height = (int)intSerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.Close();
            load.Flush();
            load.Close();
            
            if(confData.closeToTray)
            {
                mainForm.MaximizeBox = false;
                mainForm.MinimizeBox = false;
            }
        }

        public static void SaveSettings()
        {
            if(mainForm.Left < 0 || mainForm.Top < 0) return;
            String ini = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords\\gui.ini";
            FileStream save = new FileStream(ini, FileMode.Create, FileAccess.Write);
            XmlWriter writer = XmlWriter.Create(save, setWriter);
            XmlSerializer intSerializer = new XmlSerializer(typeof(int));
            // XmlSerializer stringSerializer = new XmlSerializer(typeof(string));

            writer.WriteStartElement("X1");
            intSerializer.Serialize(writer, mainForm.Left);
            writer.WriteEndElement();

            writer.WriteStartElement("X2");
            intSerializer.Serialize(writer, mainForm.Width);
            writer.WriteEndElement();

            writer.WriteStartElement("Y1");
            intSerializer.Serialize(writer, mainForm.Top);
            writer.WriteEndElement();

            writer.WriteStartElement("Y2");
            intSerializer.Serialize(writer, mainForm.Height);
            writer.WriteEndElement();

            writer.Flush();
            writer.Close();
            save.Flush();
            save.Close();
        }
        
        #endregion
    }
}
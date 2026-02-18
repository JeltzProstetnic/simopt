using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace MatthiasToolbox.Passwords
{
    public class Config
    {
        public enum IVMethod
        {
            file, mac, cpu, hd
        }
        
        public byte[] IV;
        public IVMethod IVmode = IVMethod.file;
        public bool purgePwd = true;
        public bool closeToTray = true;
        public int trayAutoClose = 0;
        public int idleAutoClose = 0;
        public String Drive = "";
        public String Adapter = "";
        public String Protection = "";
        
        public bool Save()
        {
            try
            {
                if (!File.Exists(Program.confFile.Replace(' ', ' '))) createConfFile();
                XmlWriter writer = XmlWriter.Create(Program.confFile.Replace(' ', ' '), Program.setWriter);
                XmlSerializer confSerializer = new XmlSerializer(typeof(Config));

                writer.WriteStartElement("options");

                confSerializer.Serialize(writer, this);

                writer.WriteEndElement();
                writer.Close();
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Config File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        public static Config Load()
        {
            Config result = new Config();
            String file = Program.confFile.Replace(' ', ' ');
            if (!File.Exists(file)) return result;
            
            try
            {
                XmlReader reader = XmlReader.Create(file, Program.setReader);
                XmlSerializer confSerializer = new XmlSerializer(typeof(Config));

                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty) return result;
                if (reader.NodeType == XmlNodeType.None) return result;

                if (reader.NodeType != XmlNodeType.None && reader.NodeType != XmlNodeType.EndElement)
                {

                    reader.ReadStartElement("options");

                    result = (Config)confSerializer.Deserialize(reader);

                    reader.ReadEndElement();
                }
                reader.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Config File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return result;
        }
        
        private void createConfFile()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox");
            }

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MatthiasToolbox\\Passwords");
            }
            
            File.Create(Program.confFile.Replace(' ', ' ')).Close();
        }
    }
}

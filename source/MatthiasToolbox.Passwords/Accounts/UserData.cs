using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace MatthiasToolbox.Passwords
{
    public class UserData
    {
        private String userFile;
        public Hashtable<AccountData> myData = new Hashtable<AccountData>(100);

        public String UserFile
        {
            get { return userFile; }
            set { userFile = value; }
        }
        
        public void Load()
        {
            try
            {
                FileStream File = new FileStream(userFile, FileMode.Open, FileAccess.Read);
                XmlReader myReader = XmlReader.Create(File, Program.setReader);
                myReader.Read();
                myData.ReadXml(myReader);
                myReader.Close();
                File.Flush();
                File.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load " + userFile, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool Save()
        {
            try
            {
                FileStream File = new FileStream(userFile, FileMode.Open, FileAccess.Write);
                XmlWriter myWriter = XmlWriter.Create(File, Program.setWriter);
                myWriter.WriteStartElement("MatthiasToolbox.Passwords.AccountData");
                myData.WriteXml(myWriter);
                myWriter.WriteEndElement();
                myWriter.Flush();
                myWriter.Close();
                File.Flush();
                File.Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save " + userFile, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool Backup()
        {
            KillBackup();
            try
            {
                File.Copy(userFile, userFile + ".bak");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + userFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        public void KillBackup()
        {
            try
            {
                File.Delete(userFile + ".bak");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + userFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        public bool Restore()
        {
            try
            {
                File.Delete(userFile);
                File.Copy(userFile + ".bak", userFile);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup " + userFile + ".bak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        public bool Add(AccountData newAccount)
        {
            if(myData[newAccount.AccountName] != null) return false;
            myData.Add(newAccount.AccountName, newAccount);
            return true;
        }
        
        public void Delete(String AccountName)
        {
            myData.Remove(AccountName);
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    public partial class FirstStart : Form
    {
        private String msg = "";
        
        public FirstStart()
        {
            InitializeComponent();
            textBox1.PasswordChar = (char)0x25CF;
            textBox2.PasswordChar = (char)0x25CF;
        }

        public void setFile(String file)
        {
            textBox3.Text = file;
        }
        
        private void validate()
        {
            bool equal = Crypto.EqualSecureStrings(textBox1.SecureText, textBox2.SecureText);
            
            PasswordSecurity ps = new PasswordSecurity();
            int security = ps.EvalPwdStrength(Crypto.DecryptSecureString(textBox1.SecureText));
            switch(security)
            {
                case 1:
                    textBox1.BackColor = Color.Green;
                    break;
                case 2:
                    textBox1.BackColor = Color.Yellow;
                    break;
                case 3:
                    textBox1.BackColor = Color.Orange;
                    break;
                case 4:
                    textBox1.BackColor = Color.Red;
                    break;
            }

            if (!equal)
            {
                textBox2.BackColor = Color.Red;
            }
            else
            {
                textBox2.BackColor = textBox1.BackColor;
            }
            
            bool pwdOK = (security < 4);

            bool fileNameOK = (!String.IsNullOrEmpty(textBox3.Text));
            
            button1.Enabled = (equal && pwdOK && fileNameOK);
        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!checkUserFile())
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!checkConfFile())
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // TODO: generate self validation and IV

            Program.userData.UserFile = textBox3.Text;
            
            Program.passUser = textBox1.SecureText;
            
            textBox2.SecureText.Clear();
            
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = "Alle Dateien (*.*)|*.*";
            sf.CheckPathExists = true;
            
            if(sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox3.Text = sf.FileName;
            }
            
            validate();
        }
        
        private bool checkConfFile()
        {
            bool confFileOK = false;
            try
            {
                FileStream test = File.OpenWrite(Program.confFile);
                if (test.CanWrite)
                {
                    SecureString t = Crypto.EncryptSecureString(textBox3.Text);
                    byte[] b = Encoding.UTF8.GetBytes(Crypto.EncryptRSA(t, textBox1.SecureText));
                    test.Write(b, 0, b.Length);
                    test.Flush();
                    test.Close();
                    confFileOK = true;
                }
                else
                {
                    msg = "Unable to write config data";
                    test.Close();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            return confFileOK;
        }
        
        private bool checkUserFile()
        {
            bool userFileOK = true;
            if (!File.Exists(textBox3.Text))
            {
                try
                {
                    FileStream test = File.OpenWrite(textBox3.Text);
                    if (test.CanWrite)
                    {
                        test.Close();
                    }
                    else
                    {
                        msg = "Unable to write user data.";
                        test.Close();
                        userFileOK = false;
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    userFileOK = false;
                }
            }
            return userFileOK;
        }
        
    }
}
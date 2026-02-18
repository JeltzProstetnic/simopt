using System;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
            textBox1.PasswordChar = (char)0x25CF;
            AcceptButton = button1;
            CancelButton = button2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Program.passUser = textBox1.SecureText;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        
    }
}
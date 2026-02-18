using System;
using System.Drawing;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    public partial class AddAccount : Form
    {
        public AccountData NewAccount;
        private bool edit = false;
        private bool check = false;
        private PasswordSecurity ps = new PasswordSecurity();
        private Color[] colors = { Color.LightSteelBlue, Color.LightGreen, Color.Yellow, Color.Orange, Color.Red };
        
        public AddAccount()
        {
            InitializeComponent();
            
            secureTextBox1.PasswordChar = (char)0x25CF;
            secureTextBox2.PasswordChar = (char)0x25CF;
            secureTextBox3.PasswordChar = (char)0x25CF;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!edit && Program.userData.myData[textBox1.Text] != null)
            {
                MessageBox.Show("Ein Account mit diesem Namen existiert bereits. Bitte wählen Sie einen anderen Namen!");
            }
            else
            {
                button3_Click(sender, e);
                button7_Click(sender, e);
                button2_Click(sender, e);
                
                NewAccount.AccountName = textBox1.Text;
                NewAccount.Field1S = secureTextBox1.SecureText;
                NewAccount.Field2S = secureTextBox2.SecureText;
                NewAccount.Field3S = secureTextBox3.SecureText;
                NewAccount.Comments = textBox2.Text;
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }

        private void validate()
        {
            button1.Enabled = (!String.IsNullOrEmpty(textBox1.Text));
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(button3.BackColor == Color.CornflowerBlue)
            {
                button3.BackColor = Color.RoyalBlue;
                textBox3.Visible = true;
                textBox3.Text = Crypto.DecryptSecureString(secureTextBox1.SecureText);
            }
            else
            {
                button3.BackColor = Color.CornflowerBlue;
                textBox3.Visible = false;
                secureTextBox1.SecureText = Crypto.EncryptSecureString(textBox3.Text);
                textBox3.Text = "";
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (button7.BackColor == Color.CornflowerBlue)
            {
                button7.BackColor = Color.RoyalBlue;
                textBox4.Visible = true;
                textBox4.Text = Crypto.DecryptSecureString(secureTextBox2.SecureText);
            }
            else
            {
                button7.BackColor = Color.CornflowerBlue;
                textBox4.Visible = false;
                secureTextBox2.SecureText = Crypto.EncryptSecureString(textBox4.Text);
                textBox4.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.BackColor == Color.CornflowerBlue)
            {
                button2.BackColor = Color.RoyalBlue;
                textBox5.Visible = true;
                textBox5.Text = Crypto.DecryptSecureString(secureTextBox3.SecureText);
            }
            else
            {
                button2.BackColor = Color.CornflowerBlue;
                textBox5.Visible = false;
                secureTextBox3.SecureText = Crypto.EncryptSecureString(textBox5.Text);
                textBox5.Text = "";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            PasswordGenerator pg = new PasswordGenerator();
            pg.Result = textBox3.Text;
            if(pg.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = pg.Result;
                secureTextBox1.SecureText = Crypto.EncryptSecureString(pg.Result);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            PasswordGenerator pg = new PasswordGenerator();
            pg.Result = textBox4.Text;
            if (pg.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = pg.Result;
                secureTextBox2.SecureText = Crypto.EncryptSecureString(pg.Result);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            PasswordGenerator pg = new PasswordGenerator();
            pg.Result = textBox5.Text;
            if (pg.ShowDialog() == DialogResult.OK)
            {
                textBox5.Text = pg.Result;
                secureTextBox3.SecureText = Crypto.EncryptSecureString(pg.Result);
            }
        }

        private void AddAccount_Load(object sender, EventArgs e)
        {
            if(NewAccount != null && NewAccount.AccountName != "")
            {
                textBox1.Text = NewAccount.AccountName;
                textBox2.Text = NewAccount.Comments;
                secureTextBox1.SecureText = NewAccount.Field1S;
                secureTextBox2.SecureText = NewAccount.Field2S;
                secureTextBox3.SecureText = NewAccount.Field3S;
                pictureBox2.Visible = true;
                edit = true;
            }
            check = true;
            Show();
            Application.DoEvents();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (!check) return;
            if (textBox4.Text.Length > 0) textBox4.BackColor = colors[ps.EvalPwdStrength(textBox4.Text)];
            else textBox4.BackColor = colors[0];
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (!check) return;
            if (textBox5.Text.Length > 0) textBox5.BackColor = colors[ps.EvalPwdStrength(textBox5.Text)];
            else textBox5.BackColor = colors[0];
        }

        private void secureTextBox2_TextChanged(object sender, EventArgs e)
        {
            if (!check) return;
            if (secureTextBox2.SecureText.Length > 0) secureTextBox2.BackColor = colors[ps.EvalPwdStrength(Crypto.DecryptSecureString(secureTextBox2.SecureText))];
            else secureTextBox2.BackColor = colors[0];
        }

        private void secureTextBox3_TextChanged(object sender, EventArgs e)
        {
            if (!check) return;
            if (secureTextBox3.SecureText.Length > 0) secureTextBox3.BackColor = colors[ps.EvalPwdStrength(Crypto.DecryptSecureString(secureTextBox3.SecureText))];
            else secureTextBox3.BackColor = colors[0];
        }
    }
}
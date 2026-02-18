using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    public partial class PasswordGenerator : Form
    {
        PasswordSecurity ps;
        
        public PasswordGenerator()
        {
            InitializeComponent();
            
            ps = new PasswordSecurity();
        }

        public String Result
        {
            get { return textBoxResult.Text; }
            set { textBoxResult.Text = value; }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            textBoxResult.BackColor = Color.White;
            if(checkBox1.Checked || checkBox2.Checked || checkBox3.Checked || checkBox4.Checked)
            {
                String result;
                String alphabet = "";
                if (checkBox1.Checked) alphabet += textBox5.Text;
                if (checkBox2.Checked) alphabet += textBox4.Text;
                if (checkBox3.Checked) alphabet += textBox3.Text;
                if (checkBox3.Checked) alphabet += textBox3.Text; // double for numbers are less in number
                if (checkBox4.Checked) alphabet += textBox1.Text;
                if (checkBox4.Checked && textBox1.Text.Length < 14) alphabet += textBox1.Text; // see above

                int ic = 0;
                int max = alphabet.Length - 1;
                Random r = new Random();
                
                do
                {
                    result = "";
                    for (int i = 1; i <= numericUpDown1.Value; i++)
                    {
                        result += alphabet[r.Next(0, max)];
                    }
                    ic += 1;
                    if(ic > 100)
                    {
                        MessageBox.Show("Unable to find a strong enough password with these settings.", "Sorry");
                        break;
                    }
                    textBoxResult.Text = result;
                    Application.DoEvents();
                } while (ps.EvalPwdStrength(result)>numericUpDown2.Value);
                
                int security = ps.EvalPwdStrength(result);
                switch (security)
                {
                    case 1:
                        textBoxResult.BackColor = Color.LightGreen;
                        break;
                    case 2:
                        textBoxResult.BackColor = Color.Yellow;
                        break;
                    case 3:
                        textBoxResult.BackColor = Color.Orange;
                        break;
                    case 4:
                        textBoxResult.BackColor = Color.Red;
                        break;
                }
                
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int security = ps.EvalPwdStrength(textBoxResult.Text);
            switch (security)
            {
                case 1:
                    textBoxResult.BackColor = Color.LightGreen;
                    break;
                case 2:
                    textBoxResult.BackColor = Color.Yellow;
                    break;
                case 3:
                    textBoxResult.BackColor = Color.Orange;
                    break;
                case 4:
                    textBoxResult.BackColor = Color.Red;
                    break;
            }
        }

        private void PasswordGenerator_Load(object sender, EventArgs e)
        {
            Show();
            Application.DoEvents();
            if(textBoxResult.Text == "") button2_Click(sender, e);
        }
    }
}
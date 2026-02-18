using System;
using System.IO;
using System.Security;
using System.Windows.Forms;
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    public partial class MainWindow : Form
    {
        private bool reallyClose = false;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Restore()
        {
            if(Program.confData.purgePwd)
            {
                SecureString old = Program.passUser.Copy();
                Login l = new Login();
                if(l.ShowDialog() != DialogResult.OK || !Crypto.EqualSecureStrings(old, Program.passUser))
                {
                    reallyClose = true;
                    Close();
                    return;
                }
            }
            Visible = true;
            timer1.Start();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }
        
        private void Form3_Load(object sender, EventArgs e)
        {
            foreach (object o in Program.userData.myData.Keys)
            {
                listBox1.Items.Add((String) o);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddAccount addForm = new AddAccount();
            addForm.NewAccount = new AccountData();
            
            if(addForm.ShowDialog()==System.Windows.Forms.DialogResult.OK)
            {
                Program.userData.Add(addForm.NewAccount);
                Program.userData.Save();
                listBox1.Items.Add(addForm.NewAccount.AccountName);
            }
            
            addForm.Dispose();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reallyClose = true;
            Close();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteSelectedAccount();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "http://www.bluelogic.at";
            p.Start();
        }

        private void optionsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Options o = new Options();
            if(o.ShowDialog()== DialogResult.Retry)
            {
                reallyClose = true;
                Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            Clipboard.SetDataObject(((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field1, false);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            Clipboard.SetDataObject(((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field2, false);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            Clipboard.SetDataObject(((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field3, false);
        }

        private void button7_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            textBox4.Text = ((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field1;
        }

        private void button7_MouseUp(object sender, MouseEventArgs e)
        {
            textBox4.Text = "Account User Name";
        }

        private void button6_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            textBox3.Text = ((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field2;
        }

        private void button6_MouseUp(object sender, MouseEventArgs e)
        {
            textBox3.Text = "Password 1";
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            textBox2.Text = ((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Field3;
        }

        private void button5_MouseUp(object sender, MouseEventArgs e)
        {
            textBox2.Text = "Password 2";
        }

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            textBox1.Text = ((AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()]).Comments;
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            textBox1.Text = "Comments";
        }

        private void Form3_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = false;
                timer1.Stop();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(5000);
            }
        }

        private bool inside = true;
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            bool ins = (MousePosition.X >= Left && MousePosition.Y >= Top && MousePosition.X <= Left + Width && MousePosition.Y <= Top + Height);
            if(ins!=inside)
            {
                inside = ins;
                if (inside) Opacity = 0.9;
                else Opacity = 0.6;
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Info i = new Info();
            i.ShowDialog();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            deleteSelectedAccount();
        }

        private void deleteSelectedAccount()
        {
            if (listBox1.SelectedIndex < 0) return;
            if(MessageBox.Show("Are you sure you want to delete account \""+listBox1.SelectedItem.ToString()+"\"?", "Warnung",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Program.userData.Delete(listBox1.SelectedItem.ToString());
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                Program.userData.Save();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            
            AddAccount addForm = new AddAccount();
            
            addForm.NewAccount = (AccountData)Program.userData.myData[listBox1.SelectedItem.ToString()];

            if (addForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Program.userData.Delete(listBox1.SelectedItem.ToString());
                Program.userData.Add(addForm.NewAccount);
                Program.userData.Save();
                listBox1.Items[listBox1.SelectedIndex] = addForm.NewAccount.AccountName;
            }

            addForm.Dispose();
            
            
        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FirstStart f = new FirstStart();
            String tmp = Program.userData.UserFile;
            f.setFile(tmp);
            if (f.ShowDialog() == DialogResult.OK)
            {
                Program.userData.Save();
                if (tmp != Program.userData.UserFile)
                {
                    if (MessageBox.Show("Delete <" + tmp + "> ?", "Old userfile", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            File.Delete(tmp);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Fehler beim Löschen", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.confData.closeToTray && !reallyClose)
            {
                e.Cancel = true;
                Program.SaveSettings();
                WindowState = FormWindowState.Minimized;
            }
        }

        private void notifyIcon1_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Restore();
            }
        }

        private void quitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            reallyClose = true;
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Restore();
        }

        
    }
}
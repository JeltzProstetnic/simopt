using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class Form3 : Form
    {
        public int BlockSize = 2048;
        private int minBin;
        private int maxBin;
        private int Action;
        private int from = 0;
        private int too = 0;
        private int step = 0;
        private Color err = Color.Red;
        private List<Type> digestTypes;
        public int selectedType;
        
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(maxBin > 50)
            {
                if(MessageBox.Show("Warnung: Diese Version von SDelta wurde für kleine bis mittlere Dokumente ausgelegt. Das Programm wird möglicherweise nicht mehr reagieren, wenn eine zu große Datei verarbeitet werden soll.","Warnung",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning)==DialogResult.Cancel)
                    return;
            }
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void SetState(int mainfn, List<Type> digest)
        {
            Action = mainfn;
            digestTypes = digest;
            foreach(Type t in digest)
            {
                comboBox1.Items.Add(t.FullName);
            }
            comboBox1.SelectedItem = "BlueLogic.SDelta.Core.Cryptography.SHA256";
            validate();
        }

        private void validate()
        {
            int i;
            button1.Enabled = true;
            
            selectedType = -1;
            for(int j = 0; j < digestTypes.Count; j++ )
            {
                if((String)comboBox1.SelectedItem == digestTypes[j].FullName) selectedType = j;
            }
            if(selectedType==-1) button1.Enabled = false;
            
            if (!int.TryParse(textBox1.Text, out i) || int.Parse(textBox1.Text) < 1)
            {
                if(!checkBox7.Checked) button1.Enabled = false;
                textBox1.ForeColor = err;
            }
            else if (int.Parse(textBox1.Text) < 21)
            {
                textBox1.ForeColor = err;
                BlockSize = i;
            }
            else if (int.Parse(textBox1.Text) < 100)
            {
                textBox1.ForeColor = err;
                BlockSize = i;
            }
            else
            {
                textBox1.ForeColor = Color.Black;
                BlockSize = i;
            }

            if(!int.TryParse(textBox6.Text, out i))
            {
                button1.Enabled = false;
                textBox1.ForeColor = err;
            }
            else
            {
                textBox6.ForeColor = Color.Black;
                minBin = int.Parse(textBox6.Text);
            }

            if (!int.TryParse(textBox2.Text, out i))
            {
                button1.Enabled = false;
                textBox2.ForeColor = err;
            }
            else
            {
                textBox2.ForeColor = Color.Black;
                maxBin = int.Parse(textBox2.Text);
            }
                        
            if (Action == 4)
            {
                checkBox5.Visible = true;
                checkBox7.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
                label6.Visible = true;
                textBox8.Visible = true;
                textBox9.Visible = true;
                textBox10.Visible = true;
                checkBox7.Enabled = checkBox5.Checked;
                label4.Enabled = checkBox7.Checked;
                label5.Enabled = checkBox7.Checked;
                label6.Enabled = checkBox7.Checked;
                textBox8.Enabled = checkBox7.Checked;
                textBox9.Enabled = checkBox7.Checked;
                textBox10.Enabled = checkBox7.Checked;
                textBox1.Enabled = !checkBox7.Checked;
                
                if (checkBox7.Checked)
                {
                    textBox1.Text = "";
                    bool btok = true;
                    from = 0;
                    too = 0;
                    step = 0;
                    if (!int.TryParse(textBox8.Text, out i))
                    {
                        btok = false;
                        textBox8.BackColor = err;
                    }
                    else if (i < 1)
                    {
                        btok = false;
                        textBox8.BackColor = err;
                    }
                    else
                    {
                        from = int.Parse(textBox8.Text);
                        textBox8.BackColor = Color.White;
                    }

                    if (!int.TryParse(textBox9.Text, out i))
                    {
                        btok = false;
                        textBox9.BackColor = err;
                    }
                    else if (i <= from)
                    {
                        btok = false;
                        textBox9.BackColor = err;
                    }
                    else
                    {
                        too = int.Parse(textBox9.Text);
                        textBox9.BackColor = Color.White;
                    }

                    if (!int.TryParse(textBox10.Text, out i))
                    {
                        btok = false;
                        textBox10.BackColor = err;
                    }
                    else if (i < 1)
                    {
                        btok = false;
                        textBox10.BackColor = err;
                    }
                    else
                    {
                        step = int.Parse(textBox10.Text);
                        textBox10.BackColor = Color.White;
                    }

                    if (!btok) button1.Enabled = false;
                }
            }
        }

        public int GetBlocksize()
        {
            return BlockSize;
        }
        
        public int GetDigest()
        {
            return selectedType;
        }
        
        public int GetMinSize()
        {
            return minBin;
        }

        public int GetMaxSize()
        {
            return maxBin;
        }
        
        public bool GetStatistics()
        {
            return checkBox5.Checked;
        }
        
        public bool GetBlockSizeTest()
        {
            return checkBox7.Checked;    
        }
        
        public int GetBSfrom()
        {
            return from;
        }

        public int GetBSto()
        {
            return too;
        }

        public int GetBSstep()
        {
            return step;
        }
        
        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            validate();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

    }
}
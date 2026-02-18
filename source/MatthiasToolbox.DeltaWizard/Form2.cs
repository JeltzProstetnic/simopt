using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class Form2 : Form
    {
        private int Action;
        private String f1;
        private String f2;
        private String f3;
        private String f4;
        private String f5;
        private String f6;
        private String f7;
        private String f8;
        private bool cb;
        private Color err = Color.LightGoldenrodYellow;
        
        public String F1
        {
            get { return f1; }
        }
        public String F2
        {
            get { return f2; }
        }
        public String F3
        {
            get { return f3; }
        }
        public String F4
        {
            get { return f4; }
        }
        public String F5
        {
            get { return f5; }
        }
        public String F6
        {
            get { return f6; }
        }
        public String F7
        {
            get { return f7; }
        }
        public String F8
        {
            get { return f8; }
        }
        public bool CB
        {
            get
            {
                return cb;
            }
        }
        
        public Form2()
        {
            InitializeComponent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.Windows.Forms.MessageBox.Show(System.String,System.String,System.Windows.Forms.MessageBoxButtons,System.Windows.Forms.MessageBoxIcon)")]
        private void Button_1_Click(object sender, EventArgs e)
        {
            bool success = true;
            f1 = textBox1.Text;
            f2 = textBox2.Text;
            f3 = textBox3.Text;
            f4 = textBox4.Text;
            f5 = textBox5.Text;
            f6 = textBox6.Text;
            f7 = textBox7.Text;
            f8 = textBox8.Text;
            cb = checkBox1.Checked;
            
            if (f1 == "" || f2 == "") success = false;
            if (Action != 2)
            {
                if(Action != 3)
                {
                    if (f3 == "") success = false;
                }
                if ((Action != 1  && Action !=3) || checkBox1.Checked)
                {
                    if (f4 == "") success = false;
                }
            }
            if (Action == 4)
            {
                if (f5 == "" || f6 == "" || f7 == "" || f8 == "") success = false;
            }
            if(!success)
            {
                MessageBox.Show("Bitte füllen Sie zuerst alle Felder aus!", "Fehler", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            } 
            else Close();
        }

        private void Button_2_Click(object sender, EventArgs e)
        {
            f1 = textBox1.Text;
            f2 = textBox2.Text;
            f3 = textBox3.Text;
            f4 = textBox4.Text;
            f5 = textBox5.Text;
            f6 = textBox6.Text;
            f7 = textBox7.Text;
            f8 = textBox8.Text;
            cb = checkBox1.Checked;
            Close();
        }

        public int GetState()
        {
            if (radioButton1.Checked) return 1;
            else return 2;
        }
        public int GetState(ref String[] files, ref bool checkbox)
        {
            if(files.GetUpperBound(0) < 7) throw new ArgumentException("String[] files must be initialized to at least 8 entries", "files");
            files[0] = f1;
            files[1] = f2;
            files[2] = f3;
            files[3] = f4;
            files[4] = f5;
            files[5] = f6;
            files[6] = f7;
            files[7] = f8;
            checkbox = cb;
            return GetState();
        }
        public void SetState(int status, int mainfn, String[] files, bool checkbox)
        {
            if (files.GetUpperBound(0) < 7) throw new ArgumentException("String[] files must be initialized to at least 8 entries", "files");
            textBox1.Text = files[0];
            textBox2.Text = files[1];
            textBox3.Text = files[2];
            textBox4.Text = files[3];
            textBox5.Text = files[4];
            textBox6.Text = files[5];
            textBox7.Text = files[6];
            textBox8.Text = files[7];
            checkBox1.Checked = checkbox;
            Action = mainfn;
            switch (status)
            {
                case 1:
                    radioButton1.Checked = true;
                    break;
                case 2:
                    radioButton2.Checked = true;
                    break;
            }
            switch(Action)
            {
                case 1:
                    label1.Text = "Quelldaten:";
                    label2.Text = "Zieldaten:";
                    label3.Text = "Delta speichern:";
                    label4.Text = "Hashsets speichern:";
                    checkBox1.Visible = true;
                    checkBox1.Checked = false;
                    textBox4.Enabled = false;
                    break;
                case 2:
                    label1.Text = "Quelldaten:";
                    label2.Text = "Hashset(s) speichern:";
                    checkBox1.Visible = false;
                    label3.Visible = false;
                    label4.Visible = false;
                    textBox3.Visible = false;
                    textBox4.Visible = false;
                    button3.Visible = false;
                    button4.Visible = false;
                    break;
                case 3:
                    //label1.Text = "Quelldaten:";
                    //label2.Text = "Delta:";
                    //label3.Text = "Hashset(s):";
                    //label4.Text = "Rekonstruieren nach:";
                    label1.Text = "Quelldaten:";
                    label2.Text = "Delta:";
                    label3.Text = "Rekonstruieren nach:";
                    checkBox1.Visible = false;
                    label4.Visible = false;
                    textBox4.Visible = false;
                    button4.Visible = false;
                    break;
                case 4:
                    checkBox1.Visible = false;
                    label1.Text = "Daten Version 1 :";
                    label2.Text = "Daten Version 2 :";
                    label3.Text = "Delta (1 -> 2) :";
                    label4.Text = "Delta (2 -> 1) :";
                    label5.Text = "Hashset 1 :";
                    label6.Text = "Hashset 2 :";
                    label7.Text = "Rekonstruktion 1 :";
                    label8.Text = "Rekonstruktion 2 :";
                    label5.Visible = true;
                    label6.Visible = true;
                    label7.Visible = true;
                    label8.Visible = true;
                    textBox5.Visible = true;
                    textBox6.Visible = true;
                    textBox7.Visible = true;
                    textBox8.Visible = true;
                    button5.Visible = true;
                    button6.Visible = true;
                    button7.Visible = true;
                    button8.Visible = true;
                    break;
            }
            validate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox4.Enabled = checkBox1.Checked;
            if (!textBox4.Enabled) textBox4.Text = "";
            validate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";
            
            switch(Action)
            {
                case 1:
                    // optional save hashset
                    if (radioButton1.Checked) // folder
                    {
                        title = "Save hashsets to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Save hashset as:";
                        filter = "Hashset-Datei (*.hashset)|*.hashset|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
                case 3:
                    // save reconstruction
                    if (radioButton1.Checked) // folder
                    {
                        title = "Save reconstructed files to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Save reconstructed file as:";
                        filter = "Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
                case 4:
                    // save delta
                    if (radioButton1.Checked) // folder
                    {
                        title = "Save delta files to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Save delta file as:";
                        filter = "Delta-Datei (*.delta)|*.delta|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
            }
            textBox4.Text = result;
            if (checkBox1.Visible) checkBox1.Checked = true;
            validate();
        }
        
        private bool OpenFile(String title, String filter, ref String result)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Title = title;
            of.CheckFileExists = true;
            of.Filter = filter;
            if (of.ShowDialog() == DialogResult.OK)
            {
                result = of.FileName;
                return true;
            }
            else return false;
        }

        private bool SaveFile(String title, String filter, ref String result)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Title = title;
            sf.Filter = filter;
            sf.AddExtension = true;
            if (sf.ShowDialog() == DialogResult.OK)
            {
                result = sf.FileName;
                return true;
            }
            else return false;
        }

        private bool SelectFolder(String title, ref String result)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = title;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                result = fb.SelectedPath;
                return true;
            }
            else return false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            // open source data
            if (radioButton1.Checked) // folder
            {
                title = "Open sourcedata from:";
                if (!SelectFolder(title, ref result)) return;
            }
            else // file
            {
                title = "Sourcedata:";
                filter = "Alle Dateien (*.*)|*.*";
                if (!OpenFile(title, filter, ref result)) return;
            }
            textBox1.Text = result;
            validate();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            switch (Action)
            {
                case 1:
                case 4:
                    // open target data
                    if (radioButton1.Checked) // folder
                    {
                        title = "Open targetdata from:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Targetdata:";
                        filter = "Alle Dateien (*.*)|*.*";
                        if (!OpenFile(title, filter, ref result)) return;
                    }
                    break;
                case 2:
                    // save hashset(s)
                    if (radioButton1.Checked) // folder
                    {
                        title = "Save hashsets to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Save hashset as:";
                        filter = "Hashset-Datei (*.hashset)|*.hashset|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
                case 3:
                    // open delta
                    if (radioButton1.Checked) // folder
                    {
                        title = "Open delta files from:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Open delta file:";
                        filter = "Delta-Datei (*.delta)|*.delta|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                        if (!OpenFile(title, filter, ref result)) return;
                    }
                    break;
            }
            textBox2.Text = result;
            validate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            switch (Action)
            {
                case 1:
                case 4:
                    // save delta
                    if (radioButton1.Checked) // folder
                    {
                        title = "Save delta files to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Save delta file as:";
                        filter = "Delta-Datei (*.delta)|*.delta|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
                case 3:
                    // load hashset(s)
                    if (radioButton1.Checked) // folder
                    {
                        title = "Reconstruct to:";
                        if (!SelectFolder(title, ref result)) return;
                    }
                    else // file
                    {
                        title = "Reconstruct to:";
                        filter = "Alle Dateien (*.*)|*.*";
                        if (!SaveFile(title, filter, ref result)) return;
                    }
                    break;
            }
            textBox3.Text = result;
            validate();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            if (radioButton1.Checked) // folder
            {
                title = "Save hashsets to:";
                if (!SelectFolder(title, ref result)) return;
            }
            else // file
            {
                title = "Save hashset as:";
                filter = "Hashset-Datei (*.hashset)|*.hashset|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                if (!SaveFile(title, filter, ref result)) return;
            }
            
            textBox5.Text = result;
            validate();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            if (radioButton1.Checked) // folder
            {
                title = "Save hashsets to:";
                if (!SelectFolder(title, ref result)) return;
            }
            else // file
            {
                title = "Save hashset as:";
                filter = "Hashset-Datei (*.hashset)|*.hashset|XML-Datei (*.xml)|*.xml|Alle Dateien (*.*)|*.*";
                if (!SaveFile(title, filter, ref result)) return;
            }

            textBox6.Text = result;
            validate();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            // save reconstruction
            if (radioButton1.Checked) // folder
            {
                title = "Save reconstructed files to:";
                if (!SelectFolder(title, ref result)) return;
            }
            else // file
            {
                title = "Save reconstructed file as:";
                filter = "Alle Dateien (*.*)|*.*";
                if (!SaveFile(title, filter, ref result)) return;
            }

            textBox7.Text = result;
            validate();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            String title;
            String filter;
            String result = "";

            // save reconstruction
            if (radioButton1.Checked) // folder
            {
                title = "Save reconstructed files to:";
                if (!SelectFolder(title, ref result)) return;
            }
            else // file
            {
                title = "Save reconstructed file as:";
                filter = "Alle Dateien (*.*)|*.*";
                if (!SaveFile(title, filter, ref result)) return;
            }

            textBox8.Text = result;
            validate();
        }

        private void validate()
        {
            f1 = textBox1.Text;
            f2 = textBox2.Text;
            f3 = textBox3.Text;
            f4 = textBox4.Text;
            f5 = textBox5.Text;
            f6 = textBox6.Text;
            f7 = textBox7.Text;
            f8 = textBox8.Text;
            
            bool b1, b2, b3, b4, b5, b6, b7, b8;
            bool success = false;
            if(!radioButton1.Checked)
            {
                b1 = File.Exists(f1);
                b2 = File.Exists(f2);
                b3 = File.Exists(f3);
                b4 = File.Exists(f4);
                b5 = File.Exists(f5);
                b6 = File.Exists(f6);
                b7 = File.Exists(f7);
                b8 = File.Exists(f8);
            }
            else
            {
                b1 = Directory.Exists(f1);
                b2 = Directory.Exists(f2);
                b3 = Directory.Exists(f3);
                b4 = Directory.Exists(f4);
                b5 = Directory.Exists(f5);
                b6 = Directory.Exists(f6);
                b7 = Directory.Exists(f7);
                b8 = Directory.Exists(f8);
            }

            if(radioButton1.Checked) // ordner
            {
                switch(Action)
                {
                    case 1:
                        b3 = (textBox3.Text != "");
                        b4 = (!checkBox1.Checked || (textBox4.Text != ""));
                        break;
                    case 2:
                        b2 = (textBox2.Text != "");
                        break;
                    case 3:
                        b3 = (textBox3.Text != "");
                        break;
                    // no action needed for case 4
                }
            }
            else // dateien
            {
                switch (Action)
                {
                    case 1:
                        b3 = (textBox3.Text != "");
                        b4 = (!checkBox1.Checked || (textBox4.Text != ""));
                        break;
                    case 2:
                        b2 = (textBox2.Text != "");
                        break;
                    case 3:
                        b3 = (textBox3.Text != "");
                        break;
                    case 4:
                        b3 = true;
                        b4 = true;
                        b5 = true;
                        b6 = true;
                        b7 = true;
                        b8 = true;
                        break;
                }
            }
            
            if (!b1) textBox1.BackColor = err; else textBox1.BackColor = Color.White;
            if (!b2) textBox2.BackColor = err; else textBox2.BackColor = Color.White;
            if (!b3) textBox3.BackColor = err; else textBox3.BackColor = Color.White;
            if (!b4) textBox4.BackColor = err; else textBox4.BackColor = Color.White;
            if (!b5) textBox5.BackColor = err; else textBox5.BackColor = Color.White;
            if (!b6) textBox6.BackColor = err; else textBox6.BackColor = Color.White;
            if (!b7) textBox7.BackColor = err; else textBox7.BackColor = Color.White;
            if (!b8) textBox8.BackColor = err; else textBox8.BackColor = Color.White;
            
            switch(Action)
            {
                case 1:
                    success = b1 && b2 && b3 && b4;
                    break;
                case 2:
                    success = b1 && b2;
                    break;
                case 3:
                    success = b1 && b2;
                    break;
                case 4:
                    success = b1 && b2 && b3 && b4 && b5 && b6 && b7;
                    break;
            }
            
            button_1.Enabled = success;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            // TODO: remove in final
            //if (Action == 4)
            //{
            //    textBox1.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\software1.txt";
            //    textBox2.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\software2.txt";
            //    textBox3.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\d12.delta";
            //    textBox4.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\d21.delta";
            //    textBox5.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\h1.hashset";
            //    textBox6.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\h2.hashset";
            //    textBox7.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\software1.rec.txt";
            //    textBox8.Text = "C:\\Users\\Matthias\\Desktop\\Development\\test\\text\\software2.rec.txt";
            //}
            validate();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            validate();
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            validate();
        }
    }
}
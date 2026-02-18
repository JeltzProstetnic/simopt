using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MatthiasToolbox.Delta.Diff;
using MatthiasToolbox.Delta.Utilities;

namespace MatthiasToolbox.DiffWizard
{
    public partial class Form1 : Form
    {
        // for NativeWindow and PostMessageA
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_COMMAND = 0x111;
        private const int WM_USER = 0x400;

        // for GetScroll and PostMessageA
        private const int SBS_HORZ = 0;
        private const int SBS_VERT = 1;
        private const int SB_THUMBPOSITION = 4;

        // API Function: GetScrollPos
        [DllImport("user32.dll")]
        private static extern int GetScrollPos (IntPtr hWnd, int nBar);

        // API Function: PostMessageA
        [DllImport("user32.dll")]
        private static extern bool PostMessageA(IntPtr hwnd, int wMsg, int wParam, int lParam);

        public delegate void WndProcDelegate(ref Message uMsg);
        
        // NativeWindow Subclassing
        public class EventWrapper : NativeWindow
        {
            public event WndProcDelegate WindowProcedure = new WndProcDelegate(OnWndProc);

            public EventWrapper(IntPtr pWindowHandle)
            {
                base.AssignHandle(pWindowHandle);
            }

            protected override void WndProc(ref Message uMsg)
            {
                base.WndProc(ref uMsg);
                WindowProcedure(ref uMsg);
            }

            public static void OnWndProc(ref Message uMsg) { }
            
            public void SendWndProc(ref Message uMsg)
            {
                base.WndProc(ref uMsg);
            }

        }        
        
        private EventWrapper sClass1;
        private EventWrapper sClass2;
        
        public void sClass_WindowProcedure(ref Message uMsg)
        {
            switch(uMsg.Msg)
            {
                case WM_VSCROLL:
                    if(uMsg.HWnd.Equals(richTextBox1.Handle))
                    {
                        sClass2.WindowProcedure -= sClass_WindowProcedure;
                        Message msg1 = Message.Create(richTextBox2.Handle, uMsg.Msg, uMsg.WParam, uMsg.LParam);
                        sClass2.SendWndProc(ref msg1);
                        sClass2.WindowProcedure += sClass_WindowProcedure;
                    }
                    if(uMsg.HWnd.Equals(richTextBox2.Handle))
                    {
                        sClass2.WindowProcedure -= sClass_WindowProcedure;
                        Message msg1 = Message.Create(richTextBox1.Handle, uMsg.Msg, uMsg.WParam, uMsg.LParam);
                        sClass2.SendWndProc(ref msg1);
                        sClass2.WindowProcedure += sClass_WindowProcedure;
                    }
                    break;
                case WM_HSCROLL:
                    if (uMsg.HWnd.Equals(richTextBox1.Handle))
                    {
                        sClass2.WindowProcedure -= sClass_WindowProcedure;
                        Message msg1 = Message.Create(richTextBox2.Handle, uMsg.Msg, uMsg.WParam, uMsg.LParam);
                        sClass2.SendWndProc(ref msg1);
                        sClass2.WindowProcedure += sClass_WindowProcedure;
                    }
                    if (uMsg.HWnd.Equals(richTextBox2.Handle))
                    {
                        sClass2.WindowProcedure -= sClass_WindowProcedure;
                        Message msg1 = Message.Create(richTextBox1.Handle, uMsg.Msg, uMsg.WParam, uMsg.LParam);
                        sClass2.SendWndProc(ref msg1);
                        sClass2.WindowProcedure += sClass_WindowProcedure;
                    }
                    break;
            }
            
        }
        
        private MyersDiff myers;
        private ScriptDrawer sd;
        private DiffVisualizer td;
        
        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            GC.Collect();
            
            if (!(File.Exists(textBox4.Text) && File.Exists(textBox5.Text)))
            {
                MessageBox.Show(
                    "Datei nicht gefunden. Wählen Sie zuerst zwei Textdateien auf der Registrierkate \"Diff View\"",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FileInfo f1 = new FileInfo(textBox4.Text);
            FileInfo f2 = new FileInfo(textBox5.Text);

            if(f1.Length > 20000 || f2.Length > 20000)
            {
                if(MessageBox.Show(
                    "Dieser Vorgang kann einige Minuten in Anspruch nehmen und benötigt enorm viel Arbeitsspeicher. Möglicherweise reagiert die Anwendung nicht mehr und muß terminiert werden. Wollen Sie trotzdem fortfahren?",
                    "Warnung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)==DialogResult.Cancel) return;
            }
            
            button1.Enabled = false;
            button5.Enabled = false;
            
            myers = new MyersDiff();

            Trace t = myers.GetDiff(f1, f2, true);
            // Trace t = myers.GetDiff(textBox1.Text.Replace(",", Environment.NewLine), textBox2.Text.Replace(",", Environment.NewLine), true);
            sd = new ScriptDrawer(myers.Indices1(), myers.Indices2());
            scrollablePictureBox.Image = sd.Image;
            
            textBox3.Text = t.ToString();
            
            //Application.DoEvents();
            
            Point lastPoint = new Point(-1,-1);
            for (int i = 0; i < t.Count; i++)
            {
                for (int j = lastPoint.X + 1; j < t.MatchPoints()[i].X; j++ )
                {
                    sd.Delete(j, t.MatchPoints()[i].Y);
                }
                for (int k = lastPoint.Y + 1; k < t.MatchPoints()[i].Y; k++)
                {
                    sd.Insert(lastPoint.X + 1, k);
                }
                sd.Noop(t.MatchPoints()[i].X, t.MatchPoints()[i].Y);
                lastPoint.X = t.MatchPoints()[i].X;
                lastPoint.Y = t.MatchPoints()[i].Y;
            }
            for (int j = lastPoint.X + 1; j < myers.Length1; j++)
            {
                sd.Delete(j, myers.Length2);
            }
            for (int k = lastPoint.Y + 1; k < myers.Length2; k++)
            {
                sd.Insert(lastPoint.X + 1, k);
            }

            button1.Enabled = true;
            button5.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fo = new OpenFileDialog();
            if(fo.ShowDialog() == DialogResult.OK) textBox4.Text = fo.FileName;
            if(File.Exists(textBox4.Text)) richTextBox1.LoadFile(textBox4.Text, RichTextBoxStreamType.PlainText);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!(File.Exists(textBox4.Text) && File.Exists(textBox5.Text)))
            {
                MessageBox.Show(
                    "Datei nicht gefunden. Wählen Sie zuerst zwei Textdateien aus.",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            button4.Enabled = false;
            
            FileInfo f1 = new FileInfo(textBox4.Text);
            FileInfo f2 = new FileInfo(textBox5.Text);
            
            richTextBox1.Text = ""; //  s1;
            richTextBox2.Text = ""; // s2;
            
            myers = new MyersDiff();
            Trace t = myers.GetDiff(f1, f2, true);

            td = new DiffVisualizer(richTextBox1, richTextBox2, f1, f2);
            
            Point lastPoint = new Point(-1, -1);
            for (int i = 0; i < t.Count; i++)
            {
                for (int j = lastPoint.X + 1; j < t.MatchPoints()[i].X; j++)
                {
                    td.Delete(j, t.MatchPoints()[i].Y);
                }
                for (int k = lastPoint.Y + 1; k < t.MatchPoints()[i].Y; k++)
                {
                    td.Insert(lastPoint.X + 1, k);
                }
                td.Noop(t.MatchPoints()[i].X, t.MatchPoints()[i].Y);
                lastPoint.X = t.MatchPoints()[i].X;
                lastPoint.Y = t.MatchPoints()[i].Y;
            }
            for (int j = lastPoint.X + 1; j < myers.Length1; j++)
            {
                td.Delete(j, myers.Length2);
            }
            for (int k = lastPoint.Y + 1; k < myers.Length2; k++)
            {
                td.Insert(lastPoint.X + 1, k);
            }

            button4.Enabled = true;
        } // void
        
        
        internal class DiffVisualizer
        {
            private RichTextBox rtb1;
            private RichTextBox rtb2;
            private StreamReader text1;
            private StreamReader text2;
            private String[] Spaces = { "", " ", "  ", "   ", "    ", "     ", "      " };
            private String line;
            private String dummy;
            private int i;
            
            public DiffVisualizer(RichTextBox rtb1, RichTextBox rtb2, Stream text1, Stream text2)
            {
                this.rtb1 = rtb1;
                this.rtb2 = rtb2;
                this.text1 = new StreamReader(text1, true);
                this.text2 = new StreamReader(text2, true);
            }

            public DiffVisualizer(RichTextBox rtb1, RichTextBox rtb2, FileInfo text1, FileInfo text2)
            {
                this.rtb1 = rtb1;
                this.rtb2 = rtb2;
                this.text1 = text1.OpenText();
                this.text2 = text2.OpenText();
            }
            
            public void Noop(int x, int y)
            {
                AddLine1(Color.LightGreen, x + 1);
                AddLine2(Color.LightGreen, y + 1);                        
            }
            public void Insert(int x, int y)
            {
                AddEmpty1();
                AddLine2(Color.White, y + 1);
            }
            public void Delete(int x, int y)
            {
                AddLine1(Color.OrangeRed, x + 1);
                AddEmpty2();
            }

            private void AddLine1(Color color, int index)
            {
                int p1 = rtb1.TextLength;
                
                line = index.ToString().Trim();
                i = 7 - line.Length;
                if(i >= 0) line = Spaces[i] + line;
                line += " " + GetNextLine1();
                rtb1.AppendText(line);
                
                rtb1.SelectionStart = p1;
                rtb1.SelectionLength = 7;
                rtb1.SelectionBackColor = color;
            }

            private void AddLine2(Color color, int index)
            {
                int p1 = rtb2.TextLength;
                
                line = index.ToString().Trim();
                i = 7 - line.Length;
                if (i >= 0) line = Spaces[i] + line;
                line += " " + GetNextLine2();
                rtb2.AppendText(line);

                rtb2.SelectionStart = p1;
                rtb2.SelectionLength = 7;
                rtb2.SelectionBackColor = color;
            }
            
            private void AddEmpty1()
            {
                rtb1.AppendText("\r\n");
            }

            private void AddEmpty2()
            {
                rtb2.AppendText("\r\n");
            }
            
            private String GetNextLine1()
            {
                if ((dummy = text1.ReadLine()) != null) return dummy + Environment.NewLine;
                return "";
            }
            
            private String GetNextLine2()
            {
                if ((dummy = text2.ReadLine()) != null) return dummy + Environment.NewLine;
                return "";
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog fo = new OpenFileDialog();
            if (fo.ShowDialog() == DialogResult.OK) textBox5.Text = fo.FileName;
            if (File.Exists(textBox5.Text)) richTextBox2.LoadFile(textBox5.Text, RichTextBoxStreamType.PlainText);
        }
               
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            richTextBox1.Width = Width/2 - 25;
            richTextBox2.Left = richTextBox1.Width + 10;
            richTextBox2.Width = richTextBox1.Width;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(sd==null) return;
            SaveFileDialog sf = new SaveFileDialog();
            sf.DefaultExt = "png";
            sf.Filter = "Portable Network Graphics, *.png|*.png";
            if(sf.ShowDialog() == DialogResult.OK) sd.Image.Save(sf.FileName);
        }

        private void Form1_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Info i = new Info();
            i.ShowDialog();
            e.Cancel = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sClass1 = new EventWrapper(richTextBox1.Handle);
            sClass2 = new EventWrapper(richTextBox2.Handle);
            sClass1.WindowProcedure += new WndProcDelegate(this.sClass_WindowProcedure);
            sClass2.WindowProcedure += new WndProcDelegate(this.sClass_WindowProcedure);
        }
        
    } // class
} // namespace
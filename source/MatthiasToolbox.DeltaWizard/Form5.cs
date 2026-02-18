using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
        }
        
        public String GetText()
        {
            return textBox1.Text;
        }
        
        public void SetText(String text)
        {
            textBox1.WordWrap = false;
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Text = text;
        }
        
        public void AppendText(String text)
        {
            //textBox1.Text += text + Environment.NewLine;
            textBox1.AppendText(text + Environment.NewLine);
            //textBox1.SelectionStart = textBox1.Text.Length - 1;
            //textBox1.ScrollToCaret();
        }


        /*
         
          had the same problem as Al, adding text when scrollbars are present makes the cursor scroll up to the top of the control. 
This can be prevented by adding the following code to store then restore the scroll position at the end of the update. 



//used for saving and restoring the scroll position to avoid flickering... 
[StructLayout(LayoutKind.Sequential)] 
private struct POINT 
{ 
public long X; 
public long Y; 
} 

private const int EM_GETSCROLLPOS = 0x0400 + 221; 
private const int EM_SETSCROLLPOS = 0x0400 + 222; 

[DllImport( "user32", CharSet = CharSet.Auto )] 
private static extern int SendMessage( HandleRef hWnd, int msg, 
int wParam, ref POINT lp ); 


//used to store the scroll position at the start of updating 
POINT _scrollpos = new POINT(); 


//--------Add this code to BeginUpdate() after "if ( updating > 1 ) return;"---------- 

//store the current scrollposition 
SendMessage(new HandleRef(this, Handle), EM_GETSCROLLPOS, 0, ref _scrollpos); 
//------------------------------------------------------------------------------------ 


//--------Add this code to EndUpdate() after "if ( updating > 1 ) return;"------------ 

//restore the scrollposition 
SendMessage(new HandleRef(this, Handle), EM_SETSCROLLPOS, 0, ref _scrollpos); 
//------------------------------------------------------------------------------------ 

         
         */


        public void Release()
        {
            button1.Enabled = true;
            button2.Visible = true;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form5_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
            try
            {
                textBox1.SelectionStart = textBox1.Text.Length - 1;
                textBox1.ScrollToCaret();
            }
            catch { }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.DefaultExt = "txt";
            if(sf.ShowDialog()==DialogResult.OK)
            {
                try
                {
                    FileStream fs = File.Open(sf.FileName, FileMode.CreateNew, FileAccess.Write);
                    fs.Write(Encoding.UTF8.GetBytes(textBox1.Text), 0, textBox1.Text.Length);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

    }
}
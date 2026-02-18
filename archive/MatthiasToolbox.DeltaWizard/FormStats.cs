using System;
using System.Collections;
using System.Windows.Forms;

namespace MatthiasToolbox.DeltaWizard
{
    public partial class FormStats : Form
    {
        private int anz = 0;
        
        public FormStats()
        {
            InitializeComponent();
            tabControl1.TabPages.Clear();
            tabControl1.TabPages.Add(tabPage1);
        }

        public void Init(String introText, int count)
        {
            anz = count;
            label1.Text = introText + Environment.NewLine;
            label1.Text += anz.ToString() + " Dateien verarbeitet.";
        }
        
        public void SetFails(int count)
        {
            if (count > 0) label1.Text += " (" + count.ToString() + " Fehler.)";
        }
        
        public void SetStats(double AvgSpeed, long AvgSize, double AvgCR, Hashtable exts)
        {
            tabControl1.TabPages.Add(tabPage2);
            label2.Text = "Durchschnittlicher Datendurchsatz " + Math.Round(AvgSpeed, 2).ToString() + " MB/s";
            label3.Text = "Durchschnittliche Dateigröße " + Math.Round((double)AvgSize / (1024*1024), 2).ToString() + " MB";
            label4.Text = "Durchschnittliche Compression Ratio " + Math.Round(AvgCR, 2).ToString();
            foreach(DictionaryEntry d in exts)
            {
                object[] o = { (String)d.Key, Math.Round((double)d.Value, 2) };
                dataGridView1.Rows.Add(o);
            }
        }
        
        public void SetBStats(Hashtable errs, Hashtable crs, Hashtable thr)
        {
            if(errs.Count>0)
            {
                tabControl1.TabPages.Add(tabPage3);
                foreach (DictionaryEntry d in errs)
                {
                    object[] o = { (int)d.Key, (int)d.Value };
                    dataGridView2.Rows.Add(o);
                }
            }
            if(crs != null)
            {
                tabControl1.TabPages.Add(tabPage5);
                foreach (DictionaryEntry d in crs)
                {
                    object[] o = { (int)d.Key, Math.Round((double)d.Value, 2) };
                    dataGridView3.Rows.Add(o);
                }
            }
            if(thr != null)
            {
                tabControl1.TabPages.Add(tabPage6);
                foreach (DictionaryEntry d in thr)
                {
                    object[] o = { (int)d.Key, Math.Round((double)d.Value, 2) };
                    dataGridView4.Rows.Add(o);
                }
            }
        }
        
    }
}
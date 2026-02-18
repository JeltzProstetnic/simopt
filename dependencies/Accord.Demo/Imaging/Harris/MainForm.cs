using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Imaging;
using AForge.Imaging.Filters;

namespace Harris
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Open a image
            Bitmap lenna = Harris.Properties.Resources.lena512;

            double sigma = (double)numSigma.Value;
            float k = (float)numK.Value;
            float threshold = (float)numThreshold.Value;

            // Create a new Harris Corners Detector using the given parameters
            HarrisCornersDetector harris = new HarrisCornersDetector(k);
            harris.Threshold = threshold;
            harris.Sigma = sigma;

            // Create a new AForge's Corner Marker Filter
            CornersMarker corners = new CornersMarker(harris, Color.White);

            // Apply the filter and display it on a picturebox
            pictureBox1.Image = corners.Apply(lenna);
        }
    }
}

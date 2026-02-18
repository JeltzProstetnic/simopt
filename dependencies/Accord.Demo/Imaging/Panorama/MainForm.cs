using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math;
using AForge.Imaging.Filters;
using AForge;

namespace Panorama
{
    public partial class MainForm : Form
    {
        private Bitmap img1 = Panorama.Properties.Resources.green_nature1;
        private Bitmap img2 = Panorama.Properties.Resources.green_nature2;

        private IntPoint[] harrisPoints1;
        private IntPoint[] harrisPoints2;

        private IntPoint[] correlationPoints1;
        private IntPoint[] correlationPoints2;

        private double[,] H;


        public MainForm()
        {
            InitializeComponent();

            pictureBox1.Image = img1;
            pictureBox2.Image = img2;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            // Step 1: Detect feature points using Harris Corners Detector
            HarrisCornersDetector harris = new HarrisCornersDetector(0.04f, 4000f);
            harrisPoints1 = harris.ProcessImage(img1).ToArray();
            harrisPoints2 = harris.ProcessImage(img2).ToArray();

            pictureBox1.Image = new PointsMarker(harrisPoints1).Apply(img1);
            pictureBox2.Image = new PointsMarker(harrisPoints2).Apply(img2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Step 2: Match feature points using a correlation measure
            CorrelationMatching matcher = new CorrelationMatching(9);
            IntPoint[][] matches = matcher.Match(img1, img2, harrisPoints1, harrisPoints2);

            correlationPoints1 = matches[0];
            correlationPoints2 = matches[1];

            Concatenate concat = new Concatenate(img1);
            var img3 = concat.Apply(img2);

            PairsMarker pairs = new PairsMarker(correlationPoints1,
                correlationPoints2.Apply(p => new IntPoint(p.X + img1.Width, p.Y)));

            pictureBox1.Image = img1;
            pictureBox2.Image = img2;
            pictureBox3.Image = pairs.Apply(img3);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Step 3: Create the homography matrix using a robust estimator
            RansacHomographyEstimator ransac = new RansacHomographyEstimator(0.001, 0.99);
            H = ransac.Estimate(correlationPoints1, correlationPoints2);

            // Plot RANSAC results against correlation results
            var inliers1 = correlationPoints1.Submatrix(ransac.Inliers);
            var inliers2 = correlationPoints2.Submatrix(ransac.Inliers);

            Concatenate concat = new Concatenate(img1);
            var img3 = concat.Apply(img2);
            PairsMarker pairs = new PairsMarker(inliers1,
                inliers2.Apply(p => new IntPoint(p.X + img1.Width, p.Y)));

            pictureBox1.Image = img1;
            pictureBox2.Image = img2;
            pictureBox3.Image = pairs.Apply(img3);

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Step 4: Project and blend the second image using the homography
            Blend blend = new Blend(H.Inverse(), img1);
            pictureBox3.Image = blend.Apply(img2);
        }


    }
}

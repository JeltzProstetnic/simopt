using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Windows;
using Accord.DirectSound;
using AForge;
using AForge.Controls;
using AForge.Video.DirectShow;


namespace Fourier
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        private IAudioSource source;
        private IWindow window;

        public MainForm()
        {
            InitializeComponent();
        }


        void Button1Click(object sender, EventArgs e)
        {
            source = new AudioCaptureDevice(comboBox1.Text);
            source.DesiredFrameSize = 2048;
            source.SampleRate = 22050;
            source.NewFrame += new NewFrameEventHandler(source_NewFrame);

            window = RaisedCosineWindow.Hamming(source.DesiredFrameSize);

            source.Start();
        }

        void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            ComplexSignal c = ComplexSignal.FromArray(eventArgs.Frame[0], eventArgs.SamplingRate);

            if (hammingWindowToolStripMenuItem.Checked)
                c = window.Apply(c, 0);

            c.ForwardFourierTransform();
            double[] power = Tools.GetPowerSpectrum(c.Data[0], eventArgs.SamplingRate);
            double[] freqv = Tools.GetFrequencyVector(c.Data[0].Length, eventArgs.SamplingRate);

            power[0] = 0; // zero DC

            double[,] g = new double[power.Length, 2];

            for (int i = 0; i < power.Length; i++)
            {
                g[i, 0] = freqv[i];
                g[i, 1] = power[i];
            }

            chart1.RangeX = new DoubleRange(freqv[0], freqv[freqv.Length - 1] / hScrollBar1.Value);
            chart1.RangeY = new DoubleRange(0, System.Math.Pow(10,-vScrollBar1.Value));

            chart1.UpdateDataSeries("fft", g);
        }


        void MainFormLoad(object sender, EventArgs e)
        {
        //    wavechart1.AddWaveform("wave", Color.Blue, 1, false);
            chart1.AddDataSeries("fft", Color.Black, Chart.SeriesType.Line, 1, false);

            try
            {
                // enumerate audio devices
                FilterInfoCollection audioDevices = new FilterInfoCollection(FilterCategory.AudioInputDevice);

                if (audioDevices.Count == 0)
                    throw new ApplicationException();

                // add all devices to combo
                foreach (FilterInfo device in audioDevices)
                {
                    comboBox1.Items.Add(device.Name);
                }
            }
            catch (ApplicationException)
            {
                comboBox1.Items.Add("No local capture devices");
                comboBox1.Enabled = false;
            }

            comboBox1.SelectedIndex = 0;
        }

        void Button2Click(object sender, EventArgs e)
        {
            if (source != null)
                source.SignalToStop();
        }

        void HammingWindowToolStripMenuItemClick(object sender, EventArgs e)
        {
            hammingWindowToolStripMenuItem.Checked = !hammingWindowToolStripMenuItem.Checked;
        }

        void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            if (source != null)
                source.SignalToStop();
        }

    }
}

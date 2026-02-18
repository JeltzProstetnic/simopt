using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audition.Beat;
using Accord.DirectSound;
using AForge.Video.DirectShow;
using Accord.Audition.Tempo;
using AForge;
using System.Collections.Generic;


namespace BeatDetector
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        private IAudioSource source;
        private EnergyBeatDetector detector;
        private IntercorrelationTempoDetector tempoDetector;
        private Metronome metronome;
        private Signal current;

        private List<ComplexSignal> sample;
        private bool collecting;

        public MainForm()
        {
            InitializeComponent();

            metronome = new Metronome();
            metronome.SynchronizingObject = lbManualTempo;
            metronome.TempoDetected += metronome_TempoDetected;
        }

        void metronome_TempoDetected(object sender, EventArgs e)
        {
            lbManualTempo.Text = metronome.BeatsPerMinute.ToString();
        }


        void Button1Click(object sender, EventArgs e)
        {
            lbStatus.Text = "Waiting for soundcard...";
            source = new AudioCaptureDevice(comboBox1.Text);
            source.SampleRate = 44100;
            source.DesiredFrameSize = 1024;
            source.NewFrame += new NewFrameEventHandler(source_NewFrame);


            detector = new EnergyBeatDetector(43);
            detector.Beat += new EventHandler(detector_Beat);


            tempoDetector = new IntercorrelationTempoDetector(1024, source.SampleRate);
            tempoDetector.Range = new IntRange(60, 230);
            tempoDetector.Step = 5;

            sample = new List<ComplexSignal>();

            source.Start();
        }

        void detector_Beat(object sender, EventArgs e)
        {
            if (timer1.Enabled == false)
            {
                this.button5.BackColor = Color.LightGreen;
                timer1.Start();
            }
            else
            {
                timer1.Stop();
                timer1.Start();
            }
        }

        void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            current = Signal.FromArray(eventArgs.Frame[0], eventArgs.SamplingRate);

            lbStatus.Invoke((MethodInvoker)delegate()
            { detector.Detect(current); });

            lbStatus.Invoke((MethodInvoker)delegate()
            {
                if (collecting)
                {
                    if (sample.Count != 8192)
                    {
                        sample.Add(ComplexSignal.FromArray(eventArgs.Frame[0], eventArgs.SamplingRate));
                    }
                    else
                    {
                        collecting = false;
                        var c = ComplexSignal.Combine(sample.ToArray());
                        sample.Clear();
                        lbAutoTempo.Text = tempoDetector.Detect(c).ToString();
                    }
                }
            });


            lbStatus.Invoke((MethodInvoker)delegate()
            {
                label1.Text = "Frame duration (ms): " + current.Duration;
                lbStatus.Text = "Ready";
            });
        }


        void MainFormLoad(object sender, EventArgs e)
        {
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
            source.SignalToStop();
        }


        void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            if (source != null)
                source.SignalToStop();
        }

        void Button3Click(object sender, EventArgs e)
        {
            sample.Clear();
            collecting = true;
        }

        void Button4Click(object sender, EventArgs e)
        {
            metronome.Tap();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            button5.BackColor = SystemColors.Control;
        }



    }
}

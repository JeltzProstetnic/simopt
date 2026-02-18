using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.ComplexFilters;
using Accord.Audio.Generators;

namespace SignalSplitter
{
    public partial class MainForm : Form
    {
        Signal signal;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FrequencyFilter filter = new FrequencyFilter(0, 40, 80, 160, 220);
            ComplexSignal c = ComplexSignal.FromSignal(signal);

            ComplexSignal[] bands = filter.Apply(c);

            bands[0].BackwardFourierTransform();

            wavechart2.RangeX = new AForge.DoubleRange(0, 1024);
            wavechart2.UpdateWaveform("Signal", bands[0].ToArray()[0]);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CosineGenerator generator;
            
            generator = new CosineGenerator(50, 1, 9600);
            signal = generator.Generate<Signal>(1024);

            generator = new CosineGenerator(25, 0.5, 9600);
            signal += generator.Generate<Signal>(1024);

            generator = new CosineGenerator(200, 0.2, 9600);
            signal += generator.Generate<Signal>(1024);

            wavechart2.AddWaveform("Signal", Color.Blue, 1, true);
            wavechart2.RangeX = new AForge.DoubleRange(0, 1024);
           // wavechart2.RangeY = new AForge.DoubleRange(-1, 1);
            
            wavechart2.UpdateWaveform("Signal", signal.Data[0]);
        }
    }
}

using System;
using System.Threading;
using System.Windows.Forms;
using MatthiasToolbox.Logging;
using MatthiasToolbox.Logging.Loggers;
using System.IO;
using MatthiasToolbox.Simulation.Tools;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using System.Diagnostics;

namespace MatthiasToolbox.SQSSModel
{
    /// <summary>
    /// GUI for the SQSS simulation.
    /// </summary>
    public partial class Form1 : Form
    {
        #region cvar

        private Simulation _sim;

        #endregion
        #region ctor

        /// <summary>
        /// Initialize components and logger.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            checkBoxLog.Checked = true;
            //Logger.Add(new RichTextBoxLogger(richTextBox1));
        }

        #endregion
        #region hand

        #region form

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Logger.Shutdown(true); // not required
        }

        #endregion
        #region gui

        // Start
        private void button1_Click(object sender, EventArgs e)
        {
            if (!_sim.Model.IsRunning)
            {
                _sim.Model.LogEvents = checkBoxLog.Checked;
                _sim.Queue.LogGet = checkBoxLog.Checked;
                _sim.Queue.LogPut = checkBoxLog.Checked;
                _sim.Queue.LogReject = checkBoxLog.Checked;
                if (!_sim.Model.IsReset) _sim.Model.Reset();
                button1.Enabled = false;
                button2.Enabled = true;
                (new Thread(new ThreadStart(() => _sim.Start(2)))).Start();
            }
        }

        // Stop
        private void button2_Click(object sender, EventArgs e)
        {
            _sim.Stop();
            button1.Enabled = true;
            button2.Enabled = false;
            Application.DoEvents();
        }

        #endregion
        #region sim

        private void Finished()
        {
            this.Log<INFO>("GUI finished." + Environment.NewLine);

            // the following line causes the application to wait here, 
            // until the last log message is written. Note though, that
            // the stop button will usually do nothing, because until 
            // you manage to click it, the actual simulation is already 
            // finished since aeons. for the stop button to meet a 
            // model which is still running, you must set a very long 
            // simulation duration.
            Logger.Dispatch();

            button1.Invoke(new Action(() => button1.Enabled = true));
            button2.Invoke(new Action(() => button2.Enabled = false));
            Application.DoEvents();
        }

        #endregion

        
        #endregion

       

        private void cmdInitSimulation_Click(object sender, EventArgs e)
        {
            this.Log<INFO>("Initializing simulation.");
            // TODO check ref to form1 
            _sim = new Simulation(123, richTextBox1);
            _sim.Model.SimulationTerminating += Finished;
            _sim.BuildModel();

            this.Log<INFO>("Initialization finished.");
        }

        private void cmdLoad_Click(object sender, EventArgs e)
        {
            FileStream fs = new FileStream("DataFile.dat", FileMode.Open);
            fs.Position = 0;
            // Construct a BinaryFormatter and use it 
            // to serialize the from to the stream.
            EnumerationSurrogateSelector sel = new EnumerationSurrogateSelector();
            BinaryFormatter formatter = new BinaryFormatter(sel, new StreamingContext());
            _sim = (Simulation)formatter.Deserialize(fs);

            
            fs.Close();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            
            // Now try to save the model 
            FileStream fs = new FileStream("DataFile.dat", FileMode.Create);
            // Construct a BinaryFormatter and use it 
            // to serialize the data to the stream.
            EnumerationSurrogateSelector sel = new EnumerationSurrogateSelector();
            //sel.AddSurrogate(

            BinaryFormatter formatter = new BinaryFormatter(sel, new StreamingContext());
            // Serialize the array elements.
            formatter.Serialize(fs, _sim);

            fs.Close();
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("SimTime before=:" + _sim.Model.CurrentTime.ToString());
            _sim.Start(1);
            Debug.WriteLine("SimTime after:" + _sim.Model.CurrentTime.ToString());
        }

        private void cmdContinue_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("SimTime before:" + _sim.Model.CurrentTime.ToString());
            _sim.Continue(1);
            Debug.WriteLine("SimTime after:" + _sim.Model.CurrentTime.ToString());
        }
    }


    public class NonSerializableSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        { foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))      
            info.AddValue(f.Name, f.GetValue(obj)); 
        } 
        
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
            foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))          
                f.SetValue(obj, info.GetValue(f.Name, f.FieldType)); return obj;
        }
    }


    public class EmptySurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                info.AddValue(f.Name, f.GetValue(obj));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
          //  foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
          //      f.SetValue(obj, info.GetValue(f.Name, f.FieldType)); return obj;
            return null;
        }
    }


    
    // See http://msdn.microsoft.com/msdnmag/issues/02/09/net/#S3 
    class EnumerationSurrogateSelector : ISurrogateSelector
    {
        ISurrogateSelector _next;

        public void ChainSelector(ISurrogateSelector selector)
        {
            _next = selector;
        }

        public ISurrogateSelector GetNextSelector()
        {
            return _next;
        }

        public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            //Generator
            selector = null;

            if (type.FullName.StartsWith("System.Func")){
                //System.Diagnostics.Debug.WriteLine(type.FullName);
                //return new NonSerializableSurrogate();
            }


            if (type.FullName.StartsWith("MatthiasToolbox.Simulation.Templates.Source"))
            {
                System.Diagnostics.Debug.WriteLine(type.FullName);
                return new EmptySurrogate();
            }

            
            //if (type.FullName == "System.Action")
            //{
            //    selector = this;
            //    return new NonSerializableSurrogate();
 
            //}
            //else
            //{
       
            //}

            
            return null;
        }
    }


}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MatthiasToolbox.Logging.Loggers;
using MatthiasToolbox.NetworkDriver.Model;
using MatthiasToolbox.Presentation.Shapes;
using MatthiasToolbox.Simulation;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Network;
using MatthiasToolbox.Simulation.Tools;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using MatthiasToolbox.Basics.Attributes;
using System.Diagnostics;

namespace MatthiasToolbox.NetworkDriver
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    //[SimSerializationContainer]
    public partial class MainWindow : Window
    {
        #region cvar

        private Model.Simulation sim;
        private AsyncModelRunner amr;
        private DispatcherTimer timer;

        #endregion
        #region ctor

        public MainWindow()
        {
            InitializeComponent();

            sim = new Model.Simulation(123, 300, 0.06d, 20);
            sim.Model.StepSize = new TimeSpan(0, 0, 0, 1, 0).ToDouble();

            Simulator.RegisterSimulationLogger(new ConsoleLogger());

            timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 50), DispatcherPriority.Input, TimerCallback, this.Dispatcher);
            timer.IsEnabled = false;

            CreateUIModel();
            //TODO Create Solution for HAndler outside sim model 
            //sim.SpecialDriver.PassThroughEvent.AddHandler(PassThroughHandler);
        }

        #endregion
        #region hand

        #region window

        private void TimerCallback(object sender, EventArgs e)
        {
            sim.Model.Step();
            UpdateModelVisualization();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
           
        }

        #endregion
        #region controls

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            buttonRun.IsEnabled = false;
            buttonRunAsync.IsEnabled = false;
           
            timer.Start();
        }

        private void buttonRunAsync_Click(object sender, RoutedEventArgs e)
        {
            sliderSpeed.IsEnabled = true;
            buttonRun.IsEnabled = false;
            buttonRunAsync.IsEnabled = false;
            sim.InitializeDrivers();
            amr = sim.StartAsync(UpdateModelVisualization, 20);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double min = sliderSpeed.Minimum;
            double max = sliderSpeed.Maximum;
            double minv = Math.Max(0d, Math.Log(sliderSpeed.Minimum));
            double maxv = Math.Log(sliderSpeed.Maximum);

            // calculate adjustment factor and result
            double scale = (maxv - minv) / (max - min);
            double logValue = Math.Exp(minv + scale * (sliderSpeed.Value - min));

            if (amr != null)
            {
                amr.PreferredSpeed = logValue;
                amr.ResetSpeedStatistics();
                sliderSpeed.ToolTip = "Speed set to " + Math.Round(amr.PreferredSpeed, 2).ToString();

                sim.Model.LoggingEnabled = amr.PreferredSpeed < 1000;
            }
        }

        #endregion
        #region model

        private void PassThroughHandler(MobileEntity entity, ISimulationNode node)
        {
            if (sim.Model.LoggingEnabled)
            {
                Console.WriteLine(entity.EntityName + " passed through " + node.ToString());
                Console.WriteLine("    previous node=" + entity.PreviousNode.ToString());
                Console.WriteLine("     current node=" + entity.CurrentNode.ToString());
                Console.WriteLine("        next node=" + entity.NextNode.ToString());
            }
        }

        #endregion

        #endregion
        #region impl

        /// <summary>
        /// Create GUI Model
        /// </summary>
        private void CreateUIModel()
        {
            #region nodes

            foreach (Node n in sim.Nodes)
            {
                Ellipse el = new Ellipse();
                el.Width = 4;
                el.Height = 4;
                el.Fill = new SolidColorBrush(Colors.Black);
                el.SetValue(Canvas.LeftProperty, n.Position.X - 2);
                el.SetValue(Canvas.TopProperty, n.Position.Y - 2);
                el.ToolTip = n.Name;
                canvasModel.Children.Add(el);
            }

            #endregion
            #region connections

            foreach (Connection con in sim.Connections)
            {
                if (con.IsDirected)
                {
                    SimpleArrow a = new SimpleArrow();
                    a.Stroke = new SolidColorBrush(Colors.Black);
                    a.StrokeThickness = 1;
                    a.X1 = (con.Node1 as Node).Position.X;
                    a.Y1 = (con.Node1 as Node).Position.Y;
                    a.X2 = (con.Node2 as Node).Position.X;
                    a.Y2 = (con.Node2 as Node).Position.Y;
                    a.HeadWidth = 8;
                    a.HeadHeight = 3;
                    a.Opacity = 0.5;
                    canvasModel.Children.Add(a);
                }
                else
                {
                    Line l = new Line();
                    l.Stroke = new SolidColorBrush(Colors.Black);
                    l.StrokeThickness = 1;
                    l.X1 = (con.Node1 as Node).Position.X;
                    l.Y1 = (con.Node1 as Node).Position.Y;
                    l.X2 = (con.Node2 as Node).Position.X;
                    l.Y2 = (con.Node2 as Node).Position.Y;
                    l.Opacity = 0.5;
                    canvasModel.Children.Add(l);
                }
            }

            #endregion
            #region drivers

            foreach (Driver driver in sim.Drivers)
            {
                Ellipse el = new Ellipse();
                el.Width = 6;
                el.Height = 6;
                el.Fill = new SolidColorBrush(Colors.Red);
                el.SetValue(Canvas.LeftProperty, driver.GetAbsolutePosition().X - 3);
                el.SetValue(Canvas.TopProperty, driver.Position.Y - 3);
                el.Opacity = 0.5;
                el.ToolTip = driver.EntityName;
                driver.Shape = el;
                canvasModel.Children.Add(el);
            }

            #endregion
            #region conveyors
            

            Rectangle r = new Rectangle();
            r.Width = 20;
            r.Height = 20;
            r.Stroke = new SolidColorBrush(Colors.Blue);
            r.Opacity = 0.75;

            sim.TestConveyor1.Shape = r;


            r = new Rectangle();
            r.Width = 20;
            r.Height = 20;
            r.Stroke = new SolidColorBrush(Colors.Blue);
            r.Opacity = 0.75;

            sim.TestConveyor2.Shape = r;


            sim.TestConveyor1.Shape.SetValue(Canvas.LeftProperty, sim.TestConveyor1.Position.X);
            sim.TestConveyor1.Shape.SetValue(Canvas.TopProperty, sim.TestConveyor1.Position.Y);
            canvasModel.Children.Add(sim.TestConveyor1.Shape);

            foreach (Item i in sim.TestItems1)
            {
                Ellipse e = new Ellipse();
                e.Width = 8;
                e.Height = 8;
                e.Fill = new SolidColorBrush(Colors.LightBlue);
                e.Opacity = 0.75;
                i.Shape = e;


                i.Shape.SetValue(Canvas.LeftProperty, i.Position.X);
                i.Shape.SetValue(Canvas.TopProperty, i.Position.Y);
                canvasModel.Children.Add(i.Shape);
            }

            sim.TestConveyor2.Shape.SetValue(Canvas.LeftProperty, sim.TestConveyor2.Position.X);
            sim.TestConveyor2.Shape.SetValue(Canvas.TopProperty, sim.TestConveyor2.Position.Y);
            canvasModel.Children.Add(sim.TestConveyor2.Shape);

            foreach (Item i in sim.TestItems2)
            {
                Ellipse e = new Ellipse();
                e.Width = 8;
                e.Height = 8;
                e.Fill = new SolidColorBrush(Colors.LightBlue);
                e.Opacity = 0.75;
                i.Shape = e;

                i.Shape.SetValue(Canvas.LeftProperty, i.Position.X);
                i.Shape.SetValue(Canvas.TopProperty, i.Position.Y);
                canvasModel.Children.Add(i.Shape);
            }

            #endregion
        }

        private void UpdateModelVisualization()
        {
            if (this.CheckAccess()) UpdateUIModel();
            else Dispatcher.Invoke(new Action(UpdateUIModel));
        }

        private void UpdateUIModel()
        {
            foreach (Driver driver in sim.Drivers)
            {
                double left = driver.GetAbsolutePosition().X - 3d;
                double top = driver.GetAbsolutePosition().Y - 3;

                driver.Shape.SetValue(Canvas.LeftProperty, left);
                driver.Shape.SetValue(Canvas.TopProperty, top);
                if (driver.IsDecelerating) driver.Shape.Fill = new SolidColorBrush(Colors.Green);
                else if (driver.IsAccelerating) driver.Shape.Fill = new SolidColorBrush(Colors.Red);
                else driver.Shape.Fill = new SolidColorBrush(Colors.Gray);
            }

            foreach (Item i in sim.TestItems1)
            {
                i.Shape.SetValue(Canvas.LeftProperty, i.Position.X);
                i.Shape.SetValue(Canvas.TopProperty, i.Position.Y);
            }

            foreach (Item i in sim.TestItems2)
            {
                i.Shape.SetValue(Canvas.LeftProperty, i.Position.X);
                i.Shape.SetValue(Canvas.TopProperty, i.Position.Y);
                if (!i.IsAttached) i.Shape.Fill = new SolidColorBrush(Colors.LightGray);
            }

            if (amr != null)
            {
                textSpeed.Text = "Average Speed = " + Math.Round(amr.AverageSpeedInWindow, 1).ToString()
                    + "x - Model : UI = " + Math.Round(amr.FractionOfTimeForModelInWindow * 100, 1).ToString() + "% : " + Math.Round(amr.FractionOfTimeForSyncInWindow * 100, 1).ToString() + "%";
            }
            else textSpeed.Text = "Current Speed = 20";
        }

        #endregion

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // Now try to save the model 
            FileStream fs = new FileStream("DataFile.dat", FileMode.Create);
            // Construct a BinaryFormatter and use it 
            // to serialize the data to the stream.
            EnumerationSurrogateSelector sel = new EnumerationSurrogateSelector();
            //sel.AddSurrogate(

            BinaryFormatter formatter = new BinaryFormatter(sel, new StreamingContext());
            // Serialize the array elements.
            formatter.Serialize(fs, sim);

            fs.Close();
        }

        private void buttonLaod_Click(object sender, RoutedEventArgs e)
        {
            FileStream fs = new FileStream("DataFile.dat", FileMode.Open);
            fs.Position = 0;
            // Construct a BinaryFormatter and use it 
            // to serialize the from to the stream.
            EnumerationSurrogateSelector sel = new EnumerationSurrogateSelector();
            BinaryFormatter formatter = new BinaryFormatter(sel, new StreamingContext());
            sim = (Model.Simulation)formatter.Deserialize(fs);


            fs.Close();

            buttonRun.IsEnabled = false;
            buttonRunAsync.IsEnabled = false;

            canvasModel.Children.Clear();
            CreateUIModel(); 
            timer.Start();


        }

        private void buttonInit_Click(object sender, RoutedEventArgs e)
        {
            sim.InitializeDrivers();
        }
    }

    public class NonSerializableSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                info.AddValue(f.Name, f.GetValue(obj));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                f.SetValue(obj, info.GetValue(f.Name, f.FieldType)); return obj;
        }
    }


    public class EmptySurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
         //   foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
         //       info.AddValue(f.Name, f.GetValue(obj));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {

            return null;
            //  foreach (FieldInfo f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            //      f.SetValue(obj, info.GetValue(f.Name, f.FieldType)); return obj;
            //return null;
        }
    }



    // See http://msdn.microsoft.com/msdnmag/issues/02/09/net/#S3 
    class EnumerationSurrogateSelector : ISurrogateSelector
    {

        public void ChainSelector(ISurrogateSelector selector)
        {
            throw new NotImplementedException(); 
        }

        public ISurrogateSelector GetNextSelector()
        {
            throw new NotImplementedException(); 
        }

        public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            //Generator
            selector = null;

     
                if (type.IsDefined(typeof(SimSerializationContainer), false))
                {
                    selector = this;
                    return new EmptySurrogate();
                }


                // Using reflection.
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(type);  // Reflection

    
              // return new EmptySurrogate();
          


            //if (type.FullName.StartsWith("MatthiasToolbox.Simulation.Templates.Source"))
            //{
            //    System.Diagnostics.Debug.WriteLine(type.FullName);
            //    return new EmptySurrogate();
            //}


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
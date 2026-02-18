using System.Globalization;
using System.Threading;
using System.Windows;

namespace MatthiasToolbox.GraphEditor
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private int r = 0;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //try
            //{//TODO  GraphEditor - Remove try/catch !!!!
            //    this.DispatcherUnhandledException +=
            //        new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(
            //            App_DispatcherUnhandledException);

            //    AppDomain.CurrentDomain.UnhandledException +=
            //        new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine("Unhandled Exception occured!");
            //}
        }

        //void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        //{
        //    Console.Error.WriteLine("Unhandled Exception occured!");//TODO  GraphEditor - Remove try/catch !!!!

        //    e.Handled = true;
        //}


        ////TODO  GraphEditor - Remove try/catch !!!!
        //void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    Interlocked.Increment(ref r); 

        //    Console.WriteLine("handled. {0}", r); 
        //    Console.WriteLine("Terminating " + e.IsTerminating.ToString()); 

        //    Thread.CurrentThread.IsBackground = true; Thread.CurrentThread.Name = "Dead thread"; 
        //    while (true) Thread.Sleep(TimeSpan.FromHours(1));         
        //    //Process.GetCurrentProcess().Kill(); 
        //}
    }
}

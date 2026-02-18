using MatthiasToolbox.EmergencyDepartment.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MatthiasToolbox.Utilities.IO;

namespace MatthiasToolbox.EmergencyDepartment
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			
			Console.SetOut(new RichTextBoxConsoleRedirector(richTextBox1));
		}
		
		void Button1Click(object sender, EventArgs e)
		{
			try {
				richTextBox1.Clear();
				//task A
                MatthiasToolbox.EmergencyDepartment.Model.Simulation sim1 = new MatthiasToolbox.EmergencyDepartment.Model.Simulation(124557, 1);
                //sim1.BuildModel();
				(sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogStart = true;
				sim1.Model.LoggingEnabled = false;
                (sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogFinish = true;
				sim1.Model.Start();
				Console.WriteLine("-------------------------------------------------------");
				
                //DefaultModel.ResetAutoSeeds();
				
				//task B
                MatthiasToolbox.EmergencyDepartment.Model.Simulation sim2 = new MatthiasToolbox.EmergencyDepartment.Model.Simulation(124557, 2);
                //sim2.BuildModel();
                (sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogStart = true;
                sim1.Model.LoggingEnabled = false;
                (sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogFinish = true;
                sim2.Model.Start();
				Console.WriteLine("------------------------------------------------------");
				
                //DefaultModel.ResetAutoSeeds();
				
				//task C
                MatthiasToolbox.EmergencyDepartment.Model.Simulation sim3 = new MatthiasToolbox.EmergencyDepartment.Model.Simulation(124557, 3);
                //sim3.BuildModel();
                (sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogStart = true;
                sim1.Model.LoggingEnabled = false;
                (sim1.Model as MatthiasToolbox.Simulation.Engine.Model).LogFinish = true;
                sim3.Model.Start();
				
                //DefaultModel.ResetAutoSeeds();
				
			} catch(Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EmpaticaE4_Recorder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		void App_Startup(object sender, StartupEventArgs e)
		{
			string PIDNew = "";

			if (e.Args.Length > 0)
			{
				PIDNew = e.Args[0].ToString();
			}

			if (PIDNew == "")
			{
				// Default Parameters
				MainWindow mainWindow = new MainWindow();
				mainWindow.Show();
			}
			else
			{
				// Specific Parameters
				MainWindow mainWindow = new MainWindow("P" + PIDNew);
				mainWindow.Show();
			}
		}
	}
}

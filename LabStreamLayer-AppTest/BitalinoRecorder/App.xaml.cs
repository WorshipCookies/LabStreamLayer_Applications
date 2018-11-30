using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BitalinoRecorder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		void App_Startup(object sender, StartupEventArgs e)
		{
			string PIDNew = "";
			int BitalinoDeviceIndex = -1;


			if(e.Args.Length > 0)
			{
				string[] str = e.Args[0].Split(',');


				if(str.Length > 1)
				{
					PIDNew = str[0].ToString();
					BitalinoDeviceIndex = Convert.ToInt32(str[1]);
				}
				else
				{
					PIDNew = e.Args[0].ToString();
				}	
			}

			if(PIDNew == "")
			{
				// Default Parameters
				MainWindow mainWindow = new MainWindow();
				mainWindow.Show();
			}
			else if(BitalinoDeviceIndex == -1)
			{
				// Specific Parameters
				MainWindow mainWindow = new MainWindow("P" + PIDNew);
				mainWindow.Show();
			}
			else
			{
				// Specific Parameters
				MainWindow mainWindow = new MainWindow("P" + PIDNew, BitalinoDeviceIndex);
				mainWindow.Show();
			}
		}


    }
}

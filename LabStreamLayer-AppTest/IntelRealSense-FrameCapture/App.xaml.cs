using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IntelRealSense_FrameCapture
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		void App_Startup(object sender, StartupEventArgs e)
		{
			string PIDNew = "";

			if(e.Args.Length > 0)
			{
				PIDNew = e.Args[0].ToString();
			}

			if(PIDNew == "")
			{
				// Default Parameters
				CaptureWindow captureWindow = new CaptureWindow();
				captureWindow.Show();
			}
			else
			{
				// Specific Parameters
				CaptureWindow captureWindow = new CaptureWindow("P" + PIDNew);
				captureWindow.Show();
			}
		}
    }
}

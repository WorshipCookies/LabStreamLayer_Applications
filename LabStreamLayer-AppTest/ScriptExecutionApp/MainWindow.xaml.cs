using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace ScriptExecutionApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{



		public MainWindow()
		{
			InitializeComponent();

			// Default File
			if (File.Exists("ExecutionCommands.txt"))
			{
				execFilePath.Text = System.AppDomain.CurrentDomain.BaseDirectory + "ExecutionCommands.txt";
			}
		}

		private void loadExecutionList_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
			
			// Offers the result
			Nullable<bool> result = openFileDlg.ShowDialog();
			
			// Update result only if a file was selected
			if (result == true)
			{
				execFilePath.Text = openFileDlg.FileName;
			}
		}

		private void runButton_Click(object sender, RoutedEventArgs e)
		{
			string line;
			if(execFilePath.Text != "")
			{
				StreamReader execList = new StreamReader(execFilePath.Text);
				while((line = execList.ReadLine()) != null)
				{
					string[] parser = line.Split(',');

					// RUN All Processes
					Process.Start(parser[0],parser[1]);
				}

				execList.Close();
				
				// Shutdown app at the end
				System.Windows.Application.Current.Shutdown();
			}
			
		}
	}
}

using SensingTexAPI;
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
using System.Threading;
using System.Text.RegularExpressions;

namespace SensingTex_PressureMat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MattressDevice matSensor;

        // Characteristics of this particular the Sensing Mat used
        public const int rows = 16;
        public const int cols = 16;
		public const int NODESIZE = 40;

		public static Label[,] pressureNodes = new Label[rows, cols];
		public static int threshold = 0;
		public static bool isDrawing = false;

		public static bool isConnected = false;
		
        public MainWindow()
        {
			InitializeComponent();

			matSensor = new MattressDevice(16, 16, 0.1);
			matSensor.DataReadyEvent += OnDataSensor;

			threshold = 200;
			

			for(int r = 0; r < rows; r++)
			{
				for(int c = 0; c < cols; c++)
				{
					pressureNodes[r, c] = new Label();
					pressureNodes[r, c].Height = NODESIZE;
					pressureNodes[r, c].Width = NODESIZE;
					
					pressureNodes[r, c].Content = "";
					pressureNodes[r, c].Background = Brushes.Black;
					pressureNodes[r, c].Visibility = Visibility.Visible;

					Canvas.SetLeft(pressureNodes[r, c], r * NODESIZE);
					Canvas.SetTop(pressureNodes[r, c], c * NODESIZE);

					pressureNodePanel.Children.Add(pressureNodes[r, c]);
				}
			}
        }

		public void OnDataSensor(object sender, DataReadyEventArgs e)
		{
			if (!MainWindow.isDrawing)
			{
				MainWindow.isDrawing = true;
				Thread paintThread = new Thread(PaintingHeatMap);
				paintThread.Start();
			}
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void PaintingHeatMap(object data)
		{
			
		}

		private void comPortConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (!isConnected)
			{
				try
				{
					matSensor.Open(this.comPortText.Text);
					matSensor.StartDevice();

					if (matSensor.IsOpen())
					{
						MessageBoxResult result = MessageBox.Show("Successfully Connected to Mat",
										  "Sucessfully Connected", MessageBoxButton.OK);
						isConnected = true;
						comPortConnectButton.Content = "Disconnect";
					}
						
					else
					{
						MessageBoxResult result = MessageBox.Show("ERROR: Not Able to Connect",
										  "Connection Unsuccessful", MessageBoxButton.OK);
					}
						
				}
				catch (Exception ex)
				{

				}
			}
			else
			{

			}


		}
	}
}

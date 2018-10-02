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
using LSL;

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
		public static int MAX_VAL = 4096;

		public static bool isDrawing = false;
		public static bool isConnected = false;

		/**
         * Identifying Variables: Process ID; Stream Name; Type of Data; Sampling Rate
         * **/
		private const string guid = "B3CA3876-1C8E-4E20-B855-C52B37D94EF4"; // Unique Process ID -- Pre-Generated

		private string lslStreamName = "SensingMat Streamer";
		private string lslStreamType = "Mat-Signals";
		private int sampling_rate = 100; // Default Value

		private liblsl.StreamInfo lslStreamInfo;
		private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet
		private int lslChannelCount = 256; // Number of Channels to Stream by Default

		private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_double64; // Stream Variable Format


		public MainWindow()
        {
			InitializeComponent();

			// According to the documentation the Sensing Mat is a 16x16 pressure sensor device. 
			// By default the Sampling Rate is 1000Hz, but that is too much so we reduce it to 100Hz (0.1).
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
			// LSL Implementation
			if (lslOutlet != null)
				PushDataToLSL(e.Data);

			if (MainWindow.isDrawing == false)
			{
				MainWindow.isDrawing = true;
				Thread paintThread = new Thread(PaintingHeatMap);
				paintThread.Start(e.Data);
			}
		}

		private void PushDataToLSL(object data)
		{
			// LSL Data Sender
			double[,] dataArray = (double[,])data;
			double[] sample = new double[lslChannelCount];
			int sampleCounter = 0;
			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					sample[sampleCounter] = dataArray[r, c];
					sampleCounter++;
				}
			}
			lslOutlet.push_sample(sample);
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void PaintingHeatMap(object data)
		{
			try
			{
				// Maybe there is a more efficient way of doing this
				double[,] dataArray = (double[,])data;
				if(dataArray != null)
				{
					MainWindow.isDrawing = true;
					double[,] copyData = new double[rows, cols];
					for (int r = 0; r < rows; r++)
					{
						for (int c = 0; c < cols; c++)
						{
							copyData[r, c] = dataArray[r, c];
						}
					}

					for (int r = 0; r < rows; r++)
					{
						for (int c = 0; c < cols; c++)
						{
							ColorizeLocation(r, c, (int)copyData[r, c]);
						}
					}
				}
			}
			catch(Exception ex)
			{
				MainWindow.isDrawing = false;
			}
			MainWindow.isDrawing = false;
		}

		delegate void drawLabelColorEvent(int row, int column, int value);

		private void drawLabelColor(int row, int column, int value)
		{
			pressureNodes[row, column].Content = value.ToString();
			pressureNodes[row, column].Background = CalculateHeatMapColor(threshold, MAX_VAL, value);
		}

		private void ColorizeLocation(int row, int col, int value)
		{
			drawLabelColorEvent dlc = new drawLabelColorEvent(drawLabelColor);
			Dispatcher.Invoke(dlc, new object[] { row, col, value });
		}

		private void comPortConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (!isConnected)
			{
				try
				{
					matSensor.Open("COM" + this.comPortText.Text);
					matSensor.StartDevice();

					if (matSensor.IsOpen())
					{
						// Succesfully Connected
						isConnected = true;
						comPortConnectButton.Content = "Disconnect";

						// LSL KickStart
						if (lslOutlet == null)
						{
							sampling_rate = 100;
							// This is How I Link the Output Stream!
							lslStreamInfo = new liblsl.StreamInfo(lslStreamName + "-" + idTextBox.Content, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + idTextBox.Content);
							lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);
						}
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
				try
				{
					matSensor.StopDevice();
					if (matSensor.Stop())
					{
						isConnected = false;
						// Successfully Disconnected
						comPortConnectButton.Content = "Connect";
					}
					else
					{
						MessageBoxResult result = MessageBox.Show("ERROR: Not Able to Disconnect",
										  "An Unknown Problem Occurred while Disconnecting", MessageBoxButton.OK);
					}
				}
				catch(Exception ex)
				{

				}
			}


		}

		private Brush CalculateHeatMapColor(int low, int high, int value)
		{
			int red = 0;
			int blue = 0;
			int green = 0;
			int valor = (int)((float)value * (float)((float)1024 / (float)high)); //normalizo a 1024 valores de color

			if (value <= threshold)
			{
				red = 0; blue = 0; green = 0;
			}
			else if (value > threshold && valor < 255)
			{
				red = 0; blue = 255; green = valor;
			}
			else if (valor > 255 && valor < 510)
			{
				red = 0; blue = 255 + 255 - valor; green = 255;
			}
			else if (valor > 510 && valor < 765)
			{
				red = valor - 510; blue = 0; green = 255;
			}
			else if (valor > 765)
			{
				red = 255; blue = 0; green = 1020 - valor;
			}
			else
			{
				red = 255; blue = 0; green = 0;
			}
			return new SolidColorBrush(Color.FromArgb((byte)255, (byte)red, (byte)green, (byte)blue));
		}

	}
}

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
using HSVColor;

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
			if (MainWindow.isDrawing == false)
			{
				MainWindow.isDrawing = true;
				Thread paintThread = new Thread(PaintingHeatMap);
				paintThread.Start(e.Data);
			}
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

		public static Color HSVtoRGB(float hue, float saturation, float value, float alpha)
		{
			while (hue > 1f) { hue -= 1f; }
			while (hue < 0f) { hue += 1f; }
			while (saturation > 1f) { saturation -= 1f; }
			while (saturation < 0f) { saturation += 1f; }
			while (value > 1f) { value -= 1f; }
			while (value < 0f) { value += 1f; }
			if (hue > 0.999f) { hue = 0.999f; }
			if (hue < 0.001f) { hue = 0.001f; }
			if (saturation > 0.999f) { saturation = 0.999f; }
			if (saturation < 0.001f) { return Color.FromScRgb(value * 255f, value * 255f, value * 255f, alpha); }
			if (value > 0.999f) { value = 0.999f; }
			if (value < 0.001f) { value = 0.001f; }

			float h6 = hue * 6f;
			if (h6 == 6f) { h6 = 0f; }
			int ihue = (int)(h6);
			float p = value * (1f - saturation);
			float q = value * (1f - (saturation * (h6 - (float)ihue)));
			float t = value * (1f - (saturation * (1f - (h6 - (float)ihue))));
			switch (ihue)
			{
				case 0:
					return Color.FromScRgb(value, t, p, alpha);
				case 1:
					return Color.FromScRgb(q, value, p, alpha);
				case 2:
					return Color.FromScRgb(p, value, t, alpha);
				case 3:
					return Color.FromScRgb(p, q, value, alpha);
				case 4:
					return Color.FromScRgb(t, p, value, alpha);
				default:
					return Color.FromScRgb(value, p, q, alpha);
			}
		}

	}
}

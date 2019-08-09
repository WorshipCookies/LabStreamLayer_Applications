using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using LSL;

namespace BitalinoRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // BITALINO SPECIFIC VARIABLES

        /**
         * Create a Properties Section for Bitalino Device
         * This way we can make sure it is well managed and only enable certain functions once the device is correctly loaded.
         * **/
        private Bitalino _connectedDevice;

        private Bitalino connected_device
        {
            get { return _connectedDevice; }
            set
            {
                _connectedDevice = value;
                if(_connectedDevice != null)
                    OnVariableChange(value);
            }
        }

        /**
         * Event triggers once the device is connected.
         * **/
        private delegate void OnVariableChangeDelegate(Bitalino newValue);
        private event OnVariableChangeDelegate OnVariableChange;

        // MAC Address of the selected Device
        private string macAddress;

        // String buffer to read the obtained signals from the device as it is going to be running in its own thread.
        private static List<string> bufferList = new List<string>();

        // Streaming flag to effectively stop bitalino streaming. 
        private static volatile bool IS_STREAMING = false;

        // Thread that will be streaming data
        private Thread streamingThread;

        // Necessary to delegate content obtained from Bitalino Thread to the UI
        private delegate void UpdateStreamBoxCallback(string text);


        // LAB STREAMING LAYER VARIABLES

        /**
         * Identifying Variables: Process ID; Stream Name; Type of Data; Sampling Rate
         * **/
        private const string guid = "98FF4C8E-5C2D-42E9-8F1B-8505643EAC2C"; // Unique Process ID -- Pre-Generated

        private string lslStreamName = "Bitalino Streamer";
        private string lslStreamType = "Physiological-Signals";
        private int sampling_rate = 0; // Default Value

        private liblsl.StreamInfo lslStreamInfo;
        private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet
        private int lslChannelCount = 0; // Number of Channels to Stream by Default

        private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_int16; // Stream Variable Format

		private bool QuickStart = false;

        public MainWindow()
        {
            InitializeComponent();
            ShowDevices();
            ShowSamplingRates();
            this.Closed += new EventHandler(OnWindowClosing);
            OnVariableChange += EnableStreamingButton;
        }

		public MainWindow(string pIDValue)
		{
			InitializeComponent();
			ShowDevices();
			ShowSamplingRates();
			this.Closed += new EventHandler(OnWindowClosing);
			OnVariableChange += EnableStreamingButton;
			this.idTextBox.Text = pIDValue;

			QuickConnect(0);

			connectButton.IsEnabled = false;
		}

		public MainWindow(string pIDValue, int BitalinoDeviceIndex)
		{
			InitializeComponent();
			ShowDevices();
			ShowSamplingRates();
			this.Closed += new EventHandler(OnWindowClosing);
			OnVariableChange += EnableStreamingButton;
			this.idTextBox.Text = pIDValue;

			QuickConnect(BitalinoDeviceIndex);

			connectButton.IsEnabled = false;
		}



		private void ShowDevices()
        {
            Bitalino.DevInfo[] devices = Bitalino.find();

            foreach(Bitalino.DevInfo d in devices)
            {
                string[] nameCheck = d.name.Split('-');

               if(nameCheck.Length > 0 && nameCheck[0] == "BITalino")                
                    BlinoDeviceList.Items.Add(d.macAddr + "-" + d.name);
                
            }
        }

        private void ShowSamplingRates()
        {
            SamplingRateListBox.Items.Add(10);
            SamplingRateListBox.Items.Add(100);
            SamplingRateListBox.Items.Add(1000);

            SamplingRateListBox.SelectedIndex = 1; // Select the default value of 100 Hz at the beginning.
        }

        private void StreamData(Bitalino dev, liblsl.StreamOutlet lslout, bool ecg, bool eda, bool resp, bool emg, bool egg)
        {
            // Simpler this way. By Default lets just open all channels we assume will be open.
            dev.start(sampling_rate, new int[] { 0, 1, 2, 3, 4 });

            // Data Structure used for Streaming Data
            short[,] sample = new short[100, lslChannelCount]; // Initialize Sample to the number of available Channels and chunked samples we want to send.

            Bitalino.Frame[] frames = new Bitalino.Frame[100];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = new Bitalino.Frame(); // must initialize all elements in the array

            while (MainWindow.IS_STREAMING)
            {

                double start_clock = liblsl.local_clock(); // Record local clock before chunk processing

                dev.read(frames);
                string s = "";
                
                for (int i = 0; i < frames.Length; i++)
                {
                    int auxSampleIndexer = 0; // This variable helps manage the sample array for the various channels that are currently being used.

                    // Manage the ECG Values
                    if (ecg)
                    {
                        //s += frames[i].analog[0];
                        //s += " ";
                        sample[i, auxSampleIndexer] = frames[i].analog[0];

                        auxSampleIndexer += 1;
                    }

                    // Manage the EDA Values
                    if (eda)
                    {
                        //s += frames[i].analog[1];
                        //s += " ";
                        sample[i, auxSampleIndexer] = frames[i].analog[1];

                        auxSampleIndexer += 1;
                    }

                    // Manage the Respiration Values
                    if (resp)
                    {
                        //s += frames[i].analog[2];
                        //s += " ";
                        sample[i, auxSampleIndexer] = frames[i].analog[2];

                        auxSampleIndexer += 1;
                    }

                    if (emg)
                    {
                        sample[i, auxSampleIndexer] = frames[i].analog[3];
                        auxSampleIndexer += 1;
                    }

                    if (egg)
                    {
                        sample[i, auxSampleIndexer] = frames[i].analog[4];
                        auxSampleIndexer += 1;
                    }
                }

                s += "  LENGTH = " + frames.Length;

                // Delegate the Output Values to the Streaming Text Box
                streamingOutputBox.Dispatcher.Invoke(
                    new UpdateStreamBoxCallback(this.AppendStreamTextBox), 
                    new object[] { "Receiving Data: " + s });

                double end_clock = liblsl.local_clock(); // Record local clock after chunk processing

                lslout.push_chunk( sample, (end_clock+start_clock)/2.0 ); // Push the chunk through LabStream Layer using the average time between start and end
            }
            dev.stop();
            streamingOutputBox.Dispatcher.Invoke(
                    new UpdateStreamBoxCallback(this.AppendStreamTextBox),
                    new object[] { "Thread Successfully Closed" });
        }

		private void QuickConnect(int bitalinoDeviceIndex)
		{
			if(BlinoDeviceList.Items.Count > 0)
			{
				BlinoDeviceList.SelectedValue = BlinoDeviceList.Items[bitalinoDeviceIndex];
				connectLSL();
				StreamingProcess();
			}
		}

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
			connectLSL();
        }

		private void connectLSL()
		{
			// Connect user selected device if not device is currently connected
			if (BlinoDeviceList.SelectedValue != null && connected_device == null && lslChannelCount > 0)
			{
				macAddress = BlinoDeviceList.SelectedValue.ToString().Split('-')[0];

				infoOutputBox.Text = "Connecting to Device ... " + macAddress;

				connected_device = new Bitalino(macAddress);

				infoOutputBox.Text = "Device " + macAddress + " Sucessfully Connected.";

				LinkLabStreamingLayer(); // Link to Lab Streaming Layer

			}
			// If a device is already connected warn the user.
			else if (connected_device != null)
			{
				infoOutputBox.Text = "Existing Device (" + macAddress + ") Already Connected";
			}
			else if (lslChannelCount == 0)
			{
				infoOutputBox.Text = "Error! Atleast one stream must be selected in order to start streaming.";
			}
		}

        private void LinkLabStreamingLayer()
        {
            if (lslOutlet == null)
            {
                sampling_rate = (int)SamplingRateListBox.SelectedItem;

                // This is How I Link the Output Stream!
                lslStreamInfo = new liblsl.StreamInfo(lslStreamName + "-" + idTextBox.Text, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + idTextBox.Text);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);

                infoOutputBox.Text += "\nLinked to Lab Streaming Layer\nReady to Stream at " + sampling_rate + " Hz\n" + lslChannelCount + " Channels are Open";
            }
        }

        private void OnWindowClosing(object sender, System.EventArgs e)
        {
            // Need to make sure to disconnect and stop the device once the program is closed for efficiency!
            if(connected_device != null)
            {
                if (IS_STREAMING)
                {
                    connected_device.stop();
                }
                connected_device.Dispose();
            }
        }

        private void EnableStreamingButton(Bitalino device)
        {
            if(device != null)
            {
                startStreamingButton.IsEnabled = true;

                // It is important to disable these options once the bitalino is connected.
                idTextBox.IsReadOnly = true;
                ECGCheck.IsEnabled = false;
                EDACheck.IsEnabled = false;
                RespCheck.IsEnabled = false;
                EMGCheck.IsEnabled = false;
                EGGCheck.IsEnabled = false;
                SamplingRateListBox.IsEnabled = false;
            }
            else
            {
                startStreamingButton.IsEnabled = false;
                idTextBox.IsReadOnly = false;
                ECGCheck.IsEnabled = true;
                EDACheck.IsEnabled = true;
                RespCheck.IsEnabled = true;
                EMGCheck.IsEnabled = true;
                EGGCheck.IsEnabled = true;
                SamplingRateListBox.IsEnabled = true;
            }
        }

        private void AppendStreamTextBox(string textVal)
        {
            this.streamingOutputBox.Text = textVal;
        }

        private void startStreamingButton_Click(object sender, RoutedEventArgs e)
        {
			StreamingProcess();
        }

		private void StreamingProcess()
		{
			if (IS_STREAMING)
			{
				IS_STREAMING = false;
				startStreamingButton.Content = "Start Streaming";
			}
			else if (!IS_STREAMING)
			{
				IS_STREAMING = true;

				// Run Thread
				startStreamingButton.Content = "Stop Streaming";


				bool ecgVal = ECGCheck.IsChecked.Value;
				bool edaVal = EDACheck.IsChecked.Value;
				bool respVal = RespCheck.IsChecked.Value;
                bool emgVal = EMGCheck.IsChecked.Value;
                bool eggVal = EGGCheck.IsChecked.Value;
				streamingThread = new Thread(() => StreamData(connected_device, lslOutlet,
					ecgVal, edaVal, respVal, emgVal, eggVal)); // Pass the connected device argument to the thread

				streamingThread.Start(); // Start Streaming
			}
		}


        private void ECGCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (ECGCheck.IsChecked.Value)
            {
                lslChannelCount += 1;
            }
            else
            {
                lslChannelCount -= 1;
            }
        }

        private void EDACheck_Checked(object sender, RoutedEventArgs e)
        {
            if (EDACheck.IsChecked.Value)
            {
                lslChannelCount += 1;
            }
            else
            {
                lslChannelCount -= 1;
            }
        }

        private void RespCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (RespCheck.IsChecked.Value)
            {
                lslChannelCount += 1;
            }
            else
            {
                lslChannelCount -= 1;
            }
        }

        private void EMG_Checked(object sender, RoutedEventArgs e)
        {
            if (EMGCheck.IsChecked.Value)
            {
                lslChannelCount += 1;
            }
            else
            {
                lslChannelCount -= 1;
            }
        }

        private void EGG_Checked(object sender, RoutedEventArgs e)
        {
            if (EGGCheck.IsChecked.Value)
            {
                lslChannelCount += 1;
            }
            else
            {
                lslChannelCount -= 1;
            }
        }
    }
}

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
        private string lslStreamType = "EMG";
        private double sampling_rate = 100;

        private liblsl.StreamInfo lslStreamInfo;
        private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet
        private int lslChannelCount = 1; // Number of Channels to Stream

        private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_int16; // Stream Variable Format


        public MainWindow()
        {
            InitializeComponent();
            ShowDevices();
            this.Closed += new EventHandler(OnWindowClosing);
            OnVariableChange += EnableStreamingButton;
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

        private void StreamData(Bitalino dev, liblsl.StreamOutlet lslout)
        {
            
            dev.start(100, new int[] { 0 });

            // Data Structure used for Streaming Data
            short[,] sample = new short[lslChannelCount,100]; // Initialize Sample to the number of available Channels and chunked samples we want to send.

            Bitalino.Frame[] frames = new Bitalino.Frame[100];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = new Bitalino.Frame(); // must initialize all elements in the array

            while (MainWindow.IS_STREAMING)
            {
                dev.read(frames);
                string s = "";
                for (int i = 0; i < frames.Length; i++)
                {
                    s += frames[i].analog[0];
                    s += " ";
                    sample[0,i] = frames[i].analog[0];
                }

                s += "  LENGTH = " + frames.Length;
                // Delegate the Output Values to the Streaming Text Box
                streamingOutputBox.Dispatcher.Invoke(
                    new UpdateStreamBoxCallback(this.AppendStreamTextBox), 
                    new object[] { s });

                lslout.push_chunk(sample,liblsl.local_clock()); // Push the Values through Lab Stream Layer
            }
            dev.stop();
            streamingOutputBox.Dispatcher.Invoke(
                    new UpdateStreamBoxCallback(this.AppendStreamTextBox),
                    new object[] { "Thread Successfully Closed" });
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            // Connect user selected device if not device is currently connected
            if(BlinoDeviceList.SelectedValue != null && connected_device == null)
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
        }

        private void LinkLabStreamingLayer()
        {
            if (lslOutlet == null)
            {
                // This is How I Link the Output Stream!
                lslStreamInfo = new liblsl.StreamInfo(lslStreamName, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);

                infoOutputBox.Text += "\nLinked to Lab Streaming Layer -- Ready to Stream";
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
            }
            else
            {
                startStreamingButton.IsEnabled = false;
            }
        }

        private void AppendStreamTextBox(string textVal)
        {
            this.streamingOutputBox.Text = textVal;
        }

        private void startStreamingButton_Click(object sender, RoutedEventArgs e)
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
                streamingThread = new Thread(() => StreamData(connected_device,lslOutlet)); // Pass the connected device argument to the thread
                streamingThread.Start(); // Start Streaming
            }
        }

        

    }
}

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
using LSL;
using System.Threading;

namespace LabStreamLayer_RandomSignalSender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const string guid = "C502E019-5633-4DCE-A954-A897C73DFDB9";

        public string lslStreamName = "Phil-Test Random Signal";
        public string lslStreamType = "Rand-Signal";
        public double sampling_rate = 100;

        private liblsl.StreamInfo lslStreamInfo;
        private liblsl.StreamOutlet lslOutlet = null;
        private int lslChannelCount = 1;

        //Assuming that markers are never send in regular intervalls

        private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_float32;

        private float[] sample;


        private static bool RUNNING_FLAG = false;
        private Thread sigSender;


        public MainWindow()
        {
            InitializeComponent();

            this.Closed += new EventHandler(OnWindowClosing);
        }

        public void SendRandomSignal()
        {
            Random rnd = new Random();
            while (RUNNING_FLAG)
            {
                sample[0] = rnd.Next(0, 10);
                lslOutlet.push_sample(sample);
                Thread.Sleep(10);
            }
        }

        private void lslLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (lslOutlet == null)
            {
                // This is How I Link the Output Stream!
                sample = new float[lslChannelCount]; // Initialize Sample to the number of available Channels

                lslStreamInfo = new liblsl.StreamInfo(lslStreamName, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);


                lslLinkButton.Content = "Output is Linked";

                // Kickstart the Thread
                RUNNING_FLAG = true;
                sigSender = new Thread(SendRandomSignal);
                sigSender.Start();
            }
        }

        private void OnWindowClosing(object sender, System.EventArgs e)
        {
            if (RUNNING_FLAG)
            {
                Console.WriteLine("Stopping Signal Sender Thread.");
                RUNNING_FLAG = false;
                sigSender.Join();
                Console.WriteLine("Signal Sender Thread Stopped.");
            }
            Console.WriteLine("Program Sucessfully Terminated.");
        }
    }
}

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

namespace LabStreamLayer_AppTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private const string guid = "B2038500-3E74-4859-84BB-A9F05B4B4FBF";

        public string lslStreamName = "Phil-Test App";
        public string lslStreamType = "Markers";

        private liblsl.StreamInfo lslStreamInfo;
        private liblsl.StreamOutlet lslOutlet = null;
        private int lslChannelCount = 1;

        //Assuming that markers are never send in regular intervalls
        private double nominal_srate = liblsl.IRREGULAR_RATE;

        private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_string;

        private string[] sample;


        private void startLSLOutputStream()
        {
            if(lslOutlet == null)
            {
                // This is How I Link the Output Stream!
                sample = new string[lslChannelCount]; // Initialize Sample to the number of available Channels

                lslStreamInfo = new liblsl.StreamInfo(lslStreamName, lslStreamType, lslChannelCount, nominal_srate, lslChannelFormat, guid);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);
                

                OutputButton.Content = "Output is Linked";
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            startLSLOutputStream();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // If lslOutlet is sucessfully linked start sending stuff there!
            if(lslOutlet != null)
            {
                sample[0] = textField.Text;
                lslOutlet.push_sample(sample, liblsl.local_clock());
            }
            else
            {
                textField.Text = "Link Outlet Before Sending Messages!";
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
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
using Intel.RealSense;
using LSL;

namespace Intel.RealSense
{
    /**
     * This code will send frame markers to LSL so that videos that are recorded using the tool can be parsed effectively in post-processing.
     * We might need a better protoccol so that too much video is not recorded so that it is more efficient.
     * **/

    public partial class CaptureWindow : Window
    {
        // Intel RealSense Variables
        private Pipeline pipeline;
        private Colorizer colorizer;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();


        // Lab Streaming Layer Variables
        /**
         * Identifying Variables: Process ID; Stream Name; Type of Data; Sampling Rate
         * **/
        private const string guid = "33C3D35A-51FF-44D9-8F05-81E972FA2F62"; // Unique Process ID -- Pre-Generated

        private string lslStreamName = "Intel RealSense Camera";
        private string lslStreamType = "Frame-Markers";
        private double sampling_rate = liblsl.IRREGULAR_RATE; // Default Value

        private liblsl.StreamInfo lslStreamInfo;
        private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet
        private int lslChannelCount = 2; // Number of Channels to Stream by Default

        private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_string; // Stream Variable Format

        private string[] sample; // Data Samples to be Pushed into LSL

        private const string defaultDirectory = "Recordings"; // Where the recordings are Stashed.
        private string fileRecording = "";

        private void UploadImage(Image img, VideoFrame frame)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (frame.Width == 0) return;

                var bytes = new byte[frame.Stride * frame.Height];
                frame.CopyTo(bytes);

                var bs = BitmapSource.Create(frame.Width, frame.Height,
                                  300, 300,
                                  PixelFormats.Rgb24,
                                  null,
                                  bytes,
                                  frame.Stride);

                var imgSrc = bs as ImageSource;

                img.Source = imgSrc;
            }));
        }
        
        public CaptureWindow()
        {
            InitializeComponent();
        }


        /**
         * NOTES 
         * Curently it records immediately after linking the program with LabStreamLayer. 
         * There might be a better solution, but we don't want to increase the number of button presses for the protoccol. It is probably better to record more than to forget pressing 
         * the record button before an experiment. 
         * 
         * **/
        // Code Taken Directly from the LibRealSense 2 Examples -- Captures and Displays Depth and RGB Camera.
        private void startRecordingProcess()
        {
            try
            {
                pipeline = new Pipeline();
                colorizer = new Colorizer();

                var cfg = new Config();
                cfg.EnableStream(Stream.Depth, 640, 480);
                cfg.EnableStream(Stream.Color, 640, 480);


                cfg.EnableRecordToFile(fileRecording);

                pipeline.Start(cfg);


                FrameSet frames;
                DepthFrame depth_frame;
                VideoFrame color_frame;
                VideoFrame colorized_depth;


                var token = tokenSource.Token;

                var t = Task.Factory.StartNew(() =>
                {
                    // Main Loop -- 
                    while (!token.IsCancellationRequested)
                    {
                        frames = pipeline.WaitForFrames();
                        depth_frame = frames.DepthFrame;
                        color_frame = frames.ColorFrame;
                        colorized_depth = colorizer.Colorize(depth_frame);
                        
                        if (lslOutlet != null)
                        {
                            // Do LSL Streaming Here
                            sample[0] = "" + depth_frame.Number + "_" + depth_frame.Timestamp;
                            sample[1] = "" + color_frame.Number + "_" + depth_frame.Timestamp;
                            lslOutlet.push_sample(sample, liblsl.local_clock());
                        }

                        UploadImage(imgDepth, colorized_depth);
                        UploadImage(imgColor, color_frame);

                        // It is important to pre-emptively dispose of native resources
                        // to avoid creating bottleneck at finalization stage after GC
                        // (Also see FrameReleaser helper object in next tutorial)
                        frames.Dispose();
                        depth_frame.Dispose();
                        colorized_depth.Dispose();
                        color_frame.Dispose();
                    }



                }, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }
        }

        private void LinkLabStreamingLayer()
        {
            if (lslOutlet == null)
            {
                sample = new string[lslChannelCount];

                lslStreamInfo = new liblsl.StreamInfo(lslStreamName + "-" + idTextBox.Text, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + idTextBox.Text);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);

                infoTextBox.Text += "Linked to Lab Streaming Layer\nNow Streaming Frame Data\n";
            }
            
            // Once linked no need for the button functionality so disable it.
            lslLink.Content = "Camera is Linked";
            lslLink.IsEnabled = false;

            // Disable the Experiment ID Text Functionality
            idTextBox.IsEnabled = false;

            checkDirectory();

            startRecordingProcess();
        }

        private void checkDirectory()
        {
            if (!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }

            int fileInc = 1;
            fileRecording = defaultDirectory + "\\" + idTextBox.Text + "_" + fileInc + ".bag";

            while (File.Exists(fileRecording))
            {
                fileInc += 1;
                fileRecording = defaultDirectory + "\\" + idTextBox.Text + "_" + fileInc + ".bag";
            }

            infoTextBox.Text += "Recording File = " + fileRecording;

        }


        // Interface Controls Go Here
        private void control_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pipeline.Stop();
            tokenSource.Cancel();
        }

        private void lslLink_Click(object sender, RoutedEventArgs e)
        {
            // Link LabStreamLayer
            LinkLabStreamingLayer();
        }

    }
}
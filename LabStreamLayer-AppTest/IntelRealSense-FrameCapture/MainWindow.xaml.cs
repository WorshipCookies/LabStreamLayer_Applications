using System;
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
using Accord.Video.FFMPEG;
using System.Drawing;
using System.Runtime.InteropServices;

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

		private CustomProcessingBlock processBlock;

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


        private VideoFileWriter vidWriter_Depth;
        private VideoFileWriter vidWriter_Color;
	

        private void UploadImage(System.Windows.Controls.Image img, VideoFrame frame)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (frame.Width == 0) return;

                var bytes = new byte[frame.Stride * frame.Height];
                frame.CopyTo(bytes);

                var bs = BitmapSource.Create(frame.Width, frame.Height,
                                  300, 300,
                                  PixelFormats.Bgr24,
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

		public CaptureWindow(string pIDValue)
		{
			InitializeComponent();
			this.idTextBox.Text = pIDValue;

			LinkLabStreamingLayer();
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
                cfg.EnableStream(Stream.Depth, 640, 480, Format.Z16,30);
                cfg.EnableStream(Stream.Color, 640, 480, Format.Bgr8,30);
				
                //cfg.EnableRecordToFile(fileRecording); // This is now taken care of by FFMPEG
                pipeline.Start(cfg);

				applyRecordingConfig();

				processBlock = new CustomProcessingBlock((f, src) =>
				{
					using (var releaser = new FramesReleaser())
					{
						var frames = FrameSet.FromFrame(f, releaser);

						VideoFrame depth = FramesReleaser.ScopedReturn(releaser, frames.DepthFrame);
						VideoFrame color = FramesReleaser.ScopedReturn(releaser, frames.ColorFrame);

						var res = src.AllocateCompositeFrame(releaser, depth, color);

						src.FramesReady(res);
					}
				});

				processBlock.Start(f =>
				{
					using (var releaser = new FramesReleaser())
					{
						var frames = FrameSet.FromFrame(f, releaser);

						var depth_frame = FramesReleaser.ScopedReturn(releaser, frames.DepthFrame);
						var color_frame = FramesReleaser.ScopedReturn(releaser, frames.ColorFrame);

						var colorized_depth = colorizer.Colorize(depth_frame);

						UploadImage(imgDepth, colorized_depth);
						UploadImage(imgColor, color_frame);

						// Record FFMPEG
						Bitmap bmpColor = new Bitmap(color_frame.Width, color_frame.Height, color_frame.Stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, color_frame.Data);
						vidWriter_Color.WriteVideoFrame(bmpColor);

						Bitmap bmpDepth = new Bitmap(colorized_depth.Width, colorized_depth.Height, colorized_depth.Stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, colorized_depth.Data);
						vidWriter_Depth.WriteVideoFrame(bmpDepth);

						if (lslOutlet != null)
						{
							// Do LSL Streaming Here
							sample[0] = "" + colorized_depth.Number + "_" + colorized_depth.Timestamp;
							sample[1] = "" + color_frame.Number + "_" + color_frame.Timestamp;
							lslOutlet.push_sample(sample, liblsl.local_clock());
						}
					}
				});


                var token = tokenSource.Token;

                var t = Task.Factory.StartNew(() =>
                {
                    // Main Loop -- 
                    while (!token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames())
						{
							processBlock.ProcessFrames(frames);
						}
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

			//applyRecordingConfig();

            startRecordingProcess();
        }

        private void checkDirectory()
        {
            if (!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }

            int fileInc = 1;
            fileRecording = defaultDirectory + "\\" + idTextBox.Text + "_" + fileInc;

			foreach (string f in Directory.GetFiles(defaultDirectory))
			{
				string coreName = f.Split('-')[0];

				if (coreName.Equals(fileRecording))
				{
					fileInc += 1;
					fileRecording = defaultDirectory + "\\" + idTextBox.Text + "_" + fileInc;
				}

			}
            infoTextBox.Text += "Recording File = " + fileRecording;

        }

		private void applyRecordingConfig()
		{
			vidWriter_Depth = new VideoFileWriter();
			vidWriter_Depth.Width = 640;
			vidWriter_Depth.Height = 480;
			vidWriter_Depth.VideoCodec = VideoCodec.H264;
			vidWriter_Depth.VideoOptions["crf"] = "17";
			vidWriter_Depth.VideoOptions["preset"] = "ultrafast";
			vidWriter_Depth.Open(fileRecording + "-Depth.avi");

			vidWriter_Color = new VideoFileWriter();
			vidWriter_Color.Width = 640;
			vidWriter_Color.Height = 480;
			vidWriter_Color.VideoCodec = VideoCodec.H264;
			vidWriter_Color.VideoOptions["crf"] = "17";
			vidWriter_Color.VideoOptions["preset"] = "ultrafast";
			vidWriter_Color.Open(fileRecording + "-Color.avi");
		}

        // Interface Controls Go Here
        private void control_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
			tokenSource.Cancel();
			
			while (pipeline != null)
			{
				pipeline.Stop();
				pipeline = null;
			}
		}

        private void lslLink_Click(object sender, RoutedEventArgs e)
        {
            // Link LabStreamLayer
            LinkLabStreamingLayer();
        }


    }
}

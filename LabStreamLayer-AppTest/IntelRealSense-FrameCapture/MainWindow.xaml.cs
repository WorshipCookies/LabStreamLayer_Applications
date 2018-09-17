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


        // Code Taken Directly from the LibRealSense 2 Examples -- Captures and Displays Depth and RGB Camera. -- 
        // Phil Notes -- Curently it also records automatically, to ease the protoccol probably only start recording once it is linked to LSL...
        public CaptureWindow()
        {
            //Log.ToFile(LogSeverity.Debug, "1.log");

            try
            {
                pipeline = new Pipeline();
                colorizer = new Colorizer();

                var cfg = new Config();
                cfg.EnableStream(Stream.Depth, 640, 480, Format.Z16, 30);
                cfg.EnableStream(Stream.Color, 640, 480, Format.Rgb8, 30);


                cfg.EnableRecordToFile("Test.bag");
                var profile = pipeline.Start(cfg);
                

                var software_dev = new SoftwareDevice();
                var depth_sensor = software_dev.AddSensor("Depth");
                var depth_profile = depth_sensor.AddVideoStream(new VideoStream
                {
                    type = Stream.Depth,
                    index = 0,
                    uid = 100,
                    width = 640,
                    height = 480,
                    fps = 30,
                    bpp = 2,
                    fmt = Format.Z16,
                    intrinsics = (profile.GetStream(Stream.Depth) as VideoStreamProfile).GetIntrinsics()
                });
                var color_sensor = software_dev.AddSensor("Color");
                var color_profile = color_sensor.AddVideoStream(new VideoStream
                {
                    type = Stream.Color,
                    index = 0,
                    uid = 101,
                    width = 640,
                    height = 480,
                    fps = 30,
                    bpp = 2,
                    fmt = Format.Z16,
                    intrinsics = (profile.GetStream(Stream.Color) as VideoStreamProfile).GetIntrinsics()
                });

                // Note about the Syncer: If actual FPS is significantly different from reported FPS in AddVideoStream
                // this can confuse the syncer and prevent it from producing synchronized pairs
                software_dev.SetMatcher(Matchers.Default);

                var sync = new Syncer();

                depth_sensor.Open(depth_profile);
                color_sensor.Open(color_profile);

                depth_sensor.Start(f =>
                {
                    sync.SubmitFrame(f);
                    //Debug.WriteLine("D");
                });
                color_sensor.Start(f => {
                    sync.SubmitFrame(f);
                    //Debug.WriteLine("C");
                });

                var token = tokenSource.Token;

                var t = Task.Factory.StartNew(() =>
                {
                    // Main Loop -- 
                    while (!token.IsCancellationRequested)
                    {
                        var frames = pipeline.WaitForFrames();

                        var depth_frame = frames.DepthFrame;
                        var color_frame = frames.ColorFrame;

                        var bytes = new byte[depth_frame.Stride * depth_frame.Height];
                        depth_frame.CopyTo(bytes);
                        depth_sensor.AddVideoFrame(bytes, depth_frame.Stride, 2, depth_frame.Timestamp,
                            depth_frame.TimestampDomain, (int)depth_frame.Number,
                            depth_profile);

                        bytes = new byte[color_frame.Stride * color_frame.Height];
                        color_frame.CopyTo(bytes);
                        color_sensor.AddVideoFrame(bytes, color_frame.Stride, 2, color_frame.Timestamp,
                            color_frame.TimestampDomain, (int)color_frame.Number,
                            color_profile);
                        
                        depth_frame.Dispose();
                        color_frame.Dispose();
                        frames.Dispose();

                        var new_frames = sync.WaitForFrames();
                        if (new_frames.Count == 2)
                        {
                            depth_frame = new_frames.DepthFrame;
                            color_frame = new_frames.ColorFrame;

                            // If LSL Connected Start Pushing the Frame Data.
                            if (lslOutlet != null)
                            {
                                // Do LSL Streaming Here
                                sample[0] = "" + depth_frame.Number + "_" + depth_frame.Timestamp;
                                sample[1] = "" + color_frame.Number + "_" + depth_frame.Timestamp;
                                lslOutlet.push_sample(sample, liblsl.local_clock());
                            }

                            var colorized_depth = colorizer.Colorize(depth_frame);

                            UploadImage(imgDepth, colorized_depth);
                            UploadImage(imgColor, color_frame);

                            depth_frame.Dispose();
                            colorized_depth.Dispose();
                            color_frame.Dispose();
                        }
                        new_frames.Dispose();
                    }
                }, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }

            InitializeComponent();
        }

        private void LinkLabStreamingLayer()
        {
            if (lslOutlet == null)
            {
                sample = new string[lslChannelCount];

                lslStreamInfo = new liblsl.StreamInfo(lslStreamName + "-" + idTextBox.Text, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + idTextBox.Text);
                lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);

                infoTextBox.Text += "\nLinked to Lab Streaming Layer\nNow Streaming Frame Data";
            }
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

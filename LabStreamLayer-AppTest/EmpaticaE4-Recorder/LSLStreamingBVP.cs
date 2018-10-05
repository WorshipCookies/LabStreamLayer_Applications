using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL;


namespace EmpaticaE4_Recorder
{
	public class LSLStreamingBVP
	{
		/// <summary>
		/// Lab Streaming Layer Variables Following
		/// </summary>
		private const string guid = "2C711D79-4064-46B7-87DC-2F27F07E0E8F"; // Unique Process ID -- Pre-Generated

		private string lslStreamName = "Empatica Streamer(BVP)";
		private string lslStreamType = "BVP-Signals";
		private double sampling_rate = 64.0; // Default Value

		private liblsl.StreamInfo lslStreamInfo;
		private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet
		private int lslChannelCount = 1; // Number of Channels to Stream by Default

		private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_double64; // Stream Variable Format

		// The Constructor Immediately creates a new link between LSL and the application
		public LSLStreamingBVP(string PlayerID)
		{
			if (lslOutlet == null)
			{
				// This is How I Link the Output Stream!
				lslStreamInfo = new liblsl.StreamInfo(lslStreamName + "-" + PlayerID, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + PlayerID);
				lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);
			}
		}

		public void PushSample(double sample)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample);
		}

		public void PushSample(double sample, double timestamp)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample, timestamp);
		}

		public void PushSampleLocalClock(double sample)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample, liblsl.local_clock());
		}
	}
}

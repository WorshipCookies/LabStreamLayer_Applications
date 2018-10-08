using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL;

namespace EmpaticaE4_Recorder
{
	public class LSLStreamer
	{
		private liblsl.StreamInfo lslStreamInfo;
		private liblsl.StreamOutlet lslOutlet = null; // The Streaming Outlet

		public LSLStreamer(string lslStreamName, string lslStreamType, int lslChannelCount, double sampling_rate, liblsl.channel_format_t lslChannelFormat, string guid)
		{
			if(lslOutlet == null)
			{
				// This is How I Link the Output Stream!
				lslStreamInfo = new liblsl.StreamInfo(lslStreamName, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid);
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

		public void PushSample(int sample)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample);
		}

		public void PushSample(int sample, double timestamp)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample, timestamp);
		}

		public void PushSampleLocalClock(int sample)
		{
			double[] lslSample = { sample };
			lslOutlet.push_sample(lslSample, liblsl.local_clock());
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL;

namespace EmpaticaE4_Recorder
{
	public class LSLStreamingGSR : LSLStreamer
	{
		/// <summary>
		/// Lab Streaming Layer Variables Following
		/// </summary>
		private const string guid = "0DBBFDBD-7A92-4300-9C47-9EE5B1D37B57"; // Unique Process ID -- Pre-Generated

		private const string lslStreamName = "Empatica Streamer (GSR)";
		private const string lslStreamType = "GSR-Signals";
		private const double sampling_rate = 4.0; // Default Value

		private const int lslChannelCount = 1; // Number of Channels to Stream by Default

		private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_double64; // Stream Variable Format


		public LSLStreamingGSR(string PlayerID) : base(lslStreamName + "-" + PlayerID, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + PlayerID)
		{
			
		}
	}
}

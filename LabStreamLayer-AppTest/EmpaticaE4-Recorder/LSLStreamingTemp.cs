using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL;

namespace EmpaticaE4_Recorder
{
	public class LSLStreamingTemp : LSLStreamer
	{
		/// <summary>
		/// Lab Streaming Layer Variables Following
		/// </summary>
		private const string guid = "A70ABAC0-2C0C-4572-BDEB-96886474BD60"; // Unique Process ID -- Pre-Generated

		private const string lslStreamName = "Empatica Streamer (Temp)";
		private const string lslStreamType = "Temperature-Signals";
		private const double sampling_rate = 4.0; // Default Value

		private const int lslChannelCount = 1; // Number of Channels to Stream by Default

		private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_double64; // Stream Variable Format


		public LSLStreamingTemp(string PlayerID) : base(lslStreamName + "-" + PlayerID, lslStreamType, lslChannelCount, sampling_rate, lslChannelFormat, guid + "-" + PlayerID)
		{
			
		}
	}
}

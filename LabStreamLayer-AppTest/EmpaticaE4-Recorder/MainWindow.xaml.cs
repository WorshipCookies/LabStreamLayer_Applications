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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LSL;

namespace EmpaticaE4_Recorder
{
	/// <summary>
	/// Parts of this project reuses code obtained from the official Empatica GitHub (https://github.com/empatica/ble-client-windows).
	/// </summary>
	public partial class MainWindow : Window
	{

		/// <summary>
		/// Empatica Variables
		/// </summary>
		// The port number for the remote device. (Using the default values work)
		private string ServerAddress = "127.0.0.1";
		private int ServerPort = 28000;

		// ManualResetEvent instances signal completion.
		private static readonly ManualResetEvent ConnectDone = new ManualResetEvent(false);
		private static readonly ManualResetEvent SendDone = new ManualResetEvent(false);
		private static readonly ManualResetEvent ReceiveDone = new ManualResetEvent(false);

		// The response from the remote device.
		private static String _response = String.Empty;
		private static Queue<String> sendMsgQueue = new Queue<String>();
		private static Queue<String> receiveMsgQueue = new Queue<string>();

		// Socket Info
		private IPHostEntry ipHostInfo;
		private IPAddress ipAddress;
		private IPEndPoint remoteEp;
		private Socket client;

		private static bool isConnected = false;
		private static bool isReceiving = false;
		private static bool empaticaConnected = false;


		// LabStreaming Layer Variables
		private LSLStreamingBVP lslBVPOutlet;

		/// <summary>
		/// UI Interface Variables
		/// </summary>
		private delegate void WriteToTextBoxDelegate(string message);

		public MainWindow()
		{
			InitializeComponent();
			this.Closed += new EventHandler(OnWindowClosing);

			Thread th_sender = new Thread(ConnectEmpaticaServer);
			th_sender.Name = "Sender Thread";
			th_sender.Start();

			Thread th_receiver = new Thread(CheckingServerResponse);
			th_receiver.Name = "Receiver Thread";
			th_receiver.Start();
		}

		private void ConnectDevice_Click(object sender, RoutedEventArgs e)
		{
			if(IDDeviceList.SelectedValue != null && !empaticaConnected)
			{
				string id = (string)IDDeviceList.SelectedValue;
				ConnectEmpaticaDevice(id);
			}
			else if (empaticaConnected)
			{
				DisconnectEmpaticaDevice();
			}
		}

		private void OnWindowClosing(object sender, System.EventArgs e)
		{
			if (isConnected)
			{
				if(empaticaConnected)
					DisconnectEmpaticaDevice();
				isConnected = false;
			}
			if (isReceiving)
			{
				isReceiving = false;
			}
		}
		
		private void WriteToTextBox(string message)
		{
			DataReceivedTextBlock.Text = message;
		}

		private void PushMsgToUI(string str)
		{
			DataReceivedTextBlock.Dispatcher.Invoke(
					new WriteToTextBoxDelegate(WriteToTextBox),
					new object[] { str });
		}

		/// <summary>
		/// Empatica E4 Methods
		/// </summary>
		/// 

		// This is the principal method for connecting to the server and subsequently communicating with it (Send/Receive). 
		private void ConnectEmpaticaServer()
		{
			// Connect to a remote device.
			try
			{
				// Establish the remote endpoint for the socket.
				ipHostInfo = new IPHostEntry { AddressList = new[] { IPAddress.Parse(ServerAddress) } };
				ipAddress = ipHostInfo.AddressList[0];
				remoteEp = new IPEndPoint(ipAddress, ServerPort);

				// Create a TCP/IP socket.
				client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				// Connect to the remote endpoint.
				client.BeginConnect(remoteEp, (ConnectCallback), client);
				ConnectDone.WaitOne();

				string str = "Socket connected to " + client.RemoteEndPoint;
				PushMsgToUI(str);

				isConnected = true;

				GetDeviceList(); // Obtain Current List of Devices
				while (isConnected)
				{
					if(sendMsgQueue.Count > 0)
					{
						string msg = sendMsgQueue.Dequeue();
						Send(client, msg);
						SendDone.WaitOne();
						Receive(client);
						ReceiveDone.WaitOne();
					}
				}
			}
			catch (Exception e)
			{
				PushMsgToUI(e.ToString());
			}
		}

		private void ConnectCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				var client = (Socket)ar.AsyncState;

				// Complete the connection.
				client.EndConnect(ar);

				// Signal that the connection has been made.
				ConnectDone.Set();
			}
			catch (Exception e)
			{
				PushMsgToUI(e.ToString());
			}
		}

		private void Receive(Socket client)
		{
			try
			{
				// Create the state object.
				var state = new StateObject { WorkSocket = client };

				// Begin receiving the data from the remote device.
				client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
			}
			catch (Exception e)
			{
				PushMsgToUI(e.ToString());
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the state object and the client socket 
				// from the asynchronous state object.
				var state = (StateObject)ar.AsyncState;
				var client = state.WorkSocket;

				// Read data from the remote device.
				var bytesRead = client.EndReceive(ar);

				if (bytesRead > 0)
				{
					// There might be more data, so store the data received so far.
					state.Sb.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
					_response = state.Sb.ToString();

					HandleResponseFromEmpaticaBLEServer(_response);

					state.Sb.Clear();

					ReceiveDone.Set();

					// Get the rest of the data.
					client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
				}
				else
				{
					// All the data has arrived; put it in response.
					if (state.Sb.Length > 1)
					{
						_response = state.Sb.ToString();
					}
					// Signal that all bytes have been received.
					ReceiveDone.Set();
				}
			}
			catch (Exception e)
			{
				//PushMsgToUI(e.ToString());
			}
		}

		private void Send(Socket client, String data)
		{
			// Convert the string data to byte data using ASCII encoding.
			byte[] byteData = Encoding.ASCII.GetBytes(data);

			// Begin sending the data to the remote device.
			client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				var client = (Socket)ar.AsyncState;
				// Complete sending the data to the remote device.
				client.EndSend(ar);
				// Signal that all bytes have been sent.
				SendDone.Set();
			}
			catch (Exception e)
			{
				//PushMsgToUI(e.ToString());
			}
		}

		private void HandleResponseFromEmpaticaBLEServer(string response)
		{
			receiveMsgQueue.Enqueue(response);
		}

		private void CheckingServerResponse()
		{
			isReceiving = true;
			while (isReceiving)
			{
				if (receiveMsgQueue.Count > 0)
				{
					ParseEmpaticaResponse(receiveMsgQueue.Dequeue());
				}
			}
		}

		// This function parses the server responses obtained from the Empatica Server
		private void ParseEmpaticaResponse(String msg)
		{
			if (isConnected)
			{
				if (msg != null)
				{
					// Sometimes the Message comes with multiple lines -- Check and parse it effective
					string[] lineParser = msg.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

					foreach(string line in lineParser)
					{
						string[] parser = line.Split(' ');

						// If it is a standard response
						if (parser[0] == "R")
						{
							if (parser[1] == "device_list")
							{
								// Construct a Device List
								BuildVisualListDelegate bv = new BuildVisualListDelegate(BuildVisualList);
								IDDeviceList.Dispatcher.Invoke(bv, new object[] { line });
							}
							else if (parser[1] == "device_connect")
							{
								PushMsgToUI(line);
								MainWindow.empaticaConnected = true;
								UpdateUIOnDeviceConnectDelegate dl = new UpdateUIOnDeviceConnectDelegate(UpdateUIOnDeviceConnect);
								this.Dispatcher.Invoke(dl, new object[] { true });

								// Subscribe to Selected Streams
								InitializeStreamSubscription();
							}
							else if (parser[1] == "device_disconnect")
							{
								PushMsgToUI(line);
								MainWindow.empaticaConnected = false;
								UpdateUIOnDeviceConnectDelegate dl = new UpdateUIOnDeviceConnectDelegate(UpdateUIOnDeviceConnect);
								this.Dispatcher.Invoke(dl, new object[] { false });
							}
							else if (parser[1] == "device_subscribe")
							{
								if (parser[2] == "bvp")
								{
									LSLSubscribe_BVP();
								}
							}
						}
						else
						{
							if (parser[0] == "E4_Bvp")
							{
								// Treat GSR Signal Here
								double timestamp = Convert.ToDouble(parser[1]);
								double bvpValue = Convert.ToDouble(parser[2]);
								lslBVPOutlet.PushSample(bvpValue, timestamp);
								PushMsgToUI(line);
							}
						}
					}
				}
			}
		}

		// Connect/Disconnect to the Empatica Device currently connected to the server.
		private void ConnectEmpaticaDevice(string deviceID)
		{
			string str = "device_connect " + deviceID + Environment.NewLine;
			sendMsgQueue.Enqueue(str);
		}

		// This function serves exclusively to disconnect, in situations where the program crashes or the user exits the Application.
		private void DisconnectEmpaticaDevice()
		{
			string str = "device_disconnect" + Environment.NewLine;
			sendMsgQueue.Enqueue(str);
		}

		// Query Server for Connected Devices
		private void GetDeviceList()
		{
			if (isConnected)
			{
				string str = "device_list" + Environment.NewLine;
				sendMsgQueue.Enqueue(str);
			}
		}

		// Build the List of Devices obtained from the Server into the UI List
		private delegate void BuildVisualListDelegate(string strList);
		private void BuildVisualList(string strList)
		{
			IDDeviceList.Items.Clear();

			string[] parser = strList.Split('|');

			for(int i = 1; i < parser.Length; i++)
			{
				string[] listParser = parser[i].Split(' ');
				
				IDDeviceList.Items.Add(listParser[1]);
				
			}
			if(IDDeviceList.Items.Count > 0)
			{
				IDDeviceList.SelectedIndex = 0;
			}
		}

		// Allows to Refresh the available device list currently connected to the server.
		private void RefreshDeviceListButton_Click(object sender, RoutedEventArgs e)
		{
			GetDeviceList();
		}

		private delegate void UpdateUIOnDeviceConnectDelegate(bool isConnect);
		private void UpdateUIOnDeviceConnect(bool isConnect)
		{
			if (isConnect)
			{
				//ConnectDevice.Content = "Disconnect Device";

				// Due to a bug in the Empatica API, it is better to disable this button completely. 
				// Once a device is disconnected, it raises an exception when trying to reconnect to another
				ConnectDevice.IsEnabled = false; 
				IDDeviceList.IsEnabled = false;
			}
			else
			{
				ConnectDevice.Content = "Connect Device";
				IDDeviceList.IsEnabled = true;
			}
		}

		// SUBSCRIBE STREAM RADIO BUTTON UI

		private void SubscribeStream(string streamType)
		{
			if(streamType == "BVP")
			{
				string s = "device_subscribe bvp ON" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if(streamType == "GSR")
			{
				string s = "device_subscribe gsr ON" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if (streamType == "TMP")
			{
				string s = "device_subscribe tmp ON" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if (streamType == "IBI")
			{
				string s = "device_subscribe ibi ON" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
		}

		private void UnsubscribeStream(string streamType)
		{
			if (streamType == "BVP")
			{
				string s = "device_subscribe bvp OFF" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if (streamType == "GSR")
			{
				string s = "device_subscribe gsr OFF" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if (streamType == "TMP")
			{
				string s = "device_subscribe tmp OFF" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
			else if (streamType == "IBI")
			{
				string s = "device_subscribe ibi OFF" + Environment.NewLine;
				sendMsgQueue.Enqueue(s);
			}
		}

		// This is necessary to ease the initialization. Once the user connects to a device, automatically subscribe to all streams "ticked".
		private void InitializeStreamSubscription()
		{
			if (BVPStreamBox.Dispatcher.Invoke(() => { return BVPStreamBox.IsChecked.Value; }))
			{
				SubscribeStream("BVP");
			}
			if (GSRStreamBox.Dispatcher.Invoke(() => { return GSRStreamBox.IsChecked.Value; }))
			{
				SubscribeStream("GSR");
			}
			if (TempStreamBox.Dispatcher.Invoke(() => { return TempStreamBox.IsChecked.Value; }))
			{
				SubscribeStream("TMP");
			}
			if (IBIStreamBox.Dispatcher.Invoke(() => { return IBIStreamBox.IsChecked.Value; }))
			{
				SubscribeStream("IBI");
			}
		}

		private void SubscribeBVP(object sender, RoutedEventArgs e)
		{
			if (BVPStreamBox.IsChecked.Value)
			{
				if (isConnected && empaticaConnected)
				{
					SubscribeStream("BVP");
				}
			}
			else
			{
				if (isConnected && empaticaConnected)
				{
					UnsubscribeStream("BVP");
				}
			}
		}

		private void SubscribeGSR(object sender, RoutedEventArgs e)
		{
			if (GSRStreamBox.IsChecked.Value)
			{
				if (isConnected && empaticaConnected)
				{
					SubscribeStream("GSR");
				}
			}
			else
			{
				if (isConnected && empaticaConnected)
				{
					UnsubscribeStream("GSR");
				}
			}
		}

		private void SubscribeTemp(object sender, RoutedEventArgs e)
		{
			if (TempStreamBox.IsChecked.Value)
			{
				if (isConnected && empaticaConnected)
				{
					SubscribeStream("TMP");
				}
			}
			else
			{
				if (isConnected && empaticaConnected)
				{
					UnsubscribeStream("TMP");
				}
			}
		}

		private void SubscribeIBI(object sender, RoutedEventArgs e)
		{
			if (IBIStreamBox.IsChecked.Value)
			{
				if (isConnected && empaticaConnected)
				{
					SubscribeStream("IBI");
				}
			}
			else
			{
				if (isConnected && empaticaConnected)
				{
					UnsubscribeStream("IBI");
				}
			}
		}

		// Lab Streaming Layer Functions
		 
		private void LSLSubscribe_BVP()
		{
			lslBVPOutlet = new LSLStreamingBVP(playerIDTextBox.Dispatcher.Invoke(() => { return playerIDTextBox.Text; }));
		}
	}
}

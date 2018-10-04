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
		// The port number for the remote device.
		private string ServerAddress = "127.0.0.1";
		private int ServerPort = 28000;

		// ManualResetEvent instances signal completion.
		private static readonly ManualResetEvent ConnectDone = new ManualResetEvent(false);
		private static readonly ManualResetEvent SendDone = new ManualResetEvent(false);
		private static readonly ManualResetEvent ReceiveDone = new ManualResetEvent(false);

		// The response from the remote device.
		private static String _response = String.Empty;
		private static Queue<String> sendMsgQueue = new Queue<String>();

		// Socket Info
		private IPHostEntry ipHostInfo;
		private IPAddress ipAddress;
		private IPEndPoint remoteEp;
		private Socket client;

		private static bool isConnected = false;
		private static bool empaticaConnected = false;

		/// <summary>
		/// UI Interface Variables
		/// </summary>
		private delegate void WriteToTextBoxDelegate(string message);

		public MainWindow()
		{
			//OnResponseChange += ParseEmpaticaResponse;
			InitializeComponent();
			this.Closed += new EventHandler(OnWindowClosing);

			Thread th = new Thread(connectEmpatica);
			th.Start();
		}

		private void ConnectDevice_Click(object sender, RoutedEventArgs e)
		{
			if(IDDeviceList.SelectedValue != null)
			{
				string id = (string)IDDeviceList.SelectedValue;
				connectEmpaticaDevice(id);
			}
		}

		private void OnWindowClosing(object sender, System.EventArgs e)
		{
			if (isConnected)
			{
				isConnected = false;
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
		private void connectEmpatica()
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

				getDeviceList();
				while (isConnected)
				{
					if(sendMsgQueue.Count > 0)
					{
						string msg = sendMsgQueue.Dequeue();
						Send(client, msg);
						SendDone.WaitOne();
						Receive(client);
						ReceiveDone.WaitOne();

						PushMsgToUI(msg);
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
			PushMsgToUI(response);
			ParseEmpaticaResponse(response);
		}

		private void ParseEmpaticaResponse(String msg)
		{
			//PushMsgToUI(msg);

			if (isConnected)
			{
				string[] parser = msg.Split(' ');

				// If it is a standard response
				if(parser[0] == "R")
				{
					if(parser[1] == "device_list")
					{
						// Construct a Device List
						BuildVisualListDelegate bv = new BuildVisualListDelegate(buildVisualList);
						IDDeviceList.Dispatcher.Invoke(bv, new object[] { msg });
					}
					else if (parser[1] == "device_connect")
					{
						PushMsgToUI(msg);
						empaticaConnected = true;
					}
					else if (parser[1] == "device_connect")
					{
						PushMsgToUI(msg);
						empaticaConnected = false;
					}
				}
			}
		}

		private void getDeviceList()
		{
			if (isConnected)
			{
				string str = "device_list" + Environment.NewLine;
				sendMsgQueue.Enqueue(str);
			}
		}

		private void connectEmpaticaDevice(string deviceID)
		{
			string str;
			if (!empaticaConnected)
			{
				str = "device_connect " + deviceID + Environment.NewLine;
			}
			else
			{
				str = "device_disconnect" + Environment.NewLine;
			}
			sendMsgQueue.Enqueue(str);
		}

		private delegate void BuildVisualListDelegate(string strList);
		private void buildVisualList(string strList)
		{
			string[] parser = strList.Split('|');

			for(int i = 1; i < parser.Length; i++)
			{
				string[] listParser = parser[i].Split(' ');
				
				IDDeviceList.Items.Add(listParser[1]);
				
			}
			IDDeviceList.SelectedIndex = 1;
		}
	}
}

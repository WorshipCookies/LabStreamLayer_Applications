//////////////////////////////
// PST API Windows          //
// Version 1.0              //
// (c) SensingTex - 2015    //
//////////////////////////////

//Windows 
using System;
using System.IO.Ports;
using System.Threading;

namespace SensingTexAPI
{
    //Class for the data associated to DataReadyEvent
	public class DataReadyEventArgs : EventArgs
	{
		public double[,] Data { get; internal set; }

        public DataReadyEventArgs(double[,] data)
		{
			Data = data;
		}
	}
    
	public class MattressDevice
	{
		enum ReadDataState
		{
			COMMAND,
			ROWS_PER_COLUMN,
			COLUMN,
			DATA,
			END_OF_LINE,
			EXIT,
			DATA_READY,
			ERROR}
		;

        //EVENT
        //Raised at each complete reception of data from the SDK
        public event EventHandler<DataReadyEventArgs> DataReadyEvent;

        //serial port for communications with SDK
		private SerialPort port;
        private string portName = "";
        private bool portOpen = false;
        private bool started = false;
        private double Period = 0;
        private double[,] tmpData;
        private double[,] Data;

        //SENSOR
        //number of rows and columns. Usually 16x16 or 8x8
		private int Rows = 0;
		private int Columns = 0;
        //min and max values expected to read
        private double maximoAceptado = 4095;
		private double minimoAceptado = 1;
        public double minValue{
            get{
                return minimoAceptado;
            }set{
                minimoAceptado = value;
            }
        }
        public double maxValue{
            get{
                return maximoAceptado;
            }set{
                maximoAceptado = value;
            }
        }

        //number of columns and rows received from SDK
        public int numRowsRx;
        public int numColsRx;

        //simulation configuration
        private Timer simulationTimer;
        private Timer detectPortTimer;

        
        //CONSTRUCTOR
        //rows: number of rows of the sensor (16 default)
        //columns: number of columns of the sensor (16 default)
        //period: period of data recepction. Accepted values: 0.1, 1, 10 (only useful with SDK Premium)
		public MattressDevice(int rows, int columns, double period)
		{
            //Serial port 
			port = new SerialPort();
			port.DataReceived += OnDataReceived;
			port.ErrorReceived += OnErrorReceived;

            //sensor conf
			Rows = rows;
			Columns = columns;
			Period = period;

            //data structures
			Data = new double[rows, columns];
			tmpData = new double[rows, columns];

            //event configuration
			DataReadyEvent = null;

			return;
		}

        //Sets the min and max values
		public void setMinMax(int min, int max)
		{
			minValue = min;
			maxValue = max;
		}
			
        /// <summary>
        /// Open the serial port.
        /// In SDK Standard it begins the data reception.
        /// In SDK Premium it only opens the port.
        /// </summary>
        /// <param name="portName">Port name in "COMxx" format. For a random data test you can open a "SIM" port, where no SDK is needed.</param>
        /// <returns></returns>
		public bool Open(string portName)
		{
			if (portName != "SIM")
			{
				if (port == null || portName == null)
					return false;

				if (port.IsOpen)
					port.Close();

				port.PortName = portName;
				port.BaudRate = 115200;
				port.Parity = Parity.None;
				port.DataBits = 8;
				port.StopBits = StopBits.One;
				try
				{
					port.Open();
				} catch (Exception e)
				{
					Console.WriteLine("Open Port: " + e.Message);
					return false;
				}

				if (port.IsOpen != true)
				{
					return false;
				}
				
				port.ReadTimeout = 100;
				port.WriteTimeout = 100;
				port.ReceivedBytesThreshold = (4 + 2 * Rows); // 1 row packet size

				Console.WriteLine(port.PortName + " Opened");

				this.Stop();

				if (SendConfiguration() != true)
				{
					port.Close();
					return false;
				}

				port.ReadExisting();

			}

			this.portName = portName;
			portOpen = true;
			this.Start();

			return true;
		}

        //Close the port and stops the data reception
		public void Close()
		{
			portOpen = false;

			if (portName == "SIM")
			{
				this.Stop();
			}
			else
			{
				if (port == null)
					return;

				this.Stop();

				if (port.IsOpen)
					port.Close();

			}

			Console.WriteLine(port.PortName + " Closed");
		}
			
        /// <summary>
        /// Starts the data reception.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <returns>true: accepted. false: denied</returns>
		public bool Start()
		{
			bool tmp;

			if (portName == "SIM")
			{
				StopDevice();
				tmp = StartSimulation();
			}
			else
			{
				StopSimulation();
				tmp = StartDevice();
			}

			return tmp;
		}

        /// <summary>
        /// Stops the data reception.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <returns>true: accepted. false: refused</returns>
		public bool Stop()
		{
			bool tmp;

			if (portName == "SIM")
			{
				StopDevice();
				tmp = StopSimulation();
			}
			else
			{
				StopSimulation();
				tmp = StopDevice();
			}

			return tmp;
		}

        /// <summary>
        /// Starts the data simulation for port "SIM"
        /// </summary>
        /// <returns>always true</returns>
		public bool StartSimulation()
		{
			simulationTimer = new Timer(new TimerCallback(OnSimulationTimerEvent), null, 0, (int) (1000 * Period));
			started = true;

			return true;
		}
        /// <summary>
        /// Stop the data simulation for port "SIM"
        /// </summary>
        /// <returns>always true</returns>
		public bool StopSimulation()
		{
			if (simulationTimer != null)
				simulationTimer.Dispose();
			started = false;

			return true;
		}


        /// <summary>
        /// Send to the SDK the "start sending data" command.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <returns></returns>
		public bool StartDevice()
		{
			detectPortTimer = new Timer(new TimerCallback(OnDetectPortTimerEvent), null, 0, 1000);

			if (port.IsOpen != true)
				return false;

			byte[] buffer = new byte[2];

			buffer[0] = (byte) 'S';
			buffer[1] = (byte) '\r';

			try
			{
				port.Write(buffer, 0, 2);
			} catch (Exception ex)
			{
				Console.WriteLine("Start " + ex.Message);
				return false;
			}

			started = true;

			return true;
		}

        /// <summary>
        /// Send to the SDK the "stop sending data" command.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <returns></returns>
		public bool StopDevice()
		{
			if (detectPortTimer != null)
				detectPortTimer.Dispose();

			if (port.IsOpen != true)
				return false;

			byte[] buffer = new byte[2];

			buffer[0] = (byte) 'X';
			buffer[1] = (byte) '\r';

			try
			{
				port.Write(buffer, 0, 2);
			} catch (Exception ex)
			{
				Console.WriteLine("Stop " + ex.Message);
				return false;
			}

			started = false;

			return true;
		}

        /// <summary>
        /// Informs wheter the SDK is sending data or not.
        /// </summary>
        /// <returns></returns>
		public bool IsStart()
		{
			return started;
		}

        /// <summary>
        /// Sets the sending data period.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
		public bool SetPeriod(double period)
		{
			Period = period;

			if (IsOpen() && IsStart())
			{
				if (portName == "SIM")
				{
					StopSimulation();
					StartSimulation();
				}
				else
				{
					SendConfiguration();
				}
			}
			return true;
		}

        /// <summary>
        /// Sends to the SDK the sensor configuration.
        /// Only useful in SDK Premium. If sended to SDK Standard it has no effect.
        /// </summary>
        /// <returns></returns>
		private bool SendConfiguration()
		{
			byte[] buffer = new byte[6];

			buffer[0] = (byte) 'C';
			buffer[1] = (byte) Rows;
			buffer[2] = (byte) Columns;
			buffer[3] = (byte) ((int) (10 / Period) >> 8);
			buffer[4] = (byte) ((int) (10 / Period));
			buffer[5] = (byte) '\r';

			try
			{
				port.Write(buffer, 0, 6);
			} catch (Exception ex)
			{
				Console.WriteLine("SendConfiguration " + ex.Message);
				return false;
			}

			return true;
		}

        /// <summary>
        /// Returns port name
        /// </summary>
        /// <returns></returns>
		public string GetPortName()
		{
			return port.PortName;
		}

        /// <summary>
        /// Read current available serial ports.
        /// </summary>
        /// <returns>A string array with the name of all available serial ports in the computer.</returns>
		public string[] GetAvailablePorts()
		{
			int i = 0;

			string[] portNames = SerialPort.GetPortNames();

			string[] ports = new string[portNames.Length + 1];

			foreach (string p in portNames)
			{
				ports[i] = p;
				i++;
			}
			ports[i] = "SIM";

			return ports;
		}

        /// <summary>
        /// Returns true if the port is opened.
        /// </summary>
        /// <returns>true: opened. false: closed.</returns>
		public bool IsOpen()
		{
			if (portName == "SIM")
				return portOpen;

			return port.IsOpen;
		}

        /// <summary>
        /// Returns the last received data matrix.
        /// </summary>
        /// <returns>The same data matrix that is sended with the event DataReadyEvent</returns>
		public double[,] GetData()
		{
			return this.Data;
		}

        /// <summary>
        /// Returns the value of a specific point in the matrix
        /// </summary>
        /// <param name="row">row point</param>
        /// <param name="col">column point</param>
        /// <returns>The value of the point</returns>
		public double GetCoordinate(int row, int col)
		{
			double retval;

			if (row >= 1 && row <= Rows && col >= 1 && col <= Columns)
			{
				retval = Data[row - 1, col - 1];
			}
			else
			{
				retval = 0;
			}

			return retval;
		}

        /// <summary>
        /// Translates the value received into a useful 12bit number
        /// </summary>
        /// <param name="val">double read from the SDK</param>
        /// <returns>a 12 bit number. 0:no pressure, 4095: max pressure</returns>
		private double ConvertADCValue(int val)
		{
			double retval;

			retval = (double) (4095 - val);

			return retval;
		}

        /// <summary>
        /// Reads a byte from the serial port
        /// </summary>
        /// <returns></returns>
		private int ReadByte()
		{
			int tmp;

			try
			{
				tmp = (byte) port.ReadByte();
			} catch (Exception)
			{
				return -1;
			}

			return tmp;
		}

        /// <summary>
        /// Receive the information from the SDK and controls the communication and data protocol.
        /// </summary>
        /// <returns></returns>
		private ReadDataState ReadData()
		{
			ReadDataState state = ReadDataState.COMMAND;
			byte rowspercol = 0;
			int col = 0;
			byte count = 0;
			byte bytehigh = 0;
			int adc_value = 0;
			int tmp;
            double tmpDouble;

			if (port.BytesToRead < port.ReceivedBytesThreshold)
			{
				return ReadDataState.EXIT;
			}

			while (true)
			{
				tmp = this.ReadByte();

				//Console.Write(tmp + "-");

				switch (state)
				{
					case ReadDataState.COMMAND:
						if (tmp == 'M')
						{
							count = 0;
							bytehigh = 0;
							state = ReadDataState.ROWS_PER_COLUMN;
						}
						else if (tmp == 'K')
							{
								state = ReadDataState.ERROR;
								Console.WriteLine("SERIAL PROTOCOL INFO: K received");
							}
							else
							{
								state = ReadDataState.ERROR;
								if (tmp != 'H') {
									Console.WriteLine ("SERIAL PROTOCOL ERROR: M not received");
								}
							}
						break;
					case ReadDataState.ROWS_PER_COLUMN:
						rowspercol = (byte) tmp;
						if (tmp == Rows)
						{
							state = ReadDataState.COLUMN;
						}
						else
						{
							state = ReadDataState.ERROR;
							Console.WriteLine("SERIAL PROTOCOL ERROR: rowspercol != Rows");
						}
						break;
					case ReadDataState.COLUMN:
						col = tmp;
						if (tmp < Columns)
						{
							state = ReadDataState.DATA;
						}
						else
						{
							state = ReadDataState.ERROR;
							Console.WriteLine("SERIAL PROTOCOL ERROR: col < Columns");
						}
						break;
					case ReadDataState.DATA:
						if (bytehigh == 0)
						{
							bytehigh = 1;
							adc_value = tmp;
						}
						else
						{
							bytehigh = 0;
							adc_value |= ((int) tmp) << 8;
							tmpDouble = ConvertADCValue(adc_value);
                            if (tmpDouble <= minimoAceptado)
                            {
                                tmpDouble = 0;
                            }
							tmpData [count, col] = tmpDouble;
							
							count++;
							if (count == Rows)
							{
								state = ReadDataState.END_OF_LINE;
							}
							else if (count > Rows)
								{
									state = ReadDataState.ERROR;
									Console.WriteLine("SERIAL PROTOCOL ERROR: count > Rows");
								}
						}
						break;
					case ReadDataState.END_OF_LINE:
						if (tmp == '\n')
						{
							if (col == (Columns - 1))
							{
								numRowsRx = rowspercol;
								numColsRx = col + 1;
								return ReadDataState.DATA_READY;
							}

							if (port.BytesToRead < port.ReceivedBytesThreshold)
							{
								return ReadDataState.EXIT;
							}
							else
							{
								state = ReadDataState.COMMAND;
							}
						}
						else
						{
							state = ReadDataState.ERROR;
							Console.WriteLine("SERIAL PROTOCOL ERROR: \\n not received");
						}
						break;
					case ReadDataState.ERROR:
						if (tmp == '\n')
						{
							state = ReadDataState.COMMAND;
						}
						break;
					default:
						state = ReadDataState.ERROR;
						break;
				}
			}
		}

        /// <summary>
        /// Returns a data matrix all filled with zeros. For debug or init purposes.
        /// </summary>
        /// <returns>0 data matrix</returns>
		public double[,] GetZeroData()
		{
			Double[,] data = new Double[Rows, Columns];

			for (var x = 0; x < Rows; x++)
			{
				for (var y = 0; y < Columns; y++)
				{
					data[x, y] = 0.0;
				}
			}

			return data;
		}

        /// <summary>
        /// Returns a data matrix with random data. Used with the "SIM" port option or debug purposes.
        /// </summary>
        /// <param name="max">maximum number allowed to be used in the random generator.</param>
        /// <returns>a matrix with random value between 0 and max</returns>
		public double[,] GetRandomData(double max)
		{
			Double[,] data = new Double[Rows, Columns];

			Random rand = new Random();
			for (var x = 0; x < Rows; x++)
			{
				for (var y = 0; y < Columns; y++)
				{
					double val = rand.NextDouble();
					data[x, y] = max * val;
				}
			}

			return data;
		}

        /// <summary>
        /// Raises the data event
        /// </summary>
		private void EmitDataReadyEvent()
		{
            //Raise and event with new data available
			if (DataReadyEvent != null)
			{
                Data = new double[Rows, Columns];
                for (int i = 0; i < Rows; i++)
                    for (int j = 0; j < Columns; j++)
                        Data[i, j] = tmpData[i, j];

                DataReadyEventArgs ev = new DataReadyEventArgs(Data);
                DataReadyEvent(this, ev);
			}

			return;
		}

        /// <summary>
        /// It handles the Data Received event from the serial port.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			ReadDataState state;

			if (IsStart() == false)
				return;

			state = ReadData();

			if (state == ReadDataState.DATA_READY)
			{
				EmitDataReadyEvent();
			}
			return;
		}

        /// <summary>
        /// "SIM" port timer event handler
        /// </summary>
        /// <param name="sender"></param>
		protected void OnSimulationTimerEvent(object sender)
		{
			if (portOpen && started && portName == "SIM")
			{
				tmpData = GetRandomData(4096);

				EmitDataReadyEvent();
			}
		}
        /// <summary>
        /// "SIM" port timer event handler
        /// </summary>
        /// <param name="sender"></param>
		protected void OnDetectPortTimerEvent(object sender)
		{
            bool oldstarted;

			if (portOpen && !port.IsOpen)
			{
				oldstarted = started;

				this.Close();
				portOpen = true;
				if (this.Open(portName))
				{
					this.SendConfiguration();
					if (oldstarted)
					{
						this.Start();
					}
				}
			}
		}

		private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
		{
			Console.WriteLine("Serial Port Error Received");
		}

	}
}
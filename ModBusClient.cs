using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

namespace ModBus
{
	public class ModBusClient
	{
		private ushort transactionIdentifier = 0;
		private ushort protocolIdentifier = 0;
		private TcpClient tcpClient;
		private SerialPort serialPort;

		public string SerialPortName;
		public int BaudRate;
		public Parity Parity;
		public int DataBits;
		public StopBits StopBits;
		public string IPAddress;
		public ushort Port;
		public bool IsDebug = false;
		public byte ServerID = 1;
		public ModBusMode Mode;

		public enum ModBusMode
		{
			RTU = 0,
			TCP = 1
		}

		/// <summary>
		/// Creates a new instance of the ModBusClient and sets the default parameters.
		/// The default values are: BaudRate = 19200, Parity = None, DataBits = 8, StopBits = One.
		/// It also sets the Mode of the client to RTU.
		/// </summary>
		/// <param name="SerialPortName">Serial port name.</param>
		public ModBusClient(string SerialPortName)
		{
			this.SerialPortName = SerialPortName;
			this.BaudRate = 19200;
			this.Parity = Parity.None;
			this.DataBits = 8;
			this.StopBits = StopBits.One;
			this.Mode = ModBusMode.RTU;
		}

		/// <summary>
		/// Creates a new instance of the ModBusClient.
		/// If no Port is provided the default (502) is set.
		/// It also sets the Mode of the client to TCP.
		/// </summary>
		/// <param name="IPAddress">IP Address.</param>
		/// <param name="Port">Port.</param>
		public ModBusClient(string IPAddress, ushort Port = 502)
		{
			this.IPAddress = IPAddress;
			this.Port = Port;
			this.Mode = ModBusMode.TCP;
		}

		/// <summary>
		/// Builds the message used for serial transmission.
		/// </summary>
		/// <returns>Message</returns>
		/// <param name="Data">Data</param>
		/// <param name="CRC">CRC</param>
		private byte[] BuildMessageForSerial(byte[] Data, byte[] CRC)
		{
			byte[] _message = new byte[1 + Data.Length + CRC.Length];

			_message[0] = ServerID; // Set the address
			Array.Copy(Data, 0, _message, 1, Data.Length); // Copy the data to the message array, shifted by 1 to not overwrite the ID
			Array.Copy(CRC, 0, _message, 1 + Data.Length, CRC.Length); // Copy the CRC to the end of the message, shifted by 1 to not overwrite the ID

			return _message;
		}

		/// <summary>
		/// Builds the message used for TCP transmission.
		/// </summary>
		/// <returns>Message</returns>
		/// <param name="Data">Data</param>
		private byte[] BuildMessageForTcp(byte[] Data) 
		{
			byte[] _message = new byte[7 + Data.Length];

			var _transactionIdentifier = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(transactionIdentifier) : BitConverter.GetBytes(transactionIdentifier);
			var _protocolIdentifier = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(protocolIdentifier) : BitConverter.GetBytes(protocolIdentifier);
			var _length = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(Convert.ToUInt16(1 + Data.Length)) : BitConverter.GetBytes(Convert.ToUInt16(1 + Data.Length));

			_message[0] = _transactionIdentifier[0]; // Set the HI byte of the TransactionIdentifier
			_message[1] = _transactionIdentifier[1]; // Set the LO byte of the TransactionIdentifier
			_message[2] = _protocolIdentifier[0]; // Set the HI byte of the ProtocolIdentifier
			_message[3] = _protocolIdentifier[1]; // Set the LO byte of the ProtocolIdentifier
			_message[4] = _length[0]; // Set the HI byte of the Length
			_message[5] = _length[1]; // Set the LO byte of the Length
			_message[6] = ServerID;
			Array.Copy(Data, 0, _message, 7, Data.Length);

			return _message;
		}

		/// <summary>
		/// Reads coils from the remote device.
		/// </summary>
		/// <returns>The coils.</returns>
		/// <param name="StartingAddress">Starting address</param>
		/// <param name="QuantityOfCoils">Quantity of coils to read, starting from the starting address</param>
		public byte[] ReadCoils(ushort StartingAddress, ushort QuantityOfCoils) 
		{
			if (StartingAddress < 0x0000 | StartingAddress > 0xFFFF)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			if (QuantityOfCoils < 0x0001 | QuantityOfCoils > 0x7D0)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			var _startingAddress = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(StartingAddress) : BitConverter.GetBytes(StartingAddress);
			var _quantityOfCoils = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(QuantityOfCoils) : BitConverter.GetBytes(QuantityOfCoils);

			var _data = new byte[5];
			_data[0] = 0x01; // Set the FunctionCode
			_data[1] = _startingAddress[0]; // Set the HI byte of the StartingAddress
			_data[2] = _startingAddress[1]; // Set the LO byte of the StartingAddress
			_data[3] = _quantityOfCoils[0]; // Set the HI byte of the QuantityOfCoils
			_data[4] = _quantityOfCoils[1]; // Set the LO byte of the QuantityOfCoils

			if (IsDebug)
			{
				Console.WriteLine($"FunctionCode: {_data[0].ToString("X4")}");
				Console.WriteLine($"StartingAddress HI: {_data[1].ToString("X4")}");
				Console.WriteLine($"StartingAddress LO: {_data[2].ToString("X4")}");
				Console.WriteLine($"QuantityOfCoils HI: {_data[3].ToString("X4")}");
				Console.WriteLine($"QuantityOfCoils LO: {_data[4].ToString("X4")}");
			}

			var _crc = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(ModBusUtilities.CRC16(_data)) : BitConverter.GetBytes((ModBusUtilities.CRC16(_data)));

			if (IsDebug)
			{
				Console.WriteLine($"CRC LO: {_crc[1].ToString("X4")}");
				Console.WriteLine($"CRC HI: {_crc[0].ToString("X4")}");
			}

			// Todo: Build the message according to transport mode (IP, Serial)
			var _message = BuildMessageForTcp(_data);

			// Todo: Send message

			ushort _expectedBytes = (QuantityOfCoils % 8 != 0) ? Convert.ToUInt16(QuantityOfCoils / 8 + 1) : Convert.ToUInt16(QuantityOfCoils / 8);

			// Todo: Read bytes and parse the response

			return null;
		}

		/// <summary>
		/// Reads Discrete Inputs from the device.
		/// </summary>
		/// <returns>The discrete inputs.</returns>
		/// <param name="StartingAddress">Starting address</param>
		/// <param name="QuantityOfInputs">Quantity of inputs to read, starting from the starting address</param>
		public byte[] ReadDiscreteInputs(ushort StartingAddress, ushort QuantityOfInputs)
		{
			if (StartingAddress < 0x0000 | StartingAddress > 0xFFFF)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			if (QuantityOfInputs < 0x0001 | QuantityOfInputs > 0x07D0)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			var _startingAddress = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(StartingAddress) : BitConverter.GetBytes(StartingAddress);
			var _quantityOfCoils = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(QuantityOfInputs) : BitConverter.GetBytes(QuantityOfInputs);

			var _data = new byte[5];
			_data[0] = 0x02; // Set the FunctionCode
			_data[1] = _startingAddress[0]; // Set the HI byte of the StartingAddress
			_data[2] = _startingAddress[1]; // Set the LO byte of the StartingAddress
			_data[3] = _quantityOfCoils[0]; // Set the HI byte of the QuantityOfInputs
			_data[4] = _quantityOfCoils[1]; // Set the LO byte of the QuantityOfInputs

			if (IsDebug)
			{
				Console.WriteLine($"FunctionCode: {_data[0].ToString("X4")}");
				Console.WriteLine($"StartingAddress HI: {_data[1].ToString("X4")}");
				Console.WriteLine($"StartingAddress LO: {_data[2].ToString("X4")}");
				Console.WriteLine($"QuantityOfCoils HI: {_data[3].ToString("X4")}");
				Console.WriteLine($"QuantityOfCoils LO: {_data[4].ToString("X4")}");
			}

			var _crc = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(ModBusUtilities.CRC16(_data)) : BitConverter.GetBytes((ModBusUtilities.CRC16(_data)));

			if (IsDebug)
			{
				Console.WriteLine($"CRC LO: {_crc[1].ToString("X4")}");
				Console.WriteLine($"CRC HI: {_crc[0].ToString("X4")}");
			}



			// Todo: Send message

			ushort _expectedBytes = (QuantityOfInputs % 8 != 0) ? Convert.ToUInt16(QuantityOfInputs / 8 + 1) : Convert.ToUInt16(QuantityOfInputs / 8);

			// Todo: Read bytes and parse the response

			return null;
		}

		/// <summary>
		/// Reads the holding registers from the remote device.
		/// </summary>
		/// <returns>The holding registers.</returns>
		/// <param name="StartingAddress">Starting address</param>
		/// <param name="QuantityOfRegisters">Quantity of registers to read, starting from the starting address</param>
		public byte[] ReadHoldingRegisters(ushort StartingAddress, ushort QuantityOfRegisters)
		{
			if (StartingAddress < 0x0000 | StartingAddress > 0xFFFF)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			if (QuantityOfRegisters < 0x0001 | QuantityOfRegisters > 0x007D)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			var _startingAddress = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(StartingAddress) : BitConverter.GetBytes(StartingAddress);
			var _quantityOfRegisters = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(QuantityOfRegisters) : BitConverter.GetBytes(QuantityOfRegisters);

			var _data = new byte[5];
			_data[0] = 0x01; // Set the FunctionCode
			_data[1] = _startingAddress[0]; // Set the HI byte of the StartingAddress
			_data[2] = _startingAddress[1]; // Set the LO byte of the StartingAddress
			_data[3] = _quantityOfRegisters[0]; // Set the HI byte of the QuantityOfRegisters
			_data[4] = _quantityOfRegisters[1]; // Set the LO byte of the QuantityOfRegisters

			if (IsDebug)
			{
				Console.WriteLine($"FunctionCode: {_data[0].ToString("X4")}");
				Console.WriteLine($"StartingAddress HI: {_data[1].ToString("X4")}");
				Console.WriteLine($"StartingAddress LO: {_data[2].ToString("X4")}");
				Console.WriteLine($"QuantityOfRegisters HI: {_data[3].ToString("X4")}");
				Console.WriteLine($"QuantityOfRegisters LO: {_data[4].ToString("X4")}");
			}

			var _crc = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(ModBusUtilities.CRC16(_data)) : BitConverter.GetBytes((ModBusUtilities.CRC16(_data)));

			if (IsDebug)
			{
				Console.WriteLine($"CRC LO: {_crc[1].ToString("X4")}");
				Console.WriteLine($"CRC HI: {_crc[0].ToString("X4")}");
			}

			// Todo: Send message

			ushort _expectedBytes = Convert.ToUInt16(2 * QuantityOfRegisters);

			// Todo: Read bytes and parse the response

			return null;
		}

		/// <summary>
		/// Reads the input registers from the remote device
		/// </summary>
		/// <returns>The input registers.</returns>
		/// <param name="StartingAddress">Starting address</param>
		/// <param name="QuantityOfRegisters">Quantity of registers to read, starting from the starting address</param>
		public byte[] ReadInputRegisters(ushort StartingAddress, ushort QuantityOfRegisters)
		{
			if (StartingAddress < 0x0000 | StartingAddress > 0xFFFF)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			if (QuantityOfRegisters < 0x0001 | QuantityOfRegisters > 0x007D)
			{
				throw new ArgumentOutOfRangeException(); // Technically unreachable code
			}

			var _startingAddress = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(StartingAddress) : BitConverter.GetBytes(StartingAddress);
			var _quantityOfRegisters = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(QuantityOfRegisters) : BitConverter.GetBytes(QuantityOfRegisters);

			var _data = new byte[5];
			_data[0] = 0x01; // Set the FunctionCode
			_data[1] = _startingAddress[0]; // Set the HI byte of the StartingAddress
			_data[2] = _startingAddress[1]; // Set the LO byte of the StartingAddress
			_data[3] = _quantityOfRegisters[0]; // Set the HI byte of the QuantityOfRegisters
			_data[4] = _quantityOfRegisters[1]; // Set the LO byte of the QuantityOfRegisters

			if (IsDebug)
			{
				Console.WriteLine($"FunctionCode: {_data[0].ToString("X4")}");
				Console.WriteLine($"StartingAddress HI: {_data[1].ToString("X4")}");
				Console.WriteLine($"StartingAddress LO: {_data[2].ToString("X4")}");
				Console.WriteLine($"QuantityOfRegisters HI: {_data[3].ToString("X4")}");
				Console.WriteLine($"QuantityOfRegisters LO: {_data[4].ToString("X4")}");
			}

			var _crc = (BitConverter.IsLittleEndian) ? ModBusUtilities.SwapByteOrder(ModBusUtilities.CRC16(_data)) : BitConverter.GetBytes((ModBusUtilities.CRC16(_data)));

			if (IsDebug)
			{
				Console.WriteLine($"CRC LO: {_crc[1].ToString("X4")}");
				Console.WriteLine($"CRC HI: {_crc[0].ToString("X4")}");
			}

			// Todo: Send message

			ushort _expectedBytes = Convert.ToUInt16(2 * QuantityOfRegisters);

			// Todo: Read bytes and parse the response

			return null;
		}
	}
}

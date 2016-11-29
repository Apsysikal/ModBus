using System;


namespace ModBus
{
	public static class ModBusUtilities
	{
		public static byte[] SwapByteOrder(ushort data)
		{
			var _data = BitConverter.GetBytes(data);
			Array.Reverse(_data);
			return _data;
		}

		public static byte[] SwapByteOrder(uint data)
		{
			var _data = BitConverter.GetBytes(data);
			Array.Reverse(_data);
			return _data;
		}

		public static byte[] SwapByteOrder(ulong data)
		{
			var _data = BitConverter.GetBytes(data);
			Array.Reverse(_data);
			return _data;
		}

		public static ushort CRC16(byte[] data)
		{
			ushort _crc = 0xFFFF;
			for (int pos = 0; pos < data.Length; pos++)
			{
				_crc ^= (ushort)data[pos];

				for (int i = 8; i != 0; i--)
				{
					if ((_crc & 0x0001) != 0)
					{
						_crc >>= 1;
						_crc ^= 0xA001;
					}
					else
					{
						_crc >>= 1;
					}
				}
			}
			return _crc;
		}
	}
}

using System;

namespace ModBus
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine(BitConverter.IsLittleEndian);

			var client = new ModBusClient();
			client.IsDebug = true;
			client.ReadCoils(19, 19);

			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}
	}
}

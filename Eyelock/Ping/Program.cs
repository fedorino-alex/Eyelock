using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	class Program
	{
		static Random m_rnd = new Random();
		static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					bool isTesting = true;
					using (new Eyelock.Service.QueueService(isTesting))
					{
						Console.ReadLine();
					}
				}
				else if (args[0].Trim('\\', '/').ToLower() == "c")
				{
					Type colors = typeof(Eyelock.DeviceAdapter.EyelockDevice.NotificationColor);
					using (Eyelock.DeviceAdapter.EyelockDevice device = new Eyelock.DeviceAdapter.EyelockDevice())
					{
						string[] names = Enum.GetNames(colors);
						do
						{
							string name = names[m_rnd.Next(names.Length)];
							Eyelock.DeviceAdapter.EyelockDevice.NotificationColor color = (Eyelock.DeviceAdapter.EyelockDevice.NotificationColor)Enum.Parse(colors, name);
							Console.WriteLine(string.Format("Notify {0} color", color));
							device.Notify(color);
						}
						while (Console.ReadKey(true).Key != ConsoleKey.Escape);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("Exception: {0}\nStack: {1}", ex.Message, ex.StackTrace));
			}
		}
	}
}

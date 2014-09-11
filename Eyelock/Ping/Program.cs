using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ConsoleApplication1
{
	class Program
	{
		static Random m_rnd = new Random();
		static void Main(string[] args)
		{
			Eyelock.Service.Logger.Info("Main started!");
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
						device.StartTracking();
						string[] names = Enum.GetNames(colors);
						do
						{
							string name = names[m_rnd.Next(names.Length)];
							Eyelock.DeviceAdapter.EyelockDevice.NotificationColor color = (Eyelock.DeviceAdapter.EyelockDevice.NotificationColor)Enum.Parse(colors, name);
							Eyelock.Service.Logger.Info(string.Format("Notify {0} color", color));
							device.Notify(color);
						}
						while (Console.ReadKey(true).Key != ConsoleKey.Escape);
					}
				}
				else if (args[0].Trim('\\', '/').ToLower() == "test")
				{
					using (new Eyelock.Service.QueueService())
					{
						Console.ReadLine();
					}
				}
			}

			catch (Exception ex)
			{
				Eyelock.Service.Logger.Error(ex);
			}
		}
	}
}

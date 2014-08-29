using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eyelock.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var inst = Eyelock.Database.DBManager.Instance;

            using (var device = new Eyelock.DeviceAdapter.Adapter().Connect())
            {
                device.Event += device_Event;
                device.StartTracking();

                Console.Read();
            }
        }

        static void device_Event(object sender, DeviceAdapter.EventTrackedEventArgs e)
        {
            Console.WriteLine("Event " + e.Event.UID.ToString());
        }
    }
}

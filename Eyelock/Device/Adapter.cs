using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eyelock.DeviceAdapter
{
    public class Adapter
    {
		public EyelockDevice Connect()
		{
            EyelockDevice device = new EyelockDevice();
            return device;
		}
    }
}

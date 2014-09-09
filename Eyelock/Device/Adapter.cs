using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eyelock.DeviceAdapter
{
    public class Adapter
    {
		public EyelockDevice Connect(bool isTesting)
		{
			EyelockDevice device = new EyelockDevice(isTesting);
            return device;
		}
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;

namespace Eyelock.Service
{
    public class ConnectionSettings : ConfigurationSection
    {
		private static ConnectionSettings m_Instance;
        public static ConnectionSettings Instance
        { 
            get 
            {
                if (m_Instance == null)
                    m_Instance = (ConnectionSettings)System.Configuration.ConfigurationManager.GetSection("eyelockConnection");

                return m_Instance;
            } 
        }

		public IPAddress DeviceMainPanelIP
		{
			get { return IPAddress.Parse(DeviceMainPanel); }
		}

        [ConfigurationProperty("DeviceMainPanel", IsRequired = true)]
        public string DeviceMainPanel
        {
            get { return (string)base["DeviceMainPanel"]; }
            set { base["DeviceMainPanel"] = value; }
        }

        [ConfigurationProperty("DeviceMainPanelPort", DefaultValue=8081)]
        public int DeviceMainPanelPort
		{
			get { return (int)base["DeviceMainPanelPort"]; }
			set { base["DeviceMainPanelPort"] = value; }
		}

        [ConfigurationProperty("DeviceSlavePanel", IsRequired = true)]
        public string DeviceSlavePanel
        {
            get { return (string)base["DeviceSlavePanel"]; }
            set { base["DeviceSlavePanel"] = value; }
        }

		public IPAddress DeviceSlavePanelIP
		{
			get { return IPAddress.Parse(DeviceSlavePanel); }
		}

        [ConfigurationProperty("DeviceSlavePanelPort", DefaultValue = 8081)]
        public int DeviceSlavePanelPort
        {
            get { return (int)base["DeviceSlavePanelPort"]; }
            set { base["DeviceSlavePanelPort"] = value; }
        }

        [ConfigurationProperty("LocalIP", DefaultValue="0.0.0.0")]
        public string Local
        {
            get { return (string)base["LocalIP"]; }
            set { base["LocalIP"] = value; }
        }

		public IPAddress LocalIP
		{
            get { return IPAddress.Parse(Local); }
		}

        [ConfigurationProperty("LocalPort", DefaultValue=49201)]
		public int LocalPort
		{
			get { return (int)base["LocalPort"]; }
			set { base["LocalPort"] = value; }
		}
    }
}

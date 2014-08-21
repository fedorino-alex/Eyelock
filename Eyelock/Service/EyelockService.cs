using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;

namespace Eyelock.Service
{
    public partial class EyelockWindowsService : ServiceBase
    {
		private ServiceHost m_ServiceHost;

        public EyelockWindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
			if (m_ServiceHost != null)
				m_ServiceHost.Close();

			// Create a ServiceHost for the CalculatorService type and 
			// provide the base address.
			m_ServiceHost = new ServiceHost(typeof(QueueService));

			// Open the ServiceHostBase to create listeners and start 
			// listening for messages.
			m_ServiceHost.Open();
        }

        protected override void OnStop()
        {
			if (m_ServiceHost != null)
			{
				m_ServiceHost.Close();
				m_ServiceHost = null;
			}
        }
    }
}

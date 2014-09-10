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
			Logger.Info("Eyelock Service starting...");
			try
			{
				if (m_ServiceHost != null)
					m_ServiceHost.Close();

				m_ServiceHost = new ServiceHost(typeof(QueueService));
				m_ServiceHost.Open();

				Logger.Info("Eyelock Service started!");
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
				throw;
			}
        }

        protected override void OnStop()
        {
			if (m_ServiceHost != null)
			{
				m_ServiceHost.Close();
				m_ServiceHost = null;
			}

			Logger.Info("Eyelock Service stopped.");
        }
    }
}

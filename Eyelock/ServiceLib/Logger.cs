using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace Eyelock.Service
{
	public class Logger 
	{
		static ILog m_Logger;
		static Logger()
		{
			m_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		}

		public static void Info(string message)
		{
			m_Logger.Info(message);
		}

		public static void Warn(string message)
		{
			m_Logger.Warn(message);
		}

		public static void Error(Exception ex)
		{
			Error(null, ex);
		}

		public static void Error(string message, Exception ex)
		{
			m_Logger.Error(message, ex);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.DeviceAdapter
{
    public class EyelockDevice : IDisposable
    {
		public enum NotificationColor
		{
			None = 10,
			Blue = 11,
			Green = 12,
			LightBlue = 13,
			Red = 16,
			Purple = 17,
			Yellow = 20,
			White = 21
		}

		private Guid m_DeviceID;
        public event EventHandler<EventTrackedEventArgs> Event;

        private StateManager m_StateManager;

		public EyelockDevice()
			: this(false)
		{ }

		internal EyelockDevice(bool isTesting)
		{
			m_DeviceID = Guid.NewGuid();
            m_StateManager = new StateManager(isTesting);
            m_StateManager.Event += OnEvent;
            m_StateManager.Notify(NotificationColor.Green);
		}

        private void OnEvent(object o, EventTrackedEventArgs e)
        {
            if (Event != null)
                Event(this, e);
        }

		public void Notify(NotificationColor color)
		{
			if (m_StateManager != null)
				m_StateManager.Notify(color);
		}

		/// <summary>
		/// Начинаем принимать события с устройсвта
		/// </summary>
        public void StartTracking() 
		{
			if (m_StateManager != null)
				m_StateManager.Start();
		}

        public void EndTracking() 
		{
			if (m_StateManager != null)
	            m_StateManager.Stop();
		}

        public void Dispose()
        {
            if (m_StateManager != null)
            {
                m_StateManager.Event -= OnEvent;
                m_StateManager.Dispose();
            }
        }
    }

    public class EventTrackedEventArgs : EventArgs
    {
        internal EventTrackedEventArgs(Event ev)
        {
            Event = ev;
        }

        public Event Event { get; private set; }
    }
}

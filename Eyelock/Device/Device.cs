using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.DeviceAdapter
{
    public class EyelockDevice : IDisposable
    {
		private Guid m_DeviceID;
        public event EventHandler<EventTrackedEventArgs> Event;

        private StateManager m_StateManager;

		internal EyelockDevice()
		{
			m_DeviceID = Guid.NewGuid();
            m_StateManager = new StateManager();
            m_StateManager.Event += OnEvent;
            m_StateManager.Notify(StateManager.NotificationColor.Green);
		}

        private void OnEvent(object o, EventTrackedEventArgs e)
        {
            if (Event != null)
                Event(this, e);
        }

		/// <summary>
		/// Начинаем принимать события с устройсвта
		/// </summary>
        public void StartTracking() 
		{
            m_StateManager.Start();
		}

        public void EndTracking() 
		{
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

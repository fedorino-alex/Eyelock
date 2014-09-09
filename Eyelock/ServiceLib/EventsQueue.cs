using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Eyelock.Service
{
    class EventsQueue : IDisposable
    {
        private IDictionary<Guid, Event> m_Events;
        private List<Event> m_NewEvents;
        private IList<Event> m_ProcessedEvents;

        #region Timer

        private System.Timers.Timer m_Timer;
        private void UpdateTimer()
        {
            Dispose();
            m_Timer = new System.Timers.Timer();
            m_Timer.Interval = AutoClearTimeout.TotalMilliseconds;
            m_Timer.Elapsed += OnTimerElapsed;
        }
        private void OnTimerElapsed(object sender, EventArgs args)
        {
            UpdateTimer();
            ClearProcessedEvents();
        }

        #endregion

        /// <summary>
        /// Удаляет автоматом обработанные события.
        /// </summary>
        public TimeSpan AutoClearTimeout { get; set; }

        public EventsQueue()
        {
            AutoClearTimeout = TimeSpan.FromHours(1);
			UpdateTimer();

            m_Events = new Dictionary<Guid, Event>();
            m_ProcessedEvents = new List<Event>(); // объекты тут могут не совпадать по адресу с объектами в словаре
            m_NewEvents = new List<Event>();
        }

        public void ClearProcessedEvents()
        {
            foreach (Event ev in m_ProcessedEvents)
            {
                if (ev.Processed)
                    m_Events.Remove(ev.UID);
            }

            m_ProcessedEvents.Clear();
        }

        public void Append(Event ev)
        {
            lock (m_Events)
            {
                if (ev != null)
                {
                    m_Events[ev.UID] = ev;
                    m_NewEvents.Add(ev);
                }
            }

            if (ev != null && ev.Processed)
                m_ProcessedEvents.Add(ev);
        }

        public bool Remove(Event ev)
        {
            if (!ev.Processed)
                return m_Events.Remove(ev.UID);

            return false;
        }

        public void Process(Event ev)
        {
            ev = m_Events[ev.UID];

            if (!ev.Processed)
            {
                ev.Processed = true;
                m_ProcessedEvents.Add(ev);
            }
        }

        /// <summary>
        /// Возвращает все события в очереди.
        /// </summary>
        public List<Event> GetEvents()
        {
            List<Event> result = null;
            lock (m_Events)
            {
                result = new List<Event>(m_Events.Values);
                m_NewEvents.Clear();
            }

            return result;
        }

        public List<Event> GetNewEvents()
        {
            lock (m_Events)
            {
                List<Event> result = new List<Event>(m_NewEvents);
                m_NewEvents.Clear();
                return result;
            }
        }

        public void Dispose()
        {
            if (m_Timer != null)
            {
                if (m_Timer.Enabled)
                    m_Timer.Stop();

                m_Timer.Elapsed -= OnTimerElapsed;
                m_Timer.Dispose();
            }
        }

        public Event this[Guid id]
        {
            get { return m_Events[id]; }
        }
    }
}

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
        private IList<Event> m_ProcessedEvents;

        #region Timer

        private System.Timers.Timer m_Timer;
        private void UpdateTimer()
        {
            Dispose();
            m_Timer = new System.Timers.Timer();
            m_Timer.Interval = AutoClearTimeout.TotalMilliseconds;
            m_Timer.Elapsed += OnTimerElapsed;

            if (AutoClear)
                m_Timer.Start();
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
        public bool AutoClear { get; set; }
        public TimeSpan AutoClearTimeout { get; set; }

        public EventsQueue()
        {
            AutoClear = true;
            AutoClearTimeout = TimeSpan.FromHours(1);

            m_Events = new Dictionary<Guid, Event>();
            m_ProcessedEvents = new List<Event>(); // объекты тут могут не совпадать по адресу с объектами в словаре
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
                    m_Events[ev.UID] = ev;
            }

            if (ev.Processed)
                m_ProcessedEvents.Add(ev);
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
        public Event[] GetEvents()
        { 
            Event[] result = null;
            lock (m_Events)
            {
                result = new Event[m_Events.Count];
                m_Events.Values.CopyTo(result, 0);
            }

            return result;
        }

        public Event[] GetEvents(DateTime timestamp)
        {
            List<Event> result = new List<Event>();
            lock (m_Events)
            {
                foreach (var ev in m_Events.Values.Reverse())
                {
                    if (timestamp <= ev.Created)
                        result.Add(ev);
                    else
                        break;
                }
            }

            result.Reverse();
            return result.ToArray();
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

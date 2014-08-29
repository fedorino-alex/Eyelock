using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Eyelock.DeviceAdapter;

namespace Eyelock.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
	public class QueueService : IQueueService, IDisposable
	{
        private EventsQueue m_Queue;
        private EyelockDevice m_Device;

        public QueueService()
        {
            m_Queue = new EventsQueue();
            m_Device = new Adapter().Connect();
            m_Device.Event += OnDeviceEvent;
        }

        private ServiceResult<T> ProcessWithSafeContext<T>(Func<T> process)
		{
			try
			{
				return Success<T>(process());
			}
			catch (Exception ex)
			{
				return Fail<T>(ex);
			}
		}

		#region Public API

		public ServiceResult<bool> ProcessEvent(Event ev)
		{
			return ProcessWithSafeContext<bool>(() => ProcessEventImpl(ev));
		}

        public ServiceResult<Event[]> GetAllEvents()
		{
			return ProcessWithSafeContext<Event[]>(GetAllEventsImpl);
		}

        public ServiceResult<Event[]> GetNewEvents(DateTime timestamp)
		{
			return ProcessWithSafeContext<Event[]>(() => GetNewEventsImpl(timestamp));
		}

		#endregion 

		#region Results

		public ServiceResult<T> Success<T>(T result)
		{
			return ServiceResult<T>.Success(result);
		}
		public ServiceResult<T> Fail<T>(string message)
		{
			return ServiceResult<T>.Fail(message);
		}
		public ServiceResult<T> Fail<T>(Exception ex)
		{
			return ServiceResult<T>.Fail(ex);
		}

		#endregion

		private Event[] GetAllEventsImpl()
		{
            return m_Queue.GetEvents();
		}

		private Event[] GetNewEventsImpl(DateTime timestamp)
		{
            return m_Queue.GetEvents(timestamp);
		}

		private bool ProcessEventImpl(Event ev)
		{
			if (ev != null)
			{
                if (ev.Processed)
                    return true;

				switch (ev.Type)
				{
					case EventFactory.ADD:
						return ProcessAddEvent(ev);
					case EventFactory.VIEW:
						return ProcessUpdateEvent(ev);
					default:
						throw new EventException(ev, string.Format("Не найден метод обработки [Type = \"{0}\"]", ev.Type));
				}
			}

			throw new EventException(ev, "Отсутствует событие для обработки");
		}

		private bool ProcessAddEvent(Event ev)
		{
			try
			{
				// код на добавление события в БД

                m_Queue.Process(ev);
				return true;
			}
			catch (Exception ex)
			{
				throw new EventException(ev, "Ошибка при добавлении события.", ex);
			}
		}

		private bool ProcessUpdateEvent(Event ev)
		{
			try
			{
				// код на обновление события
                m_Queue.Process(ev);
				return true;
			}
			catch (Exception ex)
			{
				throw new EventException(ev, "Ошибка при обновлении события.", ex);
			}
		}

        void OnDeviceEvent(object sender, EventTrackedEventArgs e)
        {
            m_Queue.Append(e.Event);
        }

        public void Dispose()
        {
            if (m_Queue != null)
                m_Queue.Dispose();
            if (m_Device != null)
                m_Device.Event -= OnDeviceEvent;
        }
    }
}

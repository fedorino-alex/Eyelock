using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Eyelock.Database;
using Eyelock.DeviceAdapter;

namespace Eyelock.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
	public class QueueService : IQueueService, IDisposable
	{
        private EventsQueue m_Queue;
        public EyelockDevice m_Device;

        public QueueService()
            : this(false)
        { }

        public QueueService(bool test = false)
        {
			Init(test);
        }

		private void Init(bool isTest)
		{
			if (m_Queue == null || m_Device == null)
			{
				m_Device = new Adapter().Connect(isTest);
				m_Queue = new EventsQueue();
				m_Device.Event += OnDeviceEvent;
			}

            m_Device.StartTracking();
		}

        private ServiceResult<T> ProcessWithSafeContext<T>(Func<T> process)
		{
			try
			{
				return Success<T>(process());
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
				return Fail<T>(ex);
			}
		}

		#region Public API

		public ServiceResult<bool> ProcessEvent(Event ev)
		{
			return ProcessWithSafeContext<bool>(() => ProcessEventImpl(ev));
		}

        public ServiceResult<bool> RemoveEvent(Event ev)
        {
            return ProcessWithSafeContext<bool>(() => RemoveEventImpl(ev));
        }

        public ServiceResult<List<Event>> GetAllEvents()
		{
			return ProcessWithSafeContext<List<Event>>(GetAllEventsImpl);
		}

        public ServiceResult<List<Event>> GetNewEvents()
		{
            return ProcessWithSafeContext<List<Event>>(GetNewEventsImpl);
		}

		public ServiceResult<List<User>> Find(string first, string last)
		{
            return ProcessWithSafeContext<List<User>>(() => FindImpl(first, last));
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

        private List<Event> GetAllEventsImpl()
		{
            return m_Queue.GetEvents();
		}

		private List<Event> GetNewEventsImpl()
		{
            return m_Queue.GetNewEvents();
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

        private bool RemoveEventImpl(Event ev)
        {
            if (ev != null)
            {
                ev = m_Queue[ev.UID];
                if (!ev.Processed)
                    return m_Queue.Remove(ev);
            }

            return false;
        }

		private List<User> FindImpl(string first, string last)
		{
			return DBManagerFactory.GetManager().GetUser(first, last);
		}

		private bool ProcessAddEvent(Event ev)
		{
			try
			{
                ev = ToInternalEvent(ev);

				DBManagerFactory.GetManager().AddUser(ev);
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
                ev = ToInternalEvent(ev);

				DBManagerFactory.GetManager().UpdateUser(ev);
                m_Queue.Process(ev);
				return true;
			}
			catch (Exception ex)
			{
				throw new EventException(ev, "Ошибка при обновлении события.", ex);
			}
		}

        private Event ToInternalEvent(Event ev)
        {
            var userData = ev.User;
            ev = m_Queue[ev.UID];

            ev.User.FirstName = userData.FirstName;
            ev.User.LastName = userData.LastName;
            ev.User.DateOfBirth = userData.DateOfBirth;

            return ev;
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
			{
				m_Device.StartTracking();
				m_Device.Event -= OnDeviceEvent;
				m_Device.Dispose();
			}
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Eyelock.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
	public class QueueService : IQueueService
	{
		public ServiceResult<bool> ProcessEvent(Event ev)
		{
			try
			{
				return Success<bool>(true);
			}
			catch (Exception ex)
			{
				return Fail<bool>(ex);
			}
		}

		public ServiceResult<Event[]> GetAllEvents()
		{
			try
			{
				return Success<Event[]>(null);
			}
			catch (Exception ex)
			{
				return Fail<Event[]>(ex);
			}
		}

		public ServiceResult<Event[]> GetNewEvents(DateTime timestamp)
		{
			try
			{
				return Success<Event[]>(null);
			}
			catch (Exception ex)
			{
				return Fail<Event[]>(ex);
			}
		}

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
	}
}

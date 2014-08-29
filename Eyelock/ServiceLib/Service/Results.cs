using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Eyelock.Service
{
	[DataContract]
	public class ServiceResult<T>
	{
		[DataMember]
		public bool IsSuccess { get; set; }
		[DataMember]
		public T Result { get; set; }
		[DataMember]
		public string ErrorMessage { get; set; }
		[DataMember]
		public Guid Event { get; set; }

		public static ServiceResult<T> Success(T result)
		{
			return new ServiceResult<T>() { IsSuccess = true, Result = result };
		}
		public static ServiceResult<T> Success(T result, string message)
		{
			var ret = ServiceResult<T>.Success(result);
			ret.ErrorMessage = message;
			return ret;
		}
		public static ServiceResult<T> Fail(string message)
		{
			return new ServiceResult<T>() { IsSuccess = false, ErrorMessage = message };
		}
		public static ServiceResult<T> Fail(Exception ex)
		{
			var result = Fail(ex.Message);
			if (ex is EventException)
			{
				var ev = ((EventException)ex).FailedEvent;
				if (ev != null)
					result.Event = ev.UID;
			}

			return result;
		}
	}
}

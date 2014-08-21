using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Eyelock.Service
{
    [DataContract]
    public class Event
    {
        [DataMember(IsRequired = true, Name = "uid")]
        public Guid UID { get; set; }

        [DataMember(IsRequired=true, Name="event")]
        public string Type { get; set; }
        
        [DataMember(IsRequired=true, Name="processed")]
        public bool Processed { get; set; }

        [DataMember(IsRequired=true, Name="title")]
        public string Title { get; set; }

        [DataMember(IsRequired=true, Name="data")]
        public User User { get; set; }

        [DataMember(IsRequired=true, Name="timestamp")]
        public string Timestamp { get; set; }
    }

    static class EventFactory
    {
        const string ADD_EVENT_TITLE = "Добавление профиля";
        const string VIEW_EVENT_TITLE = "Просмотр профиля";
        const string ADD = "add";
        const string VIEW = "view";

        public static Event GetAddEvent()
        {
			return new Event() { Title = ADD_EVENT_TITLE, Type = ADD, Timestamp = DateTime.UtcNow.ToString("o"), UID = Guid.NewGuid() };
        }

        public static Event GetViewEvent()
        {
            return new Event() { Title = VIEW_EVENT_TITLE, Type = VIEW, Timestamp = DateTime.UtcNow.ToString("o"), UID = Guid.NewGuid() };
        }
    }

    [DataContract]
    public class User
    {
        [DataMember(IsRequired = true, Name = "uid")]
        public Guid UID { get; set; }

        [DataMember(IsRequired=true, Name="firstName")]
        public string FirstName { get; set; }

        [DataMember(IsRequired = true, Name = "lastName")]
        public string LastName { get; set; }

        [DataMember(IsRequired = true, Name = "dob")]
        public string DateOfBirth { get; set; }

        [DataMember(IsRequired = true, Name = "leftIris")]
        public string LeftIris { get; set; }

        [DataMember(IsRequired = true, Name = "rightIris")]
        public string RightIris { get; set; }
    }

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

    public class BoolResult : ServiceResult<bool>
    { }

    public class EventsResult : ServiceResult<Event[]>
    { }
}
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
        public DateTime Timestamp { get; set; }
    }

    static class EventFactory
    {
        const string ADD_EVENT_TITLE = "Добавление профиля";
        const string VIEW_EVENT_TITLE = "Добавление профиля";
        const string ADD = "add";
        const string VIEW = "view";

        public static Event GetAddEvent()
        {
            return new Event() { Title = ADD_EVENT_TITLE, Type = ADD };
        }

        public static Event GetViewEvent()
        {
            return new Event() { Title = VIEW_EVENT_TITLE, Type = VIEW };
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
        public bool IsSuccess { get; set; }
        public T Result { get; set; }
        public string ErrorMessage { get; set; }

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
            return new ServiceResult<T>() { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
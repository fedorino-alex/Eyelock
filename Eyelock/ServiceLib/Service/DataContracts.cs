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

        public DateTime Created { get; set; }

        public Eyelock.Eye.Sorting.Eye LeftEye { get; set; }
        public Eyelock.Eye.Sorting.Eye RightEye { get; set; }
    }

    public static class EventFactory
    {
        const string ADD_EVENT_TITLE = "Добавление профиля";
        const string VIEW_EVENT_TITLE = "Просмотр профиля";
        public const string ADD = "add";
        public const string VIEW = "view";

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
}
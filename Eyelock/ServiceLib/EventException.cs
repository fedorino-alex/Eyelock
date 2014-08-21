using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eyelock.Service
{
	class EventException : Exception
	{
		public Event FailedEvent { get; private set; }

		public EventException(Event failed, string message, Exception innerException) 
			: base(message, innerException)
		{
			FailedEvent = failed;
		}

		public EventException(Event failed, string message)
			: this(failed, message, null)
		{ }
	}
}

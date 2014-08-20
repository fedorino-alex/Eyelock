using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Eyelock.Service
{
    [ServiceContract]
    public interface IQueueService
    {
        [OperationContract(Action="ProcessEvent")]
        ServiceResult<bool> ProcessEvent(Event ev);

        [OperationContract(Action = "GetAllevents")]
        ServiceResult<Event[]> GetAllEvents();

        [OperationContract(Action = "GetNewEvents")]
        ServiceResult<Event[]> GetNewEvents(DateTime timestamp);
    }
}

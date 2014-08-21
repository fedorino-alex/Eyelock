using System;
using System.Web.Script.Services;
using System.Web.Services;
using BoolResult = Eyelock.UI.EyelockService.ServiceResultOfboolean;
using EventsResult = Eyelock.UI.EyelockService.ServiceResultOfArrayOfEvent_PgLz_PdTx;

namespace Eyelock.UI.Service
{
    /// <summary>
    /// Сводное описание для QueueService
    /// </summary>
    [WebService]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ScriptService]
    public class QueueService : System.Web.Services.WebService
    {
		/// <summary>
		/// Поскольку WebService не может быть синглтоном.
		/// </summary>
		private static DateTime? m_LastUpdateTimestamp = null; 
        private T ProcessWithServiceContext<T>(Func<EyelockService.IQueueService, T> callback, bool updateTimestamp = true)
        {
            Context.Response.ContentType = "application/json";
            using (EyelockService.QueueServiceClient clientConnection = new EyelockService.QueueServiceClient())
            {
                if (clientConnection.State == System.ServiceModel.CommunicationState.Created)
                {
                    clientConnection.Open();
                    if (clientConnection.State == System.ServiceModel.CommunicationState.Opened)
                    {
                        var result = callback(clientConnection);
						if (updateTimestamp)
							m_LastUpdateTimestamp = DateTime.UtcNow;
						return result;
                    }
                }
            }

            return default(T);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public BoolResult ProcessEvent(Eyelock.UI.EyelockService.Event ev)
        {
            return ProcessWithServiceContext(service => service.ProcessEvent(ev), false);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public EventsResult GetAllEvents()
        {
			return ProcessWithServiceContext(service => service.GetAllEvents());
		}

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public EventsResult GetNewEvents() 
        {
			var method = m_LastUpdateTimestamp.HasValue ? 
				(Func<EyelockService.IQueueService, EventsResult>)(service => service.GetNewEvents(m_LastUpdateTimestamp.Value)) : 
				(Func<EyelockService.IQueueService, EventsResult>)(service => service.GetAllEvents());
			return ProcessWithServiceContext(method);
		}
    }
}

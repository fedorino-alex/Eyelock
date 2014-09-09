using Eyelock.UI.EyelockService;
using System;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using BoolResult = Eyelock.UI.EyelockService.ServiceResultOfboolean;
using EventsResult = Eyelock.UI.EyelockService.ServiceResultOfArrayOfEvent_PgLz_PdTx;
using UsersResult = Eyelock.UI.EyelockService.ServiceResultOfArrayOfUser_PgLz_PdTx;
using System.Linq;

namespace Eyelock.UI.Service
{
    /// <summary>
    /// Сводное описание для QueueService
    /// </summary>
    [WebService]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class QueueService : System.Web.Services.WebService
    {
		/// <summary>
		/// Поскольку WebService не может быть синглтоном.
		/// </summary>
		private static DateTime? m_LastUpdateTimestamp = null;
        private static JavaScriptSerializer m_Json = new JavaScriptSerializer();

        private T ProcessWithServiceContext<T>(Func<EyelockService.IQueueService, T> callback, bool updateTimestamp = true)
        {
            Context.Response.ContentType = "application/json";
            using (EyelockService.QueueServiceClient clientConnection = new EyelockService.QueueServiceClient())
            {
                if (clientConnection.State == System.ServiceModel.CommunicationState.Created)
                {
                    clientConnection.Open();
                    if (clientConnection.State == System.ServiceModel.CommunicationState.Opened)
                        return callback(clientConnection);
                }
            }

            return default(T);
        }

		[WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public BoolResult ProcessEvent(string obj)
		{
            var ev = m_Json.Deserialize<Event>(obj);
            return ProcessWithServiceContext(service => service.ProcessEvent(ev), false);
		}

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public BoolResult RemoveEvent(string obj)
        {
            var ev = m_Json.Deserialize<Event>(obj);
            return ProcessWithServiceContext(service => service.RemoveEvent(ev), false);
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
			return ProcessWithServiceContext(service => service.GetNewEvents());
		}

		[WebMethod]
		[ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
		public UsersResult Find(string first, string last)
		{
			return ProcessWithServiceContext(service => service.Find(first, last));
		}
    }
}

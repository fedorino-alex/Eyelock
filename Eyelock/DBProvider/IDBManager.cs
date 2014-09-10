using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.Database
{
	public static class DBManagerFactory
	{
		public static IDBManager GetManager()
		{
			return new SqlDBManager();
		}
	}

	public interface IDBManager
	{
		List<IrisLite> GetIrisLite();
		IrisLite GetIrisLite(Guid uid);
		List<User> GetUser();
		User GetUser(Guid uid);
		List<Iris> GetIris();
		Iris GetIris(Guid uid);

		List<Service.User> GetUser(string first, string last);
		void AddUser(Event addEvent);
		void UpdateUser(Event viewEvent);
		Service.User GetUser(int index, out Iris iris);

		DataBaseCache Cache { get; }
	}
}

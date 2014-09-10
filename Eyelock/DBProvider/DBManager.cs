using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using Eyelock.Service;
using System.Data;

namespace Eyelock.Database
{
	abstract class DBManagerBase : IDBManager
	{
		public DataBaseCache Cache { get; private set; }

		public DBManagerBase(DataBaseCache cache)
		{
			Cache = cache;
		}

		public abstract List<IrisLite> GetIrisLite();
		public abstract IrisLite GetIrisLite(Guid uid);
		public abstract List<User> GetUser();
		public abstract User GetUser(Guid uid);
        public abstract User GetUser(int index, out Iris iris);
        public abstract List<Iris> GetIris();
		public abstract Iris GetIris(Guid uid);

		public abstract List<User> GetUser(string first, string last);
		public abstract void AddUser(Event addEvent);
		public abstract void UpdateUser(Event viewEvent);
	}

    static class Extensions
    {
        public static SqlParameter AddWithValue(this SqlParameterCollection collection, string name, object value, SqlDbType type)
        {
            SqlParameter param = collection.Add(name, type);
            param.Value = value;

            return param;
        }
    }

	class SqlDBManager : DBManagerBase
	{
		private const string WHERE = " WHERE [UID] = @UID";
		private const string SELECT_IRISLITE = "SELECT [UID],[Code_LL],[Code_LR],[Code_RR],[Code_RL] FROM [IrisLite]";
		private const string SELECT_IRIS = "SELECT [UID],[UserID],[Image_LL],[Image_Display_LL],[Image_RL],[Image_Display_RL],[Image_LR],[Image_Display_LR],[Image_RR],[Image_Display_RR],[Type],[StorageType] FROM [Iris]";
		private const string SELECT_USER = "SELECT [UID],[FirstName],[LastName],[DateOfBirth],[Created],[Modified] FROM [User]";
		private const string UPDATE_USER = "UPDATE [User] SET [FirstName]=@FirstName,[LastName]=@LastName,[DateOfBirth]=@DateOfBirth,[Modified]=@Modified";
		private const string INSERT_USER = "INSERT INTO [User]([UID],[FirstName],[LastName],[DateOfBirth],[Created],[Modified])VALUES(@UserUID,@FirstName,@LastName,@DateOfBirth,@Created,@Modified);";
		private const string INSERT_IRIS = "INSERT INTO [Iris]([UID],[UserID],[Image_LL],[Image_Display_LL],[Image_RL],[Image_Display_RL],[Image_LR],[Image_Display_LR],[Image_RR],[Image_Display_RR],[Type],[StorageType])VALUES(@IrisUID,@UserID,@Image_LL,@Image_Display_LL,@Image_RL,@Image_Display_RL,@Image_LR,@Image_Display_LR,@Image_RR,@Image_Display_RR,@Type,@StorageType);";
		private const string INSERT_IRISLITE = "INSERT INTO [IrisLite]([UID],[Code_LL],[Code_LR],[Code_RR],[Code_RL])VALUES(@IrisLiteUID,@Code_LL,@Code_LR,@Code_RR,@Code_RL);";

		class SqlConnectionWrapper : IDisposable
		{
			private SqlConnection m_Connection;

			public SqlConnectionWrapper(SqlConnection connection)
			{
				m_Connection = connection;
				if (m_Connection != null && m_Connection.State == System.Data.ConnectionState.Closed)
					m_Connection.Open();
			}

			public void Dispose()
			{
				if (m_Connection != null && m_Connection.State != System.Data.ConnectionState.Closed)
					m_Connection.Close();
			}
		}

		SqlConnection m_Connection;

		private T ProcessWithinConnection<T>(Func<SqlCommand, T> process)
		{
			using (new SqlConnectionWrapper(m_Connection))
			{
				using (var command = m_Connection.CreateCommand())
				{
					command.CommandType = System.Data.CommandType.Text;
					return process(command);
				}
			}
		}
		private void ProcessWithinConnection(Action<SqlCommand> process)
		{
			using (new SqlConnectionWrapper(m_Connection))
			{
				SqlTransaction transaction = m_Connection.BeginTransaction();
				try
				{
					using (var command = m_Connection.CreateCommand())
					{
						command.CommandType = System.Data.CommandType.Text;
						command.Transaction = transaction;

						process(command);
					}

					transaction.Commit();
				}
				catch 
				{
					transaction.Rollback();
					throw;
				}
				finally
				{
					transaction.Dispose();
				}
			}
		}

		public SqlDBManager()
			: base(DataBaseCache.Instance)
		{
			m_Connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["sqlConnectionString"].ConnectionString);
            if (!Cache.IsLoaded)
                Cache.Load(this);
		}

		public override List<IrisLite> GetIrisLite()
		{
			return ProcessWithinConnection<List<IrisLite>>(command =>
			{
				command.CommandText = SELECT_IRISLITE;
				List<IrisLite> result = new List<IrisLite>();

				using (var reader = command.ExecuteReader())
				{
					if (reader.HasRows)
					{
						while (reader.Read())
							result.Add(GetIrisLite(reader));
					}
				}

				return result;
			});
		}

		public override IrisLite GetIrisLite(Guid uid)
		{
			return ProcessWithinConnection<IrisLite>(command =>
			{
				command.CommandText = string.Concat(SELECT_IRISLITE, WHERE);
				command.Parameters.AddWithValue("UID", uid);

				IrisLite result = null;

				using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SingleRow))
				{
                    if (reader.HasRows && reader.Read())
                        result = GetIrisLite(reader);
				}

				return result;
			});
		}

		public override List<User> GetUser()
		{
			return ProcessWithinConnection<List<User>>(command =>
			{
				command.CommandText = SELECT_USER;
				List<User> result = new List<User>();

				using (var reader = command.ExecuteReader())
				{
					if (reader.HasRows)
					{
						while (reader.Read())
							result.Add(GetUser(reader));
					}
				}

				return result;
			});
		}

		public override User GetUser(Guid uid)
		{
			return ProcessWithinConnection<User>(command =>
			{
				command.CommandText = string.Concat(SELECT_USER, WHERE);
				command.Parameters.AddWithValue("UID", uid);

				User result = null;
				using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SingleRow))
				{
					if (reader.HasRows && reader.Read())
                        result = GetUser(reader);
				}

				return result;
			});
		}

        public override List<User> GetUser(string first, string last)
        {
            return ProcessWithinConnection<List<Service.User>>(command =>
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Concat(SELECT_USER, " WHERE "));
                if (!string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                    sb.Append("[FirstName] LIKE ('%' + @first + '%')");
                else if (string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
                    sb.Append("[LastName] LIKE ('%' + @last + '%')");
                else
                    sb.Append("[FirstName] LIKE ('%' + @first + '%') AND [LastName] LIKE ('%' + @last + '%')");

                command.CommandText = sb.ToString();

                command.Parameters.AddWithValue("first", first);
                command.Parameters.AddWithValue("last", last);

                List<Service.User> result = new List<Service.User>();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                            result.Add(GetUser(reader));
                    }
                }

                foreach(var user in result)
                {
                    command.Parameters.Clear();
                    command.CommandText = "SELECT * FROM [Iris] WHERE [UserID] = @UID";
                    command.Parameters.AddWithValue("UID", user.UID);

                    using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.HasRows && reader.Read())
                        {
                            Iris iris = GetIris(reader);
                            user.LeftIris = Eyelock.Database.ConvertTools.ToBase64(iris.Image_LL);
                            user.RightIris = Eyelock.Database.ConvertTools.ToBase64(iris.Image_RR);
                        }
                    };
                }

                return result;
            });
        }

        public override User GetUser(int index, out Iris iris)
        {
            Guid irisId = Cache.IrisData[index].UID;
            iris = GetIris(irisId);
            return GetUser(iris.UserID);
        }

		public override List<Iris> GetIris()
		{
			return ProcessWithinConnection<List<Iris>>(command =>
			{
				command.CommandText = SELECT_IRIS;
				List<Iris> result = new List<Iris>();

				using (var reader = command.ExecuteReader())
				{
					if (reader.HasRows)
					{
						while (reader.Read())
                            result.Add(GetIris(reader));
					}
				}

				return result;
			});
		}

		public override Iris GetIris(Guid uid)
		{
			return ProcessWithinConnection<Iris>(command =>
			{
				command.CommandText = string.Concat(SELECT_IRIS, WHERE);
				command.Parameters.AddWithValue("UID", uid);

				Iris result = null;
				using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SingleRow))
				{
					if (reader.HasRows && reader.Read())
                        result = GetIris(reader);
				}

				return result;
			});
		}

        private Iris GetIris(SqlDataReader reader)
        {
            System.Data.SqlTypes.SqlInt32 sqlInt = System.Data.SqlTypes.SqlInt32.Null;
            System.Data.SqlTypes.SqlBytes sqlBytes = null;

            var iris = new Iris();

            iris.UID = reader.GetGuid(0);
            iris.UserID = reader.GetGuid(1);
			iris.Image_LL = !(sqlBytes = reader.GetSqlBytes(2)).IsNull ? sqlBytes.Value : null;
			iris.Image_Display_LL = !(sqlBytes = reader.GetSqlBytes(3)).IsNull ? sqlBytes.Value : null;
            iris.Image_RL = !(sqlBytes = reader.GetSqlBytes(4)).IsNull ? sqlBytes.Value : null;
            iris.Image_Display_RL = !(sqlBytes = reader.GetSqlBytes(5)).IsNull ? sqlBytes.Value : null;
			iris.Image_LR = !(sqlBytes = reader.GetSqlBytes(6)).IsNull ? sqlBytes.Value : null;
            iris.Image_Display_LR = !(sqlBytes = reader.GetSqlBytes(7)).IsNull ? sqlBytes.Value : null;
            iris.Image_RR = !(sqlBytes = reader.GetSqlBytes(8)).IsNull ? sqlBytes.Value : null;
            iris.Image_Display_RR = !(sqlBytes = reader.GetSqlBytes(9)).IsNull ? sqlBytes.Value : null;
			iris.Type = !(sqlInt = reader.GetSqlInt32(10)).IsNull ? sqlInt.Value : (int?)null;
            iris.StorageType = reader.GetInt16(11);

            return iris;
        }

        private User GetUser(SqlDataReader reader)
        {
            var user = new User();
            user.UID = reader.GetGuid(0);
            user.FirstName = reader.GetString(1);
            user.LastName = reader.GetString(2);
            user.DateOfBirth = reader.GetDateTime(3).ToString("dd-MM-yyyy");

            return user;
        }

        private IrisLite GetIrisLite(SqlDataReader reader)
        {
            System.Data.SqlTypes.SqlBytes sqlBytes = null;

            var iris = new IrisLite();
            iris.UID = reader.GetGuid(0);

            iris.Code_LL = !(sqlBytes = reader.GetSqlBytes(1)).IsNull ? sqlBytes.Value : null;
            iris.Code_LR = !(sqlBytes = reader.GetSqlBytes(2)).IsNull ? sqlBytes.Value : null;
			iris.Code_RR = !(sqlBytes = reader.GetSqlBytes(3)).IsNull ? sqlBytes.Value : null;
			iris.Code_RL = !(sqlBytes = reader.GetSqlBytes(4)).IsNull ? sqlBytes.Value : null;

            return iris;
        }

		public override void AddUser(Event addEvent)
		{
            List<User> existsUsers = GetUser(addEvent.User.FirstName, addEvent.User.LastName);

			ProcessWithinConnection(command =>
			{
				DateTime now = DateTime.Now;
				Guid userId = Guid.NewGuid(), irisId = Guid.NewGuid();
                bool addUser = true;
                if (existsUsers != null && existsUsers.Count > 0)
                {
                    userId = existsUsers[0].UID;
                    addUser = false;
                }

				// вставить пользователя, вставить LiteIris, вставить Iris
				StringBuilder sb = new StringBuilder();
                if (addUser)
				    sb.AppendLine(INSERT_USER);
				sb.AppendLine(INSERT_IRIS);
				sb.AppendLine(INSERT_IRISLITE);

				command.CommandText = sb.ToString();
                if (addUser)
                {
                    command.Parameters.AddWithValue("UserUID", userId);
                    command.Parameters.AddWithValue("FirstName", addEvent.User.FirstName);
                    command.Parameters.AddWithValue("LastName", addEvent.User.LastName);
                    command.Parameters.AddWithValue("DateOfBirth", DateTime.ParseExact(addEvent.User.DateOfBirth, "dd-MM-yyyy", CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("Created", now);
                    command.Parameters.AddWithValue("Modified", now);
                }

				command.Parameters.AddWithValue("IrisLiteUID", irisId);
				command.Parameters.AddWithValue("Code_LL", addEvent.LeftEye.Code, SqlDbType.Image);
				command.Parameters.AddWithValue("Code_LR", addEvent.LeftEye.Mask, SqlDbType.Image);
				command.Parameters.AddWithValue("Code_RR", addEvent.RightEye.Code, SqlDbType.Image);
				command.Parameters.AddWithValue("Code_RL", addEvent.RightEye.Mask, SqlDbType.Image);

				command.Parameters.AddWithValue("IrisUID", irisId);
				command.Parameters.AddWithValue("UserID", userId);
                command.Parameters.AddWithValue("Image_LL", addEvent.LeftEye.Data, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_Display_LL", DBNull.Value, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_RL", DBNull.Value, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_Display_RL", DBNull.Value, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_LR", DBNull.Value, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_Display_LR", DBNull.Value, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_RR", addEvent.RightEye.Data, SqlDbType.Image);
                command.Parameters.AddWithValue("Image_Display_RR", DBNull.Value, SqlDbType.Image);
				command.Parameters.AddWithValue("Type", DBNull.Value);
				command.Parameters.AddWithValue("StorageType", 1);

				command.ExecuteNonQuery();

				var newIrisLite = new IrisLite();
				newIrisLite.UID = irisId;
				newIrisLite.Code_LL = addEvent.LeftEye.Code;
				newIrisLite.Code_LR = addEvent.LeftEye.Mask;
				newIrisLite.Code_RR = addEvent.RightEye.Code;
				newIrisLite.Code_RL = addEvent.RightEye.Mask;

				Cache.Append(newIrisLite, addEvent.LeftEye.Code, addEvent.LeftEye.Mask, addEvent.RightEye.Code, addEvent.RightEye.Mask);
			});
		}

		public override void UpdateUser(Event updateEvent)
		{
			ProcessWithinConnection(command =>
			{
				command.CommandText = string.Concat(UPDATE_USER, WHERE);
				command.Parameters.AddWithValue("FirstName", updateEvent.User.FirstName);
				command.Parameters.AddWithValue("LastName", updateEvent.User.LastName);
				command.Parameters.AddWithValue("DateOfBirth", DateTime.ParseExact(updateEvent.User.DateOfBirth, "dd-MM-yyyy", CultureInfo.InvariantCulture));
				command.Parameters.AddWithValue("Modified", DateTime.Now);
				command.Parameters.AddWithValue("UID", updateEvent.User.UID);

				command.ExecuteNonQuery();
			});
		}

	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.Database
{
    public class DBManager
    {
		private static System.Data.SqlClient.SqlConnectionStringBuilder m_CSBuilder;
		public static System.Data.SqlClient.SqlConnectionStringBuilder ConnectionStringBuilder
		{
			get
			{
				if (m_CSBuilder == null)
					m_CSBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder();

				m_CSBuilder.Clear();
				return m_CSBuilder;
			}
		}

        private static DBManager m_Instance;
        public static DBManager Instance
        {
            get 
            {
                if (m_Instance == null)
                    m_Instance = new DBManager();

                return m_Instance;
            }
        }

        private EyelockDBEntities m_Model;
        
        private List<Iris> m_IrisData; // список с поиском по индексу
        private List<byte[]> m_LeftIrises;  // коллекция кодов левых глаз
        private byte[] m_LeftIrisesArray;   // то же самое, но в более удобном виде
        private List<byte[]> m_RightIrises; // коллекция кодов правых глах
        private byte[] m_RightIrisesArray;  // то же самое, но в более удобном виде

        private DBManager()
        {
            m_Model = new EyelockDBEntities();
            InitIrises();
        }

        private void InitIrises()
        {
            m_IrisData = m_Model.Iris.ToList();
            m_LeftIrises = m_IrisData.Select(i => i.Code_LL).ToList();
            m_RightIrises = m_IrisData.Select(i => i.Code_RR).ToList();

            int total = m_LeftIrises.Sum(bytes => bytes.Length);
            m_LeftIrisesArray = new byte[total];
            int index = 0;
            foreach (byte[] bytes in m_LeftIrises)
            {
                Array.Copy(bytes, 0, m_LeftIrisesArray, index, bytes.Length);
                index += bytes.Length;
            }

            total = m_RightIrises.Sum(bytes => bytes.Length);
            m_RightIrisesArray = new byte[total];
            index = 0;
            foreach (byte[] bytes in m_RightIrises)
            {
                Array.Copy(bytes, 0, m_RightIrisesArray, index, bytes.Length);
                index += bytes.Length;
            }
        }

		public Eyelock.Service.User GetUserByIndex(int index)
		{
            if (index > 0 && index < m_IrisData.Count)
            {
                Guid userID = m_IrisData[index].UID;
                return ConvertTools.User(
                    m_Model.User
                        .Include("Iris")
                        .Where(u => u.UID == userID)
                        .First());
            }

            return null;
		}

		public byte[] GetLeftIrises(out int count)
		{
            count = m_LeftIrises.Count;
			return m_LeftIrisesArray;
		}

		public byte[] GetRightIrises(out int count)
		{
            count = m_RightIrises.Count;
            return m_RightIrisesArray;
        }

		// 1. коллекция кодов всех глаз
		// 2. связь с пользователем по индексу
		// 3. первый раз запрашивается коллекция глаз, строится большой массив

    }
}

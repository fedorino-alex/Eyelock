using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.Database
{
    [Serializable]
	public class DataBaseCache
	{
		private static DataBaseCache m_Instance;
		public static DataBaseCache Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = new DataBaseCache();

				return m_Instance;
			}
		}

		private DataBaseCache()
		{ }

		public bool IsLoaded { get; private set; }

		public List<IrisLite> IrisData { get; private set; } // список с поиском по индексу

		public byte[] LeftIrisesArray { get; private set; }   // то же самое, но в более удобном виде
		public byte[] RightIrisesArray { get; private set; }  // то же самое, но в более удобном виде

		public byte[] GetLeftIrises(out int count)
		{
			count = IrisData.Count;
			return LeftIrisesArray;
		}
		public byte[] GetRightIrises(out int count)
		{
			count = IrisData.Count;
			return RightIrisesArray;
		}

		public void Load(IDBManager manager)
		{
			IrisData = manager.GetIrisLite();
			LeftIrisesArray = GetIrises(i => i.Code_LL, i => i.Code_LR);
			RightIrisesArray = GetIrises(i => i.Code_RR, i => i.Code_RL);

			IsLoaded = true;
		}

		public void Append(IrisLite irisLite, byte[] leftEye, byte[] leftEyeMask, byte[] rightEye, byte[] rightEyeMask)
		{
            IrisData.Add(irisLite);
            LeftIrisesArray = AppendEye(leftEye, leftEyeMask, LeftIrisesArray);
            RightIrisesArray = AppendEye(rightEye, rightEyeMask, RightIrisesArray);
		}

        private byte[] AppendEye(byte[] code, byte[] mask, byte[] eyesBytes)
        {
            int index = eyesBytes.Length;
			Array.Resize<byte>(ref eyesBytes, eyesBytes.Length + code.Length + mask.Length);
			Array.Copy(code, 0, eyesBytes, index, code.Length);
			Array.Copy(mask, 0, eyesBytes, index + code.Length, mask.Length);

            return eyesBytes;
        }

		private byte[] GetIrises(Func<IrisLite, byte[]> code, Func<IrisLite, byte[]> mask)
		{
			using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
			{
				for (int i = 0; i < IrisData.Count; i++)
				{
					var bytes = code(IrisData[i]);
					ms.Write(bytes, 0, bytes.Length);
					bytes = mask(IrisData[i]);
					ms.Write(bytes, 0, bytes.Length);
				}

				return ms.ToArray();
			}
		}

		public void Clear()
		{
			IrisData.Clear();
			LeftIrisesArray = null;
			RightIrisesArray = null;

			IsLoaded = false;
		}
	}
}

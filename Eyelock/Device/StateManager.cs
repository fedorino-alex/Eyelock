using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Eyelock.Service;
using GRIVideoManagerSDKNet;

namespace Eyelock.DeviceAdapter
{
	class StateManager : IDisposable
	{
		public StateManager(bool isTesting)
		{
			var endPoint = new System.Net.IPEndPoint(ConnectionSettings.Instance.DeviceMainPanelIP, ConnectionSettings.Instance.DeviceMainPanelPort);
			System.Net.Sockets.TcpClient test = null;
			try
			{
				test = new System.Net.Sockets.TcpClient();
				test.Connect(endPoint);

				m_Device = new Net.Video.Device(endPoint);
				m_Device.SetSecureMode(false);
				if (isTesting || m_Device.ProcessMode != Net.Video.Device.EyeLockProcessMode.EyeLockCamera)
					m_Device.SetProcessMode(Net.Video.Device.EyeLockProcessMode.EyeLockCamera, ConnectionSettings.Instance.LocalPort);
				m_Device.ProcessModeAcknowledged += OnAcknowledged;
				m_Device.ProcessModeException += OnException;

				InitStates();
			}
			catch (System.Net.Sockets.SocketException)
			{
				throw new Exception(string.Format("Устройство не найдено по адресу [{0}]", endPoint.ToString()));
			}
			catch (System.IO.IOException ex)
			{
				throw new Exception(string.Format("Устройство не найдено по адресу [{0}]. {1}", endPoint.ToString(), ex.Message));
			}
			finally
			{
				if (test != null)
				{
					test.Close();
					test = null;
				}
			}
		}

		void OnAcknowledged(string Acknowledgement)
		{
			Logger.Info(string.Concat("Acknowledgement: {0}", Acknowledgement));
		}

		void OnException(Exception exception)
		{
			Logger.Error("PROCESS MODE EXCEPTION", exception);
		}

		public event EventHandler<EventTrackedEventArgs> Event;
		internal void RaiseEvent(Eyelock.Service.Event ev)
		{
			if (Event != null)
				Event(this, new EventTrackedEventArgs(ev));
		}

		public bool IsProcessing { get; private set; }

		public StateFrame InitialFrame { get; set; }
		public StateFrame CurrentFrame { get; private set; }
		internal Task<FrameResult> CurrentTask { get; private set; }

		private List<StateFrame> m_States;
		private Eyelock.Net.Video.Device m_Device;

		private void OnProcessedFrame(FrameResult result)
		{
			if (IsProcessing)
			{
				CurrentFrame = result.Next;
				Logger.Info("Processing next frame");
				if (CurrentFrame != null)
					ProcessFrame(CurrentFrame, result.Result);
			}
		}

		public void Start()
		{
			if (!IsProcessing)
			{
				IsProcessing = true;

				Logger.Info("State machine starts Initial frame");

				ProcessFrame(CurrentFrame = InitialFrame, null);
			}
		}

		public void Stop()
		{
			IsProcessing = false;
		}

		private void ProcessFrame(StateFrame frame, object parameters)
		{
			if (IsProcessing)
			{
				CurrentTask = frame.BeginProcess(parameters);
				CurrentTask.ContinueWith(t =>
				{
					Logger.Info("Frame continuation starts");
					if (t.IsFaulted)
						throw t.Exception.GetBaseException();
					OnProcessedFrame(t.Result);
				});

				if (!frame.DelayedStart) // запускаем, если запуск не отложенный.
					CurrentTask.Start();
			}
		}

		private void InitStates()
		{
			// Создаются все состояния и инициализируется начальное
			if (m_States == null)
			{
				m_States = new List<StateFrame>();

				var wait = new WaitPersonFrame(this);
				var sort = new SortingFrame(this);
				var match = new MatchingFrame(this);
				var result = new ResultFrame(this);
				var error = new FailFrame(this);

				wait.Success = sort;
				sort.Success = match;
				match.Success = result;
				result.Success = wait;
				wait.Fail = sort.Fail = match.Fail = error;

				m_States.Add(InitialFrame = wait);
				m_States.Add(sort);
				m_States.Add(match);
				m_States.Add(result);
				m_States.Add(error);
			}
		}

		internal void Notify(EyelockDevice.NotificationColor color)
		{
			Notify(color, 1000);
		}

		internal void Notify(EyelockDevice.NotificationColor color, int ms)
		{
			if (m_Device != null)
				m_Device.SetLEDColor(color.GetHashCode(), Math.Max(ms, 100));
		}

		public void Dispose()
		{
			InitialFrame = null;

			foreach (var state in m_States)
			{
				state.Fail = null;
				state.Success = null;

				state.Dispose();
			}

			m_States.Clear();
			m_States = null;

			if (m_Device != null)
			{
				m_Device.ProcessModeAcknowledged -= OnAcknowledged;
				m_Device.ProcessModeException -= OnException;
			}
		}
	}

	class FrameResult
	{
		public StateFrame Next { get; set; }
		public object Result { get; set; }
	}

	abstract class StateFrame : IDisposable
	{
		protected StateManager Machine { get; private set; }
		public StateFrame Success { get; set; }
		public FailFrame Fail { get; set; }
		public bool DelayedStart { get; set; }

		public StateFrame(StateManager manager)
		{
			DelayedStart = true;
			Machine = manager;
		}

		public Task<FrameResult> BeginProcess(object parameters)
		{
			Logger.Info(string.Format("[{0}] start processing...", GetType().Name));

			BeforeProcessing();

			var task = new Task<FrameResult>(() => Process(parameters));
			task.ContinueWith(t => AfterProcessing());

			return task;
		}

		/// <summary>
		/// Инициализация фрейма для исполнения
		/// </summary>
		protected virtual void BeforeProcessing()
		{
			Logger.Info(string.Format("[{0}] before process...", GetType().Name));
		}
		/// <summary>
		/// Действия после выполнения (отписываемся от событий и т.п.)
		/// </summary>
		protected virtual void AfterProcessing()
		{
			Logger.Info(string.Format("[{0}] after process...", GetType().Name));
		}

		protected abstract FrameResult Process(object parameters);

		public virtual void Dispose()
		{ }
	}

	class FailFrame : StateFrame
	{
		public FailFrame(StateManager manager)
			: base(manager)
		{
			DelayedStart = false;
		}

		public Exception Exception { get; set; }
		protected override FrameResult Process(object parameters)
		{
			try
			{
				Machine.Notify(EyelockDevice.NotificationColor.Red);
				throw Exception;
			}
			finally
			{
				Environment.FailFast(Exception.Message, Exception);
			}
		}
	}

	class WaitPersonFrame : StateFrame
	{
		private VideoManager m_Manager;

		public WaitPersonFrame(StateManager manager)
			: base(manager)
		{ }

		protected override void BeforeProcessing()
		{
			base.BeforeProcessing();

			if (m_Manager == null)
			{
				m_Manager = new VideoManager();
				m_Manager.EndPointList.Add(new System.Net.IPEndPoint(ConnectionSettings.Instance.LocalIP, ConnectionSettings.Instance.LocalPort));
			}

			m_Manager.Start();
			m_Manager.VideoFrameReceived += OnVideoFrameRecieved;
		}

		protected override void AfterProcessing()
		{
			base.AfterProcessing();

			if (m_Manager != null)
			{
				m_Manager.VideoFrameReceived -= OnVideoFrameRecieved;
				m_Manager.Stop();
			}
		}

		private void OnVideoFrameRecieved(VideoFrame frame)
		{
			// приняли первый фрейм - выполняем таск
			ProcessFrame(frame);
		}

		internal virtual void ProcessFrame(VideoFrame frame)
		{
			if (Machine.CurrentTask.Status == TaskStatus.Created)
				Machine.CurrentTask.Start();
		}

		protected override FrameResult Process(object parameters)
		{
			try
			{
				Logger.Info(string.Format("[{0}] SUCCESS", GetType().Name));
				Machine.Notify(EyelockDevice.NotificationColor.None);
				return new FrameResult() { Next = Success };
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format("[{0}] FAIL", GetType().Name), ex);
				Fail.Exception = ex;
				return new FrameResult() { Next = Fail };
			}
		}
	}

	class SortingFrame : WaitPersonFrame
	{
		private Eyelock.Eye.Sorting.Sorter m_Sorter;

		private Eyelock.Eye.Sorting.Eye m_BestLeft;
		private Eyelock.Eye.Sorting.Eye m_BestRight;

		public SortingFrame(StateManager manager)
			: base(manager)
		{ }

		protected override void BeforeProcessing()
		{
			m_Sorter = new Eye.Sorting.Sorter(5000, 10000);
			m_Sorter.Start();
			m_Sorter.BestFoundComplete += OnBestFoundComplete;

			base.BeforeProcessing();
		}

		protected override void AfterProcessing()
		{
			base.AfterProcessing();

			m_BestLeft = null;
			m_BestRight = null;
		}

		private void OnBestFoundComplete(List<List<Eyelock.Eye.Sorting.Eye>> rankedEyeList)
		{
			m_Sorter.Stop();
			m_Sorter.BestFoundComplete -= OnBestFoundComplete;

			Logger.Info(string.Format("[{0}] sorting finished", GetType().Name));

			List<Eyelock.Eye.Sorting.Eye> bestLeft = null;
			List<Eyelock.Eye.Sorting.Eye> bestRight = null;
			//
			// Ranked lists come sorted -> best = last index
			//

			Logger.Info(string.Format("[{0}] ranked list count is {1}", GetType().Name, rankedEyeList.Count));

			if (rankedEyeList.Count >= 1)
				bestLeft = rankedEyeList[0];
			if (rankedEyeList.Count >= 2)
				bestRight = rankedEyeList[1];

			if (bestLeft != null)
			{
				m_BestLeft = bestLeft[bestLeft.Count - 1];
				EnsureSizesAndScale(m_BestLeft);
			}

			if (bestRight != null)
			{
				m_BestRight = bestRight[bestRight.Count - 1];
				EnsureSizesAndScale(m_BestRight);
			}

			if (Machine.CurrentTask.Status == TaskStatus.Created)
				Machine.CurrentTask.Start();
		}

		private void EnsureSizesAndScale(Eye.Sorting.Eye eye)
		{
			byte[] data = null;
			if (eye.Width != Eyelock.Net.Video.Frame.DefaultSize.Width || eye.Height != Eyelock.Net.Video.Frame.DefaultSize.Height)
			{
				if (m_Sorter.ResizeFrame(eye.Data, eye.Width, eye.Height, ref data, Eyelock.Net.Video.Frame.DefaultSize.Width, Eyelock.Net.Video.Frame.DefaultSize.Height))
					eye.Data = data;
			}

			if (m_Sorter.ScaleFrame(eye.Data, ref data, 640, 480, 10f / 11))
				eye.Data = data;

			using (BiOmegaNet.BiOmegaNet matcher = new BiOmegaNet.BiOmegaNet(1, 6))
			{
				byte[] code = null;
				data = eye.Data;
				if (matcher.GetIrisCode(ref data, eye.Width, eye.Height, eye.Width, ref code))
				{
					Array.Copy(code, eye.Code, eye.Code.Length);
					Array.Copy(code, eye.Code.Length, eye.Mask, 0, eye.Mask.Length);
				}
			}
		}

		internal override void ProcessFrame(VideoFrame frame)
		{
			if (m_Sorter == null)
				return;

			// выбираем лучший фрейм из приходящих
			byte[] data = frame.Frame;
			Eye.Sorting.Eye sortEye = new Eye.Sorting.Eye();

			sortEye.CameraId = frame.CameraId;
			sortEye.Data = data;
			sortEye.Diameter = frame.Diameter;
			sortEye.Filename = frame.FileName;
			sortEye.FrameId = frame.FrameId;
			sortEye.HaloScore = frame.HaloScore;
			sortEye.Id = frame.Id;
			sortEye.IllumState = frame.IllumState;
			sortEye.ImageId = frame.ImageId;
			sortEye.MaxValue = frame.MaxValue;
			sortEye.Scale = frame.Scale;
			sortEye.Score = frame.Score;
			sortEye.Width = frame.Width;
			sortEye.Height = frame.Height;
			sortEye.X = frame.X;
			sortEye.Y = frame.Y;

			m_Sorter.FindBest(sortEye);
		}

		protected override FrameResult Process(object parameters)
		{
			try
			{
				Logger.Info(string.Format("[{0}] PROCESSING...", GetType().Name));

				if (!Validate())
				{
					Machine.Notify(EyelockDevice.NotificationColor.Red);

					Logger.Warn(string.Format("[{0}] FAIL", GetType().Name));

					return new FrameResult() { Next = Machine.InitialFrame }; // снова валим на ожидание пользователя
				}
				else
				{
					// результат фрейма
					Logger.Info(string.Format("[{0}] SUCCESS", GetType().Name));
					return new FrameResult()
					{
						Next = Success,
						Result = new Eye.Sorting.Eye[] { m_BestLeft, m_BestRight }
					};
				}
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format("[{0}] FAIL", GetType().Name), ex);
				Fail.Exception = ex;
				return new FrameResult() { Next = Fail };
			}

		}

		private bool Validate()
		{
			if (m_BestLeft == null)
			{
				Logger.Warn("VALIDATION FAILED: Left eye is NULL.");
				return false;
			}

			if (m_BestRight == null)
			{
				Logger.Warn("VALIDATION FAILED: Right eye is NULL.");
				return false;
			}

			int maxPupilDiameter = 95,
				minPupilDiameter = 20,
				maxXDelta = 15,
				maxYDelta = 30,
				maxIrisDiameter = 231,
				minIrisDiameter = 185,
				maxBitCorruption = 0x1c00;

			var pupilDiameter = m_BestLeft.PupilCircle.r * 2;
			if (pupilDiameter > maxPupilDiameter || pupilDiameter < minPupilDiameter)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Left eye pupilDiameter = {0}, MIN: {1}, MAX: {2}.", pupilDiameter, minPupilDiameter, maxPupilDiameter));
				return false;
			}

			pupilDiameter = m_BestRight.PupilCircle.r * 2;
			if (pupilDiameter > maxPupilDiameter || pupilDiameter < minPupilDiameter)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Right eye Pupil diameter = {0}, MIN: {1}, MAX: {2}.", pupilDiameter, minPupilDiameter, maxPupilDiameter));
				return false;
			}

			var irisDiameter = m_BestLeft.IrisCircle.r * 2;
			if (irisDiameter > maxIrisDiameter || irisDiameter < minIrisDiameter)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Left eye Iris circle = {0}, MIN: {1}, MAX: {2}.", irisDiameter, minIrisDiameter, maxIrisDiameter));
				return false;
			}

			irisDiameter = m_BestRight.IrisCircle.r * 2;
			if (irisDiameter > maxIrisDiameter || irisDiameter < minIrisDiameter)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Right eye Iris circle = {0}, MIN: {1}, MAX: {2}.", irisDiameter, minIrisDiameter, maxIrisDiameter));
				return false;
			}

			if (m_BestLeft.Score > maxBitCorruption)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Left eye bit corruption = {0}, MAX: {1}.", m_BestLeft.Score, maxBitCorruption));
				return false;
			}

			if (m_BestRight.Score > maxBitCorruption)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Right eye bit corruption = {0}, MAX: {1}.", m_BestRight.Score, maxBitCorruption));
				return false;
			}

			var xDelta = Math.Abs(m_BestLeft.IrisCircle.x - m_BestLeft.PupilCircle.x);
			if (xDelta > maxXDelta)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Left eye delta X = {0}, MAX: {1}.", xDelta, maxXDelta));
				return false;
			}

			var yDelta = Math.Abs(m_BestLeft.IrisCircle.y - m_BestLeft.PupilCircle.y);
			if (yDelta > maxYDelta)
			{
				Logger.Warn(string.Format("VALIDATION FAILED: Left eye delta X = {0}, MAX: {1}.", yDelta, maxYDelta));
				return false;
			}

			Logger.Info("Validation successed.");
			return true;
		}

		public override void Dispose()
		{
			base.Dispose();

			if (m_Sorter != null)
			{
				m_Sorter.Dispose();
				m_Sorter = null;
			}
		}
	}

	class MatchingFrame : StateFrame
	{
		public MatchingFrame(StateManager manager)
			: base(manager)
		{
			DelayedStart = false;
		}

		BiOmegaNet.BiOmegaNet m_Matcher;

		protected override void BeforeProcessing()
		{
			base.BeforeProcessing();

			if (m_Matcher == null)
				m_Matcher = new BiOmegaNet.BiOmegaNet(1, 6);
		}

		protected override FrameResult Process(object parameters)
		{
			Logger.Info(string.Format("[{0}] PROCESSING...", GetType().Name));
			try
			{
				Service.Event ev = null;
				Eyelock.Eye.Sorting.Eye[] eyes = (Eyelock.Eye.Sorting.Eye[])parameters;
				Eyelock.Eye.Sorting.Eye left = eyes[0], right = eyes[1];
				var index = GetBestMatching(left, right);

				if (index >= 0)
				{
					ev = Eyelock.Service.EventFactory.GetViewEvent();
					Iris iris;
					ev.User = Database.DBManagerFactory.GetManager().GetUser(index, out iris);

					ev.User.LeftIris = Eyelock.Database.ConvertTools.ToBase64(iris.Image_LL);
					ev.User.RightIris = Eyelock.Database.ConvertTools.ToBase64(iris.Image_RR);
				}
				else if (index == -1)
				{
					// глаз не найден, нужно добавлять
					ev = Eyelock.Service.EventFactory.GetAddEvent();
					ev.User = new Service.User() { UID = Guid.NewGuid() };

					ev.LeftEye = left;
					ev.RightEye = right;

					ev.User.LeftIris = Eyelock.Database.ConvertTools.ToBase64(left);
					ev.User.RightIris = Eyelock.Database.ConvertTools.ToBase64(right);
				}

				Logger.Info(string.Format("[{0}] SUCCESS", GetType().Name));
				return new FrameResult() { Next = Success, Result = ev };
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format("[{0}] FAIL", GetType().Name), ex);
				Fail.Exception = ex;
				return new FrameResult() { Next = Fail };
			}
		}

		private int GetBestMatching(Eyelock.Eye.Sorting.Eye left, Eyelock.Eye.Sorting.Eye right)
		{
			BiOmegaNet.BiOmegaPair pairLeft = null;
			BiOmegaNet.BiOmegaPair pairRight = null;

			var cache = Database.DBManagerFactory.GetManager().Cache;
			BiOmegaNet.BiOmegaPair best = null;

			int count = 0;
			byte[] codes = cache.GetLeftIrises(out count),
				code = GetCode(left);
			if (count > 0)
				pairLeft = m_Matcher.MatchCode(ref code, ref codes, count);

			codes = cache.GetRightIrises(out count);
			code = GetCode(right);
			if (count > 0)
				pairRight = m_Matcher.MatchCode(ref code, ref codes, count);

			if (pairLeft != null)
				Logger.Info(string.Format("Left eye HammingDistance is [{0}]", pairLeft.GetHammingDistance()));
			if (pairRight != null)
				Logger.Info(string.Format("Right eye HammingDistance is [{0}]", pairRight.GetHammingDistance()));

			if (pairLeft != null)
				best = pairLeft;
			if (pairRight != null && best.GetHammingDistance() > pairRight.GetHammingDistance())
				best = pairRight;

			float lhd = 0.0f, rhd = 0.0f;
			if (pairLeft != null)
				lhd = pairLeft.GetHammingDistance();
			if (pairRight != null)
				rhd = pairRight.GetHammingDistance();

			int index = -1;
			if (best != null && best.GetHammingDistance() < 0.3f)
				index = best.GetIndex();

			return index;
		}

		private byte[] GetCode(Eyelock.Eye.Sorting.Eye eye)
		{
			byte[] bytes = new byte[eye.Code.Length + eye.Mask.Length];
			Array.Copy(eye.Code, bytes, eye.Code.Length);
			Array.Copy(eye.Mask, 0, bytes, eye.Code.Length, eye.Mask.Length);

			return bytes;
		}

	}

	class ResultFrame : StateFrame
	{
		public ResultFrame(StateManager manager)
			: base(manager)
		{
			DelayedStart = false;
		}

		protected override FrameResult Process(object parameters)
		{
			Eyelock.Service.Event ev = (Eyelock.Service.Event)parameters;
			Machine.RaiseEvent(ev);
			Wait();

			return new FrameResult() { Next = Success };
		}

		private void Wait()
		{
			Machine.Notify(EyelockDevice.NotificationColor.Green, 250);
			System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
			Machine.Notify(EyelockDevice.NotificationColor.Green, 250);
			System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
			Machine.Notify(EyelockDevice.NotificationColor.Green, 250);
			System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
			Machine.Notify(EyelockDevice.NotificationColor.Green, 250);
			System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
			Machine.Notify(EyelockDevice.NotificationColor.Green, 250);
			System.Threading.SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
		}
	}
}

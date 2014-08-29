using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eyelock.Service;

namespace Eyelock.DeviceAdapter
{
    class StateManager : IDisposable
    {
        public enum NotificationColor
        { 
            None = 10,
            Blue = 11,
            Green = 12,
            LightBlue = 13,
            Red = 16,
            Purple = 17,
            Yellow = 20,
            White = 21
        }

        public StateManager()
        {
            var endPoint = new System.Net.IPEndPoint(ConnectionSettings.Instance.DeviceMainPanelIP, ConnectionSettings.Instance.DeviceMainPanelPort);
            try
            {
                m_Device = new Net.Video.Device(endPoint);
                m_Device.SetSecureMode(false);
                m_Device.SetProcessMode(Net.Video.Device.EyeLockProcessMode.EyeLockCamera, ConnectionSettings.Instance.LocalPort);
            }
            catch
            {
                throw new Exception("Устройство не доступно.");
            }

            InitStates();
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
				Debug.WriteLine("Processing next frame");
                if (CurrentFrame != null)
					ProcessFrame(CurrentFrame, result.Result);
            }
        }

        public void Start()
        {
            if (!IsProcessing)
            {
                IsProcessing = true;

				Debug.WriteLine("State machine starts Initial frame");

                ProcessFrame(InitialFrame, null);
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
					Debug.WriteLine("Frame continuation starts");
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

        internal void Notify(NotificationColor color)
        {
            if (m_Device != null)
                m_Device.SetLEDColor(color.GetHashCode(), 1000);
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
			Debug.WriteLine(string.Format("[{0}] start processing...", GetType().Name));

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
			Debug.WriteLine(string.Format("[{0}] before process...", GetType().Name));
		}
        /// <summary>
        /// Действия после выполнения (отписываемся от событий и т.п.)
        /// </summary>
		protected virtual void AfterProcessing()
		{
			Debug.WriteLine(string.Format("[{0}] after process...", GetType().Name));
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
			Machine.Notify(StateManager.NotificationColor.Red);
            throw Exception;
        }
    }

    class WaitPersonFrame : StateFrame
    {
		private GRIVideoManagerSDKNet.VideoManager m_Manager;

        public WaitPersonFrame(StateManager manager)
            : base(manager)
        { }

        protected override void BeforeProcessing()
        {
			base.BeforeProcessing();

            if (m_Manager == null)
            {
                m_Manager = new GRIVideoManagerSDKNet.VideoManager();
                m_Manager.EndPointList.Add(new System.Net.IPEndPoint(ConnectionSettings.Instance.LocalIP, ConnectionSettings.Instance.LocalPort));
                m_Manager.SetSecureComm(false);
            }

            m_Manager.VideoFrameReceived += OnVideoFrameRecieved;
			var res = m_Manager.Start();
        }

		protected override void AfterProcessing()
		{
			base.AfterProcessing();

			if (m_Manager != null)
			{
				m_Manager.Stop();
				m_Manager.VideoFrameReceived -= OnVideoFrameRecieved;
			}
		}

        private void OnVideoFrameRecieved(GRIVideoManagerSDKNet.VideoFrame frame)
        {
			// приняли первый фрейм - выполняем таск
			ProcessFrame(frame);
        }

		protected virtual void ProcessFrame(GRIVideoManagerSDKNet.VideoFrame frame)
		{
            if (Machine.CurrentTask.Status == TaskStatus.Created)
			    Machine.CurrentTask.Start();
		}

		protected override FrameResult Process(object parameters)
        {
            try
            {
				Debug.WriteLine(string.Format("[{0}] PROCESSING...", GetType().Name));

                Machine.Notify(StateManager.NotificationColor.Green);

				Debug.WriteLine(string.Format("[{0}] SUCCESS", GetType().Name));

				return new FrameResult() { Next = Success };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("[{0}] FAIL", GetType().Name));
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
			base.BeforeProcessing();
		
			if (m_Sorter == null)
				m_Sorter = new Eye.Sorting.Sorter(5000, 10000);

			m_Sorter.BestFoundComplete += OnBestFoundComplete;
			m_Sorter.Start();
		}

		protected override void AfterProcessing()
		{
			base.AfterProcessing();
		
			m_Sorter.Stop();
			m_Sorter.BestFoundComplete -= OnBestFoundComplete;
		}

		void OnBestFoundComplete(List<List<Eyelock.Eye.Sorting.Eye>> rankedEyeList)
		{
			Debug.WriteLine(string.Format("[{0}] sorting finished", GetType().Name));

			List<Eyelock.Eye.Sorting.Eye> bestLeft = null;
			List<Eyelock.Eye.Sorting.Eye> bestRight = null;
			//
			// Ranked lists come sorted -> best = last index
			//
			if (rankedEyeList.Count >= 1)
				bestLeft = rankedEyeList[0];
			if (rankedEyeList.Count >= 2)
				bestRight = rankedEyeList[1];

            if (bestLeft != null)
			    m_BestLeft = bestLeft[bestLeft.Count - 1];

            if (bestRight != null)
			    m_BestRight = bestRight[bestRight.Count - 1];

			if (Machine.CurrentTask.Status == TaskStatus.Created)
                Machine.CurrentTask.Start();
		}

		protected override void ProcessFrame(GRIVideoManagerSDKNet.VideoFrame frame)
		{
			Debug.WriteLine(string.Format("[{0}] processing recieved frame...", GetType().Name));

			// выбираем лучший фрейм из приходящих
			byte[] data = frame.Frame, helperData = null;
			m_Sorter.ResizeFrame(data, frame.Width, frame.Height, ref helperData, Eyelock.Net.Video.Frame.DefaultSize.Width, Eyelock.Net.Video.Frame.DefaultSize.Height);
			
			Eye.Sorting.Eye sortEye = new Eyelock.Eye.Sorting.Eye();

			sortEye.CameraId = frame.CameraId;
			sortEye.Data = helperData;
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
			sortEye.Width = Eyelock.Net.Video.Frame.DefaultSize.Width;
			sortEye.Height = Eyelock.Net.Video.Frame.DefaultSize.Height;
			sortEye.X = frame.X;
			sortEye.Y = frame.Y;

			m_Sorter.FindBest(sortEye);
		}

		protected override FrameResult Process(object parameters)
		{
			try
			{
				Debug.WriteLine(string.Format("[{0}] PROCESSING...", GetType().Name));

				if (m_BestLeft == null || m_BestRight == null)
				{
					Machine.Notify(StateManager.NotificationColor.Red);

					Debug.WriteLine(string.Format("[{0}] FAIL", GetType().Name));

					return new FrameResult() { Next = this };
				}
				else
				{
					// результат фрейма
					Machine.Notify(StateManager.NotificationColor.Green);

					Debug.WriteLine(string.Format("[{0}] SUCCESS", GetType().Name));

					return new FrameResult() 
					{
						Next = Success, 
						Result = new Eye.Sorting.Eye[] { m_BestLeft, m_BestRight } 
					};
				}
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("[{0}] FAIL", GetType().Name));
                Fail.Exception = ex;
                return new FrameResult() { Next = Fail };
            }

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
		private Eyelock.Eye.Matching.Matcher m_Matcher;
		Eyelock.Database.DBManager m_DBManager;

		public MatchingFrame(StateManager manager) 
			: base(manager)
		{
			DelayedStart = false;
		}

		protected override void BeforeProcessing()
		{
			base.BeforeProcessing();
			
			if (m_Matcher == null)
				m_Matcher = new Eyelock.Eye.Matching.Matcher();
			if (m_DBManager == null)
				m_DBManager = Database.DBManager.Instance;
		}

		protected override FrameResult Process(object parameters)
		{
			Debug.WriteLine(string.Format("[{0}] PROCESSING...", GetType().Name));

			try
			{
				Service.Event ev = null;
				Eyelock.Eye.Sorting.Eye[] eyes = (Eyelock.Eye.Sorting.Eye[])parameters;

				Eyelock.Eye.Sorting.Eye left = eyes[0];
				Eyelock.Eye.Sorting.Eye right = eyes[1];

				int count;
				var pairLeft = m_Matcher.MatchCode(left.Code, m_DBManager.GetLeftIrises(out count), count);
				var pairRight = m_Matcher.MatchCode(right.Code, m_DBManager.GetRightIrises(out count), count);

				int indexLeft = pairLeft.GetIndex(), indexRight = pairRight.GetIndex();
				if (indexLeft == indexRight) // получается что один человек найден
				{
					float lhd = pairLeft.GetHammingDistance(), rhd = pairRight.GetHammingDistance();

					if (indexRight > 0 && lhd > 0.3f && rhd > 0.3f)
					{ 
						// глаз найден, просмотр профиля
						ev = Eyelock.Service.EventFactory.GetViewEvent();
						ev.User = m_DBManager.GetUserByIndex(indexRight);
					}
					else if (indexRight == -1)
					{
						// глаз не найден, нужно добавлять
						var view = Eyelock.Service.EventFactory.GetViewEvent();
						ev.User = new Service.User() { UID = Guid.NewGuid() };
					}
					else //  
					{
						Debug.WriteLine(string.Format("[{0}] непонятная ситуация lhd < 0.3 || rhd < 0.3", GetType().Name));
						Machine.Notify(StateManager.NotificationColor.Yellow);
					}

					if (ev != null && ev.User != null)
					{
						ev.User.LeftIris = Eyelock.Database.ConvertTools.ToBase64(left);
                        ev.User.RightIris = Eyelock.Database.ConvertTools.ToBase64(right);
					}
				}
				else
				{
					Debug.WriteLine(string.Format("[{0}] непонятная ситуация indexLeft != indexRight", GetType().Name));
					Machine.Notify(StateManager.NotificationColor.Purple);
				}

				Debug.WriteLine(string.Format("[{0}] SUCCESS", GetType().Name));
				Machine.Notify(StateManager.NotificationColor.Green);
				return new FrameResult() { Next = Success, Result = ev };
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("[{0}] FAIL", GetType().Name));
                Fail.Exception = ex;
                return new FrameResult() { Next = Fail };
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			if (m_Matcher != null)
			{
				m_Matcher.Dispose();
				m_Matcher = null;
			}
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
			return new FrameResult() { Next = Success };
		}
	}
}

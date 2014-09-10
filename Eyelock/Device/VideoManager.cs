using GRIVideoManagerSDKNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Eyelock.Service;

namespace Eyelock.DeviceAdapter
{
    class VideoManager
    {
        private List<TcpListener> m_ListenerList;
        private List<IPEndPoint> m_EndPointList;
        private bool m_IsActive = false;
        private CancellationTokenSource m_TokenSource;
        private object m_SyncObject = new object();

        public event VideoFrameReceivedHandler VideoFrameReceived;

        public VideoManager()
        {
            m_EndPointList = new List<IPEndPoint>();
            m_ListenerList = new List<TcpListener>();
        }

        private void ProcessClient(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                if (client.Connected)
                {
                    using (NetworkStream clientStream = client.GetStream())
                    {
                        if (clientStream.DataAvailable)
                        {
                            using (MemoryStream recievedStream = new MemoryStream())
                            {
                                byte[] buffer = new byte[0x10000];
                                int readBytesCount = 0;
                                while ((readBytesCount = clientStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    recievedStream.Write(buffer, 0, readBytesCount);
                                }

                                cancellationToken.ThrowIfCancellationRequested();

                                if (VideoFrameReceived != null && recievedStream.Length > 0)
                                {
                                    buffer = recievedStream.ToArray();
                                    VideoFrameReceived(VideoParser.ParseFrame(ref buffer, client.Client.RemoteEndPoint));
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                client.Close();
            }
        }

        private void AcceptClients(TcpListener listener, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (listener.Pending())
                {
                    lock (m_SyncObject)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Task.Factory.StartNew(_ => ProcessClient(client, cancellationToken), cancellationToken);
                    }
                }
            }
        }

        public bool Start()
        {
            if (m_IsActive)
                return true;

            Logger.Info("VideoManager starting...");

            m_TokenSource = new CancellationTokenSource();
            m_IsActive = true;

            foreach (IPEndPoint point in this.m_EndPointList)
            {
                TcpListener item = new TcpListener(point);
                m_ListenerList.Add(item);

                item.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                item.Start();

                Task.Factory.StartNew(_ => AcceptClients(item, m_TokenSource.Token), m_TokenSource.Token);
            }

			Logger.Info("VideoManager started.");

            return true;
        }

        public bool Stop()
        {
            if (!m_IsActive)
                return false;

            lock (m_SyncObject)
            {
                m_TokenSource.Cancel();
                foreach (TcpListener listener in m_ListenerList)
                    listener.Stop();
			}

			m_IsActive = false;
			Logger.Info("VideoManager stoped.");

            return true;
        }

        public IList<IPEndPoint> EndPointList
        {
            get
            {
                if (m_IsActive)
                    return m_EndPointList.AsReadOnly();
                else
                    return m_EndPointList;
            }
        }

        public delegate void VideoFrameReceivedHandler(VideoFrame VideoFrame);
    }

    static class VideoParser
    {
        private static CultureInfo m_FormatProvider = new CultureInfo("en-US");

        public static VideoFrame ParseFrame(ref byte[] Buffer)
        {
            return ParseFrame(ref Buffer, null);
        }

        public static VideoFrame ParseFrame(ref byte[] Buffer, EndPoint Server)
        {
            HTTPMessage message = new HTTPMessage();
            if (message.Parse(ref Buffer))
            {
                int id = 0;
                int cameraId = 0;
                int frameId = 0;
                int imageId = 0;
                int numberOfImages = 0;
                float scale = 0f;
                float num7 = 0f;
                float num8 = 0f;
                float num9 = 0f;
                float num10 = -1f;
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                int score = 0;
                int maxValue = 0;
                int diameter = 0;
                bool flag = true;
                string fileName = string.Empty;
                for (int i = 0; i < message.ContentList.Count; i++)
                {
                    if (message.ContentList[i].Filename.IndexOf(".xml") >= 0)
                    {
                        XmlDocument document = new XmlDocument();
                        document.LoadXml(Encoding.ASCII.GetString(message.ContentList[i].Data).Trim(new char[] { '\r', '\n' }));
                        XmlNode node = document.SelectSingleNode("/images/image");
                        if ((node != null) && (node.Name == "image"))
                        {
                            foreach (XmlAttribute attribute in node.Attributes)
                            {
                                switch (attribute.Name)
                                {
                                    case "id":
                                        id = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "cameraId":
                                        cameraId = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "frameId":
                                        frameId = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "imageId":
                                        imageId = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "numberOfImages":
                                        numberOfImages = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "scale":
                                        scale = float.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "irx":
                                        num7 = float.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "iry":
                                        num8 = float.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "halo":
                                        num9 = float.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "blc":
                                        num10 = float.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "x":
                                        x = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "y":
                                        y = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "width":
                                        width = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "height":
                                        height = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "score":
                                        score = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "maxValue":
                                        maxValue = int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "file_name":
                                        fileName = attribute.Value;
                                        break;

                                    case "prev":
                                        int.Parse(attribute.Value, m_FormatProvider);
                                        break;

                                    case "Il0":
                                        flag = 1 == int.Parse(attribute.Value, m_FormatProvider);
                                        break;
                                }
                            }
                        }
                        node = document.SelectSingleNode("/images/iris");
                        if ((node != null) && (node.Name == "iris"))
                        {
                            foreach (XmlAttribute attribute2 in node.Attributes)
                            {
                                string str3;
                                if (((str3 = attribute2.Name) != null) && (str3 == "diameter"))
                                {
                                    diameter = int.Parse(attribute2.Value, m_FormatProvider);
                                }
                            }
                        }
                    }
                    if (((message.ContentList[i].Filename.IndexOf(".bin") >= 0) && (width > 0)) && ((height > 0) && (maxValue > 0)))
                    {
                        Console.WriteLine("Frame: {0}-{1}-{2} H{3} B{4} S({5} {6})", new object[] { cameraId, frameId, imageId, num9, num10, num7, num8 });
                        return new VideoFrame(message.ContentList[i].Data, id, cameraId, frameId, imageId, numberOfImages, scale, x, y, width, height, score, maxValue, fileName, diameter, Server, Buffer) { HaloScore = num9, IllumState = flag };
                    }
                }
            }
            return null;
        }
    }


}

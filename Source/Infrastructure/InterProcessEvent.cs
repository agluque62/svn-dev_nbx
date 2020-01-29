#define _IPE_NOSPREAD_
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using NLog;
using ProtoBuf;
using Utilities;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// Proporcionará un modelo de Eventos C# entre procesos, utilizando "spread"
    /// </summary>
    public class InterProcessEvent : IDisposable
    {
#if _IPE_NOSPREAD_
        public static class IpcUdpPort
        {
            /** 20180208. Los puertos anteriores chocaban con algunos servicios de windows */
            private const int _first_udp_port = 50000;
            private const int _last_udp_port  = 50010;
            private static int _current_port  = _first_udp_port;
            private static Dictionary<string, int> _assigned = new Dictionary<string, int>();
            public static int Port(string name)
            {
                if (_assigned.ContainsKey(name))
                    return _assigned[name];
                else if (_current_port < _last_udp_port)
                {
                    _assigned[name] = _current_port;
                    return _current_port++;
                }
                else
                    return -1;          // Esto espero que de un error....
            }
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        private /*static*/ GenericEventHandler<SpreadDataMsg> _OnMsgReceived;
        string _name = "";
        private bool _EsConsumidor = false;

#if _IPE_NOSPREAD_
        private bool _ReceiveThreadRunning = false;
        private Thread _ReceiveThread = null;
        private UdpClient _listener = null;
        private int _udp_port;
#else
        private Registry _Registry;
        const int IPE_MESS = 0x00000001;
        const string _topic = "ipe";
#endif

        /// <summary>
        /// 
        /// </summary>
        public InterProcessEvent(string Name)
        {
            _name = Name;
            _udp_port = IpcUdpPort.Port(Name);
            Init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="EsConsumidor"></param>
        public InterProcessEvent(string Name, GenericEventHandler<SpreadDataMsg> OnMsgReceived)
        {
            _EsConsumidor = true;
            _name = Name;
            _OnMsgReceived = OnMsgReceived;
            _udp_port = IpcUdpPort.Port(Name);
            Init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="messType"></param>
        /// <param name="mess"></param>
        public void Send<T>(T data) where T : class
        {
#if _IPE_NOSPREAD_
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            byte[] bdata = ms.ToArray();
            (new UdpClient()).Send(bdata, bdata.Count(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), _udp_port));
#else
            _Registry.Send(_topic, IPE_MESS, data);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public void Raise<T>(T data)
        {
#if _IPE_NOSPREAD_
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            byte[] bdata = ms.ToArray();
            (new UdpClient()).Send(bdata, bdata.Count(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), _udp_port));
#else
            _Registry.Send(_topic, IPE_MESS, data);
#endif
        }

        #region Elementos Privados

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
#if _IPE_NOSPREAD_
            if (_EsConsumidor == true)
            {
                _ReceiveThreadRunning = false;
                _listener.Close();
                _ReceiveThread.Abort();
                _ReceiveThread.Join(2000);
            }
#else
            _Registry.Dispose();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private void Init()
        {
#if _IPE_NOSPREAD_
            if (_EsConsumidor == true)
            {
                _ReceiveThreadRunning = true;
                _ReceiveThread = new Thread(new ThreadStart(ReceiveThread)) { IsBackground = true };
                _ReceiveThread.Start();
            }
#else
            _Registry = new Registry(_name);
            if (_EsConsumidor == true)
                _Registry.UserMsgReceived += OnMsgReceived;
            _Registry.Join(_topic);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnMsgReceived(object sender, SpreadDataMsg msg)
        {
            _OnMsgReceived(sender, msg);
        }

#if _IPE_NOSPREAD_
        /// <summary>
        /// 
        /// </summary>
        private void ReceiveThread()
        {
            while (_ReceiveThreadRunning == true)
            {
                try
                {
                    if (_listener == null)
                    {
                        _listener = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _udp_port));
                    }
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, _udp_port);
                    Byte[] recibido = _listener.Receive(ref from);
                    SpreadDataMsg msg = new SpreadDataMsg("InterProcessEvent", 1, recibido, recibido.Count(), "InterProcessEvent");
                    _OnMsgReceived(null, msg);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    break;
                }
                catch (Exception x)
                {
                    LogManager.GetCurrentClassLogger().Error("Interprocess event " + _name + ", Puerto " + _udp_port.ToString() + ": " + x.Message);
                }
            }

        }
#endif

       #endregion
    }
}

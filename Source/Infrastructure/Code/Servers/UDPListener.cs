using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using U5ki.Delegates;

namespace U5ki.Infrastructure.Servers
{
    public class UDPListener : BaseServer, IDisposable
    {

        #region Declarations

        private IPEndPoint _endPoint;
        private UdpClient _udpClient;
        
        private Boolean _isRunning;
        /// <summary>
        /// Propiedad que nos dice si el Listener esta corriendo.
        /// </summary>
        public Boolean IsRunning { get { return _isRunning; } }

        private Thread _runningThread;

        public event ByteArrayDelegate OnMessageRecieved;

        #endregion

        /// <summary>
        /// Constructor esclusivo para la clase.
        /// </summary>
        /// <param name="Ip">
        /// La IP tiene que tener el fomato correcto V4 XXX.XXX.XXX.XXX.
        /// </param>
        /// <param name="port">
        /// Puerto por el que se va a escuchar la comunicación.
        /// </param>
        public UDPListener(String Ip, Int32 port, ByteArrayDelegate onMessageRecieved)
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(Ip), port);
            _udpClient = new UdpClient(_endPoint);

            OnMessageRecieved += onMessageRecieved;

            Start();
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _runningThread = new Thread(new ThreadStart(ListenerThread)) { IsBackground = true };
            _runningThread.Start();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            try
            {
                _runningThread.Abort();
            }
            catch (Exception ex)
            {
                LogError<UDPListener>(ex);
            }
        }

        private void ListenerThread()
        {
            while (_isRunning)
            {
                byte[] output = _udpClient.Receive(ref _endPoint);
                OnMessageRecieved.Invoke(output);
            }
        }
        
        public void Dispose()
        {
            Stop();

            _endPoint = null;

            _udpClient.Close();
            _udpClient = null;

            _runningThread = null;
        }

    }
}

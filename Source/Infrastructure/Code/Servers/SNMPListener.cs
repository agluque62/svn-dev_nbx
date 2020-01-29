using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using U5ki.Delegates;
using Lextm.SharpSnmpLib.Messaging;
using U5ki.Infrastructure.Code;
using Lextm.SharpSnmpLib;
using System.Collections.Generic;

namespace U5ki.Infrastructure.Servers
{
    public class SNMPListener : BaseServer, IDisposable
    {

        public delegate ErrorCode OnMessageRecievedDelegate(ISnmpMessage message, IPEndPoint endPoint);

        #region Declarations

        private IPEndPoint _endPoint;
        
        Listener _listener;

        private Boolean _isRunning;
        /// <summary>
        /// Propiedad que nos dice si el Listener esta corriendo.
        /// </summary>
        public Boolean IsRunning { get { return _isRunning; } }

        public event OnMessageRecievedDelegate OnMessageRecieved;

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
        public SNMPListener(String Ip, Int32 port, OnMessageRecievedDelegate onMessageRecieved)
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(Ip), port);
            _listener = new Listener();

            OnMessageRecieved += onMessageRecieved;

            Start();
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            
            try
            {
                _listener.AddBinding(_endPoint);
                _listener.MessageReceived += OnSNMPReceived;
                _listener.Start();
            }
            catch (Exception ex)
            {
                Globals.Events.Trigger(Enums.GlobalEventTypes.OnMessage, "ERROR: " + ex.Message);
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            try
            {
                _listener.Stop();
                _listener.ClearBindings();
            }
            catch (Exception ex)
            {
                Globals.Events.Trigger(Enums.GlobalEventTypes.OnMessage, "ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        //case SnmpType.InformRequestPdu:
        //    {
        //        InformRequestMessage inform = (InformRequestMessage)message;
        //        binding.SendResponse(inform.GenerateResponse(), sender);

        //        EventHandler<MessageReceivedEventArgs<InformRequestMessage>> handler = InformRequestReceived;
        //        if (handler != null)
        //        {
        //            handler(this, new MessageReceivedEventArgs<InformRequestMessage>(sender, inform, binding));
        //        }

        //        break;
        //    }
        /// </remarks>
        private void OnSNMPReceived(object sender, MessageReceivedEventArgs e)
        {
            if (null == OnMessageRecieved)
                throw new NotImplementedException("OnMessageRecieved cannot be null.");

            ErrorCode response = OnMessageRecieved.Invoke(e.Message, e.Sender);

            ISnmpPdu pdu = e.Message.Pdu();
            //InformRequestMessage responseMessage = (InformRequestMessage)e.Message;
            ResponseMessage responseMessage = new ResponseMessage(
                pdu.RequestId.ToInt32(),
                VersionCode.V2,
                new OctetString("public"),
                response,
                0,
                new List<Variable>());

            //responseMessage.Send(e.Sender);
            //e.Sender.Port = e.Sender.Port + 1;
            e.Binding.SendResponse(responseMessage, e.Sender);            
        }
        
        public void Dispose()
        {
            Stop();

            _endPoint = null;

            _listener.Dispose();
            _listener = null;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="sender">Sender.</param>
        /// <param name="binding">The binding.</param>
        public void Process(ISnmpMessage message, IPEndPoint sender, ListenerBinding binding)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            switch (message.Pdu().TypeCode)
            {
                //case SnmpType.TrapV1Pdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<TrapV1Message>> handler = TrapV1Received;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<TrapV1Message>(sender, (TrapV1Message)message, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.TrapV2Pdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<TrapV2Message>> handler = TrapV2Received;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<TrapV2Message>(sender, (TrapV2Message)message, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.InformRequestPdu:
                //    {
                //        InformRequestMessage inform = (InformRequestMessage)message;
                //        binding.SendResponse(inform.GenerateResponse(), sender);

                //        EventHandler<MessageReceivedEventArgs<InformRequestMessage>> handler = InformRequestReceived;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<InformRequestMessage>(sender, inform, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.GetRequestPdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<GetRequestMessage>> handler = GetRequestReceived;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<GetRequestMessage>(sender, (GetRequestMessage)message, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.SetRequestPdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<SetRequestMessage>> handler = SetRequestReceived;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<SetRequestMessage>(sender, (SetRequestMessage)message, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.GetNextRequestPdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<GetNextRequestMessage>> handler = GetNextRequestReceived;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<GetNextRequestMessage>(sender, (GetNextRequestMessage)message, binding));
                //        }

                //        break;
                //    }

                //case SnmpType.GetBulkRequestPdu:
                //    {
                //        EventHandler<MessageReceivedEventArgs<GetBulkRequestMessage>> handler = GetBulkRequestReceived;
                //        if (handler != null)
                //        {
                //            handler(this, new MessageReceivedEventArgs<GetBulkRequestMessage>(sender, (GetBulkRequestMessage)message, binding));
                //        }

                //        break;
                //    }

                default:
                    break;
            }
        }

    }
}

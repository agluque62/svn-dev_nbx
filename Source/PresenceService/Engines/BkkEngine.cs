using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Diagnostics;


using WebSocket4Net;
using Newtonsoft.Json;

using U5ki.Infrastructure;
using U5ki.PresenceService.Interfaces;


namespace U5ki.PresenceService.Engines
{
    class BkkEngine : BaseCode, IAgentEngine
    {

        #region IAgentEngine
        public event EventHandler<AgentEngineEventArgs> EventOccurred;
        /// <summary>
        /// 
        /// </summary>
        public bool Available
        {
            get
            {
                Debug.Assert(_pbxws != null);
                return (_pbxws.State == WebSocketState.Connecting || _pbxws.State == WebSocketState.Open);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnEventOccurred"></param>
        public void Init(EventHandler<AgentEngineEventArgs> OnEventOccurred)
        {
            EventOccurred = OnEventOccurred;
            Debug.Assert(WsEndpoint != null);
            String pbxUrl = String.Format("ws://{0}:{1}/pbx/ws?login_user={2}&login_password={3}&user=*&registered=True&status=True&line=*",
                WsEndpoint.Address.ToString(), 
                WsEndpoint.Port.ToString(), PSHelper.LocalParameters.BkkUser, PSHelper.LocalParameters.BkkPwd);
            _pbxws = new WebSocket(pbxUrl);
            _pbxws.Opened += new EventHandler(OnWsOpened);
            _pbxws.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(OnWsError);
            _pbxws.Closed += new EventHandler(OnWsClosed);
            _pbxws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(OnWsData);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            Debug.Assert(_pbxws != null /*&& _pbxws.State == WebSocketState.Closed*/);
            _pbxws.Open();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            Debug.Assert(_pbxws != null /*&& _pbxws.State == WebSocketState.Closed*/);
            _pbxws.Close();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Debug.Assert(_pbxws != null);

            EventOccurred = null;
            _pbxws.Opened -= new EventHandler(OnWsOpened);
            _pbxws.Error -= new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(OnWsError);
            _pbxws.Closed -= new EventHandler(OnWsClosed);
            _pbxws.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(OnWsData);
            _pbxws.Close();
            _pbxws = null;
        }


        #endregion

        #region Eventos
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBrekekeEventOccurred(AgentEngineEventArgs e)
        {
            EventHandler<AgentEngineEventArgs> handler = EventOccurred;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWsOpened(object sender, EventArgs e)
        {
            PSHelper.LOGGER.Debug<BkkEngine>(String.Format("WebSocket Brekeke: OnWsOpened"));
            OnBrekekeEventOccurred(new AgentEngineEventArgs() { ev = AgentEngineEvents.Open, idsub=null });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWsClosed(object sender, EventArgs e)
        {
            PSHelper.LOGGER.Debug<BkkEngine>(String.Format("WebSocket Brekeke: OnWsClosed"));
            OnBrekekeEventOccurred(new AgentEngineEventArgs() { ev = AgentEngineEvents.Closed, idsub = null });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWsError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            PSHelper.LOGGER.Debug<BkkEngine>(String.Format("WebSocket Brekeke: OnWsError"));
            OnBrekekeEventOccurred(new AgentEngineEventArgs() { ev = AgentEngineEvents.Error, idsub = null });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWsData(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                PSHelper.LOGGER.Trace<BkkEngine>(String.Format("WebSocket Brekeke: Mensaje Recibido: {0}", e.Message));

                string msg = e.Message.Replace("params", "parametros");

                if (msg.StartsWith("{"))
                {
                    bkkMessage bkkmsg = JsonConvert.DeserializeObject<bkkMessage>(msg);
                    ProcessData(bkkmsg);
                }
                else if (msg.StartsWith("["))
                {
                    JsonConvert.DeserializeObject<bkkMessage[]>(msg).ToList().ForEach(bkkmsg =>
                    {
                        ProcessData(bkkmsg);
                    });
                }
            }
            catch (Exception x)
            {
                LogException<BkkEngine>("OnWsData Exception", x, false);
                PSHelper.LOGGER.Trace<BkkEngine>(String.Format("OnWebSocketData exception", x.Message));
            }
        }

        #endregion

        #region Datos

        public IPEndPoint WsEndpoint { get; set; }
        private WebSocket _pbxws = null;

        #endregion

        #region Rutinas Internas

        class bkkParamInfo
        {
            // Evento Register
            public string registered { get; set; }
            public string user { get; set; }
            // Evento Status,
            public long time { get; set; }
            public string other_number { get; set; }
            public string status { get; set; }
            // public string user { get; set; }
        };
        /// <summary>
        /// 
        /// </summary>
        class bkkMessage
        {
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public bkkParamInfo parametros { get; set; }
        };

        private void ProcessData(bkkMessage data)
        {
            switch (data.method)
            {
                case "notify_serverstatus":
                    switch (data.parametros.status)
                    {
                        case "active":
                            OnBrekekeEventOccurred(new AgentEngineEventArgs() { ev = AgentEngineEvents.Open, idsub = null });
                            break;
                        default:
                            OnBrekekeEventOccurred(new AgentEngineEventArgs() { ev = AgentEngineEvents.Error, idsub = null });
                            break;
                    }
                    PSHelper.LOGGER.Trace<BkkEngine>(String.Format("Server Status: {0}", data.parametros.status));
                    break;

                case "notify_status":
                    OnBrekekeEventOccurred(new AgentEngineEventArgs()
                    {
                        ev = GetStatusEventcode(int.Parse(data.parametros.status)),
                        idsub = data.parametros.user
                    });
                    PSHelper.LOGGER.Trace<BkkEngine>(String.Format("Procesado Estado Usuario {0}, Estado: {1}",
                        data.parametros.user, data.parametros.status));
                    break;

                case "notify_registered":
                    bool registered = data.parametros.registered == "true";
                    OnBrekekeEventOccurred(new AgentEngineEventArgs()
                    {
                        ev = registered ? AgentEngineEvents.UserRegistered : AgentEngineEvents.UserUnregistered,
                        idsub = data.parametros.user
                    });
                    PSHelper.LOGGER.Trace<BkkEngine>(String.Format("Procesado Registro Usuario {0}, {1}",
                        data.parametros.user, data.parametros.registered));
                    break;

                default:
                    LogError<BkkEngine>(String.Format("Evento {0} Desconocido", data.method));
                    PSHelper.LOGGER.Trace<BkkEngine>(String.Format("Evento {0} Desconocido", data.method));
                    break;
            }
        }

        /*  Estado Externo          Estado Interno
            CALLING = 0             2, Ocupado No Interrumplible.
            INCOMING = 1            2, Ocupado No Interrumplible.
            CALL_SUCCESS = 2        1, Ocupado Interrumplible.
            ENDTALKING = 12         2, Ocupado No Interrumplible.
            ANSWER_SUCCESS = 14     1, Ocupado Interrumplible.
            PARK_CANCEL = 21        2, Ocupado No Interrumplible.
            PARK_START = 30         2, Ocupado No Interrumplible.
            STARTRINGING = 65       2, Ocupado No Interrumplible.
            HOLD = 35               1, Ocupado Interrumplible.
            UNHOLD = 36             1, Ocupado Interrumplible.
            DISCONNECT = -1         0, Libre
         * * */
        private AgentEngineEvents GetStatusEventcode(int extStatus)
        {
            switch (extStatus)
            {
                case -1:
                    return AgentEngineEvents.UserAvailable;
                case 2:
                case 14:
                case 35:
                case 36:
                    return AgentEngineEvents.UserBusy;
                case 0:
                case 1:
                case 12:
                case 21:
                case 30:
                case 65:
                    return AgentEngineEvents.UserBusyUninterrupted;
                default:
                    return AgentEngineEvents.UserAvailable;
            }
        }

        #endregion
    }
}

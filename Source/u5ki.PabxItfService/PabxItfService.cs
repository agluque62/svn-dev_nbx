#define _SUBSCRIBE_CFG_
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Timers;

using U5ki.Infrastructure;
using ProtoBuf;
using Utilities;

using NLog;
using WebSocket4Net;
using Newtonsoft.Json;
using Translate;

namespace U5ki.PabxItfService
{
    /// <summary>
    /// 
    /// </summary>
    public class PabxItfService : BaseCode, IService
    {
        #region class extTifx
        public class extTifx : BaseCode
        {
            /** Logitudes de campos en las tramas */
            private const int defMaxNameLeght = 32 + 4;
            private static Logger _Logger = LogManager.GetCurrentClassLogger();

            [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
            public struct tifxBinRec
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = defMaxNameLeght)]
                public string name;
                [MarshalAs(UnmanagedType.I4)]
                public int tipo;		        /* publica_tlf, publica_lc */
                [MarshalAs(UnmanagedType.I4)]
                public int version;	            /* cambia de valor con los cambios */
                [MarshalAs(UnmanagedType.I4)]
                public int estado;	            /* estado del recurso */
                [MarshalAs(UnmanagedType.I4)]
                public int prio_cpipl;
                [MarshalAs(UnmanagedType.I4)]
                public int ContadorTransitos;
                [MarshalAs(UnmanagedType.I4)]
                public int tiempo;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
            public struct tifxBinHeader
            {
                /** */
                [MarshalAs(UnmanagedType.I4)]
                public int tipo_msg;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = defMaxNameLeght)]
                public string name;
                [MarshalAs(UnmanagedType.I4)]
                public int nrecursos;
                [MarshalAs(UnmanagedType.I4)]
                public int version;
            };

            /// <summary>
            /// 
            /// </summary>
            public class rcInfo
            {
                public string user { get; set; }
                public Int32 status { get; set; }
                public eInternalStatus intstatus { get; set; }
                public Int32 version { get; set; }
            }

            public enum eInternalStatus { eisReposo = 0, eisBorrado = 1, eisPenReposo = 2, eisPenBorrado = 3 };

            /// <summary>
            /// 
            /// </summary>
            public extTifx()
            {
                _tbRec = new Dictionary<string, rcInfo>()
                    {
                        {"41001",new rcInfo(){user="41001"} },
                        {"41002",new rcInfo(){user="41002"} }
                    };
            }

            #region Metodos Publicos

            public string Name { get; set; }
            public List<string> AllowedRec { get { return _AllowedRec; } }
            public Dictionary<string, rcInfo> TbRec { get { return _tbRec; } }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="user"></param>
            /// <param name="reg"></param>            
            public void rcRegister(string user, bool reg = false)
            {
                rcInfo rec = rcFind(user);
                if (rec != null)
                {
                    switch (rec.intstatus)
                    {
                        case eInternalStatus.eisReposo:
                            rec.intstatus = reg ? eInternalStatus.eisReposo : eInternalStatus.eisPenBorrado;
                            break;
                        case eInternalStatus.eisPenReposo:
                            rec.intstatus = reg ? eInternalStatus.eisPenReposo : eInternalStatus.eisPenBorrado;
                            break;
                        case eInternalStatus.eisPenBorrado:
                            rec.intstatus = reg ? eInternalStatus.eisPenReposo : eInternalStatus.eisPenBorrado;
                            break;
                        case eInternalStatus.eisBorrado:
                            rec.intstatus = reg ? eInternalStatus.eisPenReposo : eInternalStatus.eisBorrado;
                            break;
                        default:
                            _Logger.Debug("rcRegister. Inconsistencia de estados internos {0},{1}", rec.intstatus, reg);
                            break;
                    }
                    //rec.version = 0;
                    //rec.status = 0;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="user"></param>
            /// <param name="status"></param>
            public void rcStatus(string user, int status)
            {
                rcInfo rec = rcFind(user);
                if (rec != null)
                {
                    status = External2IntenalStatus(status);
                    switch (rec.intstatus)
                    {
                        case eInternalStatus.eisReposo:
                            rec.intstatus = status != rec.status ? eInternalStatus.eisPenReposo : eInternalStatus.eisReposo;
                            break;
                        case eInternalStatus.eisBorrado:
                        case eInternalStatus.eisPenBorrado:
                             rec.intstatus = eInternalStatus.eisPenReposo;
                            break;
                        case eInternalStatus.eisPenReposo:
                            break;
                    }
                    rec.version = (status != rec.status) ? rec.version + 1 : rec.version;
                    rec.status = status;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public byte[] mcBinaryFrame()
            {
                byte[] frame = new byte[1];
                int count = 0;

                /** Enviar todos los activos */
                List<rcInfo> recActivos = _tbRec.Where(rr => rr.Value.intstatus == eInternalStatus.eisReposo ||
                        rr.Value.intstatus == eInternalStatus.eisPenReposo).Select(x => x.Value).ToList();

                _version = _tbRec.Where(rr => rr.Value.intstatus == eInternalStatus.eisPenBorrado ||
                                                  rr.Value.intstatus == eInternalStatus.eisPenReposo).Count() > 0 ? _version + 1 : _version;

                tifxBinHeader header = new tifxBinHeader();
                header.tipo_msg = IPAddress.HostToNetworkOrder(2);
                header.name = Name;
                header.nrecursos = IPAddress.HostToNetworkOrder(recActivos.Count());
                header.version = IPAddress.HostToNetworkOrder(_version);
                count = Copy2ByteArray(ref frame, count, header);

                for (int irec = 0; irec < recActivos.Count; irec++)
                {
                    tifxBinRec prec = new tifxBinRec();
                    prec.name = recActivos[irec].user;
                    prec.tipo = IPAddress.HostToNetworkOrder(1);
                    prec.estado = IPAddress.HostToNetworkOrder(recActivos[irec].status);
                    prec.prio_cpipl = IPAddress.HostToNetworkOrder(recActivos[irec].status < 2 ? 0 : 1);
                    prec.ContadorTransitos = IPAddress.HostToNetworkOrder(0);
                    prec.tiempo = IPAddress.HostToNetworkOrder(0);
                    prec.version = IPAddress.HostToNetworkOrder(recActivos[irec].version);

                    count = Copy2ByteArray(ref frame, count, prec);
                }

                /** Borro los pendientes de borrado... */
                foreach (rcInfo rc in _tbRec.Where(rr => rr.Value.intstatus == eInternalStatus.eisPenBorrado).Select(x => x.Value).ToList())
                    rc.intstatus = eInternalStatus.eisBorrado;
                /** Pongo a reposo los pendientes de reposo ... */
                foreach (rcInfo rc in _tbRec.Where(rr => rr.Value.intstatus == eInternalStatus.eisPenReposo).Select(x => x.Value).ToList())
                    rc.intstatus = eInternalStatus.eisReposo;

                return frame;
            }

            /// <summary>
            /// Gestionar la Tabla ON-LINE para Eliminar Recursos en Cambios de Configuracion
            /// </summary>
            public void TbRecCheck()
            {
                foreach (var item in _tbRec.Where(kvp => AllowedRec.Contains(kvp.Key)==false).ToList())
                {
                    _tbRec.Remove(item.Key);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public void SetAllRefresh()
            {
                _version++;
                foreach (rcInfo rc in _tbRec.Where(rr => rr.Value.intstatus == eInternalStatus.eisReposo).Select(x => x.Value).ToList())
                {
                    rc.version++;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public void SetAllUnregistered()
            {
                _version++;
                foreach (rcInfo rc in _tbRec.Where(rr => rr.Value.intstatus != eInternalStatus.eisBorrado).Select(x => x.Value).ToList())
                {
                    rc.version++;
                    rc.intstatus = eInternalStatus.eisPenBorrado;
                }
            }

            #endregion

            #region Datos Privados
            private Dictionary<string, rcInfo> _tbRec;
            private List<string> _AllowedRec = new List<string>();

            private int _version = 0;
            #endregion

            #region Procedimientos Privados.
            /// <summary>
            /// 
            /// </summary>
            /// <param name="user"></param>
            /// <returns></returns>
            private rcInfo rcFind(string user1, bool register = true)
            {
                if (_tbRec.ContainsKey(user1))
                    return _tbRec[user1];

                if (AllowedRec.Count==0 || AllowedRec.Contains(user1))
                {
                    rcInfo nrc = new rcInfo()
                    {
                        user = user1,
                        status = 0,
                        intstatus = register ? eInternalStatus.eisPenReposo : eInternalStatus.eisPenBorrado,
                        version = 0
                    };
                    _tbRec[user1] = nrc;
                    return nrc;
                }
                _Logger.Debug("PabxItfService. Informacion de Recurso no supervisado {0}", user1);
                return null;
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
            private int External2IntenalStatus(int extStatus)
            {
                switch (extStatus)
                {
                    case -1:
                        return 0;
                    case 2:
                    case 14:
                    case 35:
                    case 36:
                        return 1;
                    case 0:
                    case 1:
                    case 12:
                    case 21:
                    case 30:
                    case 65:
                        return 2;
                    default:
                        return 0;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="str"></param>
            /// <returns></returns>
            private byte[] getBytes<T>(T str)
            {
                int size = Marshal.SizeOf(str);
                byte[] arr = new byte[size];

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);
                return arr;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="arr"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            private int Copy2ByteArray<T>(ref byte[] arr, int offset, T obj)
            {
                try
                {
                    int size = Marshal.SizeOf(obj);

                    Array.Resize(ref arr, offset + size);
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(obj, ptr, true);
                    Marshal.Copy(ptr, arr, offset, size);
                    Marshal.FreeHGlobal(ptr);
                    return offset + size;
                }
                catch (Exception x)
                {
                    //_Logger.Error(x);
                    ExceptionManage<extTifx>("Copy2ByteArray", x, "Copy2ByteArray Exception: " + x.Message, false);
                    return 0;
                }
            }

            #endregion
        };

        #endregion

        #region IService Members
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return "PabxItfService"; }
        }
        /// <summary>
        /// 
        /// </summary>
        public ServiceStatus Status
        {
            get { return _Status; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Master
        {
            get { return _Master; }
        }

        /// <summary>
        /// 20161219. AGL. Para evitar Eventos de Reconfiguracion originados por SPREAD al entrar/salir elementos de red.
        /// </summary>
        protected string LastVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="resp"></param>
        public bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp = null)
        {
            return false;
        }

        /** 20170217. AGL. Nueva interfaz de comandos. Orientada a estructuras definidas en 'Infraestructure' */
        public bool DataGet(ServiceCommands cmd, ref List<Object> rsp)
        {
            return false;
        }
        /** Fin de la Modificacion */

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            try
            {
                LogInfo<PabxItfService>("Iniciando Servicio...");
                ExceptionManageInit();

                _Master = false;
                LastVersion = String.Empty;
                _Status = ServiceStatus.Running;
                _WorkingThread.Start();

                _TimerMcast = new Timer();
                _TimerPbax = new Timer();
                _TimerPabxSimulada = new Timer(5000);
                
                _udpClient = new UdpClient();

                _tifx = new extTifx();
                _tifx.Name = Properties.Settings.Default.PabxIp;

                // Inicializar WebSocket. 
                if (Properties.Settings.Default.PabxSimulada)
                {
                    _TimerPabxSimulada.AutoReset = false;
                    _TimerPabxSimulada.Elapsed += OnTimePabxSimuladaElapsed;
                    _TimerPabxSimulada.Enabled = true;
                }
                else
                {
                    PabxUrl = "ws://" + Properties.Settings.Default.PabxIp + ":" + Properties.Settings.Default.PabxWsPort +
                        "/pbx/ws?login_user=sa&login_password=" + Properties.Settings.Default.PabxSaPwd + "&user=*&registered=True&status=True&line=*";

                    _pabxws = new WebSocket(PabxUrl);
                    _pabxws.Opened += new EventHandler(websocket_Opened);
                    _pabxws.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
                    _pabxws.Closed += new EventHandler(websocket_Closed);
                    _pabxws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
                    _pabxStatus = ePabxStatus.epsDesconectado;
                }

                _TimerMcast.Interval = 5000;
                _TimerMcast.AutoReset = true;
                _TimerMcast.Elapsed += OnTimeMcastElapsed;

                _TimerPbax.Interval = Properties.Settings.Default.PabxPollTime;
                _TimerPbax.AutoReset = false;
                _TimerPbax.Elapsed += OnTimePabxElapsed;

                // Inicializar el Evento OnMasterStatusChange...
                ipcTifxservice = new InterProcessEvent("tifx_master", OnMasterStatusChanged);

                // Inicializa Subscripcion a la configuracion...
                cfgSubscriptionStart();

                LogInfo<PabxItfService>("Servicio Iniciado.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PbxItfService", CTranslate.translateResource("Servicio iniciado."));

            }
            catch (Exception ex)
            {
                Stop();
                //LogException<PabxItfService>("ERROR arrancando servicio.", ex);
                ExceptionManage<PabxItfService>("Start", ex, "Excepcion arrancando servicio " + ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            try
            {
                LogInfo<PabxItfService>("Iniciando parada servicio");

                if (_Status == ServiceStatus.Running)
                {
                    if (_Master)
                    {
                        Dispose();
                    }
                    ipcTifxservice.Dispose();
                    cfgSubscriptionStop();

                    _Status = ServiceStatus.Stopped;
                }
            }
            catch (Exception x)
            {
                //LogException<PabxItfService>("Error en Stop", x);
                ExceptionManage<PabxItfService>("Stop", x, "Excepcion Deteniendo Servicio: " + x.Message);
            }

            _WorkingThread.Stop();
            LogInfo<PabxItfService>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PbxItfService", CTranslate.translateResource("Servicio detenido."));
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public string PabxUrl
        {
            get;
            set;
        }


        #region Formatos de Tablas..

        /// <summary>
        /// 
        /// </summary>
        class pabxParamInfo
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

        class pabxEvent
        {
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public pabxParamInfo parametros { get; set; }
        };

        #endregion

        #region Private Members

        enum ePabxStatus { epsDesconectado, epsConectando, epsConectado };

        /// <summary>
        /// 
        /// </summary>
        // private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        private bool _Master = false;
        private ePabxStatus _pabxStatus = ePabxStatus.epsDesconectado;

        /// <summary>
        /// 
        /// </summary>
        private ServiceStatus _Status = ServiceStatus.Stopped;
        /// <summary>
        /// 
        /// </summary>
        private EventQueue _WorkingThread = new EventQueue();

        private WebSocket _pabxws=null;
        private extTifx _tifx;
        
        private Timer _TimerMcast = null;
        private Timer _TimerPbax = null;
        private Timer _TimerPabxSimulada = null;

        private UdpClient _udpClient = null;
        IPEndPoint mcastTifx = new IPEndPoint(IPAddress.Parse(Properties.Settings.Default.TifxMcastIp), Properties.Settings.Default.TifxMcastPort);
        InterProcessEvent ipcTifxservice = null;
 

        #endregion

        #region Callbacks

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="master"></param>
        // private void OnMasterStatusChanged(object sender, bool master)
        private void OnMasterStatusChanged(object sender, SpreadDataMsg msg)
        {
            _WorkingThread.Enqueue("OnMasterStatusChanged", delegate()
            {
                bool master = false;
                try
                {
                    MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                    master = Serializer.Deserialize<bool>(ms);

                    if (master && !_Master)
                    {
                        Init();
                    }
                    else if (!master && _Master)
                    {
                        Dispose();
                    }
                    _Master = master;
                    LastVersion = string.Empty;
                    LogInfo<PabxItfService>(_Master ? "MASTER" : "SLAVE", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PbxItfService",
                        _Master ? "MASTER" : "SLAVE");
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("OnMasterStatusChanged", x);
                    ExceptionManage<PabxItfService>("OnMasterStatusChanged", x, "Excepcion en OnMasterStatusChanged => " + master.ToString() + ". " + x.Message);
                }
            }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Opened(object sender, EventArgs e)
        {
            _WorkingThread.Enqueue("websocket_Opened", delegate()
            {
                try
                {
                    LogInfo<PabxItfService>(String.Format("WebSocket Abierto en {0}.", PabxUrl)/*, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                        "PbxItfService", String.Format("WebSocket Abierto en {0}.", PabxUrl)*/);
                }
                catch (Exception x)
                {
                    //LogError<PabxItfService>(x.Message);
                    ExceptionManage<PabxItfService>("websocket_Opened", x, "On websocket_Opened Exception: " + x.Message);
                }
            }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _WorkingThread.Enqueue("websocket_Error", delegate()
            {
                try
                {
                    LogError<PabxItfService>(String.Format("WebSocketError en {0}: {1}", PabxUrl, e.Exception.Message));
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("websocket_error", x);
                    ExceptionManage<PabxItfService>("websocket_error", x, "On websocket_error exception: " + x.Message);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Closed(object sender, EventArgs e)
        {
            _WorkingThread.Enqueue("websocket_Closed", delegate()
            {
                try
                {
                    if (_pabxStatus == ePabxStatus.epsConectado)
                    {
                        LogInfo<PabxItfService>(String.Format("WebSocket en {0} Cerrado.", PabxUrl)/*, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                        "PbxItfService", "WebSocket Cerrado."*/);
                    }
                    _pabxStatus = ePabxStatus.epsDesconectado;
                    /* Desregistrar todo. */
                    _tifx.SetAllUnregistered();
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("websocket_Closed", x);
                    ExceptionManage<PabxItfService>("websocket_closed", x, "On websocket_closed exception: " + x.Message);
                }
            }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _WorkingThread.Enqueue("websocket_MessageReceived", delegate()
            {
                try
                {
                    LogDebug<PabxItfService>(String.Format("WebSocket en {0}: Mensaje Recibido: {1}", PabxUrl, e.Message));

                    string msg = e.Message.Replace("params", "parametros");

                    if (msg.StartsWith("{"))
                    {
                        pabxEvent _evento = JsonConvert.DeserializeObject<pabxEvent>(msg);
                        ProcessEvent(_evento);
                    }
                    else if (msg.StartsWith("["))
                    {
                        pabxEvent[] _eventos = JsonConvert.DeserializeObject<pabxEvent[]>(msg);
                        foreach (pabxEvent _evento in _eventos)
                        {
                            ProcessEvent(_evento);
                        }
                    }
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("websocket_MessageReceived", x);
                    ExceptionManage<PabxItfService>("ws_message", x, "On ws_message exception: " + x.Message);
                }
            }
            );

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        int _refreshCounter = 0;
        private void OnTimeMcastElapsed(object sender, ElapsedEventArgs e)
        {
            _WorkingThread.Enqueue("OnTimeMcastElapsed", delegate()
            {
                try
                {
                    if (_Master)
                    {
//                        if (_pabxStatus == ePabxStatus.epsConectado)
                        {
                            
                            // Cada 5 tramas hago un refresco completo.
                            if (((++_refreshCounter) % 5) == 0)
                            {
                                _tifx.SetAllRefresh();
                            }

                            // Manda la Trama...
                            byte[] mtrama = _tifx.mcBinaryFrame();

#if DEBUG 
                            /** OJO. PRUEBA. Quitar.... La mando 10 veces seguidas.*/
                            for (int i=0; i<1; i++)
#endif
                            _udpClient.Send(mtrama, mtrama.Count(), mcastTifx);
                        }
                    }
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("OnTimeMcastElapsed", x);
                    ExceptionManage<PabxItfService>("TimeMcastElapsed", x, "OnTimeMcastElapsed exception: " + x.Message);
                }
            }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimePabxElapsed(object sender, ElapsedEventArgs e)
        {
            _WorkingThread.Enqueue("OnTimePabxElapsed", delegate()
            {
                try
                {
                    if (_Master && Properties.Settings.Default.PabxSimulada == false)
                    {
                        switch (_pabxStatus)
                        {
                            case ePabxStatus.epsDesconectado:
                                if (Ping(Properties.Settings.Default.PabxIp))
                                {
                                    _pabxStatus = ePabxStatus.epsConectando;
                                    _pabxws.Open();
                                }
                                break;

                            case ePabxStatus.epsConectando:
                                break;

                            case ePabxStatus.epsConectado:
                                if (!Ping(Properties.Settings.Default.PabxIp))
                                {
                                    LogWarn<PabxItfService>("Fallo de Ping....Cierro WS", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                                        "PbxItfService", CTranslate.translateResource("Fallo PING"));
                                    _pabxws.Close();
                                    _pabxStatus = ePabxStatus.epsDesconectado;
                                    /* Desregistrar todo */
                                    _tifx.SetAllUnregistered();
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                catch (Exception x)
                {
                    //LogException<PabxItfService>("OnTimePabxElapsed", x);
                    ExceptionManage<PabxItfService>("TimePabxElapsed", x, "OnTimePabxElapsed exception: " + x.Message);
                }
                finally
                {
                    _TimerPbax.Enabled = true;
                }
            }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        pabxParamInfo infoSimul = new pabxParamInfo() { user = "41001", registered = "false", status = "0" };
        pabxParamInfo infoSimulNoRegistrado = new pabxParamInfo() { user = "41002", registered = "false", status = "0" };
        enum epabxSimulState { eStd1, eStd2, eStd3, eStd4 };
        epabxSimulState _simulState = epabxSimulState.eStd1;
        private void OnTimePabxSimuladaElapsed(object sender, ElapsedEventArgs e)
        {
            _WorkingThread.Enqueue("OnTimePabxSimuladaElapsed", delegate()
            {
                switch (_simulState)
                {
                    case epabxSimulState.eStd1:
                        infoSimulNoRegistrado.registered = "true";
                        ProcessUserRegistered(infoSimulNoRegistrado);

                        infoSimul.registered = "true"; 
                        ProcessUserRegistered(infoSimul);
                        _simulState = epabxSimulState.eStd2;
                        break;

                    case epabxSimulState.eStd2:
                        infoSimul.status = "1";
                        ProcessUserStatus(infoSimul);
                        _simulState = epabxSimulState.eStd3;
                        break;
                    case epabxSimulState.eStd3:
                        infoSimul.status = "14";
                        ProcessUserStatus(infoSimul);
                        _simulState = epabxSimulState.eStd4;
                        break;
                    case epabxSimulState.eStd4:
                        infoSimul.status = "-1";
                        ProcessUserStatus(infoSimul);

                        infoSimulNoRegistrado.registered = "false";
                        ProcessUserRegistered(infoSimulNoRegistrado);

                        _simulState = epabxSimulState.eStd1;
                        break;
                }

                _TimerPabxSimulada.Enabled = true;
            });

        }


        #endregion

        #region Private Functions.
        /// <summary>
        /// 
        /// </summary>
        private void Init()
        {
            LogInfo<PabxItfService>("INIT");

            if (Properties.Settings.Default.PabxSimulada)
                _pabxStatus = ePabxStatus.epsConectado;
            else
            {
                _pabxStatus = ePabxStatus.epsConectando;
                _pabxws.Open();
            }

            //
            _TimerMcast.Enabled = true;
            _TimerPbax.Enabled = true;

            //
            _udpClient.MulticastLoopback = false;
            _udpClient.JoinMulticastGroup(IPAddress.Parse(Properties.Settings.Default.TifxMcastIp), IPAddress.Parse(Properties.Settings.Default.TifMcastItf));
        }
        /// <summary>
        /// 
        /// </summary>
        private void Dispose()
        {
            _TimerMcast.Enabled = false;
            _TimerPbax.Enabled = false;

            _udpClient.DropMulticastGroup(IPAddress.Parse("224.100.10.1"));

            if (Properties.Settings.Default.PabxSimulada)
            {
                _pabxStatus = ePabxStatus.epsDesconectado;
            }
            else
            {
                if (_pabxStatus == ePabxStatus.epsConectado)
                {
                    _pabxws.Close();
                    _pabxStatus = ePabxStatus.epsDesconectado;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_event"></param>
        private void ProcessEvent(pabxEvent _event)
        {
            switch (_event.method)
            {
                case "notify_serverstatus":
                    ProcessServerStatus(_event.parametros);
                    break;
                case "notify_status":
                    ProcessUserStatus(_event.parametros);
                    break;
                case "notify_registered":
                    ProcessUserRegistered(_event.parametros);
                    break;
                default:
                    LogError<PabxItfService>(String.Format("Evento {0} Desconocido", _event.method), U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR,
                        "PbxItfService", CTranslate.translateResource("Evento {0} desconocido",_event.method));
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessServerStatus(pabxParamInfo info)
        {
            switch (info.status)
            {
                case "active":
                   _pabxStatus = ePabxStatus.epsConectado;
                    break;
                default:
                    _pabxws.Close();
                    _pabxStatus = ePabxStatus.epsDesconectado;
                    break;
            }
            LogDebug<PabxItfService>(String.Format("Server Status: {0}", info.status));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessUserRegistered(pabxParamInfo info)
        {
            bool registrado = info.registered == "true";

            _tifx.rcRegister(info.user, registrado);
            LogDebug<PabxItfService>(String.Format("Procesado Registro Usuario {0}, {1}", info.user, info.registered));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessUserStatus(pabxParamInfo info)
        {
            _tifx.rcStatus(info.user, int.Parse(info.status));
            LogInfo<PabxItfService>(String.Format("Procesado Estado Usuario {0}, Estado: {1}", info.user, info.status));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool Ping(string host)
        {
            int reint = 0;
            PingReply reply;
            do
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128, 
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "Ulises V 5000i. PabxItfService..";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;  // ms
                reply = pingSender.Send(host, timeout, buffer, options);
                reint++;
                System.Threading.Thread.Sleep(10);
            } while (reply.Status != IPStatus.Success && reint < 3);
            return reply.Status == IPStatus.Success ? true : false;
        }

        #endregion

        #region _SUBSCRIBE_CFG_

#if _SUBSCRIBE_CFG_
        Registry _Registry = null;
#endif
        /// <summary>
        /// 
        /// </summary>
        void cfgSubscriptionStart()
        {
#if _SUBSCRIBE_CFG_
            _Registry = new Registry("uv5kPabxSr");
            //_Registry.ChannelError += OnChannelError;
            //_Registry.MasterStatusChanged += OnMasterStatusChanged;
            _Registry.ResourceChanged += OnResourceChanged;

            _Registry.SubscribeToTopic<Cd40Cfg>(Identifiers.CfgTopic);
            _Registry.Join(Identifiers.CfgTopic);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        void cfgSubscriptionStop()
        {
#if _SUBSCRIBE_CFG_
            if (_Registry != null)
            {
                _Registry.Dispose();
                _Registry = null;
            }
#endif
        }

#if _SUBSCRIBE_CFG_
        private void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (e.Content != null && e.Type == Identifiers.TypeId(typeof(Cd40Cfg)))
            {

                _WorkingThread.Enqueue("OnResourceChanged", delegate()
                {
                    MemoryStream ms = new MemoryStream(e.Content);
                    Cd40Cfg cfg = Serializer.Deserialize<Cd40Cfg>(ms);

                    /** 20161219. AGL. Para evitar Eventos de Reconfiguracion originados por SPREAD al entrar/salir elementos de red. */
                    if (LastVersion == cfg.Version)
                        return;
                    LastVersion = cfg.Version;
                    LogInfo<PabxItfService>("Procesando Configuracion....", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                        "PbxItfService", CTranslate.translateResource("Procesando configuración..."));
                    /**********************/

                    _tifx.AllowedRec.Clear();

                    foreach (ConfiguracionUsuario user in cfg.ConfiguracionUsuarios)
                    {
                        foreach (CfgEnlaceInterno tlink in user.TlfLinks)
                        {
                            foreach (CfgRecursoEnlaceInterno rec in tlink.ListaRecursos)
                            {
                                string recName = rec.NombreRecurso;
                                if (recName != "")
                                {
                                    foreach (AsignacionRecursosGW asgRec in cfg.ConfiguracionGeneral.PlanAsignacionRecursos)
                                    {
                                        if (asgRec.IdRecurso == recName)
                                        {
                                            String host = asgRec.IdHost;
                                            /** */
                                            foreach (DireccionamientoIP ipHost in cfg.ConfiguracionGeneral.PlanDireccionamientoIP)
                                            {
                                                if (ipHost.IdHost == host && (ipHost.TipoHost == Tipo_Elemento_HW.TEH_EXTERNO_RADIO ||
                                                                                 ipHost.TipoHost == Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA))
                                                {
                                                    LogDebug<PabxItfService>(String.Format("Encontrado Recurso IP-Externo en {0},{1},{2}", user.User.Nombre, recName, ipHost.TipoHost));
                                                    if (_tifx.AllowedRec.Contains(recName) == false)
                                                    {
                                                        _tifx.AllowedRec.Add(recName);
                                                        LogDebug<PabxItfService>(String.Format("Añadido Recurso IP a Supervisar: {0}", recName));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    /** Gestionar la Tabla ON-LINE para Eliminar Recursos en Cambios de Configuracion */
                    _tifx.TbRecCheck();

                    // Añade a la tabla de recursos los no presentes para establecer condiciones iniciales..
                    //foreach (var item in _tifx.AllowedRec)
                    //{
                    //    if (_tifx.TbRec.ContainsKey(item) == false)
                    //    {
                    //        ProcessUserRegistered(new pabxParamInfo() { user = item, registered = "false" });
                    //    }
                    //}

                    /* Dictionary<string, rcInfo> */
                });
            }
        }
#endif
        #endregion
    }
}

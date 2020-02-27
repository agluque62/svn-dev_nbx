#define _NETWORK_SUP_
#define _NETWORK_SUP_POLLING_
#define _HISTPROC_
#define _NBX_SPLITTED_
#define _NEW_WEB_SERVER_
// #define _SIM_ERRORES_RED_
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Configuration;
using NLog;
using Newtonsoft.Json;

using Utilities;
using U5ki.Infrastructure;
using Translate;

namespace U5ki.NodeBox
{
    public static class ServiceNames
    {
        public const string RadioService = "Cd40RdService";
        public const string CfgService = "Cd40ConfigService";
        public const string TifxService = "Cd40TifxService";
        public const string PresenceService = "PresenceServer";
    }
#if !_NEW_WEB_SERVER_
    /// <summary>
    /// BORRAR
    /// </summary>
    public enum CmdSupervision
    {
        cmdSrvStd,          // Peticion Estado de los Servicios.
        cmdCfgIdAct,        // Peticion ID de configuracion activa.
        cmdCfgLst,          // Peticion de Lista de Preconfiguracions.
        cmdCfgDel,          // Borrado de Preconfiguracion
        cmdCfgAct,          // Activado de Preconfiguracion
        cmdCfgSav,          // Salvar configuracion activa como...

        cmdHFGet,
        cmdHFStd,
        cmdHFLib,

        cmdRdSessions,      // Obtener la lista de sesiones radio...
        cmdRdMNEquip,       // Obtener la lista de Equipos en M+N
        cmdRdMNReset,
        cmdRdMNEnable,
        cmdRdMNAsigna,
        cmdRdMNConfigureTick,

        cmdTlfInfoGet,       // Obtiene la informacion de TIFX
        cmdPresenceInfoGet,  // Obtiene la informacion del servidor de presencia.

        /** 20200224 Gestio 1+1 */
        cmdRdUnoMasUnoData,
        cmdRdUnoMasUnoSelect,

        /** 20180625. AGL. Comandos DEBUG */
        cmdRdServiceDebugInfo,
    }
#endif

    /// <summary>
    /// 20181130. Para identificar el tipo de servidor de servicios.
    /// </summary>
    public enum ServicesServerTypes { Mixed, Radio, Phone }
    public enum ServiceMasterStates { Master, Slave, Error }

#if !_NEW_WEB_SERVER_
    /// <summary>
    /// BORRAR
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="datain"></param>
    /// <returns></returns>
    public delegate object WebSrvCommandHandler(CmdSupervision cmd, object datain);
#endif

    /// <summary>
    /// 
    /// </summary>
    public class LoggingClass : BaseCode
    {
        public LoggingClass() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="x"></param>
        /// <param name="generarHistorico"></param>
        public void ExceptionManager(string msg, Exception x, bool generarHistorico = false)
        {
            LogException<NodeBoxSrv>(msg, x, generarHistorico);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="generarHistorico"></param>
        public void InfoManager(string msg, bool generarHistorico = false)
        {
            LogInfo<NodeBoxSrv>(msg, generarHistorico == true ? U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO : U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="generarHistorico"></param>
        public void ErrorManager(string msg, bool generarHistorico = false)
        {
            LogError<NodeBoxSrv>(msg, generarHistorico == true ? U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR : U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
    }

    /// <summary>
    /// 
    /// </summary>
	public partial class NodeBoxSrv : ServiceBase
    {
        /// 20181130. Para identificar el tipo de servidor de servicios.
        static public ServicesServerTypes ServerType { get; set; }
        /// <summary>
        /// 
        /// </summary>
		public NodeBoxSrv()
        {
            /** AGL. Set el Current directory.. */
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            /** AGL. Set Global language **/
            Environment.SetEnvironmentVariable("idioma", Translate.CTranslate.Idioma);

            InitializeComponent();

            if (Directory.Exists(Application.StartupPath + "\\Services"))
            {
                string[] dlls = Directory.GetFiles(Application.StartupPath + "\\Services", "*.dll");

                foreach (string path in dlls)
                {
                    Assembly dll = Assembly.LoadFrom(path);
                    Type[] types = dll.GetExportedTypes();

                    foreach (Type type in types)
                    {
                        if (type.GetInterface("IService") != null)
                        {
                            IService srv = (IService)Activator.CreateInstance(type);
                            /** 20160413. Arrancar el Servicio PABX solo si está habilitado */
                            /** 20180208. Este servicio se sustituye por el de presencia....
                            if (srv.Name == "PabxItfService" && U5ki.NodeBox.Properties.Settings.Default.Pabx == false)
                                continue;
                             * */
                            /** 20181130. Control de servicos repetidos. */
                            //_Services.Add(srv);                            
                            if (_Services.Keys.Contains(srv.Name) == false)
                                _Services[srv.Name] = srv;
                        }
                    }
                }
            }

            /** 20190408. En 259 esto da problemas. Reconsiderar en 2.5.10*/
            //            /// 20181130. Identificar el tipo de servidor de servicios.
            //            if (RadioService != null && CfgService != null && PhoneService == null && TifxService == null && PresenceService == null)
            //            {
            //                ServerType = ServicesServerTypes.Radio;
            //                ServicesHelpers.VersionsFileAdjust("RadioService", new List<string>() { "CfgService", "RdService", "RemoteControlService" });
            //        }
            //            else if (RadioService == null && CfgService == null && PhoneService != null && TifxService != null && PresenceService != null)
            //            {
            //                ServerType = ServicesServerTypes.Phone;
            //                ServicesHelpers.VersionsFileAdjust("PhoneService", new List<string>() { "TifxService", "PresenceService", "PhoneService" });
            //            }
            //#if PHONE_AND_CFG
            //            else if (RadioService == null && PhoneService != null && TifxService != null && PresenceService != null)
            //            {
            //                ServerType = ServicesServerTypes.Phone;
            //                ServicesHelpers.VersionsFileAdjust("PhoneServiceCfg", new List<string>() { "TifxService", "PresenceService", "PhoneService", "CfgService" });
            //            }
            //#endif
            //            else
            //            {
            //                ServerType = ServicesServerTypes.Mixed;
            //                ServicesHelpers.VersionsFileAdjust("Nodebox", null);
            //            }

            // VersionsFileAdjust();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static bool bConsole = false;
        static bool mcast_running = false;
        public static void Run(string[] args)
        {
            try
            {
                // Sincronizacion con MCAST
                mcast_running = Utilities.StartSync.ProcessRunningSync("u5ki.mcast", 20);

                NodeBoxSrv server = new NodeBoxSrv();

                if ((args.Length == 1) && (args[0] == "-start"))
                {
                    using (ServiceController sc = new ServiceController(server.ServiceName))
                    {
                        sc.Start();
                    }
                }
                if ((args.Length == 1) && (args[0] == "-stop"))
                {
                    using (ServiceController sc = new ServiceController(server.ServiceName))
                    {
                        sc.Stop();
                    }
                }
                else if ((args.Length == 1) && (args[0] == "-console"))
                {
                    bConsole = true;
#if DEBUG
                    Console.WriteLine("Pulse ENTER para Iniciar...");
                    Console.ReadLine();
#endif
                    do
                    {
                        server.ServiceMain();
                    } while (Console.ReadKey().Key != ConsoleKey.Escape);
                }
                else if (args.Length == 0)
                {
                    ServiceBase.Run(server);
                }
            }
            catch (Exception e)
            {
                _Logger.ExceptionManager("ERROR arrancando nodo", e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
		protected override void OnStart(string[] args)
        {
            _ServiceMainTh = new Thread(ServiceMain);
            _ServiceMainTh.IsBackground = true;
            _ServiceMainTh.Start();

            base.OnStart(args);
        }
        /// <summary>
        /// 
        /// </summary>
		protected override void OnStop()
        {
            _EndEvent.Set();
            if (_ServiceMainTh != null)
            {
                _ServiceMainTh.Join();
            }

            base.OnStop();
        }

#region Private Members
        /// <summary>
        /// 
        /// </summary>
        // static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Para los LOG'S...
        /// </summary>
        static LoggingClass _Logger = new LoggingClass();
        /// <summary>
        /// 
        /// </summary>
		ManualResetEvent _EndEvent = new ManualResetEvent(false);
        /// <summary>
        /// 
        /// </summary>
        Dictionary<String, IService> _Services = new Dictionary<string, IService>();

        /// <summary>
        /// 
        /// </summary>
		Thread _ServiceMainTh;
#if _HISTPROC_
        /// <summary>
        /// 
        /// </summary>
        HistProc _histproc = null;  // new HistProc();
#endif
#if !_NEW_WEB_SERVER_
        /// <summary>
        /// BORRAR
        /// </summary>
        NbxWebServer _webServer = new NbxWebServer();
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger log = LogManager.GetLogger("UnHandledException");
            try
            {
                log.Fatal("OnUnhandledException event [{0},{1}]", e.ExceptionObject, e.IsTerminating);
                if (e.ExceptionObject is Exception)
                {
                    Exception x = (Exception)e.ExceptionObject;
                    // _Logger.ExceptionManager("UnHandledException", e);
                    log.Fatal("Excepcion: {0}", x.Message);
                    log.Fatal("Source   : {0}", x.Source);
                    log.Fatal("Tipo:    : {0}", x.GetType().FullName);
                    log.Fatal("Stack    : {0}", x.StackTrace);
                    if (x.InnerException != null)
                    {
                        Exception i = x.InnerException;
                        log.Fatal("    Innner Excepcion: {0}", i.Message);
                        log.Fatal("           Source   : {0}", i.Source);
                        log.Fatal("           Tipo:    : {0}", i.GetType().FullName);
                        log.Fatal("           Stack    : {0}", i.StackTrace);
                    }
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
            /** */
            log.Fatal("Saliendo de aplicacion");
            System.Environment.Exit(0);
        }

        /// <summary>
        /// 
        /// </summary>
        int _cntRun = 0;
        void ServiceMain()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Environment.CurrentDirectory = Path.GetDirectoryName(Application.ExecutablePath);

            _Logger.InfoManager("Iniciando Arranque.");
            if (!mcast_running)
                _Logger.ErrorManager("MCAST no está ACTIVO !!!!");
            try
            {
                /** */
#if _NETWORK_SUP_
                SetNetworkSupervision();
#endif

                /** Gestor Centralizado de Historicos */
                _Logger.InfoManager("Arrancando Gestor Incidencias...");
#if _HISTPROC_
                _histproc = new HistProc();
                _histproc.Inicializa();
#endif

#if !_NETWORK_SUP_
                _Logger.Info("NodeBOX: " + "Arrancando Servicios...");
                foreach (IService srv in _Services)
                {
                    if (srv.Status == ServiceStatus.Stopped)
                    {
                        srv.Start();
                    }
                }
#endif
                _Logger.InfoManager(CTranslate.translateResource("Iniciado"), true);

            }
            catch (Exception x)
            {
                _Logger.ExceptionManager("ServiceMain", x);
                return;
            }

            /** Lazo de Supervision */
#if _NETWORK_SUP_
            do
            {
#if _NETWORK_SUP_POLLING_
                NetworkStatusPolling(false);
#endif
                //if (bConsole == true && Console.KeyAvailable == true && Console.ReadKey().Key == ConsoleKey.Escape)
                //    break;
                if (bConsole == true && Console.KeyAvailable == true)
                {
                    // Salir del Lazo con ESCAPE...
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                        break;
                    switch (key)
                    {
#if DEBUG
                        case ConsoleKey.T:          // Test...
                            {
                            }
                            break;

                        case ConsoleKey.S:          // Cambiar a Slave.
                            string err = "";
                            RadioService.Commander(ServiceCommands.SrvDbg, "S", ref err);
                            break;

                        case ConsoleKey.M:
                            string err1 = "";
                            RadioService.Commander(ServiceCommands.SrvDbg, "M", ref err1);
                            break;
#endif
                        case ConsoleKey.Spacebar:
                            Console.Clear();
                            break;

                        default:
                            break;
                    }
                }

                if (_isNetworkOnline == true)
                {
                    /** 20180309. Arranque del Sip AGENT y del Servicio WEB ... */
                    if (SipAgentStarted == false)
                    {
                        SipAgentStart();
                    }
                    if (WebServerStarted == false)
                    {
                        WebServerStart();
                    }

                    /** 20181130. Supervison de los servicios*/
                    _Services.Where(s => s.Value.Status == ServiceStatus.Stopped).ToList().ForEach(s => { StartService(s.Value); });

                    /** 20170725. La presencia informa del estado de todos sus servicios. */
                    Presencia();

                    /** 20170614. Query IGMP cada x minutos si habilitado y master */
                    IgmpQuery();
                }
                else
                {
                    /** 20181130. Supervison de los servicios*/
                    _Services.Where(s => s.Value.Status != ServiceStatus.Stopped).ToList().ForEach(s => { StopService(s.Value); });

                    if (SipAgentStarted == true)
                    {
                        SipAgentStop();
                    }
                    if (WebServerStarted == true)
                    {
                        WebServerStop();
                    }
                }

            } while (!_EndEvent.WaitOne(1000, false));
#else
			while (!_EndEvent.WaitOne(1000, false))
			{
				foreach (IService srv in _Services)
				{
					if (srv.Status == ServiceStatus.Stopped)
					{
                            srv.Start();
					}
				}
                Presencia();
			}
#endif
            /** Tareas de Finalizacion.... */
            try
            {
                _Logger.InfoManager(CTranslate.translateResource("Deteniendo servicios."), true);
                _Services.Where(s => s.Value.Status != ServiceStatus.Stopped).ToList().ForEach(s => { StopService(s.Value); });

                /** */
#if _HISTPROC_
                _Logger.InfoManager("Deteniendo Gestor Incidencias.");
                _histproc.Finaliza();
                _histproc = null;
#endif
                if (WebServerStarted == true)
                    WebServerStop();

                ///** 20180208. Inicializa el SipAgent para que pueda se utilizado por diferentes servicios */
                //SipAgent.End();
                if (SipAgentStarted == true)
                    SipAgentStop();

                /** */
                _Logger.InfoManager(String.Format("Nodo detenido {0}.", ++_cntRun));
            }
            catch (Exception x)
            {
                _Logger.ExceptionManager("Excepcion en Finalizacion.", x);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private UdpClient nbxLocalSocket = null;    // new UdpClient();
        static int cuenta = 0;
        /// <summary>
        /// 20170724. Se incluye informacion de todos los servicios y del puerto WEB...
        /// Envio: |CfgService|RdService|TifxService|PabxService|Libre|Libre|Libre|Libre|WEBPORT-LB|WEBPORT-HB|
        /// Valores Estado Servicios:
        ///     0: Running / Slave.
        ///     1: Running / Master.
        ///     2: Not Running.
        ///     3: Not Active.
        /// </summary>
        private void Presencia()
        {
            U5ki.NodeBox.Properties.Settings settings = U5ki.NodeBox.Properties.Settings.Default;
            if ((cuenta % 2) == 0)
            {
                try
                {
                    //if (nbxLocalSocket == null)
                    //    nbxLocalSocket = new UdpClient(new IPEndPoint(IPAddress.Parse(settings.IpPrincipal), 0));
                    //IPEndPoint dst = new IPEndPoint(IPAddress.Parse(settings.HistServer), settings.PuertoPresencia);

                    //Byte[] msg = 
                    //{
                    //    CfgService==null ? (Byte)3 : CfgService.Status== ServiceStatus.Running ? (CfgService.Master ? (Byte)1 : (Byte)0) : (Byte)2,
                    //    RadioService==null ? (Byte)3 : RadioService.Status== ServiceStatus.Running ? (RadioService.Master ? (Byte)1 : (Byte)0) : (Byte)2,
                    //    TifxService==null ? (Byte)3 : TifxService.Status== ServiceStatus.Running ? (TifxService.Master ? (Byte)1 : (Byte)0) : (Byte)2,
                    //    PresenceService==null ? (Byte)3 : PresenceService.Status== ServiceStatus.Running ? (PresenceService.Master ? (Byte)1 : (Byte)0) : (Byte)2,
                    //    // PabxService==null ? (Byte)3 : PabxService.Status== ServiceStatus.Running ? (PabxService.Master ? (Byte)1 : (Byte)0) : (Byte)2,
                    //    (Byte)0xff,(Byte)0xff,(Byte)0xff,(Byte)0xff,
                    //    (Byte)(settings.PuertoControlRemoto & 0xff),
                    //    (Byte)(settings.PuertoControlRemoto >> 8)
                    //};
                    //nbxLocalSocket.EnableBroadcast = true;
                    //nbxLocalSocket.Send(msg, msg.Length, dst);

                    /** 20181130. Cambio el formato a TXT/JSON */
                    using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        IPEndPoint dst = new IPEndPoint(IPAddress.Parse(settings.HistServer), settings.PuertoPresencia);
                        var data = new
                        {
                            Machine = Environment.MachineName,
                            ServerType = ServerType.ToString(),
                            GlobalMaster = GlobalMasterState.ToString(),
                            RadioService = ServiceMasterStateByValue(RadioService).ToString(),
                            CfgService = ServiceMasterStateByValue(CfgService).ToString(),
                            PhoneService = ServiceMasterStateByValue(PhoneService).ToString(),
                            TifxService = ServiceMasterStateByValue(TifxService).ToString(),
                            PresenceService = ServiceMasterStateByValue(PresenceService).ToString(),
                            WebPort = settings.PuertoControlRemoto,
                            TimeStamp = DateTime.Now
                        };

                        sock.SendTo(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data)), dst);
                    }
                }
                catch (Exception x)
                {
                    _Logger.ExceptionManager("Presencia.v2", x);
                }
            }
            cuenta++;
        }

        /// <summary>
        /// 20170614. El servidor Radio Activo, genera un IgmpQuery cada x segundos.
        /// </summary>
        static DateTime lastIgmpQuery = DateTime.MinValue;
        private void IgmpQuery()
        {
            if (RadioService != null && RadioService.Master == true && Properties.Settings.Default.IgmpQueryPeriodSeconds > 0)
            {
                TimeSpan ts = DateTime.Now - lastIgmpQuery;
                if (ts.TotalSeconds > Properties.Settings.Default.IgmpQueryPeriodSeconds)
                {
                    lastIgmpQuery = DateTime.Now;
                    RawSockets.IgmpQuery(Properties.Settings.Default.IpPrincipal);
                }
            }
        }

#if !_NEW_WEB_SERVER_
        /// <summary>
        /// BORRAR
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private object OnWebServerCommand(CmdSupervision cmd, object data)
        {
            switch (cmd)
            {
                case CmdSupervision.cmdSrvStd:
                    {
                        //stdGlobal std = new stdGlobal();

                        //IService rad = RadioService;
                        //IService cfg = CfgService;
                        //IService ifx = TifxService;
                        //// IService pbx = PabxService;
                        //IService ps = PresenceService;

                        //std.rad = rad == null ? 0 : rad.Master ? 2 : 1;
                        //std.cfg = cfg == null ? 0 : cfg.Master ? 2 : 1;
                        //std.ifx = ifx == null ? 0 : ifx.Master ? 2 : 1;
                        //std.pbx = ps == null ? 0 : ps.Master ? 2 : 1;

                        //string err = string.Empty;
                        //cfg.Commander(ServiceCommands.GetDefaultConfigId, "", ref err);
                        //std.cfg_activa = err;

                        ///** 20160928. AGL. Estado del Servicio MN*/
                        //rad.Commander(ServiceCommands.RdMNStatus, "", ref err);
                        //std.mn = err;

                        /** */
                        string cfgInfo = string.Empty;
                        if (CfgService != null)
                            CfgService.Commander(ServiceCommands.GetDefaultConfigId, "", ref cfgInfo);
                        string mnInfo = "Disabled";
                        if (RadioService != null)
                            RadioService.Commander(ServiceCommands.RdMNStatus, "", ref mnInfo);

                        var stdGlobal = new
                        {
                            cfg_activa = cfgInfo,
                            sw_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                            type = ServerType.ToString(),
                            level = GlobalMasterState.ToString(),
                            rad = new
                            {
                                std = ServiceStatusByValue(RadioService).ToString(),
                                level = ServiceMasterStateByValue(RadioService).ToString()
                            },
                            mn = new
                            {
                                std = mnInfo,
                                level = ServiceMasterStateByValue(RadioService).ToString()
                            },
                            cfg = new
                            {
                                std = ServiceStatusByValue(CfgService).ToString(),
                                level = ServiceMasterStateByValue(CfgService).ToString()
                            },
                            ifx = new
                            {
                                std = ServiceStatusByValue(TifxService).ToString(),
                                level = ServiceMasterStateByValue(TifxService).ToString()
                            },
                            pre = new
                            {
                                std = ServiceStatusByValue(PresenceService).ToString(),
                                level = ServiceMasterStateByValue(PresenceService).ToString()
                            },
                            pho = new
                            {
                                std = ServiceStatusByValue(PhoneService).ToString(),
                                level = ServiceMasterStateByValue(PhoneService).ToString()
                            }
                        };

                        return stdGlobal;
                    }

                case CmdSupervision.cmdCfgLst:                      // Lista de Preconfiguraciones.
                    {
                        IService cfg = CfgService;
                        List<pcfData> lcfg = new List<pcfData>();
                        if (cfg != null && cfg.Master == true)
                        {
                            DirectoryInfo dir = new DirectoryInfo(".\\");
                            StringBuilder stb = new StringBuilder();
                            foreach (FileInfo file in dir.GetFiles("u5ki.DefaultCfg.*.bin"))
                            {
                                lcfg.Add(new pcfData() { fecha = file.CreationTime.ToShortDateString(), nombre = file.Name.Split('.')[2] });
                            }
                        }
                        return lcfg;
                    }

                case CmdSupervision.cmdCfgSav:                      // Salvar Configuracion activa como...
                    {
                        IService cfg = CfgService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (cfg != null && cfg.Master == true)
                        {
                            string err = string.Empty;
                            cfg.Commander(ServiceCommands.SetDefaultCfg, ((pcfData)data).nombre, ref err);
                            resultado.res = err;
                        }
                        return resultado;
                    }

                case CmdSupervision.cmdCfgDel:                      // Borrar Preconfiguracion
                    {
                        IService cfg = CfgService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (cfg != null && cfg.Master == true)
                        {
                            string err = string.Empty;
                            //name = name.Replace("%20", " ");
                            cfg.Commander(ServiceCommands.DelDefaultCfg, ((string)data), ref err);
                            resultado.res = err;
                        }
                        return resultado;
                    }

                case CmdSupervision.cmdCfgAct:                      // Activar Preconfiguracion
                    {
                        IService cfg = CfgService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (cfg != null && cfg.Master == true)
                        {
                            string err = string.Empty;
                            //name = name.Replace("%20", " ");
                            cfg.Commander(ServiceCommands.LoadDefaultCfg, ((pcfData)data).nombre, ref err);
                            resultado.res = err;
                        }
                        return resultado;
                    }

                case CmdSupervision.cmdRdSessions:
                    {
                        IService rad = RadioService;
                        List<GlobalTypes.radioSessionData> sessions = new List<GlobalTypes.radioSessionData>();
                        if (rad != null && rad.Master == true)
                        {
#if _COMMANDER_
                            string err = string.Empty;
                            List<string> lista = new List<string>();
                            if (rad.Commander(ServiceCommands.RdSessions, "", ref err, lista) == true)
                            {
                                foreach (string str in lista)
                                {
                                    string[] sessiondata = str.Split(new string[] { "##" }, StringSplitOptions.None);
                                    if (sessiondata.Length == 4)
                                    {
                                        sessions.Add(new radioSessionData() { 
                                            frec = sessiondata[0], 
                                            uri = sessiondata[1], 
                                            tipo = sessiondata[2], 
                                            std = (Convert.ToBoolean(sessiondata[3])) ? 1 : 0,
                                            rtpsend = 0,    // TODO..
                                            rtprecv = 0     // TODO.
                                        }); 
                                    }
                                }
                            }
#else
                            List<object> rd_data = new List<object>();
                            if (rad.DataGet(ServiceCommands.RdSessions, ref rd_data) == true)
                            {
                                foreach (object rd in rd_data)
                                {
                                    sessions.Add((GlobalTypes.radioSessionData)rd);
                                }
                            }
#endif
                        }
                        return sessions;
                    }

                case CmdSupervision.cmdRdMNEquip:
                    {
                        IService rad = RadioService;
                        List<GlobalTypes.equipoMNData> equ_list = new List<GlobalTypes.equipoMNData>();
                        if (rad != null && rad.Master == true)
                        {
#if _COMMANDER_
                            string err = string.Empty;
                            List<string> lista = new List<string>();
                            if (rad.Commander(ServiceCommands.RdMNGearListGet, "", ref err, lista) == true)
                            {
                                foreach (string str in lista)
                                {
                                    string[] eq_data = str.Split(new string[] { "##" }, StringSplitOptions.None);
                                    if (eq_data.Length == 9)
                                    {
                                        equ_list.Add(new equipoMNData()
                                        {
                                             equ=eq_data[0],
                                             grp=int.Parse(eq_data[1]),
                                             mod = int.Parse(eq_data[2]),
                                             tip = int.Parse(eq_data[3]),
                                             std = int.Parse(eq_data[4]),
                                             frec = eq_data[5],
                                             prio = int.Parse(eq_data[6]),
                                             sip = int.Parse(eq_data[7]),
                                             ip = eq_data[8]
                                        });
                                    }
                                }
                            }
#else
                            List<object> rd_data = new List<object>();
                            if (rad.DataGet(ServiceCommands.RdMNGearListGet, ref rd_data) == true)
                            {
                                foreach (object rd in rd_data)
                                {
                                    equ_list.Add((GlobalTypes.equipoMNData)rd);
                                }
                            }
#endif
                        }
                        return equ_list;
                    }

                case CmdSupervision.cmdRdMNReset:
                    {
                        IService rad = RadioService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (rad != null && rad.Master == true)
                        {
                            string err = string.Empty;
                            rad.Commander(ServiceCommands.RdMNReset, null, ref err);
                            resultado.res = err;
                        }

                        return resultado;
                    }

                case CmdSupervision.cmdRdMNEnable:
                    {
                        IService rad = RadioService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (rad != null && rad.Master == true)
                        {
                            string err = string.Empty;
                            rad.Commander(ServiceCommands.RdMNGearToogle, ((GlobalTypes.equipoMNData)data).equ, ref err);
                            resultado.res = err;
                        }
                        return resultado;
                    }

                case CmdSupervision.cmdRdMNAsigna:
                    {
                        IService rad = RadioService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };
                        if (rad != null && rad.Master == true)
                        {
                            string par = ((equipoMNAsigna)data).equ;
                            string err = ((equipoMNAsigna)data).frec;
                            if (((equipoMNAsigna)data).cmd == 1)
                                rad.Commander(ServiceCommands.RdMNGearAssign, par, ref err);
                            else
                                rad.Commander(ServiceCommands.RdMNGearUnassing, par, ref err);
                            resultado.res = err;
                        }
                        return resultado;
                    }

                case CmdSupervision.cmdRdMNConfigureTick:
                    {
                        IService rad = RadioService;
                        nbxResData resultado = new nbxResData() { res = "OnWebServerCommand: No se ha podido ejecutar la operacion. El servicio no existe o no es Maestro." };

                        string err = string.Empty;
                        rad.Commander(ServiceCommands.RdMNValidationTick, ((MNConfiguraTick)data).miliseconds, ref err);
                        resultado.res = err;

                        return resultado;
                    }

                case CmdSupervision.cmdTlfInfoGet:
                    {
                        IService tifx = TifxService;
                        if (tifx != null && tifx.Master == true)
                        {
                            List<object> psdata = new List<object>();
                            if (tifx.DataGet(ServiceCommands.TifxDataGet, ref psdata) == true)
                            {
                                return psdata[0];
                            }
                            throw new Exception("OnWebServerCommand: Error en cmdTlfInfoGet");
                        }
                        break;

                    }

                case CmdSupervision.cmdHFGet:
                    if (RadioService != null && RadioService.Master == true)
                    {
                        List<object> rd_data = new List<object>();
                        if (RadioService.DataGet(ServiceCommands.RdHFGetEquipos, ref rd_data) == true)
                        {
                            List<GlobalTypes.txHF> txs = new List<GlobalTypes.txHF>();
                            foreach (object obj in rd_data)
                            {
                                txs.Add((GlobalTypes.txHF)obj);
                            }
                            return txs;
                        }
                        else
                            throw new Exception("OnWebServerCommand: Error en RdHFGetEquipos");
                    }
                    return null;

                case CmdSupervision.cmdHFLib:
                    if (RadioService != null && RadioService.Master == true)
                    {
                        string err = string.Empty;
                        if (RadioService.Commander(ServiceCommands.RdHFLiberaEquipo, (string)data, ref err) == true)
                            return new nbxResData() { res = "OnWebServerCommand: Operacion Ejecutada..." };
                        throw new Exception("OnWebServerCommand: Error en cmdHFLib");
                    }
                    break;

                case CmdSupervision.cmdPresenceInfoGet:
                    IService PS = PresenceService;
                    if (PS != null && PS.Master == true)
                    {
                        List<object> psdata = new List<object>();
                        if (PS.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
                        {
                            return psdata[0];
                        }
                        throw new Exception("OnWebServerCommand: Error en cmdPresenceInfoGet");
                    }
                    break;

                case CmdSupervision.cmdRdServiceDebugInfo:
                    IService RS = RadioService;
                    if (RS != null && RS.Master == true)
                    {
                        List<object> psdata = new List<object>() { data };
                        if (RS.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
                        {
                            return JsonConvert.SerializeObject(psdata[0]);
                        }
                        throw new Exception("OnWebServerCommand: Error en cmdRdServiceDebugInfo");
                    }
                    break;

                /** 20200224 Gestion 1+1 */
                case CmdSupervision.cmdRdUnoMasUnoData:
                    {
                        IService rad = RadioService;
                        if (rad != null && rad.Master == true)
                        {
                            var rddata = new List<object>();
                            if (rad.DataGet(ServiceCommands.RdUnoMasUnoData, ref rddata) == true)
                            {
                                return rddata;
                            }
                        }
                        return new List<object>();
                    }
                case CmdSupervision.cmdRdUnoMasUnoSelect:
                    if (RadioService != null && RadioService.Master == true)
                    {
                        string err = string.Empty;
                        if (RadioService.Commander(ServiceCommands.RdUnoMasUnoActivate, (string)data, ref err) == true)
                            return new nbxResData() { res = "OnWebServerCommand: Operacion Ejecutada..." };
                        throw new Exception("OnWebServerCommand: Error en cmdRdUnoMasUnoSelect");
                    }
                    break;

            }
            return new nbxResData() { res = "OnWebServerCommand: Operacion no Implementada." };
        }
#endif
        /// <summary>
        /// Gestor de presencia pasivo.
        /// </summary>
        /// <returns></returns>
        protected bool IsMaster
        {
            get
            {
                return true;
            }
        }
        protected void GestorPresencia()
        {
            try
            {
                UdpClient _listener = new UdpClient(new IPEndPoint(IPAddress.Any, U5ki.NodeBox.Properties.Settings.Default.PuertoPresencia));
                // _listener.Client.ReceiveTimeout = 1000;
                IPEndPoint from = new IPEndPoint(IPAddress.Any, U5ki.NodeBox.Properties.Settings.Default.PuertoPresencia);
                while (true)
                {
                    try
                    {
                        _listener.Receive(ref from);
                        Byte[] msg = { (Byte)(IsMaster == true ? 1 : 0), 0 };
                        _listener.Send(msg, 2, from);
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception)
                    {
                    }

                }
            }
            catch (Exception)
            {
            }

        }

#if _NETWORK_SUP_
        /** Presencia RED... */
        bool _isNetworkOnline = false;
        protected void SetNetworkSupervision()
        {
#if _NETWORK_SUP_POLLING_
            NetworkStatusPolling();
#else
            NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(NetworkChange_NetworkAvailabilityChanged);
            _isNetworkOnline = NetworkInterface.GetIsNetworkAvailable();
            _Logger.Warn("NetworkSupervision. _isNetworkOnline={0}", _isNetworkOnline);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _isNetworkOnline = e.IsAvailable;
            _Logger.InfoManager(String.Format(CTranslate.translateResource("Supervisión de red. _isNetworkOnline={0}"), _isNetworkOnline), true);
        }

        /// <summary>
        /// 
        /// </summary>
        int _isNetworkOn = 0;
        bool _stdNetwork = false;
#if _SIM_ERRORES_RED_
        Random rnd = new Random();
        int _control_errores = 1;
#endif
        void NetworkStatusPolling(bool inicial = true)
        {
#if _SIM_ERRORES_RED_
            if (_control_errores>0 && --_control_errores <= 0)
            {
                _control_errores = rnd.Next(4,10);
                _isNetworkOnline = !_isNetworkOnline;
                _Logger.Warn("Simula NetworkOnline={0} para {1} seg.", _isNetworkOnline, _control_errores);
            }
#else
            bool _last = _isNetworkOnline;

            NetworkInterface nic = NetworkInterfaceGet(Properties.Settings.Default.IpPrincipal);
            if (nic != null)
            {
                _stdNetwork = nic.OperationalStatus == OperationalStatus.Up;
            }
            else
            {
                _stdNetwork = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }

            if (_last != _stdNetwork)
            {
                if (_stdNetwork == true)
                {
                    //NetworkOnDelay Este parámetro es configurable y siempre debe ser mayor que 
                    //el parametro LookupTimeout de spread.conf, para asegurar que El Mcast entra siempre antes que el Nbx
                    if (++_isNetworkOn >= U5ki.NodeBox.Properties.Settings.Default.NetworkOnDelay)
                    {
                        _isNetworkOnline = true;
                        _Logger.InfoManager(String.Format(CTranslate.translateResource("Supervisión de red. _isNetworkOnline={0}"), _isNetworkOnline), true);
                        _isNetworkOn = 0;
                    }
                }
                else
                {
                    _isNetworkOnline = false;
                    _Logger.InfoManager(String.Format(CTranslate.translateResource("Supervisión de red. _isNetworkOnline={0}"), _isNetworkOnline), true);
                    _isNetworkOn = 0;
                }
            }
            else
            {
                _isNetworkOn = 0;
            }
#endif
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srv"></param>
        void StartService(IService srv)
        {
            try
            {
                _Logger.InfoManager(String.Format("Arrancando Servicio {0}", srv.Name));
                srv.Start();
            }
            catch (Exception x)
            {
                _Logger.ExceptionManager(String.Format("NodeBOX: Excepcion Arrancando Servicio {0}...", srv.Name), x);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srv"></param>
        void StopService(IService srv)
        {
            try
            {
                _Logger.InfoManager(String.Format("Deteniendo Servicio {0}", srv.Name));
                srv.Stop();
            }
            catch (Exception x)
            {
                _Logger.ExceptionManager(String.Format("NodeBOX: Excepcion Parando Servicio {0}...", srv.Name), x);
            }
        }
#endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        protected IService ServiceByName(String serviceName)
        {
            return _Services.Keys.Contains(serviceName) == true ? _Services[serviceName] : null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected ServiceStatus ServiceStatusByValue(IService service)
        {
            return service == null ? ServiceStatus.Disabled : service.Status;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected ServiceMasterStates ServiceMasterStateByValue(IService service)
        {
            return service == null || service.Status != ServiceStatus.Running ? ServiceMasterStates.Error :
                service.Master == true ? ServiceMasterStates.Master : ServiceMasterStates.Slave;
        }

        /// <summary>
        /// 
        /// </summary>
        protected IService RadioService
        {
            get
            {
                return ServiceByName("Cd40RdService");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected IService CfgService
        {
            get
            {
                return ServiceByName("Cd40ConfigService");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected IService PhoneService
        {
            get
            {
                return ServiceByName("PhoneService");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected IService TifxService
        {
            get
            {
                return ServiceByName("Cd40TifxService");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected IService PresenceService
        {
            get
            {
                return ServiceByName("PresenceServer");
            }
        }

        protected ServiceMasterStates GlobalMasterState
        {
            get
            {
                List<ServiceMasterStates> RadioServerStates = new List<ServiceMasterStates>()
                {
                    ServiceMasterStateByValue(RadioService),
                    ServiceMasterStateByValue(CfgService)
                };
                List<ServiceMasterStates> PhoneServerStates = new List<ServiceMasterStates>()
                {
                    ServiceMasterStateByValue(PhoneService),
                    ServiceMasterStateByValue(TifxService),
                    ServiceMasterStateByValue(PresenceService)
                };

                int RadioServerServicesInMaster = RadioServerStates.Where(s => s == ServiceMasterStates.Master).ToList().Count;
                int RadioServerServicesInSlave = RadioServerStates.Where(s => s == ServiceMasterStates.Slave).ToList().Count;
                int PhoneServerServicesInMaster = PhoneServerStates.Where(s => s == ServiceMasterStates.Master).ToList().Count;
                int PhoneServerServicesInSlave = PhoneServerStates.Where(s => s == ServiceMasterStates.Slave).ToList().Count;

                switch (ServerType)
                {
                    case ServicesServerTypes.Radio:
                        return RadioServerServicesInMaster == RadioServerStates.Count ? ServiceMasterStates.Master :
                            RadioServerServicesInSlave == RadioServerStates.Count ? ServiceMasterStates.Slave : ServiceMasterStates.Error;

                    case ServicesServerTypes.Phone:
                        return PhoneServerServicesInMaster == PhoneServerStates.Count ? ServiceMasterStates.Master :
                            PhoneServerServicesInSlave == PhoneServerStates.Count ? ServiceMasterStates.Slave : ServiceMasterStates.Error;

                    case ServicesServerTypes.Mixed:
                        return (RadioServerServicesInMaster + PhoneServerServicesInMaster) == (RadioServerStates.Count + PhoneServerStates.Count) ? ServiceMasterStates.Master :
                            (RadioServerServicesInSlave + PhoneServerServicesInSlave) == (RadioServerStates.Count + PhoneServerStates.Count) ? ServiceMasterStates.Slave : ServiceMasterStates.Error;

                    default:
                        return ServiceMasterStates.Error;
                }
            }
        }

        /// <summary>
        /// Obtiene el Interfaz de Red a partir de la IP.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        protected NetworkInterface NetworkInterfaceGet(string ip)
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties ip_properties = adapter.GetIPProperties();

                //if (!adapter.GetIPProperties().MulticastAddresses.Any())
                //    continue; // most of VPN adapters will be skipped

                if (!adapter.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection

                if (OperationalStatus.Up != adapter.OperationalStatus)
                    continue; // this adapter is off or not connected

                foreach (UnicastIPAddressInformation inf in ip_properties.UnicastAddresses)
                {
                    if (inf.Address.Equals(IPAddress.Parse(ip)) == true)
                    {
                        return adapter;
                    }
                }
            }
            return null;
        }

        /** 20180309. Para poder supervisar el estado del SIP AGENT*/
        protected bool SipAgentStarted = false;
        protected void SipAgentStart()
        {
            /** 20180208. Inicializa el SipAgent para que pueda se utilizado por diferentes servicios */
            try
            {
                _Logger.InfoManager("Iniciando SipAgent.");
                uint sipPort = U5ki.NodeBox.Properties.Settings.Default.SipPort;
                if (ServerType == ServicesServerTypes.Phone)
                {
                    sipPort = U5ki.NodeBox.Properties.Settings.Default.SipPortPhone;
                }
                SipAgent.Init(
                    U5ki.NodeBox.Properties.Settings.Default.SipUser,
                    U5ki.NodeBox.Properties.Settings.Default.IpPrincipal,
                    sipPort, 128);
                if (ServerType != ServicesServerTypes.Phone)
                {
                    SipAgent.ReceiveFromRemote(
                        U5ki.NodeBox.Properties.Settings.Default.IpPrincipal,
                        U5ki.NodeBox.Properties.Settings.Default.ListenIp,
                        U5ki.NodeBox.Properties.Settings.Default.ListenPort);
                }
                SipAgent.Start();
                SipAgentStarted = true;
            }
            catch (Exception x)
            {
                LogManager.GetCurrentClassLogger().Error("SipAgentStart Exception", x);
            }
        }
        protected void SipAgentStop()
        {
            try
            {
                /** 20180208. Inicializa el SipAgent para que pueda se utilizado por diferentes servicios */
                _Logger.InfoManager("Deteniendo SipAgent.");
                SipAgent.End();
            }
            catch (Exception x)
            {
                LogManager.GetCurrentClassLogger().Error("SipAgentStop Exception", x);
            }
            finally
            {
                SipAgentStarted = false;
            }
        }

        /** 2020024. Nuevo Servidor WEB */
        WebServer.U5kNbxWebApp WebAppServer = new WebServer.U5kNbxWebApp();
        /** 20180309. Para poder supervisar el estado del WEB SERVER */
        protected bool WebServerStarted = false;
        protected void WebServerStart()
        {
            try
            {
                if (U5ki.NodeBox.Properties.Settings.Default.ControlRemoto == true)
                {
                    _Logger.InfoManager("Iniciando Web Server.");
#if OLD_WEB_SERVER
                    _webServer.WebSrvCommand += OnWebServerCommand;
                    _webServer.Start(U5ki.NodeBox.Properties.Settings.Default.PuertoControlRemoto);
#else
                    WebAppServer.Start((name)=>ServiceByName(name));
#endif

                    WebServerStarted = true;
                }
                else
                {
                    WebServerStarted = true;
                }
            }
            catch (Exception x)
            {
                LogManager.GetCurrentClassLogger().Error("WebServerStart Exception", x);
            }
        }
        protected void WebServerStop()
        {
            try
            {
                if (U5ki.NodeBox.Properties.Settings.Default.ControlRemoto == true)
                {
                    /** */
                    _Logger.InfoManager("Deteniendo WebServer.");
#if OLD_WEB_SERVER
                    _webServer.WebSrvCommand -= OnWebServerCommand;
                    _webServer.Dispose();
#else
                    WebAppServer.Stop();
#endif
                }
            }
            catch (Exception x)
            {
                LogManager.GetCurrentClassLogger().Error("WebServerStop Exception", x);
            }
            finally
            {
                WebServerStarted = false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
    }
}

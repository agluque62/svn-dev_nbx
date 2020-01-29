#define _CORESIP_1_
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using U5ki.Infrastructure;
using ProtoBuf;
using Utilities;
using System.Net;

#if _CORESIP_2_
using CoreSipNet;
#endif

namespace u5ki.PhoneService
{
    public class PhoneService : BaseCode, IService
    {
        #region IService Interfaz

        public string Name { get; set; } 

        public ServiceStatus Status { get; set; }

        public bool Master { get; set; }

        public bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp = null)
        {
            throw new NotImplementedException();
        }

        public bool DataGet(ServiceCommands cmd, ref List<object> rsp)
        {
            if (smpAccesMain.Acquire())
            {
                List<object> lista = new List<object>();
                lista.Add(ServicesHelpers.SerializeObject(new
                {
                    id = Name,                                   // TODO. Construir los datos que se quieran obtener del servicio....
                    status = Status,
                    mode = Master ? "Master" : "Slave",
                }));
                rsp = new List<object>(lista);
                smpAccesMain.Release();
                return true;
            }
            return false;
        }

        public void Start()
        {
            if (smpAccesMain.Acquire())
            {

                try
                {
                    LogInfo<PhoneService>("Iniciando Servicio...");

                    ResourcesSetup();

                    LogInfo<PhoneService>("Servicio Iniciado.",
                        U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PhoneService",
                        Translate.CTranslate.translateResource("Servicio iniciado."));          // TODO... Translate....
                }
                catch (Exception x)
                {
                    ResourcesRelease();
                    LogException<PhoneService>("Excepcion arrancando servicio", x, false);
                }
                finally
                {
                    smpAccesMain.Release();
                }
            }
        }

        public void Stop()
        {
            if (smpAccesMain.Acquire())
            {
                try
                {
                    LogInfo<PhoneService>("Iniciando parada servicio");

                    ResourcesRelease();
                    if (_Registry != null)
                    {
                        _Registry.Dispose();
                        _Registry = null;
                    }
                }
                catch (Exception x)
                {
                    LogException<PhoneService>("Excepcion deteniendo servicio", x, false);
                }
                finally
                {
                    Status = ServiceStatus.Stopped;
                    LogInfo<PhoneService>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                        "PhoneService", Translate.CTranslate.translateResource("Servicio detenido."));               // TODO. Translate.
                    smpAccesMain.Release();
                }
            }
        }

        #endregion IService Interfaz

        #region Contructores 
        public PhoneService()
        {
            Name = "PhoneService";
            Status = ServiceStatus.Stopped;
            Master = false;
            LastVersion = String.Empty;
            SipAgentRunning = false;
            TraceControl = new ServicesHelpers.TraceInOut<PhoneService>();
        }
        #endregion
        #region miembros Clase
        private List <MDCall> _MDCalls = new List<MDCall>();
        public List<MDCall> MDCalls
        {
            get { return _MDCalls; }
        }
        Config.LocalSettings settings = null;
        private string _IdProxy = null;
        Cd40Cfg _Cfg = null;
        #endregion
        #region Gestion Global del Servicio.

        /// <summary>
        /// Se Ejecuta en el START...
        /// </summary>
        private void ResourcesSetup()
        {
            Int64 tcontrol = TraceControl.TraceIn();

            Master = false;
            LastVersion = String.Empty;
            Status = ServiceStatus.Running;

            InitRegistry();

            /** */
            SipAgentStart();

            /* Subscribe to CFG mesages */
            _Registry.SubscribeToTopic<Cd40Cfg>(Identifiers.CfgTopic);
            _Registry.Join(Identifiers.PhoneMasterTopic, Identifiers.PhoneTopic, Identifiers.CfgTopic);


            /** Otras Inicializaciones */
            TraceControl.TraceOut(tcontrol);
        }

        private void InitRegistry()
        {
            /** Inicializacion de las comunicaciones por MULTICAST */
            /* solo para recibir mensajes de otro PhoneService para gestionar master/slave */
            _Registry = new Registry(Identifiers.PhoneMasterTopic);

            _Registry.ChannelError += OnChannelError;
            _Registry.MasterStatusChanged += OnMasterStatusChanged;
            _Registry.ResourceChanged += OnConfigChanged;

            _Registry.SubscribeToMasterTopic(Identifiers.PhoneMasterTopic);
            _Registry.SubscribeToTopic<SrvMaster>(Identifiers.PhoneMasterTopic);
            //_Registry.Join(Identifiers.PhoneMasterTopic, Identifiers.PhoneTopic);


        }

        /// <summary>
        /// Se ejecuta en el STOP...
        /// </summary>
        private void ResourcesRelease()
        {
            Int64 tcontrol = TraceControl.TraceIn();

            if (Master == true)
            {
                MDCallsRelease();
                SipAgentStop();
            }

            TraceControl.TraceOut(tcontrol);
        }

        /// <summary>
        /// Se ejectua al pasar a MASTER.
        /// </summary>
        private void ServiceActivate()
        {
            Int64 tcontrol = TraceControl.TraceIn();

            if (SipAgentRunning == false)
            {
                SipAgentRunning = true;
                SipAgentSetup();
            }
            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// Se ejecuta al pasar a SLAVE
        /// </summary>
        private void ServiceDeactivate()
        {
            Int64 tcontrol = TraceControl.TraceIn();
            ResourcesRelease();
            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// Se ejecuta al recibir una nueva configuracion...
        /// Es publica para poder llamarse desde un test unitario
        /// </summary>
        /// <param name="cfg"></param>
        public void ServiceSetupConfig(Cd40Cfg cfg)
        {
            string rsIp;
            Int64 tcontrol = TraceControl.TraceIn();

            _Cfg = cfg;
            //Variable para gestionar los posibles cambios en configuración
            List<MDCall> MDCallsNewCfg = new List<MDCall>();

            //Get Proxy IP
            IPEndPoint ipProxy = null;
            foreach (DireccionamientoIP obj in cfg.ConfiguracionGeneral.PlanDireccionamientoIP)
            {
                if ((obj.TipoHost == Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA) && obj.EsCentralIP)
                {
                    if (obj.Interno)
                    {
                        ipProxy = SipUtilities.SipEndPoint.Parse(obj.IpRed1);
                        _IdProxy = obj.IdHost;
                        break;
                    }
                }
            }
            if (ipProxy == null)
            {
                LogError<PhoneService>("Phone ServiceSetupConfig Error: Proxy not configured");
                return;
            }
            
            //Create MDCall and its members
            foreach (ConfiguracionUsuario userCfg in cfg.ConfiguracionUsuarios)
            {
                foreach (CfgEnlaceInterno link in MDLinksPropios (userCfg))
                {
                    //Check Name of multidestination groups is unique
                    if (MDCallsNewCfg.Exists(x => x.Name == link.Literal) == false)
                    {
                        MDCall newMDCall = new MDCall(link.Literal, Properties.Settings.Default.IpPrincipal, Properties.Settings.Default.SipPortPhone, ipProxy);
                        foreach (CfgRecursoEnlaceInterno rec in link.ListaRecursos)
                        {
                            switch (rec.Prefijo)
                            {
                                case Cd40Cfg.INT_DST:
                                    if (string.IsNullOrEmpty(rec.NombreRecurso) == false)
                                        rec.NumeroAbonado = cfg.ConfiguracionGeneral.GetUserAlias(rec.NombreRecurso, Cd40Cfg.ATS_DST);
                                    newMDCall.AddMember(rec.NombreRecurso, rec.NumeroAbonado, ipProxy);
                                    break;
                                case Cd40Cfg.RTB_DST:
                                    List <PlanRecursos> listRec = cfg.ConfiguracionGeneral.GetNetResources(rec.Prefijo, rec.NumeroAbonado);            
                                    foreach (PlanRecursos recurso in listRec)
                                    {
                                        string idEquipo;
                                        rsIp = cfg.ConfiguracionGeneral.GetGwRsIp(recurso.IdRecurso, out idEquipo);
                                        if (rsIp != null)
                                            newMDCall.AddMember(rec.NombreMostrar, rec.NumeroAbonado, SipUtilities.SipEndPoint.Parse(rsIp), recurso.IdRecurso);
                                    }
                                    break;
                                case Cd40Cfg.PP_DST:
                                    string gw;
                                    rsIp = cfg.ConfiguracionGeneral.GetGwRsIp(rec.NombreRecurso, out gw);
                                    if (rsIp != null)
                                        newMDCall.AddMember(rec.NombreMostrar, rec.NombreRecurso, SipUtilities.SipEndPoint.Parse(rsIp));
                                    break;
                                case Cd40Cfg.ATS_DST:
                                    newMDCall.AddMember(rec.NombreRecurso, rec.NumeroAbonado, ipProxy);
                                    break;
                                default:
                                    break;
                            }
                       }
                        MDCallsNewCfg.Add(newMDCall);
                    }
                }
            }
            synchronizeMDConfig(MDCallsNewCfg);
            TraceControl.TraceOut(tcontrol);

        }
        /// <summary>
        /// Se ejecuta como rutina de limpieza de la configuracion antes de cargar una nueva...
        /// Es publica para poder llamarse desde un test unitario
        /// </summary>
        public void ServiceReleaseConfig()
        {
            Int64 tcontrol = TraceControl.TraceIn();

            // TODO...
            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        private void OnChannelError(object sender, string error)
        {
            Int64 tcontrol = TraceControl.TraceIn();

            ProcessGlobalEvent(() =>
            {

                TraceControl.TraceOut(tcontrol);
                if (_Registry != null)
                {
                    _Registry.Dispose();
                    _Registry = null;
                } 
                Status = ServiceStatus.Stopped;
                LastVersion = String.Empty;
                LogError<PhoneService>("OnChannelError: " + error, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
            }, (x) =>
            {
                LogException<PhoneService>("OnCallState  Exception", x, false);
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="master"></param>
        private void OnMasterStatusChanged(object sender, bool master)
        {
            Int64 tcontrol = TraceControl.TraceIn();
            ProcessGlobalEvent(() =>
            {
               if (master && !Master)
                {
                    ServiceActivate();
                   //A veces no llega la configuración....asi que uso lo último recibido
                   if (_Cfg != null)
                    ServiceSetupConfig(_Cfg);
                }
                else if (!master && Master)
                {
                    ServiceDeactivate();
                }
                Master = master;
                LastVersion = string.Empty;

                LogInfo<PhoneService>(Master ? "MASTER" : "SLAVE",
                    U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PhoneService",
                    Master ? "MASTER" : "SLAVE");

                TraceControl.TraceOut(tcontrol);
            }, (x) =>
            {
                LogException<PhoneService>("OnMasterStatusChanged to " + master.ToString() + " Exception", x, false);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConfigChanged(object sender, RsChangeInfo e)
        {
            Int64 tcontrol = TraceControl.TraceIn();
            ProcessGlobalEvent(() =>
            {
                if (Master && e.Content != null && e.Type == Identifiers.TypeId(typeof(Cd40Cfg)))
                {
                    MemoryStream ms= new MemoryStream(Tools.Decompress(e.Content));
                    Cd40Cfg cfg = Serializer.Deserialize<Cd40Cfg>(ms);

                    if (LastVersion == cfg.Version)
                        return;

                    LastVersion = cfg.Version;
                    LogInfo<PhoneService>("Procesando Configuracion: " + LastVersion);

                    ServiceReleaseConfig();
                    ServiceSetupConfig(cfg);
                }

                TraceControl.TraceOut(tcontrol);
            }, (x) =>
            {
                LogException<PhoneService>("OnResourceChanged Exception", x, false);
            });
        }

        /// <summary>
        /// Para gestionar los eventos globales genera una thread y obtiene el acceso global a los datos...
        /// </summary>
        /// <param name="mainRoutine"></param>
        /// <param name="errorRoutine"></param>
        private void ProcessGlobalEvent(Action mainRoutine, Action<Exception> errorRoutine)
        {
            Task.Factory.StartNew(() =>
            {
                if (smpAccesMain.Acquire())
                {
                    try
                    {
                        mainRoutine();
                    }
                    catch (Exception x)
                    {
                        errorRoutine(x);
                    }
                    finally
                    {
                        smpAccesMain.Release();
                    }
                }
                else
                {
                    errorRoutine(new ApplicationException("Timeout en Semaforo de control de acceso global."));
                }
            });
        }

        private IEnumerable<CfgEnlaceInterno> MDLinksPropios(ConfiguracionUsuario cfg)
        {
                if (cfg != null)
                {
                    foreach (CfgEnlaceInterno link in cfg.TlfLinks)
                    {
                        if (link.TipoEnlaceInterno == "MD" && link.Dominio == "PROPIO")
                        {
                            yield return link;
                        }
                    }
                }
        }

        private void synchronizeMDConfig(List<MDCall> MDCallsNewCfg)
        {
            MDCall found = null;
            List<MDCall> MDCallsCopy = new List<MDCall> (_MDCalls);
            foreach (MDCall MDCfg in MDCallsCopy)
            {
                found = MDCallsNewCfg.Find(x => MDCfg.Equals(x));
                if (found == null)
                {
                    MDCfg.Dispose();
                    _MDCalls.Remove(MDCfg);
                }
                else
                    MDCallsNewCfg.Remove(found);
            }

            foreach (MDCall newMDCall in MDCallsNewCfg)
            {
                _MDCalls.Add(newMDCall);
                newMDCall.InitAgent();
            }
        }

        private void MDCallsRelease()
        {
            foreach (MDCall MDCall in _MDCalls)
            {
                MDCall.Dispose();
            }
            _MDCalls.Clear();
        }
        #endregion

        #region Control de Agente SIP y eventos asociados
        /// <summary>
        /// 
        /// </summary>
        void SipAgentStart()
        {
            Int64 tcontrol = TraceControl.TraceIn();
            LogInfo<PhoneService>(Master ? "MASTER" : "SLAVE",
    U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PhoneService",
    Master ? "MASTER" : "SLAVE");
            if (SipAgentRunning==false)
            {
                using (settings = new Config.LocalSettings())
                {
                    settings.Init();
#if _CORESIP_2_
                    SipAgentNetSettings sipSettings = new SipAgentNetSettings()
                    {
                        Default = new SipAgentNetSettings.DefaultSettings()
                        {
                            DefaultCodec = settings.Sip.DefaultCodec,
                            DefaultDelayBufPframes = settings.Sip.DefaultDelayBufPframes,
                            DefaultJBufPframes = settings.Sip.DefaultJBufPframes,
                            SndSamplingRate = settings.Sip.SndSamplingRate,
                            TxLevel = settings.Sip.TxLevel,
                            RxLevel = settings.Sip.RxLevel,
                            SipLogLevel = settings.Sip.SipLogLevel,
                            TsxTout = settings.Sip.TsxTout,
                            InvProceedingDiaTout = settings.Sip.InvProceedingDiaTout,
                            InvProceedingIaTout = settings.Sip.InvProceedingIaTout,
                            InvProceedingMonitoringTout = settings.Sip.InvProceedingMonitoringTout,
                            InvProceedingRdTout = settings.Sip.InvProceedingRdTout,
                            KAMultiplier = settings.Sip.KAMultiplier,
                            KAPeriod = settings.Sip.KAPeriod
                        }
                    };
                    SipAgentNet.Init(sipSettings, settings.Sip.AgentName, settings.Ip, settings.Sip.UdpPort, 1000);
                    SipAgentNet.Start();
#endif
                    SipAgentRunning = true;
                    SipAgentSetup();
                }
            }

            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// 
        /// </summary>
        public void SipAgentSetup()
        {
            Int64 tcontrol = TraceControl.TraceIn();
#if _CORESIP_2_
            SipAgentNet.CallIncoming += OnCallIncoming;
            SipAgentNet.CallState += OnCallState;
#else
            SipAgent.CallIncoming += OnCallIncoming;
            SipAgent.CallState += OnCallState;
            SipAgent.IncomingSubscribeConfAcc += OnIncomingSubscribeConf;

#endif
            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// 
        /// </summary>
        void SipAgentRelease()
        {
            Int64 tcontrol = TraceControl.TraceIn();

#if _CORESIP_2_
            SipAgentNet.CallState -= OnCallState;
            SipAgentNet.CallIncoming -= OnCallIncoming;
#else
            SipAgent.CallState -= OnCallState;
            SipAgent.CallIncoming -= OnCallIncoming;
            SipAgent.IncomingSubscribeConfAcc -= OnIncomingSubscribeConf;
#endif

            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// 
        /// </summary>
        void SipAgentStop()
        {
            Int64 tcontrol = TraceControl.TraceIn();

            if (SipAgentRunning == true)
            {
                SipAgentRunning = false;
                SipAgentRelease();
                //SipAgentNet.End();
            }

            TraceControl.TraceOut(tcontrol);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="info"></param>
        /// <param name="stateInfo"></param>
#if _CORESIP_2_
        void OnCallState(int call, CoreSipNet.CORESIP_CallInfo info, CoreSipNet.CORESIP_CallStateInfo stateInfo)
#else
        void OnCallState(int call, CORESIP_CallInfo info, CORESIP_CallStateInfo stateInfo)
#endif
        {
            Int64 tcontrol = TraceControl.TraceIn();

            ProcessGlobalEvent(() =>
            {
                foreach (MDCall MDcall in _MDCalls)
                    MDcall.OnCallState(call, info, stateInfo);
                TraceControl.TraceOut(tcontrol);
            }, (x) =>
            {
                LogException<PhoneService>("OnCallState  Exception", x, false);
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="call2replace"></param>
        /// <param name="info"></param>
        /// <param name="inInfo"></param>
#if _CORESIP_2_
        void OnCallIncoming(int call, int call2replace, CoreSipNet.CORESIP_CallInfo info, CoreSipNet.CORESIP_CallInInfo inInfo)
#else
        public void OnCallIncoming(int call, int call2replace, CORESIP_CallInfo info, CORESIP_CallInInfo inInfo)
#endif
        {
            Int64 tcontrol = TraceControl.TraceIn();

            ProcessGlobalEvent(() =>
            {
               foreach (MDCall MDcall in _MDCalls)
                    MDcall.OnCallIncoming(call, call2replace, info, inInfo);
                TraceControl.TraceOut(tcontrol);
            }, (x) =>
            {
                LogException<PhoneService>("OnCallIncoming  Exception", x, false);
            });
        }
        public void OnIncomingSubscribeConf(string id, string info, uint lenInfo)
        {
            Int64 tcontrol = TraceControl.TraceIn();

            ProcessGlobalEvent(() =>
            {
                foreach (MDCall MDcall in _MDCalls)                    
                    MDcall.OnIncomingSubscribeConf(id, info, lenInfo);
                TraceControl.TraceOut(tcontrol);
            }, (x) =>
            {
                LogException<PhoneService>("OnCallIncoming  Exception", x, false);
            });

        }
        #endregion

        #region Private Attributes

        /// <summary>
        /// Semaforo para el acceso global a los recursos de los servicios....
        /// </summary>
        private ServicesHelpers.ManagedSemaphore smpAccesMain = new ServicesHelpers.ManagedSemaphore(1, 1, "PhoneServiceAccess", 5000);
        /// <summary>
        /// 
        /// </summary>
        private Registry _Registry=null;
        /// <summary>
        /// 
        /// </summary>
        private String LastVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private bool SipAgentRunning { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private ServicesHelpers.TraceInOut<PhoneService> TraceControl { get; set; }

        #endregion Private Attributes

    }
}

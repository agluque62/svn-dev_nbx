#define _HF_RTPOLL_
#define _HF_GLOBAL_STATUS_
#define _MN_ENQUEUE_NODES_
#define _MN_SET_RESET_RESOURCES_V0

#define _MN_NEW_START_STOP

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;

using U5ki.Infrastructure;
using U5ki.RdService.Gears;
using U5ki.RdService.NM;
using U5ki.RdService.Properties;
using Utilities;
using U5ki.Infrastructure.Code;
using U5ki.RdService.Helpers;
using U5ki.Enums;
using System.Runtime.CompilerServices;
using U5ki.RdService;
using Translate;

namespace U5ki.RdService
{
    /// <summary>
    /// 20200224. AGL
    /// Clase Estática para extensiones de clases...
    /// </summary>
    static public class RdExtensions
    {
        private static readonly ConditionalWeakTable<RdResource, RdResourcePair> _pairs = new ConditionalWeakTable<RdResource, RdResourcePair>();
        public static void SetContainerPair(this RdResource @this, RdResourcePair pair)
        {
            _pairs.Remove(@this);
            _pairs.Add(@this, pair);
        }
        public static RdResourcePair GetContainerPair(this RdResource @this)
        {
            RdResourcePair pair;
            if (_pairs.TryGetValue(@this, out pair)) return pair;
            return default(RdResourcePair);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class RdService : BaseCode, IService
    {
        #region Declarations

        struct AskingThread
        {
            public string from;
            public FrTxChangeAsk ask;
            public int result;
        }
        /// <summary>
        /// 
        /// </summary>
        private bool _Master = false;
        private System.Timers.Timer _Timer = new System.Timers.Timer(Settings.Default.ConectionRetryTimer * 1000);
        private System.Timers.Timer _TimerHfStatus = new System.Timers.Timer(500);
        private string _SipIp = Settings.Default.SipIp;
        Cd40Cfg _Cfg = null;
        private ServiceStatus _Status = ServiceStatus.Stopped;
        /// <summary>
        /// 
        /// </summary>
        private IDictionary<string, RdFrecuency> _frecuencies;
#if DEBUG
        public IDictionary<string, RdFrecuency> Frecuencies
#else
        private IDictionary<string, RdFrecuency> Frecuencies
#endif
        {
            get
            {
                if (null == _frecuencies)
                    _frecuencies = new Dictionary<string, RdFrecuency>();
                return _frecuencies;
            }
        }
        /// <summary>
        /// TODO. Explicar.
        /// </summary>
        private IDictionary<string, RdFrecuency> _frecuenciesReserved;
        private IDictionary<string, RdFrecuency> FrecuenciesReserved
        {
            get
            {
                if (null == _frecuenciesReserved)
                    _frecuenciesReserved = new Dictionary<string, RdFrecuency>();
                return _frecuenciesReserved;
            }
        }
        /// <summary>
        /// Referencias a los puertos pjmedia donde se reciben los audios de transmisión de los PICTS
        /// </summary>
        private Dictionary<string, int> _SndRxPorts = new Dictionary<string, int>();
        private static EventQueue _EventQueue = new EventQueue();
        public static EventQueue evQueueRd
        {
            get { return _EventQueue; }
        }
        /// <summary>
        /// Asociones Usuarios/Frecuencias asignadas.
        /// TODO.
        /// </summary>
        private Dictionary<KeyValuePair<string, string>, RdFrecuency> _UsrFreq = new Dictionary<KeyValuePair<string, string>, RdFrecuency>();

       //JOI 201709 NEWRDRP INI
        private Dictionary<string, RdResource.NewRdResourceParams> _RdRParam = new Dictionary<string, RdResource.NewRdResourceParams>();
        public Dictionary<string, RdResource.NewRdResourceParams> RdRParam
        {
            get
            { return _RdRParam; }
        }
        public RdResource.NewRdResourceParams RdRParamGet(String rdRParamId)
        {
            if (_RdRParam.ContainsKey(rdRParamId))
                return _RdRParam[rdRParamId];
            return null;
        }
        //JOI 201709 NEWRDRP FIN

        /// <summary>
        /// AGL.HF. Declarar y contruir el objeto de getion HF
        /// </summary>
        private RdGestorHF _gestorHF = new RdGestorHF();

        /// <summary>
        /// Acumula eventos del timer para reintentos conexión canales radio
        /// y por cada tres eventos monitoriza el estado de los equipos HF
        /// </summary>
        private static int _contTimerEvents = (int)Math.Round(((float)Settings.Default.HFMonitorTimer / Settings.Default.ConectionRetryTimer), MidpointRounding.AwayFromZero);
        private static int _HFTimerCount = (int)Math.Round(((float)Settings.Default.HFMonitorTimer / Settings.Default.ConectionRetryTimer), MidpointRounding.AwayFromZero);
        /// <summary>
        /// 
        /// </summary>
        private MNManager _mNManager;
        public MNManager MNManager
        {
            get
            {
                if (null == _mNManager)
                    _mNManager = new MNManager(
                        ReserveFrecuency,
                        UnReserveFrecuency,
                        OnNodeAllocate,
                        OnNodeDeallocate);
                return _mNManager;
            }
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        public RdService()
        {
#if DEBUG 
            if (Globals.Test.IsTestRunning)
                return;
#endif

            _Timer.Elapsed += OnTimer;
            _Timer.AutoReset = true;

            _TimerHfStatus.Elapsed += TimerHfStatus_Elapsed;
            _TimerHfStatus.AutoReset = false;
        }

        #region IService Members
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return "Cd40RdService"; }
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
#if DEBUG
            set {_Master = value;}
#endif
        }

        /// <summary>
        /// Metodo que viene del Interfaz de Servicio. Se utiliza para enviarle comandos desde fuera del servicio.
        /// <para>Ejemplo, la web de control de sessiones.</para>
        /// </summary>
        public bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp = null)
        {
            bool retorno = false;
            string error = default;
            try
            {
                switch (cmd)
                {
                    // -------------------------------------------------------------
                    // HF
                    case ServiceCommands.RdHFGetEquipos:
                        return _gestorHF.ListaEquipos(resp);

                    case ServiceCommands.RdHFGetEstadoEquipo:
                        return _gestorHF.EstadoEquipo(par, resp);

                    case ServiceCommands.RdHFLiberaEquipo:
                        if (_Master == false)
                        {
                            error = "Servicio en Modo Slave";
                        }
                        else
                        {
                            SetData(ServiceCommands.RdHFLiberaEquipo, par, (success, internal_error) =>
                            {
                                retorno = success;
                                error = internal_error;
                            });
                        }
                        err = error;
                        return retorno;
                        //return _gestorHF.LiberaEquipo(par);

                    // -------------------------------------------------------------
                    // NMManager
                    case ServiceCommands.RdSessions:
                        return RdSessionsGet(par, ref err, resp);

                    case ServiceCommands.RdMNGearListGet:
                        return NMGearsGet(par, ref err, resp);

                    case ServiceCommands.RdMNReset:
                        return NMGearReset(par, ref err, resp);

                    case ServiceCommands.RdMNGearToogle:
                        return NMGearToogle(par, ref err, resp);

                    case ServiceCommands.RdMNGearAssign:
                        return NMGearAssign(par, ref err, resp);

                    case ServiceCommands.RdMNGearUnassing:
                        return NMGearUnassing(par, ref err, resp);

                    case ServiceCommands.RdMNValidationTick:
                        return MNValidationTick(par, ref err, resp);

                    case ServiceCommands.SrvDbg:
#if DEBUG
                        // MNHistoricosTest();
                        OnMasterStatusChanged(this, par == "M" ? true : false);
#endif
                        return true;

                    /** 20200224. Mando 1+1 */
                    case ServiceCommands.RdUnoMasUnoActivate:
                        if (_Master == false)
                        {
                            error = "Servicio en Modo Slave";
                        }
                        else
                        {
                            SetData(ServiceCommands.RdUnoMasUnoActivate, par, (success, internal_error) =>
                             {
                                 retorno = success;
                                 error = internal_error;
                             });
                        }
                        err = error;
                        return retorno;
                    /** 20160928. AGL. Estado del Gestror para la Pagina WEB del NODEBOX. */
                    case ServiceCommands.RdMNStatus:
                        return MNStatus(par, ref err, resp);

                }
            }
            catch (Exception ex)
            {
                ExceptionManage<RdService>("Commander", ex, "OnCommander Exception: " + ex.Message, false);
                LogError<RdService>(ex.Message, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR, "RdService", ex.Message);
            }
            return false;
        }

        ///** 20170217. AGL. Nueva interfaz de comandos. Orientada a estructuras definidas en 'Infraestructure' */
        /// <summary>
        /// 20180524. Las llamadas a STATUS de Frecuencia/Recursos deberian estar sincronizadas a través de EventQueue...
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="rsp"></param>
        /// <returns></returns>
        public bool DataGet(ServiceCommands cmd, ref List<Object> rsp)
        {
            bool retorno = false;
            List<Object> local_rsp = rsp;
            ManualResetEvent sync = new ManualResetEvent(false);

            _EventQueue.Enqueue("DataGet", () =>
            {
                try
                {
                    var datain = local_rsp.Count > 0 ? local_rsp[0] : null;
                    local_rsp.Clear();
                    switch (cmd)
                    {
                        case ServiceCommands.RdSessions:
                            {
                                foreach (RdFrecuency frec in Frecuencies.Values)
                                {
                                    foreach (IRdResource irdr in frec.RdRs.Values)
                                    {
                                        foreach (RdResource rdr in irdr.GetListResources())
                                        {
                                            GlobalTypes.radioSessionData data = new GlobalTypes.radioSessionData()
                                            {
                                                frec = frec.Frecuency,
                                                // Tipo de Frecuencia 0: Normal, 1: 1+1, 2: FD, 3: EM
                                                ftipo = (int)frec.GetParam.FrequencyType,
                                                // Prioridad de session, 0: Normal, 1: Emergencia
                                                prio = frec.GetParam.Priority == CORESIP_Priority.CORESIP_PR_NORMAL ? 0
                                                    : frec.GetParam.Priority == CORESIP_Priority.CORESIP_PR_EMERGENCY ? 1 : 2,
                                                // Metodo de Calculo CLD. 0: Relativo, 1: Absoluto
                                                fp_climax_mc = (int)frec.GetParam.CLDCalculateMethod,
                                                // Ventana BSS
                                                fp_bss_win = frec.GetParam.BssWindows,
                                                // Estado Frecuencia. 0: No Disponible. 1: Disponible, 2: Degradada.
                                                fstd = (int)frec.Status,
                                                // Emplazamiento Seleccionado y QIDX del seleccionado...
                                                selected_site = frec.SelectedSite,
                                                selected_site_qidx = frec.SelectedSiteQidx,
                                                /** 20180618. Funcion Transmisor seleccionado */
                                                selected_tx = frec.SelectedTxSiteString,

                                                uri = rdr.Uri1,
                                                tipo = rdr.Type.ToString(),
                                                std = rdr.Connected ? 1 : 0,

                                                tx_rtp = rdr.new_params.tx_rtp_port,
                                                tx_cld = rdr.new_params.tx_cld,
                                                tx_owd = rdr.new_params.tx_owd,
                                                rx_rtp = rdr.new_params.rx_rtp_port,
                                                rx_qidx = rdr.new_params.rx_qidx,
                                                /** 20170807. */
                                                site = rdr.new_params.site
                                            };

                                            local_rsp.Add(data);
                                        }
                                    }
                                local_rsp = local_rsp
                                    .OrderBy(o => ((GlobalTypes.radioSessionData)o).std)
                                    .ThenBy(o => ((GlobalTypes.radioSessionData)o).frec).ToList();
                                }
                                local_rsp = local_rsp
                                    .OrderBy(o => ((GlobalTypes.radioSessionData)o).std)
                                    .ThenBy(o => ((GlobalTypes.radioSessionData)o).frec).ToList();
                            }
                            retorno = true;
                            break;

                        case ServiceCommands.RdMNGearListGet:
                            {
                                foreach (BaseGear eq in MNManager.NodePool.Values)
                                {
                                    if (eq.FrecuencyType == Tipo_Frecuencia.UHF || eq.FrecuencyType == Tipo_Frecuencia.VHF)
                                    {
                                        GlobalTypes.equipoMNData equipo = new GlobalTypes.equipoMNData()
                                        {
                                            equ = eq.Id,
                                            grp = eq.FrecuencyType == Tipo_Frecuencia.VHF ? 0 : 1,
                                            mod = eq.IsEmitter == true ? 0 : 1,
                                            tip = eq.ChannelType == Tipo_Canal.Monocanal ? 0 : 1,
                                            std = (int)eq.Status,
                                            frec = eq.Frecuency,
                                            prio = eq.Priority == null ? 0 : (int)eq.Priority,
                                            sip = CoberturaSip(eq),
                                            ip = eq.IP,
                                            tfrec = 0,                      // TODO.
                                            emp = eq.IdEmplazamiento
                                        };

                                        local_rsp.Add(equipo);
                                    }
                                }
                            }
                            retorno = true;
                            break;

                        case ServiceCommands.RdHFGetEquipos:
                            lock (_gestorHF.Equipos)
                            {
                                foreach (RdGestorHF.EquipoHF thf in _gestorHF.Equipos)
                                {
                                    local_rsp.Add(new GlobalTypes.txHF()
                                    {
                                        id = thf.IdEquipo,
                                        gestor = thf.IpRcs,
                                        std = (int)thf.Estado,
                                        oid = thf.Oid,
                                        fre = thf.IDFrecuencia,
                                        user = thf.Usuario,
                                        uri = thf.SipUri
                                    });
                                }
                            }
                            retorno = true;
                            break;

                        case ServiceCommands.RdUnoMasUnoData:
                            var UnoMasUnoFreqs = MSResources.Select(r => new
                                {
                                    fr = r.Frecuency,
                                    id = r.ID,
                                    site = r.Site,
                                    tx = r.isTx ? 1 : 0,
                                    ab = 0,
                                    sel = r.GetContainerPair().ActiveResource.Uri1 == r.Uri1 ? 1 : 0,
                                    ses = r.Connected ? 1 : 0,
                                    uri = r.Uri1
                                })
                                .OrderBy(r=>r.fr).ThenBy(r=>r.id)
                                .ToList();
                            UnoMasUnoFreqs.ForEach(item => local_rsp.Add(item));
                            retorno = true;
                            break;

                        case ServiceCommands.SrvDbg:
                            if (datain == null)
                            {
                                var srv = new
                                {
                                    Master = _Master,
                                    SipIp = _SipIp,
                                    Status = _Status,
                                    LastVersion = LastVersion,
                                    Frequencies = from f in Frecuencies.Values
                                                  select new
                                                  {
                                                      Id = f.Frecuency,
                                                      Status = f.Status,
                                                      NewParams = new
                                                      {
                                                          cld_supervision_time = f.GetParam.Cld_supervision_time,
                                                          Priority = f.GetParam.Priority,
                                                          FrequencyType = f.GetParam.FrequencyType,
                                                          CLDCalculateMethod = f.GetParam.CLDCalculateMethod,
                                                          BssWindows = f.GetParam.BssWindows,
                                                          AudioSync = f.GetParam.AudioSync,
                                                          AudioInBssWindow = f.GetParam.AudioInBssWindow,
                                                          NotUnassignable = f.GetParam.NotUnassignable,
                                                          MetodosBssOfrecidos = f.GetParam.MetodosBssOfrecidos
                                                      },
                                                      PublicData = f.PublicData,
                                                      PrivateData = f.PrivateData
                                                  },
                                    FrecuenciesReserved = from f in FrecuenciesReserved.Values
                                                          select new { Id = f.Frecuency },
                                    SndRxPorts = from item in _SndRxPorts
                                                 select new { key = item.Key, val = item.Value },
                                    UsrFreq = from item in _UsrFreq
                                              select new { key1 = item.Key.Key, key2 = item.Key.Value, Freq = item.Value.Frecuency },
                                    RdRParam = from item in RdRParam
                                               select new
                                               {
                                                   key = item.Key,
                                                   Par = item.Value
                                               },

                                };
                                local_rsp.Add(srv);
                            }
                            else if (datain is string)
                            {
                                /** Solo pide una frecuencia */
                                string fr = datain as string;
                                var fro = Frecuencies.Values.Where(f => f.Frecuency == fr).FirstOrDefault();
                                if (fro != null)
                                {
                                    var jfr = new
                                    {
                                        Id = fro.Frecuency,
                                        Status = fro.Status,
                                        NewParams = new
                                        {
                                            cld_supervision_time = fro.GetParam.Cld_supervision_time,
                                            Priority = fro.GetParam.Priority,
                                            FrequencyType = fro.GetParam.FrequencyType,
                                            CLDCalculateMethod = fro.GetParam.CLDCalculateMethod,
                                            BssWindows = fro.GetParam.BssWindows,
                                            AudioSync = fro.GetParam.AudioSync,
                                            AudioInBssWindow = fro.GetParam.AudioInBssWindow,
                                            NotUnassignable = fro.GetParam.NotUnassignable,
                                            MetodosBssOfrecidos = fro.GetParam.MetodosBssOfrecidos
                                        },
                                        PublicData = fro.PublicData,
                                        PrivateData = fro.PrivateData
                                    };
                                    local_rsp.Add(jfr);
                                }
                                else
                                {
                                    local_rsp.Add(new { msg = "Frecuencia " + fr + " no encontrada en el servicio" });
                                }
                            }
                            else
                            {
                                local_rsp.Add(new { msg = "Peticion no soportada" });
                            }
                            retorno = true;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ExceptionManage<RdService>("DataGet", ex, "On DataGet Exception: " + ex.Message, false);
                }
                finally
                {
                    sync.Set();
                }
            });

            sync.WaitOne(10000);
            return retorno;
        }
        /** 20200225. Estado de cada módulo adicional de Radio */
        bool MNRadioModule = false;
        bool HFRadioModule = false;
        bool MSRadioModule = false;
        public object AllDataGet()
        {
            var level = Status != ServiceStatus.Running ? "Error" : Master == true ? "Master" : "Slave";
            return new
            {
                std = Status.ToString(),
                level,
                modules = new[]
                        {
                            new
                            {
                                id="M+N",
                                enable = MNRadioModule ? 1 : 0,
                                std = MNRadioModule ? Status.ToString() : "",
                                level = MNRadioModule ? level : ""
                            },
                            new
                            {
                                id="HF",
                                enable = HFRadioModule ? 1 : 0,
                                std = HFRadioModule ? Status.ToString() : "",
                                level = HFRadioModule ? level : ""
                            },
                            new
                            {
                                id="1+1",
                                enable = MSRadioModule ? 1 : 0,
                                std = MSRadioModule ? Status.ToString() : "",
                                level = MSRadioModule ? level : ""
                            }
                        }
            };
        }
        /// <summary>
        /// Obtiene la lista de recursos radio que pertenecen a pares 1+1
        /// </summary>
        public IEnumerable<RdResource> MSResources
        {
            get
            {
                var ret = Frecuencies.Values
                                .Where(f => f.RdRs.Values.Where(r => r is RdResourcePair).ToList().Count > 0)   // Son Frecuencias 1+1 las que tienen algun RdResourcePair
                                .SelectMany(f => f.RdRs.Values)
                                .Where(r => r is RdResourcePair)        // De todos los recursos selecciono los pareados.
                                .SelectMany(rp =>
                                {
                                    var resources = (rp as RdResourcePair).GetListResources();
                                    resources.ForEach(r => r.SetContainerPair((rp as RdResourcePair)));
                                    return resources;                   // Por cada pareado selecciona ambos componentes y marca su container...
                                });
                return ret;
            }
        }

        public void SetData(ServiceCommands cmd, object data, Action<bool, string> reply)
        {
            ManualResetEvent sync = new ManualResetEvent(false);
            bool retorno = false;
            string err = default(string);
            _EventQueue.Enqueue("DataSet", () =>
            {
                try
                {
                    switch (cmd)
                    {
                        case ServiceCommands.RdHFLiberaEquipo:
                            if (data is GlobalTypes.txHF)
                            {
                                retorno = _gestorHF.LiberaEquipo(((GlobalTypes.txHF)data).id);
                            }
                            else
                            {
                                throw new Exception(String.Format("El objeto {0} no es del tipo esperado (txHF)", data));
                            }
                            break;
                        case ServiceCommands.RdUnoMasUnoActivate:
                            if (data is string)
                            {
                                retorno = ActivateResource(data as string, ref err);
                            }
                            else
                            {
                                throw new Exception(String.Format("El objeto {0} no es del tipo esperado (string)", data));
                            }
                            break;
                    }
                }
                catch (Exception x)
                {
                    ExceptionManage<RdService>("SetData", x, "On SetData Exception: " + x.Message, false);
                    err = x.Message;
                }
                finally
                {
                    sync.Set();
                }
            });
            sync.WaitOne(10000);
            reply(retorno, err);
        }

        /** Fin de la Modificacion */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool RdSessionsGet(string par, ref string err, List<string> resp = null)
        {
            if (resp == null)
            {
                err = "Lista de entrada NULA";
                return false;
            }

            resp.Clear();
            foreach (RdFrecuency frec in Frecuencies.Values)
            {
                foreach (RdResource rdr in frec.RdRs.Values)
                {
                    string strsession = string.Format("{0}##{1}##{2}##{3}", frec.Frecuency, rdr.Uri1, rdr.Type, rdr.Connected);
                    resp.Add(strsession);
                }
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns>0: NA. 1: Con Cobertura. 2: Sin Cobertura</returns>
        private int CoberturaSip(BaseGear gear)
        {
            if (gear.Status == GearStatus.Assigned)
            {
                RdFrecuency frec = Frecuencies.Values.Where(f => f.Frecuency == gear.Frecuency).FirstOrDefault();
                if (frec == null)
                    return 2;
                /* RdResource rec = frec.RdRs.Values.Where(r => (r.Type == RdRsType.Rx && gear.IsEmitter == false) ||
                                                              (r.Type == RdRsType.Tx && gear.IsEmitter == true)).FirstOrDefault();*/
                // RdResource rec = frec.RdRs.Values.Where(r => r.ID == gear.Id).FirstOrDefault(); // JOI OJO 2017
                IRdResource rec = frec.RdRs.Values.Where(r => r.Uri1 == gear.SipUri).FirstOrDefault();

                return rec == null ? 1 : rec.Connected ? 3 : 2;
            }
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool NMGearsGet(string par, ref string err, List<string> resp = null)
        {
            if (resp != null)
            {
                resp.Clear();
                foreach (BaseGear eq in MNManager.NodePool.Values)
                {
                    if (eq.FrecuencyType == Tipo_Frecuencia.UHF || eq.FrecuencyType == Tipo_Frecuencia.VHF)
                    {
                        string strequipo = string.Format("{0}##{1}##{2}##{3}##{4}##{5}##{6}##{7}##{8}",
                            eq.Id,
                            eq.FrecuencyType == Tipo_Frecuencia.VHF ? 0 : 1,    // VHF o UHF
                            eq.IsEmitter == true ? 0 : 1,                       // Receptor o Transmisor
                            eq.ChannelType == Tipo_Canal.Monocanal ? 0 : 1,     // Main o RSVA
#if DEBUG1
                            eq.ChannelType== Tipo_Canal.Monocanal ?
                                (resp.Count < 4 ? 0 :
                                resp.Count < 8 ? 1 :
                                resp.Count < 12 ? 2 :
                                resp.Count < 16 ? 3 : 4) :
                                (resp.Count < 10 ? 1 : 2),
#else
                            (int)eq.Status,                                     // 20160912. AGL. eq.IsAvailable ? 1 : 0,
#endif
                            eq.Frecuency,
                            eq.Priority,
                            CoberturaSip(eq),
                            eq.IP
                            );
                        resp.Add(strequipo);
                    }
                }
                return true;
            }
            err = "Lista de entrada NULA";
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool NMGearReset(string par, ref string err, List<string> resp = null)
        {
            MNManager.Stop();
            MNManager.Start();
            LogInfo<RdService>("Gestor NM reiniciado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND, "MNManager", CTranslate.translateResource("Reiniciado"));
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool NMGearToogle(string par, ref string err, List<string> resp = null)
        {
            if (!MNManager.NodeToogle(par))
            {
                LogInfo<RdService>("Equipo " + par + " no encontrado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR, "RdService",
                    CTranslate.translateResource("Equipo " + par + " no encontrado."));
                err = "Equipo " + par + " no encontrado.";
                return false;
            }
            //LogInfo<RdService>("Equipo " + par + " conmutado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND);
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool NMGearAssign(string par, ref string err, List<string> resp = null)
        {
            string frq = err;
            
            if (!MNManager.NodeAllocate(par, err, GearStatus.Forbidden))
            {
                err = "Error asignando equipo " + par + " a frecuencia " + err + ".";
                LogInfo<RdService>(err, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR, CTranslate.translateResource("RdService", "Error asignando equipo " + par  + " a frecuencia " + frq));
                return false;
            }
            LogInfo<RdService>("Equipo " + par + " asignado a frecuencia " + err + ".", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND,
                par, CTranslate.translateResource("Equipo asignado a frecuencia " + frq));
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool NMGearUnassing(string par, ref string err, List<string> resp = null)
        {
            if (!MNManager.NodeDeallocate(par))
            {
                err = "Error liberando equipo " + par + ".";
                LogInfo<RdService>(err, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR, "RdService", CTranslate.translateResource("Error liberando equipo " + par));
                return false;
            }
            LogInfo<RdService>("Equipo " + par + " liberado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND,
                par, CTranslate.translateResource("Liberado."));
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool MNValidationTick(string par, ref string err, List<string> resp = null)
        {
            MNManager.Stop();
            MNManager.TimerInterval = Convert.ToInt32(par);
            MNManager.Start();
            LogInfo<RdService>("Intervalo de validacion de NM Manager puesto a " + par + ".",
                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND, "MNManager",
                CTranslate.translateResource("Tiempo= " + par));
            return true;
        }
        /// <summary>
        /// 20160928. AGL. Estado del Servicio para las Páginas WEB.
        /// </summary>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        private bool MNStatus(string par, ref string err, List<string> resp = null)
        {
            err = MNManager != null ? MNManager.Status.ToString() : "ERROR";
            return true;
        }
        /// <summary>
        /// Manual command to make the resource become active. 
        /// As a result, the resource may not become active if it has no SIP session.
        /// </summary>
        /// <param name="par">if of the resource</param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns>false if command not done because the resource hasn't been found</returns>
        private bool ActivateResource(string par, ref string err, List<string> resp = null)
        {
            foreach (RdFrecuency freq in Frecuencies.Values)
                if (freq.ActivateResource(par))
                {
                    LogInfo<RdService>("Equipo " + par + " conmutado manualmente", 
                        U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService",                
                        CTranslate.translateResource("Equipo " + par + " conmutado manualmente"));
                    return true;
                }

            LogInfo<RdService>("Equipo " + par + " no encontrado.", 
                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR, "RdService",
                CTranslate.translateResource("Equipo " + par + " no encontrado."));
            err = "Equipo " + par + " no encontrado.";
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            try
            {
#if DEBUG
                LogInfo<RdService>("Iniciando Servicio ...");
#endif
                U5kiIncidencias.Inicializa("inci");
                ExceptionManageInit();

                _Master = false;
                _Status = ServiceStatus.Running;
                LastVersion = String.Empty;

                _EventQueue.Start();

                if (string.IsNullOrEmpty(_SipIp))
                {
                    List<string> ips = General.GetOperationalV4Ips();
                    _SipIp = (ips.Count > 0) ? ips[ips.Count - 1] : "127.0.0.1";
                }

#if DEBUG
                if (!Globals.Test.IsTestRunning)
#endif
                RdRegistry.Init(OnChannelError, OnMasterStatusChanged, OnResourceChanged, OnMsgReceived);

#if _MN_NEW_START_STOP
#else
                MNManager.Start();
#endif
                LogInfo<RdService>("Servicio Iniciado", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", CTranslate.translateResource("Iniciado"));
            }
            catch (Exception ex)
            {
                //LogFatal<RdService>("Excepcion no esperada arrancando servicio de radio. ERROR: " + ex.Message,
                //    U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
                ExceptionManage<RdService>("Start", ex, "Excepcion no esperada arrancando servicio de radio. ERROR: " + ex.Message);
                Stop();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            AutoResetEvent _Sync = new AutoResetEvent(false);

            LogInfo<RdService>("Iniciando parada servicio radio.");

            /** Lo ejecuta el mismo Thread... */
            _EventQueue.Enqueue("Stop....", delegate()
            {
                try
                {
                    _Timer.Enabled = false;
                    U5kiIncidencias.Dispose();

#if _MN_NEW_START_STOP
                    if (MNManager.Status == ServiceStatus.Running)
#endif
                        MNManager.Stop();

                    if (_Status == ServiceStatus.Running)
                    {
                        Dispose();
                    }
                }
                catch (Exception ex)
                {
                    //LogFatal<RdService>("Excepcion no esperada parando servicio de radio. ERROR: " + ex.Message,
                    //    U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
                    ExceptionManage<RdService>("Stop", ex, "Excepcion no esperada parando servicio de radio. ERROR: " + ex.Message);
                }
                finally
                {
                    _Status = ServiceStatus.Stopped;
                    _Sync.Set();
                }

            });

            _Sync.WaitOne(10000);
            _Sync.Close();
            _EventQueue.Stop();
            LogInfo<RdService>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", CTranslate.translateResource("Detenido"));
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        private void Dispose()
        {
            RdRegistry.End();
            LogTrace<RdService>("Dispose: RdRegistry.Ended");

            if (_Master == true)
            {
                /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
                FinalizeSip();
                //SipAgent.End();
                //LogTrace<RdService>("Dispose SipAgent.Ended");
                /*******************/

                /** AGL.HF. Destruir el objeto HF */
                _gestorHF.Limpiar();
                Frecuencies.Clear();
                _SndRxPorts.Clear();
                // JOI 201709 NEWRDRP INI
                _RdRParam.Clear();
                // JOI 201709 NEWRDRP FIN				
            }
            MNManager.Dispose();
            // TODO: Query de historicos dispose.
        }

        #region RdRegistry Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        private void OnChannelError(object sender, string error)
        {
            _EventQueue.Enqueue("OnChannelError", delegate()
            {
#if _LOCKING_
				lock (_lock)
#endif
                {
                    try
                    {
                        _Timer.Enabled = false;
                        Dispose();
                        _Status = ServiceStatus.Stopped;
                        _EventQueue.InternalStop();
                        LogError<RdService>("OnChannelError: " + error, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
                    }
                    catch (Exception x)
                    {
                        //LogException<RdService>("OnChannelError", x);
                        ExceptionManage<RdService>("OnChannelError", x, "OnChannelError Exception: " + x.Message);
                    }
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        bool _sync = false;
#if DEBUG
        public void OnMasterStatusChanged(object sender, bool master)
#else
        private void OnMasterStatusChanged(object sender, bool master)
#endif
        {
            // Debug.Assert(_Master != master);
            // _Logger.Info("RdService.OnMasterStatusChanged {0}", _IsClosing);
            _EventQueue.Enqueue("OnMasterStatusChanged-2", delegate()
            {
                if (_Master == master)
                {
                    //Espero a publicar para asegurarme que no se cruzan con los mensajes de
                    //un master temporal
                    System.Threading.Thread.Sleep(1000);
                    if (_Master)
                    {
                        foreach (RdFrecuency rdFr in Frecuencies.Values)
                        {
                            rdFr.Publish();
                        }
                    }
                }
            });

            _EventQueue.Enqueue("OnMasterStatusChanged-1", delegate()
            {
#if DEBUG
                if (Globals.Test.IsTestRunning)
                    return;
#endif
                if (master && !_Master)
                {
                    try
                    {
                        LogInfo<RdService>("U5ki.RdService ==> " + "MASTER");
#if DEBUG
                        LogTrace<RdService>("RdService.OnMasterStatusChanged => Activating Registry.");
#endif
                        RdRegistry._Master = true;
                        InitializeSip();
                        _Timer.Enabled = true;
                        _Timer.Start();

#if _MN_NEW_START_STOP
                        MNManager.Start();
#endif
                        LogInfo<RdService>("MASTER", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", "MASTER");

                         _Master = master;

                        /** 20161219. AGL. Comprobar que no es la misma config, generada por una entrada 'merge' de otro NBX. */
                        LastVersion = string.Empty;
                        //A veces no llega la configuración....asi que uso lo último recibido
                        if (_Cfg != null)
                            ProcessNewConfig(_Cfg);
                        //System.Threading.Thread.Sleep(1000);
                        //retraso el refresco al entrar en master por si soy master transitorio
                        foreach (RdFrecuency rdFr in Frecuencies.Values)
                        {
                            rdFr.Publish();
                        }
                    }
                    catch (Exception ex)
                    {
                        //LogException<RdService>("OnMasterStatusChanged (==>MASTER)", ex);
                        ExceptionManage<RdService>("OnMasterStatusChangedMaster", ex, "OnMasterStatusChanged => MASTER Exception: " + ex.Message);
                    }
                }
                else if (!master && _Master)
                {
                    try
                    {
                        LogInfo<RdService>("U5ki.RdService ==> " + "SLAVE");
                        RdRegistry._Master = false;
                        //Lo adelanto para que no entren las tareas periodicas de inicio sesion SIP
                        _Master = master;
                        /** AGL... */
                        _gestorHF.Limpiar();

                        _Timer.Enabled = false;

                        /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
                        FinalizeSip();
                        // SipAgent.End();
                        /****/

                        //Frecuencies.Clear();
                        _SndRxPorts.Clear();
						
                        // JOI 201709 NEWRDRP INI
                        _RdRParam.Clear();
                        // JOI 201709 NEWRDRP FIN						
#if _MN_NEW_START_STOP
                        if (MNManager.Status == ServiceStatus.Running)
                            MNManager.Dispose();//JOI 20170907 antes Stopped
#endif                        
                        LogInfo<RdService>("SLAVE", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", "SLAVE");
                    }
                    catch (Exception x)
                    {
                        //LogException<RdService>("OnMasterStatusChanged (==>SLAVE)", x);
                        ExceptionManage<RdService>("OnMasterStatusChangedSlave", x, "OnMasterStatusChanged => SLAVE Exception: " + x.Message);
                    }
                }

            });

        }

        /// <summary>
        /// 
        /// </summary>
        Semaphore _onResourceChangedSemaphore = new Semaphore(1, 1);
        private void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (e.Type == Identifiers.TypeId(typeof(Cd40Cfg)))
            {
                if (e.Content != null)
                {
                    try
                    {
                        MemoryStream ms = new MemoryStream(Tools.Decompress(e.Content));
                        _Cfg = Serializer.Deserialize<Cd40Cfg>(ms);

                        LogInfo<RdService>(String.Format("Recibida nueva configuracion ({0})", _Cfg.Version), U5kiIncidencias.U5kiIncidencia.IGNORE);

                        _EventQueue.Enqueue("OnCfgChanged", delegate()
                        {
                            _onResourceChangedSemaphore.WaitOne();
                            try
                            {
                                ProcessNewConfig(_Cfg);
                            }
                            catch (Exception ex)
                            {
                                //LogException<RdService>("Error Procesando Configuracion", ex);
                                ExceptionManage<RdService>("OnResourceChanged0", ex, "OnConfigurationChanged Exception: " + ex.Message);
                            }
                            finally
                            {
                                _onResourceChangedSemaphore.Release();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        //LogException<RdService>("OnResourceChanged", ex);
                        ExceptionManage<RdService>("OnResourceChanged1", ex, "OnResourceChanged Exception: " + ex.Message);
                    }
                }
            }
            else if (e.Type == Identifiers.TypeId(typeof(TopRs)))
            {
                if (e.Content == null)
                {
                    _EventQueue.Enqueue("OnTopRemoved", delegate()
                    {
                        ProcessTopRemoved(e.Id);
                    });
                }
            }
            else if (e.Type == Identifiers.TypeId(typeof(MNDisabledNodes)))
            {
                if (e.Content != null)
                {
                    _EventQueue.Enqueue("OnMNDisabledNodes", delegate()
                    {
                        MemoryStream ms = new MemoryStream(e.Content);
                        MNDisabledNodes DisabledNodesInfo = Serializer.Deserialize<MNDisabledNodes>(ms);

                        LogDebug<RdService>(String.Format("MNDISABEDNODES RECEIVED: {0} nodes", DisabledNodesInfo.nodes.Count));

                        if (_Master == false)
                        {
                            MNManager.NodePoolForbiddenActualize(DisabledNodesInfo.nodes);
                        }
                    });
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnMsgReceived(object sender, SpreadDataMsg msg)
        {
            if (!_Master)
                return;
            MemoryStream ms= new MemoryStream(msg.Data, 0, msg.Length);
            switch (msg.Type)
            {
                case Identifiers.FR_RX_CHANGE_ASK_MSG:                         
                    _EventQueue.Enqueue("RxChangeAsk", delegate()
                    {
                        FrRxChangeAsk ask; 
                        try
                        {                               
                            ms = new MemoryStream(msg.Data, 0, msg.Length);
                            ask = Serializer.Deserialize<FrRxChangeAsk>(ms);
                            ProcessRxChangeAsk(msg.From, ask);
                        }
                        catch (Exception ex)
                        {
                            ExceptionManage<RdService>("OnMsgReceived", ex, "RxChangeAsk Exception: " + ex.Message +
                                ". Data: " + BitConverter.ToString(msg.Data, 0, msg.Length),
                                false);
                        }
                    });
                    break;

                case Identifiers.FR_TX_CHANGE_ASK_MSG:
                    _EventQueue.Enqueue("TxChangeAsk", delegate()
                    {
                        FrTxChangeAsk ask;
                        try
                        {
                            ms = new MemoryStream(msg.Data, 0, msg.Length);
                            ask = Serializer.Deserialize<FrTxChangeAsk>(ms);
                                ProcessTxChangeAsk(msg.From, ask);
                        }
                        catch (Exception ex)
                        {
                            ExceptionManage<RdService>("OnMsgReceived", ex, "TxChangeAsk Exception: " + ex.Message +
                                ". Data: " + BitConverter.ToString(msg.Data, 0, msg.Length),
                                false);
                        }
                    });
                    break;

                case Identifiers.PTT_CHANGE_ASK_MSG:
                    _EventQueue.Enqueue("PttChangeAsk", delegate()
                    {
                        try
                        {
                            PttChangeAsk ask = Serializer.Deserialize<PttChangeAsk>(ms);
                            ProcessPttChangeAsk(msg.From, ask);
                        }
                        catch (Exception ex)
                        {
                            ExceptionManage<RdService>("OnMsgReceived", ex, "PttChangeAsk Exception: " + ex.Message +
                                ". Data: " + BitConverter.ToString(msg.Data, 0, msg.Length),
                                false);
                        }
                    });
                    break;

                case Identifiers.RTX_GROUP_CHANGE_ASK_MSG:
                    _EventQueue.Enqueue("RtxGroupChangeAsk", delegate()
                    {
                        try
                        {
                            RtxGroupChangeAsk ask = Serializer.Deserialize<RtxGroupChangeAsk>(ms);
                            ProcessRtxGroupChangeAsk(msg.From, ask);
                        }
                        catch (Exception ex)
                        {
                            ExceptionManage<RdService>("OnMsgReceived", ex, "RtxChangeAsk Exception: " + ex.Message +
                                ". Data: " + BitConverter.ToString(msg.Data, 0, msg.Length),
                                false);
                        }
                    });
                    break;

                case Identifiers.SELCAL_PREPARE:
                    _EventQueue.Enqueue("SelcalPrepare", delegate()
                    {
                        SelcalPrepareMsg ask = Serializer.Deserialize<SelcalPrepareMsg>(ms);
                        ProcessSelcalPrepare(msg.From, ask);
                    });
                    break;

                case Identifiers.SELCAL_SEND_TONES:
                    _EventQueue.Enqueue("SelcalSendTones", delegate()
                    {
                        SelcalPrepareMsg ask = Serializer.Deserialize<SelcalPrepareMsg>(ms);
                        ProcessSelcalSendTones(msg.From, ask);
                    });
                    break;

                case Identifiers.SITE_CHANGING_MSG:
                    _EventQueue.Enqueue("ChangingSite", delegate()
                    {
                        ChangeSiteMsg ask = Serializer.Deserialize<ChangeSiteMsg>(ms);
                        ProcessChangingSite(msg.From, ask);
                    });
                    break;

                ///** 20180316. MNDISABEDNODES */
                //case Identifiers.MNDISABLED_NODES:
                //    _EventQueue.Enqueue("MNDISABLEDNODES", delegate()
                //    {
                //        MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                //        MNDisabledNodes DisabledNodesInfo = Serializer.Deserialize<MNDisabledNodes>(ms);

                //        LogDebug<RdService>(String.Format("MNDISABEDNODES RECEIVED: {0} nodes", DisabledNodesInfo.nodes.Count));

                //        if (_Master == false)
                //        {
                //            MNManager.NodePoolForbiddenActualize(DisabledNodesInfo.nodes);
                //        }
                //    });

                //    break;

                default:
                    break;
            }
        }

        #endregion

        #region Timers Handlers

        /// <summary>
        /// Solo se activa después de cada actuación sobre un canal HF
        /// </summary>
        void TimerHfStatus_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_Master)
                return;

            SendStatusHF();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            _EventQueue.Enqueue("RetryFailedConnectionsTimer", delegate()
            {
                if (!_Master)
                    return;

                LogTrace<RdService>("OnTimer");

                _gestorHF.CheckFrequency();
                foreach (RdFrecuency rdFr in Frecuencies.Values)
                {
                    try
                    {
                        rdFr.RetryFailedConnections();
                        rdFr.CheckFrequency();
                        //if (rdFr.SanityCheckCalls())
                        //    rdFr.LimpiaLlamadaDeRecurso();
                    }
                    catch (Exception ex)
                    {
                        //LogException<RdService>("OnTimer.RetryFailedConnections", ex, false);
                        ExceptionManage<RdService>("OnTimer", ex, "OnRetryFailedConnextions Exception: " + ex.Message);
                    }
                }

                if (_HFTimerCount == 0 || ++_contTimerEvents >= _HFTimerCount)
                {
                    _contTimerEvents = 0;
#if _HF_RTPOLL_
                    LogTrace<RdService>("Monitorizando Equipos HF.");

                    // 20171005. AGL. Se ha implementado un procedimiento interno de supervisio mas controlado...
                    //foreach (RdGestorHF.EquipoHF equipo in _gestorHF.Equipos)
                    //{
                    //    (new Thread(new ParameterizedThreadStart(EstadoEquipoHF)) { IsBackground = true }).Start(equipo);
                    //}
                    _gestorHF.SupervisaEstadoEquipos();
                    ///////////////////////////////////////////////////////////////////////////////////////////
#else
                    Func<bool> TxChange = ProcessHfMonitoring;
                    TxChange.BeginInvoke(null, TxChange);
#endif
                }

#if _HF_GLOBAL_STATUS_
                SendStatusHF();
#endif
#if DEBUG
                TestMNDisabledPublish();
#endif
            });
        }

        #endregion

#if _HF_RTPOLL_
        // 20171005. AGL. Se ha implementado un procedimiento interno de supervisio mas controlado...
        /// <summary>
        /// ojo...
        /// </summary>
        //protected void EstadoEquipoHF(object obj)
        //{
        //    EquipoHF _equipo = (EquipoHF)obj;
        //    lock (_equipo)
        //    {
        //        try
        //        {
        //            _equipo.GetEstado();
        //        }
        //        catch (Exception x)
        //        {
        //            //LogException<RdService>(String.Format("EXCP. HFMON {0}({1}): {2}", _equipo.IdEquipo, _equipo.IpRcs, x.Message), x, false);
        //            ExceptionManage<RdService>("EstadoEquipoHF", x, "OnEstadoEquipoHF Exception: " + x.Message);
        //        }
        //    }
        //}
#else
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool ProcessHfMonitoring()
        {
            _Logger.Debug("RdService. Iniciando monitorización estado equipos HF.");            
            foreach (RdGestorHF.EquipoHF equipo in _gestorHF.Equipos)            
            {            
                equipo.GetEstado();                
                _Logger.Debug("RdService. Chequeado Estado de Equipo HF {0}.", equipo.IdEquipo);                
            }            
            _Logger.Debug("RdService. Fin monitorización estado equipos HF.");            
            return true;
        }
#endif

#if _HF_GLOBAL_STATUS_
        /// <summary>
        /// 
        /// </summary>
        private void SendStatusHF()
        {
            HFStatus status = new HFStatus() { code = HFStatusCodes.DISC };
            status.code = _gestorHF.GlobalStatus();
            RdRegistry.SendHFStatus(status);
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="rdFr"></param>
        private void OnRdFrecuencyTimerElapsed(object timer, RdFrecuency rdFr)
        {
            _EventQueue.Enqueue(rdFr.Frecuency + "_PublishChanges", delegate()
            {
                if (!_Master)
                    return;
                rdFr.PublishChanges(timer);
            });
        }
        /// <summary>
        /// 
        /// </summary>
        private String LastVersion { get; set; }
        public Cd40Cfg Cfg { get => _Cfg; set => _Cfg = value; }

        /// <summary>
        /// Procesado de la Configuracion. 
        /// </summary>
#if DEBUG
        public void ProcessNewConfig(Cd40Cfg cfg)
#else
        private void ProcessNewConfig(Cd40Cfg cfg)
#endif
        {
            /** Se recibe la configuracion radio.*/
            if (!_Master)
                return;

            /** 20161219. AGL. Comprobar que no es la misma config, generada por una entrada 'merge' de otro NBX. */
            if (LastVersion == cfg.Version)
                return;

            LastVersion = cfg.Version;
            LogInfo<RdService>(String.Format("Procesando nueva configuracion ({0})", cfg.Version), U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                "RdService", CTranslate.translateResource("Procesando nueva configuración ( " + cfg.Version + " )"));

            try
            {
                _Timer.Enabled = false;
                _Timer.Stop();

                MNManager.StopConfig();

//                /** AGL.HF  Inicializar la gestion HF.*/
//                HFStatus status = new HFStatus() { code = HFStatusCodes.DISC };

//#if DEBUG
//                if (!Globals.Test.IsTestRunning)
//                {
//#endif
//                RdRegistry.SendHFStatus(status);
//                _gestorHF.Cargar(cfg);
//#if DEBUG
//                }
//#endif
                /** 20171116. Incializar el gestor HF, Eliminando el status que desasignaba todos los Tx asociados. */
                _gestorHF.Cargar(cfg);
                /** */

                Dictionary<string, bool> selectedRs = new Dictionary<string, bool>();

                Dictionary<string/*Freq*/, Dictionary<string/*Host*/, string/*rdResourceSelected*/>> _Picts;
                Dictionary<string, string> pPPPPPPPP = new Dictionary<string, string>();
                _Picts = new Dictionary<string, Dictionary<string, string>>();

                foreach (ConfiguracionUsuario userCfg in cfg.ConfiguracionUsuarios)
                {
                    foreach (CfgEnlaceExterno rdLink in userCfg.RdLinks)
                    {
                        string pict = GeneraListaDeFrecuenciaPorTop(cfg.ConfiguracionGeneral.PlanAsignacionUsuarios, userCfg.User.IdIdentificador, rdLink);

                        if (!_Picts.ContainsKey(rdLink.Literal))
                            _Picts.Add(rdLink.Literal, null);
                        else
                            pPPPPPPPP = _Picts[rdLink.Literal];

                        foreach (CfgRecursoEnlaceExterno rs in rdLink.ListaRecursos)
                            // JCAM 01072016
                            // Para considerar las frecuencias con cambio de emplazamiento
                            // hay que tener en cuenta los recursos que vienen configurados
                            // con el estado de "A"ctivos
                            if (rs.Estado == "S" || rs.Estado == "A")
                            {
                                bool active = false;
                                if (selectedRs.TryGetValue(rs.IdRecurso, out active))
                                {
                                    RdFrecuency rF;
                                    if (Frecuencies.TryGetValue(rdLink.Literal.ToUpper(), out rF))
                                    {
                                        foreach (KeyValuePair<string, IRdResource> rdRs in rF.RdRs)
                                        {
                                            if (rdRs.Value.ID == rs.IdRecurso)
                                            {
                                                rdRs.Value.SelectedSite = (rs.Estado == "S");
                                            }
                                        }
                                    }

                                    selectedRs[rs.IdRecurso] &= (rs.Estado == "S");
                                    if (rs.Estado == "S")
                                        pPPPPPPPP[pict] = rs.IdRecurso;
                                }
                                else
                                {
                                    RdFrecuency rF;
                                    if (Frecuencies.TryGetValue(rdLink.Literal.ToUpper(), out rF))
                                    {
                                        foreach (KeyValuePair<string, IRdResource> rdRs in rF.RdRs)
                                        {
                                            if (rdRs.Value.ID == rs.IdRecurso)
                                                rdRs.Value.SelectedSite = (rs.Estado == "S");
                                        }
                                    }
                                    selectedRs[rs.IdRecurso] = rs.Estado == "S";
                                    if (rs.Estado == "S")
                                        pPPPPPPPP[pict] = rs.IdRecurso;
                                }
                            }

                        //if (_Picts[rdLink.Literal] != null && _Picts[rdLink.Literal].Count > 0)
                        //    _Picts[rdLink.Literal].Clear();

                        _Picts[rdLink.Literal] = new Dictionary<string, string>(pPPPPPPPP);
                        pPPPPPPPP.Clear();
                    }
                }


                Dictionary<string, RdFrecuency> rdFrToRemove = new Dictionary<string, RdFrecuency>(Frecuencies);
                Frecuencies.Clear();

                _UsrFreq.Clear();
				
				//JOI 201709 NEWRDRP INI
				_RdRParam.Clear();
				//JOI 201709 NEWRDRP FIN
				
                foreach (ConfiguracionUsuario userCfg in cfg.ConfiguracionUsuarios)
                    foreach (CfgEnlaceExterno rdLink in userCfg.RdLinks)
                    {
                        string rdLinkId = rdLink.Literal.ToUpper();

                        //string pict = GeneraListaDeFrecuenciaPorTop(cfg.ConfiguracionGeneral.PlanAsignacionUsuarios, userCfg.User.IdIdentificador, rdLink);

                        if (!Frecuencies.ContainsKey(rdLinkId))
                        {
                            RdFrecuency rdFr;

                            if (rdFrToRemove.TryGetValue(rdLinkId, out rdFr))
                                rdFrToRemove.Remove(rdLinkId);
                            else
                            {
                                rdFr = new RdFrecuency(rdLink.Literal);
                                rdFr.TimerElapsed += OnRdFrecuencyTimerElapsed;
                                rdFr.SupervisionPortadora = rdLink.SupervisionPortadora;
                            }

                            // JCAM 24/02/2015
                            // Puede que haya cambiado algún parámetro de la frecuencia, 
                            // por ejemplo, la frecuencia sintonizada...
                            rdFr.TipoDeFrecuencia = Enum.Parse(typeof(Tipo_Frecuencia), rdLink.TipoFrecuencia.ToString()).ToString();
                            if (rdLink.TipoFrecuencia == Tipo_Frecuencia.HF)
                                rdFr.FrecuenciaSintonizada = rdLink.FrecuenciaSintonizada;
                            /** Genera los  INVITE de los nuevos y BYE de los antiguos por este orden de la frecuencia ***/
                            rdFr.Reset(cfg.ConfiguracionGeneral, rdLink, selectedRs);
#if DEBUG
                            base.LogTrace<RdService>("[FRECUENCY] Frecuency added: " + rdLinkId);
#endif
                            Frecuencies[rdLinkId] = rdFr;
                            Frecuencies[rdLinkId].Picts = _Picts[rdLink.Literal];
                    
                        }
                    }

                foreach (RdFrecuency rdFr in rdFrToRemove.Values)
                    rdFr.Dispose();
                //JOI 201709 NEWRDRP INI
                foreach (RdFrecuency rdfy in Frecuencies.Values)
                    foreach (IRdResource irdrsc in rdfy.RdRs.Values)
                    {
                        foreach (RdResource rdRes in irdrsc.GetListResources())
                            _RdRParam[rdRes.ID] = rdRes.new_params;
                    }
				
#if !DEBUG1
                MNManager.UpdatePool(cfg);

                MNManager.StartConfig();
#endif
                /** 20200225. Estado de cada módulo adicional de Radio */
                MNRadioModule = cfg.Nodes.Count() > 0;
                HFRadioModule = cfg.PoolHf.Count() > 0;
                MSRadioModule = MSResources.Count() > 0;

            }
            catch (Exception ex)
            {
                ExceptionManage<RdService>("Config", ex, "Excepcion no esperada process new config. ERROR: " + ex.StackTrace);
            }
            finally
            {
                //foreach (RdFrecuency rdFr in Frecuencies.Values)
                //{
                //    rdFr.Publish();
                //}

                _Timer.Enabled = true;
                _Timer.Start();
            }

#if DEBUG
            if (!Globals.Test.IsTestRunning)
#endif
            SendStatusHF();

        }
        /// <summary>
        /// Genera las asociaciones de frecuencias a Usuarios...
        /// </summary>
        private string GeneraListaDeFrecuenciaPorTop(List<AsignacionUsuariosTV> list, string idUsr, CfgEnlaceExterno rdLink)
        {
            string pict = string.Empty;

            AsignacionUsuariosTV usr = list.Find(delegate(AsignacionUsuariosTV p) { return (p.IdUsuario == idUsr); });
            if (usr != null)
            {
                KeyValuePair<string, string> clave = new KeyValuePair<string, string>(usr.IdHost, rdLink.Literal);
                if (!_UsrFreq.ContainsKey(clave))
                {
                    RdFrecuency rdFr;
                    rdFr = new RdFrecuency(rdLink.Literal);
                    rdFr.TimerElapsed += OnRdFrecuencyTimerElapsed;
                    rdFr.SupervisionPortadora = rdLink.SupervisionPortadora;
                    _UsrFreq[clave] = rdFr;

                    pict = usr.IdHost;
                }
            }

            return pict;
        }

        #region TxRx
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostId"></param>
        private void ProcessTopRemoved(string hostId)
        {
            if (!_Master)
                return;

            // Borrar en Coresip el puerto asociados al puesto de operador
            int sndRxPort = 0;
            if (_SndRxPorts.TryGetValue(hostId, out sndRxPort))
                SipAgent.DestroySndRxPort(sndRxPort);

            _SndRxPorts.Remove(hostId);

            foreach (RdFrecuency rdFr in Frecuencies.Values)
            {
                rdFr.SetRx(hostId, false, null);
                if (rdFr.TipoDeFrecuencia == "HF")
                {
                    RdFrecuency rdFrHf = rdFr;
#if _HF_GLOBAL_STATUS_
                    if (_gestorHF.GlobalStatus() != HFStatusCodes.DISC)
#endif
                        _gestorHF.DesasignarTx(hostId, ref rdFrHf);
#if _HF_GLOBAL_STATUS_
                    SendStatusHF();
#endif
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        private void ProcessRxChangeAsk(string from, FrRxChangeAsk ask)
        {
            if (_Master)
            {
                RdFrecuency rdFr;

                LogDebug<RdService>(String.Format("ProcessRxChangeAsk: Frecuencia {0} Host {1}", ask.Frecuency, ask.HostId),
                    U5kiIncidencias.U5kiIncidencia.IGNORE);

                if (Frecuencies.TryGetValue(ask.Frecuency.ToUpper(), out rdFr))
                {
                    rdFr.SetRx(ask.HostId, ask.Rx, from);
                    if (!ask.Rx && rdFr.TipoDeFrecuencia == "HF")
                    {
#if _HF_GLOBAL_STATUS_
                        if (_gestorHF.GlobalStatus() != HFStatusCodes.DISC)
#endif
                            _gestorHF.DesasignarTx(ask.HostId, ref rdFr);
#if _HF_GLOBAL_STATUS_
                        SendStatusHF();
#endif
                    }
                }
                else
                {
                    LogError<RdService>(String.Format("ProcessRxChangeAsk: No encuentra Frecuencia {0}", ask.Frecuency),
                        U5kiIncidencias.U5kiIncidencia.IGNORE);
                }
            }
        }
        /// <summary>
        /// Procesa las peticiones de Asignacion / Desasignacion en TX de una Frecuencia....
        /// </summary>
        private void ProcessTxChangeAsk(string from, FrTxChangeAsk ask)
        {
            if (_Master)
            {
                LogDebug<RdService>(String.Format("ProcessTxChangeAsk: Frecuencia {0} Host {1}", ask.Frecuency, ask.HostId),
                    U5kiIncidencias.U5kiIncidencia.IGNORE);

                Func<string, FrTxChangeAsk, AskingThread> TxChange = ProcessTxChange;
                TxChange.BeginInvoke(from, ask, TxChangeProcessed, TxChange);

            }
        }

        /// <summary>
        /// Comienzo del tratamiento de la peticion de asignacion TX
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        /// <returns></returns>
        private AskingThread ProcessTxChange(string from, FrTxChangeAsk ask)
        {
            RdFrecuency rdFr;
            AskingThread ret;

            ret.result = -1;
            ret.from = from;
            ret.ask = ask;

            // 20161124. AGL. A veces este parámetro viene a NULL ????
            if (ask.Frecuency == null)
                return ret;
            if (!_Master)
                return ret;

            if (Frecuencies.TryGetValue(ask.Frecuency.ToUpper(), out rdFr))
            {
                //** AGL.  Asignacion HF 
                int prosigue = 0;
#if _HF_GLOBAL_STATUS_
                if (rdFr.TipoDeFrecuencia != "HF" || _gestorHF.GlobalStatus() != HFStatusCodes.DISC)
#endif
                    prosigue = (ask.Tx == true) ? _gestorHF.AsignarTx(ask.HostId, ref rdFr, from) : _gestorHF.DesasignarTx(ask.HostId, ref rdFr);
#if _HF_GLOBAL_STATUS_
                // JCAM: 21/11/2014
                // En este punto, si esta asignación en Tx corresponde con el último equipo HF disponible, 
                // SendStatusHF() va a enviar NODISP, con lo que al puesto que intentó la 
                // asignación en Tx le va a llegar que no hay equipos HF disponibles antes que el resultado de
                // la asignación. Por eso, reiniciamos el timer de HFStatus
                _TimerHfStatus.Enabled = true;
                _TimerHfStatus.Start();
#endif
                ret.result = prosigue;
            }
            else
            {
                LogError<RdService>(String.Format("ProcessTxChange: No encuentra Frecuencia {0}", ask.Frecuency),
                    U5kiIncidencias.U5kiIncidencia.IGNORE);
            }

            return ret;
        }

        /// <summary>
        /// Final del tratamiento de la peticion de asignacion TX
        /// </summary>
        /// <param name="cookie"></param>
        private void TxChangeProcessed(IAsyncResult cookie)
        {
            string rdFrData = "";
            if (!_Master)
                return;

            try
            {
                RdFrecuency rdFr;
                var target = (Func<string, FrTxChangeAsk, AskingThread>)cookie.AsyncState;
                int prosigue;

                AskingThread resultProcess = target.EndInvoke(cookie);

                prosigue = resultProcess.result;

                if (prosigue != -1)
                {
                    if (Frecuencies.TryGetValue(resultProcess.ask.Frecuency.ToUpper(), out rdFr))
                    {
                        rdFrData = rdFr.Frecuency;

                        if (prosigue == (int)EquipoHFStatus.stdAsignado)  //* 3 => Asignado 
                        {
                            rdFr.SetTx(resultProcess.ask.HostId, resultProcess.ask.Tx,
                                resultProcess.ask.CheckAlreadyAssigned, (CORESIP_PttType)resultProcess.ask.PttType,
                                resultProcess.from);
                        }
                        else
                        {
                            if (prosigue == (int)EquipoHFStatus.stdDisponible)
                                rdFr.SetTx(resultProcess.ask.HostId, resultProcess.ask.Tx, resultProcess.ask.CheckAlreadyAssigned, (CORESIP_PttType)resultProcess.ask.PttType, resultProcess.from);
                            else if (prosigue == (int)EquipoHFStatus.stdError || prosigue == (int)EquipoHFStatus.stdNoinfo)
                                rdFr.SetTx(resultProcess.ask.HostId, false, resultProcess.ask.CheckAlreadyAssigned, (CORESIP_PttType)resultProcess.ask.PttType, resultProcess.from);

                            //** Comunicar al operador. 
                            if (resultProcess.from != null)
                            {
                                // 20171005. AGL. Se ha implementado este estado para aislar las peticiones consecutivas....                                
                                if (rdFr.TipoDeFrecuencia == "HF" && prosigue != (int)RdGestorHF.EquipoHFStd.stdOperationInProgress)
                                    RdRegistry.RespondToFrHfTxChange(resultProcess.from, rdFr.Frecuency, prosigue);
                                else
                                {
                                    // 20180108. AGL. Las Operation in progress no se contestan...
                                    // RdRegistry.RespondToFrTxChange(resultProcess.from, rdFr.Frecuency, false);
                                }
                            }
                            else 
                            {
                                string msg = String.Format("TxChangeProcessed: resultProcess NULL");
                                LogError<RdService>(msg, U5kiIncidencias.U5kiIncidencia.IGNORE, "RdService", msg);
                            }
                        }
                    }
                    else
                    {
                        LogError<RdService>(String.Format("TxChangeProcessed: No encuentra Frecuencia {0}", resultProcess.ask.Frecuency),
                            U5kiIncidencias.U5kiIncidencia.IGNORE);
                    }
                }
            }
            catch (Exception x)
            {
                //LogException<RdService>(String.Format("FR: {0}. Error al Asignar TX...", rdFrData), x);
                ExceptionManage<RdService>("TxChangeProcessed", x, "TxChangeProcessed Frecuencia: " + rdFrData + ". Exception: " + x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        private void ProcessPttChangeAsk(string from, PttChangeAsk ask)
        {
            if (_Master)
            {
                List<int> sndRxPorts = null;

                if (ask.Src != PttSource.NoPtt)
                {
                    int sndRxPort;
                    if (!_SndRxPorts.TryGetValue(ask.HostId, out sndRxPort))
                    {
                        sndRxPort = SipAgent.CreateSndRxPort(ask.HostId);
                        LogTrace<RdService>("ProcessPttChangeAsk "+ sndRxPort.ToString() +", " + ask.Src);
                        _SndRxPorts[ask.HostId] = sndRxPort;
                    }

                    sndRxPorts = new List<int>(1)
                    {
                        sndRxPort
                    };
                }

                foreach (RdFrecuency rdFr in Frecuencies.Values)
                {
                    rdFr.ReceivePtt(ask.HostId, ask.Src, sndRxPorts);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        private void ProcessRtxGroupChangeAsk(string from, RtxGroupChangeAsk ask)
        {
            if (!_Master)
                return;

            List<RdFrecuency> rtxGroupRdFr = new List<RdFrecuency>();

            for (int i = 0, to = ask.FrIds.Count; i < to; i++)
            {
                RdFrecuency rdFr;
                if (ask.Changes[i] != RtxGroupChangeAsk.ChangeType.Delete
                    && Frecuencies.TryGetValue(ask.FrIds[i].ToUpper(), out rdFr))
                    rtxGroupRdFr.Add(rdFr);
            }

            RdFrecuency.ChangeRtxGroup(ask.HostId, ask.GroupId, rtxGroupRdFr);
        }
        /// <summary>
        /// AGL. Gestión de las peticiones SELCAL...
        /// </summary>
        private void ProcessSelcalPrepare(string from, SelcalPrepareMsg msg)
        {
            if (!_Master)
                return;

            foreach (RdFrecuency rdFr in Frecuencies.Values)
            {
                if (rdFr.TipoDeFrecuencia == "HF" && rdFr.FindHost(msg.HostId))
                {

                    //if (_gestorHF.PrepareSelcal(rdFr, msg.HostId, msg.OnOff) == true)
                    //{
                    //    LogInfo<RdService>("SelcalPrepare OK", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", "SelcalPrepare OK");
                    //    /** Respuesta OK..*/
                    //    RdRegistry.RespondToPrepareSelcal(from, rdFr.Frecuency, true, msg.Code);
                    //}
                    //else
                    //{
                    //    RdRegistry.RespondToPrepareSelcal(from, rdFr.Frecuency, false, "Error");
                    //}
                    Task.Factory.StartNew(() =>
                    {
                        bool result = _gestorHF.PrepareSelcal(rdFr, msg.HostId, msg.OnOff, msg.Code);
                        RdGestorHF.HFHelper.RespondToPrepareSelcal(from, rdFr.Frecuency, result, result ? msg.Code : "Error");
                    });
                    return;
                }
            }

            /** Respuesta NO-OK..*/
            LogError<RdService>("SelcalPrepare NO OK", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
            RdRegistry.RespondToPrepareSelcal(from, "", false, "");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="tones"></param>
        private void ProcessSelcalSendTones(string from, SelcalPrepareMsg tones)
        {
            if (_Master)
            {
                foreach (RdFrecuency rdFr in Frecuencies.Values)
                {
                    if (rdFr.TipoDeFrecuencia == "HF" && rdFr.FindHost(tones.HostId))
                    {
                        if (rdFr.PttSrc != string.Empty)
                        {
                            //Las frecuencias HF son todas simples
                            foreach (RdResource rs in rdFr.RdRs.Values)
                            {
                                if (rs.Connected && (rs.Type == RdRsType.Tx || rs.Type == RdRsType.Tx))
                                {
                                    LogInfo<RdService>(String.Format("SelcalSendTones {0}", tones.Code));
                                    SipAgent.SendInfo(rs.SipCallId, tones.Code);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="msg"></param>
        private void ProcessChangingSite(string from, ChangeSiteMsg msg)
        {
            if (_Master)
            {
                int numFrecuencias = 0;
                foreach (RdFrecuency rdFr in Frecuencies.Values)
                {
                    if (rdFr.Frecuency == msg.Frequency && rdFr.FindHost(msg.HostId))
                    {
                        LogInfo<RdService>(String.Format("ProcessChangingSite {0}: {1}: {2}", msg.HostId, msg.Frequency, msg.Alias));
                        if (rdFr.ChangeSite(msg.HostId, msg.Frequency, msg.Alias))
                            numFrecuencias++;
                    }
                }

                RdRegistry.RespondToChangingSite(from, msg.Frequency, msg.Alias, numFrecuencias);
            }
        }

        #endregion

        #region SIP
        /// <summary>
        /// 
        /// </summary>
        private void InitializeSip()
        {
            /** AGL. Para que no se acumulenn... */
            SipAgent.KaTimeout -= OnKaTimeout;
            SipAgent.RdInfo -= OnRdInfo;
            SipAgent.CallState -= OnCallState;
            /** 20171130. Para el ping a los TX HF por Options... */
            SipAgent.OptionsReceive -= OnOptionsResponse;

            SipAgent.KaTimeout += OnKaTimeout;
            SipAgent.RdInfo += OnRdInfo;
            SipAgent.CallState += OnCallState;
            /** 20171130. Para el ping a los TX HF por Options... */
            SipAgent.OptionsReceive += OnOptionsResponse;

            /** 20180202. AGL. Maximas sesiones radio. TODO: Poner configurable en .config */
            /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
            //SipAgent.Init(Settings.Default.SipUser, _SipIp, Settings.Default.SipPort, 128);
            //SipAgent.ReceiveFromRemote(_SipIp, Settings.Default.ListenIp, Settings.Default.ListenPort);
            //SipAgent.Start();
            /****/

            /** 20170126. AGL. Control de Eventos de Conexion / Desconexion */
            _sessions_sip_control.Init();
        }

        /** 20180209. Limpiar las llamdas y puertos ya que no se hace END... */
        private void FinalizeSip()
        {
            /** Desconecto los puertos */
            foreach (int port in _SndRxPorts.Values)
            {
                try
                {
                    SipAgent.DestroySndRxPort(port);
                }
                finally { }
            }
            _SndRxPorts.Clear();

            /** Desconecto las sesiones */
            foreach (RdFrecuency fr in Frecuencies.Values)
            {
                try
                {
                    fr.Dispose();
                }
                catch (Exception x)
                {
                    LogException<RdService>(String.Format("Disposing FR [{0}]", fr.Frecuency), x, false);
                }
                finally { }
            }

            /** Desconecto el agente */
            SipAgent.KaTimeout -= OnKaTimeout;
            SipAgent.RdInfo -= OnRdInfo;
            SipAgent.CallState -= OnCallState;

            /** 20171130. Para el ping a los TX HF por Options... */
            SipAgent.OptionsReceive -= OnOptionsResponse;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        private void OnKaTimeout(int call)
        {
            if (!_Master)
                return;

            _EventQueue.Enqueue(call.ToString() + "_KaTimeout", delegate()
            {
                foreach (RdFrecuency rdFr in Frecuencies.Values)
                {
                    /** 20170126. AGL. Identifico el Recurso para poder generar el Historico. */
                    RdResource rdRes;
                    if (rdFr.HandleKaTimeout(call, out rdRes))
                    {
                        /** 20170126. AGL. Generar Historico KEEP-ALIVE TIMEOUT */
                        LogInfo<RdService>(String.Format("KeepAlive Timeout. Frecuencia {0}, Equipo {1}", rdFr.Frecuency, rdRes.ID),
                            U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR,
                            rdFr.Frecuency,
                            "Recurso: " + rdRes.ID + " KeepAlive Timeout.");
                        break;
                    }
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="info"></param>
        private void OnRdInfo(int call, CORESIP_RdInfo info)
        {
            _EventQueue.Enqueue(call.ToString() + "_RdInfo", delegate()
            {
                if (_Master)
                {
                    foreach (RdFrecuency rdFr in Frecuencies.Values)
                    {
                        rdFr.SupervisionPortadora = ConfirmaSupervisionPortadora(rdFr.PttSrc, rdFr.Frecuency);
                        if (rdFr.HandleRdInfo(call, info))
                        {
                            break;
                        }
                    }
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentSrcPtt"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private bool ConfirmaSupervisionPortadora(string currentSrcPtt, string frequency)
        {
            KeyValuePair<string, string> clave = new KeyValuePair<string, string>(currentSrcPtt, frequency);
            if (_UsrFreq.ContainsKey(clave))
            {
                return _UsrFreq[clave].SupervisionPortadora;
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="info"></param>
        /// <param name="stateInfo"></param>
        private void OnCallState(int call, CORESIP_CallInfo info, CORESIP_CallStateInfo stateInfo)
        {
            if ((stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED) ||
                (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED))
            {
                _EventQueue.Enqueue(call.ToString() + "_State", delegate()
                {
                    try
                    {
                        if (!_Master)
                            return;

                        RdFrecuency fr = null;
                        string keyFr = string.Empty;

                        // Comprobar el estado de los recursos 
                        // que dan servicio a los equipos de HF
                        _gestorHF.ActualizaEquipo(call, stateInfo);

                        /** */
                        bool eventZombie = true;
                        foreach (KeyValuePair<string, RdFrecuency> rdFr in Frecuencies)
                        {
                            /** 20170126. AGL. Identifico el Recurso para poder generar el Historico. */
                            IRdResource rdRes;
                            if (rdFr.Value.HandleChangeInCallState(call, stateInfo, out rdRes))
                            {
                                if (rdFr.Value.TipoDeFrecuencia == "HF" && stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED)
                                {
                                    fr = rdFr.Value;
                                    keyFr = rdFr.Key;
                                }

                                /** 20170126. AGL. Generar Historico Apertura / Cierre Session Radio */
                                if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED)
                                {
                                    /** 20170126. AGL. Control de Eventos de Conexion / Desconexion */
                                    if (_sessions_sip_control.Event(rdRes.ID, CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED) == true)
                                    {
                                        LogWarn<RdService>(String.Format("Desconexion SIP. Causa: {0}. Frecuencia {1}, Equipo {2}", stateInfo.LastCode, rdFr.Value.Frecuency, rdRes.ID),
                                            U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR,
                                            rdFr.Value.Frecuency,
                                            CTranslate.translateResource("Recurso: " + rdRes.ID +". Desconexion SIP. Causa: "+ stateInfo.LastCode.ToString() + "." ));
                                    }
                                    /**********************************/
                                }
                                else
                                {
                                    /** 20170126. AGL. Control de Eventos de Conexion / Desconexion */
                                    if (_sessions_sip_control.Event(rdRes.ID, CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED) == true)
                                    {
                                        LogInfo<RdService>(String.Format("Conexion SIP. Frecuencia {0}, Equipo {1}", rdFr.Value.Frecuency, rdRes.ID),
                                            U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                                            rdFr.Value.Frecuency,
                                            CTranslate.translateResource("Recurso:"+ rdRes.ID +". Conexion SIP."));
                                    }
                                    /**********************************/
                                }
                                /****************************/
                                LogTrace<RdService>(String.Format("Sesion SIP Radio <{0};{1}>: {2} con {3}", rdFr.Value.Frecuency, rdRes.ID,
                                    stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED ? "Conectada" : "Desconectada", rdRes.Uri1
                                    ));
                                eventZombie = false;
                                break;
                            }
                        }

                        /** */
                        if (eventZombie == true)
                        {
                            /** AGL. */
                            LogDebug<RdService>(String.Format("OnCallState: Recibido Evento de Sesion ZOMBI {0:X} estado {1}", call, stateInfo.State));
                        }

                        if (fr != null)
                        {
#if _HF_GLOBAL_STATUS_
                            if (_gestorHF.GlobalStatus() != HFStatusCodes.DISC)
#endif
                                _gestorHF.DesasignarTxHf(ref fr);
#if _HF_GLOBAL_STATUS_
                            SendStatusHF();
#endif
                            Frecuencies[keyFr] = fr;
                            return;
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<RdService>("OnCallState", x,false);
                    }
                });
            }
        }

        #endregion

#if _HF_RTPOLL_
        /// <summary>
        /// 
        /// </summary>
        private bool _SupervisaHf = false;
        private void SupervisaHf()
        {
            int cEquipo = 0;
            while (_SupervisaHf == true)
            {
                int nEquipos = _gestorHF.Equipos.Count;
                cEquipo = cEquipo == nEquipos ? 0 : cEquipo;
                if (_Master && nEquipos > 0)
                {
                    _gestorHF.Equipos[cEquipo++].GetEstado();
                }
            }
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wg67"></param>
        /// <param name="wg67Info"></param>
        /// <param name="userData"></param>
        private void OnWG67Notify(IntPtr wg67, CORESIP_WG67Info wg67Info, IntPtr userData)
        {
            _EventQueue.Enqueue(wg67.ToString() + "_WG67", delegate()
            {
#if _LOCKING_
                    lock (_lock)
#endif
                {
                    if (_Master)
                    {
                        foreach (RdFrecuency rdFr in Frecuencies.Values)
                        {
                            WG67Info info = new WG67Info();

                            info.DstUri = wg67Info.DstUri;
                            if (wg67Info.SubscriptionTerminated != 0)
                            {
                                info.SubscriptionTerminated = true;
                                info.LastReason = wg67Info.LastReason;
                            }
                            else
                            {
                                //if (rdFr.HandleWG67(wg67, wg67Info))
                                //{
                                //    break;
                                //}

                                //info.SubscribersCount = wg67Info.SubscribersCount;
                                //info.Subscribers = new WG67Info.SubscriberInfo[wg67Info.SubscribersCount];
                                //for (uint i = 0; i < wg67Info.SubscribersCount; i++)
                                //{
                                //    info.Subscribers[i].PttId = wg67Info.Subscribers[i].PttId;
                                //    info.Subscribers[i].SubsUri = wg67Info.Subscribers[i].SubsUri;
                                //}
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 20171130. Para el ping a los TX HF por Options...
        /// </summary>
        /// <param name="from"></param>
        /// <param name="code"></param>
        /// <param name="supported"></param>
        /// <param name="allowed"></param>
        private void OnOptionsResponse(string from, string callid, int code, string supported, string allowed)
        {
            if (!_Master)
                return;

            if (_gestorHF != null)
            {
                _gestorHF.OptionsResponseReceived(from, code, supported, allowed);
            }
        }

        /// <summary>
        /// 20170126. Control de Eventos de Conexion / Desconexion
        /// </summary>
        SessionSipLogControl _sessions_sip_control = new SessionSipLogControl();
        class SessionSipLogControl
        {
            Dictionary<string, CORESIP_CallState> _sessions_states = new Dictionary<string, CORESIP_CallState>();
            public void Init()
            {
                _sessions_states.Clear();
            }
            public bool Event(string idres, CORESIP_CallState current)
            {
                if (_sessions_states.ContainsKey(idres))
                {
                    bool retorno = _sessions_states[idres] != current;
                    _sessions_states[idres] = current;
                    return retorno;
                }
                _sessions_states[idres] = current;
                return true;
            }
        }


        #region Pruebas...
        /// <summary>
        /// 
        /// </summary>
        public void TestWav2Remote()
        {
            System.Threading.Thread.Sleep(2000);
            string filename = "";
            SelcalGen selcal = new SelcalGen() { SampleRate = 8000, Gain = 0.7 };
            if (selcal.Generate("A", "S", "B", "K", out filename) == true)
            {
                SipAgent.Wav2Remote(filename, "test", "192.168.1.255", 22222);
            }
            else
            {
                // ???
                // SipAgent.SetPresenceSubscriptionCallBack(OnEventSubscription);
            }
        }

        private void OnEventSubscription(string dst_uri, int subscription_status, int presence_status)
        {
        }

        #endregion

        #region NMManager Handlers

        /// <summary>
        /// Reserva la frecuencia para un equipo.
        /// </summary>
        /// <returns>
        /// True si ha reservado la frecuencia para el equipo.
        /// </returns>
        internal bool ReserveFrecuency(BaseGear gear)
        {
            // IMPORTANTE: Hay que definir el sistema de reserva de frecuencias, para evitar colisiones entre hilos asincronos. 
            // Por ahora devuelve true para no afectar al codigo del N&M.
            // 20161118. AGL. Como, potencialmente puede tocar variables del motor de radio, debe encolarse la acción.
            _EventQueue.Enqueue("ReserveFrecuency", delegate()
            {
            });
            return true;
        }
        /// <summary>
        /// Saca de la reserva a una frecuencia.
        /// </summary>
        /// <returns>
        /// True sacada correctamente.
        /// </returns>
        internal bool UnReserveFrecuency(BaseGear gear)
        {
            // IMPORTANTE: Hay que definir el sistema de reserva de frecuencias, para evitar colisiones entre hilos asincronos. 
            // Por ahora devuelve true para no afectar al codigo del N&M.
            // 20161118. AGL. Como, potencialmente puede tocar variables del motor de radio, debe encolarse la acción.
            _EventQueue.Enqueue("UnReserveFrecuency", delegate()
            {
            });
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        internal void OnNodeAllocate(BaseGear gear)
        {
#if DEBUG
            if (Globals.Test.IsTestRunning)
            {
                LogTrace<RdService>("OnNodeAllocate: " + gear.Id);
                return;
            }
#endif
            lock (Frecuencies)
            {
#if _MN_ENQUEUE_NODES_
#else
                RdFrecuency frecuency = Frecuencies.Values.Where(e => e.Frecuency == gear.Frecuency).FirstOrDefault();
                if (null == frecuency)
                {
                    //LogInfo<RdService>("Frecuencia NO encontrada asignado equipo. " + gear.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_ALLOCATE_ERROR);
                    LogFatal<RdService>("Frecuencia no Encontrada y asignada a Equipo" + gear.ToString(),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                        gear.Frecuency, "Equipo: " + gear.Id + ". Frecuencia de Equipo no configurada.");
                    return;
                }
#endif
                //JOI FREC_DES
                String uriGearToReplace = "--";
                //JOI FREC_DES FIN

                // Obtener el Id del equipo que espera el sistema (en concreto el HMI).
                String gearId = gear.FrecuencyKey;
                if (null == gearId)
                    gearId = gear.Id; // Esto se añade por si la frecuenciaKey no ha sido configurada en el servidor.
                if (gear.IsSlave)
                {
                    if (null == gear.ReplaceTo)
                    {
                        if (gear.Status != GearStatus.Forbidden)
                        {
                            LogError<RdService>("La referencia del equipo al que reemplaza un esclavo, no puede ser vacio. Error no esperado.",
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                                gear.Frecuency, 
                                CTranslate.translateResource("Equipo: "+ gear.Id + ". La referencia del equipo al que reemplaza un esclavo, no puede ser vacio. Error no esperado."));
                        }
                    }
                    else
                    {
                        gearId = gear.ReplaceTo.FrecuencyKey;
                        if (null == gearId)
                            gearId = gear.ReplaceTo.Id; // Esto se añade por si la frecuenciaKey no ha sido configurada en el servidor.
                        //JOI FREC_DES
                        uriGearToReplace = gear.ReplaceTo.SipUri;
                        //JOI FREC_DES FIN
                    }
                }

#if _MN_ENQUEUE_NODES_
                if (gear.Status != GearStatus.Forbidden)
                    //EnqueueFrecAllocate(gear.Frecuency, gearId, gear.ResourceType, gear.SipUri, (gear.Status != GearStatus.Fail), gear.IsMaster);
                    //JOI FREC_DES
                    EnqueueFrecAllocate(gear.Frecuency, gearId, gear.ResourceType, gear.SipUri, (gear.Status != GearStatus.Fail), gear.IsMaster, gear.IdEmplazamiento, uriGearToReplace);
                //JOI FREC_DES FIN
#else

#if _MN_SET_RESET_RESOURCES_V0
                // 20161116. AGL. Desasigno por si las moscas...
                // Esto abría que revisarlo para los 1+1 en M+N...
                if (!new FrecuencyHelper(Frecuencies).ResourceFree(frecuency, gear.ResourceType, gear.SipUri)) { return; }
                //if (!new FrecuencyHelper(Frecuencies).ResourceFree(frecuency, gear.ResourceType)) { }
                // Asignacion de recurso.
                if (!new FrecuencyHelper(Frecuencies).ResourceSet(frecuency, gearId, gear.SipUri, gear.ResourceType))
#else
                    if (!new FrecuencyHelper(Frecuencies).ResourceSet(frecuency, gearId, gear.SipUri, gear.ResourceType))
#endif
                {
                    //LogWarn<RdService>("Frecuencia/Recurso no configurado." + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_ALLOCATE_ERROR);
                    LogFatal<RdService>("Frecuencia/Recurso no configurado." + gear.ToString(),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                        gear.Frecuency, "Equipo: " + gear.Id + ". Frecuencia/Recurso no configurado.");
                }
                else
                {
                    //LogInfo<RdService>("Frecuencia Asignada: " + gear.Frecuency, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_ALLOCATE_OK);
                    // 20160922. AGL. Para evitar que las asignaciones por defecto en ALARMA de Frecuencia generen historico de asignacion...
                    if (gear.Status != GearStatus.Fail)
                    {
                        LogInfo<RdService>("Frecuencia Asignada: " + gear.Frecuency,
                            gear.IsMaster ? (gear.IsReceptor ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONMAIN : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONMAIN) :
                            (gear.IsReceptor ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONRSVA : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONRSVA),
                            gear.Frecuency, "Equipo: " + gear.Id);
                    }
                }
#endif
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        internal void OnNodeDeallocate(BaseGear gear)
        {
#if DEBUG
            if (Globals.Test.IsTestRunning)
            {
                LogTrace<RdService>("OnNodeDeallocate: " + gear.Id);
                return;
            }
#endif
            lock (Frecuencies)
            {
#if _MN_ENQUEUE_NODES_
                // JOI FREC_DES
                //EnqueueFrecDeallocate(gear.LastFrecuency, gear.ResourceType, gear.SipUri, gear.Status);
                //EnqueueFrecDeallocate(gear.LastFrecuency, gear.ResourceType, gear.SipUri, gear.Status, gear.IdEmplazamiento);
                EnqueueFrecDeallocate(gear.LastFrecuency, gear.ResourceType, gear.SipUri, gear.Status, gear.IdEmplazamiento, gear.IsSlave);
                // JOI FREC_DES FIN
#else
                // Optener la frecuancia.
                RdFrecuency frecuency = Frecuencies.Values.Where(e => e.Frecuency == gear.LastFrecuency).FirstOrDefault();
                if (null == frecuency)
                {
                    //LogInfo<RdService>("Frecuencia NO encontrada liberando equipo. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR);
                    LogFatal<RdService>("Frecuencia NO encontrada liberando equipo. " + gear.ToString(),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                        gear.Id, "Frecuencia NO encontrada liberando equipo: " + "Frecuencia: " + gear.LastFrecuency);
                    return;
                }

                // Dasignacion de recurso.
                if (!new FrecuencyHelper(Frecuencies).ResourceFree(frecuency, gear.SipUri, gear.ResourceType))
                {
                    //LogWarn<RdService>("Frecuencia/Recurso no liberado. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR);
                    LogFatal<RdService>("Frecuencia/Recurso no liberado. " + gear.ToString(),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                        gear.LastFrecuency, "Frecuencia/Recurso no liberado. " + ", Equipo: " + gear.Id);
                }
                else
                {
                    // LogInfo<RdService>("Frecuencia Liberada: " + gear.LastFrecuency, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_OK);
                    LogInfo<RdService>("Frecuencia Liberada: " + gear.LastFrecuency);
                }
#endif
            }
        }

        /// <summary>
        /// 20161124. AGL. Evento por frecuencia, para que pueda ser encolado....
        /// </summary>
        /// <param name="frec"></param>
        /// <param name="tipo"></param>
        /// <param name="uri"></param>
        //internal void EnqueueFrecAllocate(string frec, string gearId, RdRsType tipo, string uri, bool generaLogAsignacion, bool isMaster)
        //JOI FREC_DES
        internal void EnqueueFrecAllocate(string frec, string gearId, RdRsType tipo, string uri, bool generaLogAsignacion, bool isMaster, string idEmplazamiento, string uriGearToReplace)
        //JOI FREC_DES FIN 
        {
            _EventQueue.Enqueue("FrecAllocate " + frec, delegate()
            {
                try
                {
                    /** Obtener la Frecuencia */
                    RdFrecuency frecuency = Frecuencies.Values.Where(e => e.Frecuency == frec).FirstOrDefault();
                    if (null == frecuency)
                    {
                        LogInfo<RdService>("Frecuencia no Encontrada y asignada a Equipo " + gearId,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                            frec, CTranslate.translateResource("Equipo: "+ gearId + ". Frecuencia de equipo no configurada." ));
                        return;
                    }
                    // JOI 201709 NEWRDRP INI
                    RdResource.NewRdResourceParams newconfparams = null;
                    newconfparams = RdRParamGet(gearId);
                    if (null == newconfparams)
                    {
                        LogInfo<RdService>("Equipo Master no encontrado para obtención parámetros " + gearId,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                            frec, CTranslate.translateResource("Equipo: " + gearId + ". Equipo no configurado."));
                        return;
                    }
                    // JOI 201709 NEWRDRP FIN
#if _MN_SET_RESET_RESOURCES_V0
                    // 20161116. AGL. Desasigno por si las moscas...
                    // Esto habría que revisarlo para los 1+1 en M+N...
                    if (new FrecuencyHelper(Frecuencies).RsIsInFrec(frecuency, tipo, uri))
                    {
                        return;
                    }

                    RdFrecuency.NewRdFrequencyParams confParams = frecuency.GetParam;
                    //JOI FREC_DES
                    if (uriGearToReplace != "--")
                    {
                        string _uriGearToReplace = new FrecuencyHelper(Frecuencies).ResourceIdGet(uriGearToReplace, tipo);
                        if (!new FrecuencyHelper(Frecuencies).ResourceRemoveEmplaz(frecuency, _uriGearToReplace))
                        {

                            // JOI OJO
                            //Console.WriteLine("intento liberar por emplazamiento. frequency: {0} , uri: {1} ", frecuency, _uriGearToReplace);
                            //return;                         
                        }
                    }
                    //JOI FREC_DES FIN

                    // Asignacion de recurso.

                    if (!new FrecuencyHelper(Frecuencies).ResourceSet(frecuency, gearId, uri, tipo, idEmplazamiento, confParams, newconfparams, isMaster)) //#3603                  

#else
                    if (!new FrecuencyHelper(Frecuencies).ResourceSet(frecuency, gearId, gear.SipUri, gear.ResourceType))
#endif
                    {
                        LogFatal<RdService>("Frecuencia/Recurso no configurado " + gearId,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                            frec, CTranslate.translateResource("Equipo: "+ gearId + ". Frecuencia/Recurso no configurado." ));
                    }
                    else
                    {
                        // 20160922. AGL. Para evitar que las asignaciones por defecto en ALARMA de Frecuencia generen historico de asignacion...
                        if (generaLogAsignacion)
                        {


                            /** 20170529. JOI. **/
                            if (isMaster)
                            {
                                LogInfo<RdService>("Frecuencia Asignada: " + frec,
                                   ((tipo == RdRsType.Rx) ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONMAIN : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONMAIN),
                                    frec, CTranslate.translateResource("Equipo: " + gearId));
                            }
                            else
                            {
                                int _origendospuntos = uri.IndexOf(":");
                                int _finarroba = uri.IndexOf("@");
                                if (_origendospuntos > 0 && _origendospuntos < _finarroba)
                                {
                                    string sequiporeserva = uri.Substring(_origendospuntos + 1, (--_finarroba - _origendospuntos));
                                    if (sequiporeserva != "")
                                    {
                                        LogInfo<RdService>("Frecuencia Asignada: " + frec,
                                            ((tipo == RdRsType.Rx) ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONRSVA : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONRSVA),
                                            //frec, CTranslate.translateResource("Equipo: {0} sustituido por: {1}", gearId, sequiporeserva));
                                            // 20180313 JOI
                                             frec, CTranslate.translateResource("Equipo: " + gearId + " sustituido por: " + sequiporeserva));
                                    }
                                }
                            }


                            /** 20170406. AGL. */

                            /* LogInfo<RdService>("Frecuencia Asignada: " + frec,
                                 isMaster ? ((tipo == RdRsType.Rx) ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONMAIN : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONMAIN) :
                                 ((tipo == RdRsType.Rx) ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONRSVA : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONRSVA),
                                 //frec, "Equipo: " + gearId);
                                 frec, "Equipo: " + uriGearToReplace + "Sustituido por :" + uri);*/
                        }
                    }
          
                }
                catch (Exception x)
                {
                    LogError<RdService>(String.Format("FrecAllocate Exception F={0}, E={1}, U=<{2}>: {3}", frec, gearId, uri, x.Message),
                        U5kiIncidencias.U5kiIncidencia.IGNORE);
                }
                Thread.Sleep(250);
            });
        }

        /// <summary>
        /// 20161124. AGL. Evento por frecuencia, para que pueda ser encolado....
        /// </summary>
        /// <param name="frec"></param>
        /// <param name="tipo"></param>
        //  internal void EnqueueFrecDeallocate(string frec, RdRsType tipo, string uri, GearStatus Status)
        //JOI FREC_DES
        //internal void EnqueueFrecDeallocate(string frec, RdRsType tipo, string uri, GearStatus Status, string idEmplazamiento)
        internal void EnqueueFrecDeallocate(string frec, RdRsType tipo, string uri, GearStatus Status, string idEmplazamiento, bool IsSlave)
        //JOI FREC_DES FIN
        {
            _EventQueue.Enqueue("FrecDeallocate: " + frec, delegate()
            {
                try
                {
                    // Obtener la frecuancia.
                    RdFrecuency frecuency = Frecuencies.Values.Where(e => e.Frecuency == frec).FirstOrDefault();
                    if (null == frecuency)
                    {
                        if (Status != GearStatus.Initial)
                        {
                            //LogInfo<RdService>("Frecuencia NO encontrada liberando equipo. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR);
                            LogInfo<RdService>("Frecuencia NO encontrada liberando equipo. ",
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                                frec, "URI: " + uri);
                        }
                        return;
                    }

                    // Desasignacion de recurso.
                    if (!new FrecuencyHelper(Frecuencies).ResourceFree(frecuency, uri, tipo))
                    {
                        if (!IsSlave && Status != GearStatus.Initial)
                        {
                            //LogWarn<RdService>("Frecuencia/Recurso no liberado. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR);
                            LogError<RdService>("Frecuencia/Recurso no liberado. ",
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,
                                frec, "URI: " + uri);
                        }
                    }
                    else
                    {
                        // LogInfo<RdService>("Frecuencia Liberada: " + gear.LastFrecuency, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_OK);
                        LogInfo<RdService>("Frecuencia Liberada: " + frec);
                    }
                }
                catch (Exception x)
                {
                    LogError<RdService>(String.Format("FrecDeallocate Exception F={0}, U=<{1}>: {2}", frec, uri, x.Message),
                        U5kiIncidencias.U5kiIncidencia.IGNORE);
                }
                Thread.Sleep(250); 
            });
        }

        #endregion

#if DEBUG

        void MNHistoricosTest()
        {
            //string Id = "EQ-TEST";
            //string Frec = "555.000";
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_ERROR_GENERIC, 
            //    "U5KI_NBX_NM_ERROR_GENERIC: Texto del Historico");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
            //    "Equipo: " + Id + ", 'Channel Spacing Incorrecto'. Se asigna el valor por defecto (25).");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ALLOCATE_ERROR,
            //    String.Format("Equipo {1}: Excepcion No Esperada. Ver LOG {0}", DateTime.Now.ToString(), Id));
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ALLOCATE_OK,
            //    "Equipo: " + Id + ", Frecuencia: " + Frec);
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DEALLOCATE_ERROR, 
            //    "U5KI_NBX_NM_ERROR_GENERIC: Incidencia No utilizada...");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DEALLOCATE_OK,
            //    "Equipo: " + Id);
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_LOCAL_MODE_ON, 
            //    Id);
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_SOCKET_ERROR,
            //    "Equipo: " + Id + ", SOCKET EXCEPTION (Posible modo Interactivo)");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_ALLOCATE_ERROR,
            //    "Frecuencia: " + Frec + ", Equipo: " + Id + ". Texto del Error encontrado.");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_ALLOCATE_OK,
            //    "Frecuencia: " + Frec + ", Equipo: " + Id);
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR,
            //    "Texto Error Encontrada: " + "Frecuencia: " + Frec + ", Equipo: " + Id);
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_DEALLOCATE_OK,
            //    "Frecuencia: " + Frec, ", Equipo: " + Id);
            //LogError<RdService>("Texto de ERROR....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR);
            //LogError<RdService>("Equipo " + Id + " assignado a frecuencia " + Frec + ".", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_GEAR_ASSING);
            //LogError<RdService>("Equipo " + Id + " liberado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_GEAR_UNASSING);
            //LogError<RdService>("Equipo " + Id + " no encontrado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_GEAR_TOOGLE);
            //LogError<RdService>("Gestor NM reiniciado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_SERVICE_RESTART, "");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_SERVICE_TICK_CHANGED,
            //    "Tiempo= " + "FFFF");
            //LogError<RdService>("Servicio Iniciado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_SERVICE_START,"");
            //LogError<RdService>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_SERVICE_STOP,"");
            //LogError<RdService>("Texto de LOG....", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_SERVICE_NEW_CONFIGURATION,
            //    "El servicio no está activo...");

        }

        Random random = new Random();
        void TestMNDisabledPublish()
        {
            int n = random.Next(0, 4);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            var lista = new List<string>();
            for (int i = 0; i < n; i++)
                lista.Add(new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray()));
            FrecuencyHelper.MNDisabledNodesPublish(lista);
        }

#endif

    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using U5ki.Infrastructure;
using U5ki.CfgService.Properties;

using Utilities;
using ProtoBuf;
using NLog;

using Translate;
using Newtonsoft.Json;

namespace U5ki.CfgService
{
    /// <summary>
    /// 
    /// </summary>
    public class CfgService : BaseCode, IService
    {
        /// <summary>
        /// 
        /// </summary>
        public CfgService()
        {
        }

        #region IService Members
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return "Cd40ConfigService"; }
        }
        /// <summary>
        /// 
        /// </summary>
        public ServiceStatus Status
        {
            get { return _Status; }
        }

        /**
         * AGL 20120706. Para la Interfaz de control
         * */
        /// <summary>
        /// 
        /// </summary>
        public bool Master
        {
            get { return _Master; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="resp"></param>
        public bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp = null)
        {
            string spar = (string)par;
#if _LOCKING_
            lock (_lock)
            {
#endif
            switch (cmd)
            {
                case ServiceCommands.LoadDefaultCfg:

                    if (_Master == false)
                    {
                        err = "El servicio no esta en modo MASTER";
                    }
                    else if (File.Exists(DefaultCfgFile(spar)))
                    {
                        // using (var file = File.OpenRead(_DefaultCfgFile))
                        using (FileStream file = File.OpenRead(DefaultCfgFile(spar)))
                        {
                            _LastCfg = Serializer.Deserialize<Cd40Cfg>(file);

                            _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, _LastCfg);
                            _Registry.Publish(_LastCfg.Version);

                            // y Lo salvo en el fichero de última configuracion.
                            using (FileStream file1 = File.Create(_LastCfgFile))
                            {
                                // Serializer.Serialize(file1, _LastCfgFile);
                                Serializer.Serialize(file1, _LastCfg);
                            }
                        }
                        LogInfo<CfgService>("Activada configuración " + spar, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                            "CfgService", CTranslate.translateResource("Activada configuración " + spar));
                        return true;
                    }
                    else
                    {
                        err = "No existe la configuracion " + DefaultCfgFile(spar);
                    }
                    break;

                case ServiceCommands.SetDefaultCfg:
                    if (_Master == false)
                    {
                        err = "El servicio no esta en modo MASTER";
                    }

                    else if (_LastCfg != null)
                    {
                        // using (var file = File.Create(_DefaultCfgFile))
                        using (FileStream file = File.Create(DefaultCfgFile(spar)))
                        {
                            Serializer.Serialize(file, _LastCfg);
                        }
                        // _Registry.Send<string>(Identifiers.CfgTopic, Identifiers.CFG_SAVE_AS_DEFAULT_MSG, "");
                        _Registry.Send<string>(Identifiers.CfgTopic, Identifiers.CFG_SAVE_AS_DEFAULT_MSG, spar);
                        LogInfo<CfgService>("Generada configuración " + spar + " por defecto...", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                            "CfgService", CTranslate.translateResource("Generada configuración " + spar + "por defecto..."));
                        return true;
                    }
                    else
                    {
                        err = "No hay ninguna configuracion cargada...";
                    }
                    break;

                case ServiceCommands.ListDefaultCfg:
                    break;

                case ServiceCommands.DelDefaultCfg:
                    string file_name = DefaultCfgFile((string)par);
                    if (_Master == false)
                    {
                        err = "El servicio no esta en modo MASTER";
                    }
                    else if (File.Exists(file_name))
                    {
                        File.Delete(file_name);

                        _Registry.Send<string>(Identifiers.CfgTopic, Identifiers.CFG_DEL_DEFAULT, file_name);
                        LogInfo<CfgService>("Configuración " + spar + " eliminada...", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                            "CfgService", CTranslate.translateResource("Configuración " + spar + " eliminada..."));
                        return true;
                    }
                    else
                    {
                        err = "El Fichero: " + file_name + " no existe"; ;
                    }
                    break;

                case ServiceCommands.GetDefaultConfigId:
                    if (_Master == false)
                    {
                        err = "El servicio no esta en modo MASTER";
                    }
                    else if (_LastCfg != null)
                    {
                        if (resp != null)
                            resp.Add(_LastCfg.Version);
                        err = _LastCfg.Version;
                        return true;
                    }
                    else
                    {
                        err = "No hay ninguna configuracion cargada...";
                    }
                    break;

                default:
                    break;
            }
#if _LOCKING_
            }
#endif

            return false;
        }
        /**
         * Fin del cambio */

        /** 20170217. AGL. Nueva interfaz de comandos. Orientada a estructuras definidas en 'Infraestructure' */
        public bool DataGet(ServiceCommands cmd, ref List<Object> rsp)
        {
            return false;
        }
        /** Fin de la Modificacion */
        public object AllDataGet()
        {
            return new
            {
                std = Status.ToString(),
                level = Status != ServiceStatus.Running ? "Error" : Master == true ? "Master" : "Slave",
                cfg_activa = _Master == false ? "El servicio no esta en modo MASTER" :
                    _LastCfg != null ? _LastCfg.Version : "No hay ninguna configuracion cargada...",
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            try
            {
#if _LOCKING_
                lock (_lock)
                {
#endif

                LogInfo<CfgService>("Iniciando Servicio...");
                ExceptionManageInit();

                _Master = false;
                _LastCfg = null;
                _Status = ServiceStatus.Running;

                _WorkingThread.Start();

                _CheckCfg.AutoReset = false;
                _CheckCfg.Elapsed += OnCheckCfg;

                InitRegistry();

                _CheckCfg.Enabled = true;
                LogInfo<CfgService>("Servicio iniciado.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "CfgService", CTranslate.translateResource("Servicio iniciado."));
            }
#if _LOCKING_
			}
#endif
            catch (Exception ex)
            {
                //LogException<CfgService>("Error arrancando servicio de configuracion.", ex);
                ExceptionManage<CfgService>("Start", ex, "En START. Excepcion: " + ex.Message);
                _CheckCfg.Enabled = false;
                Stop();
            }
        }

        /// <summary>
        /// InitRegistry realiza la inicialización del _Registry
        /// </summary>
        private void InitRegistry()
        {
            _Registry = new Registry(Identifiers.CfgMasterTopic);
            _Registry.ChannelError += OnChannelError;
            _Registry.MasterStatusChanged += OnMasterStatusChanged;
            _Registry.ResourceChanged += OnResourceChanged;

            /**
             * AGL 20120706 Para Sincronizar Configuraciones.
             * */
            _Registry.UserMsgReceived += OnUserMsgReceived;
            /**
             * Fin de la modificacion */

            _Registry.SubscribeToMasterTopic(Identifiers.CfgMasterTopic);
            _Registry.SubscribeToTopic<Cd40Cfg>(Identifiers.CfgTopic);
            _Registry.SubscribeToTopic<SrvMaster>(Identifiers.CfgMasterTopic);
            _Registry.Join(Identifiers.CfgMasterTopic, Identifiers.CfgTopic);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            LogInfo<CfgService>("Iniciando parada servicio Configuracion.");
#if _LOCKING_
            lock (_lock)
            {
                if (_Status == ServiceStatus.Running)
                {
                    Dispose();
                    _Status = ServiceStatus.Stopped;
                }
            }
            _WorkingThread.Stop();
#else
            _WorkingThread.Stop();
            if (_Status == ServiceStatus.Running)
            {
                Dispose();
                _Status = ServiceStatus.Stopped;
            }
#endif
            LogInfo<CfgService>("Servicio detenido.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "CfgService", CTranslate.translateResource("Servicio detenido."));
        }

        #endregion

        #region Private Members
        /// <summary>
        /// 
        /// </summary>
        private bool _Master = false;
        /// <summary>
        /// 
        /// </summary>
        private ServiceStatus _Status = ServiceStatus.Stopped;
        /// <summary>
        /// 
        /// </summary>
        private EventQueue _WorkingThread = new EventQueue();
        /// <summary>
        /// 
        /// </summary>
        private System.Timers.Timer _CheckCfg = new System.Timers.Timer();
        /// <summary>
        /// 
        /// </summary>
        private Registry _Registry = null;
        /// <summary>
        /// 
        /// </summary>
        private UdpSocket _CfgChangesListener = null;
        /// <summary>
        /// 
        /// </summary>
        private Cd40Cfg _LastCfg = null;
        /// <summary>
        /// 
        /// </summary>
        private bool _StopCfgThread = false;
        /// <summary>
        /// Flag that is true during the recovery of data of a new confguration
        /// </summary>
        private bool _CfgOnGoing = false;
        /// <summary>
        /// 
        /// </summary>
        private string _LastVersionReceived = "";
        /// <summary>
        /// 
        /// </summary>
        private string _LastCfgFile = "u5ki.LastCfg.bin";
        private string _LastCfgFileJson = "u5ki.LastCfg.json";
        /// <summary>
        /// Tipo para consulta GetPoolNMElements
        /// </summary>
        private const string TYPE_POOL_NM = "0";
        private const string TYPE_POOL_EE = "1";
#if _LOCKING_
        private object _lock = new object();
#endif
        /// <summary>
        /// 
        /// </summary>
        private void Dispose()
        {
            /**
             * AGL 20120704. Para sincronizar las salidas...
             * */
            while (_Master == true && _CheckCfg.Enabled == false) ;
            /**
             * Fin de la Modificacion */

            _CheckCfg.Enabled = false;
            _StopCfgThread = true;
            if (_Registry != null)
            {
                _Registry.Dispose();
                _Registry = null;
            }

            if (_CfgChangesListener != null)
            {
                _CfgChangesListener.Dispose();
                _CfgChangesListener = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        private void OnChannelError(object sender, string error)
        {
            _WorkingThread.Enqueue("OnChannelError", delegate ()
            {
                LogError<CfgService>("OnChannelError: " + error);

                // Hace Lock interno. lo saco del bloque.
                _WorkingThread.InternalStop();
#if _LOCKING_
                lock (_lock)
                {
#endif
                // Hace Lock interno. lo saco del bloque.                
                // _WorkingThread.InternalStop();

                Dispose();
                _Status = ServiceStatus.Stopped;
#if _LOCKING_
                }
#endif
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="master"></param>
        private void OnMasterStatusChanged(object sender, bool master)
        {
            _Master = master;

            _WorkingThread.Enqueue("OnMasterStatusChanged", delegate ()
            {
#if _LOCKING_
                lock (_lock)
                {
#endif
                if (_Master)
                    //Para que el servicio de configuración entre el último master
                    System.Threading.Thread.Sleep(3000);

                if (_Master)
                {
                    try
                    {
                        //Debug.Assert(_CfgChangesListener == null);
                        if (_LastCfg != null)
                        {
                            _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, _LastCfg);
                            /**
                             * AGL 20120705. Siempre se envía la informacion publicada...
                             * Anterior
                            _Registry.Publish(_LastCfg.Version, false);
                             * */
                            /**
                             * Cambio */
                            _Registry.Publish(_LastCfg.Version);
                            /**
                             * Fin del cambio */
                        }
                        /**
                         * AGL 20120705. Leo la Última configuracion de disco.
                         * */
                        else if (File.Exists(_LastCfgFile))
                        {
                            using (FileStream file = File.OpenRead(_LastCfgFile))
                            {
                                _LastCfg = Serializer.Deserialize<Cd40Cfg>(file);

                                _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, _LastCfg);
                                _Registry.Publish(_LastCfg.Version);

                                // _Logger.Info(_LastCfg.ConfiguracionGeneral.ParametrosGenerales.ToString());
                            }
                        }
                        else if (File.Exists(_LastCfgFileJson))
                        {
                            using (StreamReader r = new StreamReader(_LastCfgFileJson))
                            {
                                string json = r.ReadToEnd();
                                _LastCfg = JsonConvert.DeserializeObject<Cd40Cfg>(json);

                                _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, _LastCfg);
                                _Registry.Publish(_LastCfg.Version);

                                // _Logger.Info(_LastCfg.ConfiguracionGeneral.ParametrosGenerales.ToString());
                            }
                        }
                        else
                            LogInfo<CfgService>("No cfg file found", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "CfgService", "MASTER");
                        /**
                         * Fin del cambio.*/
                    }
                    catch (Exception x)
                    {
                        //LogException<CfgService>("Cambiando a MASTER...", x);
                        ExceptionManage<CfgService>("OnMasterStatusChangedMaster", x, "Cambiando a MASTER. Excepcion: " + x.Message);
                    }
                    finally
                    {
                        LogInfo<CfgService>("MASTER", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "CfgService", "MASTER");
                        OnCheckCfg(null, null);
                    }
                }
                else
                {
                    try
                    {
                        _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, (Cd40Cfg)null);
                        _Registry.Publish(null, false);

                        _CheckCfg.Enabled = false;

                        if (_CfgChangesListener != null)
                        {
                            _CfgChangesListener.Dispose();
                            _CfgChangesListener = null;
                        }

                        _StopCfgThread = true;
                    }
                    catch (Exception x)
                    {
                        //LogException<CfgService>("CONFIG Cambiando a SLAVE...", x);
                        ExceptionManage<CfgService>("OnMasterStatusChangedSlave", x, "Cambiando a SLAVE. Excepcion: " + x.Message);
                    }
                    finally
                    {
                        _CheckCfg.Enabled = false;
                        _CfgChangesListener = null;

                        LogInfo<CfgService>("SLAVE");
                    }
                }
#if _LOCKING_
                }
#endif
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (e.Content != null)
            {
                _WorkingThread.Enqueue("OnResourceChanged", delegate ()
                {
#if _LOCKING_
                    lock (_lock)
                    {
#endif
                    if (!_Master)
                    {
                        try
                        {
                            MemoryStream ms = new MemoryStream(Tools.Decompress(e.Content));
                            _LastCfg = Serializer.Deserialize<Cd40Cfg>(ms);
                            /**
                             * AGL 20120704. Salvo la configuracion en disco...
                             * */
                            using (FileStream file = File.Create(_LastCfgFile))
                            {
                                Serializer.Serialize(file, _LastCfg);
                            }
                            /**
                             * Fin Cambio */
                            string json = JsonConvert.SerializeObject(_LastCfg);
                            File.WriteAllText(_LastCfgFileJson, json);
                        }
                        catch (Exception exc)
                        {
                            ExceptionManage<CfgService>("OnResourceChanged", exc, "Slave save cfg to file " + _LastCfg.Version + " Excepción: " + exc.Message);
                        }
                    }
#if _LOCKING_
                    }
#endif
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckCfg(object sender, System.Timers.ElapsedEventArgs e)
        {
            _WorkingThread.Enqueue("OnTimeElapsed", delegate ()
            {
                LogTrace<CfgService>("OnTimeElapsed-IN");
#if _LOCKING_
                lock (_lock)
                {
#endif
                if (_Master)
                {
                    try
                    {
                        _CheckCfg.Interval = TimeSpan.FromSeconds(Settings.Default.CfgRefreshSegTime).TotalMilliseconds;  // 30000;
                        _CheckCfg.Enabled = false;

                        if (_CfgChangesListener == null)
                        {
                            using (SoapCfg.InterfazSOAPConfiguracion soapSrv = new U5ki.CfgService.SoapCfg.InterfazSOAPConfiguracion())
                            {
                                SoapCfg.ParametrosMulticast mc = soapSrv.GetParametrosMulticast(Settings.Default.CfgSystemId);

                                _CfgChangesListener = new UdpSocket(Settings.Default.MCastItf4Config, ((int)mc.PuertoMulticastConfiguracion));
                                _CfgChangesListener.MaxReceiveThreads = 1;
                                _CfgChangesListener.NewDataEvent += OnSoapCfgChanged;
                                _CfgChangesListener.Base.JoinMulticastGroup(IPAddress.Parse(mc.GrupoMulticastConfiguracion));
                                _CfgChangesListener.BeginReceive();
                            }
                        }

                        _StopCfgThread = false;
                        _CfgOnGoing = true;
                        TryGetSoapCfg();
                        _CfgOnGoing = false;
                    }
                    catch (Exception ex)
                    {
                        //LogException<CfgService>("Conexion a SOAP", ex);
                        _CheckCfg.Enabled = true;
                        ExceptionManage<CfgService>("OnTimeElapsed", ex, "En conexion SOAP. Exception: " + ex.Message);
                    }
                    finally
                    {
                        LogTrace<CfgService>("OnTimeElapsed-OUT");
                    }
                }
#if _LOCKING_
                }
#endif
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dg"></param>
        private void OnSoapCfgChanged(object sender, DataGram dg)
        {
            _WorkingThread.Enqueue("OnSoapCfgChanged", delegate ()
            {
#if _LOCKING_
                lock (_lock)
                {
#endif
                if (_Master && (dg.Data.Length > 0))
                {
                    if (Settings.Default.ConfigByte[0] == dg.Data[0] ||	// 0x31: Comando de configuración; 0x32: Comando Principal/Reserva
                        Settings.Default.ConfigByte.Length == 0)
                    {
                        if (dg.Data[1] >= 0x30)
                        {
                            try
                            {
                                string version = Encoding.ASCII.GetString(dg.Data, 1, dg.Data.Length - 1);
                                if (version == _LastVersionReceived)
                                {
                                    return;
                                }

                                _LastVersionReceived = version;
                            }
                            catch (Exception) { }

                            LogInfo<CfgService>("OnSoapCfgChanged: " + "Recibida notificacion de cambio de configuracion SOAP");
                            if (_CfgOnGoing == true)
                                _StopCfgThread = true;
                            _CheckCfg.Stop();
                            _CheckCfg.Interval = 1;
                            _CheckCfg.Start();

                        }
                    }
                }
#if _LOCKING_
                }
#endif
            });
        }
        /// <summary>
        /// Thread para la carga de una nueva configuración.
        /// </summary>
        private void TryGetSoapCfg()
        {
            try
            {
                /** Identifica la configuracion en la base de datos */
                string systemId = Settings.Default.CfgSystemId;
                LogTrace<CfgService>("TryGetSoapCfg: " + "Chequeando configuración SOAP. SystemId: " + systemId);

                /** Encapsula el cliente del servicio WEB de Configuracion */
                using (SoapCfg.InterfazSOAPConfiguracion soapSrv = new U5ki.CfgService.SoapCfg.InterfazSOAPConfiguracion())
                {
                    /** Comprueba si han habido cambios de configuracion */
                    string soapVersion = soapSrv.GetVersionConfiguracion(systemId);
                    if (_StopCfgThread) return;
                    if ((_LastCfg != null) && (soapVersion == _LastCfg.Version))
                    {
                        LogTrace<CfgService>("TryGetSoapCfg: " + "Configuración actual coincide con configuración SOAP");
                        return;
                    }

                    /** Obtiene una copia de la configuracion en el formato SOAP del servicio */
                    SoapCfg.ConfiguracionSistema soapSysCfg = soapSrv.GetConfigSistema(systemId);
                    if (_StopCfgThread) return;

                    /** */
                    List<SoapCfg.CfgUsuario> soapUsers = new List<SoapCfg.CfgUsuario>();
                    List<SoapCfg.CfgEnlaceExterno[]> soapUsersExLinks = new List<SoapCfg.CfgEnlaceExterno[]>();
                    List<SoapCfg.CfgEnlaceInterno[]> soapUsersInLinks = new List<SoapCfg.CfgEnlaceInterno[]>();
                    List<string> hosts = new List<string>();
                    Dictionary<string, string> dominantUsers = new Dictionary<string, string>();

                    /** Extrae de la Configuracion SOAP la lista de 'hosts' ??? */
                    foreach (SoapCfg.AsignacionUsuariosTV asign in soapSysCfg.PlanAsignacionUsuarios)
                    {
                        if (!string.IsNullOrEmpty(asign.IdUsuario) && !hosts.Contains(asign.IdHost))
                        {
                            hosts.Add(asign.IdHost);
                        }
                    }
                    SoapCfg.LoginTerminalVoz soapLoginCfg;
                    SoapCfg.CfgUsuario soapCfgUser;
                    /** De la lista de Hosts, determina los usuarios y de enlaces a usuarios internos y externos ??? y de 'usarios dominantes' ??? */
                    foreach (string host in hosts)
                    {
                        soapLoginCfg = soapSrv.LoginTop(systemId, host);
                        if (_StopCfgThread) return;

                        if (!string.IsNullOrEmpty(soapLoginCfg.IdUsuario))
                        {
                            soapCfgUser = soapSrv.GetCfgUsuario(systemId, soapLoginCfg.IdUsuario);
                            soapUsers.Add(soapCfgUser);
                            dominantUsers[host] = soapCfgUser.IdIdentificador;
                            if (_StopCfgThread) return;

                            soapUsersExLinks.Add(soapSrv.GetListaEnlacesExternos(systemId, soapLoginCfg.IdUsuario));
                            if (_StopCfgThread)
                                return;

                            soapUsersInLinks.Add(soapSrv.GetListaEnlacesInternos(systemId, soapLoginCfg.IdUsuario));
                            if (_StopCfgThread) return;

                        }
                    }

                    /** Nueva configuracion en formato 'nodebox' */
                    Cd40Cfg cfg = new Cd40Cfg();
                    cfg.Version = soapVersion;
                    cfg.ConfiguracionGeneral = new ConfiguracionSistema();

                    /** Carga la configuracion 'nodebox' desde la configuracion SOAP. */
                    CfgTranslators.Translate(cfg.ConfiguracionGeneral, soapSysCfg);

                    /** Añade los Usuarios... */
                    for (int i = 0, to = soapUsers.Count; i < to; i++)
                    {
                        ConfiguracionUsuario user = new ConfiguracionUsuario();

                        CfgTranslators.Translate(user, soapUsers[i], soapUsersExLinks[i], soapUsersInLinks[i]);
                        cfg.ConfiguracionUsuarios.Add(user);
                    }

                    /** Añade los usuarios 'dominantes' */
                    foreach (KeyValuePair<string, string> p in dominantUsers)
                    {
                        AsignacionUsuariosDominantesTV dominantUser = new AsignacionUsuariosDominantesTV();
                        dominantUser.IdHost = p.Key;
                        dominantUser.IdUsuario = p.Value;

                        cfg.ConfiguracionGeneral.PlanAsignacionUsuariosDominantes.Add(dominantUser);
                    }

                    /** AGL. Carga la Configuracion HF */
                    SoapCfg.PoolHfElement[] hfpool = soapSrv.GetPoolHfElement(systemId);
                    if (hfpool != null)
                    {
                        foreach (SoapCfg.PoolHfElement hfelement in hfpool)
                        {
                            CfgTranslators.Translate(cfg, hfelement);
                        }
                    }

                    SoapCfg.Node[] nmPool = soapSrv.GetPoolNMElements(systemId, TYPE_POOL_NM);
                    if (nmPool != null)
                    {
                        foreach (SoapCfg.Node nmelement in nmPool)
                        {
#if DEBUG1
                            nmelement.ModeloEquipo = 1000;
#endif
                            CfgTranslators.Translate(cfg, nmelement);
                        }
                    }
                    SoapCfg.Node[] eePool = soapSrv.GetPoolNMElements(systemId, TYPE_POOL_EE);
                    if (eePool != null)
                    {
                        foreach (SoapCfg.Node extElement in eePool)
                        {
#if DEBUG1
                            nmelement.ModeloEquipo = 1000;
#endif
                            CfgTranslators.Translate(cfg, extElement, false);
                        }
                    }
                    try
                    {
                        /**
                         * AGL 20120705. Salvar la configuracion en disco
                         * */
                        using (FileStream file = File.Create(_LastCfgFile))
                        {
                            Serializer.Serialize(file, cfg);
                        }

                        string json = JsonConvert.SerializeObject(cfg);
                        File.WriteAllText(_LastCfgFileJson, json);
                    }
                    catch (Exception exc)
                    {
                        ExceptionManage<CfgService>("TryGetSoapCfg", exc, "En TryGetSoapCfg save to file " + cfg.Version + " Excepción: " + exc.Message);
                    }
                    /**
                     * Fin de Modificacion */

                    /** Salva y distribuye la configuracion */
                    _WorkingThread.Enqueue("PublishNewCfg", delegate ()
                    {
                        try
                        {
                            if (_Master && !_StopCfgThread)
                            {
                                _LastCfg = cfg;
                                LogInfo<CfgService>("Publicando nueva configuración: " + cfg.Version, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                                    "CfgService", CTranslate.translateResource("Publicando nueva configuración: " + cfg.Version));

                                _Registry.SetValue(Identifiers.CfgTopic, Identifiers.CfgRsId, cfg);
                                _Registry.Publish(cfg.Version);
                            }
                        }
                        catch (Exception ex)
                        {
                            //LogException<CfgService>("TryGetSoapCfg: Error publicando configuracion " + cfg.Version, ex);
                            ExceptionManage<CfgService>("TryGetSoapCfg", ex, "En TryGetSoapCfg. Excepcion: " + ex.Message + ". publicando configuracion " + cfg.Version);

                            _WorkingThread.InternalStop();
                            Dispose();
                            _Status = ServiceStatus.Stopped;
                        }
                    });
                }
            }
            catch (ThreadAbortException ex)
            {
                Thread.ResetAbort();
                ExceptionManage<CfgService>("TryGetSoapCfg", ex, "En TryGetSoapCfg Excepcion: " + ex.Message);
            }
            catch (Exception ex)
            {
                //LogException<CfgService>("TryGetSoapCfg: Intento fallido de obtener la configuracion SOAP.", ex);
                ExceptionManage<CfgService>("TryGetSoapCfg", ex, "En TryGetSoapCfg Excepcion: " + ex.Message);
            }
            finally
            {
                _CheckCfg.Enabled = true;
            }
            return;
        }

        /// <summary>
        /// AGL 20120706. Para Sincronizar Configuracion por Defecto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnUserMsgReceived(object sender, SpreadDataMsg msg)
        {
#if _LOCKING_
            lock (_lock)
            {
#endif
            if (msg.Type == Identifiers.CFG_SAVE_AS_DEFAULT_MSG)
            {
                if (_Master == false)
                {
                    if (_LastCfg != null)
                    {
                        MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                        string par = Serializer.Deserialize<string>(ms);
                        using (FileStream file = File.Create(DefaultCfgFile(par)))
                        {
                            Serializer.Serialize(file, _LastCfg);
                            LogInfo<CfgService>("Generada configuracion por defecto", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                                "CfgService", CTranslate.translateResource("Generada configuracion por defecto"));
                        }
                    }
                    else
                    {
                        LogError<CfgService>(CTranslate.translateResource("No tengo configuración válida"),
                            U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR);
                    }
                }
            }
            if (msg.Type == Identifiers.CFG_DEL_DEFAULT)
            {
                if (_Master == false)
                {
                    MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                    string file_name = Serializer.Deserialize<string>(ms);
                    if (File.Exists(file_name))
                    {
                        File.Delete(file_name);
                    }

                }
            }
#if _LOCKING_
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfgnumber"></param>
        /// <returns></returns>
        private string DefaultCfgFile(string cfgnumber)
        {
            return string.Format("u5ki.DefaultCfg.{0}.bin", cfgnumber);
        }

        /**
         * Fin del cambio */

        #endregion
    }
}

//#define _WITH_EVENTQUEUE_
//#define _ACTIVATE_EXRES_
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using System.Diagnostics;
using Newtonsoft.Json;

using U5ki.PresenceService.Agentes;

using U5ki.Infrastructure;
using ProtoBuf;
using Utilities;

namespace U5ki.PresenceService
{
    public class U5kPresService : BaseCode, IService
    {
#if _WITH_EVENTQUEUE_
        private EventQueue _WorkingThread = new EventQueue();
#endif
        #region IService

        bool _Master = false;
        public bool Master { get { return _Master; } }
        string _Name = "PresenceServer";
        public string Name { get { return _Name; } }
        ServiceStatus _Status = ServiceStatus.Stopped;
        public ServiceStatus Status { get { return _Status; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="par"></param>
        /// <param name="err"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        public bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp = null) { return false; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="rsp"></param>
        /// <returns></returns>
        public bool DataGet(ServiceCommands cmd, ref List<object> rsp)
        {
            if (smpAccesMain.Acquire())
            {
                List<object> lista = new List<object>();
                lista.Add(JData);
                rsp = new List<object>(lista);
                smpAccesMain.Release();
                return true;
            }
            return false;
        }

        public object AllDataGet()
        {
            return new
            {
                std = Status.ToString(),
                level = Status != ServiceStatus.Running ? "Error" : Master == true ? "Master" : "Slave",
                data = new
                {
                    id = Name,
                    mode = Master ? "Master" : "Slave",
                    conf = LastVersion,
                    proxies = from p in global_agent?.RsTable
                              select new
                              {
                                  id = p.Dependency,
                                  type = p.type,
                                  endp = p.name,
                                  status = p.Status,
                                  ver = p.version
                              },
                    internals = from i in agents
                                where i.Type == Interfaces.AgentType.ForInternalSub
                                select new
                                {
                                    id = i.DependencyName,
                                    type = i.Type,
                                    main = i.MainService,
                                    connected = i.State == Interfaces.AgentStates.Connected,
                                    endp = i.ProxyEndpoint.ToString(),
                                    pres = i.PresenceEndpoint != null ? i.PresenceEndpoint.ToString() : "none",
                                    subs = from s in i.RsTable
                                           select new
                                           {
                                               id = s.name,
                                               uri = s.Uri,
                                               status = s.Status,
                                               ver = s.version
                                           }
                                },
                    externals = from i in agents
                                where i.Type == Interfaces.AgentType.ForExternalSub
                                select new
                                {
                                    id = i.DependencyName,
                                    type = i.Type,
                                    main = i.MainService,
                                    connected = i.State == Interfaces.AgentStates.Connected,
                                    endp = i.ProxyEndpoint.ToString(),
                                    pres = i.PresenceEndpoint != null ? i.PresenceEndpoint.ToString() : "none",
                                    subs = from s in i.RsTable
                                           select new
                                           {
                                               id = s.name,
                                               uri = s.Uri,
                                               status = s.Status,
                                               ver = s.version
                                           }
                                },
#if _ACTIVATE_EXRES_
                    externals_res = from i in agents
                                    where i.Type == Interfaces.AgentType.ForExternalResources
                                    select new
                                    {
                                        id = i.DependencyName,
                                        type = i.Type,
                                        main = i.MainService,
                                        connected = i.State == Interfaces.AgentStates.Connected,
                                        endp = i.ProxyEndpoint?.ToString(),
                                        pres = i.PresenceEndpoint?.ToString(),
                                        res = from s in i.RsTable
                                              select new
                                              {
                                                  id = s.name,
                                                  uri = s.Uri,
                                                  status = s.Status,
                                                  ver = s.version
                                              }
                                    },
#endif
                    PersistenceOfStates = from s in PresenceServerResource.PersistenceOfStates.LastStates
                                          select new
                                          {
                                              res = s.Key,
                                              std = s.Value
                                          }

                }
            };
        }
        /// <summary>
        /// 
        /// </summary>
        protected string JData
        {
            get
            {
                var data = new
                {
                    id = Name,
                    mode = Master ? "Master" : "Slave",
                    conf = LastVersion,
                    proxies = from p in global_agent?.RsTable
                              select new
                              {
                                  id = p.Dependency,
                                  type = p.type,
                                  endp = p.name,
                                  status = p.Status,
                                  ver = p.version
                              },
                    internals = from i in agents
                                where i.Type == Interfaces.AgentType.ForInternalSub
                                select new
                                {
                                    id = i.DependencyName,
                                    type = i.Type,
                                    main = i.MainService,
                                    connected = i.State == Interfaces.AgentStates.Connected,
                                    endp = i.ProxyEndpoint.ToString(),
                                    pres = i.PresenceEndpoint != null ? i.PresenceEndpoint.ToString() : "none",
                                    subs = from s in i.RsTable
                                           select new
                                           {
                                               id = s.name,
                                               uri = s.Uri,
                                               status = s.Status,
                                               ver = s.version
                                           }
                                },
                    externals = from i in agents
                                where i.Type == Interfaces.AgentType.ForExternalSub
                                select new
                                {
                                    id = i.DependencyName,
                                    type = i.Type,
                                    main = i.MainService,
                                    connected = i.State == Interfaces.AgentStates.Connected,
                                    endp = i.ProxyEndpoint.ToString(),
                                    pres = i.PresenceEndpoint != null ? i.PresenceEndpoint.ToString() : "none",
                                    subs = from s in i.RsTable
                                           select new
                                           {
                                               id = s.name,
                                               uri = s.Uri,
                                               status = s.Status,
                                               ver = s.version
                                           }
                                },
#if _ACTIVATE_EXRES_
                    externals_res = from i in agents
                                where i.Type == Interfaces.AgentType.ForExternalResources
                                select new
                                {
                                    id = i.DependencyName,
                                    type = i.Type,
                                    main = i.MainService,
                                    connected = i.State == Interfaces.AgentStates.Connected,
                                    endp = i.ProxyEndpoint?.ToString(),
                                    pres = i.PresenceEndpoint?.ToString(),
                                    res = from s in i.RsTable
                                           select new
                                           {
                                               id = s.name,
                                               uri = s.Uri,
                                               status = s.Status,
                                               ver = s.version
                                           }
                                },
#endif
                    PersistenceOfStates = from s in PresenceServerResource.PersistenceOfStates.LastStates
                                          select new
                                          {
                                              res = s.Key,
                                              std = s.Value
                                          }

                };
                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            /** Gestion por semaforo */
            if (smpAccesMain.Acquire())
            {

                try
                {
                    LogInfo<U5kPresService>("Iniciando Servicio...");

                    _Master = false;
                    LastVersion = String.Empty;
                    _Status = ServiceStatus.Running;

#if _WITH_EVENTQUEUE_
                    _WorkingThread.Start();
#endif

                    _udpClient = new UdpClient();
                    /** Comunica los eventos MASTER/SLAVE del Servicio TIFX */
                    _ipcService = new InterProcessEvent("tifx_master", OnMasterStatusChanged);

                    LogInfo<U5kPresService>("Servicio Iniciado.",
                        U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PresenceService",
                        Translate.CTranslate.translateResource("Servicio iniciado."));

                }
                catch (Exception x)
                {
                    Dispose();
                    LogException<U5kPresService>("Excepcion arrancando servicio", x, false);
                }
                finally
                {
                    smpAccesMain.Release();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            /** Gestion por semaforo */
            if (smpAccesMain.Acquire())
            {
                try
                {
                    LogInfo<U5kPresService>("Iniciando parada servicio");

                    if (_Master == true)
                    {
                        DeactivateService();
                    }

                    Dispose();
                }
                catch (Exception x)
                {
                    LogException<U5kPresService>("Excepcion deteniendo servicio", x, false);
                }
                finally
                {
                    LogInfo<U5kPresService>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                        "PresenceService", Translate.CTranslate.translateResource("Servicio detenido."));
                    smpAccesMain.Release();
                }
            }
        }

#endregion

        /** Para evitar reconfiguraciones ociosas... */
        protected string LastVersion { get; set; }
        /** Lista de Agentes de Presencia */
        protected List<Interfaces.IAgent> agents = new List<Interfaces.IAgent>();
        protected Interfaces.IAgent global_agent = new PSProxiesAgent("");
        /** */
        InterProcessEvent _ipcService = null;
        /** */
        UdpClient _udpClient;
        /** */
        Registry _Registry = null;
        /** */
#if _WITH_EVENTQUEUE_
        PSHelper.DummySemaphore smpAccesMain = new PSHelper.DummySemaphore(1, 1, "PresenceServiceAccess", 5000);
#else
        PSHelper.ManagedSemaphore smpAccesMain = new PSHelper.ManagedSemaphore(1, 1, "PresenceServiceAccess", 5000);
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnMasterStatusChanged(object sender, SpreadDataMsg msg)
        {
            bool master = false;
#if _WITH_EVENTQUEUE_
            _WorkingThread.Enqueue("OnMasterStatusChanged", delegate()
#else
            Task.Factory.StartNew(() =>
#endif
            {
                /** Gestion por semaforo */
                if (smpAccesMain.Acquire())
                {
                    try
                    {
                        MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                        master = Serializer.Deserialize<bool>(ms);

                        if (master && !_Master)
                        {
                            ActivateService();
                        }
                        else if (!master && _Master)
                        {
                            DeactivateService();
                            LastVersion = string.Empty;
                        }
                        PSHelper.LOGGER.Trace<U5kPresService>(String.Format("OnMasterStatusChanged {0}->{1}", _Master, master));
                        _Master = master;

                        LogInfo<U5kPresService>(_Master ? "MASTER" : "SLAVE",
                            U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "PresenceService",
                            _Master ? "MASTER" : "SLAVE");
                    }
                    catch (Exception x)
                    {
                        LogException<U5kPresService>("OnMasterStatusChanged to " + master.ToString() + " Exception", x, false);
                    }
                    finally
                    {
                        smpAccesMain.Release();
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (_Master && e.Content != null && e.Type == Identifiers.TypeId(typeof(Cd40Cfg)))
            {
#if _WITH_EVENTQUEUE_
                _WorkingThread.Enqueue("OnResourceChanged", delegate()
#else
                Task.Factory.StartNew(() =>
#endif
                {
                    try
                    {
                        MemoryStream ms = new MemoryStream(Tools.Decompress(e.Content));
                        Cd40Cfg cfg = Serializer.Deserialize<Cd40Cfg>(ms);

                        if (LastVersion == cfg.Version)
                            return;

                        LastVersion = cfg.Version;
                        LogInfo<U5kPresService>("Procesando Configuracion: " + LastVersion);
                        if (smpAccesMain.Acquire())
                        {
                            try
                            {
                                UnconfigureService();
                            }
                            catch (Exception x)
                            {
                                LogException<U5kPresService>("OnResourceChanged Exception", x, false);
                            }
                            smpAccesMain.Release();
                        }
                        /** Hay que esperar a que terminen los eventos internos generados */
                        Thread.Sleep(1000);
                        ConfigureService(cfg);
                    }
                    catch (Exception x)
                    {
                        LogException<U5kPresService>("OnResourceChanged Exception", x, false);
                    }
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAgentEventOccurred(object sender, Interfaces.AgentEventArgs e)
        {
            if (!_Master)
                return;
#if _WITH_EVENTQUEUE_
            _WorkingThread.Enqueue("OnAgentEventOcurred", delegate()
#else
            var tevent = Task.Factory.StartNew(() =>
#endif
            {
                /** Gestion por semaforo */
                if (smpAccesMain.Acquire())
                {
                    try
                    {
                        PSHelper.LOGGER.Trace<U5kPresService>(String.Format("OnAgentEventOccurred in {0}: {1}, {2}",
                            e.agent.Name, e.ev, e.p1));
                        switch (e.ev)
                        {
                            case Interfaces.AgentEvents.ResourceOptions:
                                if (ServiceConfigured)
                                {
                                    e.retorno(PSHelper.ControlledSipAgent.SendOptionsMsg(e.p1));
                                }
                                break;

                            case Interfaces.AgentEvents.Ping:
                                // PresenceServiceHelper.AsyncPing(e.p1, true, OnAsyncPingCompleted);
                                if (ServiceConfigured)
                                {
                                    e.agent.callIdOptions = PSHelper.ControlledSipAgent.SendOptionsMsg(e.p1);
                                }
                                break;
                            case Interfaces.AgentEvents.SubscribeUser:
                                if (ServiceConfigured)
                                {
                                    PSHelper.ControlledSipAgent.CreatePresenceSubscription(e.p1);
                                    PSHelper.LOGGER.Trace<U5kPresService>(String.Format("Subscribe User: [{0}],", e.p1));
                                }
                                else
                                {
                                    PSHelper.LOGGER.Trace<U5kPresService>(String.Format("Subscribe User Fail: [{0}],", e.p1));
                                }
                                break;
                            case Interfaces.AgentEvents.UnsubscribeUser:
                                PSHelper.ControlledSipAgent.DestroyPresenceSubscription(e.p1);
                                PSHelper.LOGGER.Trace<U5kPresService>(String.Format("UnSubscribe User: [{0}],", e.p1));
                                break;
                            case Interfaces.AgentEvents.Active:
                            case Interfaces.AgentEvents.Inactive:
                            case Interfaces.AgentEvents.Refresh:
                                /** Se acelera el envio de trama correspondiente */
                                if (SendingControl.ContainsKey(e.agent.GetType().Name))
                                {
                                    SendingControl[e.agent.GetType().Name] = true;
                                }
                                PSHelper.LOGGER.Trace<U5kPresService>(String.Format("Agente Presencia {0}/{1}: {2}: {3}",
                                    e.agent.GetType().Name,
                                    ((Agentes.PSBaseAgent)e.agent).pingCount,
                                    e.ev.ToString(), e.p1));
                                break;

                            case Interfaces.AgentEvents.LogException:
                                LogException<U5kPresService>(e.p1, e.x, false);
                                PSHelper.LOGGER.Trace<U5kPresService>("Excepcion de Agente: " + e.p1 + ": " + e.x.Message);
                                break;

                            default:
                                break;
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<U5kPresService>("OnAgentEventOccurred Exception", x, false);
                        PSHelper.LOGGER.Trace<U5kPresService>("OnAgentEventOccurred Exception: " + x.Message);
                    }
                    finally
                    {
                    }
                    smpAccesMain.Release();
                }
            }/*, TaskCreationOptions.LongRunning*/);
            // tevent.Wait();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAsyncPingCompleted(object sender, PingCompletedEventArgs e)
        {
#if _WITH_EVENTQUEUE_
            _WorkingThread.Enqueue("OnAsyncPingCompleted", delegate()
#else
            Task.Factory.StartNew(() =>
#endif
            {
                if (smpAccesMain.Acquire())
                {
                    if (ServiceConfigured == true && _Master)
                    {
                        agents.ForEach(agent =>
                        {
                            agent.PingResponse(e.Reply.Address.ToString(), "",
                                e.Reply.Status == IPStatus.Success ? Interfaces.AgentStates.Connected : Interfaces.AgentStates.NotConnected);
                        });
                    }
                    smpAccesMain.Release();
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="code"></param>
        /// <param name="supported"></param>
        /// <param name="allowed"></param>
        private void OnOptionsResponse(string from, string callid, int code, string supported, string allowed)
        {
#if _WITH_EVENTQUEUE_
            _WorkingThread.Enqueue("OnOptionsResponse", delegate()
#else
            Task.Factory.StartNew(() =>
#endif
            {
                PSHelper.LOGGER.Trace<U5kPresService>(String.Format("OnOptionsResponse: [{0}],[{1}],[{2}],[{3}],[{4}]", from, callid, code, supported, allowed));
                if (smpAccesMain.Acquire())
                {
                    if (ServiceConfigured == true && _Master)
                    {
                        agents.ForEach(agent =>
                        {
                            agent.PingResponse(from, callid,
                                (code == 200 || code == 404) ? Interfaces.AgentStates.Connected : Interfaces.AgentStates.NotConnected, code);
                        });
                    }
                    smpAccesMain.Release();
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst_uri"></param>
        /// <param name="subscription_status"></param>
        /// <param name="presence_status"></param>
        private void OnRegisterInfoReceived(string dst_uri, int subscription_status, int presence_status)
        {
#if _WITH_EVENTQUEUE_
            _WorkingThread.Enqueue("OnRegisterInfoReceived", delegate()
#else
            Task.Factory.StartNew(() =>
#endif
            {
                PSHelper.LOGGER.Trace<U5kPresService>(String.Format("OnRegisterInfoReceived: [{0}],[{1}],[{2}],",
                    dst_uri, subscription_status, presence_status));
                if (smpAccesMain.Acquire())
                {
                    if (ServiceConfigured == true && _Master)
                    {
                        try
                        {
                            /** Calculo el estado... */
                            bool NotifiedStatus = subscription_status == 0 ? true :
                                presence_status == 1 ? true : false;

                            agents.ForEach(agent =>
                            {
                                agent.PresenceEventOcurred(dst_uri, NotifiedStatus);
                            });

                            /** Si no está registrado lo borro y lo reintento en un tiempo */
                            if (subscription_status == 0)
                            {
                                Task.Factory.StartNew(() =>
                                {
                                    PSHelper.ControlledSipAgent.DestroyPresenceSubscription(dst_uri);
                                    Thread.Sleep(1000 * PSHelper.LocalParameters.TimeoutOnPresenceSubscription);
                                    if (ServiceConfigured == true)
                                    {
                                        /** Comprobar que el Agente correspondiente no se ha desactivado en el intervalo */
                                        Interfaces.IAgent ag = agents.Where(a => a.Type == Interfaces.AgentType.ForExternalSub &&
                                            a.RsTable.Where(r => r.Uri == dst_uri).FirstOrDefault() != null).FirstOrDefault();

                                        if (ag != null && ag.State == Interfaces.AgentStates.Connected)
                                        {
                                            PSHelper.ControlledSipAgent.CreatePresenceSubscription(dst_uri);
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception x)
                        {
                            throw x;
                        }
                    }
                    smpAccesMain.Release();
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private void ActivateService()
        {
            PSHelper.LOGGER.Trace<U5kPresService>("Activating Service");

            /** Al activarse el servicio se borran la persistencia de estado de recursos */
            PresenceServerResource.PersistenceOfStates.Free();

            _Registry = new Registry("uv5k-prs");
            _Registry.ResourceChanged += OnResourceChanged;
            _Registry.SubscribeToTopic<Cd40Cfg>(Identifiers.CfgTopic);
            _Registry.Join(Identifiers.CfgTopic);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        bool ServiceConfigured = false;
        Task FrameSenderTask = null;
        private void ConfigureService(Cd40Cfg cfg)
        {
            PSHelper.LOGGER.Trace<U5kPresService>("Configuring Service");
            /** Sincronizar con el Enviador de tramas */
            if (FrameSenderTask != null)
                FrameSenderTask.Wait(5000);

            PSHelper.LocalParameters.ReadConfig(cfg);

            /** Activar el socket de notificaciones. */
            _udpClient.MulticastLoopback = false;
            _udpClient.JoinMulticastGroup(
                IPAddress.Parse(PSHelper.LocalParameters.MulticastGroupIp),
                IPAddress.Parse(PSHelper.LocalParameters.MulticastInterfaceIp));

            /**  Cliente para el SIP. */
            PSHelper.ControlledSipAgent.Init(OnOptionsResponse, OnRegisterInfoReceived);
            PSHelper.ControlledSipAgent.Start();

            /** Obtiene la lista de Dependencias, y extrae los parametros necesarios para
             *  crear los agentes. Pueden venir Dependencias Repetidas en funcion de los rangos que
                se configuren. */
            var Dependencias = cfg.ConfiguracionGeneral.PlanDireccionamientoIP.
                Where(rs => rs.TipoHost == Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA && rs.EsCentralIP == true/* (rs.Max != 0 || rs.Min != 0)*/).
                GroupBy(d => d.IdHost).     // Para filtrar las posibles repeticiones.
                Select(d => d.First()).
                Select(d => new
                {
                    Agents = new[] 
                    {
                        new 
                        {
                            Name = d.IdHost, IsInterno = d.Interno, IsMain = true,
                            ProxyEP = PSHelper.SipEndPointFrom(d.IpRed1),
                            PresenceEP = PSHelper.SipEndPointFrom(d.SrvPresenciaIpRed1)
                        },
                        new 
                        {
                            Name = d.IdHost, IsInterno = d.Interno, IsMain = false,
                            ProxyEP = PSHelper.SipEndPointFrom(d.IpRed2),
                            PresenceEP = PSHelper.SipEndPointFrom(d.SrvPresenciaIpRed2)
                        },
                        new 
                        {
                            Name = d.IdHost, IsInterno = d.Interno, IsMain = false,
                            ProxyEP = PSHelper.SipEndPointFrom(d.IpRed3),
                            PresenceEP = PSHelper.SipEndPointFrom(d.SrvPresenciaIpRed3)
                        }
                    }
                }).
                ToList();

            /** Crear y activar los agentes.... */
            Dependencias.ForEach(d =>
            {
                d.Agents.ToList().ForEach(a =>
                {
                    Interfaces.IAgent agent = null;
                    if (a.IsInterno && a.ProxyEP != null)
                    {
                        /** Activar los agentes Internos */
                        agent = new Agentes.PSBkkAgent()
                        {
                            ProxyEndpoint = a.ProxyEP,
                            PresenceEndpoint = a.PresenceEP,
                            DependencyName = a.Name,
                            MainService = a.IsMain
                        };
                    }
                    else if (a.ProxyEP != null)
                    {
                        /** Activar Los agentes de Destinos externos */
                        agent = new Agentes.PSExternalAgent()
                        {
                            ProxyEndpoint = a.ProxyEP,
                            PresenceEndpoint = a.PresenceEP,
                            DependencyName = a.Name,
                            MainService = a.IsMain
                        };
                    }
                    else
                    {
                        /** Dependencia sin Proxy. Registrar y Notificar */
                    }
                    if (agent != null)
                    {
                        agent.Init(OnAgentEventOccurred, cfg);
                        agent.Start();
                        agents.Add(agent);
                    }
                });
            });

#if _ACTIVATE_EXRES_
            /** EXRES. Crear el Agente de Equipos Externos */
            var extAgent = new Agentes.PSExternalResourcesAgent();
            extAgent.Init(OnAgentEventOccurred, cfg);
            extAgent.Start();
            agents.Add(extAgent);
#endif
            /** Activar el Agente de Agentes */
            global_agent = new Agentes.PSProxiesAgent( ServiceSite);
            global_agent.Init(OnAgentEventOccurred, agents);
            global_agent.Start();

            ServiceConfigured = true;

            /** Activo el Thread de envio de tramas... */
            FrameSenderTask = Task.Factory.StartNew(FrameSenderTaskRoutine);
        }
        /// <summary>
        /// 
        /// </summary>
        private void UnconfigureService()
        {
            PSHelper.LOGGER.Trace<U5kPresService>("Unconfiguring Service");

            bool lastServiceConfigured = ServiceConfigured;

            ServiceConfigured = false;

            /** Desactivar los Agentes */
            if (global_agent != null)
                global_agent.Dispose();
            global_agent = new PSProxiesAgent("");

            agents.ForEach(agent =>
            {
                agent.Dispose();
            });
            agents.Clear();

            /** Desactivar el socket de notificaciones */
            if (lastServiceConfigured)
                _udpClient.DropMulticastGroup(IPAddress.Parse(PSHelper.LocalParameters.MulticastGroupIp));

            /** Desactivar el SIP */
            if (lastServiceConfigured)
            {
                PSHelper.ControlledSipAgent.End(OnOptionsResponse, OnRegisterInfoReceived);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void DeactivateService()
        {
            PSHelper.LOGGER.Trace<U5kPresService>("Deactivating Service");
            if (_Registry != null)
            {
                _Registry.Dispose();
                _Registry = null;
            }

            UnconfigureService();
        }
        /// <summary>
        /// 
        /// </summary>
        private void Dispose()
        {
            PSHelper.LOGGER.Trace<U5kPresService>("Disposing Service");
            if (_ipcService != null)
            {
                _ipcService.Dispose();
                _ipcService = null;
            }

            _udpClient.Close();

#if _WITH_EVENTQUEUE_
            _WorkingThread.Stop();
#endif

            _Status = ServiceStatus.Stopped;
            LastVersion = String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, bool> SendingControl = new Dictionary<string, bool>()
        {
            {typeof(PSBkkAgent).Name,false},
            {typeof(PSExternalAgent).Name, false},
            {typeof(PSProxiesAgent).Name, false},
#if _ACTIVATE_EXRES_
            /** EXRES Incluir el control de envio del agente de recursos externos de telefonía. */
            {typeof(PSExternalResourcesAgent).Name, false},
#endif
        };


        private void FrameSenderTaskRoutine()
        {
            IPEndPoint mcastTifx = new IPEndPoint(
                IPAddress.Parse(PSHelper.LocalParameters.MulticastGroupIp),
                PSHelper.LocalParameters.MulticastGroupPort);
            Byte[] mtrama;

            /** Para sincronizar eventos en las reconfiguraciones */
            Thread.Sleep(2000);

            DateTime last = DateTime.Now;
            /** Para las tramas externas agrupadas */
            Agentes.PSExternalAgent.GlobalVersion = 0;
            PSHelper.LOGGER.Trace<U5kPresService>(String.Format("FrameSenderTask Started"));
            while (ServiceConfigured == true)
            {
                Thread.Sleep(200);

#if _WITH_EVENTQUEUE_
                _WorkingThread.Enqueue("FrameSenderTaskRoutine", delegate()
            {
#endif
                if (smpAccesMain.Acquire())
                {
                    if (ServiceConfigured == true && _Master)
                    {
                        try
                        {
                            if (SendingControl[typeof(PSBkkAgent).Name] == true)
                            {
                                /** Fusionar y Enviar Trama Internos...  Priorizando el activo y despues el PPAL ... */
                                Interfaces.IAgent IntAgent =
                                    agents.Where(a => a.Type == Interfaces.AgentType.ForInternalSub)
                                    .OrderBy(a => a.State == Interfaces.AgentStates.Connected ? "0" : "1")
                                    .ThenBy(a => a.MainService ? "0" : "1")
                                    .FirstOrDefault();

                                if (IntAgent != null)
                                {
                                    mtrama = IntAgent.Frame;
                                    _udpClient.Send(mtrama, mtrama.Count(), mcastTifx);
                                    LogTrace<U5kPresService>(String.Format("FrameSenderTask Sending Internal Subs ({0})",
                                        ((Agentes.PSBaseAgent)IntAgent).rsCount));
                                }
                                SendingControl[typeof(PSBkkAgent).Name] = false;
                            }

                            if (SendingControl[typeof(PSExternalAgent).Name] == true)
                            {
                                /** Fusionar y Enviar Tramas Externos... */
                                /** 1. Obtengo la lista, priorizando a los activos y despues principales */
                                var ExtAgents = agents.Where(a => a.Type == Interfaces.AgentType.ForExternalSub)
                                    .OrderBy(a => a.State == Interfaces.AgentStates.Connected ? "0" : "1")
                                    .ThenBy(a => a.MainService ? "0" : "1")
                                    .GroupBy(a => a.DependencyName)
                                    .Select(g => g.First()).ToList();

                                if (ExtAgents.Count > 0)
                                {
                                    /** Se genera una tabla que es la suma de las tablas de los agentes obtenidos */
                                    var ExtAgent = new Agentes.PSExternalAgent() { name = "EXT-USERS" };
                                    PSHelper.LOGGER.Trace<U5kPresService>(String.Format("FrameSenderTask Sending External Subs ({0})", ExtAgent.rsCount));
                                    ExtAgents.ForEach(a =>
                                    {
                                        /** Introducir en el nombre de los recursos las IP de los Proxies */
                                        var newTable = from rs in a.RsTable
                                                       select new PresenceServerResource(rs)
                                                       {
                                                           name = String.Format("<sip:{0}@{1}>", rs.name, a.ProxyEndpoint.ToString())
                                                       };
                                        ExtAgent.RsTable.AddRange(/*a.RsTable*/newTable);
                                    });
                                    /** Actualiza la version */
                                    ExtAgent.version = Agentes.PSExternalAgent.GlobalVersion;
                                    mtrama = ExtAgent.Frame;
                                    _udpClient.Send(mtrama, mtrama.Count(), mcastTifx);
                                }
                                SendingControl[typeof(PSExternalAgent).Name] = false;
                            }

                            if (SendingControl[typeof(PSProxiesAgent).Name] == true)
                            {
                                PSHelper.LOGGER.Trace<U5kPresService>(String.Format("FrameSenderTask Sending Global Proxies ({0})", ((Agentes.PSBaseAgent)global_agent).rsCount));
                                /** Enviar Trama Global */
                                mtrama = global_agent.Frame;
                                _udpClient.Send(mtrama, mtrama.Count(), mcastTifx);
                                SendingControl[typeof(PSProxiesAgent).Name] = false;
                            }
#if _ACTIVATE_EXRES_
                            /** EXRES Gestionar el envío de la informacion de recursos de telefonía externos */
                            if (SendingControl[typeof(PSExternalResourcesAgent).Name] == true)
                            {
                                Interfaces.IAgent agent =
                                    agents.Where(a => a.Type == Interfaces.AgentType.ForExternalResources)
                                    .FirstOrDefault();
                                if (agent != null)
                                {
                                    mtrama = agent.Frame;
                                    _udpClient.Send(mtrama, mtrama.Count(), mcastTifx);
                                    LogTrace<U5kPresService>(String.Format("FrameSenderTask Sending External Resources ({0})",
                                        ((Agentes.PSBaseAgent)agent).rsCount));
                                }
                                SendingControl[typeof(PSExternalResourcesAgent).Name] = false;
                            }
#endif
                        }
                        catch (Exception x)
                        {
                            LogException<U5kPresService>("FrameSenderTask Exception", x, false);
                            PSHelper.LOGGER.Trace<U5kPresService>(String.Format("FrameSenderTask Exception: ({0})", x.Message));
                        }
                    }
                    smpAccesMain.Release();
                }
#if _WITH_EVENTQUEUE_
            });
#endif
                /** Control de envio por tiempo... */
                TimeSpan elapsed = DateTime.Now - last;
                TimeSpan tick = new TimeSpan(0, 0, PSHelper.LocalParameters.MulticastSenderTick);
                /** Control de puesta en hora (SYNC NTP) hacia atras... */
                if (elapsed < TimeSpan.Zero || elapsed >= tick)
                {
                    SendingControl[typeof(PSBkkAgent).Name] = true;
                    SendingControl[typeof(PSProxiesAgent).Name] = true;
                    SendingControl[typeof(PSProxiesAgent).Name] = true;
#if _ACTIVATE_EXRES_
                    /** EXRES Activar el Envio de la información de recursos externos de telefonía */
                    SendingControl[typeof(PSExternalResourcesAgent).Name] = true;
#endif
                    last = DateTime.Now;
                }
            }
            LogInfo<U5kPresService>(String.Format("FrameSenderTask Ended"));
        }
    }
}

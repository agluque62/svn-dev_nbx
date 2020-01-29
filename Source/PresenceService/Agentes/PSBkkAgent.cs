using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using U5ki.Infrastructure;

using U5ki.PresenceService.Interfaces;
using U5ki.PresenceService.Engines;

namespace U5ki.PresenceService.Agentes
{
    public class PSBkkAgent : PSBaseAgent
    {
        /// <summary>
        /// 
        /// </summary>
        public PSBkkAgent()
        {
            type = AgentType.ForInternalSub;
            name = "INT_USERS";
        }

        #region Overrides
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnEventOccurred"></param>
        public override void Init(EventHandler<Interfaces.AgentEventArgs> OnEventOccurred, object ocfg)
        {
            try
            {
                PSHelper.LOGGER.Trace<PSBkkAgent>(String.Format("Initializing Bkk agent.."));
                Debug.Assert(ocfg != null && ProxyEndpoint != null /*&& PresenceEndpoint != null*/);

                base.Init(OnEventOccurred, ocfg);
                
                Cd40Cfg cfg = (Cd40Cfg)ocfg;

                /** Exploro el Plan de Asignacion de Recursos... por DependencyName */
                List<AsignacionRecursosGW> InternalRs = cfg.ConfiguracionGeneral.PlanAsignacionRecursos.
                    Where(rs => rs.IdHost == DependencyName ).ToList();

                /** Genero la nueva lista de abonados*/
                List<PresenceServerResource> new_rsTable = InternalRs.Select(rs =>
                    new PresenceServerResource() 
                    { 
                        Dependency = DependencyName,
                        name = rs.IdRecurso,
                        type = RsChangeInfo.RsTypeInfo.InternalSub,
                        KeyOfPersistence = rs.IdRecurso,
                        Status = PresenceServerResource.PersistenceOfStates.Get(rs.IdRecurso, RsStatus.NotAvailable),
                        PresenceAdd = ProxyEndpoint/*.Address*/.ToString() // TODO. PresenceEndpoint ???
                    }
                ).ToList();

                if (smpRsTableAccess.Acquire())
                {
                    rsTable = new List<PresenceServerResource>(new_rsTable);
                    name = ProxyEndpoint.Address.ToString();
                    smpRsTableAccess.Release();
                }

                if (PresenceEndpoint != null)
                {
                    /** TODO. Esto ya debería venir en la configuracion */
                    PresenceEndpoint.Port = 8080;
                    engine = new BkkEngine() { WsEndpoint = PresenceEndpoint };
                    engine.Init(OnBkkEvent);
                }
                else
                {
                    engine = null;
                }
            }
            catch (Exception x)
            {
                ExceptionManager(x, "PSBkkAgent Init Exception");
            }
            finally
            {
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            PSHelper.LOGGER.Trace<PSBkkAgent>(String.Format("Disposing Bkk agent [{0}]", name));
            base.Dispose();
            ProxyEndpoint = PresenceEndpoint = null;
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            PSHelper.LOGGER.Trace<PSBkkAgent>(String.Format("Starting Bkk agent [{0}]", name));
            base.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void TickOnDisconnected()
        {
            PSHelper.LOGGER.Trace<PSBkkAgent>(String.Format("TickOnDisconnected on Bkk agent.."));
            base.TickOnDisconnected();
        }
        #endregion

        void OnBkkEvent(object sender, AgentEngineEventArgs eventOcurred)
        {
            try
            {
                PSHelper.LOGGER.Trace<PSBkkAgent>(String.Format("BkkEvent on Bkk agent: [{0},{1}]", eventOcurred.ev, eventOcurred.idsub));

                bool refresh = false;
                switch (eventOcurred.ev)
                {
                    case AgentEngineEvents.Open:
                        break;
                    case AgentEngineEvents.Closed:
                    case AgentEngineEvents.Error:
                        // Fallo en WebSocket Service.
                        // Poner todos los recursos desconectados...
                        if (smpRsTableAccess.Acquire())
                        {
                            refresh = rsTable.Where(rs => rs.Status != RsStatus.NotAvailable).ToList().Count > 0 ? true : false;
                            rsTable.ForEach(rs => rs.Status = RsStatus.NotAvailable);
                            this.version = this.version + 1;
                            smpRsTableAccess.Release();
                        }
                        break;

                    case AgentEngineEvents.UserUnregistered:
                        refresh = UserStatusSet(eventOcurred.idsub, false, RsStatus.NoInfo);
                        break;

                    case AgentEngineEvents.UserRegistered:
                        refresh = UserStatusSet(eventOcurred.idsub, true, RsStatus.NoInfo);
                        break;

                    case AgentEngineEvents.UserAvailable:
                        refresh = UserStatusSet(eventOcurred.idsub, true, RsStatus.Available);
                        break;

                    case AgentEngineEvents.UserBusy:
                        refresh = UserStatusSet(eventOcurred.idsub, true, RsStatus.Busy);
                        break;

                    case AgentEngineEvents.UserBusyUninterrupted:
                        refresh = UserStatusSet(eventOcurred.idsub, true, RsStatus.BusyUninterruptible);
                        break;

                    default:
                        break;
                }
                /** Se ha cambiado el estado de un recurso. Genero el evento 'refresh', para acelerar el envio de la trama */
                if (refresh == true)
                {
                    if (smpRsTableAccess.Acquire())
                    {
                        OnAgentEventOccurred(
                            new AgentEventArgs()
                            {
                                agent = this,
                                ev = AgentEvents.Refresh,
                                p1 = UserInfoGet(eventOcurred.idsub)
                            });
                        smpRsTableAccess.Release();
                    }
                }
            }
            catch (Exception x)
            {
                ExceptionManager(x, "PSBkkAgent OnBkkEvent Exception");
            }
            finally
            {
            }
        }
    }
}

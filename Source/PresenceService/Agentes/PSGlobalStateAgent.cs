using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using U5ki.PresenceService.Interfaces;
using U5ki.PresenceService.Engines;

namespace U5ki.PresenceService.Agentes
{
    class PSProxiesAgent : PSBaseAgent
    {
        #region Helpers

        U5ki.Infrastructure.RsChangeInfo.RsTypeInfo RsType(IAgent ag)
        {
            var type = ag.Type == AgentType.ForInternalSub ?    
                (ag.MainService ? U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.InternalProxy : U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.InternalAltProxy) :    
                (ag.MainService ? U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.ExternalProxy : U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.ExternalAltProxy);
            return type;
        }

        string KeyOfPersistence(IAgent ag)
        {
            return String.Format("<{0},{2},[{1}]>", ag.DependencyName, ag.ProxyEndpoint, RsType(ag));
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public PSProxiesAgent(string serviceSite)
        {
            type = AgentType.ForProxies;
            name = "PROXIES";
        }

        #region Overrides
        public override void Init(EventHandler<AgentEventArgs> OnEventOccurred, object Cfg)
        {
            try
            {
                PSHelper.LOGGER.Trace<PSProxiesAgent>(String.Format("Initializing Proxies agent..."));
                Debug.Assert(Cfg != null /*&& ProxyEndpoint != null && PresenceEndpoint != null*/);

                base.Init(OnEventOccurred, Cfg);

                /** Relleno la lista de Agentes */
                agents = (List<IAgent>)Cfg;

                /** Genero la nueva lista de abonados */
                List<PresenceServerResource> new_rsTable = agents.Select(ag =>
                    new PresenceServerResource() 
                    {
                        Dependency = ag.DependencyName,
                        name = ag.ProxyEndpoint/*.Address*/.ToString(), 
                        type = RsType(ag),
                        //ag.Type==AgentType.ForInternalSub ?
                        //    (ag.MainService ? U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.InternalProxy : U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.InternalAltProxy) :
                        //    (ag.MainService ? U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.ExternalProxy : U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.ExternalAltProxy),
                        KeyOfPersistence = KeyOfPersistence(ag),
                        Status = PresenceServerResource.PersistenceOfStates.Get(KeyOfPersistence(ag), RsStatus.NotAvailable)
                    }).ToList();

                if (smpRsTableAccess.Acquire())
                {
                    rsTable = new List<PresenceServerResource>(new_rsTable);
                    this.version = version + 1;
                    smpRsTableAccess.Release();
                }
            }
            catch (Exception x)
            {
                ExceptionManager(x, "PSGlobalAgent Init Exception");
            }
            finally
            {
            }
        }

        public override void Dispose()
        {
                PSHelper.LOGGER.Trace<PSProxiesAgent>(String.Format("Disposing Proxies agent [{0}]", name));
            base.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            /** */
            PSHelper.LOGGER.Trace<PSProxiesAgent>(String.Format("Startig (deferred 3 s) Proxies agent [{0}]", name));
            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(3000);
                base.Start();
                base.ActivateAgent();
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override bool IsConnected()
        {
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void TickOnConnected()
        {
            PSHelper.LOGGER.Trace<PSProxiesAgent>(String.Format("TickOnConnected on Proxies agent [{0}]", name));
            // Actualizar estados y enviar eventos.... 
            if (smpRsTableAccess.Acquire())
            {
                rsTable.ForEach(res =>
                {
                    IAgent agent = agents.Where(ag => res.name == ag.ProxyEndpoint/*.Address*/.ToString()).FirstOrDefault();
                    if (agent != null)
                    {
                        RsStatus nextStatus = agent.State == AgentStates.Connected ? RsStatus.Available : RsStatus.NotAvailable;
                        if (nextStatus != res.Status)
                        {
                            res.Status = PresenceServerResource.PersistenceOfStates.Set(res.KeyOfPersistence, nextStatus);
                            res.version = res.version + 1;
                            res.last_set = DateTime.Now.Ticks;

                            /** */
                            this.version = version + 1;

                            /** Evento para acelerar el envio de trama */
                            OnAgentEventOccurred(
                                new AgentEventArgs()
                                {
                                    agent = this,
                                    ev = AgentEvents.Refresh,
                                    p1 = UserInfoGet(res.name)
                                });
                        }
                    }
                });
                smpRsTableAccess.Release();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        protected override void UsersStatusSupervision(int seconds)
        {
            // base.UsersStatusSupervision(seconds);
        }

        public override bool RsSelect(PresenceServerResource rs)
        {
            return true;
        }

        #endregion

        /** Referencia a la lista de Agentes supervisados */
        protected List<Interfaces.IAgent> agents = null;
    }
}

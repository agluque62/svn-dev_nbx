using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using U5ki.PresenceService;
using U5ki.PresenceService.Interfaces;
using U5ki.PresenceService.Engines;

using U5ki.Infrastructure;


namespace U5ki.PresenceService.Agentes
{
    public class PSExternalResourcesAgent : PSBaseAgent
    {
        public PSExternalResourcesAgent()
        {
            type = AgentType.ForExternalResources;
            name = "EXT_RES_AGENT";
            DependencyName = "ExternalResources";
        }

        #region overrides

        public override void Init(EventHandler<AgentEventArgs> OnEventOccurred, object ocfg)
        {
            base.Init(OnEventOccurred, ocfg);

            TimeoutNotConnected = TimeSpan.FromSeconds(120);
            TimeoutConnected = TimeSpan.FromSeconds(30);
            TimeoutOnInactiveResource = 90;

            Cd40Cfg cfg = (Cd40Cfg)ocfg;
            var resources = (from equipo in cfg.ConfiguracionGeneral.PlanDireccionamientoIP
                             where equipo.TipoHost == Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA &&
                                   equipo.EsCentralIP == false
                             join resource in cfg.ConfiguracionGeneral.PlanAsignacionRecursos on equipo.IdHost equals resource.IdHost
                             select
                                new PresenceServerResource()
                                {
                                    Dependency = DependencyName,
                                    name = resource.IdRecurso,
                                    type = RsChangeInfo.RsTypeInfo.PhLine,
                                    KeyOfPersistence = resource.IdRecurso,
                                    Status = PresenceServerResource.PersistenceOfStates.Get(resource.IdRecurso, RsStatus.NotAvailable),
                                    PresenceAdd = PSHelper.EndPointFrom(equipo.IpRed1, (int)resource.SipPort)
                                }).ToList();

            if (smpRsTableAccess.Acquire())
            {
                rsTable = new List<PresenceServerResource>(resources);
                smpRsTableAccess.Release();
            }
            LastTickProccesed = DateTime.MinValue;
        }

        protected override bool IsConnected()
        {
            return true;
        }

        protected override void TickOnConnected()
        {
            PSHelper.LOGGER.Trace<PresenceServerResource>(String.Format("TickOnConnected on Proxies agent [{0}]", name));
            base.TickOnConnected();

            // Envia Options...
            if (smpRsTableAccess.Acquire())
            {
                rsTable.ForEach(res =>
                {
                    /** Genero la peticion de OPTIONS */
                    OnAgentEventOccurred(new AgentEventArgs()
                    {
                        agent = this,
                        ev = AgentEvents.ResourceOptions,
                        p1 = res.Uri,
                        retorno = (callid) => { CallidControl[res.Uri] = (callid as string); }
                    });

                });
                smpRsTableAccess.Release();
            }
        }

        protected override void TickOnDisconnected()
        {
            ActivateAgent();
            TickOnConnected();
        }

        public override void PingResponse(string from, string callid, AgentStates res, int code=200)
        {
            try
            {
                if (smpRsTableAccess.Acquire())
                {
                    var resource = rsTable.Where(r => CallidControl.ContainsKey(r.Uri) && CallidControl[r.Uri] == callid).FirstOrDefault();
                    if (resource != null)
                    {
                        var conected = res == AgentStates.Connected && code == 200;
                        CallidControl[resource.Uri] = "";

                        if (resource.ProcessResult(conected))
                        {
                            /** Activar o Desactivar el Recurso */
                            if (UserStatusSet(resource, conected ? RsStatus.Available : RsStatus.NotAvailable)==true)
                            {
                                /** Para acelerar el refresco del cambio */
                                OnAgentEventOccurred(
                                    new AgentEventArgs()
                                    {
                                        agent = this,
                                        ev = AgentEvents.Refresh,
                                        p1 = UserInfoGet(resource.name)
                                    });
                            }
                        }
                    }
                    smpRsTableAccess.Release();
                }
            }
            catch (Exception x)
            {
                ExceptionManager(x, "PSBaseAgent PingResponse Exception");
            }
            finally
            {
            }
        }

        public override bool RsSelect(PresenceServerResource rs)
        {
            return true;
        }

        #endregion overrides


        #region Especificos

        protected bool UserStatusSet(PresenceServerResource rs, RsStatus StatusInfo)
        {
            RsStatus next_status = StatusInfo == RsStatus.NoInfo ?
                (rs.Status == RsStatus.NotAvailable ? RsStatus.Available : rs.Status) : StatusInfo;
            if (next_status != rs.Status)
            {
                rs.Status = next_status;
                rs.version += 1;

                this.version += 1;
                rs.last_set = DateTime.Now.Ticks;
                return true;
            }
            return false;
        }
        Dictionary<string, string> CallidControl = new Dictionary<string, string>();

        #endregion

    }
}

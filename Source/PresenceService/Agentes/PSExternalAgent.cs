using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using U5ki.PresenceService.Interfaces;
using U5ki.PresenceService.Engines;

using U5ki.Infrastructure;

namespace U5ki.PresenceService.Agentes
{
    public class PSExternalAgent : PSBaseAgent
    {
        /** Para notificar cambios en la trama agregada... */
        public static int GlobalVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        protected class SubsRange
        {
            public Int64 Max { get; set; }
            public Int64 Min { get; set; }
            public static bool TestRanges(string rec, string Abonado, List<SubsRange> ranges)
            {
                try
                {
                    Int64 iAbonado = Convert.ToInt64(Abonado);
                    foreach (SubsRange r in ranges)
                    {
                        if (iAbonado >= r.Min && iAbonado <= r.Max)
                            return true;
                    };
                }
                catch (Exception x)
                {
                }
                return false;
            }
            public List<string> Enumerate
            {
                get
                {
                    List<string> res = new List<string>();

                    for (var i = Min; i <= Max; i++)
                        res.Add(i.ToString());

                    return res;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public PSExternalAgent()
        {
            type = AgentType.ForExternalSub;
            name = "EXT_AGENT";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected bool Test()
        {
            return true;
        }

        #region Overrides
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnEventOccurred"></param>
        /// <param name="ocfg"></param>
        public override void Init(EventHandler<AgentEventArgs> OnEventOccurred, object ocfg)
        {
            try
            {
                PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Initializing External agent.."));

                Debug.Assert(ocfg != null && ProxyEndpoint != null /*&& PresenceEndpoint != null*/);

                base.Init(OnEventOccurred, ocfg);

                Cd40Cfg cfg = (Cd40Cfg)ocfg;

                /** Obtengo el rango asociado a la dependencia */
                var rangos = cfg.ConfiguracionGeneral.PlanDireccionamientoIP.
                    Where(dep => dep.IdHost == DependencyName).
                    Select(dep1 => new SubsRange() { Max = dep1.Max, Min = dep1.Min }).ToList();

                List<PresenceServerResource> new_rsTable = null;

                if (Properties.Settings.Default.MonitorAllUsersOfDependencies == true)
                {
                    /** Obtengo los usuarios de los rangos.. */
                    new_rsTable = rangos.SelectMany(r => r.Enumerate)
                        .Select(rs =>
                                new PresenceServerResource()
                                {
                                    Dependency = DependencyName,
                                    name = rs,
                                    type = RsChangeInfo.RsTypeInfo.ExternalSub,
                                    KeyOfPersistence = string.Format("<sip:{1}@{0}>", ProxyEndpoint.ToString(), rs),
                                    Status = PresenceServerResource.PersistenceOfStates.Get(string.Format("<sip:{1}@{0}>", ProxyEndpoint.ToString(), rs),
                                             RsStatus.NotAvailable),
                                    PresenceAdd = PresenceEndpoint == null ? "" : PresenceEndpoint/*.Address*/.ToString() // TODO. PresenceEndpoint ???
                                }).ToList();
                }
                else
                {
                    /** Obtengo los Destinos de los Enlaces Telefonicos de los Usuarios, tipo ATS, cuyos rangos esten
                     *  en la Dependencia. */
                    new_rsTable = cfg.ConfiguracionUsuarios.
                       SelectMany(u => u.TlfLinks).
                       SelectMany(lnk => lnk.ListaRecursos).
                       Where(rec =>
                           (rec.Prefijo == 3 || rec.Prefijo == 2) && /*PresenceEndpoint != null &&*/
                           SubsRange.TestRanges(rec.NombreRecurso, rec.NumeroAbonado, rangos)).
                       Select(rs =>
                               new PresenceServerResource()
                               {
                                   Dependency = DependencyName,
                                   name = rs.NumeroAbonado,
                                   type = RsChangeInfo.RsTypeInfo.ExternalSub,
                                   KeyOfPersistence = string.Format("<sip:{1}@{0}>", ProxyEndpoint.ToString(), rs.NumeroAbonado),
                                   Status = PresenceServerResource.PersistenceOfStates.Get(string.Format("<sip:{1}@{0}>", ProxyEndpoint.ToString(), rs.NumeroAbonado),
                                            RsStatus.NotAvailable),
                                   PresenceAdd = PresenceEndpoint == null ? "" : PresenceEndpoint/*.Address*/.ToString() // TODO. PresenceEndpoint ???
                                }).
                       GroupBy(p => p.name).
                       Select(g => g.First()).
                       ToList();
                }

                if (smpRsTableAccess.Acquire())
                {
                    rsTable = new List<PresenceServerResource>(new_rsTable);
                    GlobalVersion = GlobalVersion + 1;
                    smpRsTableAccess.Release();
                }

                name = ProxyEndpoint.Address.ToString();
                /** */
                PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Init Event on {0}({2},{3}. {1} Resources",
                    DependencyName, new_rsTable.Count,
                    ProxyEndpoint, PresenceEndpoint));
            }
            catch (Exception x)
            {
                PSHelper.LOGGER.Error<PSExternalAgent>(String.Format("Excepcion {0} on Init Dependency {1} ({2},{3}",
                    x.Message,
                    DependencyName,
                    ProxyEndpoint,
                    PresenceEndpoint != null ? PresenceEndpoint.ToString() : "No Presence Server"));
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
            PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Disposing External agent [{0}]", name));

            if (State == AgentStates.Connected && smpRsTableAccess.Acquire())
            {
                /** Destruyo las subscripciones */
                /** 20180302. Solo si tiene servidor de Presencia configurado */
                if (PresenceEndpoint != null)
                {
                    rsTable.ForEach(rs =>
                    {
                        OnAgentEventOccurred(
                            new AgentEventArgs()
                            {
                                agent = this,
                                ev = AgentEvents.UnsubscribeUser,
                                p1 = rs.Uri
                            });

                        PSHelper.LOGGER.Trace<PSExternalAgent>(
                            String.Format("UnSubscribe to Presence of {0} on {1}", rs.Uri, PresenceEndpoint));
                    });
                }
                smpRsTableAccess.Release();
            }
            base.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Starting External agent [{0}]", name));
            base.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        protected override void UsersStatusSupervision(int seconds)
        {
            // base.UsersStatusSupervision(seconds);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sipuriUser"></param>
        /// <param name="available"></param>
        public override void PresenceEventOcurred(string sipuriUser, bool available)
        {
            base.PresenceEventOcurred(sipuriUser, available);

            if (smpRsTableAccess.Acquire())
            {
                /** */
                PresenceServerResource res = rsTable.Where(rs => rs.Uri == sipuriUser).FirstOrDefault();
                if (res != null)
                {
                    RsStatus nextStatus = available ? RsStatus.Available : RsStatus.NotAvailable;

                    PSHelper.LOGGER.Trace<PSExternalAgent>(
                        String.Format("PresenceEventOcurred Event on [{0} ({1},{2})] Gestionado en: {3}: {4}, {5} ({6}) => {7}",
                        DependencyName, ProxyEndpoint, PresenceEndpoint,
                        sipuriUser, available,
                        res.name, res.status, nextStatus));

                    if (nextStatus != res.Status)
                    {
                        res.Status = PresenceServerResource.PersistenceOfStates.Set(res.KeyOfPersistence, nextStatus);
                        res.version = res.version + 1;
                        res.last_set = DateTime.Now.Ticks;

                        /** */
                        this.version = this.version + 1;
                        GlobalVersion = GlobalVersion + 1;

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
                smpRsTableAccess.Release();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        public override bool RsSelect(PresenceServerResource rs)
        {
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void ResetAllResources(RsStatus to = RsStatus.Available)
        {
            GlobalVersion = GlobalVersion + 1;
            if (smpRsTableAccess.Acquire())
            {
                rsTable.ForEach(rs =>
                {
                    if (rs.status != RsStatus.Available)
                    {
                        rs.Status = to;
                        rs.version = rs.version + 1;
                    }
                });
                smpRsTableAccess.Release();
            }
        }
        /** */
        protected override void ActivateAgent()
        {
            PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Activatin External agent [{0}]", name));
            base.ActivateAgent();

            if (smpRsTableAccess.Acquire())
            {
                rsTable.ForEach(rs =>
                {
                    if (PresenceEndpoint != null)
                    {
                        /** Efectuar las Subscripciones */
                        OnAgentEventOccurred(
                            new AgentEventArgs()
                            {
                                agent = this,
                                ev = AgentEvents.SubscribeUser,
                                p1 = rs.Uri
                            });

                        PSHelper.LOGGER.Trace<PSExternalAgent>(
                            String.Format("Subscribe to Presence of {0} on {1}", rs.Uri, PresenceEndpoint));
                    }
                    else
                    {
                        rs.Status = RsStatus.Available;
                    }
                });
                smpRsTableAccess.Release();
            }
        }
        /** */
        protected override void DeactivateAgent()
        {
            PSHelper.LOGGER.Trace<PSExternalAgent>(String.Format("Deactivating External agent [{0}]", name));
            base.DeactivateAgent();

            if (smpRsTableAccess.Acquire())
            {
                /** Destruyo las subscripciones */
                rsTable.ForEach(rs =>
                {
                    rs.Status = RsStatus.NotAvailable;
                    if (PresenceEndpoint != null)
                    {
                        OnAgentEventOccurred(
                            new AgentEventArgs()
                            {
                                agent = this,
                                ev = AgentEvents.UnsubscribeUser,
                                p1 = rs.Uri
                            });

                        PSHelper.LOGGER.Trace<PSExternalAgent>(
                            String.Format("UnSubscribe to Presence of {0} on {1}", rs.Uri, PresenceEndpoint));
                    }
                });
                smpRsTableAccess.Release();
            }
        }

        /** */
        /// 20180710. Sirve para poder Resetear la tabla de usuarios a 'Not Available', en cambios de CFG que 'desactivan' en proxy
        int _agentDeactivatedCounter = 0;
        protected override void ConfirmAgentDeactivated()
        {
            if (++_agentDeactivatedCounter > 1)
            {
                ResetAllResources(RsStatus.NotAvailable);
                _agentDeactivatedCounter = 0;
            }
        }
        #endregion
    }
}

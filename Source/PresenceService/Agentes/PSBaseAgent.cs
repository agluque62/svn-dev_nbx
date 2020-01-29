using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using System.Net;
using System.Runtime.InteropServices;

using U5ki.Infrastructure;

using U5ki.PresenceService.Interfaces;

namespace U5ki.PresenceService.Agentes
{
    public abstract class PSBaseAgent : BinaryResource, IAgent
    {
        protected enum PingingState { Pending, Ok, Fail }

        #region IAgent

        public IAgentEngine engine { get; set; }
        public IPEndPoint ProxyEndpoint { get; set; }
        public IPEndPoint PresenceEndpoint { get; set; }
        public event EventHandler<AgentEventArgs> EventOccurred;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnEventOccurred"></param>
        public virtual void Init(EventHandler<AgentEventArgs> OnEventOccurred, object Cfg)
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>("Initializing base agent...");
            EventOccurred = null;
            EventOccurred += OnEventOccurred;

            engine = null;
            State = AgentStates.NotConnected;

            TimeoutNotConnected = new TimeSpan(0, 0, PSHelper.LocalParameters.TimeoutOnDisconnected);
            TimeoutConnected = new TimeSpan(0, 0, PSHelper.LocalParameters.TimeoutOnConnected);

            wkTimer = new Timer();
            wkTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Start()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Starting base agent: [{0}]", this.name));

            wkTimer.Interval = 100;
            wkTimer.Enabled = true;
            last = DateTime.MinValue;
#if DEBUG1
            tm = new PresenceServiceHelper.TimeMeasurement(Name + ": TimeToStart");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        public virtual bool RsSelect(PresenceServerResource rs)
        {
            return rs.Status != RsStatus.NotAvailable;
        }
        /// <summary>
        /// 
        /// </summary>
        public byte[] Frame
        {
            get
            {
                byte[] frame = new byte[1];
                int offset = 0;

                if (smpRsTableAccess.Acquire())
                {
                    try
                    {
                        /** Seleccionar los activos */
                        List<PresenceServerResource> ActiveRsList = rsTable.Where(rs => RsSelect(rs)).ToList();

                        rsCount = ActiveRsList.Count;

                        offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)type));
                        offset = CopyString2ByteArray(ref frame, offset, name, defMaxNameLenght);
                        offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)rsCount));
                        offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)version));

                        ActiveRsList.ForEach(rs =>
                        {
                            byte[] rsFrame = rs.Frame;

                            Array.Resize(ref frame, offset + rsFrame.Length);
                            rsFrame.CopyTo(frame, offset);
                            offset += rsFrame.Length;
                        });
                    }
                    catch (Exception x)
                    {
                        ExceptionManager(x, "PSBaseAgent Frame Exception");
                    }
                    finally
                    {
                        smpRsTableAccess.Release();
                    }
                }
                return frame;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Disposing Base Agent: [{0}]", this.name));
            wkTimer.Enabled = false;
            /** 20180724. Evita que se produzca un error al tratar eventos del timer que ya estan en la cola al invocar esta funcion */
            //wkTimer.Dispose();

            if (engine != null)
                engine.Dispose();

            EventOccurred = null;

            if (smpRsTableAccess.Acquire())
            {
                rsTable.Clear();
                smpRsTableAccess.Release();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public AgentType Type { get { return type; } }
        public String Name { get { return name; } }
        public AgentStates State { get; set; }
        public bool MainService { get; set; }
        public string DependencyName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idfrom"></param>
        /// <param name="res"></param>
        protected PingingState pingState = PingingState.Fail;
        public string callIdOptions { get; set; }
        public virtual void PingResponse(string from, string callid, AgentStates res)
        {
            try
            {
                ///** Los PING siempre van a la direccion del Proxy */
                //Utilities.SipUtilities.SipUriParser UriFrom = new Utilities.SipUtilities.SipUriParser(from);
                //if (ProxyEndpoint != null && ProxyEndpoint.ToString().Contains(UriFrom.Dominio)==true)
                //if (ProxyEndpoint != null && from.Contains(ProxyEndpoint.Address.ToString()) == true)
                if (callid == callIdOptions)
                {
                    PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Options Response on base agent [{0},{1},{2}]", name, from, res));

                    pingState = res == AgentStates.Connected ? PingingState.Ok : PingingState.Fail;

                    if (pingState == PingingState.Ok && State == AgentStates.NotConnected)
                    {
                        /** Conexion del servicio */
                        ActivateAgent();
                    }
                    else if (pingState == PingingState.Fail)
                    {
                        if (State == AgentStates.Connected)
                        {
                            /** Desconexion del servicio */
                            DeactivateAgent();
                        }
                        else
                        {
                            /** 20180710. Al cargar una sectorizacion que 'desactiva' al proxy se deben borrar los estados...
                             * En este caso limpio la tabla de recursos */
                            ConfirmAgentDeactivated();
                        }
                    }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sipuriUser"></param>
        /// <param name="available"></param>
        public virtual void PresenceEventOcurred(string sipuriUser, bool available)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string ProxyOptionsUri
        {
            get
            {
                if (ProxyEndpoint != null)
#if DEBUG1
                    return String.Format("<sip:{1}{0}>", ProxyEndpoint./*Address.*/ToString(), "SimProxy@");
#else
                    return String.Format("<sip:{1}{0}>", ProxyEndpoint./*Address.*/ToString(), "");
#endif
                return "";
            }
        }

        #endregion

        #region Binario

        [MarshalAs(UnmanagedType.I4)]
        public AgentType type = AgentType.ForInternalSub;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = defMaxNameLenght)]
        public string name = "INTERNAL_PROXY";
        [MarshalAs(UnmanagedType.I4)]
        public int rsCount = 0;
        [MarshalAs(UnmanagedType.I4)]
        public int version = 0;

        private const int defMaxNameLenght = 32 + 4;
        #endregion

        #region Recursos

        protected List<PresenceServerResource> rsTable = new List<PresenceServerResource>();
        protected PSHelper.ManagedSemaphore smpRsTableAccess = new PSHelper.ManagedSemaphore(1, 1, "AgentDataAccess", 5000);
        //protected PSHelper.DummySemaphore smpRsTableAccess = new PSHelper.DummySemaphore(1, 1, "AgentDataAccess", 5000);
        public List<PresenceServerResource> RsTable
        {
            get { return rsTable; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="RegisteredInfo"></param>
        /// <param name="StatusInfo"></param>
        /// <returns></returns>
        protected virtual bool UserStatusSet(string userid, bool RegisteredInfo, RsStatus StatusInfo)
        {
            bool refresh = false;
            if (smpRsTableAccess.Acquire())
            {
                try
                {
                    PresenceServerResource rs = rsTable.Find(rsin => rsin.name == userid);
                    if (rs != null)
                    {
                        if (RegisteredInfo == false)
                        {
                            refresh = rs.Status != RsStatus.NotAvailable;
                            rs.Status = RsStatus.NotAvailable;
                            rs.last_set = 0;

                            this.version = this.version + 1;
                        }
                        else
                        {
                            RsStatus next_status = StatusInfo == RsStatus.NoInfo ?
                                (rs.Status == RsStatus.NotAvailable ? RsStatus.Available : rs.Status) : StatusInfo;
                            if (next_status != rs.Status)
                            {
                                rs.Status = next_status;
                                rs.version = rs.version + 1;
                                refresh = true;

                                this.version = this.version + 1;
                            }
                            rs.last_set = DateTime.Now.Ticks;
                        }
                    }
                }
                catch (Exception x)
                {
                    ExceptionManager(x, "PSBaseAgent UserStatusSet Exception");
                }
                finally
                {
                    smpRsTableAccess.Release();
                }
            }
            return refresh;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        protected virtual void UsersStatusSupervision(int seconds)
        {
            if (smpRsTableAccess.Acquire())
            {
                try
                {
                    long now = DateTime.Now.Ticks;
                    long ts = (new TimeSpan(0, 0, seconds)).Ticks;

                    rsTable.ForEach(rs =>
                    {
                        if (rs.Status != RsStatus.NotAvailable)
                        {
                            long diff = now - rs.last_set;
                            if (diff > ts)
                            {
                                PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Deativating user by timeout on base agentt. [{0},{1},{2}]", name, rs.Dependency, rs.name));
                                rs.Status = RsStatus.NotAvailable;
                            }
                        }
                    });
                }
                catch (Exception x)
                {
                }
                finally
                {
                    smpRsTableAccess.Release();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void ResetAllResources(RsStatus to = RsStatus.NotAvailable)
        {
            if (smpRsTableAccess.Acquire())
            {
                rsTable.ForEach(rs =>
                {
                    if (rs.Status != RsStatus.NotAvailable)
                    {
                        rs.Status = to;
                        rs.version = rs.version + 1;
                        //Envia los cambios de los recursos dependientes jerarquicamente
                        rs.last_set = DateTime.Now.Ticks;
                        this.version = this.version + 1;
                        OnAgentEventOccurred(
                        new AgentEventArgs()
                        {
                            agent = this,
                            ev = AgentEvents.Refresh,
                            p1 = UserInfoGet(rs.name)
                        });
                    }
                });
                smpRsTableAccess.Release();
            }
        }

        #endregion

        #region Trabajo

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        protected string UserInfoGet(string userid)
        {
            string userInfo = String.Format("Res {0}: Not Found", userid);
            PresenceServerResource rs = rsTable.Find(rsin => rsin.name == userid);
            if (rs != null)
                userInfo = String.Format("Res {0}: [T:{1}], [S:{2}], [V:{3}]", userid, rs.type, rs.status, rs.version);
            return userInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int pingCount = 0;
        protected virtual bool IsConnected()
        {
            bool status = pingState == PingingState.Ok;
            if (ProxyEndpoint != null)
            {
                pingState = PingingState.Pending;
                var ev = new AgentEventArgs()
                {
                    agent = this,
                    ev = AgentEvents.Ping,
                    p1 = ProxyOptionsUri
                };
                OnAgentEventOccurred(ev);
            }
            return status;
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void TickOnConnected()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("TickOnConnected in base agent: [{0}]", name));

            if (!IsConnected())
            {
                DeactivateAgent();
            }
            else
            {
                // Supervisar la Subcripcion...
                if (engine != null && !engine.Available)
                {
                    engine.Start();
                }
                else
                {
                    UsersStatusSupervision(PSHelper.LocalParameters.TimeoutOnInactiveResource);
                }
                State = AgentStates.Connected;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void TickOnDisconnected()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("TickOnDisconnected in base agent: [{0}]", name));

            if (IsConnected())
            {
                ActivateAgent();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual object OnAgentEventOccurred(AgentEventArgs e)
        {
            EventHandler<AgentEventArgs> handler = EventOccurred;
            if (handler != null)
            {
                handler(this, e);
                return e.retorno;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void ActivateAgent()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Activating agent in base agent: [{0}]", this.name));

            // Arrancar la Subcripcion...
            if (engine != null && !engine.Available)
            {
                engine.Start();
            }
            State = AgentStates.Connected;
            // Evento de Conexion al servicio
            OnAgentEventOccurred(
                new AgentEventArgs()
                {
                    agent = this,
                    ev = AgentEvents.Active,
                    p1 = ""
                });
        }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void DeactivateAgent()
        {
            PSHelper.LOGGER.Trace<PSBaseAgent>(String.Format("Deactivating agent in base agent: [{0}]", this.name));

            // Borrar la Subscripcion....
            if (engine != null)
            {
                engine.Stop();
            }

            // Poner todos los recursos desconectados...
            ResetAllResources();
            State = AgentStates.NotConnected;

            // Notificacion al servicio...
            OnAgentEventOccurred(
                new AgentEventArgs()
                {
                    agent = this,
                    ev = AgentEvents.Inactive,
                    p1 = ""
                });
        }
        /// <summary>
        /// 20180710. Sirve para poder Resetear la tabla de usuarios a 'Not Available', en cambios de CFG que 'desactivan' en proxy
        /// </summary>
        protected virtual void ConfirmAgentDeactivated()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected TimeSpan TimeoutNotConnected { get; set; }
        protected TimeSpan TimeoutConnected { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        Timer wkTimer;
        DateTime last = DateTime.MinValue;
#if DEBUG1
        PresenceServiceHelper.TimeMeasurement tm = null;
#endif
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            /** 20180724. Para protegerse de los eventos que ocurran al cerrar el timer con algun evento pendiente encolado */
            try
            {
                if (((System.Timers.Timer)source).Enabled == true)
                {
                    ((System.Timers.Timer)source).Enabled = false;

                    TimeSpan control = State == AgentStates.NotConnected ? TimeoutNotConnected : TimeoutConnected;
                    TimeSpan transcurrido = DateTime.Now - last;
#if DEBUG1
            if (tm != null)
            {
                tm.StopAndPrint("Timer Event");
                tm = new PresenceServiceHelper.TimeMeasurement(Name + ": NextEvent");
            }
#endif
                    pingCount++;
                    if (transcurrido >= control)
                    {
                        last = DateTime.Now;
                        if (State == AgentStates.NotConnected)
                        {
                            TickOnDisconnected();
                        }
                        else
                        {
                            TickOnConnected();
                        }
                    }
                    ((System.Timers.Timer)source).Interval = 500;
                    ((System.Timers.Timer)source).Enabled = true;
                }
            }
            finally { }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="msg"></param>
        protected void ExceptionManager(Exception x, string msg)
        {
            OnAgentEventOccurred(
                new AgentEventArgs()
                {
                    agent = this,
                    ev = AgentEvents.LogException,
                    p1 = msg,
                    x = x
                });
        }

        #endregion
    }

}

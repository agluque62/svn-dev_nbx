using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using NLog;
using Newtonsoft.Json;

using U5ki.Infrastructure;

namespace U5ki.PresenceService 
{
    public static class PSHelper
    {
        #region Clases
        /// <summary>
        /// 
        /// </summary>
        public class ManagedSemaphore 
        {
            public ManagedSemaphore(int initialCount, int maxCount, String id, int maxTime)
            {
                _semaphore = new Semaphore(initialCount, maxCount);
                Id = id;
                Maxtime = maxTime;
                OccupiedBy = "";
            }
            public bool Acquire([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
            {
#if !DEBUG1
                if (_semaphore == null)
                    return Throw("SEM not Found", false, lineNumber, caller);
                if (_semaphore.WaitOne(Maxtime) == false)
                    return Throw("SEM Timeout", false, lineNumber, caller);
#endif
                OccupiedBy = String.Format("[{0}-{1}]", caller, lineNumber);
                return true;
            }
            public void Release(bool launch=false, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
            {
#if !DEBUG1
                if (_semaphore == null)
                    Throw("SEM not Found", false, lineNumber, caller);
                if (_semaphore.WaitOne(0) == true)
                    Throw("SEM Release Error", false, lineNumber, caller);
                _semaphore.Release();
#endif
                OccupiedBy = "";
            }
            protected bool Throw(string msg, bool launch = false, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
            {
                Logger _logger = LogManager.GetLogger("ManagedSemaphore");
                _logger.Log(LogLevel.Fatal, String.Format("{0}: {1}, From: [{2}-{3}], Last: [{4}]", Id, msg, caller, lineNumber, OccupiedBy));
#if DEBUG
                if (launch)
                    throw new Exception(msg);
#endif
                return false;
            }
            /** */
            protected string Id { get; set; }
            protected string OccupiedBy { get; set; }
            protected int Maxtime { get; set; }
            protected Semaphore _semaphore { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public class DummySemaphore
        {
            public DummySemaphore(int initialCount, int maxCount, String id, int maxTime) { }
            public bool Acquire() { return true; }
            public void Release(bool launch = false) { }            
        }
        /// <summary>
        /// 
        /// </summary>
        public class LocalParameters
        {
            const string DefMulticastGroupIp = "224.100.10.1";
            const int DefMulticastGroupPort = 1001;

            public static string MulticastGroupIp { get; set; }
            public static string MulticastInterfaceIp { get; set; }
            public static int MulticastGroupPort { get; set; }
            public static int TimeoutOnConnected { get; set; }
            public static int TimeoutOnDisconnected { get; set; }
            public static int TimeoutOnInactiveResource { get; set; }
            public static int TimeoutOnPresenceSubscription { get; set; }
            public static int MulticastSenderTick { get; set; }

            public static string BkkUser { get; set; }
            public static string BkkPwd { get; set; }

            public static void ReadConfig(Object config)
            {
                Properties.Settings settings = Properties.Settings.Default;

                /** TODO. Leer Preferentemente de Base de Datos o Fichero de Configuracion */
                MulticastGroupIp = settings.MulticastGroupIp;
                MulticastGroupPort = settings.MulticastGroupPort;

                TimeoutOnConnected = settings.TimeoutOnConnected;
                TimeoutOnDisconnected = settings.TimeoutOnDisconnected;
                TimeoutOnInactiveResource = settings.TimeoutOnInactiveResource;
                TimeoutOnPresenceSubscription = settings.TimeoutOnPresenceSubscription;
                MulticastSenderTick = settings.MulticastSenderTick;
                BkkUser = settings.BkkUser;
                BkkPwd = settings.BkkPwd;

                /** */
                MulticastInterfaceIp = settings.MulticastGroupInterfazIp;
            }
        }
        /// <summary>
        /// Accede de forma controlada a la CORESIP...
        /// </summary>
        public class ControlledSipAgent : BaseCode
        {
#if DEBUG
            public static bool InSimulatedProxiesScenario = false;
#endif
            // static SubPresCb OnPresenceCb;
            public static void Init(OptionsReceiveCb optionsCb, SubPresCb presenceCb)
            {
                try
                {
#if !DEBUG
                    SipAgent.OptionsReceive += optionsCb;
                    SipAgent.SubPres += presenceCb;
                    //SipAgent.Init(LocalParameters.SipAgentName,
                    //    LocalParameters.SipAgentIp,
                    //    LocalParameters.SipAgentPort, 2);
#else
                    if (InSimulatedProxiesScenario==true)
                    {
                        U5ki.PresenceService.Simulation.SimulatedProxiesScenario.Init(optionsCb, presenceCb);
                        Task.Delay(100).Wait();
                    }
                    else
                    {
                        SipAgent.OptionsReceive += optionsCb;
                        SipAgent.SubPres += presenceCb;
                    }
#endif
                }
                catch (Exception x)
                {
                    LogException("Init Exception", x);
                }
            }
            public static void Start()
            {
                try
                {
                    //SipAgent.Start();
                }
                catch (Exception x)
                {
                    LogException("Start Exception", x);
                }
            }
            public static void End(OptionsReceiveCb optionsCb, SubPresCb presenceCb)
            {
                try
                {
#if !DEBUG
                    SipAgent.OptionsReceive -= optionsCb;
                    SipAgent.SubPres -= presenceCb;
                    //SipAgent.End();
#else
                    if (InSimulatedProxiesScenario == true)
                    {
                        U5ki.PresenceService.Simulation.SimulatedProxiesScenario.End();
                    }
                    else
                    {
                        SipAgent.OptionsReceive -= optionsCb;
                        SipAgent.SubPres -= presenceCb;
                    }
#endif
                }
                catch (Exception x)
                {
                    LogException("End Exception", x);
                }
            }
            public static string SendOptionsMsg(string OptionsUri)
            {
                try
                {
#if !DEBUG
                    string callid = "";
                    SipAgent.SendOptionsMsg(OptionsUri, out callid, false);
                    return callid;
#else
                    if (InSimulatedProxiesScenario == true)
                    {
                        return U5ki.PresenceService.Simulation.SimulatedProxiesScenario.SendOptionsMsg(OptionsUri);
                    }
                    else
                    {
                        string callid = "";
                        SipAgent.SendOptionsMsg(OptionsUri, out callid, false);
                        return callid;
                    }
#endif
                }
                catch (Exception x)
                {
                    LogException("SendOptionsMsg Exception", x);
                }
                return "";
            }
            public static bool CreatePresenceSubscription(string PresenceUri)
            {
                try
                {
#if !DEBUG
                    SipAgent.CreatePresenceSubscription(PresenceUri);
#else
                    if (InSimulatedProxiesScenario == true)
                    {
                        return U5ki.PresenceService.Simulation.SimulatedProxiesScenario.CreatePresenceSubscription(PresenceUri);
                    }
                    else
                    {
                        SipAgent.CreatePresenceSubscription(PresenceUri);
                    }
#endif
                    return true;
                }
                catch (Exception x)
                {
                    LogException("CreatePresenceSubscriotion Exception", x);
                    return false;
                }
            }
            public static bool DestroyPresenceSubscription(string PresenceUri)
            {
                try
                {
#if !DEBUG
                    SipAgent.DestroyPresenceSubscription(PresenceUri);
#else
                    if (InSimulatedProxiesScenario == true)
                    {
                        return U5ki.PresenceService.Simulation.SimulatedProxiesScenario.DestroyPresenceSubscription(PresenceUri);
                    }
                    else
                    {
                        SipAgent.DestroyPresenceSubscription(PresenceUri);
                    }                    
#endif
                    return true;
                }
                catch (Exception x)
                {
                    LogException("DestroyPresenceSubscription Exception", x);
                }
                return false;
            }
            protected static void LogException(string msg, Exception x, params object[] par)
            {
                LogManager.GetCurrentClassLogger().Error(msg, x);            
            }
        }
        /// <summary>
        /// Para medir tiempos en modo DEBUG
        /// </summary>
        public class TimeMeasurement 
        {
            Stopwatch watch;
            String Id { get; set; }
            public TimeMeasurement(String name = "Generico")
            {
                Id = name;
                watch = new Stopwatch();
                watch.Start();
            }
            public void StopAndPrint(string etiqueta = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
            {
                watch.Stop();
#if DEBUG
                LogLevel level = LogLevel.Warn;
#else
                LogLevel level = LogLevel.Trace;
#endif
                //StackTrace stackTrace = new StackTrace();           // get call stack
                //StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                //StackFrame callingFrame = stackFrames[1];
                //MethodBase method = callingFrame.GetMethod();

                //LogManager.GetCurrentClassLogger().Warn(String.Format("From [{0}.{1} [line {2}]]: Tiempo Transcurrido: {3} ms.",
                //    method.ReflectedType,
                //    method.Name,
                //    lineNumber,
                //    watch.ElapsedMilliseconds));

                LogManager.GetCurrentClassLogger().Log(level, String.Format("[{0,-16}<{1,-8}>]: Tiempo Medido: {2,6} ms.",
                    Id,
                    etiqueta,
                    watch.ElapsedMilliseconds));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public class MttoInfo
        {
            public class AgentMttoInfo
            {
                public class ResourceMttoInfo
                {
                    public string Name { get; set; }
                    public string Type { get; set; }
                    public string Status { get; set; }
                    public string Version { get; set; }
                    public string Uri { get; set; }
                }
                public string DependencyName { get; set; }
                public string MainService { get; set; }
                public string Type { get; set; }
                public string ProxyEndpoint { get; set; }
                public string PresenceEndpoint { get; set; }
                public string Status { get; set; }
                public List<ResourceMttoInfo> Users { get; set; }
            }

            public string Name { get; set; }
            public string Mode { get; set; }
            public string Status { get; set; }
            public string Configured { get; set; }
            public string BdtConfig { get; set; }
            public AgentMttoInfo ProxiesAgent { get; set; }
            public List<AgentMttoInfo> UsersAgents { get; set; }
            /** */
            public MttoInfo(Action<MttoInfo> Init)
            {
                if (Init != null) Init(this);
            }
            /** */
            public string Serialize()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public class LOGGER
        {
            private static void Log<T>(LogLevel level, String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Logger _logger = LogManager.GetLogger(typeof(T).FullName/*.Name*/);
                String display = String.Format("[{0},{1}]: {2}", memberName, sourceLineNumber, msg);
                _logger.Log(level, display);
            }
            public static void Trace<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Trace, msg, memberName, sourceLineNumber);
            }
            public static void Info<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Info, msg, memberName, sourceLineNumber);
            }
            public static void Debug<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Debug, msg, memberName, sourceLineNumber);
            }
            public static void Error<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Error, msg, memberName, sourceLineNumber);
            }
            public static void Warning<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Warn, msg, memberName, sourceLineNumber);
            }
            public static void Fatal<T>(String msg,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            {
                Log<T>(LogLevel.Fatal, msg, memberName, sourceLineNumber);
            }


        }

#endregion

#region Rutinas

        /// <summary>
        /// Efectúa un PING Síncrono.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="presente"></param>
        /// <returns></returns>
        public static bool Ping(string host, bool presente)
        {
            int maxReint = presente ? 3 : 1;
            int reint = 0;
            PingReply reply;

            do
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128, 
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "Ulises V 5000i. PresenceService.";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 200;  // ms
                reply = pingSender.Send(host, timeout, buffer, options);
                reint++;
                System.Threading.Thread.Sleep(10);
            } while (reply.Status != IPStatus.Success && reint < maxReint);

            return reply.Status == IPStatus.Success ? true : false;
        }
        /// <summary>
        /// Efectúa un PING Asíncrono...
        /// </summary>
        /// <param name="host"></param>
        /// <param name="presente"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static void AsyncPing(string host, bool presente, PingCompletedEventHandler pingCompleted)
        {

            using (Ping ping = new Ping())
            {
                ping.PingCompleted += new PingCompletedEventHandler(pingCompleted);

                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128, 
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "Ulises V 5000i. PresenceService.";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 1000;  // ms
                ping.SendAsync(host, timeout, buffer, options);
            }
        }
        /// <summary>
        /// Retorna un EndPoint en funcion de especificacion en STRING
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static IPEndPoint SipEndPointFrom(string endpoint)
        {
            string[] parts = endpoint.Split(':');
            string ip = parts[0];
            string port = parts.Count() == 2 ? parts[1] : "5060";

            IPAddress Ip;
            int Port;
            if (IPAddress.TryParse(ip, out Ip) == false ||
                Int32.TryParse(port, out Port) == false)
                return null;

            return new IPEndPoint(Ip, Port);
        }

#endregion
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using U5ki.Infrastructure;


namespace U5ki.PresenceService.Simulation
{
    public class SimulatedProxiesScenario 
    {
        #region Simulated Responses

        protected static event OptionsReceiveCb OptionsResponse;
        protected static event SubPresCb PresenceResponse;

        #endregion Simulated Responses

        #region Simulated Request

        public static void Init(OptionsReceiveCb optionsCb, SubPresCb presenceCb)
        {
            OptionsResponse = optionsCb;
            PresenceResponse = presenceCb;

            InternalInit();
        }
        public static void End()
        {
            // Desactivar los procedimientos internos del simulador.
            InternalEnd();

            OptionsResponse = null;
            PresenceResponse = null;
        }
        public static string SendOptionsMsg(string OptionsUri)
        {
            return InternalOptionsRequestHandle(OptionsUri);
        }
        public static bool CreatePresenceSubscription(string PresenceUri)
        {
            InternalSubscribeRequestHandle(PresenceUri);
            return true;
        }
        public static bool DestroyPresenceSubscription(string PresenceUri)
        {
            InternalUnsubscribeRequestHandle(PresenceUri);
            return false;
        }

        #endregion Simulated Request

        #region Internals

        static void InternalInit()
        {
            Scenario = new SimulatedScenario();
            SubscribedUserUris = new List<string>();
            callid = 1;
            MainTask = Task.Factory.StartNew(() =>
            {
                int spSubscriptionCount = 0;
                do
                {
                    List<Tuple<string, int, int>> presenceEvents = new List<Tuple<string, int, int>>();
                    lock (Scenario)
                    {
                        // Leer la configuracion
                        if (File.Exists(PathOfJsonFile))
                        {
                            Scenario = JsonConvert.DeserializeObject<SimulatedScenario>(File.ReadAllText(PathOfJsonFile));
                        }

                        if (++spSubscriptionCount >= 10)
                        {
                            spSubscriptionCount = 0;
                            Scenario.Proxies.ForEach(p =>
                            {
                                if (p.Up==1)
                                {
                                    if (p.PresEndp != "" && p.PresUp == 1)
                                    {
                                        /** Refrescar las subscripciones */
                                        p.Subs.ForEach(s =>
                                        {
                                            string uri = "<sip:" + s.Id + "@" + p.PresEndp + ">";
                                            if (SubscribedUserUris.Contains(uri))
                                            {
                                                presenceEvents.Add(new Tuple<string, int, int>(uri, 1, s.Up));
                                            }
                                        });
                                    }
                                    else
                                    {
                                        /** Borrar todas las subscripciones */
                                        p.Subs.ForEach(s =>
                                        {
                                            string uri = "<sip:" + s.Id + "@" + p.PresEndp + ">";
                                            if (SubscribedUserUris.Contains(uri))
                                            {
                                                presenceEvents.Add(new Tuple<string, int, int>(uri, 0, 0));
                                                SubscribedUserUris.Remove(uri);
                                            }
                                        });
                                    }
                                }
                                else
                                {
                                    /** Borrar todas las subcripciones */
                                    p.Subs.ForEach(s =>
                                    {
                                        string uri = "<sip:" + s.Id + "@" + p.PresEndp + ">";
                                        if (SubscribedUserUris.Contains(uri))
                                        {
                                            presenceEvents.Add(new Tuple<string, int, int>(uri, 0, 0));
                                            SubscribedUserUris.Remove(uri);
                                        }
                                    });
                                }
                            });
                        }
                    }
                    presenceEvents.ForEach(e =>
                    {
                        PresenceResponse(e.Item1, e.Item2, e.Item3);
                    });
                    Task.Delay(1000).Wait();
                } while (MainTask != null);
            });


        }
        static void InternalEnd()
        {
            MainTask = null;
        }
        static string InternalOptionsRequestHandle(string to)       // <sip:ip:port>
        {
            string callidstr = (callid++).ToString().PadLeft(8, '0');
            lock (Scenario)
            {
                var proxy = Scenario.Proxies.Where(p => p.Endp == to && p.Up == 1).FirstOrDefault();
                if (proxy != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Task.Delay(50); // Podia estar en el fichero de simulacion.
                        OptionsResponse(to, callidstr, 200, "", "");
                    });
                }
            }
            return callidstr;
        }
        static void InternalSubscribeRequestHandle(string uri)      // <sip:user@ip:port>
        {
            string pserv = UserUri2ServerEndp(uri);
            Task.Factory.StartNew(() =>
            {
                int subs_status = 0;
                int pres_status = 0;
                Task.Delay(50).Wait();
                lock (Scenario)
                {
                    var proxy = Scenario.Proxies.Where(p => p.Up == 1 && p.PresEndp == pserv).FirstOrDefault();
                    if (proxy != null)
                    {
                        if (proxy.PresUp==1)
                        {
                            var userid = UserUri2UserId(uri);
                            if (SubscribedUserUris.Contains(uri) == false)
                                SubscribedUserUris.Add(uri);

                            subs_status = 1;

                            var sub = proxy.Subs.Where(s => s.Id == userid).FirstOrDefault();
                            if (sub != null)
                            {
                                pres_status = sub.Up;
                            }
                        }
                    }
                }
                if (PresenceResponse != null)
                    PresenceResponse(uri, subs_status, pres_status);
            });            
        }
        static void InternalUnsubscribeRequestHandle(string uri)    // <sip:user@ip:port>
        {
            Task.Factory.StartNew(() =>
            {
                Task.Delay(50).Wait();
                lock (Scenario)
                {
                    //var userid = UserUri2UserId(uri);
                    if (SubscribedUserUris.Contains(uri))
                    {
                        SubscribedUserUris.Remove(uri);
                    }
                }
                //if (PresenceResponse!=null)
                //    PresenceResponse(uri, 0, 0);
            });
        }

        #endregion Internals

        #region Helpers

        static string UserUri2ServerEndp(string uri)                // <sip:user@ip:port>
        {
            int i1 = uri.IndexOf('@') + 1;
            int i2 = uri.IndexOf('>');

            return uri.Substring(i1, i2-i1);
        }
        static string UserUri2UserId(string uri)                    // <sip:user@ip:port>
        {
            int i1 = uri.IndexOf(':') + 1;
            int i2 = uri.IndexOf('@');

            return uri.Substring(i1, i2 - i1);
        }

        #endregion Helpers

        #region Internal Data

        class SubscriberData
        {
            public string Id { get; set; }
            public int Up { get; set; }
        }
        class ProxyData
        {
            public string Id { get; set; }
            public string Endp { get; set; }
            public int Up { get; set; }
            public string PresEndp { get; set; }
            public int PresUp { get; set; }
            public List<SubscriberData> Subs { get; set; }
        }
        class SimulatedScenario
        {
            public List<ProxyData> Proxies { get; set; }
        }
        static SimulatedScenario Scenario { get; set; }
        static string PathOfJsonFile = "Simulation/simulatedproxies.json";
        static Task MainTask { get; set; }
        static Int32 callid { get; set; }
        static List<string> SubscribedUserUris = new List<string>();

        #endregion Internal Data
    }
}

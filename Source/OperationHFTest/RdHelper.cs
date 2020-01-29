using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;
using U5ki.Infrastructure;
namespace U5ki.RdService
{
    class RdHelper
    {
    }

    public enum RdRsType
    {
        Rx,
        Tx,
        RxTx
    }
    
    public static class Properties
    {
        public static class Settings
        {
            public static class Default
            {
                public static string HFSnmpReadCommunity { get { return "public"; } }
                public static string HFSnmpWriteCommunity { get { return "public"; } }
                public static int HFSnmpTimeout { get { return 3000; } }
                public static int HFJBusTimeout { get { return 5000; } }
                public static bool HFSnmpSimula { get { return false; } }
                public static bool HFBalanceoAsignacion { get { return true; } }
                public static int HFSipSupervisionMode { get { return 0; } }
            }
        }
    }

    public class RdFrecuency
    {
        public RdFrecuency() { }
        public RdFrecuency(string idequipo) { }
        public bool FindHost(string user){return true;}
        public Dictionary<string, RdResource> RdRs = new Dictionary<string, RdResource>();
        public int FrecuenciaSintonizada { get; set; }
        public void RemoveSipCall(RdResource res) { }
        public string TipoDeFrecuencia { get { return "HF"; } }
        public string Frecuency { get; set; }
    }

    public class RdResource
    {
        public RdResource() { }
        public RdResource(string IdEquipo, string SipUri, RdRsType tipo, string IdEquipo1, string empl) 
        {
            Uri1 = SipUri;
            LogManager.GetCurrentClassLogger().Info("RdResource ON {0}", SipUri);
        }
        public RdResource(string IdEquipo, string SipUri, RdRsType tipo, string IdEquipo1, bool toCheck) 
        {
            Uri1 = SipUri;
            LogManager.GetCurrentClassLogger().Info("RdResource ON {0}", SipUri);
        }
        public void Check() { }
        public void HandleChangeInCallState(CORESIP_CallStateInfo p) { }
        public void Dispose() 
        {
            LogManager.GetCurrentClassLogger().Info("RdResource OFF {0}", Uri1);
        }
        public bool ToCheck { get { return false; } }
        public string Uri1 { get; set; }
        public string Uri2 { get { return "---"; } }
        public int SipCallId { get { return -1; } }
        public RdRsType Type { get; set; }
    }

    public static class RdRegistry
    {
        public static Action RefreshConsole;
        public static void RespondToFrHfTxChange(string ip, string frec, int error) 
        {
            RefreshConsole();
        }
        public static void RespondToPrepareSelcal(string to, string fr, bool res, string men)
        {
            RefreshConsole();
        }
    }

    struct AskingThread
    {
        public string from;
        public FrTxChangeAsk ask;
        public int result;
    }

}

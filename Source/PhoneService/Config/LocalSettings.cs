using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace u5ki.PhoneService.Config
{
    class LocalSettings : IDisposable
    {
        public class SipSettings : ICloneable
        {
            public string AgentName { get; set; }
            public uint UdpPort { get; set; }


            public string DefaultCodec { get; set; }
            public uint DefaultDelayBufPframes { get; set; }
            public uint DefaultJBufPframes { get; set; }
            public uint SndSamplingRate { get; set; }
            public float RxLevel { get; set; }
            public float TxLevel { get; set; }
            public uint SipLogLevel { get; set; }
            public uint TsxTout { get; set; }
            public uint InvProceedingIaTout { get; set; }
            public uint InvProceedingMonitoringTout { get; set; }
            public uint InvProceedingDiaTout { get; set; }
            public uint InvProceedingRdTout { get; set; } 
            public uint KAPeriod { get; set; }
            public uint KAMultiplier { get; set; }

            public SipSettings()
            {
                AgentName  = "UVK5-TFOCUS";
                UdpPort  = 7060;
                DefaultCodec = "PCMA";
                DefaultDelayBufPframes = 3;
                DefaultJBufPframes = 4;
                SndSamplingRate = 8000;
                RxLevel = 1;
                TxLevel = 1;
                SipLogLevel = 3;
                TsxTout = 400;
                InvProceedingIaTout = 1000;
                InvProceedingMonitoringTout = 30000;
                InvProceedingDiaTout = 30000;
                InvProceedingRdTout = 1000;
                KAPeriod = 200;
                KAMultiplier = 10; 
            }

            public object Clone()
            {
                return new SipSettings()
                {
                    AgentName = AgentName,
                    UdpPort = UdpPort,
                    DefaultCodec = DefaultCodec,
                    DefaultDelayBufPframes = DefaultDelayBufPframes,
                    DefaultJBufPframes = DefaultJBufPframes,
                    SndSamplingRate = SndSamplingRate,
                    RxLevel = RxLevel,
                    TxLevel = TxLevel,
                    SipLogLevel = SipLogLevel,
                    TsxTout = TsxTout,
                    InvProceedingIaTout = InvProceedingIaTout,
                    InvProceedingDiaTout = InvProceedingDiaTout,
                    InvProceedingMonitoringTout = InvProceedingMonitoringTout,
                    InvProceedingRdTout = InvProceedingRdTout,
                    KAPeriod = KAPeriod,
                    KAMultiplier = KAMultiplier
                };
            }
        }
        public class FocusSettings : ICloneable
        {
            public object Clone()
            {
                FocusSettings clon = new FocusSettings();

                return clon;
            }
        }

        public string Ip { get; set; }
        public SipSettings Sip { get; set; }
        public FocusSettings Focus { get; set; }
        public LocalSettings()
        {
            Ip = LocalIPAddress.ToString();
            Sip = new SipSettings();
            Focus = new FocusSettings();
            FileName = typeof(SipSettings).Namespace + ".json";
        }

        public void Init()
        {
            if (System.IO.File.Exists(FileName))
            {
                String jdata = System.IO.File.ReadAllText(FileName);

                if (IsValidJsonString(jdata))
                {
                    LocalSettings filesettings = Utilities.ServicesHelpers.DeserializeObject<LocalSettings>(jdata);

                    Ip = filesettings.Ip;
                    Sip = filesettings.Sip.Clone() as SipSettings;
                    Focus = filesettings.Focus.Clone() as FocusSettings;
                    return;
                }
            }
        }

        public void Dispose()
        {
            System.IO.File.WriteAllText(FileName, Utilities.ServicesHelpers.SerializeObject(this));
        }

        protected bool IsValidJsonString(string data)
        {
            // TODO...
            return true;
        }

        protected static System.Net.IPAddress LocalIPAddress
        {
            get
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip;
                    }
                }
                // throw new Exception("No network adapters with an IPv4 address in the system!");
                return System.Net.IPAddress.Loopback;
            }
        }

        protected string FileName { get; set; }
    }
}

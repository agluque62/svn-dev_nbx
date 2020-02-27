using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using U5ki.PresenceService.Interfaces;

namespace U5ki.PresenceService
{
    /// <summary>
    /// 
    /// </summary>
    public enum RsStatus { Available = 0, Busy = 1, BusyUninterruptible = 2, NotAvailable = 3, NoInfo = -1 }
    /// <summary>
    /// 
    /// </summary>
    public class PresenceServerResource : BinaryResource
    {
        private const int defMaxNameLenght = 32 + 4;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = defMaxNameLenght)]
        public String name;
        [MarshalAs(UnmanagedType.I4)]
        public U5ki.Infrastructure.RsChangeInfo.RsTypeInfo type;		        /* publica_tlf, publica_lc */
        [MarshalAs(UnmanagedType.I4)]
        public int version;	            /* cambia de valor con los cambios */
        [MarshalAs(UnmanagedType.I4)]
        public RsStatus status;	        /* estado del recurso */
        [MarshalAs(UnmanagedType.I4)]
        public int prio_cpipl;
        [MarshalAs(UnmanagedType.I4)]
        public int trafficCounter;
        [MarshalAs(UnmanagedType.I4)]
        public int time;
        /// <summary>
        /// 
        /// </summary>
        public PresenceServerResource()
        {
            name = "RS-NAME";
            KeyOfPersistence = "RS-NAME";
            type = U5ki.Infrastructure.RsChangeInfo.RsTypeInfo.PhLine;
            version = 0;
            status = RsStatus.NotAvailable;
            prio_cpipl = 0;
            trafficCounter = 0;
            time = 0;

            FailedPollCount = 0;
        }

        public PresenceServerResource(PresenceServerResource other)
        {
            name = other.name;
            KeyOfPersistence = other.KeyOfPersistence;
            type = other.type;
            version = other.version;
            status = other.status;
            prio_cpipl = other.prio_cpipl;
            trafficCounter = other.trafficCounter;
            time = other.time;
            FailedPollCount = other.FailedPollCount;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Frame
        {
            get
            {
                byte[] frame = new byte[1];

                int offset = CopyString2ByteArray(ref frame, 0, name, defMaxNameLenght);                    
                offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)type));
                offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)version));
                offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)status));
                offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)prio_cpipl));
                offset = CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)trafficCounter));
                CopyObject2ByteArray(ref frame, offset, IPAddress.HostToNetworkOrder((int)time));

                return frame;
            }
        }
        public bool Equals(PresenceServerResource other)
        {
            return name == other.name && type == other.type;
        }
        public long last_set { get; set; }
        public string PresenceAdd { get; set; }
        public string Uri
        {
            get
            {
                if (PresenceAdd != "")
                    return String.Format("<sip:{0}@{1}>", name, PresenceAdd);
                return "Not Presence Server";
            }
        }
        public string OptionsUri
        {
            get
            {
                return String.Format("<sip:{0}>", name);
            }
        }
        public RsStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                // Salvar Ultimo valor.
                status = PersistenceOfStates.Set(KeyOfPersistence, value);
            }
        }
        public string Dependency { get; set; }

        public string KeyOfPersistence { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public class PersistenceOfStates
        {
            static Dictionary<string, RsStatus> last_states = new Dictionary<string, RsStatus>();
            protected static string Key(IAgent agent, string rsname)
            {
                return string.Format("[{0}##{1}]", agent.ProxyEndpoint.ToString(), rsname);
            }

            public static void Free()
            {
                last_states.Clear();
            }
            public static RsStatus Get(string key, RsStatus defStatus)
            {
                if (key is string)
                {
                    if (last_states.Keys.Contains(key))
                        return last_states[key];
                }
                return defStatus;
            }
            public static RsStatus Set(string key, RsStatus NewStatus)
            {
                if (key is string)
                {
                    last_states[key] = NewStatus;
                }
                return NewStatus;
            }

            //public static RsStatus Get(Object obj, RsStatus defStatus)
            //{
            //    string rsname;
            //    if (obj is string)
            //        rsname = (obj as string);
            //    else if (obj is PresenceServerResource)
            //        rsname = (obj as PresenceServerResource).name;
            //    else
            //        return defStatus;
            //    if (last_states.Keys.Contains(rsname))
            //        return last_states[rsname];
            //    return defStatus;
            //}
            //public static RsStatus Set(Object obj, RsStatus NewStatus)
            //{
            //    string rsname;
            //    if (obj is string)
            //        rsname = (obj as string);
            //    else if (obj is PresenceServerResource)
            //        rsname = (obj as PresenceServerResource).name;
            //    else
            //        return NewStatus;

            //    last_states[rsname] = NewStatus;
            //    return NewStatus;
            //}


            public static Dictionary<string, RsStatus> LastStates { get { return last_states; } }
        }

        Int64 ConsecutiveFailedPollLimit = 2;
        Int64 FailedPollCount { get; set; }
        public bool ProcessResult(bool success)
        {
            if (success)
            {
                FailedPollCount = 0;
                return true;
            }
            FailedPollCount++;
            return (FailedPollCount >= ConsecutiveFailedPollLimit) ? true : false;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text;

using U5ki.Infrastructure;
using Utilities;

namespace U5ki.TifxService
{
    /// <summary>
    /// 
    /// </summary>
	[Serializable]
	public class RsInfo
	{
		[SerializeAs(Size = 36, Encoding = "windows-1252")]
		public string RsId = null;

		[NonSerialized]
        public string GwIp = null;  /** 1,2: Ip Pasarela, 3: Ip PBX, 4: Ip Proxy Dependencia, 5,6,7,8: Ip Proxy */

        public uint Type = 0;		/** NoType = 0, PhLine = 1, IcLine = 2, InternalSub = 3, ExternalSub = 4, InternalProxy = 5, InternalAltProxy = 6, ExternalProxy = 7, ExtenalAltProxy = 8  */
		public uint Version = 0;
        public uint State = 0;      /** Available = 0, Busy = 1, BusyUninterruptible = 2, NotAvailable = 3*/
		public uint Priority = 0;
		public uint Steps = 0;
		public uint CallBegin = 0;
	}

    /// <summary>
    /// 
    /// </summary>
	[Serializable]
	public class GwInfo
	{
        public uint Type = 0;       /** Gw = 1, ForInternalSub = 5, ForExternalSub = 3, ForProxies = 4*/

		[NonSerialized]
		public int? LastReceived = null;

		[NonSerialized]
		public string GwIp = null;

        [SerializeAs(Size = 36, Encoding = "windows-1252")]
        public string GwId  = null;

		public uint NumRs = 0;
		public uint Version = 0;

		[SerializeAs(LengthField = "NumRs", ElementSize = 60)]
		public RsInfo[] Resources = new RsInfo[0];
	}
}

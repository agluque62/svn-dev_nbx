using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;

namespace U5ki.RdService
{
    public interface IRdResource: IDisposable
    {
        RdRsType Type
        { get; }

        bool isRx
        { get; }

        bool isTx
        { get; }

        RdRsPttType Ptt
        { get; }

        ushort PttId
        { get; }
        bool Squelch
        { get; }

        bool TxMute
        { get; set; }

        string ID
        { get; }

        string Uri1
        { get; }
        string Uri2
        { get; }

        bool ToCheck
        { get; }

        string Site
        { get; set; }

        bool SelectedSite
        { get; set; }
        /// <summary>
        /// Active SipCallId
        /// </summary>
        int SipCallId
        { get; }

        bool MasterMN
        { get; set; }

        bool ReplacedMN
        { get; set; }
        Boolean IsForbidden
        { get; set; }
        bool Connected
        { get; }
        bool OldSelected
        { get; set; }

        bool Connect();
        void PttOff();

        void PttOn(CORESIP_PttType srcPtt);

        RdResource GetSimpleResource(int sipCallId);

        RdResource GetRxSelected();

        List<RdResource> GetListResources();
        bool HandleChangeInCallState(int sipCallId, CORESIP_CallStateInfo stateInfo);

        bool ActivateResource(string IdResource);
    }
}

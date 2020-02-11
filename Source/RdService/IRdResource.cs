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

        bool TxMute
        { get; set; }

        string ID
        { get; }

        string Uri1
        { get; }

        bool ToCheck
        { get; }

        string Site
        { get; }

        bool Selected
        { get; set; }

        int SipCallId
        { get; }

        bool Connect();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;

namespace U5ki.RdService
{
    public class RdResourcePair : BaseCode //, IRdResource, IDisposable
    {
        RdResource ResourceActive;
        RdResource ResourceIdle;

        public RdResourcePair()
        {
        }
        public int SipCallId
        {
            get { return ResourceActive.SipCallId; }
        }

    #region IDisposable Members
    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
        {
            ResourceIdle.Dispose();
            ResourceIdle = null;
            ResourceActive.Dispose();
            ResourceActive = null;
        }

        #endregion

    }
}

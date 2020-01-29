using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace u5ki.RemoteControlService
{
    public static class Locals
    {

        private static RemoteControlFactory _remoteControlFactory;
        public static RemoteControlFactory RemoteControlFactory
        {
            get
            {
                if (null == _remoteControlFactory)
                    _remoteControlFactory = new RemoteControlFactory();
                return _remoteControlFactory;
            }
        }

    }
}

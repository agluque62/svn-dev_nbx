using AdminConsole.Code.Delegates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminConsole.Code
{
    public static class Globals
    {

        private static EventsHandler _events;
        public static EventsHandler Events
        {
            get
            {
                if (null == _events)
                    _events = new EventsHandler();
                return _events;
            }
        }

    }
}

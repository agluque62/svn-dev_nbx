using System;
using System.Collections.Generic;
using U5ki.Infrastructure.Handlers;

namespace U5ki.Infrastructure.Code
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

        /// <summary>
        /// Englobes the variables and configuration needed to activate the "Test" mode.
        /// </summary>
        public static class Test
        {

            /// <summary>
            /// Used to set the "test mode" mode ON or OFF. This is going to be used in the code to "jump" the actual operations, and go direct to the parts need to test.
            /// </summary>
            public static Boolean IsTestRunning;
            public static Boolean IsRCNDFSimuladoRunning;

            /// <summary>
            /// Used to set the "bypass" the calls in the RemoteControl.
            /// </summary>
            public static Boolean RemoteControlByPass;

            /// <summary>
            /// Used in the test to set if the behaviour is random or not.
            /// </summary>
            public static Boolean RandomBehaviour;
            /// <summary>
            /// The probability of status change. 
            /// Default value = 40 (41).
            /// </summary>
            /// <remarks>
            /// Add one the value you wanna use.
            /// </remarks>
            public static Int32 RandomBehaviourProbability = 4;

            public static class Gears
            {
                /// <summary>
                /// Represent a list of gear's IPs that are real and have to use the Remote Command without bypass.
                /// </summary>
                public static IList<String> GearsReal = new List<String>();
                /// <summary>
                /// Represent a list of gears that are not responding. Gears in fail status.
                /// </summary>
                public static IList<String> GearsFails = new List<String>();
                /// <summary>
                /// Represent a list of gears that are not responding. Gears in timeout status.
                /// </summary>
                public static IList<String> GearsTimeout = new List<String>();
                /// <summary>
                /// 
                /// </summary>
                public static IList<String> GearsLocal = new List<String>();
            }
        }

    }
}

using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using U5ki.Infrastructure;

namespace U5ki.RdService
{

    public enum EquipoHFStatus 
    { 
        stdNoinfo, 
        stdError, 
        stdDisponible, 
        stdAsignado, 
        stdNoResource, 
        stdFrequencyAlreadyAssigned = 0xFD, 
        stdNoGateway = 0xFE, 
        stdTxAlreadyAssigned = 0xFF 
    }

}
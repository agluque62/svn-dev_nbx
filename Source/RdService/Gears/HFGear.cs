using System;
using U5ki.Infrastructure;
using U5ki.Delegates;

namespace U5ki.RdService.Gears
{

    public class HFGear : BaseGear
    {

        public HFGear(
            Node input, 
            Func<BaseGear, bool> reserveFrecuency,
            Func<BaseGear, bool> freeFrecuency,
            BaseGearOperation onGearAllocated,
            BaseGearOperation onGearDeallocated,
            BaseGearDelegate onGearStatusUpdated,
            BaseGearOperation onGearChecked)
            : base(input, reserveFrecuency, freeFrecuency, onGearAllocated, onGearDeallocated, onGearStatusUpdated, onGearChecked)
        {
        }

    }

}

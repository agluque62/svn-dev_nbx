using U5ki.Enums;
using U5ki.RdService.Gears;

namespace U5ki.Delegates
{


    public delegate void BaseGearDelegate(BaseGear gear);
    public delegate void BaseGearOperation(BaseGear gear, GearOperationStatus status);

    public delegate void NodeOperation(BaseGear gear);


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.Enums
{

    #region Global

    public enum GlobalEventTypes
    {
        OnMessage = 1000
    }

    #endregion

    #region Gears

    public enum GearOperationStatus
    {
        OK = 1,
        Fail = 2,
        Timeout = 3,
        Local = 4,
        Rejected = 5,
        FailSessionsSip = 6,
        FailMasterConfig = 7, //JOI CONTROL FRECUENCIA MASTER
        FailProtocolSNMP = 8, //20180319 INHABILITACIÓN POR ERROR SNMP
        None
    }

    public enum GearStatus
    {
        Initial = 0,
        Ready = 1,
        Assigned = 2,
        Fail = 3,
        /// <summary>
        /// Este estado se utiliza para marcar un elemento como no "validable" y como no utilizable. 
        /// Se debe asignar solo de forma manual desde el Commander, para mantenimiento.
        /// </summary>
        Forbidden = 4,
        /// Este estado es temporal entre el ready y el assigned. 
        /// Se usa para marcar el recurso como no disponible para una conmutación
        AssignationInProgress = 5
    }

    public enum GearActivationState
    {
        Inactive = 0,
        Active = 1
    }

    public enum GearActivationCommand
    {
        NoGo = 0,
        Go = 1
    }

    public enum GearSessionStatus
    {
        None = 0,
        Remote = 1,
        Local
    }

    public enum GearTypes
    {
        RCRohde4200 = 1,
        RCJotron7000 = 2
    }

    #endregion

    #region RemoteControl

    public enum RCTypes
    {
        RCRohde4200 = 1000,
        RCJotron7000 = 1001,
        RCNDFSimulado = 1999,
        Other = 2000
    }

    public enum RCConfigurationAction
    {
        Assing = 1,
        Unassing = 2
    }

    public enum RCSessionTypes
    {
        /// <summary>
        /// Reading Only.
        /// </summary>
        Monitoring = 0,
        /// <summary>
        /// Do not use with remote SNMP.
        /// </summary>
        Local = 1,
        /// <summary>
        /// Read/Write.
        /// </summary>
        Remote = 2
    }

    #endregion
    
    #region SNMP
    
    public enum SNMPVersions
    {
        V1 = 1,
        V2 = 2,
        V3 = 3
    }

    #endregion

}

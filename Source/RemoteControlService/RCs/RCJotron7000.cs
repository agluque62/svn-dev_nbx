using Lextm.SharpSnmpLib;
// using NLog;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;
using U5ki.Infrastructure.Resources;
using Utilities;

using Translate;

namespace u5ki.RemoteControlService
{
    /// <summary>
    /// Esta es la clase que engloba la funcionalidad principal del telemando de Jotron modelo 7000.
    /// </summary>
    class RCJotron7000 : BaseCode, IRemoteControl
    {

        #region Declarations

        public readonly Int32 Port = 160;

        public readonly String Community = "public";
        public readonly Int32 SessionTimeout = 999;
        public readonly VersionCode SNMPVersion = VersionCode.V1;
        public readonly Int32 SNMPCallTimeout = 500; // Miliseconds = 1,0 seconds antes 0,5
        public readonly Int32 NUMMAXTimeout = 1;
        // JOI: 20171031 ERROR #3231 
        public readonly Int32 PowerLevelMin = 300;
        public readonly Int32 PowerLevelMax = 470;
        public readonly Int32 PowerLevelDefault = 470;
        // JOI: 20171031 ERROR #3231
        // private static Logger _logger = LogManager.GetCurrentClassLogger();
        public string Name { get { return U5ki.Infrastructure.Resources.ServiceNames.RemoteControlService; } }
        private Action<GearOperationStatus> _response;
        private Action<String> _responseString;
        private Thread _thread;
        /// <summary>
        /// Variable para control de las excecpciones. Se utiliza para evitar que cada iteracion/comprobación que un equipo no este respondiendo, 
        /// no genere continuamente eventos de historico, solo la primera vez.
        /// </summary>
        IDictionary<String, Type> _lastExceptions = new Dictionary<String, Type>();
        //JOI
        enum GearCarrierOffStatusJotron
        {
            Off = 7,
            kHz_7_5 = 12,
            kHz_5_0 = 10,
            kHz_2_5 = 8,
            Hz_0_0 = 7, //JOTRON No lo implementa paso a off
            kHz_minus_2_5 = 6,
            kHz_minus_5_0 = 4,
            kHz_minus_7_5 = 2,
            kHz_8 = 13,
            kHz_4 = 9,
            kHz_minus_4 = 5,
            kHz_minus_8 = 1,
            kHz_7_3 = 11,
            kHz_minus_7_3 = 3
        }

        enum GearModulationsJotron
        {
            AM = 1,
            AMMSK = 2,
            VDL2 = 3,
            FM = 4,
            DSC = 5
        }

        enum GearEventLevelJotron
        {
            jt_Ok = 0,
            jt_Warning = 1,
            jt_Error = 2
        }

        enum GearIndexOperationalState
        {

            keyInp = 0,                 //There is an active key input to the radio (tx) or the txBusy input is low (rx)
            forcedptt = 1,              //The radio is forced to the key (ptt) state by a software command
            rxSqtxKeyConf = 2,          //The squelch on the radio is open(Rx). KeyConf is a confirmation signal that the PA internal to the transmitter is keyed.
            forcedsqopen = 3,           //The squelch is forced open (always) (rx only) by a software command
            alarm = 4,                  //The radio is in alarm state
            forcedalarm = 5,            //The radio is forced to the alarm state by a software command
            standby = 6,                //The radio is in standby state
            forcedstandby = 7,          //The radio is forced into standby state by a software command
            lowpower = 8,               //The radio is in lowpower state
            forcedlowpower = 9,         //The radio is forced into lowpower state by a software command
            forcedmute = 10,            //The radio is forced into mute state(rx only) by a software command (ICU :pinalarm One or more pins are in alarm state)
            alert = 11,                 //The radio is in alert state (ICU :unAckAlarm)
            fail = 12,                  //The radio is in fail state (ICU :unAckPinAlarm)
            rs232CommActive = 13,       //The radio has processed commands on the rs232 interface (5sec timeout)
            ac = 14,                    //The radio is operated from AC power
            rxScttxTimeout = 15,        //The radio indicates possible Simultaneous Call detection on RX and TX in timeout for TX
            selectIn = 16,              //The select input signal is active
            voipstreamInTimeout = 17,   //The radio has one ore more voip streams in timeout
            telsaBusy = 18              //The radio has enabled Telsa filter, and Telsafilter is not ready/cmd not present.
        }

        enum GearNumParamOperationalState
        {
            jt_NumParamOk = 2         
        }
        enum GearOperMode
        {
            jt_reset = 1,
            jt_main = 2,
            jt_norm = 3,
            jt_off = 4,
            test = 5 // Do not use (internal)
        }

        enum GearForceLowPower
        {
            jt_true = 1,
            jt_false = 2
             
        }

        enum GearInServiceMode
        {
            jt_true = 1,
            jt_false = 2

        }
        enum GearChannelSpacingsJotorn
        {
            jt_kHz_8_33 = 1,
            jt_kHz_25_00 = 2
        }

        enum GearAnalogInputModulationSourceTx
        {
            jt_AIMST_Auto   = 1,
            jt_AIMST_LineIn   = 2,
            jt_AIMST_Mic   = 3,
            jt_AIMST_ModGen   = 4,
            jt_AIMST_VoIP   = 5
        }

        //JOI. CONTROL_ALARMAS_PERMANENTES
        private bool bcontrol_alarmas_permanentes = u5ki.RemoteControlService.Properties.Settings.Default.ControlAlarmasPermanentes;
        //JOI. CONTROL_ALARMAS_PERMANENTES FIN

        // JOI. CONTROL_SIP
        private bool bcontrol_sip = u5ki.RemoteControlService.Properties.Settings.Default.ControlSessionSIP;
        const string OID_EUROCONTROL_VOSIPSESSIONLIST = "1.3.6.1.4.1.2363.6.1.1.8.0";
        private string sipNdbx { get { return Properties.Settings.Default.SipUser + "@" + Properties.Settings.Default.SipIp; } }
        // JOI. CONTROL_SIP FIN.
 
        // JOI. CONTROL_EQUIPO_EN_ALARMA.(20170616)
        const string OID_JOTRON_ALARMAS_PERMANENTES_TX = "1.3.6.1.4.1.22154.3.1.2.5.1.6.0";
        const string OID_JOTRON_ALARMAS_PERMANENTES_RX = "1.3.6.1.4.1.22154.3.1.2.5.2.6.0";
        // JOI. CONTROL_EQUIPO_EN_ALARMA FIN.

        // JOI: 20171031 ERROR #3231 
        //The carrier output power in dBm x 10 (0.1 dB resolution) min  300 max 470.
        const string OID_JOTRON_TXAMPOWERFINE = "1.3.6.1.4.1.22154.3.1.2.2.1.4.4.0";
        // JOI: 20171031 ERROR #3231
 
        // JOI. CONTROL_SET_SIP
        private int delaySetFrequencyMs = Convert.ToInt32(u5ki.RemoteControlService.Properties.Settings.Default.DelaySetFrequencyMs);
        // JOI. CONTROL_SET_SIP FIN.
        //JOI FIN
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public RCJotron7000()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public RCJotron7000(Int32 port)
        {
            Port = port;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sNMPVersion"></param>
        public RCJotron7000(Int32 port, VersionCode sNMPVersion)
        {
            Port = port;
            SNMPVersion = sNMPVersion;
        }

        #region Logic

#if DEBUG
        /// <summary>
        /// Usado exclusivamente para el emulador de prueba. 
        /// Como se puede ver este codigo en RELEASE desaparecera.
        /// </summary>
        private void RandomBehaviour(BaseNode node)
        {
            Random random = new Random((int)DateTime.Now.Ticks);

            Int32 number = random.Next(5, 50);
            if (DateTime.Now > node.LastStatusModification.AddSeconds(number))
            {
                node.LastStatusModification = DateTime.Now;
                _response.Invoke(GearOperationStatus.OK);
                return;
            }

            number = random.Next(1, Globals.Test.RandomBehaviourProbability);
            if (number == 1)
            {
                if (Convert.ToBoolean(random.Next(0, 1)))
                    _response.Invoke(GearOperationStatus.Fail);
                else
                    _response.Invoke(GearOperationStatus.Timeout);
            }
            else
                _response.Invoke(GearOperationStatus.OK);
        }
#endif

        #region Logic - IRemoteControl

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="node"></param>
        public void CheckNode(Action<GearOperationStatus> response, BaseNode node)
        {
            _response = response;

#if DEBUG
            if (Globals.Test.IsTestRunning && !Globals.Test.Gears.GearsReal.Contains(node.IP))
            {
                if (Globals.Test.RandomBehaviour)
                {
                    RandomBehaviour(node);
                    return;
                }

                if (Globals.Test.Gears.GearsTimeout.Contains(node.Id))
                {
                    Thread.Sleep(3000);
                    _response.Invoke(GearOperationStatus.Timeout);
                    return;
                }
                if (Globals.Test.Gears.GearsFails.Contains(node.Id))
                {
                    _response.Invoke(GearOperationStatus.Fail);
                    return;
                }
                if (Globals.Test.Gears.GearsLocal.Contains(node.Id))
                {
                    //LogWarn<RCJotron7000>("[SNMP][" + "Device Status GET" + "] [" + node.IP + "] LOCAL MODE ON: " + this.ToString(node.IP),
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_LOCAL_MODE_ON,
                    //    new object[] { Id });
                    LogWarn<RCJotron7000>("[SNMP][" + "Device Status GET" + "] [" + node.IP + "] LOCAL MODE ON: " + this.ToString(node.IP),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,
                        Id, "Modo LOCAL");
                    _response.Invoke(GearOperationStatus.Fail);
                    return;
                }

                _response.Invoke(GearOperationStatus.OK);
                return;
            }
#endif

            _thread = new Thread(new ParameterizedThreadStart(DeviceStatusGet)) { IsBackground = true };
            _thread.Start(node);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="node"></param>
        public void FrecuencyGet(Action<String> response, BaseNode node)
        {
            _responseString = response;

            _thread = new Thread(new ParameterizedThreadStart(DeviceFrecuencyGet)) { IsBackground = true };
            _thread.Start(node);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="response"></param>
        /// <param name="node"></param>
        /// <param name="isEmitter"></param>
        /// <param name="isMaster"></param>
        public void ConfigureNode(RCConfigurationAction action, Action<GearOperationStatus> response, BaseNode node, Boolean isEmitter, Boolean isMaster)
        {
            _response = response;

            switch (action)
            {
                case RCConfigurationAction.Assing:
                    _thread = new Thread(new ParameterizedThreadStart(DeviceConfigure)) { IsBackground = true };
                    _thread.Start(new Object[] { node, isEmitter, isMaster});
                    break;

                case RCConfigurationAction.Unassing:
                    response.Invoke(GearOperationStatus.OK);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void DeviceConfigure(Object input)
        {
            // JOI: 20171031 ERROR #3231
            Int32 potencia_convertida = 0;

			// JOI. CONTROL_SET_SIP
			 System.Threading.Thread.Sleep(delaySetFrequencyMs);
            lock (_thread)
            {
                // Parse
                if (!(input is Object[]))
                    throw new NotImplementedException();
                Object[] inputParsed = (Object[])input;

                if (!(inputParsed[0] is BaseNode))
                    throw new NotImplementedException();
                if (!(inputParsed[1] is Boolean))
                    throw new NotImplementedException();
                if (!(inputParsed[2] is Boolean))
                    throw new NotImplementedException();

                BaseNode parsed = (BaseNode)inputParsed[0];
                Boolean isEmitter = (Boolean)inputParsed[1];
                Boolean isMaster = (Boolean)inputParsed[2];
                GearOperationStatus output;
                //20161102 JOI: Si es Master no se configura ningún parámetro. Retornamos OK.
                if (isMaster)
                {
                    output = GearOperationStatus.OK;
                }
                else
                {
                    // Fecuency
                    output = SNMPFrecuencySet(
                        parsed.IP,
                        parsed.Frecuency,
                        isEmitter);

                    // Channel Spacing
                    if (output == GearOperationStatus.OK)
                        output = SNMPChannelSpacingSet(
                        parsed.IP,
                        parsed.Channeling,
                        isEmitter,
                        false);

                    // Modulation
                    if (output == GearOperationStatus.OK)
                        output = SNMPModulationSet(
                        parsed.IP,
                        parsed.Modulation,
                        isEmitter,
                        false);

                    if (isEmitter)
                    {
                        // Carrier Offset
                        if (output == GearOperationStatus.OK)
                            output = SNMPCarrierOffsetSet(
                            parsed.IP,
                            parsed.Offset,
                            isEmitter,
                            false);

                        // JOI: 20171031 ERROR #3231
                        if (null != parsed.Power && output == GearOperationStatus.OK && parsed.Power != 0)
                        {
                            // JOI 20180425 CONVERSION POTENCIA
                            bool convierte = (parsed.Power < PowerLevelMin);
                            if (convierte)
                            {
                                potencia_convertida = ConvertWattTodBm((Int32)parsed.Power);
                            }
                            output = SNMPPowerSet(
                            parsed.IP,
                            convierte ? (Int32)potencia_convertida : (Int32)parsed.Power,
                            isEmitter,
                            false);
                        }
                    }
                }
                try
                {
                    _response.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCJotron7000>("DeviceConfigure", x, false);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void DeviceFrecuencyGet(Object input)
        {
            lock (_thread)
            {
                if (!(input is BaseNode))
                    throw new NotImplementedException();

                String output = SNMPFrecuencyGet(((BaseNode)input).IP, ((BaseNode)input).IsEmitter);

                try
                {
                    _responseString.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCJotron7000>("DeviceFrecuencyGet", x, false);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void DeviceStatusGet(Object input)
        {
            // JOI. CONTROL_SIP  
            GearOperationStatus output_Sip = GearOperationStatus.OK;
            GearOperationStatus output_MCC = GearOperationStatus.OK;

            lock (_thread)
            {
                if (!(input is BaseNode))
                    throw new NotImplementedException();

                GearOperationStatus output = SNMPDeviceStatusGet(
                    ((BaseNode)input).IP,
                    ((BaseNode)input).IsEmitter,
                    RCSessionTypes.Remote);

                // JOI: 20171031 ERROR #3231
                if (output == GearOperationStatus.OK && ((BaseNode)input).IsMaster == true && ((BaseNode)input).Power == 0 && ((BaseNode)input).IsEmitter == true)
                {
                    GetPower(input);
                }
                // JOI: 20171031 ERROR #3231

                // JOI. CONTROL_SIP  
                if (bcontrol_sip)
                {
                    output_Sip = SNMPSessionListGet(((BaseNode)input).IP, sipNdbx);
                }
                //JOI FREC_DES
                if (output == GearOperationStatus.OK && (((BaseNode)input).IsMaster == true) && ((BaseNode)input).Frecuency != "")
                {
                    output_MCC = SNMPMasterControlConfig(((BaseNode)input).IP, ((BaseNode)input).IsMaster, ((BaseNode)input).Frecuency);
                    if (output_MCC != GearOperationStatus.OK)
                    {
                        LogInfo<RCJotron7000>(" Equipo Master con frecuencia no reconocida ",
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Master. Frecuencia inválida. Se deshabilita equipo."));
                    }
                }

                try
                {
                    // JOI. CONTROL_SIP FIN
                    if (output != GearOperationStatus.OK)
                        _response.Invoke(output);
                    else if (bcontrol_sip && output_Sip != GearOperationStatus.OK)
                        _response.Invoke(output_Sip);
                    else if (output_MCC == GearOperationStatus.FailMasterConfig)
                        _response.Invoke(output_MCC);
                    else
                        _response.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCJotron7000>("DeviceStatusSet", x, false);
                }
            }
        }

        // JOI: 20171031 ERROR #3231
        /// <summary>
        /// Obtención del valor de potencia del equipo Master
        /// </summary>
        /// <param name="input"></param>
        private void GetPower(Object input)
        {
            Int32 Power = 0;
            Power = SNMPPowerGet(((BaseNode)input).IP);
            if (Power != 0 && (Power > PowerLevelMax || Power < PowerLevelMin))
            {
                ((BaseNode)input).Power = PowerLevelDefault;
            }
            else
                ((BaseNode)input).Power = Power;
        }
        // JOI: 20171031 ERROR #3231        
        #region Logic - SNMP Conexion

        /// <summary>
        /// Funcion para agrupar la gestion de excepciones de las conexion SNMP
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logMethod"></param>
        /// <param name="targetIp"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private GearOperationStatus ExceptionTreatment(
            Exception ex, String logMethod, String targetIp, String value = "",
            U5kiIncidencias.U5kiIncidencia type = U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR)
        {
            // Timeout devuelto por la libreria de SNMP.
            if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
            {
                LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] Timeout: " + this.ToString(targetIp));
                _lastExceptions[targetIp] = ex.GetType();
                return GearOperationStatus.Timeout;
            }

            // Error devuelto por la libreria del SNMP, controlamos el 16 solo, que es el error por que este en modo LOCAL
            if (ex is Lextm.SharpSnmpLib.Messaging.Error /* .ErrorException*/
                && ((Lextm.SharpSnmpLib.Messaging.Error/*Exception*/)ex).Body.ToString().Contains("status: 16"))
            {
                if (_lastExceptions[targetIp] != ex.GetType()) // Validate don't shot the same exception again.
                {
                    // 20160809. Cambio de Texto para la Incidencia U5KI_NBX_NM_GEAR_LOCAL_MODE_ON
                    //LogWarn<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] LOCAL MODE ON: " + this.ToString(targetIp),
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_LOCAL_MODE_ON);
                    LogWarn<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] LOCAL MODE ON: " + this.ToString(targetIp),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR, 
                        Id, "Modo LOCAL");

                    _lastExceptions[targetIp] = ex.GetType();
                }
                return GearOperationStatus.Fail;
            }

            // Socket Exception, que es el POSIBLE error, entre otros, por que este en modo applicacion interactiva.
            if (ex is SocketException)
            {
                if (_lastExceptions.ContainsKey(targetIp) && _lastExceptions[targetIp] != ex.GetType()) // Validate don't shot the same exception again.
                {
                    //LogWarn<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] SOCKET EXCEPTION (Posible modo Interactivo): " + this.ToString(targetIp),
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_SOCKET_ERROR);
                    LogWarn<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] SOCKET EXCEPTION (Posible modo Interactivo): " + this.ToString(targetIp),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,
                        Id, "SOCKET EXCEPTION (Posible modo Interactivo)");
                    _lastExceptions[targetIp] = ex.GetType();
                }
                return GearOperationStatus.Fail;
            }

            // Resto de casos, Excepcion no esperada 
            //LogError<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] ERROR: " 
            //    + this.ToString(targetIp) + ". EXCEPTION: " + ex.Message,
            //    type);
            LogError<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] ERROR: "
                + this.ToString(targetIp) + ". EXCEPTION: " + ex.Message,
                type,
                Id, String.Format("Excepcion No Esperada. Ver LOG {0}", DateTime.Now.ToString()));

            LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] [" + targetIp + "] [Value: " + value + "] ERROR DETAIL: " + ex.ToString()
                    + Environment.NewLine + "[SNMP][" + logMethod + "] [" + targetIp + "] STACK TRACE: " + ex.StackTrace);

            _lastExceptions[targetIp] = ex.GetType();
            //20180319 JOI INHABILITO POR ERROR SNMP
            //return GearOperationStatus.FailProtocolSNMP;
            return GearOperationStatus.Fail;

        }
        /// <summary>
        /// 
        /// </summary>

        /// <summary>
        /// Obtiene el estado del equipo. 
        /// Devuelve 19 valores en 0s y 1s que son booleanos sobre diferentes variables de estado del equipo. 
        /// Utilizmos esto para ver que logs debemos dejar. A continuación una copia de la documentación relevante: 
        /// PARA MAS INFO: Ver las notas en la carpeta OIDs, el Recurso RCJotron7000/DeviceStatus.
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="sessionType"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPDeviceStatusGet(String targetIp, bool isEmitter, RCSessionTypes sessionType)
        {
            String logMethod = "DEVICE STATUS GET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    //20170718 JOI
                    if (bcontrol_alarmas_permanentes)
                    {
                        //20170619 JOI
                        if (GetAlarmasPermanentes(targetIp, isEmitter) != GearOperationStatus.OK)
                            return GearOperationStatus.Fail;
                    }

                    Byte[] sStatus = { 0 };
                    Byte[] DeviceStatus = { 0 };
                    // Se obtiene estado de la oid bsOperState Interfaz JOTRON
                    DeviceStatus = SnmpClient.GetOctectString(
                    targetIp,
                    Community,
                    u5ki.RemoteControlService.OIDs.RCJotron7000.DeviceStatus,
                    SNMPCallTimeout,
                    Port,
                    SNMPVersion);
                    if (DeviceStatus.Length < 6) // Data library snmp  tipo(1), long.(1), datos(4)"
                    {
                        return GearOperationStatus.Fail;
                    }

                    //BLOQUE 2 alarma bit 3 standby bit 4//
                    Buffer.BlockCopy(DeviceStatus, 2, sStatus, 0, 1);
                    BitArray bits = new BitArray(sStatus);
                    if (bits[3] == true ||
                        bits[1] == true)
                        return GearOperationStatus.Fail;

                    // BLOQUE 3 low bit 7 mute bit 5 fail bit 3
                    Buffer.BlockCopy(DeviceStatus, 3, sStatus, 0, 1);
                    bits = new BitArray(sStatus);
                    if (bits[3] == true ||
                        bits[5] == true ||
                        bits[7] == true)
                        return GearOperationStatus.Fail;

                    // Se obtiene estado de la oid grOpStat  Interfaz euroControl atcCommunication
                    String valueOpStatATC = SnmpClient.GetString(
                    targetIp,
                    Community,
                    u5ki.RemoteControlService.OIDs.RCJotron7000.OperationalStatusATC,
                    SNMPCallTimeout,
                    Port,
                    SNMPVersion);
                    // Parse the response
                    String[] parsedValue = valueOpStatATC.Split(',');
                    GearNumParamOperationalState nparams = (GearNumParamOperationalState)Enum.Parse(typeof(GearNumParamOperationalState), parsedValue[0]);
                    if (nparams != GearNumParamOperationalState.jt_NumParamOk)
                        return GearOperationStatus.Fail;

                    GearActivationCommand command = (GearActivationCommand)Enum.Parse(typeof(GearActivationCommand), parsedValue[1]);
                    GearEventLevelJotron state = (GearEventLevelJotron)Enum.Parse(typeof(GearEventLevelJotron), parsedValue[2]);

                    if (
                        command == GearActivationCommand.NoGo ||
                        state == GearEventLevelJotron.jt_Error
                        )
                        return GearOperationStatus.Fail;

                    // Se obtiene estado de la oid bsInServiceMode Interfaz JOTRON
                    // Control Local 1(true), Control Remoto 2(false)
                    Int32 InServiceMode = SnmpClient.GetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.InServiceMode,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    if (InServiceMode == (Int32)GearInServiceMode.jt_true)
                        return GearOperationStatus.Fail;

                    if (isEmitter)
                    {
                        // Se obtiene configuración de entrada de audio, si es distinta de auto 
                        Int32 iTxAnaModSource = SnmpClient.GetInt(
                            targetIp,
                            Community,
                            u5ki.RemoteControlService.OIDs.RCJotron7000.TxAnaModSource,
                            SNMPCallTimeout,
                            Port,
                            SNMPVersion);
                        if (iTxAnaModSource != (Int32)GearAnalogInputModulationSourceTx.jt_AIMST_Auto)
                            return GearOperationStatus.Fail;
                    }

                    return GearOperationStatus.OK;

                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        private GearOperationStatus GetAlarmasPermanentes(String targetIp, bool isEmitter)
        {
            String logMethod = "DEVICE STATUS GET";
            Byte[] sStatus = { 0 };
            Byte[] DeviceStatus = { 0 };
            bool bAlarmado = false;
            //20170619 JOI: SE CONTROLA STATUS JOTRON CON OID OID_JOTRON_ALARMAS_PERMANENTES_TX
            // O OID_JOTRON_ALARMAS_PERMANENTES_RX
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    // Se obtiene estado de la oid bsOperState Interfaz JOTRON
                    DeviceStatus = SnmpClient.GetOctectString(
                    targetIp,
                    Community,
                    isEmitter ? OID_JOTRON_ALARMAS_PERMANENTES_TX : OID_JOTRON_ALARMAS_PERMANENTES_RX,
                    SNMPCallTimeout,
                    Port,
                    SNMPVersion);
                    if (DeviceStatus.Length < 6) // Data library snmp  tipo(1 Byte) , long.(1 Byte), datos(4 Bytes)"
                    {
                        return GearOperationStatus.Fail;
                    }

                    //Detección estado alarma
                    Buffer.BlockCopy(DeviceStatus, 2, sStatus, 0, 1);
                    BitArray bits = new BitArray(sStatus);
                    // No hay Alarmas
                    if (bits[7] == false)
                    {
                        return GearOperationStatus.OK;
                    }
                    // Analizamos las alarmas.
                    if (isEmitter == true)
                    {
                        bAlarmado = HayAlarmaPermanenteTx(DeviceStatus);
                    }
                    else
                    {
                        bAlarmado = HayAlarmaPermanenteRx(DeviceStatus);
                    }
                    if (bAlarmado == true)
                        return GearOperationStatus.Fail;
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        private bool HayAlarmaPermanenteTx( Byte[] deviceStatus )
        {
            Byte[] sStatus = { 0 };

            // Analizamos primer bloque
            // txAlarm(7)---->Viene activo en este punto, txPaModuleAlarm(6), txModModuleAlarm(5), txFrontModuleAlarm(4),
            // txMainModuleAlarm(3),txExternalAlarm(2),txForcedAlarm(1)---->No se gestiona,txExtUnitAlarm(0)
            Buffer.BlockCopy(deviceStatus, 2, sStatus, 0, 1);
            BitArray bits = new BitArray(sStatus);
            if (bits[6] == true || bits[5] == true ||bits[4] == true ||bits[3] == true ||
                bits[2] == true ||bits[0] == true)
                return true;

            // Analizamos segundo bloque
            // txPASWRAlarm(7),txPACurrentAlarm(6),txPATemperatureAlarm(5),txPA28V0Alarm(4),
            // txPA12V0Alarm(3),txPA5V0Alarm(2),txPA3V3Alarm(1),txPA5V0Neg(0)
            Buffer.BlockCopy(deviceStatus, 3, sStatus, 0, 1);
            BitArray bits1 = new BitArray(sStatus);
            if (bits1[7] == true || bits1[6] == true || bits1[5] == true || bits1[4] == true ||
                bits1[3] == true || bits1[2] == true || bits1[1] == true || bits1[0] == true)
                return true;

            // Analizamos tercer bloque
            // txPAFanFailure(7),txPAPwrOutAlarm(6),txRFTuneAlarm(5),txModLoLvlAlarm(4),
            // txModLoLockAlarm(3),txMod6V0Alarm(2),txPowerACAlarm(1),txMainInStby(0),
            Buffer.BlockCopy(deviceStatus, 4, sStatus, 0, 1);
            BitArray bits2 = new BitArray(sStatus);
            if (bits2[7] == true || bits2[6] == true || bits2[5] == true || bits2[4] == true ||
                bits2[3] == true || bits2[2] == true || bits2[1] == true || bits2[0] == true)
                return true;

            // Analizamos cuarto bloque
            // txMainEthernetAlarm(7),txMainCodecAlarm(6),txMainSPIAlarm(5),txMainFrontAlarm(4)
            // txMainRemExpAlarm(3),txMainBiteADCAlarm(2),txMainMemAlarm(1),txSpareAlarm31(0)
            Buffer.BlockCopy(deviceStatus, 5, sStatus, 0, 1);
            BitArray bits3 = new BitArray(sStatus);
            if (bits3[7] == true || bits3[6] == true || bits3[5] == true || bits3[4] == true ||
                bits3[3] == true || bits3[2] == true || bits3[1] == true || bits3[0] == true)
                return true;

            return false;
        }


        private bool HayAlarmaPermanenteRx(Byte[] deviceStatus)
        {
            Byte[] sStatus = { 0 };

            // Analizamos primer bloque
            // rxAlarm(7)---->Viene activo en este punto, rxRfModuleAlarm(6), rxPowerModuleAlarm(5), rxFrontModuleAlarm(4),
            // rxMainModuleAlarm(3),rxExternalAlarm(2),rxForcedAlarm(1)---->No se gestiona,rxExtUnitAlarm(0)
            Buffer.BlockCopy(deviceStatus, 2, sStatus, 0, 1);
            BitArray bits = new BitArray(sStatus);
            if (bits[6] == true || bits[5] == true || bits[4] == true || bits[3] == true ||
                bits[2] == true || bits[0] == true)
                return true;

            // Analizamos segundo bloque
            // rxRFLoLvlAlarm(7),rxRFLoLockAlarm(6),rxRF6V0Alarm(5),rxRFLNACurrentAlarm(4),
            // rxRFIFCurrentAlarm(3),rxRF30V0Alarm(2),rxSpareAlarm14(1),rxPowerACAlarm(0)
            Buffer.BlockCopy(deviceStatus, 3, sStatus, 0, 1);
            BitArray bits1 = new BitArray(sStatus);
            if (bits1[7] == true || bits1[6] == true || bits1[5] == true || bits1[4] == true ||
                bits1[3] == true || bits1[2] == true || bits1[1] == true || bits1[0] == true)
                return true;

            // Analizamos tercer bloque
            // rxPower12V0Alarm(7),rxPower5V0Alarm(6),rxPower3V3Alarm(5),rxPowerTempAlarm(4),
            // rxPowerCurrentAlarm(3),rxPowerDCInputAlarm(2),rxCodecLDAlarm(1),rxMainAGCAlarm(0)
            Buffer.BlockCopy(deviceStatus, 4, sStatus, 0, 1);
            BitArray bits2 = new BitArray(sStatus);
            if (bits2[7] == true || bits2[6] == true || bits2[5] == true || bits2[4] == true ||
                bits2[3] == true || bits2[2] == true || bits2[1] == true || bits2[0] == true)
                return true;

            // Analizamos cuarto bloque
            // rxMainEthernetAlarm(7),rxMainCodecAlarm(6),rxMainSPIAlarm(5)rxMainFrontAlarm(4),
            // rxMainRemExpAlarm(3),rxMainBiteADCAlarm(2),rxMainMemAlarm(1),rxMainIFAlarm(0)
            Buffer.BlockCopy(deviceStatus, 5, sStatus, 0, 1);
            BitArray bits3 = new BitArray(sStatus);
            if (bits3[7] == true || bits3[6] == true || bits3[5] == true || bits3[4] == true ||
                bits3[3] == true || bits3[2] == true || bits3[1] == true || bits3[0] == true)
                return true;

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="frecuency"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPFrecuencySet(String targetIp, String frecuency, bool isEmitter, Boolean openSession = true)
        {
            String logMethod = "FRECUENCY SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp,isEmitter, RCSessionTypes.Remote);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            
            frecuency = frecuency.Replace(".", "");
            frecuency = frecuency.Replace(",", "");
            int frecuencia = Convert.ToInt32(frecuency);
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.Frecuency,
                        frecuencia,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, frecuency,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, frecuency,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private String SNMPFrecuencyGet(String targetIp, bool isEmitter)
        {
            String logMethod = "FRECUENCY GET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return "ERROR: ByPass Active.";
#endif
            GearOperationStatus status = SNMPDeviceStatusGet(targetIp, isEmitter, RCSessionTypes.Remote);
            if (status != GearOperationStatus.OK)
                return "ERROR: Not able to read.";
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    Int32 frecuency = SnmpClient.GetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.Frecuency,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    return frecuency.ToString();
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            ExceptionTreatment(ex, logMethod, targetIp);
                            return null;
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
                        return null;
                    }
                }

            }
            return "0000000";
            // JOI: 20170831 FIN
        }

        ////JOI
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modulation"></param>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private GearChannelSpacingsJotorn ConvertChannelSpacingSet(GearChannelSpacings channelSpacing, String targetIp)
        {

            String logMethod = "CNV CHANNEL SPACING SET";

            try
            {
                switch (channelSpacing)
                {
                    case GearChannelSpacings.kHz_8_33:
                        return GearChannelSpacingsJotorn.jt_kHz_8_33;
                    case GearChannelSpacings.kHz_25_00:
                        return GearChannelSpacingsJotorn.jt_kHz_25_00;
                    default:
                        return GearChannelSpacingsJotorn.jt_kHz_25_00;
                }
            }

            catch (Exception ex)
            {
                LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] value: Invalid" + ex + ". " + ToString(targetIp));
                return GearChannelSpacingsJotorn.jt_kHz_25_00;
            }
        }
        ////JOI FIN
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="channelSpacing"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPChannelSpacingSet(String targetIp, GearChannelSpacings channelSpacing, bool isEmitter, Boolean openSession = true)
        {
            String logMethod = "CHANNEL SPACING SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, isEmitter, RCSessionTypes.Remote);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.ChannelSpacing,
                        Convert.ToInt32(ConvertChannelSpacingSet(channelSpacing, targetIp)),
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, channelSpacing.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, channelSpacing.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        ////JOI
        /// <summary>
        /// 
        /// </summary>
        /// <param name="carrierOffstatus"></param>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private GearCarrierOffStatusJotron ConvertCarrierOffsetJotron(GearCarrierOffStatus carrierOffstatus, String targetIp)
        {


            String logMethod = "CNV CARRIEROFF SET";

            try
            {
                switch (carrierOffstatus)
                {
                    case GearCarrierOffStatus.Off:
                        return GearCarrierOffStatusJotron.Off;
                    case GearCarrierOffStatus.kHz_7_5:
                        return GearCarrierOffStatusJotron.kHz_7_5;
                    case GearCarrierOffStatus.kHz_5_0:
                        return GearCarrierOffStatusJotron.kHz_5_0;
                    case GearCarrierOffStatus.kHz_2_5:
                        return GearCarrierOffStatusJotron.kHz_2_5;
                    case GearCarrierOffStatus.Hz_0_0:
                        return GearCarrierOffStatusJotron.Hz_0_0;
                    case GearCarrierOffStatus.kHz_minus_2_5:
                        return GearCarrierOffStatusJotron.kHz_minus_2_5;
                    case GearCarrierOffStatus.kHz_minus_5_0:
                        return GearCarrierOffStatusJotron.kHz_minus_5_0;
                    case GearCarrierOffStatus.kHz_minus_7_5:
                        return GearCarrierOffStatusJotron.kHz_minus_7_5;
                    case GearCarrierOffStatus.kHz_8:
                        return GearCarrierOffStatusJotron.kHz_8;
                    case GearCarrierOffStatus.kHz_4:
                        return GearCarrierOffStatusJotron.kHz_4;
                    case GearCarrierOffStatus.kHz_minus_4:
                        return GearCarrierOffStatusJotron.kHz_minus_4;
                    case GearCarrierOffStatus.kHz_minus_8:
                        return GearCarrierOffStatusJotron.kHz_minus_8;
                    case GearCarrierOffStatus.kHz_7_3:
                        return GearCarrierOffStatusJotron.kHz_7_3;
                    case GearCarrierOffStatus.kHz_minus_7_3:
                        return GearCarrierOffStatusJotron.kHz_minus_7_3;
                    default:
                        return GearCarrierOffStatusJotron.Off;
                }

            }

            catch (Exception ex)
            {
                LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] value: Invalid" + ex + ". " + ToString(targetIp));
                return GearCarrierOffStatusJotron.Off;
            }
        }
        ////JOI FIN

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="carrierOffstatus"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPCarrierOffsetSet(String targetIp, GearCarrierOffStatus carrierOffstatus, bool isEmitter, Boolean openSession = true)
        {
            String logMethod = "CARRIEROFF SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, isEmitter, RCSessionTypes.Remote);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.CarrierOffStatus,
                        Convert.ToInt32(ConvertCarrierOffsetJotron(carrierOffstatus, targetIp)),
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, carrierOffstatus.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, carrierOffstatus.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        ////JOI
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modulation"></param>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private GearModulationsJotron ConvertModulationsJotron(GearModulations modulation, String targetIp)
        {

            String logMethod = "CNV MODULATION SET";

            try
            {
                switch (modulation)
                {
                    case GearModulations.AM:
                        return GearModulationsJotron.AM;
                    case GearModulations.VDL2:
                        return GearModulationsJotron.VDL2;
                    default:
                        return GearModulationsJotron.AM;
                }
            }

            catch (Exception ex)
            {
                LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] value: Invalid" + ex + ". " + ToString(targetIp));
                return GearModulationsJotron.AM;
            }
        }
        ////JOI FIN

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="modulation"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPModulationSet(String targetIp, GearModulations modulation, bool isEmitter, Boolean openSession = true)
        {
            String logMethod = "MODULATION SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, isEmitter, RCSessionTypes.Remote);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.Modulation,
                        Convert.ToInt32(ConvertModulationsJotron(modulation, targetIp)),
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] value: " + modulation + ". " + ToString(targetIp));
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, modulation.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, modulation.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="level"></param>
        /// <param name="power"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        //private GearOperationStatus SNMPPowerSet(String targetIp, GearPowerLevels level, Int32 power, bool isEmitter, Boolean openSession = true)
        private GearOperationStatus SNMPPowerSet(String targetIp, Int32 power, bool isEmitter, Boolean openSession = true)
        {
            String logMethod = "POWER SET";

#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, isEmitter,RCSessionTypes.Remote);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {

                    // Set the power level.
                    SnmpClient.SetInt(
                        targetIp,
                        Community,
                        OID_JOTRON_TXAMPOWERFINE, // JOI: 20171031 ERROR #3231
                        power,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);


                    LogTrace<RCJotron7000>("[SNMP][" + logMethod + "] value: [" + power + "]. " + ToString(targetIp));// JOI: 20171031
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (i_rtimeout >= NUMMAXTimeout)
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, power.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
                i_rtimeout++;
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        #endregion
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="Oid"></param>
        /// <param name="sessionType"></param>
        /// <param name="shortString"></param>
        /// <returns></returns>
        public string ToString(String ip = "", String Oid = "", RCSessionTypes sessionType = RCSessionTypes.Remote, Boolean shortString = true)
        {
            if (shortString)
                return " {IP: " + ip + "}"
                    + " {Port: " + Port + "}"
                    + " {Community: " + Community + "}"
                    + " {SNMPVersion: " + SNMPVersion + "}";
            else
                return " {IP: " + ip + "}"
                    + " {Port: " + Port + "}"
                    + " {OID: " + Oid + "}"
                    + " {SessionType: " + sessionType + "}"
                    + " {Community: " + Community + "}"
                    + " {SNMPVersion: " + SNMPVersion + "}"
                    + " {SNMPCallTimeout: " + SNMPCallTimeout + "}"
                    + " {SessionTimeout: " + SessionTimeout + "}";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString();
        }

        /// <summary>
        /// Obtiene las sesiones SIP establecidas con la radio. Formato: N,"xxx@0.0.0.0:nnn","xxx@0.0.0.0:nnn" N-Número de sesiones.. ID-SIP, ID-SIP.. 
        /// MIB EUROCAE-ATC
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns>GearOperationStatus</returns>
        private GearOperationStatus SNMPSessionListGet(String targetIp, String sipNdbx)
        {

            String logMethod = "SESSION LIST GET";
            const int CONTROL_DATOS_VARIAS_SESIONES = 3;
            bool bSesionEstablecida = false;
            bool bErrorSesionesNdbx = false;
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {

                    String value_sesions_sip = SnmpClient.GetString(
                        targetIp,
                        Community,
                        OID_EUROCONTROL_VOSIPSESSIONLIST,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    if (!String.IsNullOrEmpty(value_sesions_sip))
                    {
                        String[] parsedValueSessionSip = value_sesions_sip.Split(',');
                        int sesiones = parsedValueSessionSip.Length;
                        if (sesiones >= CONTROL_DATOS_VARIAS_SESIONES)
                        {
                            for (int ind = 1; ind < sesiones; ind++)
                            {
                                if (parsedValueSessionSip[ind].Contains(sipNdbx))
                                {
                                    if (bSesionEstablecida == true)
                                    {
                                        bErrorSesionesNdbx = true;
                                        break;
                                    }
                                    else
                                        bSesionEstablecida = true;

                                }
                            }

                        }
                        if (bErrorSesionesNdbx == true)
                        {
                            LogWarn<RCJotron7000>("[SNMP][" + "Device Status GET" + "] [" + ToString(targetIp) + "] SESSION SIP: " + this.ToString(targetIp),
                             U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,
                            Id, CTranslate.translateResource("Número de sesiones SIP superadas"));
                            return GearOperationStatus.FailSessionsSip;
                        }
                    }
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }

        /// <summary>
        /// Controla los diferentes parámetros de configuración del equipo Master (Monocanal).
        /// Inicialmente solo se controla la frecuencia.
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="isMaster"></param>
        /// <param name="sFrecuencia"></param>
        /// <param name="sessionType"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPMasterControlConfig(String targetIp, bool isMaster, string sFrecuencia)
        {
            String logMethod = "MASTER CONTROL CONFIG";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    // Control de frecuencia.
                    string[] frecuencia = new string[] { sFrecuencia };
                    Int32 frecuency = SnmpClient.GetInt(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCJotron7000.Frecuency,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    string frecuenciaEquipo = frecuency.ToString();

                    frecuencia[0] = frecuencia[0].Replace(".", "");
                    frecuencia[0] = frecuencia[0].Replace(",", "");

                    if (frecuenciaEquipo != frecuencia[0])
                    {
                        return GearOperationStatus.FailMasterConfig;
                    }

                    // Control de frecuencia. Fin
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }
        // JOI: 20171031 ERROR #3231 
        /// <summary>
        /// Obtiene la potencia de transmisión del equipo.
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private Int32 SNMPPowerGet(String targetIp)
        {
            String logMethod = "POWER GET";
            Int32 power = 0;
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return 0;
#endif
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    // Control de potencia.
                    power = SnmpClient.GetInt(
                    targetIp,
                    Community,
                    OID_JOTRON_TXAMPOWERFINE,
                    SNMPCallTimeout,
                    Port,
                    SNMPVersion);
                    return power;
                }

                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            ExceptionTreatment(ex, logMethod, targetIp);
                            return power;
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
                        return power;
                    }
                }
            }
            return power;

        }
        // JOI: 20171031 ERROR #3231 
        // JOI: 20171031 ERROR #3231  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="watt"></param>
        private Int32 ConvertWattTodBm(Int32 pot)
        {
            Double watt = pot;
            Int32 dBm = 0;

            dBm = (Int32)( 10 * (10 * Math.Log10((watt/1)) + 30));

            if ( dBm < PowerLevelMin)
            {
                dBm = PowerLevelMin;
                LogInfo<RCRohde4200>(" Equipo configurado con potencia mínima por defecto ",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("Asignación potencia mínima en equipo N Jotron "));
            }
            else if (dBm > PowerLevelMax)
            {
                dBm = PowerLevelMax;
                LogInfo<RCRohde4200>(" Equipo configurado con potencia por defecto ",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("Asignación potencia máxima en equipo N Jotron "));
            }
            return dBm;
        }
        // JOI: 20171031 ERROR #3231 
        #endregion

    }
}

using NLog;
using System;
using System.Collections.Generic;
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

using Lextm.SharpSnmpLib;

using Translate;

namespace u5ki.RemoteControlService
{

    /// <summary>
    /// Esta es la clase que engloba la funcionalidad principal del telemando de Rohde modelo 4200.
    /// </summary>
    public class RCRohde4200 : BaseCode, IRemoteControl
    {

        #region Declarations

        /// <summary>
        /// 
        /// </summary>
        public readonly Int32 Port = 161;
        public readonly String Community = "public";
        public readonly Int32 SessionTimeout = 999;
        public readonly VersionCode SNMPVersion = VersionCode.V2;
        public readonly Int32 SNMPCallTimeout = 500; // Miliseconds = 0,5 seconds
        public readonly Int32 SNMPSetCallTimeout = Convert.ToInt32(u5ki.RemoteControlService.Properties.Settings.Default.SNMPSetCallTimeout);
        public readonly Int32 NUMMAXTimeout = 1;

        // JOI: 20171031 ERROR #3231 
        public readonly UInt32 PowerLevelMin = 5;
        public readonly UInt32 PowerLevelLowMax = 20;
        public readonly UInt32 PowerLevelMax = 50;
        public readonly UInt32 PowerLevelLowDefault = 10;
        public readonly UInt32 PowerLevelNormalDefault = 50;
        // JOI: 20171031 ERROR #3231

        // private static Logger _logger = LogManager.GetCurrentClassLogger();
        public string Name { get { return U5ki.Infrastructure.Resources.ServiceNames.RemoteControlService; } }
        private Action<GearOperationStatus> _response;
        private Action<String> _responseString;
        private Thread _thread;
        /// <summary>
        /// Variable para control de las excecpciones. 
        /// Se utiliza para evitar que cada iteracion/comprobación que un equipo no este respondiendo, 
        /// no genere continuamente eventos de historico, solo la primera vez.
        /// </summary>
        IDictionary<String, Type> _lastExceptions = new Dictionary<String, Type>();

        // JOI. CONTROL_SIP
        private bool bcontrol_sip = u5ki.RemoteControlService.Properties.Settings.Default.ControlSessionSIP;
        const string OID_EUROCONTROL_VOSIPSESSIONLIST = "1.3.6.1.4.1.2363.6.1.1.8.0";
        private string sipNdbx { get { return Properties.Settings.Default.SipUser + "@" + Properties.Settings.Default.SipIp; } }
        // JOI. CONTROL_SIP FIN. 
		
        // JOI. CONTROL_SET_SIP
        private int delaySetFrequencyMs = Convert.ToInt32(u5ki.RemoteControlService.Properties.Settings.Default.DelaySetFrequencyMs);
        // JOI. CONTROL_SET_SIP FIN.		
        #endregion
        /// <summary>
        /// 
        /// </summary>
        public RCRohde4200()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public RCRohde4200(Int32 port)
        {
            Port = port;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sNMPVersion"></param>
        public RCRohde4200(Int32 port, VersionCode sNMPVersion)
        {
            Port = port;
            SNMPVersion = sNMPVersion;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sNMPVersion"></param>
        /// <param name="community"></param>
        /// <param name="sessionTimeout"></param>
        public RCRohde4200(Int32 port, VersionCode sNMPVersion, String community, Int32 sessionTimeout)
        {
            Port = port;
            SNMPVersion = sNMPVersion;
            Community = community;
            SessionTimeout = sessionTimeout;
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
                    //LogWarn<RCRohde4200>(Id,U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_LOCAL_MODE_ON);
                    LogWarn<RCRohde4200>(Id, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR, 
                        Id, "Modo LOCAL");
                    _response.Invoke(GearOperationStatus.Fail);
                    return;
                }

                _response.Invoke(GearOperationStatus.OK);
                return;
            }
#endif
            _thread = new Thread(new ParameterizedThreadStart(DeviceStatusGet)) { IsBackground = true, Name="Check: "+node.Id };
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

            switch(action)
            {
                case RCConfigurationAction.Assing:
                    _thread = new Thread(new ParameterizedThreadStart(DeviceConfigure)) { IsBackground = true };
                    _thread.Start(new Object[] { node, isEmitter, isMaster});
                    break;

                case RCConfigurationAction.Unassing:
#if DEBUG
                    LogManager.GetCurrentClassLogger().Fatal("ConfigureNode-UnAssing {0}. Invoke OK.", Id);
#endif
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

                BaseNode parsed = (BaseNode)inputParsed[0];
                Boolean isEmitter = (Boolean)inputParsed[1];
                Boolean isMaster = (Boolean)inputParsed[2];
                //20161102 JOI: Si es Master no se configura ningún parámetro. Retornamos OK.
                GearOperationStatus output;
                if (isMaster)
                {
                    output = GearOperationStatus.OK;
                }
                else
                {
                    // Fecuency
                    output = SNMPFrecuencySet(
                        parsed.IP,
                        parsed.Frecuency);

                    // Channel Spacing
                    if (output == GearOperationStatus.OK)
                        output = SNMPChannelSpacingSet(
                        parsed.IP,
                        parsed.Channeling,
                        false);

                    // Modulation
                    if (output == GearOperationStatus.OK)
                        output = SNMPModulationSet(
                        parsed.IP,
                        parsed.Modulation,
                        false);

                    if (isEmitter)
                    {
                        // Carrier Offset
                        if (output == GearOperationStatus.OK)
                            output = SNMPCarrierOffsetSet(
                            parsed.IP,
                            parsed.Offset,
                            false);

                        // Power
                        // JOI: 20171031 ERROR #3231
                        if (null != parsed.Power && output == GearOperationStatus.OK && parsed.Power != 0) 
                        {
                            //20180725 #3669 Se establece nivel de potencia a Normal
                            parsed.PowerLevel = GearPowerLevels.Normal;
                            // JOI 20180425 CONVERSION POTENCIA
                            bool convierte = (parsed.Power > PowerLevelMax);
                            if (convierte)
                            {
                                potencia_convertida = ConvertdBmToWatt((Int32)parsed.Power);
                            }
                            output = SNMPPowerSet(
                            parsed.IP,
                            parsed.PowerLevel,
                            convierte ? (Int32)potencia_convertida : (Int32)parsed.Power,
                            false);
					     }
                    }
                }
                try
                {
#if DEBUG
                    LogManager.GetCurrentClassLogger().Fatal("DeviceConfigure {0}, Invoke {1}", Id, output);
#endif
                    _response.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCRohde4200>("DeviceConfigure", x, false);
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

                String output = SNMPFrecuencyGet(((BaseNode)input).IP);

                try
                {
                    _responseString.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCRohde4200>("DeviceFrecuencyGet", x, false);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void DeviceStatusSet(Object input)
        {
            lock (_thread)
            {
                if (!(input is BaseNode))
                    throw new NotImplementedException();

                GearOperationStatus output = SNMPDeviceStatusSet(
                    ((BaseNode)input).IP,
                    RCSessionTypes.Monitoring);

                try
                {
#if DEBUG
                    LogManager.GetCurrentClassLogger().Fatal("DeviceStatusSet {0}, Invoke {1}", Id, output);
#endif
                    _response.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCRohde4200>("DeviceStatusSet", x, false);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void DeviceStatusGet(Object input)
        {

            lock (_thread)
            {
                if (!(input is BaseNode))
                    throw new NotImplementedException();

                GearOperationStatus output = SNMPDeviceStatusGet(
                    ((BaseNode)input).IP,
                    RCSessionTypes.Monitoring);
                // JOI: 20171031 ERROR #3231
                if ((output == GearOperationStatus.OK) && ((BaseNode)input).IsMaster == true && ((BaseNode)input).Power == 0 && ((BaseNode)input).IsEmitter == true)
                {
                        GetPowerLevel(input);
                        GetPower(input);
                }
                // JOI: 20171031 ERROR #3231


                // JOI. CONTROL_SIP 
                if (bcontrol_sip && (output == GearOperationStatus.OK))
                {
                    output = SNMPSessionListGet(((BaseNode)input).IP, sipNdbx);
                }
//JOI FREC_DES
                if (output == GearOperationStatus.OK && (((BaseNode)input).IsMaster == true) && ((BaseNode)input).Frecuency != "")
                {
                    output = SNMPMasterControlConfig(((BaseNode)input).IP, ((BaseNode)input).IsMaster, ((BaseNode)input).Frecuency);
                    if (output == GearOperationStatus.FailMasterConfig)
                    {
                        LogInfo<RCRohde4200>(" Equipo Master con frecuencia no reconocida ",
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Master. Frecuencia invalida. Se deshabilita equipo."));
                    }
                }
                try
                {
#if DEBUG
                    //                  LogManager.GetCurrentClassLogger().Fatal("DeviceStatusGet {0}, Invoke {1}", Id, output);
#endif
                        _response.Invoke(output);
                }
                catch (Exception x)
                {
                    ((BaseNode)input).Status = GearStatus.Fail;
                    LogException<RCRohde4200>("DeviceStatusGet", x, false);
                }
            }
        }
        // JOI: 20171031 ERROR #3231
        /// <summary>
        /// Obtención del valor de potencia del equipo Master
        /// </summary>
        /// <param name="input"></param>
        private void GetPowerLevel(Object input)
        {
            UInt32 PowerLevel = 0;    
            PowerLevel = SNMPPowerLevelGet(((BaseNode)input).IP);

            if (PowerLevel == (UInt32)GearPowerLevels.Low)
                ((BaseNode)input).PowerLevel = GearPowerLevels.Low;
            else
              ((BaseNode)input).PowerLevel = GearPowerLevels.Normal;
        }
        // JOI: 20171031 ERROR #3231
        // JOI: 20171031 ERROR #3231
        /// <summary>
        /// Obtención del valor de potencia del equipo Master
        /// </summary>
        /// <param name="input"></param>
        private void GetPower(Object input)
        {
            UInt32 Power = 0;
            if(((BaseNode)input).PowerLevel == GearPowerLevels.Low)
                Power = SNMPPowerLowGet(((BaseNode)input).IP);
            else
                Power = SNMPPowerNormalGet(((BaseNode)input).IP);

            if (Power != 0 && (Power > PowerLevelMax || Power < PowerLevelMin))
            {
                if (((BaseNode)input).PowerLevel == GearPowerLevels.Low)
                    ((BaseNode)input).Power = (Int32)PowerLevelLowDefault;
                else
                    ((BaseNode)input).Power = (Int32)PowerLevelNormalDefault;
            }
            else
                ((BaseNode)input).Power = (Int32)Power;
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
                LogTrace<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] Timeout: " + this.ToString(targetIp));
                _lastExceptions[targetIp] = ex.GetType();
                return GearOperationStatus.Timeout;
            }

            // Error devuelto por la libreria del SNMP, controlamos el 16 solo, que es el error por que este en modo LOCAL.
            if (ex is Lextm.SharpSnmpLib.Messaging.Error/*OJO Exception*/
                && ((Lextm.SharpSnmpLib.Messaging.Error/* OJO Exception*/)ex).Body.ToString().Contains("status: 16"))
            {
                if (_lastExceptions[targetIp] != ex.GetType()) // Validate don't shot the same exception again.
                {
                    // 20160809. Acortar el mensaje del Historico.
                    //LogWarn<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] LOCAL MODE ON/OTHER REMOTE ON: " + this.ToString(targetIp),
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_LOCAL_MODE_ON);
                    LogWarn<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] LOCAL MODE ON/OTHER REMOTE ON: " + this.ToString(targetIp),
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
                    //LogWarn<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] SOCKET EXCEPTION (Posible modo Interactivo): " + this.ToString(targetIp),
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_SOCKET_ERROR);
                    LogWarn<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] SOCKET EXCEPTION (Posible modo Interactivo): " + this.ToString(targetIp),
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,
                        Id, "SOCKET EXCEPTION (Posible modo Interactivo)");
                    _lastExceptions[targetIp] = ex.GetType();
                }
                return GearOperationStatus.Fail;
            }

            // Resto de casos, Excepcion no esperada. 
            //LogError<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] ERROR: " 
            //    + this.ToString(targetIp) + ". EXCEPTION: " + ex.Message,
            //    type);
            
            LogError<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] ERROR: "
                + this.ToString(targetIp) + ". EXCEPTION: " + ex.Message,
                type,
                Id, String.Format("Excepcion No Esperada. Ver LOG {0}", DateTime.Now.ToString()));

            LogTrace<RCRohde4200>("[SNMP][" + logMethod + "] [" + targetIp + "] [Value: " + value + "] ERROR DETAIL: " + ex.ToString()
                    + Environment.NewLine + "[SNMP][" + logMethod + "] [" + targetIp + "] STACK TRACE: " + ex.StackTrace);
            _lastExceptions[targetIp] = ex.GetType();
            //20180319 JOI INHABILITO POR ERROR SNMP
            //return GearOperationStatus.FailProtocolSNMP;
            return GearOperationStatus.Fail;
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="sessionType"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPSessionSet(String targetIp, RCSessionTypes sessionType)
        {
            String logMethod = "SESSION SET";

            if (!_lastExceptions.ContainsKey(targetIp))
                _lastExceptions.Add(targetIp, null);
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            String baseData = this.ToString(targetIp, u5ki.RemoteControlService.OIDs.RCRohde4200.RequestSession, sessionType, false);
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.RequestSession,
                        (UInt32)sessionType,
                        SNMPSetCallTimeout,
                        Port,
                        SNMPVersion);

                    _lastExceptions[targetIp] = null;
                    return GearOperationStatus.OK;
                }

                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, sessionType.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;   
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, sessionType.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
             }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// Variable para control de las excecpciones de tipo Socket, que no esten metiendo Logs continuamente.
        /// </summary>
        IDictionary<String, Type> _lastEx_SNMPSessionGet = new Dictionary<String, Type>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private RCSessionTypes? SNMPSessionGet(String targetIp)
        {
            String logMethod = "SESSION GET";

            if (!_lastEx_SNMPSessionGet.ContainsKey(targetIp))
                _lastEx_SNMPSessionGet.Add(targetIp, null);
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    RCSessionTypes output = (RCSessionTypes)
                        Enum.Parse(
                            typeof(RCSessionTypes),
                            SnmpClient.GetInt(
                                targetIp,
                                Community,
                                u5ki.RemoteControlService.OIDs.RCRohde4200.RequestSession,
                                SNMPCallTimeout,
                                Port,
                                SNMPVersion).ToString());

                    _lastEx_SNMPSessionGet[targetIp] = null;
                    return output;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return RCSessionTypes.Monitoring;
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="sessionType"></param>
        /// <param name="state"></param>
        /// <param name="command"></param>
        /// <param name="sessionState"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPDeviceStatusSet(
            String targetIp,
            RCSessionTypes sessionType,
            GearActivationState state = GearActivationState.Active, 
            GearActivationCommand command = GearActivationCommand.Go, 
            GearSessionStatus sessionState = GearSessionStatus.Remote, 
            Boolean openSession = true)
        {
            String logMethod = "DEVICE STATUS SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            GearOperationStatus status = SNMPSessionSet(targetIp, sessionType);
            if (status != GearOperationStatus.OK)
                return status;

            String input = String.Empty;
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    // See comments on OIDs/RCRohde4200.resx/DeviceStatus.
                    input = "3," + (Int32)state + "," + (Int32)command + "," + (Int32)sessionState;

                    // Get the value.
                    SnmpClient.SetString(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.DeviceStatus,
                        input,
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
                            return ExceptionTreatment(ex, logMethod, targetIp, sessionType.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, sessionType.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
            }
            return GearOperationStatus.OK;
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// Obtiene el estado del equipo. 
        /// Documented Response: CSV [RecordElements, StatActInact, StatGoNogo, StatLocRem].
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="sessionType"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPDeviceStatusGet(String targetIp, RCSessionTypes sessionType)
        {
            String logMethod = "DEVICE STATUS GET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            GearOperationStatus status = SNMPSessionSet(targetIp, sessionType);
            if (status != GearOperationStatus.OK)
                return status;
            // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    // Get the value.
                    String value = SnmpClient.GetString(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.DeviceStatus,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    // Parse the response [See remarks].
                    String[] parsedValue = value.Split(',');
                    GearActivationState state = (GearActivationState)Enum.Parse(typeof(GearActivationState), parsedValue[1]);
                    GearActivationCommand command = (GearActivationCommand)Enum.Parse(typeof(GearActivationState), parsedValue[2]);
                    GearSessionStatus sessionState = (GearSessionStatus)Enum.Parse(typeof(GearActivationState), parsedValue[3]);

                    LogTrace<RCRohde4200>("[SNMP][" + logMethod + "] [" + state.ToString().ToUpper() + "/" + command.ToString().ToUpper() + "/" + sessionState.ToString().ToUpper() + "]: Value: " + value + ". " + ToString(targetIp));
                    if (state != GearActivationState.Active || command != GearActivationCommand.Go || sessionState == GearSessionStatus.Local)
                        return GearOperationStatus.Fail;
                    return GearOperationStatus.OK;
                }
                catch(Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            ExceptionTreatment(ex, logMethod, targetIp);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPTimeoutSet(String targetIp, Int32 timeout)
        {
            String logMethod = "TIMEOUT SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif

            GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
            if (status != GearOperationStatus.OK)
                return status;
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.TimeOut,
                        Convert.ToUInt32(timeout),
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
                            return ExceptionTreatment(ex, logMethod, targetIp, timeout.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;   
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, timeout.ToString(),
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
        /// <param name="frecuency"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPFrecuencySet(String targetIp, String frecuency, Boolean openSession = true)
        {
            String logMethod = "FRECUENCY SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
                if (status != GearOperationStatus.OK)
                    return status;
            }
            
            // Parsear la frecuencia como la necesita el equipo de Rhode.
            frecuency = frecuency.Replace(".", "");
            frecuency = frecuency.Replace(",", "");
			UInt32 uifrequency = Convert.ToUInt32(frecuency);
			if (uifrequency < 1000000)
				uifrequency = uifrequency * 1000;
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.Frecuency,
                        uifrequency,
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
        private String SNMPFrecuencyGet(String targetIp)
        {
            String logMethod = "FRECUENCY GET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return "ERROR: ByPass Active.";
#endif
            GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
            if (status != GearOperationStatus.OK)
                return "ERROR: Not able to read.";
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    UInt32 frecuency = SnmpClient.GetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.Frecuency,
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
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
                    }
                }
            }
            return "000000";
            // JOI: 20170831 FIN
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="channelSpacing"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPChannelSpacingSet(String targetIp, GearChannelSpacings channelSpacing, Boolean openSession = true)
        {
            String logMethod = "CHANNEL SPACING SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
                if (status != GearOperationStatus.OK)
                    return status;
            }
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.ChannelSpacing,
                        Convert.ToUInt32(channelSpacing),
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="carrierOffstatus"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPCarrierOffsetSet(String targetIp, GearCarrierOffStatus carrierOffstatus, Boolean openSession = true)
        {
            String logMethod = "CARRIEROFF SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
                if (status != GearOperationStatus.OK)
                    return status;
            }
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.CarrierOffStatus,
                        Convert.ToUInt32(carrierOffstatus),
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="modulation"></param>
        /// <param name="openSession"></param>
        /// <returns></returns>
        private GearOperationStatus SNMPModulationSet(String targetIp, GearModulations modulation, Boolean openSession = true)
        {
            String logMethod = "MODULATION SET";
#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
                if (status != GearOperationStatus.OK)
                    return status;
            }
             // JOI: 20170831
            int i_rtimeout = 0;
            while (i_rtimeout <= NUMMAXTimeout)
            {
                try
                {
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.Modulation,
                        Convert.ToUInt32(modulation),
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    LogTrace<RCRohde4200>("[SNMP][" + logMethod + "] value: " + modulation + ". " + ToString(targetIp));
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
        private GearOperationStatus SNMPPowerSet(String targetIp, GearPowerLevels level, Int32 power, Boolean openSession = true)
        {
            String logMethod = "POWER SET";

#if DEBUG
            if (Globals.Test.RemoteControlByPass && !Globals.Test.Gears.GearsReal.Contains(targetIp))
                return GearOperationStatus.OK;
#endif
            if (openSession)
            {
                GearOperationStatus status = SNMPDeviceStatusGet(targetIp, RCSessionTypes.Monitoring);
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
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerLevel,
                        (UInt32)level,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    // Set the power of that level.
                    String snmpPowerLevelAdjustMethod = u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerNormal;
                    if (level == GearPowerLevels.Low)
                        snmpPowerLevelAdjustMethod = u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerLow;
                    SnmpClient.SetGauge32(
                        targetIp,
                        Community,
                        snmpPowerLevelAdjustMethod,
                        (UInt32)power,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);

                    LogTrace<RCRohde4200>("[SNMP][" + logMethod + "] value: [" + level + "/" + power + "]. " + ToString(targetIp));
                    return GearOperationStatus.OK;
                }
                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            return ExceptionTreatment(ex, logMethod, targetIp, power.ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        return ExceptionTreatment(ex, logMethod, targetIp, power.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR);
                    }
                }
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
        public string ToString(String ip = "", String Oid = "", RCSessionTypes sessionType = RCSessionTypes.Monitoring, Boolean shortString = true)
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
                            LogWarn<RCRohde4200>("[SNMP][" + "Device Status GET" + "] [" + ToString(targetIp) + "] SESSION SIP: " + this.ToString(targetIp),
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
                    UInt32 frequency = SnmpClient.GetGauge32(
                        targetIp,
                        Community,
                        u5ki.RemoteControlService.OIDs.RCRohde4200.Frecuency,
                        SNMPCallTimeout,
                        Port,
                        SNMPVersion);
                    if (frequency > 1000000)
                    {
                        frequency = frequency / 1000;
                    }
                    string frecuenciaEquipo = frequency.ToString();

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
        /// Obtiene la potencia de transmisión del equipo en modo low.
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private UInt32 SNMPPowerLowGet(String targetIp)
        {
            String logMethod = "POWER LOW GET";
            UInt32 power = 0;
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
                    power = SnmpClient.GetGauge32(
                    targetIp,
                    Community,
                    u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerLow,
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
        /// Obtiene la potencia de transmisión del equipo en modo normal.
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private UInt32 SNMPPowerNormalGet(String targetIp)
        {
            String logMethod = "POWER LOW GET";
            UInt32 power = 0;
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
                    power = SnmpClient.GetGauge32(
                    targetIp,
                    Community,
                    u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerNormal,
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
        /// Obtiene el nivel de potencia LOW(1)/NORMAL(2).
        /// </summary>
        /// <param name="targetIp"></param>
        /// <returns></returns>
        private UInt32 SNMPPowerLevelGet(String targetIp)
        {
            String logMethod = "POWER LEVEL GET";
            UInt32 level = 0;
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
                    level = SnmpClient.GetGauge32(
                    targetIp,
                    Community,
                    u5ki.RemoteControlService.OIDs.RCRohde4200.TxPowerLevel,
                    SNMPCallTimeout,
                    Port,
                    SNMPVersion);
                    return level;
                }

                catch (Exception ex)
                {
                    if (ex is Lextm.SharpSnmpLib.Messaging.TimeoutException)
                    {
                        if (i_rtimeout >= NUMMAXTimeout)
                        {
                            ExceptionTreatment(ex, logMethod, targetIp);
                            return level;
                        }
                        i_rtimeout++;
                    }
                    else
                    {
                        ExceptionTreatment(ex, logMethod, targetIp);
                        return level;
                    }
                }
            }
            return level;
        }
        // JOI: 20171031 ERROR #3231  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="watt"></param>
        private Int32 ConvertdBmToWatt(Int32 pot)
        {
            Double dBm = pot;
            Double dWatt;
            UInt32 watt;
            dBm = dBm / 100;
            dWatt = (Math.Pow(10.0, dBm)) / 1000.0;
            watt = (UInt32)dWatt;
            if ( watt < PowerLevelMin)
            {
                watt = PowerLevelMin;
                LogInfo<RCRohde4200>(" Equipo configurado con potencia mínima por defecto ",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("Asignación potencia mínima en equipo N Rohde "));
            }
            else if ( watt > PowerLevelMax)
            {
                watt = PowerLevelMax;
                LogInfo<RCRohde4200>(" Equipo configurado con potencia máxima por defecto ",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("Asignación potencia máxima en equipo N Rohde "));
            }
            return (Int32)watt;
        }
        // JOI: 20171031 ERROR #3231 
        #endregion

    }
}

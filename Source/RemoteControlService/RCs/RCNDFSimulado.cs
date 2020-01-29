#define DEBUG_VERBOSE_NO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Net;
using System.Text.RegularExpressions;

using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;
using U5ki.Infrastructure.Resources;
using Utilities;

using Lextm.SharpSnmpLib;
//using Lextm.SharpSnmpLib.Messaging;
//using Lextm.SharpSnmpLib.Objects;
//using Lextm.SharpSnmpLib.Pipeline;
using NLog;

namespace u5ki.RemoteControlService
{
    class RCNDFSimulado : BaseCode, IRemoteControl
    {
        /// <summary>
        /// 
        /// </summary>
        static Object locker = new Object();

        /* .1.3.6.1.4.1.7916.8.4.1.3.1. + .col + .row */
        #region IRemoteControl
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        GearOperationStatus status = GearOperationStatus.Fail;
		
        // JOI. CONTROL_SET_SIP
        private int delaySetFrequencyMs = Convert.ToInt32(u5ki.RemoteControlService.Properties.Settings.Default.DelaySetFrequencyMs);
        // JOI. CONTROL_SET_SIP FIN.
		
        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="node"></param>
        public void CheckNode(Action<GearOperationStatus> response, BaseNode node)
        {
            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.CurrentThread.Name = "CheckNode_" + (Id.Contains("_N")==true ? Id.ToLower() : Id); 
                try
                {
#if LOCKER
                    lock (locker)
#endif
                    // JOI 
                    if (node.IsMaster == true && node.Frecuency != "")
                    {
                        GearOperationStatus res = SNMPMasterControlConfig(node.IP, node.IsMaster, node.Frecuency);
                        if (res == GearOperationStatus.FailMasterConfig)
                        {
                            Invoke(response, "CheckNode", GearOperationStatus.FailMasterConfig);
                            return;
                        }
                    }
                    // JOI FIN
                    {
                        ip_equipo = node.IP;
                        Invoke(response, "CheckNode", Status(node.IP));
                    }
                    _lastExceptions["CheckNode"] = null;
                }
                catch (SnmpException x)
                {
                    ExceptionTreatment("CheckNode", x);
                    Invoke(response, "CheckNode", GearOperationStatus.Fail);
                }
                catch (Exception x)
                {
                    ExceptionTreatment("CheckNode", x);
                    Invoke(response, "CheckNode", GearOperationStatus.Fail);
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="node"></param>
        public void FrecuencyGet(Action<String> response, BaseNode node)
        {
            LogInfo<RCNDFSimulado>(String.Format("RCNDFSimulado.FrecuencyGet({0})", Id));
            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.CurrentThread.Name = "FrecuencyGet_" + (Id.Contains("_N") == true ? Id.ToLower() : Id);
                try
                {
#if LOCKER
                    lock (locker)
#endif
                    {
                        ip_equipo = node.IP;
                        if (String.IsNullOrEmpty(OidEquipo))
                        {
                            response.Invoke("Error Generico de Equipo...");
                            _lastExceptions["FrecuencyGet"] = null;
                            return;
                        }
                        String snmp_frecuency = SnmpClient.GetString(ip_equipo, Community,
                            u5ki.RemoteControlService.OIDs.NDFSimulado.Frecuency + OidEquipo,
                            SNMPCallTimeout, Port, SNMPVersion);
                        response.Invoke(snmp_frecuency);
                        _lastExceptions["FrecuencyGet"] = null;
                    }
                }
                catch (SnmpException x)
                {
                    ExceptionTreatment("FrecuencyGet", x);
                    response.Invoke("SnmpException en FrecuencyGet: " + x.Message);
                }
                catch (Exception x)
                {
                    ExceptionTreatment("FrecuencyGet", x);
                    response.Invoke("Exception en FrecuencyGet: " + x.Message);
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="response"></param>
        /// <param name="node"></param>
        /// <param name="isEmitter"></param>
        public void ConfigureNode(RCConfigurationAction action, Action<GearOperationStatus> response, BaseNode node, Boolean isEmitter, Boolean isMaster)
        {
            if (action == RCConfigurationAction.Unassing)
            {
                Invoke(response, "ConfigureNode.Unassing", GearOperationStatus.OK, false); 
                return;
            }
            LogInfo<RCNDFSimulado>(String.Format("RCNDFSimulado.ConfigureNode.Assign({0})", Id));
            //Task.Factory.StartNew(() =>
            //{
            //    System.Threading.Thread.CurrentThread.Name = "CfgNode_" + (Id.Contains("_N") == true ? Id.ToLower() : Id);
                try
                {
					// JOI. CONTROL_SET_SIP
					System.Threading.Thread.Sleep(delaySetFrequencyMs);
#if LOCKER
                    lock (locker)
#endif
                    {
                        ip_equipo = node.IP;
                        if (String.IsNullOrEmpty(OidEquipo))
                        {
                            Invoke(response, "ConfigureNode.Assing", GearOperationStatus.Fail, false);
                            _lastExceptions["ConfigureNode"] = null;
                            return;
                        }
                        // Fecuency
                        SnmpClient.SetString(ip_equipo, Community,
                            u5ki.RemoteControlService.OIDs.NDFSimulado.Frecuency + OidEquipo,
                            node.Frecuency, SNMPCallTimeout, Port, SNMPVersion);

                        // Channel Spacing.
                        SnmpClient.SetInt(ip_equipo, Community,
                            u5ki.RemoteControlService.OIDs.NDFSimulado.ChannelSpacing + OidEquipo,
                            (int)node.Channeling, SNMPCallTimeout, Port, SNMPVersion);

                        // Modulation
                        SnmpClient.SetInt(ip_equipo, Community,
                            u5ki.RemoteControlService.OIDs.NDFSimulado.Modulation + OidEquipo,
                            (int)node.Modulation, SNMPCallTimeout, Port, SNMPVersion);

                        if (isEmitter)
                        {
                            // Carrier Offset
                            SnmpClient.SetInt(ip_equipo, Community,
                                u5ki.RemoteControlService.OIDs.NDFSimulado.CarrierOffStatus + OidEquipo,
                                (int)node.Offset, SNMPCallTimeout, Port, SNMPVersion);

                            // Power
                            SnmpClient.SetInt(ip_equipo, Community,
                                u5ki.RemoteControlService.OIDs.NDFSimulado.TxPower + OidEquipo,
                                (int)node.PowerLevel, SNMPCallTimeout, Port, SNMPVersion);
                        }

                        Invoke(response, "ConfigureNode.Assing", GearOperationStatus.OK, false);
                        _lastExceptions["ConfigureNode"] = null;
                    }
                }
                catch (Lextm.SharpSnmpLib.Messaging.TimeoutException x)
                {
                    ExceptionTreatment("ConfigureNode", x);
                    Invoke(response, "ConfigureNode.Assing", GearOperationStatus.Timeout, false);
                }
                catch (Exception x)
                {
                    ExceptionTreatment("ConfigureNode", x);
                    Invoke(response, "ConfigureNode.Assing", GearOperationStatus.Fail, false);
                }
            //});
        }

        #endregion

        #region protected

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        GearOperationStatus Status(string ip)
        {
            if (String.IsNullOrEmpty(OidEquipo))
            {
                return GearOperationStatus.Fail;
            }
            int snmp_status = SnmpClient.GetInt(ip, Community,
                u5ki.RemoteControlService.OIDs.NDFSimulado.DeviceStatus + OidEquipo,
                SNMPCallTimeout, Port, SNMPVersion);
            switch (snmp_status)
            {
                case 0:
                    // status = GearOperationStatus.Local;
                    if (status != GearOperationStatus.Local)
                    {
                        status = GearOperationStatus.Local;
                        LogWarn<RCNDFSimulado>("[SNMP][" + "Device Status GET" + "] [" + OidEquipo + "] LOCAL MODE ON: " + OidEquipo,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,
                            Id, "Modo LOCAL");
                    }
                    return GearOperationStatus.Fail;

                case 1:
                    status = GearOperationStatus.OK;
                    break;
                case 2:
                    status = GearOperationStatus.Fail;
                    break;
                case 3:
                    System.Threading.Thread.Sleep(1000);
                    status = GearOperationStatus.Timeout;
                    break;
            }
            return status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strProc"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        IDictionary<String, Type> _lastExceptions = new Dictionary<String, Type>();
        private GearOperationStatus ExceptionTreatment(String strProc, Exception ex)
        {
            if (_lastExceptions.ContainsKey(strProc) &&
                _lastExceptions[strProc] == ex.GetType() )
                return GearOperationStatus.Fail;

            LogWarn<RCNDFSimulado>(String.Format("Excepcion en \"{0}:\" \"{1}\"", strProc, ex.Message));
            _lastExceptions[strProc] = ex.GetType();

            return GearOperationStatus.Fail;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strProc"></param>
        /// <param name="status"></param>
        GearOperationStatus _last_status = GearOperationStatus.None;
        private void Invoke(Action<GearOperationStatus> respuesta, String strProc, GearOperationStatus status, bool control=true)
        {
#if DEBUG_VERBOSE
            if (control==false || _last_status != status)
                LogManager.GetCurrentClassLogger().Warn("TH <{0}>:{2} {1} Invoking...",
                    System.Threading.Thread.CurrentThread.Name, 
                    status, strProc);
#endif

            respuesta.Invoke(status);

#if DEBUG_VERBOSE
            if (control == false || _last_status != status)
                LogManager.GetCurrentClassLogger().Error("TH <{0}>:{2} {1} Invoked...",
                    System.Threading.Thread.CurrentThread.Name,
                    status, strProc);
#endif

            _last_status = status;
        }

        #endregion

        #region parametros

        Int32 Port = 161;
        String Community = "public";
        VersionCode SNMPVersion = VersionCode.V2;
        Int32 SNMPCallTimeout = 1000;                // Miliseconds = 0,25 seconds

        string ip_equipo = null;
        //string old_oid_equipo = null;
        //string Old_OidEquipo 
        //{ 
        //    get
        //    {
        //        if (oid_equipo == null)
        //        {
        //            if (ip_equipo != null)
        //            {
        //                lock (locker)
        //                {
        //                    try
        //                    {
        //                        /** Solicito una OID */
        //                        SnmpClient.SetString(ip_equipo, Community, ".1.3.6.1.4.1.7916.8.4.1.1.0", Id, SNMPCallTimeout, Port, SNMPVersion);
        //                        System.Threading.Thread.Sleep(200);

        //                        oid_equipo = SnmpClient.GetString(ip_equipo, Community, ".1.3.6.1.4.1.7916.8.4.1.2.0", SNMPCallTimeout, Port, SNMPVersion);
        //                        LogInfo<RCNDFSimulado>(String.Format("Oid {0}: {1}", Id, oid_equipo));
        //                    }
        //                    catch (Exception)
        //                    {
        //                        oid_equipo = null;
        //                    }
        //                }
        //            }
        //        }
        //        return oid_equipo;            
        //    } 
        //    set
        //    {
        //        oid_equipo = value;
        //    }
        //}


        static Dictionary<string, string> EquipmentsOid = new Dictionary<string, string>();
        static bool isEquipmentOid(string value)
        {
            return (new Regex(@"^\.[0-9]*$")).Match(value).Success;
        }
        string OidEquipo
        {
            get
            {
                if (EquipmentsOid.Keys.Contains(Id) == false)
                {
                    lock (locker)
                    {
                        try
                        {
                            /** Solicito una OID */
                            SnmpClient.SetString(ip_equipo, Community, ".1.3.6.1.4.1.7916.8.4.1.1.0", Id, SNMPCallTimeout, Port, SNMPVersion);
                            System.Threading.Thread.Sleep(200);

                            string new_oid = SnmpClient.GetString(ip_equipo, Community, ".1.3.6.1.4.1.7916.8.4.1.2.0", SNMPCallTimeout, Port, SNMPVersion);
                            if (isEquipmentOid(new_oid)==false || EquipmentsOid.Values.Contains(new_oid))
                                throw new Exception("");

                            EquipmentsOid[Id] = new_oid;
                            LogInfo<RCNDFSimulado>(String.Format("Oid {0}: {1}", Id, new_oid));
                        }
                        catch (Exception x)
                        {
                            if (EquipmentsOid.Keys.Contains(Id)) 
                                EquipmentsOid.Remove(Id);
                            throw x;
                        }
                    }
                }
                return EquipmentsOid[Id];
            }
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

            try
            {
                // Control de frecuencia.
                string[] frecuencia = new string[] { sFrecuencia };
                frecuencia[0] = frecuencia[0].Replace(".", "");
                frecuencia[0] = frecuencia[0].Replace(",", "");

                String frecuenciaEquipo = SnmpClient.GetString(ip_equipo, Community,
                    u5ki.RemoteControlService.OIDs.NDFSimulado.Frecuency + OidEquipo,
                    SNMPCallTimeout, Port, SNMPVersion);

                frecuenciaEquipo = frecuenciaEquipo.Replace(".", "");
                frecuenciaEquipo = frecuenciaEquipo.Replace(",", "");

                if (frecuenciaEquipo != frecuencia[0])
                {
                    return GearOperationStatus.FailMasterConfig;
                }

                // Control de frecuencia. Fin
                return GearOperationStatus.OK;
            }
            catch (Exception ex)
            {
                ExceptionTreatment("SNMPMasterControlConfig", ex);
                return GearOperationStatus.Fail;
            }
        }


        #endregion
    }
}

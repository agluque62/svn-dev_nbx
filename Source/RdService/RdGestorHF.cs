using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Net;


using NLog;

using Utilities;
using U5ki.Infrastructure;

using Translate;

namespace U5ki.RdService
{
    /// <summary>
    /// 20171004. AGL. Unificación de LOG e Incidencias.
    /// </summary>
    public class RdGestorHF
    {
        public static class HFSnmpHelper
        {
            static public int GetInt(string ip, string community, string oid, int timeout, int port, Lextm.SharpSnmpLib.VersionCode version)
            {
                int localTimeout = timeout > 1000 ? 1000 : timeout;
                int localReint = (int)Math.Ceiling(((decimal)timeout / localTimeout));
                do
                {
                    try
                    {
                        int retorno = SnmpClient.GetInt(ip, community, oid, localTimeout, port, version);
                        return retorno;
                    }
                    catch (Exception x)
                    {
                        if (x.GetType().FullName.Contains("Lextm.SharpSnmpLib.Messaging.TimeoutException")==false)
                            throw x;
                    }
                } while (--localReint > 0);

                throw new Exception(String.Format("HFSnmpHelper (Get): {0} [{1}]. No responde. Reintentos {2}.", ip, oid, (int)Math.Ceiling(((decimal)timeout / localTimeout))));
            }

            static public void SetInt(string ip, string community, string oid, int valor, int timeout, int port, Lextm.SharpSnmpLib.VersionCode version)
            {
                int localTimeout = timeout > 1000 ? 1000 : timeout;
                int localReint = (int)Math.Ceiling(((decimal)timeout / localTimeout));
                do
                {
                    try
                    {
                        SnmpClient.SetInt(ip, community, oid, valor, localTimeout, port, version);
                        return;
                    }
                    catch (Exception x)
                    {
                        if (x.GetType().FullName.Contains("Lextm.SharpSnmpLib.Messaging.TimeoutException") == false)
                            throw x;
                    }
                } while (--localReint > 0);

                throw new Exception(String.Format("HFSnmpHelper (Set): {0} [{1}]. No responde. Reintentos {2}.", ip, oid, (int)Math.Ceiling(((decimal)timeout / localTimeout))));
            }

            //static public string GetString(string ip, string community, string oid, int timeout)
            //{
            //    return "";
            //}

            //static public void SetString(string ip, string community, string oid, string valor, int timeout)
            //{
            //}

            //static public void TrapTo(string ipTo, string community, string oid, string val)
            //{
            //}
        }

        public static class HFHelper
        {
            class HFLogger : BaseCode
            {
                public void Trace(String msg, U5kiIncidencias.U5kiIncidencia type = U5kiIncidencias.U5kiIncidencia.IGNORE) { LogTrace<HFLogger>(msg); }
                public void Debug(String msg, U5kiIncidencias.U5kiIncidencia type = U5kiIncidencias.U5kiIncidencia.IGNORE) { LogDebug<HFLogger>(msg); }
                public void Info(String msg, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages) { LogInfo<HFLogger>(msg, type, issueMessages); }
                public void Error(String msg, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages) { LogError<HFLogger>(msg, type, issueMessages); }
            }
            static HFLogger hflog = new HFLogger();
            //static Logger nlog = LogManager.GetCurrentClassLogger();
            public static string Log(LogLevel level, string message, string idEquipo, string Frecuencia = "---", string usuario = "---", U5kiIncidencias.U5kiIncidencia inci = U5kiIncidencias.U5kiIncidencia.IGNORE)
            {
                StackFrame frame = new StackFrame(1);
                String calling = frame.GetMethod().Name;
                String msg = String.Format("({0}) => [{1,4}/{2,4}/{3,4}]: {4}", calling, idEquipo ?? "---" , Frecuencia ?? "---", usuario ?? "---", message);

                if (level == LogLevel.Debug)
                    hflog.Debug(msg);
                else if (level == LogLevel.Info)
                    hflog.Info(msg, inci, "HFM", usuario, idEquipo, Frecuencia, msg);
                else
                    hflog.Error(msg, inci, "HFM", usuario, idEquipo, Frecuencia, msg);
                
#if DEBUG
                TestHist();
#endif
                return msg;
            }
            public static void Trace([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerMemberName] string caller = null)
            {
                String msg = String.Format("TxHFGestor TRACE: {0} line {1}", caller, lineNumber);
                // nlog.Trace(msg);
                hflog.Trace(msg);
            }
            public static void RespondToFrHfTxChange(string ip, string frec, int code)
            {
                Log(LogLevel.Warn, String.Format("HF Mensaje to {0}. Frec: {1}, Codigo: {2}", ip, frec, code), null);
                RdRegistry.RespondToFrHfTxChange(ip, frec, code);
            }
            public static void RespondToPrepareSelcal(string to, string fr, bool res, string men)
            {
                Log(LogLevel.Warn, String.Format("HF Secal Mensaje to {0}. Frec: {1}, res: {2}, mensaje: {3}", to, fr, res ? "OK" : "FALLO", men), null);
                RdRegistry.RespondToPrepareSelcal(to, fr, res, men);
            }
            /** */
            public static string HostOfUser(Cd40Cfg cfg, string user)
            {
                AsignacionUsuariosTV asg = cfg.ConfiguracionGeneral.PlanAsignacionUsuarios.Where(a => a.IdUsuario == user).FirstOrDefault();
                return asg == null ? "" : asg.IdHost;
            }

#if DEBUG
            static int TestingHis = 2;
            static public void TestHist()
            {
                var lista = new List<U5kiIncidencias.U5kiIncidencia>()
                {
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_CONECTADO,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ERROR,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_DESCONECTADO,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ASIGNADO,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_ASIGNACION,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_INTENTO_ASIGNACION_MULTIPLE,
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_PREPARACIONSELCAL,
                };

                if (TestingHis-- > 0)
                {
                    lista.ForEach(e =>{
                        hflog.Info("Testing #01", e, "HFM", "##_user_##", "##_equipment_##", "##_frequency_##", "Testing #01");
                    });                    
                }
            }
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        // static Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        public enum EquipoHFStd
        {
            stdNoinfo, 
            stdError = 1, 
            stdDisponible, 
            stdAsignado = 3, 
            stdNoResource, 
            stdOperationInProgress = 0xFC,
            stdFrequencyAlreadyAssigned = 0xFD, 
            stdNoGateway = 0xFE, 
            stdTxAlreadyAssigned = 0xFF
        };

        /// <summary>
        /// Contiene la información de configuracion y estado de los equipos HF.
        /// La interfaz IComparable, permite la Reordenacion de Listas, segun varios Criterios.
        /// Estos Criterios permiten, variar los algoritmos de asignacion de varias maneras...
        /// </summary>
        public class EquipoHF : IComparable<EquipoHF>
        {
            /** Valores sacados de OSCH
             
                enum EComandoTx
                {
                    CMD_FRECUENCIA  =   4, 
                    CMD_MODULACION  =   8, 
                    CMD_POTENCIA    =   2, 
                    CMD_ACTUALIZAR  =   1001
                };
              
                const char *const OID_FREC			= ".25";        Valor 'leido' del Equipo
                const char *const OID_FREC_CMD	    =  ".5";        Valor puesto por el sistema.

                const char *const OID_MOD			= ".29";
                const char *const OID_MOD_CMD		= ".9";

                const char *const OID_POT			= ".20";
                const char *const OID_POT_CMD		= ".3";

                const char *const OID_JBUS			= ".18";
              
                const char *const OID_CMD			= ".0";         Se escribe un 'EComandoTx' orden para OID_XXX_CMD --> OID_XXX

                const char *const OID_ESTADO		= ".36";
                const char *const OID_MUTE			= ".38";
             
             * 
             */

            /// Código de órdenes a RCS-2002.
            enum eCmdTx { CMD_FRECUENCIA = 4, CMD_MODULACION = 8, CMD_POTENCIA = 2, CMD_ACTUALIZAR = 1001 }

            enum eModoModulacion { mH3E = 2, mBLS = 0 }

            /// A partir de la Direccion del Equipo (en _oid), extension para cada tipo de comando.
            /// 
            const string OID_FREC_CMD = ".5";               // Escritura.
            const string OID_FREC = ".25";

            const string OID_ESTADO = ".36";
            const string OID_CMD = ".0";

            const string OID_MOD_CMD = ".9";                  // Escritura.
            const string OID_MOD = ".29";

            /// <summary>
            /// Datos de Estado.
            /// </summary>
            public EquipoHFStd Estado { get; set; }
            public int StdRem { get; set; }
            public int CntAsignaciones { get; set; }

            /// <summary>
            /// Datos de Asignacion
            /// </summary>
            public string IDFrecuencia { get; set; }
            public string Usuario { get; set; }
            public string IpFrom { get; set; }                  // Para las Respuestas asíncronas....
            public int Frecuencia { get; set; }                 // Frecuencia de Trabajo en kHz..

            protected int ModAnt { get; set; }         // Tipo de Modulacion antes de SELCAL
            RdFrecuency _frAsignada = null;
            public RdFrecuency FrAsignada { get { return _frAsignada; } }
            RdFrecuency _frToCheck = null;
            public RdFrecuency FrToCheck { get { return _frToCheck; } }

            /// <summary>
            /// Datos de Configuracion.
            /// </summary>
            public string IdEquipo { get; set; }
            public string SipUri { get; set; }
            public string IpRcs { get; set; }
            public string Oid { get; set; }
            List<HfRangoFrecuencias> _Frecs = null;
            /// <summary>
            /// 
            /// </summary>
            public string TextoTecla
            {
                get
                {
                    return _frAsignada == null ? IDFrecuencia : _frAsignada.Frecuency;
                }
            }

            /// <summary>
            /// Configuracion Local....
            /// </summary>
            string _snmpRComm = Properties.Settings.Default.HFSnmpReadCommunity;
            string _snmpWComm = Properties.Settings.Default.HFSnmpWriteCommunity;
            int _snmpTimeout = Properties.Settings.Default.HFSnmpTimeout;
            int _jbusTimeout = Properties.Settings.Default.HFJBusTimeout / 100;         // Lo paso a ticks de 100 mseg

            // 20171005. Semaforo de acceso al equipo....
            Semaphore accessSemaphore = new Semaphore(1, 1);

            public bool acquire()
            {
                return accessSemaphore.WaitOne(10000);    // TODO. Debe ser configurable.               
            }
            public void release()
            {
                try
                {
                    accessSemaphore.Release();
                }
                catch (Exception x)
                {
                    HFHelper.Log(LogLevel.Error, x.Message, IdEquipo);
                }
            }
            public bool IsBeingSnmpSupervised { get; set; }
            public bool IsBeingAssigned { get; set; }
            public bool IsBeingDesassigned { get; set; }
            public bool IsBeingSipSupervised { get; set; }
            public int SnmpPort { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public EquipoHF()
            {
                IdEquipo = "";
                SipUri = "";
                IpRcs = "127.0.0.1";
                Oid = "";

                Estado = EquipoHFStd.stdNoinfo;
                IDFrecuencia = "";
                Usuario = "";
                IpFrom = "";
                Frecuencia = 30000;

                CntAsignaciones = 0;

                ModAnt = (int)eModoModulacion.mBLS;
                IsBeingSnmpSupervised = false;
                IsBeingAssigned = IsBeingDesassigned = false;
                IsBeingSipSupervised = false;
                SnmpPort = 161;                         // TODO. Esto debería ser configurable.

                ConsecutiveOptionsRequestCounter = 0;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public EquipoHF(string name)
                : this()
            {
                IdEquipo = name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="otro"></param>
            public EquipoHF(EquipoHF otro)
            {
                CopyFrom(otro);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="otro"></param>
            public void CopyFrom(EquipoHF otro)
            {
                IdEquipo = otro.IdEquipo;
                SipUri = otro.SipUri;
                IpRcs = otro.IpRcs;
                Oid = otro.Oid;
                Frecs = otro.Frecs.Select(r => new HfRangoFrecuencias() { fmax = r.fmax, fmin = r.fmin }).ToList();
                SnmpPort = otro.SnmpPort;

                Estado = otro.Estado;
                IDFrecuencia = otro.IDFrecuencia;
                Usuario = otro.Usuario;
                IpFrom = otro.IpFrom;
                Frecuencia = otro.Frecuencia;
                CntAsignaciones = otro.CntAsignaciones;
                ModAnt = otro.ModAnt;
                IsBeingSnmpSupervised = otro.IsBeingSnmpSupervised;
                IsBeingAssigned = otro.IsBeingAssigned;
                IsBeingDesassigned = otro.IsBeingDesassigned;
                IsBeingSipSupervised = otro.IsBeingSipSupervised;
                ConsecutiveOptionsRequestCounter = otro.ConsecutiveOptionsRequestCounter;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="otro"></param>
            /// <returns></returns>
            public bool Equals(EquipoHF otro)
            {
                bool retorno = (
                    IdEquipo == otro.IdEquipo &&
                    SipUri == otro.SipUri &&
                    IpRcs == otro.IpRcs &&
                    Oid == otro.Oid &&
                    SnmpPort == otro.SnmpPort);

                // Comparo los rangos...
                Frecs.ForEach(r1 => retorno = retorno && (otro.Frecs.Where(r2 => r1.fmax == r2.fmax && r1.fmin == r2.fmin).FirstOrDefault() != null));
                return retorno;
            }

            /// <summary>
            /// 
            /// </summary>
            public List<HfRangoFrecuencias> Frecs
            {
                get { return _Frecs; }
                set { _Frecs = value; }
            }

            /// <summary>
            /// Esta Funcion, perteneciente a la Interfaz IComparable, permite establecer los criterios
            /// de Ordenacion de listas
            /// </summary>
            /// <param name="b">Objeto al que se compara</param>
            /// <returns> Negativo: this menor que b, Positivo: this mayor que b, 0: Objetos Iguales. </returns>
            public int CompareTo(EquipoHF b)
            {
                return CntAsignaciones == b.CntAsignaciones ? 0 :
                       CntAsignaciones < b.CntAsignaciones ? -1 : 1;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="frec"></param>
            /// <returns></returns>
            public void Sintoniza(int frec)
            {
                try
                {
                    if (Properties.Settings.Default.HFSnmpSimula == false)
                        HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_FREC_CMD, frec, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);
                    else
                        HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_FREC, frec, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);

                    HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_CMD, (int)(eCmdTx.CMD_FRECUENCIA), _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);

                    /** Espera a que el Agente lo Ejecute... */
                    int _timeout = _jbusTimeout;
                    while (_timeout-- > 0)
                    {
                        Thread.Sleep(100);
                        if (HFSnmpHelper.GetInt(IpRcs, _snmpRComm, Oid + OID_FREC, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2) == frec)
                        {
                            Frecuencia = frec;
                            return;
                        }
                    }

                    throw new Exception(string.Format("Sintonizando Equipo {0} en {1}, TIMEOUT", IdEquipo, frec));
                }
                catch (Exception x)
                {
                    // _log.Error("GestorHF.Sintoniza: ", x);
                    // 20171003. AGL. ¿Convendría aquí 'limpiar' el equipo o al menos marcar un estado 'erroneo', enviar un historico...
                    HFHelper.Log(LogLevel.Error, x.Message, IdEquipo, frec.ToString(), ToString());
                    throw x;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public void GetEstado()
            {
                try
                {
                    StdRem = HFSnmpHelper.GetInt(IpRcs, _snmpRComm, Oid + OID_ESTADO, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);
                    AutomataEstado();
                    // 201710. AGL. Esto parece un control para 'sesiones zombies'. Investigar...
                    if (Estado == EquipoHFStd.stdAsignado && _frAsignada != null && !_frAsignada.FindHost(this.Usuario))
                    {
                        Desasigna(ref _frAsignada);
                    }
                }
                catch (Exception x)
                {
                    // Log de la Incidencia.
                    HFHelper.Log(LogLevel.Error, x.Message, IdEquipo, IDFrecuencia, Usuario);

                    /** AGL2014. Generar la Incidencia solo si hay transicion... */
                    if (Estado != EquipoHFStd.stdNoinfo)
                    {
                        /** Incidencia Perdida de Conexion con Equipo */
                        HFHelper.Log(LogLevel.Error, x.Message, IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_DESCONECTADO);
                    }

                    /** Si Asignado Desasignar y comunicar a Operador */
                    if (Estado == EquipoHFStd.stdAsignado)
                    {
                        HFHelper.Log(LogLevel.Error, "Equipo desasignado por Time-Out en operación SNMP", IdEquipo, IDFrecuencia, Usuario);
                        HFHelper.RespondToFrHfTxChange(IpFrom, TextoTecla, (int)EquipoHFStd.stdError);
                        Desasigna(ref _frAsignada);
                    }

                    Estado = EquipoHFStd.stdNoinfo;
                    throw x;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            private void AutomataEstado()
            {
                switch (Estado)
                {
                    case EquipoHFStd.stdNoinfo:
                        if (StdRem != 0 && StdRem != 3)
                        {
                            Estado = EquipoHFStd.stdDisponible;
                            /** Equipo Conectado */
                            HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo conectado"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_CONECTADO);
                        }
                        else
                        {
                            Estado = EquipoHFStd.stdError;
                            /** Equipo Error */
                            HFHelper.Log(LogLevel.Error, CTranslate.translateResource("Equipo en error"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ERROR);
                        }
                        break;

                    case EquipoHFStd.stdDisponible:
                        if (StdRem == 0 || StdRem == 3)
                        {
                            Estado = EquipoHFStd.stdError;
                            /** Incidencia Equipo Error */
                            HFHelper.Log(LogLevel.Error, CTranslate.translateResource("Equipo en error"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ERROR);
                        }
                        break;

                    case EquipoHFStd.stdAsignado:
                        if (StdRem == 0 || StdRem == 3)
                        {
                            /** Incidencia Equipo Error */
                            HFHelper.Log(LogLevel.Error, CTranslate.translateResource("Equipo en error"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ERROR);

                            /** Desasignar y Notificar Desasignacion USUARIO */
                            //RdRegistry.RespondToFrTxChange(IpFrom, IDFrecuencia, false);
                            HFHelper.RespondToFrHfTxChange(IpFrom, TextoTecla, (int)EquipoHFStd.stdError);
                            Desasigna(ref _frAsignada);

                            Estado = EquipoHFStd.stdError;
                        }
                        break;

                    case EquipoHFStd.stdError:
                        if (StdRem != 0 && StdRem != 3)
                        {
                            Estado = EquipoHFStd.stdDisponible;
                            /** Equipo Conectado */
                            HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo conectado"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_CONECTADO);
                        }
                        break;

                    case EquipoHFStd.stdNoResource:
                        break;

                    default:
                        /** Error Raro.... */
                        HFHelper.Log(LogLevel.Error,  CTranslate.translateResource("Código de estado de equipo no válido"), IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                        break;
                }
            }

            /// <summary>
            /// 20180116. AGL. Control de excepciones de la Desconexion SIP, para que al menos se borre la tabla SIP...
            /// </summary>
            private void DesconectaSip(ref RdFrecuency fr, string rsId)
            {
                try
                {
                    fr.RemoveSipCall(fr.RdRs[rsId]);
                    fr.RdRs[rsId].Dispose();
                }
                catch (Exception x)
                {
                    HFHelper.Log(LogLevel.Error, x.Message, IdEquipo, IDFrecuencia, Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                }
                finally
                {
                    fr.RdRs.Remove(rsId);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public bool CheckFrecuencia(int f)
            {
                foreach (HfRangoFrecuencias r in _Frecs)
                {
                    if (f >= r.fmin && f <= r.fmax)
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Chequea mediante INVITE que está disponible el agente SIP.
            /// </summary>
            public void Check()
            {
                bool toCheck = true;
                IRdResource rr;

                string rsId = SipUri.ToUpper() + RdRsType.Tx;

                if (_frToCheck == null)
                    _frToCheck = new RdFrecuency(IdEquipo);

                if (_frToCheck.RdRs.Count == 0 || !_frToCheck.RdRs.ContainsKey(rsId))
                {
                    rr = new RdResource(IdEquipo, SipUri, RdRsType.Tx, IdEquipo, toCheck);
                    _frToCheck.RdRs[rsId] = rr;
                }
                else
                {
                    rr = _frToCheck.RdRs[rsId];
                    rr.Connect();
                }
            }
            /// <summary>
            /// 20171130. Chequea mediante OPTIONS que está disponible el agente SIP.
            /// </summary>
            public int ConsecutiveOptionsRequestCounter { get; set; }
            public void SipOptionsCheck()
            {
                string callid = "";
                SipAgent.SendOptionsMsg(this.SipUri, out callid, true);
                if (++ConsecutiveOptionsRequestCounter >= 2)    // TODO poner configurable
                {
                    ConsecutiveOptionsRequestCounter = 0;
                    if (Estado != EquipoHFStd.stdNoinfo)
                    {
                        Estado = EquipoHFStd.stdNoResource;
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="frecuencia"></param>
            public void Asigna(string usuario, ref RdFrecuency fr, string from)
            {
                /** 20180109. Preseleccionar el equipo para gestionar las operaciones asignacion => desasignacion consecutivas */
                try
                {
                    /** Pre-seleccionar el equipo */
                    Usuario = usuario;
                    IDFrecuencia = fr.FrecuenciaSintonizada.ToString();                     // Frecuency;

                    /** Chequear que no está la frecuencia asignada a otro transmisor */
                    foreach (RdResource ra in fr.RdRs.Values)
                    {
                        if (ra.Type == RdRsType.Tx)
                        {
                            string msg = HFHelper.Log(LogLevel.Error, 
                                CTranslate.translateResource("¡Asignando frecuencia con TX asignado!"), 
                                IdEquipo, fr.FrecuenciaSintonizada.ToString(), usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                            throw new Exception(msg);
                        }
                    }

                    /** */
                    Sintoniza(fr.FrecuenciaSintonizada);

                    /** Asignar el equipo a la frecuencia */
                    string rsId = SipUri.ToUpper() + RdRsType.Tx;
                    // JOI FREC_DES
                    // RdResource rr = new RdResource(IdEquipo, SipUri, RdRsType.Tx, IdEquipo);
                    // RdResource rr = new RdResource(IdEquipo, SipUri, RdRsType.Tx, IdEquipo, " ");
                    // 20171107. AGL. los parametros de configuracion por Defecto.
                    RdResource rr = new RdResource(IdEquipo, SipUri, RdRsType.Tx, IdEquipo, " ", new RdFrecuency.NewRdFrequencyParams());
                    // JOI FREC_DES FIN
                    fr.RdRs[rsId] = rr;

                    /** Ocupo el equipo */
                    Estado = EquipoHFStd.stdAsignado;
                    _frAsignada = fr;                   // Debe ser una referencia....
                    IpFrom = from;

                    CntAsignaciones += 1;
                }
                catch (Exception x)
                {
                    /** Liberar la preseleccion... Por el error... */
                    Usuario = "";
                    IDFrecuencia = "";
                    throw x;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public void Desasigna()
            {
                Desasigna(ref _frAsignada);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="state"></param>
            public void DesasignaFrToCheck(CORESIP_CallStateInfo state)
            {
                if (_frToCheck != null)
                {
                    string rsId = SipUri.ToUpper() + RdRsType.Tx;
                    //Las frecuencias HF, son todas simples
                    IRdResource iRes = null;
                    if (_frToCheck.RdRs.TryGetValue(rsId, out iRes))
                    {
                        RdResource res = (RdResource)iRes;
                        res.HandleChangeInCallState(res.SipCallId, state);
                        res.Dispose();
                        _frToCheck.RdRs.Remove(rsId);
                    }

                    _frToCheck = null;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fr"></param>
            public void Desasigna(ref RdFrecuency fr)
            {
                try
                {
                    /** Busco el recurso en la Frecuencia y lo elimino */
                    if (_frAsignada != null && fr != null)
                    {
                        string rsId = SipUri.ToUpper() + RdRsType.Tx;
                        if (fr.RdRs.ContainsKey(rsId))
                        {
                            // 20180116. AGL. Control de excepciones de la Desconexion SIP, para que al menos se borre la tabla SIP...
                            //fr.RemoveSipCall(fr.RdRs[rsId]);
                            //fr.RdRs[rsId].Dispose();
                            //fr.RdRs.Remove(rsId);
                            DesconectaSip(ref fr, rsId);
                        }
                        else
                        {
                            /** TODO. Algo Raro pasa... */
                            HFHelper.Log(LogLevel.Error, 
                                CTranslate.translateResource("¡Desasignando frecuencia sin TX asignado!"), 
                                IdEquipo, fr.FrecuenciaSintonizada.ToString(), null, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                        }
                    }
                    else
                    {
                        HFHelper.Log(LogLevel.Error, CTranslate.translateResource("¡Desasignando frecuencia con _frAsignada a NULL!"), IdEquipo, null, null, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                    }
                }
                catch (Exception x)
                {
                    throw x;
                }
                finally
                {
                    /** Desocupo el equipo */
                    Usuario = "";
                    Estado = Estado != EquipoHFStd.stdNoinfo ? EquipoHFStd.stdDisponible : Estado;
                    _frAsignada = null;
                    IpFrom = "";
                    IDFrecuencia = "";
                    CntAsignaciones -= 1;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public bool PrepareSelcal(bool OnOff)
            {
                try
                {
                    //int NewMod = (int)eModoModulacion.mH3E;
                    //if (OnOff)
                    //{   /** Leer el Modo de Modulacion y Guardarlo. */
                    //    ModAnt = HFSnmpHelper.GetInt(IpRcs, _snmpRComm, Oid + OID_MOD, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);
                    //}
                    //else
                    //{
                    //    /** Recupero el modo de modulacion */
                    //    NewMod = ModAnt;
                    //}
                    int NewMod = OnOff ? (int)eModoModulacion.mH3E : (int)eModoModulacion.mBLS;

                    /** Escribo el nuevo Modo de Modulacion */
                    if (Properties.Settings.Default.HFSnmpSimula == false)
                        HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_MOD_CMD, NewMod, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);
                    else
                        HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_MOD, NewMod, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);

                    /** Orden de Actualizacion*/
                    HFSnmpHelper.SetInt(IpRcs, _snmpWComm, Oid + OID_CMD, (int)(eCmdTx.CMD_MODULACION), _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2);

                    /** Espera a que el Agente lo Ejecute... */
                    int _timeout = _jbusTimeout;
                    while (_timeout-- > 0)
                    {
                        Thread.Sleep(100);
                        if (HFSnmpHelper.GetInt(IpRcs, _snmpRComm, Oid + OID_MOD, _snmpTimeout, SnmpPort, Lextm.SharpSnmpLib.VersionCode.V2) == NewMod)
                        {
                            return true;
                        }
                    }

                    throw new Exception(string.Format("Cambiando Modulacion en Equipo {0} a Mod={1}, TIMEOUT", IdEquipo, NewMod));

                }
                catch (Exception x)
                {
                    HFHelper.Log(LogLevel.Error, x.Message, IdEquipo, IDFrecuencia, Usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                    // 20171003. AGL. ¿Convendría aquí 'limpiar' el equipo o al menos marcar un estado 'erroneo'.
                    throw x;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private int HFSipSupervisionMode { get; set; }
        /// <summary>
        /// Tabla de gestion de equipos;
        /// </summary>
        List<EquipoHF> _equipos;
        public List<EquipoHF> Equipos
        {
            get { return _equipos; }
        }
        /// <summary>
        /// 
        /// </summary>
        public RdGestorHF()
        {
            _equipos = new List<EquipoHF>();
            LastGlobalStatus = HFStatusCodes.DISC;
            HFSipSupervisionMode = Properties.Settings.Default.HFSipSupervisionMode;
        }

        /// <summary>
        /// Mas tarde veremos con que parámetros. Puede que con un puntero a una configuracion....
        /// </summary>
        /// <returns></returns>
        public bool Cargar(Cd40Cfg cfg)
        {
            lock (_equipos)
            {
#if __VERSION_0__
                Limpiar();

                /// Cargo los datos...
                foreach (PoolHfElement tx in cfg.PoolHf)
                {
                    EquipoHF hf = new EquipoHF();

                    hf.IdEquipo = tx.Id;
                    hf.SipUri = tx.SipUri;
                    hf.IpRcs = tx.IpGestor;
                    hf.Oid = tx.Oid;
                    hf.Frecs = tx.Frecs;

                    hf.Usuario = "";
                    hf.Estado = EquipoHFStd.stdNoinfo;

                    _equipos.Add(hf);
                }
#else
                try
                {
                    /** 20171115. Carga inteligente que no toque lo que no cambia */
                    List<EquipoHF> actuales = _equipos.Select(etx => new EquipoHF(etx)).ToList();
                    List<EquipoHF> nuevos = cfg.PoolHf.Select(tx => new EquipoHF()
                    {
                        IdEquipo = tx.Id,
                        SipUri = tx.SipUri,
                        IpRcs = tx.IpGestor,
                        Oid = tx.Oid,
                        Frecs = tx.Frecs
                    }).ToList();

                    nuevos.ForEach(nuevo =>
                    {
                        bool existe = actuales.Where(act => act.Equals(nuevo)).FirstOrDefault() != null;
                        if (!existe)
                        {
                            _equipos.Add(nuevo);
                        }
                        else
                        {
                            EquipoHF borrar = actuales.Find(eq => eq.Equals(nuevo));
                            actuales.Remove(borrar);
                        }
                    });

                    // Borro el resto.
                    actuales.ForEach(borrar =>
                    {
                        EquipoHF borrar1 = _equipos.Find(eq => eq.Equals(borrar));
                        if (borrar1.Usuario != "")
                        {
                            Desasigna(borrar1);
                        }
                        _equipos.Remove(borrar1);
                    });

                    /** 20180109. AGL. Supervisar los equipos y Desasignar los asignados que hayan cambiado de usuario */
                    SupervisaEstadoEquipos();
                    List<EquipoHF> asignados = _equipos.Where(eq => eq.Usuario != "").ToList();
                    asignados.ForEach(eq =>
                    {
                        string frecActual = eq.IDFrecuencia;
                        string hostActual = eq.Usuario;

                        ConfiguracionUsuario user = cfg.ConfiguracionUsuarios.Where(u => 
                            HFHelper.HostOfUser(cfg, u.User.IdIdentificador) == hostActual).FirstOrDefault();
                        if (user == null)
                        {
                            /** El Usuario ha desaparecido. Debo desasignar el equipo... */
                            Desasigna(eq);
                        }
                        else
                        {
                            CfgEnlaceExterno rdlink = user.RdLinks.Where(rdl => rdl.FrecuenciaSintonizada.ToString().ToUpper() == frecActual).FirstOrDefault();
                            if (rdlink == null)
                            {
                                /** La frecuencia ha desaparecido en este usuario. Debo desasignar el equipo... */
                                HFHelper.RespondToFrHfTxChange(eq.IpFrom, eq.TextoTecla, (int)EquipoHFStd.stdError); 
                                Desasigna(eq);
                            }
                        }
                    });
                }
                catch (Exception x)
                {
                    throw x;
                }
#endif
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Limpiar()
        {
            /// Limpiar la tabla...
            lock (_equipos)
            {
                foreach (EquipoHF eq in _equipos)
                {
                    /// 20171005. AGL. Control de acceso a los equipos para evitar solapamientos...
                    // No devuelvo el semaforo porque voy a borrar los equipos...
                    eq.acquire();
                    ////////////////////////////////////////////////////////////////////////////////

                    eq.Desasigna();
                }
                _equipos.Clear();

                LastGlobalStatus = HFStatusCodes.DISC;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usuario"></param>
        /// <param name="fr"></param>
        /// <returns></returns>
        public int AsignarTx(string usuario, ref RdFrecuency fr, string from)
        {
            HFHelper.Trace();
            /** Mirar que es una frecuencia HF */
            if (fr.TipoDeFrecuencia != "HF")
                return (int)EquipoHFStd.stdAsignado;
            HFHelper.Trace();
            HFHelper.Log(LogLevel.Debug, "Peticion de Asignacion de Equipo HF", from, fr.FrecuenciaSintonizada.ToString(), usuario);

#if __VERSION_0__
            lock (_equipos)
            {
                /** Miro si el usuario tiene ya una frecuencia */
                foreach (EquipoHF equipo in _equipos)
                {
                    // 20171003. Mirar si tiene sentido la segunda condicion, investigar porque se puso y no se quitó al poner el comentario en las listas siguientes...
                    // Parece que un usuario solo puede ocupar un equipo, y una 'frecuencia' solo se puede seleccionar en un equipo....
                    if (equipo.Usuario == usuario || equipo.IDFrecuencia == fr.FrecuenciaSintonizada.ToString())
                    {
                        //if (equipo.IDFrecuencia == fr.Frecuency)
                        //    return (int)EquipoHFStd.stdAsignado;                        
                        /** Log e Historico */
                        HFLog.Log(LogLevel.Error, "Intento de Asignacion Múltiple", null, fr.FrecuenciaSintonizada, usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_INTENTO_ASIGNACION_MULTIPLE);
                        return (equipo.Usuario == usuario) ? (int)EquipoHFStd.stdTxAlreadyAssigned : (int)EquipoHFStd.stdFrequencyAlreadyAssigned;
                    }
                }
            }

            /** Miro si hay equipos disponibles y pueden sintonizar la frecuencia solicitada */
            lock (_equipos)
            {
                /** Ordeno la Lista */
                if (Properties.Settings.Default.HFBalanceoAsignacion==true)
                    _equipos.Sort();

                // 20171004. AGL. Solo considero los equipos disponibles, para acelerar la asignacion...
                // foreach (EquipoHF equipo in _equipos)
                List<EquipoHF> disponibles = _equipos.Where(eq => eq.Estado == EquipoHFStd.stdDisponible).ToList();
                foreach (EquipoHF equipo in disponibles)
                /////////////////////////////////
                {
                    // Actualiza el estado del equipo
                    // 20171004. AGL. Proteger con try--catch
                    try
                    {
                        equipo.GetEstado();
                    }
                    catch (Exception)
                    {
                        HFLog.Trace();
                    }
                    /////////////////////////////////

                    if (equipo.Estado == EquipoHFStd.stdDisponible && equipo.CheckFrecuencia(fr.FrecuenciaSintonizada) == true)
                    {
                        try
                        {
                            HFLog.Trace();
                            /** Sintonizar el equipo */
                            equipo.Asigna(usuario, ref fr, from);
                            HFLog.Log(LogLevel.Info, "Equipo Asignado", equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ASIGNADO);
                            return (int)equipo.Estado;
                        }
                        catch (Exception x)
                        {
                            /** Log e Historico. */
                            HFLog.Log(LogLevel.Error, x.Message, equipo.IdEquipo, fr.FrecuenciaSintonizada, usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_ASIGNACION);
                            // 20171003. esto parece un error. Si un equipo pre-seleccionado no puede ser asignado, debería marcarse como tal y continuar con un nuevo equipo si existen 
                            // otros disponibles.
                            return (int)EquipoHFStd.stdError;
                        }
                    }
                    // 20171004. AGL. Este tema no entiendo por que está aqui. Creo que es un error. Lo comento.
                    //else if (equipo.Estado == EquipoHFStd.stdNoinfo)
                    //{
                    //    /** Log e Historico. */
                    //    HFLog.Log(LogLevel.Error, "Error en Asignacion de Equipo por Time-Out.", equipo.IdEquipo, fr.FrecuenciaSintonizada, usuario,
                    //        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                    //    // 20171003. Iden anterior..
                    //    return (int)EquipoHFStd.stdError;
                    //}
                    /////////////////////////////////////////////////
                }
            }

            return (int)EquipoHFStd.stdError;         
#else
            // 20171004. AGL. Nueva version de gestión de tablas y equipos...
            EquipoHF equipo;
            int retorno = (int)EquipoHFStd.stdError;
            lock (_equipos)
            {
                int fsint = fr.FrecuenciaSintonizada;
                
                /** 20171128. Si el usuario y la frecuencia coinciden en un equipo... Le doy curso... */
                EquipoHF yaAsignado = _equipos.Where(eq => 
                    eq.Usuario == usuario && 
                    eq.IDFrecuencia == fsint.ToString() ).FirstOrDefault();
                if (yaAsignado != null)
                {
                    /** 20180109. Solo le doy curso si se ha completado la asignacion */
                    if (yaAsignado.IsBeingAssigned)
                    {
                        HFHelper.Log(LogLevel.Debug, "Peticion de Asignacion con Equipo stdOperationInProgress", yaAsignado.IdEquipo, "????", yaAsignado.Usuario);
                        return (int)EquipoHFStd.stdOperationInProgress;
                    }
                    else
                    {
                        HFHelper.Log(LogLevel.Error, "Peticion de Asignacion Consecutiva...", yaAsignado.IdEquipo, yaAsignado.IDFrecuencia, yaAsignado.Usuario);
                        return (int)yaAsignado.Estado;
                    }
                }
                /**********************************************/
                /** Miro si el usuario tiene ya una frecuencia o la frecuencia la tiene otro usuario */
                EquipoHF equipoEnUsuario = _equipos.Where(eq => eq.Usuario == usuario).FirstOrDefault();
                EquipoHF equipoEnFrecuen = _equipos.Where(eq => eq.IDFrecuencia == fsint.ToString()).FirstOrDefault();
                if (equipoEnUsuario != null || equipoEnFrecuen != null)
                {
                    HFHelper.Log(LogLevel.Error,
                        CTranslate.translateResource("Intento de asignacion múltiple"),
                        null, fr.FrecuenciaSintonizada.ToString(), usuario, U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_INTENTO_ASIGNACION_MULTIPLE);
                    return (equipoEnUsuario != null) ? (int)EquipoHFStd.stdTxAlreadyAssigned : (int)EquipoHFStd.stdFrequencyAlreadyAssigned;
                }

                /** Ordeno la Lista y busco un equipo adecuado */
                if (Properties.Settings.Default.HFBalanceoAsignacion == true)
                    _equipos.Sort();
                equipo = _equipos.Where(eq => eq.Estado == EquipoHFStd.stdDisponible && eq.CheckFrecuencia(fsint) == true).FirstOrDefault();
            }

            if (equipo != null && equipo.IsBeingAssigned == false)
            {
                try
                {
                    equipo.acquire();
                    equipo.IsBeingAssigned = true;

                    HFHelper.Trace();
                    /** Sintonizar el equipo */
                    equipo.Asigna(usuario, ref fr, from);
                    HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo asignado"), equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ASIGNADO);
                    retorno = (int)equipo.Estado;
                }
                catch (Exception x)
                {
                    /** Log e Historico. */
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, fr.FrecuenciaSintonizada.ToString(), usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_ASIGNACION);
                    // 20171003. esto parece un error. Si un equipo pre-seleccionado no puede ser asignado, debería marcarse como tal y continuar con un nuevo equipo si existen 
                    // otros disponibles.
                    retorno = (int)EquipoHFStd.stdError;
                }
                finally
                {
                    equipo.IsBeingAssigned = false;
                    equipo.release();
                }
            }
            else
            {
                if (equipo != null)
                    HFHelper.Log(LogLevel.Debug, "Peticion de Asignacion con Equipo stdOperationInProgress", equipo.IdEquipo, "????", equipo.Usuario);
                retorno = (int)(equipo == null ? EquipoHFStd.stdError : EquipoHFStd.stdOperationInProgress);
            }
            return retorno;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="equipo"></param>
        /// <returns></returns>
        int Desasigna(EquipoHF equipo)
        {
            int retorno = (int)EquipoHFStd.stdError;
            if (equipo != null && equipo.IsBeingDesassigned == false)
            {
                HFHelper.Trace();
                try
                {
                    equipo.acquire();
                    equipo.IsBeingDesassigned = true;

                    equipo.Desasigna();
                    HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo desasignado"), equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO);
                    retorno = (int)EquipoHFStd.stdDisponible;
                }
                catch (Exception x)
                {
                    /** Log e Historico. */
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                    // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible'...
                    retorno = (int)EquipoHFStd.stdError;
                }
                finally
                {
                    equipo.IsBeingDesassigned = false;
                    equipo.release();
                }
            }
            else
            {
                retorno = (int)(equipo == null ? EquipoHFStd.stdError : EquipoHFStd.stdOperationInProgress);
            }
            return retorno;
        }

        /// <summary>
        /// Esta rutina se invoca al recibirse una 'Desconexion SIP' de algun recurso (Rx o Tx) asociado a una Frecuencia con <TipoDeFrecuencia == "HF">
        /// </summary>
        /// <param name="fr"></param>
        /// <returns></returns>
        public int DesasignarTxHf(ref RdFrecuency fr)
        {
#if __VERSION_0__
            /** Miro si el usuario tiene ya una frecuencia */
            lock (_equipos)
            {
                // 2017104. AGL. Utilizo LINQ...
                string str_frec = fr.FrecuenciaSintonizada.ToString();
                EquipoHF equipo = _equipos.Where(eq => eq.IDFrecuencia == str_frec).FirstOrDefault();
                if (equipo != null)
                //foreach (EquipoHF equipo in _equipos)
                ///////////////////////////////////////////////////
                {
                    //if (equipo.IDFrecuencia == fr.Frecuency)
                    if (equipo.IDFrecuencia == fr.FrecuenciaSintonizada.ToString())
                        {
                        try
                        {
                            HFLog.Trace();
                            equipo.Desasigna(ref fr);
                            HFLog.Log(LogLevel.Info, "Equipo Desasignado", equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO);
                            return (int)EquipoHFStd.stdDisponible;
                        }
                        catch (Exception x)
                        {
                            /** Log e Historico. */
                            HFLog.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                            // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible'...
                            return (int)EquipoHFStd.stdError;
                        }
                    }
                }
            }

            return (int)EquipoHFStd.stdError;
#else
            // 20171004. AGL. Nueva version de gestión de tablas y equipos...
            EquipoHF equipo;

            /** Miro si el usuario tiene ya una frecuencia */
            lock (_equipos)
            {
                string str_frec = fr.FrecuenciaSintonizada.ToString();
                equipo = _equipos.Where(eq => eq.IDFrecuencia == str_frec).FirstOrDefault();
            }
#if __NOT_DESASIGNA_ROUTINE__
            int retorno = (int)EquipoHFStd.stdError;
            if (equipo != null && equipo.IsBeingDesassigned == false)
            {
                HFHelper.Trace();
                try
                {
                    equipo.acquire();
                    equipo.IsBeingDesassigned = true;

                    equipo.Desasigna(ref fr);
                    HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo desasignado"), equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO);
                    retorno = (int)EquipoHFStd.stdDisponible;
                }
                catch (Exception x)
                {
                    //** Log e Historico. 
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                    // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible'...
                    retorno = (int)EquipoHFStd.stdError;
                }
                finally
                {
                    equipo.IsBeingDesassigned = false;
                    equipo.release();
                }
            }
            else
            {
                retorno = (int)(equipo == null ? EquipoHFStd.stdError : EquipoHFStd.stdOperationInProgress);
            }
            return retorno;
#else
            return Desasigna(equipo);
#endif

#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usuario"></param>
        /// <param name="fr"></param>
        /// <returns></returns>
        public int DesasignarTx(string usuario, ref RdFrecuency fr)
        {
            /** Mirar que es una frecuencia HF */
            if (fr.TipoDeFrecuencia != "HF")
                return (int)EquipoHFStd.stdDisponible;

#if __VERSION_0__
            /** Miro si el usuario tiene ya una frecuencia */
            lock (_equipos)
            {
                // 2017104. AGL. Utilizo LINQ...
                EquipoHF equipo = _equipos.Where(eq => eq.Usuario == usuario).FirstOrDefault();
                if (equipo != null)
                //foreach (EquipoHF equipo in _equipos)
                /////////////////////////////////////////////////////////
                {
                    if (equipo.Usuario == usuario)
                    {
                        try
                        {
                            HFLog.Trace();
                            equipo.Desasigna(ref fr);
                            HFLog.Log(LogLevel.Info, "Equipo Desasignado", equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO);
                            return (int)EquipoHFStd.stdDisponible;
                        }
                        catch (Exception x)
                        {
                            /** Log e Historico. */
                            HFLog.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                            // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible'...
                            return (int)EquipoHFStd.stdError;
                        }
                    }
                }
            }

            return (int)EquipoHFStd.stdError;
#else
            // 20171004. AGL. Nueva version de gestión de tablas y equipos...
            EquipoHF equipo;

            /** Miro si el usuario tiene ya una frecuencia */
            lock (_equipos)
            {
                string str_frec = fr.FrecuenciaSintonizada.ToString();
                equipo = _equipos.Where(eq => eq.Usuario == usuario && eq.IDFrecuencia == str_frec).FirstOrDefault();
            }
#if __NOT_DESASIGNA_ROUTINE__
            int retorno = (int)EquipoHFStd.stdError;
            if (equipo != null && equipo.IsBeingDesassigned == false)
            {
                HFHelper.Trace();
                try
                {
                    equipo.acquire();
                    equipo.IsBeingDesassigned = true;

                    equipo.Desasigna(ref fr);
                    HFHelper.Log(LogLevel.Info, CTranslate.translateResource("Equipo desasignado"), equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO);
                    retorno = (int)EquipoHFStd.stdDisponible;
                }
                catch (Exception x)
                {
                    /** Log e Historico. */
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                    // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible'...
                    retorno = (int)EquipoHFStd.stdError;
                }
                finally
                {
                    equipo.IsBeingDesassigned = false;
                    equipo.release();
                }
            }
            else
            {
                retorno = (int)(equipo == null ? EquipoHFStd.stdError : EquipoHFStd.stdOperationInProgress);
            }

            return retorno;
#else
            return Desasigna(equipo);
#endif

#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="OnOff"></param>
        /// <returns></returns>
        public bool PrepareSelcal(RdFrecuency fr, string usuario, bool OnOff, string msg="")
        {
            /** Mirar que es una frecuencia HF */
            if (fr.TipoDeFrecuencia != "HF")
                return false;
#if __VERSION_0__
            lock (_equipos)
            {
                /** Miro si el usuario tiene ya una frecuencia */
                // 2017104. AGL. Utilizo LINQ...
                EquipoHF equipo = _equipos.Where(eq => eq.Estado == EquipoHFStd.stdAsignado && eq.Usuario == usuario).FirstOrDefault();
                if (equipo != null)
                //foreach (EquipoHF equipo in _equipos)
                /////////////////////////////////////////////////////////
                {
                    try
                    {
                        HFLog.Trace();
                        if (equipo.Estado == EquipoHFStd.stdAsignado && equipo.Usuario == usuario)
                            return equipo.PrepareSelcal(OnOff);
                    }
                    catch (Exception x)
                    {
                        /** Log e Historico. */
                        HFLog.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_PREPARACIONSELCAL);
                        // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible' o en 'error'...
                        return false;
                    }
                }
            }

            return false;
#else
            EquipoHF equipo;
            bool retorno = false;
            lock (_equipos)
            {
                /** Miro si el usuario tiene ya una frecuencia */
                // 2017104. AGL. Utilizo LINQ...
                equipo = _equipos.Where(eq => eq.Estado == EquipoHFStd.stdAsignado && eq.Usuario == usuario).FirstOrDefault();
            }
            if (equipo != null && equipo.IsBeingAssigned == false)
            {
                HFHelper.Trace();
                try
                {
                    equipo.acquire();
                    equipo.IsBeingAssigned = true;

                    retorno = equipo.PrepareSelcal(OnOff);
                }
                catch (Exception x)
                {
                    /** Log e Historico. */
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_PREPARACIONSELCAL);
                    // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible' o en 'error'...
                }
                finally
                {
                    equipo.IsBeingAssigned = false;
                    equipo.release();
                }
            }

            return retorno;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ListaEquipos(List<string> equipos)
        {
            equipos.Clear();
            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    equipos.Add(equipo.IdEquipo);
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="equipo"></param>
        /// <returns></returns>
        public bool EstadoEquipo(string idequipo, List<string> estados)
        {
            estados.Clear();

            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.IdEquipo == idequipo)
                    {
                        estados.Add(equipo.SipUri);
                        estados.Add(equipo.IpRcs + ": " + equipo.Oid);
                        estados.Add(equipo.Estado.ToString());
                        estados.Add(equipo.Usuario);
                        estados.Add(equipo.Frecuencia.ToString());
                        return true;
                    }
                }
            }
            estados.Add("NoExiste");
            estados.Add("NoExiste");
            estados.Add("NoExiste");
            estados.Add("NoExiste");
            estados.Add("NoExiste");
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idequipo"></param>
        /// <returns></returns>
        public bool LiberaEquipo(string idequipo)
        {
#if __VERSION_0__
            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.IdEquipo == idequipo)
                    {
                        equipo.Desasigna();
                        return true;
                    }
                }
            }
            return false;
#else
            EquipoHF equipo;
            bool retorno = false;
            lock (_equipos)
            {
                /** Miro si el usuario tiene ya una frecuencia */
                // 2017104. AGL. Utilizo LINQ...
                equipo = _equipos.Where(eq => eq.IdEquipo == idequipo).FirstOrDefault();
            }
            if (equipo != null)
            {
                HFHelper.Trace();
                try
                {
                    equipo.acquire();
                    HFHelper.RespondToFrHfTxChange(equipo.IpFrom, equipo.TextoTecla, (int)EquipoHFStd.stdError);
                    equipo.Desasigna();
                    retorno = true;
                }
                catch (Exception x)
                {
                    /** Log e Historico. */
                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                        U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION);
                    // 20171003. AGL. Comprobar que si se llega aquí, el equipo queda en cualquier caso 'disponible' o en 'error'...
                }
                finally
                {
                    equipo.release();
                }
            }

            return retorno;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequency"></param>
        public void CheckFrequency()
        {
#if __VERSION_0__
            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.Estado != EquipoHFStd.stdError)
                    {
                        /** AGL. Controlar los Errores. */
                        try
                        {
                            HFLog.Trace();
                            equipo.Check();
                        }
                        catch (Exception x)
                        {
                            HFLog.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_PREPARACIONSELCAL);
                        }
                    }
                }
            }
#else
            HFHelper.Trace();
            lock (_equipos)
            {
                List<EquipoHF> afectados = _equipos.Where(eq => eq.Estado != EquipoHFStd.stdError && eq.Estado != EquipoHFStd.stdNoinfo).ToList();
                foreach (EquipoHF equipo in afectados)
                {
                    Task.Factory.StartNew(() =>
                    {
                        if (equipo.IsBeingSipSupervised == false)
                        {
                            try
                            {
                                equipo.acquire();
                                equipo.IsBeingSipSupervised = true;

                                switch (HFSipSupervisionMode)
                                {
                                    case 0:
                                        break;
                                    case 1:
                                        equipo.Check();
                                        break;
                                    case 2:
                                        equipo.SipOptionsCheck();
                                        break;
                                }
                            }
                            catch (Exception x)
                            {
                                HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                            }
                            finally
                            {
                                equipo.IsBeingSipSupervised = false;
                                equipo.release();
                            }
                        }
                    });
                }
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
        public void ActualizaEquipo(int sipCallId, CORESIP_CallStateInfo stateInfo)
        {
#if __VERSION_0__
            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.FrToCheck != null)
                    {
                        HFLog.Trace();
                        string rToRemove = string.Empty;
                        foreach (KeyValuePair<string,RdResource> rs in equipo.FrToCheck.RdRs)
                        {
                            if (rs.Value.ToCheck &&
                                (equipo.SipUri.ToUpper() == rs.Value.Uri1.ToUpper() ||
                                 (rs.Value.Uri2 != null && equipo.SipUri.ToUpper() == rs.Value.Uri2.ToUpper()))
                               )
                            {
                                //equipo.FrToCheck.HandleChangeInCallState(sipCallId, stateInfo);
                                if (sipCallId == rs.Value.SipCallId)
                                {
                                    rToRemove = rs.Key;
                                    break;
                                }
                            }
                        }

                        if (rToRemove != string.Empty)
                        {
                            HFLog.Trace();
                            equipo.DesasignaFrToCheck(stateInfo);

                            //rs.HandleChecked();
                            //fr.RemoveSipCall(fr.RdRs[rsId]);
                            //fr.RdRs[rsId].Dispose();
                            //fr.RdRs.Remove(rsId);
                            //equipo.FrToCheck.RemoveSipCall(rs);

                            /** AGL2014. Solo lo actualizo si el equipo radio está presente... */
                            if (equipo.Estado != EquipoHFStd.stdNoinfo)
                            {
                                if (stateInfo.State != CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED)
                                    equipo.Estado = EquipoHFStd.stdNoResource;
                                else if (equipo.Estado != EquipoHFStd.stdAsignado && equipo.Estado != EquipoHFStd.stdError)
                                    equipo.Estado = EquipoHFStd.stdDisponible;
                            }
                        }
                    }
                }
            }
#else
            HFHelper.Trace();
            lock (_equipos)
            {
                List<EquipoHF> afectados = _equipos.Where(eq => eq.FrToCheck != null).ToList();
                foreach (EquipoHF equipo in afectados)
                {
                    Task.Factory.StartNew(() =>
                    {
                        if (equipo.IsBeingSipSupervised == false)
                        {
                            try
                            {
                                equipo.acquire();
                                equipo.IsBeingSipSupervised = true;

                                switch (HFSipSupervisionMode)
                                {
                                    case 0:
                                        break;
                                    case 1:
                                        {
                                            string rToRemove = string.Empty;
                                            foreach (KeyValuePair<string, IRdResource> rdRs in equipo.FrToCheck.RdRs)
                                            {
                                                RdResource simpleRes = rdRs.Value.GetSimpleResource(sipCallId);
                                                if (simpleRes != null)
                                                {
                                                    //        foreach (KeyValuePair<string, RdResource> rs in equipo.FrToCheck.RdRs)
                                                    //{
                                                    if (simpleRes.ToCheck &&
                                                        (equipo.SipUri.ToUpper() == simpleRes.Uri1.ToUpper() ||
                                                         (simpleRes.Uri2 != null && equipo.SipUri.ToUpper() == simpleRes.Uri2.ToUpper()))
                                                       )
                                                    {
                                                        HFHelper.Trace();
                                                        equipo.DesasignaFrToCheck(stateInfo);

                                                        //rs.HandleChecked();
                                                        //fr.RemoveSipCall(fr.RdRs[rsId]);
                                                        //fr.RdRs[rsId].Dispose();
                                                        //fr.RdRs.Remove(rsId);
                                                        //equipo.FrToCheck.RemoveSipCall(rs);

                                                        /** AGL2014. Solo lo actualizo si el equipo radio está presente... */
                                                        if (equipo.Estado != EquipoHFStd.stdNoinfo)
                                                        {
                                                            if (stateInfo.State != CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED)
                                                                equipo.Estado = EquipoHFStd.stdNoResource;
                                                            else if (equipo.Estado != EquipoHFStd.stdAsignado && equipo.Estado != EquipoHFStd.stdError)
                                                                equipo.Estado = EquipoHFStd.stdDisponible;
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case 2:
                                        // TODO. Por SIP OPTIONS.
                                        break;
                                }
                            }
                            catch (Exception x)
                            {
                                HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario);
                            }
                            finally
                            {
                                equipo.IsBeingSipSupervised = false;
                                equipo.release();
                            }
                        }
                    });
                }
            }
#endif
        }

        /// <summary>
        /// 20171130. Para la supervision de los transmisores por Options...
        /// </summary>
        /// <param name="from"></param>
        /// <param name="code"></param>
        /// <param name="supported"></param>
        /// <param name="allowed"></param>
        public void OptionsResponseReceived(String from, int code, string supported, string allowed)
        {
            if (HFSipSupervisionMode == 2)
            {
                lock (_equipos)
                {
                    // Encuentro el equipo
                    EquipoHF equipo = _equipos.Where(eq => eq.SipUri == from).FirstOrDefault();
                    if (equipo != null)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            if (equipo.IsBeingSipSupervised == false)
                            {
                                try
                                {
                                    equipo.acquire();
                                    equipo.IsBeingSipSupervised = true;
                                    if (equipo.Estado != EquipoHFStd.stdNoinfo)
                                    {
                                        if (code != 200)
                                            equipo.Estado = EquipoHFStd.stdNoResource;
                                        else if (equipo.Estado != EquipoHFStd.stdAsignado && equipo.Estado != EquipoHFStd.stdError)
                                            equipo.Estado = EquipoHFStd.stdDisponible;
                                    }
                                }
                                catch (Exception x)
                                {
                                    HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario);
                                }
                                finally
                                {
                                    equipo.IsBeingSipSupervised = false;
                                    equipo.ConsecutiveOptionsRequestCounter = 0;
                                    equipo.release();
                                }
                            }
                        });
                    }
                }
            }
            else
            {
                HFHelper.Log(LogLevel.Error, "Recibido Options no localizado en ningun equipo...", "???");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HFStatusCodes LastGlobalStatus { get; set; }
        public HFStatusCodes GlobalStatus()
        {
#if __VERSION_0__
            HFStatusCodes status = HFStatusCodes.DISC;
            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.Estado == EquipoHFStd.stdDisponible)
                        return HFStatusCodes.DISP;
                    if (equipo.Estado == EquipoHFStd.stdAsignado)
                        status = HFStatusCodes.NODISP;
                }
            }

            if (status == HFStatusCodes.DISC)
            {
                foreach (EquipoHF eq in _equipos)
                {
                    eq.Desasigna();
                    RdRegistry.RespondToFrHfTxChange(eq.IpFrom, eq.IDFrecuencia, (int)EquipoHFStd.stdError);
                }
            }
#else
            HFStatusCodes status = GlobalStatusPeriodico();
            if (status == HFStatusCodes.DISC)
            {
                lock (_equipos)
                {                    
                    foreach (EquipoHF equipo in _equipos)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                equipo.acquire();

                                if (equipo.FrAsignada != null)
                                {
                                    equipo.Desasigna();
                                    HFHelper.RespondToFrHfTxChange(equipo.IpFrom, equipo.TextoTecla, (int)EquipoHFStd.stdError);
                                }
                            }
                            catch (Exception x)
                            {
                                HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                            }
                            finally
                            {
                                equipo.release();
                            }
                        });
                    }
                }
            }
#endif
            return status;
        }

        /** */
        public HFStatusCodes GlobalStatusPeriodico()
        {
            HFStatusCodes status = HFStatusCodes.DISC;

            lock (_equipos)
            {
                foreach (EquipoHF equipo in _equipos)
                {
                    if (equipo.Estado == EquipoHFStd.stdDisponible)
                        return HFStatusCodes.DISP;
                    if (equipo.Estado == EquipoHFStd.stdAsignado)
                        status = HFStatusCodes.NODISP;
                }
            }

            return status;
        }

#if __VERSION_0__
#else
        public void SupervisaEstadoEquipos()
        {
            HFHelper.Trace();
            lock (_equipos)
            {
                List<EquipoHF> afectados = _equipos;
                foreach (EquipoHF equipo in afectados)
                {
                    Task.Factory.StartNew(() =>
                    {
                        if (equipo.IsBeingSnmpSupervised == false)
                        {
                            try
                            {
                                equipo.acquire();
                                equipo.IsBeingSnmpSupervised = true;

                                equipo.GetEstado();
                            }
                            catch (Exception x)
                            {
                                HFHelper.Log(LogLevel.Error, x.Message, equipo.IdEquipo, equipo.IDFrecuencia, equipo.Usuario,
                                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL);
                            }
                            finally
                            {
                                equipo.IsBeingSnmpSupervised = false;
                                equipo.release();
                            }
                        }
                    });
                }
            }
        }

#endif
    }
}

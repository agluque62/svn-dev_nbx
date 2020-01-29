using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;
using u5ki.RemoteControlService;
using U5ki.Delegates;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.RdService.Properties;
using Translate;
using U5ki.RdService.NM;

namespace U5ki.RdService.Gears
{    
    /// <summary>
    /// Nodo que representa un equipo generico que va a ser manejado por el NMManager.
    /// </summary>
    public class BaseGear : BaseNode
    {

        #region Declarations

        private Func<BaseGear, bool> ReserveFrecuency { get; set; }
        private Func<BaseGear, bool> UnReserveFrecuency { get; set; }

        public event BaseGearOperation OnGearAllocated;
        public event BaseGearOperation OnGearDeallocated;
        public event BaseGearDelegate OnGearStatusUpdated;
        public event BaseGearOperation OnGearChecked;
        
        private IRemoteControl _remoteControl;
        /// <summary>
        /// Usado para realizar las conexiones de los nodos.
        /// </summary>
        public IRemoteControl RemoteControl
        {
            get
            {
                if (null == _remoteControl)
                    _remoteControl = Locals.RemoteControlFactory.ManufactureOne(this); 
                return _remoteControl;
            }
        }

        // --------------------------------------------------
        // --------------------------------------------------
        // Datos basicos del nodo

        public string SipUri { get; set; }
        public string Oid { get; set; }
        public IList<U5ki.Infrastructure.HfRangoFrecuencias> FrecuenciesAllowed { get; set; }

        // --------------------------------------------------
        // --------------------------------------------------
        // Datos avanzados del nodo

        public bool IsReceptor { get; set; }
        public RdRsType ResourceType
        {
            get
            {
                if (IsReceptor && IsEmitter)
                    return RdRsType.RxTx;
                else if (IsReceptor)
                    return RdRsType.Rx;
                else if (IsEmitter)
                    return RdRsType.Tx;

                //LogDebug<BaseGear>("[ResourceType] Error de configuración. El equipo " + SipUri + " no es ni receptor ni trasnmisor.", 
                //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                LogInfo<BaseGear>("[ResourceType] Error de configuración. El equipo " + SipUri + " no es ni receptor ni trasnmisor.",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("El equipo no es ni receptor ni trasnmisor."));
                return RdRsType.Tx; // TODO: Comprobar si podemos devolver un null aqui en vez de un default de emisor.
            }
        }
        /// <summary>
        /// Codigo de identificación del emplazamiento al que esta asignado el nodo para el control de frecuencias desplazadas.
        /// </summary>
        //JOI FREC_DES
        public string IdEmplazamiento { get; set; }
        //JOI FREC_DES FIN

        /// <summary>
        /// Representa la "FrecuenciaClave" que vienen en el proto, es decir, el ID de la frecuencia que se utiliza en el HMI para gestionar las frecuencias. 
        /// <para>Para que al hacer Allocate y Deallocate en una frecuencia el resto de los miembros se enteren, se ha de utilizar esta Clave.</para>
        /// </summary>
        public String FrecuencyKey { get; set; }

        public Tipo_Frecuencia FrecuencyType { get; set; }
        /// <summary>
        /// Frecuencia que tiene el equipo configurada desde la configuracion. 
        /// Solo debe de estar rellana desde configuracion si es Master. 
        /// El Slave no deberia tenerlo.
        /// </summary>
        public string FrecuencyMain { get; set; }
        public Tipo_Formato_Frecuencia FrecuencyMainFormat { get; set; }
        public Tipo_Canal ChannelType { get; set; }
        public Int32? Priority { get; set; }

        public Tipo_Formato_Trabajo WorkingFormat { get; set; }
        public new Boolean IsMaster
        {
            get
            {
                return WorkingFormat == Tipo_Formato_Trabajo.Principal || WorkingFormat == Tipo_Formato_Trabajo.Ambos;
            }
        }
        public Boolean IsSlave
        {
            get
            {
                return WorkingFormat == Tipo_Formato_Trabajo.Reserva || WorkingFormat == Tipo_Formato_Trabajo.Ambos;
            }
        }
        
        /// <summary>
        /// Frecuencia a la que esta operando este nodo en este momento. 
        /// </summary>
        /// <remarks>
        /// Esta propiedad representa la cambiante frecuencia que puede asignarse y se asigna a un nodo. Hay que limpiarla al desasignar el nodo.
        /// </remarks>
        public new String Frecuency 
        {
            get
            {
                if (!String.IsNullOrEmpty(base.Frecuency))
                    return base.Frecuency;
                return FrecuencyMain;
            }
            set
            {
                String input = value;
                if (base.Frecuency != input)
                    LastFrecuency = base.Frecuency;
                base.Frecuency = value;
            }
        }
        /// <summary>
        /// Frecuencia anterior a la que tenemos justo asignada.
        /// </summary>
        public String LastFrecuency { get; set; }

        // --------------------------------------------------
        // --------------------------------------------------
        // Datos de Estado.

        public GearStatus OldStatus { get; set; }
        /// <summary>
        /// IMPORTANTE: Usar el campo para lectura interna, para no bloquear el hilo. con el semaforo.
        /// </summary>
        private GearStatus _status;
        /// <summary>
        /// Estado actual del equipo. 
        /// Hay que recordar que es un supuesto estado, puesto que no hay sistema de comunicación directa.
        /// Es la aplicación la que tiene que estar preguntando regularmente y actualizando el estado.
        /// 
        /// </summary>
        public override GearStatus Status 
        { 
            get
            {
                    return _status;
                }
            set
            {
                OldStatus = _status;
                _status = value;
                if (OldStatus != _status && null != OnGearStatusUpdated)
                {
                    this.LastStatusModification = DateTime.Now;
                    OnGearStatusUpdated.Invoke(this);
                    //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX
                    if (_status == GearStatus.Forbidden)
                    {
                        if (MNManager.NodeAddPoolForbidden(Id, GearStatus.Forbidden) == true)
                        {
                            MNManager.NodePoolForbiddenPublish();
                        }
                    }
                    else
                    {
                        if (MNManager.NodeRemovePoolForbidden(Id) == true)
                        {
                            MNManager.NodePoolForbiddenPublish();
                        }
                    }
                    //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX FIN
                }
            }
        }

        public Boolean IsAvailable
        {
            get
            {
                return (this._status != Enums.GearStatus.Fail && 
                        this._status != Enums.GearStatus.Forbidden &&
                        this._status != Enums.GearStatus.AssignationInProgress);
            }
        }

        private BaseGear _replaceBy;
        /// <summary>
        /// Representa la referencia al Nodo que esta haciendo de reemplazo mientras este esta indispuesto. 
        /// </summary>
        public BaseGear ReplaceBy
        {
            get
            {
                return _replaceBy;
            }
            set
            {
                if (this.IsMaster || null == value)
                    _replaceBy = value;
#if DEBUG
                else
                    LogFatal<BaseGear>(Environment.NewLine + "   ---   FATAL   ---   NO SE PUEDE ASIGNAR UN REEMPLAZO A UN SLAVE." + Environment.NewLine);
#endif
            }
        }
        private String ReplaceById
        {
            get
            {
                if (null == _replaceBy)
                    return null;
                return _replaceBy.Id;
            }
        }

        private BaseGear _replaceTo;
        /// <summary>
        /// Representa la referencia al Nodo que esta reemplazando. 
        /// </summary>
        public BaseGear ReplaceTo
        {
            get
            {
                return _replaceTo;
            }
            set
            {
                if (this.IsSlave || null == value)
                    _replaceTo = value;
#if DEBUG
                else
                    LogFatal<BaseGear>(Environment.NewLine + "   ---   FATAL   ---   NO SE PUEDE ASIGNAR UN MASTER PARA REEMPLAZAR." + Environment.NewLine);
#endif
            }
        }
        private String ReplaceToId
        {
            get
            {
                if (null == _replaceTo)
                    return null;
                return _replaceTo.Id;
            }
        }

        /// <summary>
        /// Representa el numero de veces que tiene que ser ignorado el elemento en el tick de comprobación. 
        /// </summary>
        /// <remarks>
        /// Se utiliza para evitar una sobrecarga de timeouts, una vez que un elemento da un Timeout, 
        /// se queda fuera de la comprobación durante X tiempo,
        /// para no saturar el semaforo de respuestas.
        /// </remarks>
        private Int32 _ignoreValidationCount { get; set; }
        /// <summary>
        /// Representa si el elemento ha de ser ignorado en la validación. Cada vez que se pregunte a esta propiedad reducira en 1 el numero de iteraciones que tiene que ignorar.
        /// NOTA: Contiene un algoritmo de control. Ver el codigo.
        /// </summary>
        public Boolean CanValidate
        {
            get
            {
                if (_ignoreValidationCount > 0)
                {
                    _ignoreValidationCount--;
                    return false;
                }
                return true;
            }
            set
            {
                if (value)
                    _ignoreValidationCount = 0;
                else
                    _ignoreValidationCount = Settings.Default.TimeoutIgnoreValidationCount;
            }
        }

        /// <summary>
        /// Representa el numero de veces seguidas que la comprobacion de un equipo ha devuelvo Timeout. 
        /// </summary>
        public Int32 TimeoutsCount { get; set; }
        /// <summary>
        /// Representa si el elemento ha hecho el numero sufieciente de veces timeout seguidas.
        /// </summary>
        public Boolean IsTimeout
        {
            get
            {
                return (TimeoutsCount >= Convert.ToInt32(Settings.Default.TimeoutHitAmount));
            }
        }

        /// <summary>
        /// Representa el numero de veces seguidas que la comprobacion de un equipo ha devuelvo KO. 
        /// </summary>
        public Int32 KOsCount { get; set; }
        /// <summary>
        /// Representa si el elemento ha hecho el numero sufieciente de veces KO seguidas.
        /// </summary>
        public Boolean IsKO
        {
            get
            {
                return (KOsCount >= Convert.ToInt32(Settings.Default.KOAmount));
            }
        }
        //20161222 JOI
        /// <summary>
        /// Representa el numero de veces que se testea un equipo Master en KO omitiendo proceso de busqueda de Slave. 
        /// </summary>
        public Int32 OmiteKOsCount { get; set; }
        /// <summary>
        /// Representa si el equipo Master ha omitido  un numero suficiente de veces KO para relanzar una nueva busqueda de Slave.
        /// </summary>
        public Boolean BuscaSlave
        {
            get
            {
                return (OmiteKOsCount > 1); //20170113 20' del pool
            }
        }
        //20161222 JOI FIN

        //20180319 INHABILITACIÓN POR ERROR SNMP
        static int iMaxKOSNMP = 5;
        /// <summary>
        /// Representa el numero de veces seguidas que la gestión SNMP devuelve ERROR. 
        /// </summary>
        private Int32 _CountKOSNMP { get; set; }
        /// <summary>
        /// <summary>
        /// Representa si el elemento ha fallado un determinado número veces en el proceso de gestión SNMP.
        /// </summary>
        public Boolean IsKOSNMP
        {
            get
            {
                _CountKOSNMP++;
                return (_CountKOSNMP >= iMaxKOSNMP);
            }
            set
            {
                _CountKOSNMP = 0;
            }
        }
        //20180319 INHABILITACIÓN POR ERROR SNMP FIN

#if DEBUG
        private ManagedSemaphore _semaphore = new ManagedSemaphore(1,1,"BaseGear");
        Random rnd = new Random();
#else
        private System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(1, 1);
#endif

        #endregion

        #region Initialize
        
        /// <summary>
        /// NOTA: Contructor basico para el Logger. No ut5ilizar para desarrollo.
        /// </summary>
        public BaseGear()
        {
        }

        /// <summary>
        /// Constructor basico en el que se pasa el nodo recibido de configuración.
        /// </summary>
        /// <param name="input">El nodod de configuración.</param>
        /// <param name="reserveFrecuency">La funcion remota que se va a utilziar para pedir que se reserve una frecuencia para este nodo.</param>
        /// <param name="reserveFrecuency">La funcion remota que se va a utilziar para liberar una frecuencia para uso externo.</param>
        public BaseGear(
            Node input, 
            Func<BaseGear, bool> reserveFrecuency,
            Func<BaseGear, bool> unReserveFrecuency,
            BaseGearOperation onGearAllocated,
            BaseGearOperation onGearDeallocated,
            BaseGearDelegate onGearStatusUpdated,
            BaseGearOperation onGearChecked)
            : base(input)
        {
#if DEBUG
            _semaphore.SetName(Id);
#endif
            // Inicializacion de los Handlers.
            ReserveFrecuency = reserveFrecuency;
            UnReserveFrecuency = unReserveFrecuency;

            OnGearAllocated += onGearAllocated;
            OnGearDeallocated += onGearDeallocated;
            OnGearStatusUpdated += onGearStatusUpdated;
            OnGearChecked += onGearChecked;

            /** 20180625. Parseo de sip uri*/
            SipUtilities.SipUriParser sipuri = new SipUtilities.SipUriParser(input.SipUri);
#if DEBUG            
            if (U5ki.Infrastructure.Code.Globals.Test.IsRCNDFSimuladoRunning)
            {
                this.RemoteControlType = RCTypes.RCNDFSimulado;
                this.IP = "192.168.0.71";
                this.Port = 161;
                this.Priority = rnd.Next(40, 60);
            }
            else
#endif
            {
				this.RemoteControlType = (RCTypes)input.ModeloEquipo;   // RCTypes.RCRohde4200;
                this.IP = sipuri.Host;                                  // 20180625. Parseo de sip uri ==>*/ ParseSipUriIp(input.SipUri);
				this.Port = (int)input.Puerto;
				this.Priority = input.Prioridad;
            }

            // Inicializacion de los parametros.
            // JOI: URI PORT
            string sSipUri = sipuri.UlisesFormat;                       // 20180625. Parseo de sip uri ==>*/ ControlSipUriPort(input.SipUri);
            this.SipUri = sSipUri; // input.SipUri;
            // JOI: SIP PORT FIN
 
            this.Oid = input.Oid;
            this.FrecuenciesAllowed = input.Frecs;

            this.IsReceptor = input.EsReceptor;
            //this.IsEmitter = input.EsTransmisor;

            this.FrecuencyKey = input.FrecuenciaClave;

            this.FrecuencyType = input.TipoDeFrecuencia;
            this.FrecuencyMain = input.FrecuenciaPrincipal;
            if (null == FrecuencyMain)
                FrecuencyMain = String.Empty;
            this.FrecuencyMainFormat = input.FormatoFrecuenciaPrincipal;
            this.ChannelType = input.TipoDeCanal;
            this.WorkingFormat = input.FormaDeTrabajo;

            this.PowerLevel = input.NivelDePotencia;
            this.Power = input.Potencia;
            this.Modulation = input.Modulacion;
            this.Offset = input.Offset;
            this.Channeling = input.Canalizacion;
            //JOI FREC_DES
            this.IdEmplazamiento = input.idEmplazamiento;
            //JOI FREC_DES FIN

            // Valores por defecto
            InitializeDefaultValues();

            // Tick on the LastMod.
            LastCfgModification = DateTime.Now;

#if DEBUG
            Console.WriteLine();
            Console.WriteLine(" ===> " + this.ToString(false));
            Console.WriteLine();
#endif
        }

        /// <summary>
        /// Obsoletas...
        /// </summary>
        /// <param name="sipUri"></param>
        /// <returns></returns>
        private String ParseSipUriIp(String sipUri)
        {
            if (String.IsNullOrEmpty(sipUri))
                return String.Empty;

            String[] parsed = sipUri.Split('@');
            if (parsed.Count() == 2)
                return parsed[1].Remove(parsed[1].Length - 1);
            else
                return parsed[0];
        }
        //JOI: SIP PORT
        private String ControlSipUriPort(String sipUri)
        {
            const String UriPortDefault = "5060";

            if (sipUri.Split(':').Length - 1 > 1)
                return sipUri;

            string[] campo = sipUri.Split('>');

            return campo[0] + ":" + UriPortDefault + ">";
        }

        private void InitializeDefaultValues()
        {
            if (this.Port == 0)
            {
                //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Puerto de escucha del SNMP. Se asigna el valor por defecto. " + this.ToString(),
                //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Puerto de escucha del SNMP. Se asigna el valor por defecto. " + this.ToString(),
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                    Id, CTranslate.translateResource("Puerto de escucha del SNMP. Se asigna el valor por defecto."));
                if (IsEmitter)
                    Port = 161;
                else
                    if (this.RemoteControlType == RCTypes.RCJotron7000)
                        this.Port = 161;
                    else
                    this.Port = 160;
            }

            if (this.Channeling == GearChannelSpacings.ChannelSpacingsDefault)
            {
                if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                    LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el 'Canal' de la emision. Se asigna el valor por defecto. " + this.ToString(),
                        U5kiIncidencias.U5kiIncidencia.IGNORE,
                        Id, CTranslate.translateResource("Channel spacing incorrecto. Se asigna el valor por defecto (25)."));
                this.Channeling = GearChannelSpacings.kHz_25_00;
            }
            if (this.IsEmitter) 
            {
                if (this.PowerLevel == GearPowerLevels.PowerLevelsDefault)
                {
                    if (!IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        this.PowerLevel = GearPowerLevels.Normal;
                }

                //20170109 JOI: NO se modifica el valor de potencia. La radio debe estar previamente configurada 
                //InitializeValidatePower();
                //20170109 JOI: NO se modifica el valor de potencia. La radio debe estar previamente configurada FIN
            }
        }

        private void InitializeValidatePower()
        {
            if (!this.IsEmitter)
                return;

            if (this.FrecuencyType == Tipo_Frecuencia.VHF)
                InitializeValidatePowerVHF();
            else if (this.FrecuencyType == Tipo_Frecuencia.UHF)
                InitializeValidatePowerUHF();
        }
        private void InitializeValidatePowerVHF()
        {
            if (this.PowerLevel == GearPowerLevels.Normal)
            {
                // NormalLevel Valid Power Range: 5 - 50 (VHF)
                if (this.Power < 5)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power level (VHF-Normal) inferior a 5. Se asigna el valor por defecto (5)."));
                    this.Power = 5;
                }
                else if (this.Power > 50)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power level (VHF-Normal) superior a 50. Se asigna el valor por defecto (50)."));
                    this.Power = 50;
                }
            }
            else
            {
                // LowLevel Valid Power Range: 4 - 6 (VHF)
                if (this.Power < 4)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power level (VHF-Normal) inferior a 4. Se asigna el valor por defecto (4)."));
                    this.Power = 4;
                }
                else if (this.Power > 6)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power Level (VHF-Low) superior a 6. Se asigna el valor por defecto (6)."));
                    this.Power = 6;
                }
            }
        }
        private void InitializeValidatePowerUHF()
        {
            if (this.PowerLevel == GearPowerLevels.Normal)
            {
                // NormalLevel Valid Power Range: 10 - 50 (UHF)
                if (this.Power < 10)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power level (UHF-Normal) inferior a 10. Se asigna el valor por defecto (10)."));
                    this.Power = 10;
                }
                else if (this.Power > 50)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power Level (UHF-Normal) superior a 50. Se asigna el valor por defecto (50)."));
                    this.Power = 50;
                }
            }
            else
            {
                // LowLevel Valid Power Range: 9 - 11 (UHF)
                if (this.Power < 9)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power level (UHF-Low) inferior a 9. Se asigna el valor por defecto (9)."));
                    this.Power = 9;
                }
                else if (this.Power > 11)
                {
                    //LogDebug<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR);
                    if (IsMaster) // 20160921. AGL. En los SLAVE LIBRES se pone por defecto...
                        LogInfo<BaseGear>("[GEAR INIT] Error de configuración en el Power Level de " + this.ToString(),
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,
                            Id, CTranslate.translateResource("Power Level (UHF-Low) superior a 11. Se asigna el valor por defecto (11)."));
                    this.Power = 11;
                }
            }
        }

        #endregion

        #region Allocate

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="offset"></param>
        /// <param name="channeling"></param>
        /// <param name="modulation"></param>
        /// <param name="powerLevel"></param>
        /// <param name="power"></param>
        /// <param name="priority"></param>
        /// <param name="onOkOperationForbidden"></param>
        protected virtual void Allocate(
            String frecuency, 
            GearCarrierOffStatus offset,
            GearChannelSpacings channeling,
            GearModulations modulation,
            GearPowerLevels powerLevel,
            Int32? power,
            Int32? priority,
            Boolean onOkOperationForbidden = false)  
        {

            // Si la frecuencia recibida esta vacia. No hay que continuar.
            // OJO. Si viene una MASTER...
            if (String.IsNullOrEmpty(frecuency))
            {
                AllocateResponse(GearOperationStatus.Rejected);                
                return;
            }

            // Adicionalmente un nodo con mas prioridad y en estado asignado tiene que rechazar peticiones.
            //   Esta es una segunda comprobación de seguridad debido al posibles crash de multithread desde dos peticiones de reemplazo.
            if (_status == GearStatus.Assigned && priority >= this.Priority)
            {
                AllocateResponse(GearOperationStatus.Rejected);
                return;
            }

            // Logica de asignación.
            this.Frecuency = frecuency;
            this.Channeling = channeling;
            this.Modulation = modulation;
            if (this.IsEmitter)
            {
                this.Offset = offset;
                this.PowerLevel = powerLevel;
                this.Power = power;
            }
            if (this.IsSlave)
                this.Priority = priority;

            // Validamos si disponemos de la frecuencia para este equipo.
            if (!ReserveFrecuency(this))
            {
                AllocateResponse(GearOperationStatus.Fail);
                return;
            }

            // Check if the response of the operation, in case of Ok, must be a Forbidden.
            if (onOkOperationForbidden)
                RemoteControl.ConfigureNode(RCConfigurationAction.Assing, OnAllocateResponseForbidden, this, this.IsEmitter, this.IsMaster);                
            else
                RemoteControl.ConfigureNode(RCConfigurationAction.Assing, OnAllocateResponse, this, this.IsEmitter, this.IsMaster);
        }
        /// <summary>
        /// Asignar este nodo con los datos de otro nodo.
        /// </summary>
        public void Allocate(BaseGear gearToReplace, Boolean onOkOperationForbidden = false)
        {
            _semaphore.WaitOne();

            SetReplacements(gearToReplace);
            Allocate(
                gearToReplace.Frecuency,
                gearToReplace.Offset,
                gearToReplace.Channeling,
                gearToReplace.Modulation,
                gearToReplace.PowerLevel,
                gearToReplace.Power, 
                gearToReplace.Priority);
        }
        /// <summary>
        /// Asignar este nodo con su propia configuración.
        /// </summary>
        public void Allocate(Boolean onOkOperationForbidden = false)
        {
            _semaphore.WaitOne();
            ClearReplacements();
            Allocate(
                this.Frecuency,
                this.Offset,
                this.Channeling,
                this.Modulation,
                this.PowerLevel,
                this.Power,
                this.Priority,
                onOkOperationForbidden);
        }

        private void AllocateResponse(GearOperationStatus response, GearStatus? onOkOperationStatus = null)
        {
            try
            {
                if (null == onOkOperationStatus)
                    onOkOperationStatus = GearStatus.Assigned;

                switch (response)
                {
                    case GearOperationStatus.OK:
                        if (_status == GearStatus.Initial)
                        {
                            LogInfo<BaseGear>("[OPERATION " + GearOperationStatus.OK + "] " + ToString(),
                                U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DISP, Id);
                        }
                        if (IsSlave && ReplaceTo != null && 
                            (ReplaceTo.Status == GearStatus.Assigned || ReplaceTo.Status == GearStatus.Ready))
                        {
                            //Se cancela el allocate del esclavo porque el maestro se ha recuperado 
                            //durante el proceso de conmutacion
                            ClearReplacements();
                            this.Priority = null;
                            this.Frecuency = String.Empty;
                        }
                        else
                            Status = (GearStatus)onOkOperationStatus;
                        break;

                    case GearOperationStatus.Timeout:
                    case GearOperationStatus.Fail:
                        ClearReplacements();
                        this.Frecuency = String.Empty;
                        if (this.IsSlave)
                            this.Priority = null;

                        if ((this.IsMaster && OldStatus != GearStatus.Initial) || this.IsSlave)
                            Status = GearStatus.Fail;
                        break;

                    case GearOperationStatus.Rejected:
                        ClearReplacements();
                        return;
                }
                // Movido dentro del semaforo para evitar llamar a ClearReplacements antes de terminar un allocate OK
                // y provocar una excepción
                UnReserveFrecuency(this);
                OnGearAllocated.Invoke(this, response);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        /// <summary>
        /// Respuesta asincrona a la peticion de asignación.
        /// </summary>
        private void OnAllocateResponse(GearOperationStatus response)
        {
            AllocateResponse(response);
        }
        /// <summary>
        /// Respuesta asincrona a la peticion de asignación.
        /// </summary>
        private void OnAllocateResponseForbidden(GearOperationStatus response)
        {
            AllocateResponse(response, GearStatus.Forbidden);
        }

        #endregion

        #region Deallocate

        /// <summary>
        /// Lanza la llamada para Configurar el nodo como NO ASIGNADO, pero con el estado deseado (willingStatus), que puede ser diferente segun el punto de origen.
        /// </summary>
        /// <param name="willingStatus">Representa el estado deseado despues de llamar a la desasignación del nodo.</param>
        public virtual void Deallocate(GearStatus willingStatus)
        {
            _semaphore.WaitOne();

            // Limpiamos los valores.

            this.Frecuency = String.Empty;
            if (this.IsSlave)
            {
                this.Priority = null;

                this.Offset = GearCarrierOffStatus.Off;
                this.Channeling = GearChannelSpacings.ChannelSpacingsDefault;
                this.Modulation = GearModulations.AM;
                this.PowerLevel = GearPowerLevels.PowerLevelsDefault;
                this.Power = 0;

                InitializeDefaultValues();
            }

            // Desasignacion.
            ClearReplacements();
            Status = willingStatus;
            RemoteControl.ConfigureNode(RCConfigurationAction.Unassing, DeallocateResponse, this, this.IsEmitter, this.IsMaster);            
        }

        private void DeallocateResponse(GearOperationStatus response)
        {
            switch (response)
            {
                case GearOperationStatus.OK:
                case GearOperationStatus.Rejected:
                    // Sin mas cambios. Hemos puesto en Deallocate el estado que tenia que tener.
                    break;

                case GearOperationStatus.Timeout:
                case GearOperationStatus.Fail:
                    Status = GearStatus.Fail;
                    break;
            }
            _semaphore.Release();
            OnGearDeallocated.Invoke(this, response);
        }

        #endregion

        #region Check
        private bool isChecking = false;
        public virtual void Check()
        {
            if (isChecking)
                return;
            isChecking = true;

            _semaphore.WaitOne();
            RemoteControl.CheckNode(CheckResponse, this);
        }
        private void CheckResponse(GearOperationStatus response)
        {
            _semaphore.Release();
            isChecking = false;
            OnGearChecked.Invoke(this, response);
        }

        #endregion

        #region Helpers

        private void ClearReplacements()
        {
            if (null != this.ReplaceTo)
                this.ReplaceTo.ReplaceBy = null;
            this.ReplaceTo = null;
        }
        private void SetReplacements(BaseGear gearToReplace)
        {
            if (null != gearToReplace.ReplaceBy)
                gearToReplace.ReplaceBy.ReplaceTo = null;
            gearToReplace.ReplaceBy = this;

            if (null != this.ReplaceTo)
                this.ReplaceTo.ReplaceBy = null;
            this.ReplaceTo = gearToReplace;

            if (null != this.ReplaceBy)
                this.ReplaceBy.ReplaceTo = null;
            this.ReplaceBy = null;
        }

        public string ToString(Boolean shortText)
        {
            if (shortText)
            {
                return " Node: " + this.Id
                    + " {IP=" + this.IP + "}"
                    + " {S=" + _status + "}"
                    + " {F=" + this.Frecuency + "}"
#if !DEBUG
                    + " {FT=" + this.FrecuencyType + "}"
                    + " {RT=" + this.ResourceType + "}"
#endif
                    + " {ReB=" + this.ReplaceById + "}"
                    + " {ReT=" + this.ReplaceToId + "}";    
            }
            else
            {
                StringBuilder builder = new StringBuilder(
                    " Node: " + this.Id
                    + " {S=" + _status + "}");
                if (null != this.ReplaceBy)
                    builder.Append(" {RId=" + this.ReplaceBy.Id + "}");
                builder.Append(
                    " {F=" + this.Frecuency + "}"
                    + " {P=" + this.Priority + "}"
                    + " {WF=" + this.WorkingFormat + "}"
                    + " {IP=" + this.IP + "}");
                if (this.WorkingFormat == Tipo_Formato_Trabajo.Reserva)
                    foreach (HfRangoFrecuencias range in FrecuenciesAllowed)
                        builder.Append(" {R=" + range.fmin + "/" + range.fmax + "}");
                builder.Append(" {FM=" + this.FrecuencyMain + "}");
                builder.Append(" {FT=" + this.FrecuencyType + "}");
                builder.Append(" {RT=" + this.ResourceType + "}");
                return builder.ToString();           
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToIssueString(Boolean bFrec)
        {
            return " Equipo: " + this.Id + (bFrec ? (" {F=" + this.Frecuency + "}") : "");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToString(true);
        }
        
        public Boolean Compare(BaseGear input)
        {
            //20180207 #3136
            //Campos de configuración comunes M - N
            if (this.RemoteControlType != input.RemoteControlType)
                return false;

            if (this.IP != input.IP)
                return false;

            if (this.Port != input.Port)
                return false;

            if (this.FrecuencyType != input.FrecuencyType)
                return false;

            if (this.IsEmitter != input.IsEmitter)
                return false;

            if (this.IsReceptor != input.IsReceptor)
                return false;

            if (this.SipUri != input.SipUri)
                return false;

            if (this.Oid != input.Oid)
                return false;

            if (this.WorkingFormat != input.WorkingFormat)
                return false;

            if (this.ChannelType != input.ChannelType)
                return false;

            if (this.IdEmplazamiento != input.IdEmplazamiento)
                return false;

            //20180207 #3136
            //Campos específicos equipo Maestro
            if (this.IsMaster && this.FrecuencyMain != input.FrecuencyMain)
                return false;

            if (this.IsMaster && this.FrecuencyKey != input.FrecuencyKey)
                return false;

            if (this.IsMaster && this.FrecuencyMainFormat != input.FrecuencyMainFormat)
                return false;

            if (this.IsMaster && this.Priority != input.Priority)
                return false;

            if (this.IsMaster && this.Channeling != input.Channeling)
                return false;

            if (this.IsMaster && this.Modulation != input.Modulation)
                return false;

            //20180207 #3136
            //Campos específicos equipo Maestro Transmisor
            if (this.IsMaster && this.IsEmitter && this.Offset != input.Offset)
                return false;

            //20180207 #3231 En esta versión se obtiene del equipo Master, no de configuración.
            //if (this.IsMaster && this.IsEmitter && this.PowerLevel != input.PowerLevel)
            //    return false;
            //if (this.IsMaster && this.IsEmitter && this.Power != input.Power)
            //    return false;
            //20180207 #3231 FIN


            //20180207 #3136
            //Campos específicos equipo Reserva (N)        
            // Frecuencias permitidas.
            if (!this.IsMaster && (null != this.FrecuenciesAllowed || null != input.FrecuenciesAllowed))
            {
                if (null == this.FrecuenciesAllowed && null != input.FrecuenciesAllowed)
                    return false;
                if (null != this.FrecuenciesAllowed && null == input.FrecuenciesAllowed)
                    return false;

                if (this.FrecuenciesAllowed.Count != input.FrecuenciesAllowed.Count)
                    return false;

                for (Int32 count = 0; count < this.FrecuenciesAllowed.Count; count++)
                {
                    HfRangoFrecuencias rangoThis = this.FrecuenciesAllowed[count];
                    HfRangoFrecuencias rangoInput = input.FrecuenciesAllowed[count];

                    if (rangoThis.fmax != rangoInput.fmax || rangoThis.fmin != rangoInput.fmin)
                        return false;
                }
            }

            return true;
        }		
		
 /*       
        public Boolean Compare(BaseGear input)
        {                                    
            if (this.FrecuencyMain != input.FrecuencyMain)
                return false;
            if (this.FrecuencyKey != input.FrecuencyKey)
                return false;
            if (this.FrecuencyMainFormat != input.FrecuencyMainFormat)
                return false;
            if (this.FrecuencyType != input.FrecuencyType)
                return false;
			
            if (this.IdEmplazamiento != input.IdEmplazamiento)
                return false;
			
            if (this.IsMaster && this.Priority != input.Priority)
                return false;
            
            if (this.IP != input.IP)
                return false;
            if (this.Port != input.Port)
                return false;

            if (this.IsEmitter != input.IsEmitter)
                return false;
            if (this.IsReceptor != input.IsReceptor)
                return false;

            if (this.SipUri != input.SipUri)
                return false;
            if (this.Oid != input.Oid)
                return false;

            if (this.WorkingFormat != input.WorkingFormat)
                return false;
            if (this.ChannelType != input.ChannelType)
                return false;

            if (this.PowerLevel != input.PowerLevel)
                return false;
            //20180320 JOI
            //if (this.Power != input.Power)
              //  return false;
            if (this.Channeling != input.Channeling)
                return false;
            if (this.Modulation != input.Modulation)
                return false;
            if (this.Offset != input.Offset)
                return false;
            
            if (this.RemoteControlType != input.RemoteControlType)
                return false;
            
            // Frecuencias permitidas.
            if (null != this.FrecuenciesAllowed || null != input.FrecuenciesAllowed)
            {
                if (null == this.FrecuenciesAllowed && null != input.FrecuenciesAllowed)
                    return false;
                if (null != this.FrecuenciesAllowed && null == input.FrecuenciesAllowed)
                    return false;

                if (this.FrecuenciesAllowed.Count != input.FrecuenciesAllowed.Count)
                    return false;

                for (Int32 count = 0; count < this.FrecuenciesAllowed.Count; count++)
                {
                    HfRangoFrecuencias rangoThis = this.FrecuenciesAllowed[count];
                    HfRangoFrecuencias rangoInput = input.FrecuenciesAllowed[count];

                    if (rangoThis.fmax != rangoInput.fmax || rangoThis.fmin != rangoInput.fmin)
                        return false;
                }
            }

            return true;
        }
        */
        #endregion

    }

}

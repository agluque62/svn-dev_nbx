using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Translate;
using U5ki.Infrastructure;
using Utilities;

namespace U5ki.RdService
{
    /// <summary>
    /// 
    /// </summary>
    public class RdFrecuency : BaseCode, IDisposable
    {
        private readonly int TIME_DELAY_TO_RTX = U5ki.RdService.Properties.Settings.Default.RtxDelay;
        private readonly int TIME_DELAY_TO_DISABLE_FREQUENCY = U5ki.RdService.Properties.Settings.Default.FrequencyDisabled * 1000;

        /************************************************************************/
        /** 201702-FD. AGL. Nuevos Atributos de Configuracion y Estado. *********/
        //public enum FREQUENCY_STATUS { NotAvailable = 0, Available = 1, Degraded = 2 }
        public class NewRdFrequencyParams
        {
            //public string Zona { get; set; }
            public int Cld_supervision_time { get; set; }
            public CORESIP_Priority Priority { get; set; }
            public CORESIP_FREQUENCY_TYPE FrequencyType { get; set; }
            public CORESIP_CLD_CALCULATE_METHOD CLDCalculateMethod { get; set; }
            public int BssWindows { get; set; }
            public bool AudioSync { get; set; }
            public bool AudioInBssWindow { get; set; }
            public bool NotUnassignable { get; set; }
            public int MetodosBssOfrecidos { get; set; }
            public uint PorcentajeRSSI { get; set; }

            public NewRdFrequencyParams()
            {
                //Zona = "##ZONA##";
                Priority = CORESIP_Priority.CORESIP_PR_NORMAL;
                FrequencyType = CORESIP_FREQUENCY_TYPE.Simple;
                CLDCalculateMethod = CORESIP_CLD_CALCULATE_METHOD.Relative;
                BssWindows = 200;
                AudioSync = false;
                AudioInBssWindow = true;
                NotUnassignable = false;
                Cld_supervision_time = 1000;
                MetodosBssOfrecidos = (int)RdResource.BssMethods.Ninguno;
                PorcentajeRSSI = 0;
            }
        }
        private NewRdFrequencyParams new_params = new NewRdFrequencyParams();
        public NewRdFrequencyParams GetParam
        {
            get { return this.new_params; }
        }
        private RdSrvFrRs.FrequencyStatusType StatusCheck;
        private RdSrvFrRs.FrequencyStatusType _OldStatus;
        private RdSrvFrRs.FrequencyStatusType _Status;
        public RdSrvFrRs.FrequencyStatusType Status
        {
            get
            {
                int Tx = _RdRs.Values.Where(r => r.isTx).Count();
                int Rx = _RdRs.Values.Where(r => r.isRx).Count();
                int TxConn = _RdRs.Values.Where(r => r.isTx && r.Connected).Count();
                int RxConn = _RdRs.Values.Where(r => r.isRx && r.Connected).Count();

                _OldStatus = _Status;

                switch (new_params.FrequencyType)
                {
                    case CORESIP_FREQUENCY_TYPE.Simple:
                        if (TipoDeFrecuencia == "HF")
                        {
                            if (TxConn != 0 || RxConn != 0)
                            {
                                _Status = RdSrvFrRs.FrequencyStatusType.Available;
                                SendLogNewStatus(_OldStatus);
                                return _Status;
                            }
                        }
                        else
                        {
                            if (Tx == 1 && Rx == 1)
                            {
                                if (TxConn == 1 && RxConn == 1)
                                {
                                    _Status = RdSrvFrRs.FrequencyStatusType.Available;
                                    SendLogNewStatus(_OldStatus);
                                    return _Status;
                                }
                            }
                            //Caso de frecuencias sólo RX
                            else if ((Rx == 1) && (Tx == 0) && (RxConn == 1))
                            {
                                _Status = RdSrvFrRs.FrequencyStatusType.Available;
                                SendLogNewStatus(_OldStatus);
                                return _Status;
                            }
                        }
                        break;

                    case CORESIP_FREQUENCY_TYPE.FD:
                        /** 20180626. Incidencia #3617. Una FD puede tener un solo TX */
                        if (Tx >= 1 && Tx <= 5)
                        {
                            if (TxConn > 0 && RxConn > 0)
                            {
                                if (TxConn < Tx || RxConn < Rx)
                                {
                                    _Status = RdSrvFrRs.FrequencyStatusType.Degraded;
                                    SendLogNewStatus(_OldStatus);
                                    return _Status;
                                    /*
                                    bool sameSite = false;
                                    // TODO. Meter el control de Emplazamientos.
                                    // JCAM. 27/02/2017
                                    foreach (RdResource rs in _RdRs.Values)
                                    {
                                        if (rs.Type == RdRsType.Tx)
                                            // El recurso seleccionado es un Tx, buscamos un Rx conectado en el mismo emplazameinto
                                            sameSite |= _RdRs.Values.Where(r => ((r.Type == RdRsType.Rx) && (r.Connected) && (r.Site == rs.Site))).Count() > 0;
                                        else if (rs.Type == RdRsType.Rx)
                                            // El recurso seleccionado es un Tx, buscamos un Tx conectado en el mismo emplazameinto
                                            sameSite |= _RdRs.Values.Where(r => ((r.Type == RdRsType.Tx) && (r.Connected) && (r.Site == rs.Site))).Count() > 0;
                                    }

                                    RdSrvFrRs.FrequencyStatusType st = sameSite ? RdSrvFrRs.FrequencyStatusType.Degraded : RdSrvFrRs.FrequencyStatusType.NotAvailable;
                                    _Status = st;
                                    if (oldStatus != _Status)
                                        LogInfo<RdService>("FD Status. Frequency ID: " + this.Frecuency + ". Status: " + RdSrvFrRs.FrequencyStatusType.NotAvailable,
                                                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", "FD Status. Frequency ID: " + this.Frecuency + ". Status: " + RdSrvFrRs.FrequencyStatusType.NotAvailable);

                                    return st;
                                    */
                                }
                                _Status = RdSrvFrRs.FrequencyStatusType.Available;
                                SendLogNewStatus(_OldStatus);
                                return _Status;
                            }
                        }

                        _Status = RdSrvFrRs.FrequencyStatusType.NotAvailable;
                        SendLogNewStatus(_OldStatus);
                        return _Status;

                    case CORESIP_FREQUENCY_TYPE.ME:
                    case CORESIP_FREQUENCY_TYPE.Dual:
                        LogInfo<RdService>("FD Status. Frequency ID: " + this.Frecuency + ". Status: " + RdSrvFrRs.FrequencyStatusType.NotAvailable,
                                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR, "RdService",
                                CTranslate.translateResource("Frequency type not implemented. Frequency ID: " + this.Frecuency + ". Type: " +
                                    new_params.FrequencyType.ToString()));
                        break;
                    default:
                        LogInfo<RdService>("FD Status. Frequency ID: " + this.Frecuency + ". Status: " + RdSrvFrRs.FrequencyStatusType.NotAvailable,
                                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR, "RdService",
                                CTranslate.translateResource("Unknow frequency type. Frequency ID: " + this.Frecuency + ". Type: " +
                                    new_params.FrequencyType.ToString()));
                        break;
                }

                _Status = RdSrvFrRs.FrequencyStatusType.NotAvailable;
                return RdSrvFrRs.FrequencyStatusType.NotAvailable;
            }
        }
        /************************************************************************/

        ///Semaforo usado para realizar el tratamiento de ptt en concurrencia
        ///Evita que los cambios concurrentes de _CurrentSrcPtt y _SrcPtts, dejen el sistema incoherente,
        ///por ejemplo Reset y NextPtt se ejecutan en hilos diferentes. Visto en Incidencia #3014
        private ManagedSemaphore _CurrentPttSemaphore = new ManagedSemaphore(1, 1, "CurrentPttSemaphore");

        /// <summary>
        /// 
        /// </summary>
        public event GenericEventHandler<RdFrecuency> TimerElapsed;

        /// <summary>
        /// Guarda el valor configurado para cada frecuencia desde el CfgService
        /// Sirve para gestionar el atributo txSeleccionado de sus recursos
        /// </summary>
        private Tipo_ModoTransmision _ModoTransmision = Tipo_ModoTransmision.Ninguno;
        /// <summary>
        /// Guarda el tx con el emplazamiento configurado por defecto
        /// </summary> 
        private string _TxIDDefault = null;
        /// Guarda el tiempo configurado para ir a emplazamiento por defecto para tx, 0 significa que no tiene efecto
        private int _TimeToTxDefault = 0;
        ///Timer para Tx por el emplazamiento de defecto en caso de inactividad de la FD
        private Timer _TimerTxDefault = null;
        ///Para optimizar busquedas se guarda el recurso seleccionado
        private string _TxIDSelected = null;
        ///Guarda el ultimo emplazamiento seleccionado bien por SQ, bien por selección del de defecto. 
        ///Se usa para elegir el _TxRsSelected en la funcion de evaluacion
        private String _LastSelectedSite = "";
        /// <summary>
        /// 
        /// </summary>
        public string Frecuency
        {
            get { return _Frecuency; }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool SupervisionPortadora { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PttSrc
        {
            get { return _CurrentSrcPtt != null ? _CurrentSrcPtt.SrcId : string.Empty; }
        }

        private Dictionary<string, string> _Picts;
        public Dictionary<string, string> Picts
        {
            get
            {
                return _Picts;
            }
            set
            {
                _Picts = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
        public RdFrecuency(string fr)
        {
            _Frecuency = fr;

            _RtxSquelchTimer = new Timer(TIME_DELAY_TO_RTX)
            {
                AutoReset = false
            };
            _RtxSquelchTimer.Elapsed += OnRtxSquelchElapsed;

            if (TIME_DELAY_TO_DISABLE_FREQUENCY > 0)
            {
                _DisableFrequencyTimer = new Timer(TIME_DELAY_TO_DISABLE_FREQUENCY)
                {
                    AutoReset = false,
                    Enabled = false
                };
                _DisableFrequencyTimer.Elapsed += OnDisableFrequencyElapsed;
            }
            _TimerTxDefault = new Timer
            {
                AutoReset = false,
                Enabled = false
            };
            _TimerTxDefault.Elapsed += OnTxDefaultElapsed;
        }

        #region IDisposable Members
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _DisableFrequencyTimer.Elapsed -= OnDisableFrequencyElapsed;

            foreach (IRdResource rdRs in _RdRs.Values)
            {
                rdRs.Dispose();
            }

            Reset(false);

            if (_TimerTxDefault != null)
                _TimerTxDefault.Dispose();
            if (_WaitingForSuperviser != null)
                _WaitingForSuperviser.Dispose();
            if (_RtxSquelchTimer != null)
                _RtxSquelchTimer.Dispose();
            if (_DisableFrequencyTimer != null)
                _DisableFrequencyTimer.Dispose();            
        }

        #endregion

        /// <summary>
        /// Método para devolver el recurso tx de defecto 
        /// </summary>
        private IRdResource GetTxRsDefault()
        {
            if (_TxIDDefault != null)
            {
                foreach (IRdResource res in _RdRs.Values)
                    if (res.ID.Equals(_TxIDDefault, StringComparison.CurrentCultureIgnoreCase))
                        return res;
            }
            return null;
        }

        /// <summary>
        /// Método para devolver el recurso tx seleccionado 
        /// </summary>
        private IRdResource GetTxRsSelected()
        {
            if (_TxIDSelected != null)
            {
                foreach (IRdResource res in _RdRs.Values)
                    if (res.ID.Equals(_TxIDSelected, StringComparison.CurrentCultureIgnoreCase))
                        return res;
            }
            return null;
        }
        /// <summary>
        /// Genera los recursos radio (canales físicos) asociados a la frecuencia
        /// </summary>
        public void Reset(ConfiguracionSistema sysCfg, CfgEnlaceExterno cfg, Dictionary<string, bool> selectedRs)
        {
            Debug.Assert(cfg.Literal == _Frecuency);
            
            // Actualización de los nuevos parámetros de la frecuencia a partir de los recibidos en cfg.
            bool hayCambiosEnFrecuencia = ResetNewParams(cfg);

            Dictionary<string, IRdResource> rdRsToRemove = new Dictionary<string, IRdResource>(_RdRs);
            _RdRs.Clear();

            //Creación de parejas de redundancia 1 + 1, si las hay
            List<RdResourcePair> pairs = new List<RdResourcePair>();
            foreach (CfgRecursoEnlaceExterno rsCfg in cfg.ListaRecursos.Where(x => String.IsNullOrEmpty(x.RedundanciaIdPareja) == false))
            {
                if (pairs.FindIndex(x => x.ID.Equals(rsCfg.RedundanciaIdPareja)) == -1)
                    pairs.Add(new RdResourcePair(rsCfg.RedundanciaIdPareja));
            }

            //foreach (CfgRecursoEnlaceExterno rsCfg in cfg.ListaRecursos)
            //{
            //    if (selectedRs.ContainsKey(rsCfg.IdRecurso))
            //    {
            //        RdResource resourceFound = null;
            //        RdResourcePair pairFound = null;
            //        bool hayCambiosEnRecurso = false;

            //        foreach (IRdResource iRes in rdRsToRemove.Values)
            //        {
            //            if (iRes.ID.Equals(rsCfg.RedundanciaIdPareja))
            //                pairFound = (RdResourcePair)iRes;
            //            resourceFound = iRes.GetListResources().First(x => x.ID.Equals(rsCfg.IdRecurso));
            //            if (resourceFound != null)
            //            {
            //                hayCambiosEnRecurso = CompareResources(ref resourceFound, rsCfg, hayCambiosEnFrecuencia);
            //                break;
            //            }
            //        }

            //    }
            //}

            try
            {
                foreach (CfgRecursoEnlaceExterno rsCfg in cfg.ListaRecursos)
                {
                    if (selectedRs.ContainsKey(rsCfg.IdRecurso))
                    {
                        RdResource singleRdRs = null;
                        string[] rdUri = RsUri(rsCfg.IdRecurso, sysCfg);

                        if (rdUri.Length == 0)
                        {
                            LogDebug<RdFrecuency>(String.Format("Lista.Reset: " + "Recurso no asociado a un host [Rs={0}]", rsCfg.IdRecurso));
                            continue;
                        }

                        if (rdUri[0] != null)       // AGL 20160208. Cuando hay recursos sin URI no se configuran...
                        {
                            //20180724 #3603
                            string rsKeyOld = "";
                            bool hayCambiosEnSip = false;
                            foreach (IRdResource iRes in rdRsToRemove.Values)
                            {
                                foreach (RdResource resource in iRes.GetListResources())
                                {
                                    if (resource.ID == rsCfg.IdRecurso)
                                        if (resource.Uri1.Equals(rdUri[0]) == false)
                                        {
                                            hayCambiosEnSip = true;
                                            break;
                                        }
                                }
                            }
                            //if (RdRs.TryGetValue(rsCfg.IdRecurso, out IRdResource res))
                            //{
                            //    //if (res.MasterMN)
                            //        if (rdUri[0] != res.Uri1)
                            //        {
                            //            rsKeyOld = res.Uri1;
                            //            hayCambiosEnSip = true;
                            //            break;
                            //        }
                            //}
                            string rsKey;
                            if (hayCambiosEnSip)
                            {
                                rsKey = rsKeyOld.ToUpper() + rsCfg.Tipo;
                            }
                            else
                            {
                                rsKey = rdUri[0].ToUpper() + rsCfg.Tipo;
                            }
                            //20180724  #3603 FIN
                            //string rsKey = rdUri[0].ToUpper() + rsCfg.Tipo;

                            IRdResource existingRdRs;
                            if (rdRsToRemove.TryGetValue(rsKey, out existingRdRs))
                            {
                                rdRsToRemove.Remove(rsKey);
                            }
                            else
                            {
                                //Caso de un N que sustituye a un M, ambos recursos tienen el mismo ID
                                //Mantengo el N, con los nuevos parámetros configurados
                                foreach (KeyValuePair<string, IRdResource> rdRsPair in rdRsToRemove)
                                    if ((rsCfg.IdRecurso == rdRsPair.Value.ID) && rdRsPair.Value.ReplacedMN)
                                    {
                                        rdUri[0] = ((RdResource)rdRsPair.Value).Uri1;
                                        rdUri[1] = ((RdResource)rdRsPair.Value).Uri2;
                                        rdRsToRemove.Remove(rdRsPair.Key);
                                        rsKey = rdRsPair.Key;
                                        singleRdRs = (RdResource)rdRsPair.Value;
                                        break;
                                    }
                            }
                            if (existingRdRs != null)
                            {
                                // JCAM 20170406.
                                // Si hay cambios en los parámetros de la frecuencia, se debe resetear las sesiones establecidas
                                // con los recursos asociados para que esos cambios tengan efecto
                                // if (hayCambiosEnFrecuencia)
                                //20180724  #3603
                                if (hayCambiosEnFrecuencia || hayCambiosEnSip)
                                {
                                    hayCambiosEnSip = false; //#3603
                                    if (existingRdRs.Connected)
                                        RemoveSipCall(singleRdRs);
                                    existingRdRs.Dispose();
                                    bool isReplacedMNTemp = existingRdRs.ReplacedMN;
                                    bool isMasterMNTemp = existingRdRs.MasterMN;
                                    singleRdRs = new RdResource(rsCfg.IdRecurso, rdUri[0], rdUri[1], (RdRsType)rsCfg.Tipo, cfg.Literal, rsCfg.IdEmplazamiento, selectedRs[rsCfg.IdRecurso], new_params, rsCfg);  //EDU 20170223 // JCAM 20170313                            }
                                    singleRdRs.ReplacedMN = isReplacedMNTemp;
                                    singleRdRs.MasterMN = isMasterMNTemp;//#3603
                                }
                                else
                                {
                                    // Hay cambios pero no requieren reinicio de sesion, 
                                    // pero si hay que actualizar los datos:
                                    // -el emplazamiento
                                    existingRdRs.Site = rsCfg.IdEmplazamiento;
                                }
                            }
                            else
                            {
                                singleRdRs = new RdResource(rsCfg.IdRecurso, rdUri[0], rdUri[1], (RdRsType)rsCfg.Tipo, cfg.Literal, rsCfg.IdEmplazamiento, selectedRs[rsCfg.IdRecurso], new_params, rsCfg);  //EDU 20170223 // JCAM 20170313

                                if ((cfg.TipoFrecuencia == Tipo_Frecuencia.FD) && (cfg.ModoTransmision == Tipo_ModoTransmision.UltimoReceptor))
                                    singleRdRs.TxMute = true;
                            }
                            //if (!rdRs.Connected && rdRs.Selected)
                            //    ChangeSite();
                            //Not changed resources
                            if (singleRdRs == null)
                            {
                                if (existingRdRs.GetType().Equals(typeof(RdResourcePair)))
                                {
                                    if (rsCfg.RedundanciaRol.Equals("P"))
                                        _RdRs[rsKey] = existingRdRs;
                                }
                                else
                                    _RdRs[rsKey] = existingRdRs;
                            }
                            //New resources
                            else
                            {
                                if (String.IsNullOrEmpty(rsCfg.RedundanciaIdPareja) == false)
                                {
                                    RdResourcePair rdPair = pairs.Find(x => x.ID.Equals(rsCfg.RedundanciaIdPareja));
                                    if (rdPair != null)
                                    {
                                        if (rsCfg.RedundanciaRol.Equals("P"))
                                        {
                                            rdPair.SetActive(singleRdRs);
                                            _RdRs[rsKey] = rdPair;
                                        }
                                        else if (rsCfg.RedundanciaRol.Equals("R"))
                                            rdPair.SetStandby(singleRdRs);
                                    }
                                    else
                                        LogError<RdFrecuency>(String.Format("Error al configurar un par 1+1 ", rsCfg.IdRecurso));
                                }
                                else
                                    _RdRs[rsKey] = singleRdRs;
                            }
                        }
                    }
                    else
                    {
                        LogDebug<RdFrecuency>(String.Format("Nuevo recurso ", rsCfg.IdRecurso));
                    }
                }

            }
            catch (Exception exc)
            {
                LogError<RdFrecuency>(String.Format("Excepcion ", exc.StackTrace));
            }
            try
            {
                foreach (IRdResource rdRs in rdRsToRemove.Values)
                {
                    if (TipoDeFrecuencia != "HF")
                    {
                        if (rdRs.Connected)
                        {
                            RemoveSipCall(rdRs.SipCallId, rdRs);
                        }
                        rdRs.Dispose();
                    }
                    else    // 20171116. AGL. Si la frecuencia es HF, y el recurso activo es el transmisor, no se borra de la lista de recursos de la frecuencia.
                    {
                        if (rdRs.Connected && rdRs.isTx)
                        {
                            string rsId = rdRs.Uri1.ToUpper() + rdRs.Type/* (uint)rdRs.Type*/;
                            _RdRs[rsId] = rdRs;
                        }
                        else
                        {
                            if (rdRs.Connected)
                            {
                                RemoveSipCall(rdRs.SipCallId, rdRs);
                            }

                            rdRs.Dispose();
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            if (_FrRs != null)
                RdRegistry.Publish(_Frecuency, _FrRs);
            ConfiguraModoTransmision(cfg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topId"></param>
        /// <param name="on"></param>
        /// <param name="responseAddr"></param>
        public void SetRx(string topId, bool on, string responseAddr)
        {
            if (_FrRs != null)
            {
                int index = _RxIds.FindIndex(delegate (string rxId) { return (rxId == topId); });
                if (on)
                {
                    if (index < 0)
                    {
                        _RxIds.Add(topId);
                    }

                    if (responseAddr != null)
                    {
                        RdRegistry.RespondToFrRxChange(responseAddr, _Frecuency, true);
                    }
                }
                else if (index >= 0)
                {
                    SetTx(topId, false, false, CORESIP_PttType.CORESIP_PTT_OFF, null);
                    _RxIds.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Proceso de Asignación en TX de una frecuencia...
        /// </summary>
        /// <param name="topId">Usuario que solicita la asignacion</param>
        /// <param name="on">Asignación o Desasignacion</param>
        /// <param name="checkAlreadyAssigned"></param>
        /// <param name="pttType"></param>
        /// <param name="responseAddr">Direccion a la que 'responder' con el resultado</param>
        public void SetTx(string topId, bool on, bool checkAlreadyAssigned, CORESIP_PttType pttType, string responseAddr)
        {

            if (_FrRs != null)
            {
                int index = _TxIds.FindIndex(delegate (string txId) { return (txId == topId); });
                if (on)
                {
                    bool tx = false;
                    //Si no hay configurados recursos de TX no hago nada
                    if (_RdRs.Values.Where(r => r.isTx).Count() == 0)
                        return;
                    if (index >= 0)
                    {
                        _PttTypes[index] = pttType;
                        tx = true;
                    }
                    else if (!checkAlreadyAssigned || (_TxIds.Count == 0))
                    {
                        SetRx(topId, true, null);

                        _TxIds.Add(topId);
                        _PttTypes.Add(pttType);
                        tx = true;
                    }

                    if (responseAddr != null)
                    {
                        RdRegistry.RespondToFrTxChange(responseAddr, _Frecuency, tx);
                    }
                }
                else if (index >= 0)
                {
                    bool changed = false;
                    PttInfo pttInfo = _SrcPtts.Find(delegate (PttInfo p) { return (p.SrcId == topId); });

                    if (pttInfo != null)
                    {
                        _CurrentPttSemaphore.WaitOne();
                        _SrcPtts.Remove(pttInfo);
                        LogTrace<RdFrecuency>(_Frecuency + " SetTX:Remove " + pttInfo.SrcId + ",srcPtts.Count " + _SrcPtts.Count.ToString());

                        if ((_CurrentSrcPtt != null) && (_CurrentSrcPtt.SrcId == pttInfo.SrcId))
                        {
                            changed = NextPtt(topId);
                        }
                        _CurrentPttSemaphore.Release();
                    }

                    if ((_FrRs.RtxGroupId > 0) && (_FrRs.RtxGroupOwner == topId))
                    {
                        RemoveFromRtxGroup(false);
                        changed = true;
                    }

                    _TxIds.RemoveAt(index);
                    _PttTypes.RemoveAt(index);

                    if (changed)
                    {
                        RdRegistry.Publish(_Frecuency, _FrRs);
                    }
                }
            }
        }

        public void ReceivePtt(string topId, PttSource srcType, IEnumerable<int> srcPorts)
        {
            LogTrace<RdFrecuency>(_Frecuency + " ReceivePtt topID " + topId + ",srcType " + srcType.ToString());
            if (_FrRs != null)
            {

                bool changed = false;
                PttInfo pttInfo = _SrcPtts.Find(delegate (PttInfo p) { return (p.SrcId == topId); });

                if (srcType == PttSource.NoPtt)
                {
                    //Debug.Assert((pttInfo != null) || (_CurrentSrcPtt == null) || (_CurrentSrcPtt.SrcId != srcId));
                    if (pttInfo == null)
                        pttInfo = _CurrentSrcPtt;

                    changed = true;

                    if (pttInfo != null)
                    {
                        _CurrentPttSemaphore.WaitOne();
                        _SrcPtts.Remove(pttInfo);
                        LogTrace<RdFrecuency>(_Frecuency + " ReceivePtt:Remove " + pttInfo.SrcId + ",srcPtts.Count " + _SrcPtts.Count.ToString());

                        if ((_CurrentSrcPtt != null) && (_CurrentSrcPtt.SrcId == pttInfo.SrcId))
                        {
                            changed = NextPtt(topId);
                        }
                        _CurrentPttSemaphore.Release();
                    }
                }
                else if (((srcType != PttSource.Avion) && _TxIds.Contains(topId)) ||
                    ((srcType == PttSource.Avion) && (_FrRs.RtxGroupId > 0) && topId.StartsWith("Rtx_" + _FrRs.RtxGroupId + "_")))
                {
                    CORESIP_PttType pttType = CORESIP_PttType.CORESIP_PTT_COUPLING;
                    if (srcType != PttSource.Avion)
                    {
                        int index = _TxIds.FindIndex(delegate (string tx) { return (tx == topId); });
                        pttType = _PttTypes[index];
                    }
                    Debug.Assert(pttType != CORESIP_PttType.CORESIP_PTT_OFF);

                    _CurrentPttSemaphore.WaitOne();
                    if (pttInfo == null)
                    {
                        pttInfo = new PttInfo(topId);
                        _SrcPtts.Add(pttInfo);
                        LogTrace<RdFrecuency>(_Frecuency + " ReceivePtt:Add " + pttInfo.SrcId + ",srcPtts.Count " + _SrcPtts.Count.ToString());
                    }
                    LogTrace<RdFrecuency>(_Frecuency + " set pttType " + pttType.ToString());

                    if (pttInfo.Reset(srcType, pttType, srcPorts))
                    {
                        // JCAM. 20170316. Ya no todos los PTTs tienen la misma prioridad ...
                        /* Suponemos todos los PTT son del mismo tipo-> No es necesaria una ordenación */
                        // JCAM. 20170316. Ahora sí es necesaria una ordenación

                        LogTrace<RdFrecuency>(_Frecuency + " ReceivePtt:Sort " + ",srcPtts.Count " + _SrcPtts.Count.ToString() + " primero: " + _SrcPtts[0].SrcId);
                        _SrcPtts.Sort(delegate (PttInfo x, PttInfo y)
                        {
                            if (x.Type == CORESIP_PttType.CORESIP_PTT_COUPLING)
                            {
                                return (y.Type == CORESIP_PttType.CORESIP_PTT_COUPLING ? 0 : 1);
                            }
                            else if (y.Type == CORESIP_PttType.CORESIP_PTT_COUPLING)
                            {
                                return -1;
                            }

                            return ((int)y.Type - (int)x.Type);
                        });
                    }

                    if ((_CurrentSrcPtt == null) || (_CurrentSrcPtt.SrcId != _SrcPtts[0].SrcId))
                    {
                        changed = NextPtt(topId);
                    }
                    else if (_CurrentSrcPtt.SrcId == pttInfo.SrcId)
                    {
                        ActualizePtt(pttInfo);
                    }
                    _CurrentPttSemaphore.Release();
                }

                // Aunque no haya cambiado el puesto que ha hecho/quitado el ptt tiene que refrescar
                // el estado ptt de la frecuencia
                if (changed || ((pttInfo != null) && (pttInfo.SrcType != PttSource.Avion)))
                {
                    RdRegistry.Publish(_Frecuency, _FrRs);
                }

            }
        }

        public bool FindHost(string host)
        {
            return _TxIds.FindIndex(delegate (string tx) { return (tx == host); }) < 0 ? false : true;
        }

        public void RetryFailedConnections()
        {
            
            foreach (IRdResource rdRs in _RdRs.Values)
            {
                try
                {
                    foreach (RdResource simpleRes in rdRs.GetListResources())
                        if ((!simpleRes.ToCheck) && !simpleRes.Connecting)
                        {
                            simpleRes.Connect();
                        }

                }
                catch (Exception exc)
                {

                    throw exc;
                }
            }
        }

        public void CheckFrequency()
        {
            //No actualizo el estado de la frecuencia si está habilitado el timer
            //que pretende retrasar la actualización del estado
            if ((_FrRs != null) && !_DisableFrequencyTimer.Enabled)
            {
                try
                {
                    StatusCheck = _FrRs.FrequencyStatus;
                    _FrRs.FrequencyStatus = this.Status;
                    if (_FrRs.FrequencyStatus != StatusCheck)
                    {
                        RdRegistry.Publish(_Frecuency, _FrRs);
                    }
                }
                catch (Exception x)
                {
                    LogException<RdFrecuency>("CheckFrecuency", x, false);
                }
            }
        }

        /// <summary>
        /// AGL. Que es el _PostPtt ???
        /// </summary>
        public void PublishChanges(object timer)
        {
            LogTrace<RdFrecuency>(_Frecuency + " PublishChanges ");
            if (_FrRs != null)
            {
                if (timer == _PostPtt)
                {
                    RdRegistry.EnablePublish(_Frecuency, true);

                    _PostPtt.Enabled = false;
                    _PostPtt.Dispose();
                    _PostPtt = null;

                    if ((_FrRs.RtxGroupId > 0) && !_SendingPttToRtxGroup &&
                        (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch) &&
                        ((_CurrentSrcPtt == null) || (_CurrentSrcPtt.SrcId != _FrRs.PttSrcId)))
                    {
                        SendPttToRtxGroup(true, false);
                    }
                }
            }
        }

        public void Publish()
        {
            RdRegistry.Publish(_Frecuency, _FrRs);
        }
        /// <summary>
        /// 20170126. AGL. Retorno el RdResource, para poder identificar al recurso en los historicos de Sesiones SIP.
        /// </summary>
        public bool HandleKaTimeout(int sipCallId, out RdResource rdRsOut)
        {
            foreach (IRdResource rdRs in _RdRs.Values)
            {
                RdResource simpleRes = rdRs.GetSimpleResource(sipCallId);
                if (simpleRes != null)
                {
                    simpleRes.HandleKaTimeout(sipCallId);
                    RemoveSipCall(sipCallId, rdRs);
                    rdRsOut = simpleRes;
                    return true;
                }
            }
            rdRsOut = null;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sipCallId"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        // 20170630. AGL. Propiedades para reflejar ON-LINE la seleccion de emplazamiento en recepcion...
        // 20170704. AGL. La anterior versión era incorrecta.
        public string SelectedSite
        {
            get
            {
                switch (this.new_params.FrequencyType)
                {
                    case CORESIP_FREQUENCY_TYPE.Simple:
                    case CORESIP_FREQUENCY_TYPE.FD:
                        foreach (IRdResource rdr in RdRs.Values.Where(r => r.isRx &&
                                    (r.Squelch)))
                        {
                            RdResource simpleResource = rdr.GetRxSelected();
                            if (simpleResource != null)
                                return simpleResource.Site;
                        }
                        break;
                    case CORESIP_FREQUENCY_TYPE.Dual:
                    case CORESIP_FREQUENCY_TYPE.ME:
                        break;

                }
                return "";
            }
        }
        public int SelectedSiteQidx
        {
            get
            {
                switch (this.new_params.FrequencyType)
                {
                    case CORESIP_FREQUENCY_TYPE.Simple:
                    case CORESIP_FREQUENCY_TYPE.FD:
                        foreach (IRdResource rdr in RdRs.Values.Where(r => r.isRx &&
                                                            (r.Squelch)))
                        {
                            RdResource simpleResource = rdr.GetRxSelected();
                            if (simpleResource != null)
                                return simpleResource.new_params.rx_qidx;
                        }
                        break;
                    case CORESIP_FREQUENCY_TYPE.Dual:
                    case CORESIP_FREQUENCY_TYPE.ME:
                        break;

                }
                return 0;
            }
        }
        /****************************************************************/
        public bool HandleRdInfo(int sipCallId, CORESIP_RdInfo info)
        {
            bool changed = false;
            bool sendToHMI = false;
            foreach (IRdResource rdRs in _RdRs.Values)
            {
                RdResource simpleRes = rdRs.GetSimpleResource(sipCallId);
                if (simpleRes != null)
                {
                    LogTrace<RdFrecuency>("Estado BSS recibido desde coresip. " + "" +
                                            "Recurso: " + rdRs.ID +
                                            ". PTT: " + info.PttType +
                                            ". PttId: " + info.PttId +
                                            ". Squelch: " + info.Squelch +
                                            ". Seleccionado: " + info.rx_selected +
                                            ". info.rx_qidx: " + info.rx_qidx +
                                            ". en Site: " + rdRs.Site);

                    if (simpleRes.HandleRdInfo(info) && (_FrRs != null))
                    {
                        bool confirmaPortadora = SupervisionPortadora;

                        if (rdRs.isTx)
                        {
                            string oldPttSrcId = _FrRs.PttSrcId;
                            if (rdRs.Ptt == RdRsPttType.OwnedPtt)
                            {
                                if (_CurrentSrcPtt != null)
                                {
                                    if (confirmaPortadora && _FrRs.Squelch == RdSrvFrRs.SquelchType.NoSquelch && _WaitingForSuperviser == null)
                                    {
                                        _WaitingForSuperviser = new Timer(U5ki.RdService.Properties.Settings.Default.MonitorCarrierTimeOut);
                                        _WaitingForSuperviser.Elapsed += OnTimerSuperviser;
                                        _WaitingForSuperviser.AutoReset = false;
                                        _WaitingForSuperviser.Enabled = true;
                                    }

                                    ConfirmaPtt();

                                    /* Funciona */
                                    /*
                                    if (!confirmaPortadora)
                                        ConfirmaPtt();
                                    else
                                    {
                                        // A la espera de confirmar la portadora o confirmar PTT si ya se recibió el squelch
                                        if (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                                            ConfirmaPtt();
                                        else
                                            _FrRs.PttSrcId = "";
                                    }
                                    */
                                }
                            }
                            else if (rdRs.Ptt == RdRsPttType.NoPtt)
                            {
                                //if (_FrRs.PttSrcId != null && _FrRs.RtxGroupId > 0)
                                //{
                                //    ReceivePtt("Rtx_" + _FrRs.RtxGroupId + "_" + _Frecuency, PttSource.NoPtt, null);
                                //}

                                if (_WaitingForSuperviser != null)
                                {
                                    _WaitingForSuperviser.Enabled = false;
                                    _WaitingForSuperviser.Dispose();
                                    _WaitingForSuperviser = null;
                                }

                                _FrRs.PttSrcId = null;
                            }
                            else if (rdRs.Ptt == RdRsPttType.ExternalPtt)
                            {
                                _FrRs.PttSrcId = _Frecuency + "ExternalPtt";

                                if (_WaitingForSuperviser != null)
                                {
                                    _WaitingForSuperviser.Enabled = false;
                                    _WaitingForSuperviser.Dispose();
                                    _WaitingForSuperviser = null;
                                }
                            }

                            changed = sendToHMI = (oldPttSrcId != _FrRs.PttSrcId);
                        }

                        if (rdRs.isRx)
                        {
                            RdSrvFrRs.SquelchType oldSquelch = _FrRs.Squelch;

                            if (confirmaPortadora)
                            {
                                if (rdRs.Squelch)
                                {
                                    // PttId recibido y el PttID registrado de la sesión son distintos.
                                    // Puede ser un ExternalPtt provocado por la confirmación de portadora.
                                    // Buscar un rdRs en _SipTxCall un rdRs_Tx con PttId==rdRs.PttId
                                    // Si lo encuentra se trata de una confirmación de portadora del recurso rdRs_Tx
                                    // Si no lo encuentra sería un ExternalPtt
                                    //bool confirmaPortadora = ConfirmaPortadora(rdRs.PttId);
                                    if (rdRs.Ptt == RdRsPttType.OwnedPtt)
                                    {
                                        // Confirmada portadora
                                        if (_CurrentSrcPtt != null)
                                        {
                                            if (_WaitingForSuperviser != null)
                                            {
                                                _WaitingForSuperviser.Enabled = false;
                                                _WaitingForSuperviser.Dispose();
                                                _WaitingForSuperviser = null;
                                            }

                                            ConfirmaPtt();

                                            /* Funciona */
                                            //ConfirmaPtt();
                                            /* */
                                        }
                                    }
                                    else
                                    {
                                        _FrRs.PttSrcId = _Frecuency + "ExternalPtt";

                                        foreach (RdResource rdRs_Tx in SipTxCalls().Values)
                                        {
                                            if (rdRs_Tx.PttId == rdRs.PttId)
                                            {
                                                // Confirmada portadora
                                                if (_CurrentSrcPtt != null)
                                                {
                                                    if (_WaitingForSuperviser != null)
                                                    {
                                                        _WaitingForSuperviser.Enabled = false;
                                                        _WaitingForSuperviser.Dispose();
                                                        _WaitingForSuperviser = null;
                                                    }

                                                    ConfirmaPtt();

                                                    /* Funciona */
                                                    //ConfirmaPtt();
                                                    /* */
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch && _WaitingForSuperviser == null)
                                {
                                    _WaitingForSuperviser = new Timer(U5ki.RdService.Properties.Settings.Default.MonitorCarrierTimeOut);
                                    _WaitingForSuperviser.Elapsed += OnTimerSuperviser;
                                    _WaitingForSuperviser.AutoReset = false;
                                    _WaitingForSuperviser.Enabled = true;
                                }
                            }

                            // JCAM. El estado de squelch de la frecuencia sólo cambia
                            // si ha cambiado el estado del recurso seleccionado en rx.
                            if (simpleRes.new_params.rx_selected)
                            {
                                _FrRs.SqSite = rdRs.Site;

                                // JCAM.
                                // 27/02/2017. U5KI2.RQ.007.07
                                if (this.new_params.FrequencyType == CORESIP_FREQUENCY_TYPE.FD)
                                {
                                    _FrRs.FrequencyStatus = this.Status;
                                    if (_FrRs.FrequencyStatus != _OldStatus)
                                        sendToHMI = true;
                                    _FrRs.ResourceId = rdRs.ID;
                                    _FrRs.QidxMethod = new_params.MetodosBssOfrecidos.ToString();
                                    _FrRs.QidxValue = (UInt32)simpleRes.new_params.rx_qidx;
                                }


                                if (rdRs.Squelch)
                                {
                                    //Este caso es de SQ no provocado por PTT propio (avion u otro SCV).
                                    if ((_CurrentSrcPtt == null) && (info.PttId == 0))
                                    {
                                        _LastSelectedSite = rdRs.Site;
                                        EvaluaTxMute();
                                        StartTimerTxDefault();
                                    }
                                    _FrRs.Squelch = RdSrvFrRs.SquelchType.SquelchOnlyPort;
                                }
                                else
                                {
                                    _FrRs.Squelch = RdSrvFrRs.SquelchType.NoSquelch;
                                    if ((_CurrentSrcPtt == null) && (_PostPtt != null) &&
                                        (_FrRs.Squelch == RdSrvFrRs.SquelchType.NoSquelch))
                                    {
                                        RdRegistry.EnablePublish(_Frecuency, false);

                                        _PostPtt.Enabled = false;
                                        _PostPtt.Dispose();
                                        _PostPtt = null;
                                    }
                                }

                                //changed |= ((oldSquelch != _FrRs.Squelch) && rdRs.new_params.rx_selected);
                                sendToHMI |= (oldSquelch != _FrRs.Squelch);
                                changed |= simpleRes.new_params.rx_selected;
                            }
                        }
                    }
                    //Se ha separado la condicion 'change' de la condicion de entrada inicial
                    if (sendToHMI)
                    {
                        LogTrace<RdFrecuency>("Estado BSS enviado al HMI:" +
                                                ", resource ID: " + rdRs.ID +
                                                ", qidx value: " + _FrRs.QidxValue +
                                                ", qidx method: " + _FrRs.QidxMethod +
                                                ", Ptt: " + rdRs.Ptt +
                                                ", Squelch: " + rdRs.Squelch);
                        RdRegistry.Publish(_Frecuency, _FrRs);
                    }
                    if (changed)
                    {
                        if (_FrRs.RtxGroupId > 0)
                        {
                            LogTrace<RdFrecuency>("Evento en Grupo RTX (" + _FrRs.RtxGroupOwner + "): " +
                                " _SendingPttToRtxGroup: " + _SendingPttToRtxGroup +
                                ", _FrRs.Squelch: " + _FrRs.Squelch +
                                ", _FrRs.PttSrcId: " + _FrRs.PttSrcId +
                                ", _CurrentSrcPtt null ?: " + (_CurrentSrcPtt == null) +
                                ", _CurrentSrcPtt.SrcId: " + ((_CurrentSrcPtt == null) ? "null" : _CurrentSrcPtt.SrcId) +
                                ", _PostPtt null?: " + (_PostPtt == null).ToString()
                                );
                            if (!_SendingPttToRtxGroup && (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch) &&
                                    ((_CurrentSrcPtt == null) || (_CurrentSrcPtt.SrcId != _FrRs.PttSrcId)) &&
                                    (_PostPtt == null))
                            {
                                // La retransmisión del Squelch se temporiza para evitar oscilaciones de coupling
                                LogTrace<RdFrecuency>("Evento en Grupo RTX: Activando TIMER...");
                                _RtxSquelchTimer.Enabled = true;
                            }
                            else if (_SendingPttToRtxGroup && ((_FrRs.Squelch == RdSrvFrRs.SquelchType.NoSquelch) ||
                                ((_CurrentSrcPtt != null) && (_CurrentSrcPtt.SrcId == _FrRs.PttSrcId))))
                            {
                                if (_RtxSquelchTimer != null)
                                {
                                    _RtxSquelchTimer.Enabled = false;
                                    LogTrace<RdFrecuency>("Evento en Grupo RTX (" + _FrRs.RtxGroupOwner + "): Para timer _RtxSquelch porque ptt off y sq off");
                                }

                                SendPttToRtxGroup(false, false);
                            }
                            else
                            {
                                LogTrace<RdFrecuency>("Evento en Grupo RTX (" + _FrRs.RtxGroupOwner + "): Evento en Grupo RTX: Ignorado...");
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Searchs in all its pair resources if the toogle towards active is for them or not.
        /// </summary>
        /// <param name="IdResource"></param>
        /// <returns>false if the resource does not belongs to the frequency </returns>
        public bool ActivateResource (string IdResource)
        {
            foreach (IRdResource res in _RdRs.Values)
                if (res.GetType().Equals(typeof(RdResourcePair)))
                    if (res.ActivateResource(IdResource))
                        return true;
            return false;
        }
        /// <summary>
        /// Temporizador que se utiliza para transmitir por el emplazamiento por defecto en FD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTxDefaultElapsed(object sender, ElapsedEventArgs e)
        {
            RdService.evQueueRd.Enqueue("OnTxDefaultElapsed", delegate ()
            {
                IRdResource TxRsDefault = GetTxRsDefault();
                IRdResource TxRsSelected = GetTxRsSelected();
                if ((TxRsDefault == null) || (TxRsSelected == null))
                    return;
                if (TxRsDefault != null && (TxRsDefault.Connected) && (TxRsDefault != TxRsSelected))
                {
                    TxRsDefault.TxMute = false;
                    TxRsSelected.TxMute = true;
                    //Cambio dinámico del PTT por cambio de TX seleccionado 
                    if (!string.IsNullOrEmpty(PttSrc))
                    {
                        if (TxRsSelected.Connected)
                            TxRsSelected.PttOn(_CurrentSrcPtt.Type);
                        if (TxRsDefault.Connected)
                            TxRsDefault.PttOn(_CurrentSrcPtt.Type);
                        //Esto provoca que salte el aviso acustico de falsa maniobra,
                        //lo comentamos porque se ha elegido de momento el cambio dinámico de TX
                        //    _FrRs.PttSrcId = "ERROR"; ;
                        //    RdRegistry.Publish(_Frecuency, _FrRs);
                    }
                    _TxIDSelected = TxRsDefault.ID;
                    _LastSelectedSite = TxRsDefault.Site;
                    LogDebug<RdFrecuency>(String.Format("Nuevo tx seleccionado por timeout {0}", TxRsSelected.Site));
                }
            });
        }
        /// <summary>
        /// Temporizador que se utiliza para para retrasar el ptt coupling. 
        /// Se utiliza en dos casos:
        /// relacionado con XC1, para evitar oscilaciones en el ptt de retransmisión y
        /// como XC2, tiempo de inhibición de RTX despues de un ptt propio para evitar bloqueos.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRtxSquelchElapsed(object sender, ElapsedEventArgs e)
        {
            RdService.evQueueRd.Enqueue("OnRtxSquelchElapsed", delegate ()
            {
                LogTrace<RdFrecuency>("Evento en Grupo RTX (" + _FrRs.RtxGroupOwner + "): " + "OnRtxSquelchElapsed: " +
                    " _FrRs null?: " + (_FrRs == null) +
                    ", _FrRs.Squelch: " + (_FrRs == null ? "null" : _FrRs.Squelch.ToString())
                    );
                _RtxSquelchTimer.Enabled = false;

                if (_FrRs != null && _FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                {
                    /** 20180719. Incidencias #3668. Solo activo el grupo si al vencer el timer permanece el SQH y no hay PTT */
                    if (_CurrentSrcPtt == null)
                        SendPttToRtxGroup(true, false);
                }
            });
        }

        /// <summary>
        /// 20170126. AGL. Retorno el RdResource, para poder identificar al recurso en los historicos de Sesiones SIP.
        /// Este método se llama cuando hay una conmutación M+N, entre otros escenarios de inicio/fin de sesión.
        /// </summary>
        /// <param name="sipCallId"></param>
        /// <param name="stateInfo"></param>
        /// <returns></returns>
        public bool HandleChangeInCallState(int sipCallId, CORESIP_CallStateInfo stateInfo, out IRdResource rdResOut)
        {
            rdResOut = null;
            try
            {
                foreach (IRdResource rdRs in _RdRs.Values)
                {
                    bool publish = false;
                    bool previousSq = rdRs.Squelch;
                    if (rdRs.HandleChangeInCallState(sipCallId, stateInfo))
                    {
                        if (rdRs.Connected)
                        {
                            publish = AddSipCall(rdRs);
                        }
                        else
                        {
                            RemoveSipCall(sipCallId, rdRs);

                            if (TipoDeFrecuencia == "HF")
                            {
                                RdSrvFrRs frRs = new RdSrvFrRs();
                                frRs = _FrRs;

                                foreach (IRdResource rs in _RdRs.Values.Where(x => (x.isRx && x.Connected)))
                                {
                                    frRs.PttSrcId = "TxHfOff";

                                    //frRs.Squelch = _FrRs.Squelch;
                                    //frRs.RtxGroupId = _FrRs.RtxGroupId;

                                    _PttTypes.Clear();
                                    _TxIds.Clear();

                                    RdRegistry.Publish(_Frecuency, frRs);

                                    frRs.PttSrcId = string.Empty;
                                    break;
                                }
                            }
                            else if (TipoDeFrecuencia == "FD")
                            {
                                RdSrvFrRs.FrequencyStatusType st = this.Status;
                                if ((_FrRs != null) && (_FrRs.FrequencyStatus != st))
                                {
                                    _FrRs.FrequencyStatus = st;

                                    publish = true;
                                }
                                if ((rdRs.isRx) && (_FrRs != null))
                                {
                                    //Quitar Squelch 
                                    if (previousSq && rdRs.GetRxSelected() == null)
                                    {
                                        _FrRs.Squelch = RdSrvFrRs.SquelchType.NoSquelch;
                                        //Buscar si hay uno nuevo que meter el Squelch que no sea el mismo
                                        foreach (IRdResource res in _RdRs.Values.Where(x => x.isRx && x.Connected))
                                        {
                                            foreach (RdResource rdSimpleRes in res.GetListResources().
                                                Where(x => x.Squelch && x.new_params.rx_selected))
                                            {
                                                _FrRs.Squelch = RdSrvFrRs.SquelchType.SquelchOnlyPort;
                                                break;
                                            }
                                        }
                                        publish = true;
                                    }
                                }
                            }
                        }
                        //Hay que actualizar y publicar los datos de estado y squelch con los del 
                        //recurso que se conecta o desconecta
                        //Se protege con este semáforo para evitar la excepcion cuando se borra _FrRs desde otro hilo
                        _CurrentPttSemaphore.WaitOne();
                        if (_FrRs != null)
                            publish |= ActualizaFrecuenciaConRecurso(rdRs);
                        _CurrentPttSemaphore.Release();
                        if (publish == true)
                            RdRegistry.PublishStatusFr(_Frecuency, _FrRs);
                        /** */
                        rdResOut = rdRs;
                        EvaluaTxMute();
                        break;
                    }
                } // foreach end
            }
            catch (Exception x)
            {
                LogManager.GetCurrentClassLogger().Error(" ERROR {0} ejecutando {1}", x.Message, x.StackTrace);
            }
            return (rdResOut != null);
        }

        /// <summary>
        /// Funcion auxiliar que actualiza el objeto de publicación de la frecuencia 
        /// con los datos del recurso. También actualiza el atributo propio Squelch. 
        /// </summary>
        /// <param name="rdRs"> recurso que hay que tomar como origen de los datos</param>
        /// <param name="rtxGroupId"></param>
        /// <param name="wantedRtxGroupRdFr"></param>
        private bool ActualizaFrecuenciaConRecurso(IRdResource rdRs)
        {
            bool hayCambio = false;
            //Actualiza el estado
            if (TipoDeFrecuencia == "FD")
            {
                RdSrvFrRs.FrequencyStatusType st = this.Status;

                if (_FrRs.FrequencyStatus != st)
                {
                    _FrRs.FrequencyStatus = st;

                    LogTrace<RdService>("FD Status. Frequency ID: " + this.Frecuency + ". Status: " + st,
                                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService", CTranslate.translateResource("FD Status. Frequency ID: " + this.Frecuency, st.ToString()));
                    hayCambio = true;
                    LogTrace<RdService>("ActualizaFrecuenciaConRecurso FrRs: " + _FrRs.ResourceId + " cambia:" + hayCambio + " St:" + _FrRs.FrequencyStatus);
                }
            }
            //Actualiza el squelch si es un recurso de rx
            if (rdRs.isRx &&
                (((TipoDeFrecuencia == "FD") && (rdRs.ID == _FrRs.ResourceId)) ||
                   TipoDeFrecuencia != "FD"))
            {
                if (rdRs.Squelch == false && _FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                {
                    _FrRs.Squelch = RdSrvFrRs.SquelchType.NoSquelch;
                    hayCambio = true;
                    LogTrace<RdService>("ActualizaFrecuenciaConRecurso SQ:" + _FrRs.Squelch);
                }
                else if (rdRs.Squelch == true && _FrRs.Squelch == RdSrvFrRs.SquelchType.NoSquelch)
                {
                    hayCambio = true;
                    LogTrace<RdService>("ActualizaFrecuenciaConRecurso SQ:" + _FrRs.Squelch);
                }
            }
            return hayCambio;
        }

        public void RemoveSipCall(IRdResource rdResource)
        {
            RemoveSipCall(rdResource.SipCallId, rdResource);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rtxGroupOwner"></param>
        /// <param name="rtxGroupId"></param>
        /// <param name="wantedRtxGroupRdFr"></param>
        public static void ChangeRtxGroup(string rtxGroupOwner, uint rtxGroupId, List<RdFrecuency> wantedRtxGroupRdFr)
        {
            List<RdFrecuency> rtxGroupRdFr = new List<RdFrecuency>();
            List<RdFrecuency> rdFrToRemove = new List<RdFrecuency>();
            List<RdFrecuency> actualRtxGroupRdFr;

            if (_RtxGroups.TryGetValue(rtxGroupOwner.ToUpper() + rtxGroupId, out actualRtxGroupRdFr))
            {
                rdFrToRemove.AddRange(actualRtxGroupRdFr);
            }

            foreach (RdFrecuency rdFr in wantedRtxGroupRdFr)
            {
                if (rdFrToRemove.Remove(rdFr) || rdFr.CanBeInRtxGroup(rtxGroupId, rtxGroupOwner))
                {
                    rtxGroupRdFr.Add(rdFr);
                }
            }

            if (rtxGroupRdFr.Count > 0)
            {
                if (rtxGroupRdFr.Count == 1)
                {
                    rdFrToRemove.AddRange(rtxGroupRdFr);
                }
                else
                {
                    RdFrecuency frInSquelch = null;
                    foreach (RdFrecuency rdFr in rtxGroupRdFr)
                    {
                        if (rdFr._FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                            frInSquelch = rdFr;
                        else
                            rdFr.AddToRtxGroup(rtxGroupId, rtxGroupOwner);
                    }
                    // La frecuencia que está con squelch será la última que se añada al grupo
                    // para que al enviar el PTT estemos seguros que hay más de una frecuencia en el grupo
                    if (frInSquelch != null)
                    {
                        frInSquelch.AddToRtxGroup(rtxGroupId, rtxGroupOwner);
                    }
                }
            }

            if (rdFrToRemove.Count > 0)
            {
                rdFrToRemove = RemoveFromRtxGroupWithSquelch(rdFrToRemove);
                foreach (RdFrecuency rdFr in rdFrToRemove)
                {
                    rdFr.RemoveFromRtxGroup(true);
                }
            }
        }

        //public bool HandleWG67(IntPtr wg67, CORESIP_WG67Info wg67Info)
        //{
        //    foreach (RdResource rdRs in _RdRs.Values)
        //    {
        //        if (rdRs.WG67Subscription == wg67)
        //        {
        //            rdRs.HandleWG67Info(wg67Info);
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// Compara el recurso que llega de configuración con el que ya exisiste en memoria y 
        /// devuleve el recurso cambiado o el existente.
        /// Si no hay cambios que afecten al sip, permite no cortar la sesion.
        /// </summary>
        /// <param name="exisitingResource"></param>
        /// <param name="rsCfg"></param>
        /// <param name="reinicioSip">true si es necesario un reinicio de sesion por cambios en la frecuencia</param>
        /// <returns>true si el recurso ha cambiado</returns>
        private bool CompareResources(ref RdResource exisitingResource, CfgRecursoEnlaceExterno rsCfg, bool reinicioSip)
        {
            return false;
        }
        private bool ChangeSite()
        {
            bool changed = false;
            string alias = string.Empty;
            string idResource = string.Empty;

            // Buscar los recursos de esta frecuencia.
            foreach (IRdResource rdRs in _RdRs.Values)
            {
                if (rdRs.Connected)
                {
                    changed = rdRs.SelectedSite = true;
                    alias = rdRs.Site;
                    idResource = rdRs.ID;

                    _FrRs.SqSite = rdRs.Site;
                    _FrRs.Squelch = rdRs.Squelch ? RdSrvFrRs.SquelchType.SquelchOnlyPort : RdSrvFrRs.SquelchType.NoSquelch;
                }
                //else
                //    rdRs.Selected = false;
            }

            if (changed)
            {
                foreach (KeyValuePair<string, string> item in Picts)
                {
                    Picts[item.Key] = idResource;
                }
                RdRegistry.Publish(_Frecuency, _FrRs);
                RdRegistry.RespondToChangingSite(null, _Frecuency, alias, 1);
            }

            return changed;
        }

        public bool ChangeSite(string hostId, string frequency, string alias)
        {
            bool changed = false;
            string idResource = string.Empty;
            // Buscar los recursos de esta frecuencia.
            foreach (IRdResource rdRs in _RdRs.Values)
            {
                rdRs.OldSelected = rdRs.SelectedSite;

                if (rdRs.Connected)
                {
                    rdRs.SelectedSite = false;
                    if (frequency == this._Frecuency && rdRs.Site == alias)
                    {
                        changed = rdRs.SelectedSite = rdRs.OldSelected = true;
                        _FrRs.SqSite = rdRs.Site;
                        _FrRs.Squelch = rdRs.Squelch ? RdSrvFrRs.SquelchType.SquelchOnlyPort : RdSrvFrRs.SquelchType.NoSquelch;
                        idResource = rdRs.ID;
                    }
                }
            }

            if (changed)
            {
                Picts[hostId] = idResource;
                RdRegistry.Publish(_Frecuency, _FrRs);
            }
            else
            {
                foreach (IRdResource rdRs in _RdRs.Values)
                {
                    rdRs.SelectedSite = rdRs.OldSelected;
                }
            }

            return changed;
        }

        /// <summary>
        /// Añade recursos a la frecuencia procedentes de la conmutación M+N
        /// El nuevo recurso que entra tiene mute si no reemplaza al seleccionado
        /// o el modo de transmisión no es utlimo recpetor
        /// </summary>
        public void ResourceAdd(String key, RdResource resource, bool isMaster) //#3603
        {
            if ((_ModoTransmision == Tipo_ModoTransmision.UltimoReceptor) && 
                (_TxIDSelected != resource.ID))
                resource.TxMute = true;
            RdRs[key] = (IRdResource)resource;
            resource.ReplacedMN = true;
            resource.MasterMN = isMaster; //#3603
        }

        /** 20180621. AGL. Obtiene el String para MTTO referido al Transmisor seleccionado */
        public string SelectedTxSiteString
        {
            get
            {
                switch (GetParam.FrequencyType)
                {
                    case CORESIP_FREQUENCY_TYPE.Simple:
                        break;
                    case CORESIP_FREQUENCY_TYPE.ME:
                    case CORESIP_FREQUENCY_TYPE.Dual:
                        break;
                    case CORESIP_FREQUENCY_TYPE.FD:
                        switch (_ModoTransmision)
                        {
                            case Tipo_ModoTransmision.Manual:
                            case Tipo_ModoTransmision.Ninguno:
                                break;
                            case Tipo_ModoTransmision.Climax:
                                return "CLX";
                            case Tipo_ModoTransmision.UltimoReceptor:
                                IRdResource TxRsSelected = GetTxRsSelected();
                                return TxRsSelected == null ? "???" : TxRsSelected.Site;
                        }
                        break;
                }
                return "---";
            }
        }

        #region Private Members
        /// <summary>
        /// 
        /// </summary>
        class PttInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public string SrcId;
            /// <summary>
            /// 
            /// </summary>
            public PttSource SrcType = PttSource.NoPtt;
            /// <summary>
            /// 
            /// </summary>
            public CORESIP_PttType Type = CORESIP_PttType.CORESIP_PTT_OFF;
            /// <summary>
            /// 
            /// </summary>
            public List<int> SrcPorts = new List<int>();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="srcId"></param>
            public PttInfo(string srcId)
            {
                SrcId = srcId;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="p"></param>
            public PttInfo(PttInfo p)
            {
                SrcId = p.SrcId;
                SrcType = p.SrcType;
                Type = p.Type;
                SrcPorts.AddRange(p.SrcPorts);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="srcType"></param>
            /// <param name="type"></param>
            /// <param name="srcPorts"></param>
            /// <returns></returns>
            public bool Reset(PttSource srcType, CORESIP_PttType type, IEnumerable<int> srcPorts)
            {
                CORESIP_PttType oldType = Type;

                SrcType = srcType;
                Type = type;

                SrcPorts.Clear();
                SrcPorts.AddRange(srcPorts);

                return (oldType != Type);
            }
        }


        private bool _flag = false;
        /// <summary>
        /// 
        /// </summary>
        private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<string, List<RdFrecuency>> _RtxGroups = new Dictionary<string, List<RdFrecuency>>();
        /// <summary>
        /// 
        /// </summary>
        private string _Frecuency = null;
        /// <summary>
        /// 
        /// </summary>
        private RdSrvFrRs _FrRs = null;
        /// <summary>
        /// 
        /// </summary>
        private PttInfo _CurrentSrcPtt = null;
        /// <summary>
        /// 
        /// </summary>
        private List<PttInfo> _SrcPtts = new List<PttInfo>();
        /// <summary>
        /// 
        /// </summary>
        private List<string> _TxIds = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        private List<string> _RxIds = new List<string>();
        /// <summary>
        /// Tx Active Calls
        /// </summary>
        private Dictionary<int, IRdResource> SipTxCalls()
        {
            Dictionary<int, IRdResource> sipCalls = new Dictionary<int, IRdResource>();
            foreach (IRdResource found in _RdRs.Values.Where(r => r.isTx && r.Connected))
                sipCalls.Add(found.SipCallId, found);
            return sipCalls;
        }
        /// <summary>
        /// Rx Active Calls
        /// </summary>
        private Dictionary<int, IRdResource> SipRxCalls()
        {
            Dictionary<int, IRdResource> sipCalls = new Dictionary<int, IRdResource>();
            foreach (IRdResource found in _RdRs.Values.Where(r => r.isRx && r.Connected))
                sipCalls.Add(found.SipCallId, found);
            return sipCalls;
        }

        /// <summary>
        /// 
        /// </summary>
        private List<CORESIP_PttType> _PttTypes = new List<CORESIP_PttType>();
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, IRdResource> _RdRs = new Dictionary<string, IRdResource>();
        public Dictionary<string, IRdResource> RdRs
        {
            get
            { return _RdRs; }
            set { _RdRs = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        private bool _SendingPttToRtxGroup = false;
        public bool SendingPttToRtxGroup
        {
            get { return _SendingPttToRtxGroup; }
        }
        /// <summary>
        /// 
        /// </summary>
        private Timer _PostPtt = null;
        /// <summary>
        /// 
        /// </summary>
        private Timer _WaitingForSuperviser;

        /// <summary>
        /// Temporizador que se utiliza para para retrasar el ptt coupling. 
        /// Se utiliza en dos casos:
        /// relacionado con XC1, para evitar oscilaciones en el ptt de retransmisión y
        /// como XC2, tiempo de inhibición de RTX despues de un ptt propio para evitar bloqueos.
        /// </summary>
        private Timer _RtxSquelchTimer;

        /// <summary>
        /// 
        /// </summary>
        private Timer _DisableFrequencyTimer;

        /// <summary>
        /// AGL.HF. Contiene el Tipo Frecuencia VHF, UHF o HF
        /// </summary>
        private string _FrecuencyType = "VHF";
        public string TipoDeFrecuencia
        {
            get { return _FrecuencyType; }
            set { _FrecuencyType = value; }
        }

        /// <summary>
        /// AGL.HF. Contiene la frecuencia en kHz
        /// </summary>
        private int _FrecuenciaSintonizada = 2850;
        public int FrecuenciaSintonizada
        {
            get { return _FrecuenciaSintonizada; }
            set { _FrecuenciaSintonizada = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rsId"></param>
        /// <param name="sysCfg"></param>
        /// <returns></returns>
        //private string RsUri(string rsId, ConfiguracionSistema sysCfg)
        //{
        //    string ip = sysCfg.GetGwRsIp(rsId);
        //    return (ip != null) ? string.Format("<sip:{0}@{1}>", rsId, ip) : null;
        //}

        private string[] RsUri(string rsId, ConfiguracionSistema sysCfg)
        {
            string[] rsUri = new string[2];

            string ip1 = sysCfg.GetGwRsIp(rsId, 1);
            string ip2 = sysCfg.GetGwRsIp(rsId, 2);

            rsUri[0] = (ip1 != null) ? string.Format("<sip:{0}@{1}>", rsId, ip1) : null;
            rsUri[1] = (ip2 != null) ? string.Format("<sip:{0}@{1}>", rsId, ip2) : null;

            return rsUri;
        }

        /// <summary>
        /// Esta función se usa cuando se conecta un nuevo recurso a esta frecuencia
        /// </summary>
        /// <param name="rdRs"></param>
        /// <return> true si es necesario publicar el recurso, falso en caso contrario </return>
        private bool AddSipCall(IRdResource rdRs)
        {
            Debug.Assert(rdRs.Connected);
            bool publish = false;

            if (rdRs.isRx)
            {
                if (_SendingPttToRtxGroup)
                {
                    Debug.Assert(_FrRs != null);
                    Debug.Assert(_FrRs.RtxGroupId > 0);

                    SendPttToRtxGroup(true, true);
                }

                //EDU 08/06/2017
                // Buscamos un transmisor del mismo emplazamiento y frecuencia y a este receptor le enviamos el mismo PTT
                if ((_CurrentSrcPtt != null) && (rdRs.Type != RdRsType.RxTx))
                {
                    foreach (IRdResource foundTx in _RdRs.Values.Where(res => (res.isTx &&
                                        res.Connected &&
                                        res.Site == rdRs.Site)))
                    {
                        rdRs.PttOn(_CurrentSrcPtt.Type);
                        break;
                    }
                }
            }
            if (rdRs.isTx)
            {
                if (_CurrentSrcPtt != null)
                {
                    RdMixer.Link(_CurrentSrcPtt.SrcType, _CurrentSrcPtt.SrcPorts, rdRs.SipCallId);
                    rdRs.PttOn(_CurrentSrcPtt.Type);

                    //EDU 08/06/2017
                    // Al receptor del mismo emplazamiento y frecuencia se le envia el mismo PTT type
                    if (rdRs.Type != RdRsType.RxTx)
                    {
                        foreach (IRdResource foundRx in _RdRs.Values.Where(res => (res.isRx &&
                        res.Connected &&
                        res.Site == rdRs.Site)))
                            foundRx.PttOn(_CurrentSrcPtt.Type);
                    }
                }
                IRdResource TxRsDefault = GetTxRsDefault();
                if ((TxRsDefault != null) && (rdRs.Site == TxRsDefault.Site))
                    StartTimerTxDefault();
            }

            if ((_FrRs == null) && HasSIPSession())
            {
                _FrRs = new RdSrvFrRs();
                //Actualizo datos del squelch que están en los recursos asociados de RX
                if (rdRs.Squelch && rdRs.GetRxSelected() != null)
                {
                    _FrRs.Squelch = RdSrvFrRs.SquelchType.SquelchOnlyPort;
                    _FrRs.SqSite = rdRs.Site;
                }
                publish = true;
            }

            // Para abortar el envio temporizado del ASPA
            if (TipoDeFrecuencia != "HF" && HasSIPSession())
            {
                if (_DisableFrequencyTimer != null && _DisableFrequencyTimer.Enabled)
                {
                    _DisableFrequencyTimer.Enabled = false;
                    _flag = false;
                }
            }
            return publish;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sipCallId"></param>
        /// <param name="rsType"></param>
        private void RemoveSipCall(int sipCallId, IRdResource resource)
        {
            if (resource.isRx)
            {
                //_SipRxCalls.Remove(sipCallId);
                //????????????? no se como se hace ahora
                //if (_SipRxCalls.Count == 0 && TipoDeFrecuencia == "HF")
                //    _SipTxCalls.Clear();

                if (_SendingPttToRtxGroup)
                {
                    Debug.Assert(_FrRs != null);
                    Debug.Assert(_FrRs.RtxGroupId > 0);
                    bool squelchInFreq = false;
                    //La RTX se mantiene sólo si aún queda un recurso conectado con SQ
                    squelchInFreq = _RdRs.Values.Where(x => (x.isRx && x.Connected && x.Squelch)).Count() > 0;
                    SendPttToRtxGroup(squelchInFreq, true);
                }
            }
            if (resource.isTx)
            {
                if ((_CurrentSrcPtt != null) && (resource != null))
                {
                    resource.PttOff();
                    if (sipCallId != -1)
                        RdMixer.Unlink(_CurrentSrcPtt.SrcType, _CurrentSrcPtt.SrcPorts, sipCallId);
                    foreach (IRdResource foundRx in RdRs.Values.Where(r => (r.isRx) &&
                            (r.Site == resource.Site)))
                    {
                        foundRx.PttOff();
                    }
                }
                IRdResource TxRsDefault = GetTxRsDefault();
                if ((TxRsDefault != null) && (resource != null) && (resource.Site == TxRsDefault.Site))
                    StopTimerTxDefault();
            }

            /** AGL. 20151022. Reseteo la frecuencia cuando no haya receptores o no haya transmisores. */
            /** AGL. 20160212. Esto solo es válido en frecuencias no HF */
            if (TipoDeFrecuencia == "HF")
            {
                if ((_FrRs != null) && !HasSIPSession() && (SipTxCalls().Count == 0))
                {
                    Reset(false);
                }
            }
            else
            {
                if ((_FrRs != null) && !HasSIPSession())
                {
                    LogDebug<RdFrecuency>("Enviando ASPAS temporizada en " + Frecuency);
                    Reset(true);
                }
            }
        }
        /// <summary>
        /// Devuelve true si está en servicio a nivel SIP (tiene sesion/es).
        /// Para HF y frecuencias con sólo RX, se comprueba que hay al menos una llamada en RX
        /// Para frecuencias con RX y TX, se comprueba que hay llamadas en RX y en TX
        /// </summary>
        /// <returns>true si está en servicio a nivel SIP según su configuración</returns>
        private bool HasSIPSession()
        {
            bool session = false;
            if (TipoDeFrecuencia == "HF")
            {
                if (SipRxCalls().Count > 0)
                    session = true;
            }
            else
            {
                int TxConfig = _RdRs.Values.Where(r => r.isTx).Count();
                if ((SipRxCalls().Count > 0) && ((SipTxCalls().Count > 0) || TxConfig == 0))
                    session = true;
            }
            return session;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rtxGroupId"></param>
        /// <param name="rtxGroupOwner"></param>
        /// <returns></returns>
        private bool CanBeInRtxGroup(uint rtxGroupId, string rtxGroupOwner)
        {
            return ((_FrRs != null) &&
                (((_FrRs.RtxGroupId == 0) && _TxIds.Contains(rtxGroupOwner)) ||
                ((_FrRs.RtxGroupId == rtxGroupId) && (_FrRs.RtxGroupOwner == rtxGroupOwner))));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rtxGroupId"></param>
        /// <param name="rtxGroupOwner"></param>
        private void AddToRtxGroup(uint rtxGroupId, string rtxGroupOwner)
        {
            Debug.Assert(_FrRs != null);
            Debug.Assert((_FrRs.RtxGroupId == 0) || ((_FrRs.RtxGroupId == rtxGroupId) && (_FrRs.RtxGroupOwner == rtxGroupOwner)));
            LogTrace<RdFrecuency>(_Frecuency + " AddToRtxGroup id " + rtxGroupId.ToString());

            if (_FrRs.RtxGroupId == 0)
            {
                _FrRs.RtxGroupId = rtxGroupId;
                _FrRs.RtxGroupOwner = rtxGroupOwner;

                List<RdFrecuency> rtxGroupRdFr;
                string owner = rtxGroupOwner.ToUpper() + rtxGroupId;

                if (!_RtxGroups.TryGetValue(owner, out rtxGroupRdFr))
                {
                    rtxGroupRdFr = new List<RdFrecuency>();
                    _RtxGroups[owner] = rtxGroupRdFr;
                }

                rtxGroupRdFr.Add(this);
                RdRegistry.Publish(_Frecuency, _FrRs);

                if (_FrRs.RtxGroupId > 0 && _RtxGroups[owner].Count > 1)
                {
                    if (!_SendingPttToRtxGroup && (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch) &&
                        ((_CurrentSrcPtt == null) || (_CurrentSrcPtt.SrcId != _FrRs.PttSrcId)) &&
                        (_PostPtt == null))
                    {
                        SendPttToRtxGroup(true, false);
                    }
                }
            }
            else if (_SendingPttToRtxGroup)
            {
                SendPttToRtxGroup(true, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void RemoveFromRtxGroup(bool publish)
        {
            LogTrace<RdFrecuency>(_Frecuency + " RemoveFromRtxGroup ");
            if ((_FrRs != null) && (_FrRs.RtxGroupId > 0))
            {
                string owner = _FrRs.RtxGroupOwner.ToUpper() + _FrRs.RtxGroupId;
                List<RdFrecuency> rtxGroupFr = _RtxGroups[owner];

                if (_FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                    SendPttToRtxGroup(false, false);

                bool removed = rtxGroupFr.Remove(this);
                Debug.Assert(removed);

                PttInfo pttInfo = _SrcPtts.Find(delegate (PttInfo p)
                {
                    if (_CurrentSrcPtt != null)
                        return (p.SrcId == _CurrentSrcPtt.SrcId);

                    return false;
                });

                if (pttInfo != null)
                {
                    _CurrentPttSemaphore.WaitOne();
                    _SrcPtts.Remove(pttInfo);
                    LogTrace<RdFrecuency>(_Frecuency + "RemoveFromRtxGroup:Remove " + pttInfo.SrcId + ",srcPtts.Count " + _SrcPtts.Count.ToString());
                    if ((_CurrentSrcPtt != null) && (_CurrentSrcPtt.SrcType == PttSource.Avion))
                    {
                        NextPtt();
                    }
                    _CurrentPttSemaphore.Release();
                }

                if (rtxGroupFr.Count == 1)
                {
                    rtxGroupFr[0].RemoveFromRtxGroup(true);
                    Debug.Assert(rtxGroupFr.Count == 0);
                }
                else if (rtxGroupFr.Count == 0)
                {
                    _RtxGroups.Remove(owner);
                }

                _FrRs.RtxGroupId = 0;
                _FrRs.RtxGroupOwner = null;

                if (publish)
                {
                    RdRegistry.Publish(_Frecuency, _FrRs);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool NextPtt(string topId = null)
        {
            bool changed = true;
            PttInfo oldPtt = _CurrentSrcPtt;
            Dictionary<int, IRdResource> sipTxCalls = SipTxCalls();
            if (_CurrentSrcPtt != null)
            {
                //EDU 08/06/2017
                //Al receptor del mismo emplazamiento y frecuencia se le envia el mismo PTT off que al tx
                foreach (IRdResource rdRes in RdRs.Values.Where(x => x.Connected))
                {
                    LogTrace<RdFrecuency>("PttOff_1 " + Frecuency + ",srcPtts.Count " + _SrcPtts.Count.ToString());
                    rdRes.PttOff();
                }

                RdMixer.Unlink(_CurrentSrcPtt.SrcType, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);
                _CurrentSrcPtt = null;
                StartTimerTxDefault();
                //ReceivePtt("Rtx_" + _FrRs.RtxGroupId + "_" + _Frecuency, PttSource.NoPtt, null);
                //ReceivePtt("Rtx_" + _FrRs.RtxGroupId + "_" + _Frecuency, PttSource.Avion, _SipRxCalls.Keys);
            }

            if (_SrcPtts.Count > 0)
            {
                _CurrentSrcPtt = new PttInfo(_SrcPtts[0]);
                LogTrace<RdFrecuency>("NextPtt _srcPtt[0]" + _SrcPtts[0].SrcId + " srcPtts.Count " + _SrcPtts.Count.ToString());

                RdMixer.Link(_CurrentSrcPtt.SrcType, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);

                foreach (IRdResource rdRes in RdRs.Values.Where(x => x.Connected))
                {
                    // JCAM. 27/02/2017
                    // Todos los recursos asociados a la frecuencia en transmisión hacen PTT
                    //EDU 08/06/2017
                    //Al receptor del mismo emplazamiento y frecuencia se le envia el PTT type
                    LogTrace<RdFrecuency>("PttOn_1 " + Frecuency + "," + _CurrentSrcPtt.Type.ToString() + ",srcPtts.Count " + _SrcPtts.Count.ToString());
                    rdRes.PttOn(_CurrentSrcPtt.Type);

                    if (rdRes.isTx)
                    {
                        StopTimerTxDefault();
                        if (rdRes.Ptt == RdRsPttType.OwnedPtt)
                        {
                            _FrRs.PttSrcId = _CurrentSrcPtt.SrcId;
                            changed = true;
                        }
                    }
                }
            }

            // Creo que esto no tiene sentido ya al incluir el semaforo y 
            // además no está bien implementado (changed siempre true). Igual se puede quitar (Blanca 28/06/17)
            if (oldPtt != _CurrentSrcPtt)
            {
                if (_PostPtt != null)
                {
                    _PostPtt.Enabled = false;
                    _PostPtt.Dispose();
                }

                if (!changed)
                {
                    RdRegistry.DisablePublish(_Frecuency);

                    _PostPtt = new Timer(_CurrentSrcPtt != null ? 200 : 750);
                    _PostPtt.AutoReset = false;
                    _PostPtt.Elapsed += OnTimerElapsed;
                    _PostPtt.Enabled = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        private void ActualizePtt(PttInfo p)
        {
            Dictionary<int, IRdResource> sipTxCalls = SipTxCalls();
            _CurrentSrcPtt.Type = p.Type;

            List<int> currentPorts = new List<int>(_CurrentSrcPtt.SrcPorts);
            _CurrentSrcPtt.SrcPorts.Clear();

            foreach (int srcPort in p.SrcPorts)
            {
                if (!currentPorts.Contains(srcPort))
                {
                    RdMixer.Link(_CurrentSrcPtt.SrcType, srcPort, sipTxCalls.Keys);
                }
                _CurrentSrcPtt.SrcPorts.Add(srcPort);
            }

            if (_CurrentSrcPtt.SrcType != p.SrcType)
            {
                Debug.Assert((_CurrentSrcPtt.SrcType != PttSource.Avion) && (p.SrcType != PttSource.Avion), "1");
                Debug.Assert(_CurrentSrcPtt.SrcPorts.Count == 1, "2");

                if (p.SrcType == PttSource.Hmi)
                {
                    if (_CurrentSrcPtt.SrcType == PttSource.Instructor)
                    {
                        RdMixer.Link(PttSource.Alumn, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);
                    }
                    else
                    {
                        Debug.Assert(_CurrentSrcPtt.SrcType == PttSource.Alumn);
                        RdMixer.Link(PttSource.Instructor, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);
                    }
                }
                else
                {
                    Debug.Assert(p.SrcType == PttSource.Instructor, "Ptt nuevo: " + p.SrcType.ToString());
                    Debug.Assert(_CurrentSrcPtt.SrcType == PttSource.Alumn, "Ptt anterior: " + _CurrentSrcPtt.SrcType);

                    RdMixer.Unlink(PttSource.Alumn, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);
                    RdMixer.Link(PttSource.Instructor, _CurrentSrcPtt.SrcPorts, sipTxCalls.Keys);
                }
                _CurrentSrcPtt.SrcType = p.SrcType;
            }
        }

        /// <summary>
        /// Se usa para realizar/quitar la retransmision del SQ recibido por la frecuencia, 
        /// a las frecuencias del grupo
        /// </summary>
        /// <param name="pttOn">true si ptt on, false si ptt off</param>
        /// <param name="force"></param>
        private void SendPttToRtxGroup(bool pttOn, bool force)
        {
            //Establece una sección crítica para corregir el desorden de eventos de Ptt de rtx
            lock (_RtxGroups)
            {
                List<RdFrecuency> rtxGroupFr = _RtxGroups[_FrRs.RtxGroupOwner.ToUpper() + _FrRs.RtxGroupId];
                LogTrace<RdFrecuency>(_Frecuency + " SendPttToRtxGroup on " + pttOn.ToString() + ",force " + force.ToString() +
                    " RtxGr " + _RtxGroups.Count().ToString() + "_sendingPtt:" + _SendingPttToRtxGroup.ToString());

                if (pttOn && (!_SendingPttToRtxGroup || force))
                {
                    Debug.Assert(_FrRs.RtxGroupId > 0);

                    Debug.Assert(rtxGroupFr.Count > 1);

                    //Solo envio los PTT coupling si nadie en el grupo está ya retransmitiendo o es forzado 
                    if ((IsGroupActive(rtxGroupFr) == false) || force)
                    {
                        _SendingPttToRtxGroup = true;

                        foreach (RdFrecuency rdFr in rtxGroupFr)
                        {
                            if (rdFr != this)
                            {
                                rdFr.ReceivePtt("Rtx_" + _FrRs.RtxGroupId + "_" + _Frecuency, PttSource.Avion, SipRxCalls().Keys);
                            }
                        }
                    }
                }
                else if (!pttOn && (_SendingPttToRtxGroup || force))
                {
                    Debug.Assert(_FrRs.RtxGroupId > 0);

                    _SendingPttToRtxGroup = false;

                    foreach (RdFrecuency rdFr in rtxGroupFr)
                    {
                        if (rdFr != this)
                        {
                            rdFr.ReceivePtt("Rtx_" + _FrRs.RtxGroupId + "_" + _Frecuency, PttSource.NoPtt, null);
                        }
                    }
                }
            }
        }
        //Devuelve true si alguna frecuencia del grupo ya está retransmitiendo
        private bool IsGroupActive(List<RdFrecuency> rtxGroupFr)
        {
            bool isGroupActive = false;
            foreach (RdFrecuency rdFr in rtxGroupFr)
            {
                if (rdFr != this)
                {
                    isGroupActive |= rdFr.SendingPttToRtxGroup;
                }
            }
            return isGroupActive;
        }
        /// <summary>
        /// 
        /// </summary>
        private void Reset(bool temporizacion)
        {
            if (_FrRs != null)
            {
                // Solo afectaria a reset si está temporizado
                if (temporizacion)
                {
                    if (_DisableFrequencyTimer != null && _DisableFrequencyTimer.Enabled)
                        return;

                    if (!_flag)
                    {
                        _flag = true;
                        if (_DisableFrequencyTimer != null)
                            _DisableFrequencyTimer.Enabled = true;
                        return;
                    }
                }
                else
                {
                    if (_DisableFrequencyTimer != null && _DisableFrequencyTimer.Enabled)
                        _DisableFrequencyTimer.Enabled = false;
                }


                if (_PostPtt != null)
                {
                    _PostPtt.Enabled = false;
                    _PostPtt.Dispose();
                    _PostPtt = null;
                }
                if ((_CurrentSrcPtt != null) && (SipTxCalls().Count > 0))
                {
                    StartTimerTxDefault();
                    foreach (IRdResource res in _RdRs.Values.Where(x => x.Connected))
                    {
                        res.PttOff();
                    }
                    RdMixer.Unlink(_CurrentSrcPtt.SrcType, _CurrentSrcPtt.SrcPorts, SipTxCalls().Keys);
                }

                RemoveFromRtxGroup(false);
                _CurrentPttSemaphore.WaitOne();

                _CurrentSrcPtt = null;

                LogTrace<RdFrecuency>(_Frecuency + "Reset:Clear " + " srcPtts.Count " + _SrcPtts.Count.ToString());
                _SrcPtts.Clear();
                _PttTypes.Clear();
                _RxIds.Clear();
                _TxIds.Clear();
                _FrRs = null;

                RdRegistry.Publish(_Frecuency, null);
                _CurrentPttSemaphore.Release();
            }
        }

        /// <summary>
        /// Tiempo que tiene que transcurrir para que una frecuencia se considere
        /// como caída. (Puede haber sido un cambio M->N o vv.)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnDisableFrequencyElapsed(object sender, ElapsedEventArgs e)
        {
            RdService.evQueueRd.Enqueue("OnDisableFrequencyElapsed", delegate ()
            {
                if (_DisableFrequencyTimer != null)
                    _DisableFrequencyTimer.Enabled = false;
                _flag = false;

                Reset(false);
                LogInfo<RdFrecuency>("ASPAS enviadas en " + Frecuency);
            });
        }


        /// <summary>
        /// AGL. ???. Que timer es este???
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerSuperviser(object sender, ElapsedEventArgs e)
        {
            LogTrace<RdFrecuency>(_Frecuency + " OnTimerSuperviser ");
            SendPttToRtxGroup(false, false);

            _WaitingForSuperviser.Enabled = false;
            _WaitingForSuperviser.Dispose();
            _WaitingForSuperviser = null;

            _FrRs.PttSrcId = "NO_CARRIER";

            RdRegistry.Publish(_Frecuency, _FrRs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            General.SafeLaunchEvent(TimerElapsed, sender, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rdFrToRemove"></param>
        /// <returns></returns>
        private static List<RdFrecuency> RemoveFromRtxGroupWithSquelch(List<RdFrecuency> rdFrToRemove)
        {
            List<RdFrecuency> sinSquelch = new List<RdFrecuency>();

            foreach (RdFrecuency rdFr in rdFrToRemove)
            {
                if (rdFr._FrRs.Squelch != RdSrvFrRs.SquelchType.NoSquelch)
                {
                    rdFr.RemoveFromRtxGroup(true);
                }
                else
                    sinSquelch.Add(rdFr);
            }
            return sinSquelch;
        }

        /// <summary>
        /// 
        /// </summary>
        private void ConfirmaPtt()
        {
            _FrRs.PttSrcId = _CurrentSrcPtt.SrcId;

            if (_PostPtt != null)
            {
                RdRegistry.EnablePublish(_Frecuency, false);

                _PostPtt.Enabled = false;
                _PostPtt.Dispose();
                _PostPtt = null;
            }
        }

        /// <summary>
        /// Actualiza los nuevos parámetros de la frecuencia con los recibidos en la configuración
        /// Retorna true si ha habido cambios con respecto a los anteriores configurados
        /// </summary>
        /// <param name="cfg"></param>
        private bool ResetNewParams(CfgEnlaceExterno cfg)
        {
            bool hayCambios = this.new_params.CLDCalculateMethod != (CORESIP_CLD_CALCULATE_METHOD)cfg.MetodoCalculoClimax ||
                                this.new_params.BssWindows != cfg.VentanaSeleccionBss ||
                                this.new_params.AudioSync != cfg.SincronizaGrupoClimax ||
                                this.new_params.NotUnassignable != cfg.FrecuenciaNoDesasignable ||
                                this.new_params.AudioInBssWindow != cfg.AudioPrimerSqBss ||
                                this.new_params.Priority != (CORESIP_Priority)cfg.PrioridadSesionSip ||
                                this.new_params.Cld_supervision_time != cfg.CldSupervisionTime ||
                                this.new_params.MetodosBssOfrecidos != cfg.MetodosBssOfrecidos ||
                                this.new_params.PorcentajeRSSI != cfg.PorcentajeRSSI;

            this.new_params.CLDCalculateMethod = (CORESIP_CLD_CALCULATE_METHOD)cfg.MetodoCalculoClimax;
            this.new_params.BssWindows = cfg.VentanaSeleccionBss;
            this.new_params.AudioSync = cfg.SincronizaGrupoClimax;
            // Convertir los tipos de frecuencia a los esperados por el módulo de tratamiento BSS-CLIMAX
            switch (cfg.TipoFrecuencia)
            {
                case Tipo_Frecuencia.DUAL:
                    hayCambios |= this.new_params.FrequencyType != CORESIP_FREQUENCY_TYPE.Dual;
                    this.new_params.FrequencyType = CORESIP_FREQUENCY_TYPE.Dual;
                    break;
                case Tipo_Frecuencia.FD:
                    hayCambios |= this.new_params.FrequencyType != CORESIP_FREQUENCY_TYPE.FD;
                    this.new_params.FrequencyType = CORESIP_FREQUENCY_TYPE.FD;
                    break;
                case Tipo_Frecuencia.ME:
                    hayCambios |= this.new_params.FrequencyType != CORESIP_FREQUENCY_TYPE.ME;
                    this.new_params.FrequencyType = CORESIP_FREQUENCY_TYPE.ME;
                    break;
                default:
                    hayCambios |= this.new_params.FrequencyType != CORESIP_FREQUENCY_TYPE.Simple;
                    this.new_params.FrequencyType = CORESIP_FREQUENCY_TYPE.Simple;
                    break;
            }

            this.new_params.NotUnassignable = cfg.FrecuenciaNoDesasignable;
            this.new_params.AudioInBssWindow = cfg.AudioPrimerSqBss;
            this.new_params.Priority = (CORESIP_Priority)cfg.PrioridadSesionSip;
            this.new_params.Cld_supervision_time = cfg.CldSupervisionTime;
            this.new_params.MetodosBssOfrecidos = cfg.MetodosBssOfrecidos;
            this.new_params.PorcentajeRSSI = cfg.PorcentajeRSSI;
            return hayCambios;
        }


        private void SendLogNewStatus(RdSrvFrRs.FrequencyStatusType oldStatus)
        {
            if (oldStatus != _Status)
                LogDebug<RdService>("FS Status. Frequency ID: " + this.Frecuency + ". Status: " + _Status,
                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, "RdService",
                CTranslate.translateResource("FS Status. Frequency ID: " + this.Frecuency + " Status: " + _Status.ToString()));
        }

        /// <summary>
        /// Gestiona los cambios en el metodo de transmision recibido en la configuración
        /// Actualiza el valor TxSelected inicial de los recursos de Tx según corresponda a lo configurado
        /// Los cambios de configuracion relacionados con el modo de transmisión tienen efecto en el
        /// siguiente Ptt, no son instantaneos
        /// </summary>
        /// <param name="cfg"></param>
        private void ConfiguraModoTransmision(CfgEnlaceExterno cfg)
        {
            IRdResource oldTxRsDefault = GetTxRsDefault();
            _TxIDDefault = null;
            if (_TimeToTxDefault != cfg.TiempoVueltaADefecto)
            {
                _TimeToTxDefault = cfg.TiempoVueltaADefecto;
                if (cfg.TiempoVueltaADefecto > 0)
                    _TimerTxDefault.Interval = cfg.TiempoVueltaADefecto * 1000;
                else
                    StopTimerTxDefault();
            }
            if (cfg.TipoFrecuencia == Tipo_Frecuencia.FD)
            {
                if (_ModoTransmision != cfg.ModoTransmision)
                {
                    _ModoTransmision = cfg.ModoTransmision;
                    //Sólo a los TX o TXRX
                    foreach (IRdResource rdRs in _RdRs.Values.Where(x => x.isTx))
                        rdRs.TxMute = _ModoTransmision == Tipo_ModoTransmision.UltimoReceptor ? true : false;
                    _TxIDSelected = null;
                }
                if (!String.IsNullOrEmpty(cfg.EmplazamientoDefecto))
                {
                    foreach (IRdResource rdRs in _RdRs.Values.Where (x=>(x.isTx && x.Site == cfg.EmplazamientoDefecto)))
                    {
                        _TxIDDefault = rdRs.ID;
                        StartTimerTxDefault();
                        if (String.IsNullOrEmpty(_LastSelectedSite))
                            _LastSelectedSite = cfg.EmplazamientoDefecto;
                        break;
                    }
                }
                else
                {
                    StopTimerTxDefault();
                }
                EvaluaTxMute();
            }
            else
            {
                if (_ModoTransmision != cfg.ModoTransmision)
                {
                    foreach (IRdResource rdRs in _RdRs.Values.Where(x => x.isTx))
                        rdRs.TxMute = false;
                    _TxIDSelected = null;
                    _LastSelectedSite = "";
                }
                _ModoTransmision = Tipo_ModoTransmision.Ninguno;
            }
        }

        /// <summary>
        /// Selecciona un transmisor de entre los conectados con estos criterios:
        /// 1-El que tenga el emplazamiento guardado en _LastSQSite siempre tiene preferencia
        /// 2-El siguiente que se elige es el de defecto
        /// 3-si no hay uno ya seleccionado previamente, cualquiera conectado
        /// Si no hay ninguna radio conectada, deselecciona el TX, si lo hay
        /// Si no tiene valor _LastSQSite, se le pone el del seleccionado (para poder conservar M+N)
        /// Si hay un cambio de seleccion de TX, con Ptt en curso, se actualizan los Ptt 
        /// para mantenerlo en el aire
        /// </summary>
        /// <param name="cfg"></param>
        private void EvaluaTxMute()
        {
            bool hayTxEnSite = false;
            IRdResource txSelected = null;
            IRdResource txConnected = null;

            if (_ModoTransmision == Tipo_ModoTransmision.UltimoReceptor)
            {
                //Para optimizar busquedas se usa TxRsSelected
                IRdResource TxRsSelected = GetTxRsSelected();
                if ((TxRsSelected != null) &&
                    (TxRsSelected.Connected) &&
                    (TxRsSelected.Site == _LastSelectedSite))
                    return;
                IRdResource TxRsDefault = GetTxRsDefault();
                //Se busca el seleccionado entre los recursos
                foreach (IRdResource rdRs in _RdRs.Values.Where(x => x.isTx))
                {
                    if (rdRs.Connected)
                    {
                        if (rdRs.Site == _LastSelectedSite)
                        {
                            hayTxEnSite = true;
                            txConnected = rdRs;
                        }
                        else if (!hayTxEnSite && (TxRsDefault != null) && (rdRs == TxRsDefault))
                        {
                            hayTxEnSite = true;
                            txConnected = rdRs;
                        }
                        else if (!hayTxEnSite)
                            txConnected = rdRs;
                    }
                    if (!rdRs.TxMute) txSelected = rdRs;
                }
                if (txSelected == txConnected)
                    return;
                //Deselecciona
                if (txSelected != null)
                {
                    if (!txSelected.Connected)
                    {
                        txSelected.TxMute = true;
                        _TxIDSelected = null;
                        //Cambio dinámico del PTT por cambio de TX seleccionado por desconexión
                        if (!string.IsNullOrEmpty(PttSrc))
                        {
                            if (txSelected.SipCallId != -1)
                                txSelected.PttOn(_CurrentSrcPtt.Type);
                            //Esto provoca que salte el aviso acustico de falsa maniobra,
                            //lo comentamos porque se ha elegido de momento el cambio dinámico de TX
                            //    _FrRs.PttSrcId = "ERROR"; ;
                            //    RdRegistry.Publish(_Frecuency, _FrRs);
                        }
                        LogDebug<RdFrecuency>(String.Format("tx deseleccionado por caida {0}", txSelected.Site));
                        txSelected = null;
                    }
                    //#4053
                    else if (hayTxEnSite && string.IsNullOrEmpty(PttSrc))
                    {
                        txSelected.TxMute = true;
                        _TxIDSelected = null;
                        LogDebug<RdFrecuency>(String.Format("tx deseleccionado por SQ mejor{0}", txSelected.Site));
                        txSelected = null;
                    }
                    //else no hay cambio de seleccionado
                }

                //Selecciona
                if (txSelected == null && (txConnected != null))
                {
                    txConnected.TxMute = false;
                    _TxIDSelected = txConnected.ID;
                    //Cambio dinámico del PTT por cambio de TX seleccionado 
                    if (!string.IsNullOrEmpty(PttSrc))
                        txConnected.PttOn(_CurrentSrcPtt.Type);
                    //Para evitar cambios por M+N o por sectorización, si no ha habido ningun SQ antes.
                    if (_LastSelectedSite == "")
                        _LastSelectedSite = txConnected.Site;
                    LogDebug<RdFrecuency>(String.Format("Nuevo tx seleccionado {0}", txConnected.Site));
                }
            }
        }
        //Timer for frequency inactivity, i.e. it should never be started if PTT is on
        private void StartTimerTxDefault()
        {
            if (!string.IsNullOrEmpty(PttSrc))
                return;
            IRdResource TxRsDefault = GetTxRsDefault();
            if ((_TimeToTxDefault > 0) && (TxRsDefault != null))
                if (TxRsDefault != GetTxRsSelected())
                    _TimerTxDefault.Enabled = true;
        }

        private void StopTimerTxDefault()
        {
            if (_TimerTxDefault.Enabled)
                _TimerTxDefault.Enabled = false;
        }

        #endregion

        #region Datos para mostrar en WEB para DEBUG

        public object PrivateData
        {
            get
            {
                return new
                {
                    ModoTransmision = _ModoTransmision,
                    TxRsSelected = GetTxRsSelected(),
                    LastSQSite = _LastSelectedSite,
                    Flag = _flag,
                    RtxGroups = from g in _RtxGroups select new { key = g.Key, grp = from f in g.Value select new { id = f.Frecuency } },
                    FrRs = _FrRs,
                    CurrentSrcPtt = _CurrentSrcPtt,
                    SrcPtts = from s in _SrcPtts select new { PttInfo = s },
                    TxIds = from t in _TxIds select new { id = t },
                    RxIds = from t in _RxIds select new { id = t },
                    SipTxCalls = from c in SipTxCalls() select new { key = c.Key, val = c.Value },
                    SipRxCalls = from c in SipRxCalls() select new { key = c.Key, val = c.Value },
                    PttTypes = from p in _PttTypes select new { Ptt = p },
                    RdRs = from r in _RdRs select new { key = r.Key, val = r.Value },
                    SendingPttToRtxGroup = SendingPttToRtxGroup,
                };
            }
        }

        public object PublicData
        {
            get
            {
                return new
                {
                    SupervisionPortadora = SupervisionPortadora,
                    PttSrc = PttSrc,
                    Picts = from pict in Picts select new { key = pict.Key, val = pict.Value },
                    SelectedSite = SelectedSite,
                    SelectedSiteQidx = SelectedSiteQidx,
                    SelectedTxSiteString = SelectedTxSiteString,
                    TipoDeFrecuencia = TipoDeFrecuencia,
                    FrecuenciaSintonizada = FrecuenciaSintonizada
                };
            }
        }

        #endregion
    }
}

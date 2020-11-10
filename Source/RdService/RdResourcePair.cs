using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;
using U5ki.RdService.Gears;

namespace U5ki.RdService
{
    public class RdResourcePair : BaseCode, IRdResource, IDisposable
    {
        private RdResource _ActiveResource = null;
        public RdResource ActiveResource
        {
            get { return _ActiveResource; }
            set { _ActiveResource = value; }
        }

        private RdResource _StandbyResource = null;
        public RdResource StandbyResource
        {
            get { return _StandbyResource; }
            set { _StandbyResource = value; }
        }
        public string _ID
        { get; set; } = "";

        private string FrequencyId { get; set; }
        public RdResourcePair(string id, string Frequency)
        {
            _ID = id;
            FrequencyId = Frequency;
        }

        private System.Timers.Timer checkPairWhenNbxStarts_Timer = null;

        public RdResourcePair(RdResource ActiveResource, RdResource StandbyResource, List<Node> nodes)
        {
            //List<string> ids = new List<string>();
            //ids.Add(ActiveResource.ID);
            //ids.Add(StandbyResource.ID);
            //ids.Sort();
            //foreach (string id in ids)
            //    _ID = String.Concat(id);
            FrequencyId = "TEST-FQ";

            checkPairWhenNbxStarts_Timer = null;
            checkPttConfirmed_Timer = null;

            _ActiveResource = ActiveResource;
            StandbyResource.TxMute = true;
            _StandbyResource = StandbyResource;
            //NodeSet(NodeParse(node));
            _ActiveResource = ActiveResource;
            MSTxPersistence.SelectMain(_ActiveResource, _StandbyResource);
        }

        public void SetActiveStandbyFromPersistence()
        {
            if (MSTxPersistence.IsMain(_ActiveResource) == false)
            {
                RdResource current_ActiveResource = _ActiveResource;
                RdResource current_StandbyResource = _StandbyResource;

                current_StandbyResource.TxMute = false;
                _ActiveResource = current_StandbyResource;
                current_ActiveResource.TxMute = true;
                _StandbyResource = current_ActiveResource;
            }            
        }

        public void SetActive(RdResource activeResource)
        {
            _ActiveResource = activeResource;
            ActiveResource.TxMute = false;
        }
        public void SetStandby(RdResource standbyResource)
        {
            _StandbyResource = standbyResource;
            StandbyResource.TxMute = true;
        }
        public bool Isconfigured()
        {
            return (_ActiveResource != null && _StandbyResource != null);
        }        

        #region IRdResource Members
        public RdRsType Type
        { get { return _ActiveResource.Type; } }

        public bool isRx
        { get { return _ActiveResource.isRx; } }

        public bool isTx
        { get { return _ActiveResource.isTx; } }
        /// <summary>
        /// Active SipCallId
        /// </summary>
        public int SipCallId
        { 
            get 
            {
                if (_ActiveResource.Connected)
                    return _ActiveResource.SipCallId;
                else if (_StandbyResource.Connected)
                    return _StandbyResource.SipCallId;
                else
                    return -1;
            } 
        }        

        public RdRsPttType Ptt
        { get { return _ActiveResource.Ptt; }}

        public ushort PttId
        { get { return _ActiveResource.PttId; } }

        /// <summary>
        /// Devuelve el recurso que tiene SQ
        /// El SQ puede llegar indiferentemente del activo o del standby
        /// </summary>
        /// <returns></returns>
        public bool Squelch
        { get { return (_ActiveResource.Squelch || _StandbyResource.Squelch); } }

        public bool TxMute
        { get { return _ActiveResource.TxMute; }
           set
            {
                _ActiveResource.TxMute = value;
            }
         }

        public string ID
        { get { return _ID; } }

        public string Uri1
        { get { return _ActiveResource.Uri1; } }
        public string Uri2
        { get { return _ActiveResource.Uri2; } }

        public bool ToCheck
        { get { return _ActiveResource.ToCheck; } }

        public string Site
        { get { return _ActiveResource.Site; }
            set
            {
                _ActiveResource.Site = value;
            }
        }

        public bool SelectedSite
        {
            get { return _ActiveResource.SelectedSite; }
            set
            {
                _ActiveResource.SelectedSite = value;
                _StandbyResource.SelectedSite = value;
            }
        }

        public bool Connected
        { get 
            {
                return (_ActiveResource.Connected || _StandbyResource.Connected);
            } 
        }
        public bool OldSelected
        {
            get { return _ActiveResource.OldSelected; }
            set
            {
                _ActiveResource.OldSelected = value;
                _StandbyResource.OldSelected = value;
            }
        }
        //used only in M+N resources
        public bool MasterMN { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ReplacedMN { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsForbidden { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion
        #region IDisposable Members
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (checkPairWhenNbxStarts_Timer != null)
            {
                checkPairWhenNbxStarts_Timer.Stop();
            }
            if (checkPttConfirmed_Timer != null)
            {
                checkPttConfirmed_Timer.Stop();
                checkPttConfirmed_Timer.Enabled = false;
            }

            _StandbyResource.Dispose();
            _StandbyResource = null;
            _ActiveResource.Dispose();
            _ActiveResource = null;
        }
        #endregion
        #region IRdResource methods

        /// <summary>
        /// Retorna true si se ha conectado algun recurso
        /// </summary>
        public bool Connect()
        {
            bool ret = false;
            if (_ActiveResource.Connected == false && _ActiveResource.Connecting == false && MSTxPersistence.IsNodeDisabled(_ActiveResource) == false)
            {
                _ActiveResource.Connect();
                ret = true;
            }
            if (_StandbyResource.Connected == false && _StandbyResource.Connecting == false && MSTxPersistence.IsNodeDisabled(_StandbyResource) == false)
            {
                _StandbyResource.Connect();
                ret = true;
            }
            return ret;
        }

        private CORESIP_PttType Last_CORESIP_PttType_sent = CORESIP_PttType.CORESIP_PTT_OFF;

        public void PttOff()
        {
            Last_CORESIP_PttType_sent = CORESIP_PttType.CORESIP_PTT_OFF;
            _ActiveResource.PttOff();
            _StandbyResource.PttOff();

            //Paramos el timer de confirmarcion de ptt si se envia un PTT off
            if (checkPttConfirmed_Timer != null && checkPttConfirmed_Timer.Enabled == true)
            {
                checkPttConfirmed_Timer.Stop();
                checkPttConfirmed_Timer.Enabled = false;
            }
        }

        public void PttOn(CORESIP_PttType srcPtt)
        {
            Last_CORESIP_PttType_sent = srcPtt;
            _ActiveResource.PttOn(srcPtt);
            _StandbyResource.PttOn(srcPtt);
        }

        public RdResource GetSimpleResource(int sipCallId)
        {
            if (_ActiveResource.SipCallId == sipCallId)
                return _ActiveResource;
            else if (_StandbyResource.SipCallId == sipCallId)
                return _StandbyResource;
            else return null;
        }
        /// <summary>
        /// Devuelve el recurso que tiene SQ seleccionado, o null si ninguno está seleccionado
        /// El SQ puede llegar indiferentemente del activo o del standby
        /// </summary>
        /// <returns></returns>
        public RdResource GetRxSelected()
        {
            if (_ActiveResource.Connected && _StandbyResource.Connected)
            {
                if (_ActiveResource.new_params.rx_selected)
                    return _ActiveResource;
                else if (_StandbyResource.new_params.rx_selected)
                    return _StandbyResource;
            }
            else if (_ActiveResource.Connected && !_StandbyResource.Connected && _ActiveResource.new_params.rx_selected)
            {
                return _ActiveResource;
            }
            else if (!_ActiveResource.Connected && _StandbyResource.Connected && _StandbyResource.new_params.rx_selected)
            {
                return _StandbyResource;
            }

            return null;
        }

        public List<RdResource> GetListResources()
        {
            List<RdResource> list = new List<RdResource>();
            list.Add(_ActiveResource);
            list.Add(_StandbyResource);
            return list;
        }

        private void OncheckPairWhenNbxStarts_Timer_Event(Object source, System.Timers.ElapsedEventArgs e)
        {
            RdService.evQueueRd.Enqueue("RdResourcePair:OncheckPairWhenNbxStarts_Timer_Event", delegate ()
            {
                if (!_ActiveResource.Connected && _StandbyResource.Connected)
                {
                    // Seleccion por Inactividad al Arrancar.
                    Switch();
                    NotifyAutomaticSelection("Inactividad Inicial de Recurso Seleccionado");
                }
                else
                {
                    MSTxPersistence.SelectMain(_ActiveResource, _StandbyResource);
                }
            });
        }

        private const double CHECK_PAIR_TIMEOUT = 250;

        public bool HandleChangeInCallState(int sipCallId, CORESIP_CallStateInfo stateInfo)
        {
            RdResource resChange;
            if (_ActiveResource.SipCallId == sipCallId)
            {
                resChange = _ActiveResource;                
            }
            else if (_StandbyResource.SipCallId == sipCallId)
            {
                resChange = _StandbyResource;
            }
            else 
                return false;

            if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED && checkPairWhenNbxStarts_Timer == null)
            {
                //The first session of the pair is established after Nodebox starts. Then checkPairWhenNbxStarts_Timer is started
                //When it elapsed, then if only one of the session of the pair is established, then this is the main
                checkPairWhenNbxStarts_Timer = new System.Timers.Timer();
                checkPairWhenNbxStarts_Timer.Interval = CHECK_PAIR_TIMEOUT;
                checkPairWhenNbxStarts_Timer.AutoReset = false;
                checkPairWhenNbxStarts_Timer.Elapsed += this.OncheckPairWhenNbxStarts_Timer_Event;
                checkPairWhenNbxStarts_Timer.Enabled = true;
            }

            if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED)
            {
                if ((_ActiveResource.SipCallId == sipCallId && _StandbyResource.Connected) ||
                    (_StandbyResource.SipCallId == sipCallId && _ActiveResource.Connected))
                {
                    if (checkPairWhenNbxStarts_Timer != null && checkPairWhenNbxStarts_Timer.Enabled)
                    {
                        checkPairWhenNbxStarts_Timer.Stop();
                        MSTxPersistence.SelectMain(_ActiveResource, _StandbyResource);
                    }
                    else if (checkPairWhenNbxStarts_Timer == null)
                    {
                        MSTxPersistence.SelectMain(_ActiveResource, _StandbyResource);
                    }
                }
            }
            else if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED)
            {
                if (checkPairWhenNbxStarts_Timer != null && checkPairWhenNbxStarts_Timer.Enabled == false)
                {
                    //The checkPairWhenNbxStarts_Timer finished
                    if (resChange == _ActiveResource)
                    {
                        if (_StandbyResource.Connected)
                        {
                            // Seleccion por Caida del Seleccionado.
                            Switch();
                            NotifyAutomaticSelection("Caida de Recurso Seleccionado");
                        }
                    }
                }
            }

            resChange.HandleChangeInCallState(sipCallId, stateInfo);
            LastRdResourceChanged = resChange;
            return true;
        }

        /// <summary>
        /// Performs a switch if the resource is the standby one.
        /// </summary>
        /// <param name="idResource">id of the resource</param>
        /// <returns>true if the resource belongs to this pair</returns>
        public bool ActivateResource(string idResource)
        {
            if (_StandbyResource.ID == idResource)
            {
#if !DEBUG
                if (_StandbyResource.Connected) 
#endif
                {
                    // Seleccion Manual.
                    Switch();
                    return true;
                }
            }
            return false;
        }

        #endregion
        #region NodeManager
        //public override string Name { get; }
        ///// <summary>
        ///// Recoge el Elapsed del timer del BaseManager, y lo utiliza para comprobar los nodos. 
        ///// Y lanzar la algoritmica de validación de nodos en caso de que haya cambios.
        ///// </summary>
        //protected override void OnElapsed(object sender, ElapsedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}
        //protected override BaseGear NodeParse(Node node)
        //{
        //    throw new NotImplementedException();
        //}
        ///// <summary>
        ///// Función basica de asignación de un nodo existente o nuevo.
        ///// </summary>
        //protected override bool NodeSet(BaseGear node)
        //{
        //    throw new NotImplementedException();
        //}
        ///// <summary>
        ///// Función basica de optención de información de un nodo de la session.
        ///// </summary>
        //protected override BaseGear NodeGet(BaseGear node)
        //{
        //    throw new NotImplementedException();
        //}
        #endregion
        /// <summary>
        /// Método para conmutar activo y standby mediante tx mute
        /// </summary> 
        private void Switch()
        {
            RdResource temp = _ActiveResource;
            _ActiveResource = _StandbyResource;
            _ActiveResource.TxMute = temp.TxMute;
            temp.TxMute = true;
            _StandbyResource = temp;

            MSTxPersistence.SelectMain(_ActiveResource, _StandbyResource);
        }

        /// <summary>
        /// Metodo que desconecta un recurso de recepcion que ha sido desactivado
        /// Retorna tru si se ha desactivado alguno
        /// </summary>
        public bool Check_1mas1_Resources_Disabled()
        {
            bool ret = false;

            RdResource current_active_resource = _ActiveResource;
            RdResource current_standby_resource = _StandbyResource;

            bool active_resource_disabled = MSTxPersistence.IsNodeDisabled(current_active_resource);
            bool standby_resource_disabled = MSTxPersistence.IsNodeDisabled(current_standby_resource);

            if (active_resource_disabled == true && standby_resource_disabled == false)
            {
                // Seleccion por haber deshabilitado el recurso.
                Switch();
                NotifyAutomaticSelection("Deshabilitacion de Recurso Seleccionado");
            }

            if (current_active_resource.Connected == true && current_active_resource.Connecting == false && active_resource_disabled == true)
            {
                current_active_resource.Dispose();
                ret = true;
            }
            if (current_standby_resource.Connected == true && current_standby_resource.Connecting == false && standby_resource_disabled == true)
            {
                current_standby_resource.Dispose();
                ret = true;
            }                      

            return ret;
        }
        void NotifyAutomaticSelection(string cause)
        {
            var msg = $"Equipo {_ActiveResource.ID} seleccionado automaticamente en grupo 1+1 {ID}. Motivo: {cause}";
            LogInfo<RdService>(msg,
                U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,
                FrequencyId,
                Translate.CTranslate.translateResource(msg));
        }

        public RdResource LastRdResourceChanged { get; set; }


        private System.Timers.Timer checkPttConfirmed_Timer = null;

        /// <summary>
        /// Checks PTT is confirmed. When standby resource comfirms PTT, it starts a timer. When the timer ends, if selected resource has not confirmed PTT, a switch is done
        /// </summary>
        /// <returns>
        public void CheckPttConfirmed(int sipCallId)
        {
            if ((_ActiveResource.SipCallId != sipCallId) && (_StandbyResource.SipCallId != sipCallId))
            {
                return;
            }

            //La info recibida corresponde a un recurso de este par 1+1

            if (isTx == false || Last_CORESIP_PttType_sent == CORESIP_PttType.CORESIP_PTT_OFF)
            {
                //Si no se esta transmitiendo PTT entonces paramos el timer si estaba corriendo y no hacemos nada
                if (checkPttConfirmed_Timer != null)
                {
                    if (checkPttConfirmed_Timer.Enabled == true)
                    {
                        checkPttConfirmed_Timer.Stop();
                        checkPttConfirmed_Timer.Enabled = false;
                    }
                }
                return;
            }

            if (_ActiveResource.Connected == false || _StandbyResource.Connected == false)
            {
                //Si uno de los dos recursos no esta conectado, entonces no podra haber conmutacion. 
                //No se hace nada y se para el timer si estaba corriendo
                if (checkPttConfirmed_Timer != null && checkPttConfirmed_Timer.Enabled == true)
                {
                    checkPttConfirmed_Timer.Stop();
                    checkPttConfirmed_Timer.Enabled = false;
                }
            }
            else if (_ActiveResource.Ptt == RdRsPttType.OwnedPtt)
            {
                //Si el recurso que tiene el PTT confirmado es el Activo entonces paramos el timer y no hacemos nada mas
                if (checkPttConfirmed_Timer != null && checkPttConfirmed_Timer.Enabled == true)
                {
                    checkPttConfirmed_Timer.Stop();
                    checkPttConfirmed_Timer.Enabled = false;
                }
            }
            else if (_StandbyResource.Ptt == RdRsPttType.OwnedPtt && _ActiveResource.Ptt != RdRsPttType.OwnedPtt)
            {
                //la confirmacion de Ptt es del standby y el activo no tiene PTT confirmado, entonces arrancamos un timer
                //Si cuando vence el timer el activo no ha confirmado el PTT entonces conmutamos

                if (checkPttConfirmed_Timer == null)
                {
                    checkPttConfirmed_Timer = new System.Timers.Timer();
                    checkPttConfirmed_Timer.Interval = U5ki.Infrastructure.SipAgent._KAPeriod + 50;                        ;
                    checkPttConfirmed_Timer.AutoReset = false;
                    checkPttConfirmed_Timer.Elapsed += this.OncheckPTTIsConfirmed_Timer_Event;
                    checkPttConfirmed_Timer.Start();
                }
                else if (checkPttConfirmed_Timer.Enabled == false)
                {
                    checkPttConfirmed_Timer.Start();
                }
            }    
            else if (_StandbyResource.Ptt != RdRsPttType.OwnedPtt)
            {
                //Si el standby deja de confirmar el PTT cuando el timer esta corriendo, entonces se para el timer
                if (checkPttConfirmed_Timer != null && checkPttConfirmed_Timer.Enabled == true)
                {
                    checkPttConfirmed_Timer.Stop();
                    checkPttConfirmed_Timer.Enabled = false;
                }
            }
        }

        private void OncheckPTTIsConfirmed_Timer_Event(Object source, System.Timers.ElapsedEventArgs e)
        {            
            RdService.evQueueRd.Enqueue("RdResourcePair:OncheckPTTIsConfirmed_Timer_Event", delegate ()
            {
                if (_ActiveResource.Connected && _StandbyResource.Connected && 
                    _ActiveResource.Ptt == RdRsPttType.NoPtt && _StandbyResource.Ptt != RdRsPttType.NoPtt)
                {
                    // Se conmuta porque el recurso activo no confirma el PTT y el standby si
                    Switch();
                    if (Last_CORESIP_PttType_sent == CORESIP_PttType.CORESIP_PTT_OFF)
                    {
                        this.PttOff();
                    }
                    else
                    {
                        this.PttOn(Last_CORESIP_PttType_sent);
                    }
                    NotifyAutomaticSelection("El Recurso Seleccionado no confirma PTT");
                }                
            });
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using U5ki.Infrastructure;
using U5ki.RdService.Gears;

namespace U5ki.RdService
{
    public class RdResourcePair : BaseCode, IRdResource, IDisposable
    {
        RdResource _ActiveResource;
        public RdResource ActiveResource { get => _ActiveResource; }
        RdResource _StandbyResource;
        string _ID = "";
        public RdResourcePair(RdResource ActiveResource, RdResource StandbyResource, List<Node> nodes)
        {
            List<string> ids = new List<string>();
            ids.Add(ActiveResource.ID);
            ids.Add(StandbyResource.ID);
            ids.Sort();
            foreach (string id in ids)
                _ID = String.Concat(id);
            _ActiveResource = ActiveResource;
            StandbyResource.TxMute = true;
            _StandbyResource = StandbyResource;
            //NodeSet(NodeParse(node));
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
        { get { return _ActiveResource.SipCallId; }}

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
        { get { return _ActiveResource.Connected; } }
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
            _StandbyResource.Dispose();
            _StandbyResource = null;
            _ActiveResource.Dispose();
            _ActiveResource = null;
        }
        #endregion
        #region IRdResource methods
        public bool Connect()
        {
            bool res = false;
            if (_ActiveResource.Connected == false)
                res |= _ActiveResource.Connect();
            if (_StandbyResource.Connected == false)
                _StandbyResource.Connect();
            return true;
        }

        public void PttOff()
        {
            _ActiveResource.PttOff();
            _StandbyResource.PttOff();
        }

        public void PttOn(CORESIP_PttType srcPtt)
        {
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
            if (_ActiveResource.new_params.rx_selected)
                return _ActiveResource;
            else if (_StandbyResource.new_params.rx_selected)
                return _StandbyResource;
            else return null;
        }

        public List<RdResource> GetListResources()
        {
            List<RdResource> list = new List<RdResource>();
            list.Add(_ActiveResource);
            list.Add(_StandbyResource);
            return list;
        }
        public bool HandleChangeInCallState(int sipCallId, CORESIP_CallStateInfo stateInfo)
        {
            RdResource resChange;
            if (_ActiveResource.SipCallId == sipCallId)
            {
                resChange = _ActiveResource;
                if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED)
                {
                    if (_StandbyResource.Connected)
                        Switch();
                }
            }
            else if (_StandbyResource.SipCallId == sipCallId)
            {
                resChange = _StandbyResource;
                if (stateInfo.State == CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED)
                {
                    if (_ActiveResource.Connected == false)
                        Switch();
                }
            }
            else return false;
            resChange.HandleChangeInCallState(sipCallId, stateInfo);
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
                if (_StandbyResource.Connected) 
                    Switch();
                return true;
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
        }
    }
}

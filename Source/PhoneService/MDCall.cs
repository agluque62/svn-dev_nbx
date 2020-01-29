using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;
using NLog;
using System.Timers;
using System.Threading;
using Utilities;
namespace u5ki.PhoneService
{
    public class MDCall: BaseCode
    {
        public class DestinationTlf
        {
            private string _Host;
            public string _Name { get; set; }
            private uint _Port;
            public string _Number ;
            public bool _Subscribed {get; set; }
            public string _Cd40rs ;
            public int _Result = 0;
            public List<DestinationTlf> _DestBag = new List<DestinationTlf>();
            public DestinationTlf (string name, string host, int port, string number, string cd40rs=null)
            {
                _Host = host;
                _Name = name;
                _Port = (uint) port;
                _Number = number;
                _Subscribed = false;
                _Cd40rs = cd40rs;
            }
            public void AddDestBag(string name, string host, int port, string number, string cd40rs)
            {
                DestinationTlf otherDst = new DestinationTlf(name, host, port, number, cd40rs);
                _DestBag.Add(otherDst);
            }

            public bool SwitchDstFromBag()
            {
                DestinationTlf otherDst = _DestBag.Find(x => x._Result == 0);
                if (otherDst != null)
                {
                   //TODO !!!!!
                    _Host = otherDst._Host;
                    _Name = otherDst._Name;
                    _Number = otherDst._Number;
                    _Port = otherDst._Port;
                    _Cd40rs = otherDst._Cd40rs;
                    _Result = otherDst._Result;
                    return true;
                }
                return false;
            }

            public void ClearResults()
            {
                _Result = 0;
                foreach (DestinationTlf dst in _DestBag)
                    dst._Result = 0;
            }
            public string Uri
            {
                get
                {
                    //sin display name
                    string ret = new Utilities.SipUtilities.SipUriParser(_Number, _Host).UlisesFormat;
                    if (_Cd40rs != null) 
                    {
                        ret = ret.TrimEnd('>') + ";cd40rs=" + _Cd40rs + ">";
                    }
                    return ret;
                }
            }
            public override bool Equals(object obj)
            {
                bool ret = false;
                if (obj == null)
                    return ret;
                if (this.GetType() != obj.GetType()) return ret;

                DestinationTlf p = (DestinationTlf)obj;
                if ((this._Name == p._Name) && (this._Number == p._Number) &&
                    (this._Host == p._Host) && (this._Port == p._Port))
                    ret = true;
                return ret;
            }
        }
        public class CallInfo
        {
            public enum CallState { Idle, OutGoing, InComing, InConversation, OutConversation, Hold, LocalHold} ;
            private CallState _State = CallState.Idle;
            private CallState _OldState = CallState.Idle;
            public CallState State { get { return _State; } set { _OldState = _State; _State = value; } }
            public CallState OldState { get { return _OldState; } }
            private int _CallId = -1;
            public int CallId { get { return _CallId; } set { _CallId = value; } } 
            public enum CallRole {MD_CALLER, MD_MEMBER, UNKNOWN};
            private CallRole _Role = CallRole.UNKNOWN;
            public CallRole Role { get { return _Role; } set { _Role = value; } }
            private DestinationTlf _Dst;
            public DestinationTlf Dst { get { return _Dst; } set { _Dst = value; } }
            public CallInfo(int callId, CallRole role, DestinationTlf dst)
            {
                _CallId = callId;
                _Role = role;
                _Dst = dst;
            }
        }
        #region members
        /// <summary>
        /// States defined by ED137C for MD call FSM
        /// </summary>
        public enum MDCallState { Idle, Ringing, InUse } ;
        private string _Name;
        public string Name
        {
            get { return _Name; }
        }
        private string _UriGroup;
        public string Uri
        {
            get { return _UriGroup; }
        }
        private List<DestinationTlf> _GroupMembers = new List<DestinationTlf> ();
        public List<string> GroupMembers
        {
            get {
                List<string> list = new List<string>();
                foreach (DestinationTlf dst in _GroupMembers)
                {
                    list.Add(dst.Uri);
                }
                return list;
            }
        }
        private List<CallInfo> _LiveCall = new List<CallInfo>();
        public List<CallInfo> LiveCall
        {
            get {return _LiveCall;}
        }
        private MDCallState _State = MDCallState.Idle;
        public MDCallState State
        {
            get { return _State; }
        }
        private Mixer _Mixer = new Mixer();
        private IPEndPoint _ProxyIPEndPoint;
        public string ProxyIP
        {
            get { return _ProxyIPEndPoint.ToString(); }
        }
        //Destination media to link other sources. It is the idCall of original caller 
        private const int NOBODY = -1;
        private int _Starter = NOBODY;
        private System.Timers.Timer _StopCallingMD = new System.Timers.Timer(Properties.Settings.Default.CancelMDCallTime*1000+10);
        private bool _Disposing = false;
        private int _ToneCalling = -1;
        //Clave callId y valor el tono  
        protected Dictionary <int, int> _ToneCallingSet = new Dictionary<int,int> ();
        private uint _VersionNotify = 0;
        #endregion

        public MDCall(string name, string ip, uint port, IPEndPoint proxyIP)
        {
            _Name = name;
            _ProxyIPEndPoint = proxyIP;
            Utilities.SipUtilities.SipUriParser sipUri = new Utilities.SipUtilities.SipUriParser(name, ip, port);
            _UriGroup = sipUri.UlisesFormat;
            _StopCallingMD.AutoReset = false;
            _StopCallingMD.Enabled = false;
            _StopCallingMD.Elapsed += OnStopCalling;

            LogInfo<MDCall>("Create MD group "+_Name);
        }
        public void Dispose()
        {
            _Disposing = true;
            _StopCallingMD.Enabled = false;
            if (_State != MDCallState.Idle)
            {
                EndMDCall();
                Thread.Sleep(200);
            }
            SipAgent.DestroyAccount(_Name);
        }

        public void AddMember(string user, string number, IPEndPoint ip, string cd40rs = null)
        {
            DestinationTlf destFound = null;
            if (cd40rs == null)
                _GroupMembers.Add(new DestinationTlf(user, ip.Address.ToString(), ip.Port, number));
            else
            {
                destFound = _GroupMembers.Find(x => x._Number == number);
                if (destFound == null)
                {
                    destFound = new DestinationTlf(user, ip.Address.ToString(), ip.Port, number, cd40rs);
                    _GroupMembers.Add(destFound);
                }
                else
                    destFound.AddDestBag(user, ip.Address.ToString(), ip.Port, number, cd40rs);
            }           
        }

        public void InitAgent()
        {
            LogInfo<MDCall>("InitAgent Focus " + _Name);
            SipAgent.CreateAccountAndRegisterInProxy(_Name, ProxyIP, Properties.Settings.Default.ExpireInProxy, _Name, _Name, _Name, true);
            _ToneCalling = SipAgent.CreateWavPlayer("Tones/Calling.wav", true);
        }

        /// <summary>
        /// Reimplementa el método Equals para sncronizar las configuracion
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            bool ret = false;
            DestinationTlf found = null;
            if (obj == null)
                return ret;
            if (this.GetType() != obj.GetType()) return ret;

            MDCall p = (MDCall)obj;
            if ((this._Name == p._Name) && (this._ProxyIPEndPoint.ToString() == p._ProxyIPEndPoint.ToString()) && (this._UriGroup == p._UriGroup))
            {
                ret = true;
                foreach (DestinationTlf dst in p._GroupMembers)
                {
                    found = _GroupMembers.Find(x => dst.Equals(x));
                    if (found == null)
                    {
                        ret = false;
                        break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Handle para tratar llamadas entrantes
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="call2replace"></param>
        /// <param name="info"></param>
        /// <param name="inInfo"></param>
		public void OnCallIncoming(int callId, int call2replace, CORESIP_CallInfo info, CORESIP_CallInInfo inInfo)
        {
            if (_Disposing)
                return;
            int CORESIP_Answer = SipAgent.SIP_NOT_FOUND;
            if (inInfo.DstId.Equals(_Name))
            {
                //it'is for me
                LogDebug<MDCall>("OnIncomingCall: " + inInfo.SrcId + " call: " + callId.ToString("X"));
                CORESIP_Answer = SipAgent.SIP_OK;
                DestinationTlf dst = new DestinationTlf(inInfo.DisplayName, inInfo.SrcIp, (int)inInfo.SrcPort, inInfo.SrcId);
                switch (_State)
                {
                    case MDCallState.Idle:
                        if (!_GroupMembers.Exists(x => x._Number == inInfo.SrcId))
                        {
                            CallInfo OrigenMD = new CallInfo(callId, CallInfo.CallRole.MD_CALLER, dst);
                            OrigenMD.State = CallInfo.CallState.InComing;
                            _LiveCall.Add(OrigenMD);
                            _Starter = callId;
                        }
                        //No acepto llamadas de miembros del grupo si no está activo la MDCall
                        else 
                            CORESIP_Answer = SipAgent.SIP_DECLINE;
                        break;
                    //case MDCallState.Ringing:
                    //    LogError<MDCall>("Not implemented: incoming call during Ringing phase");
                    //    CORESIP_Answer = SipAgent.SIP_TRYING; // ????? para que vuelva a intentarlo más tarde
                    //    break;
                    case MDCallState.Ringing:
                    case MDCallState.InUse:
                        CallInfo newCall;
                        // Es miembro del grupo?
                        DestinationTlf member = _GroupMembers.Find(x => x._Number == inInfo.SrcId);
                        if (member != null)
                        {
                            member.ClearResults();
                            newCall = new CallInfo(callId, CallInfo.CallRole.MD_MEMBER, member);
                        }
                        else
                            newCall = new CallInfo(callId, CallInfo.CallRole.MD_CALLER, dst);
                        newCall.State = CallInfo.CallState.InComing;
                        _LiveCall.Add(newCall);
                        break;
                }
                try
                {
                    SipAgent.AnswerCall(callId, CORESIP_Answer);
                }
                catch (Exception excep)
                {
                    LogException<MDCall>(String.Format("SipAgent.AnswerCall: " + inInfo.SrcId + " call: " + callId.ToString()), excep, false);
                } 
            }
        }
        /// <summary>
        /// Handle para tratar cambios de estados en las llamadas
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="info"></param>
        /// <param name="stateInfo"></param>
        public void OnCallState(int callId, CORESIP_CallInfo info, CORESIP_CallStateInfo stateInfo)
        {
            if (_Disposing)
                return;
            CallInfo thisCall = _LiveCall.Find(x => x.CallId == callId);
            if (thisCall != null )
            {
                //it'is for me
                LogDebug<MDCall>("OnCallState: " + stateInfo.State);
                switch (stateInfo.State)
                {
                    case CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED:
                        switch (_State)
                        {
                            case MDCallState.Idle:                                
                                FanOutMDCalls();
                                thisCall.State = CallInfo.CallState.InConversation;
                                _State = MDCallState.Ringing;
                                //Pone tono
                                _ToneCallingSet.Add(callId, _ToneCalling);
                                _Mixer.Link(_ToneCalling, Mixer.UNASSIGNED_PRIORITY, callId, Mixer.UNASSIGNED_PRIORITY);
                                 break;
                            case MDCallState.Ringing:
                                 if (thisCall.Role == CallInfo.CallRole.MD_MEMBER)
                                 {
                                     thisCall.State = CallInfo.CallState.OutConversation;
                                     _StopCallingMD.Enabled = true;
                                     _State = MDCallState.InUse;
                                     //Quita tono
                                     foreach (int call in _ToneCallingSet.Keys)
                                         _Mixer.Unlink(call);
                                     _ToneCallingSet.Clear();
                                 }
                                 else
                                 {
                                     //Pone tono
                                     _ToneCallingSet.Add(callId, _ToneCalling);
                                     _Mixer.Link(_ToneCalling, Mixer.UNASSIGNED_PRIORITY, callId, Mixer.UNASSIGNED_PRIORITY);
                                     thisCall.State = CallInfo.CallState.InConversation;
                                 }
                                 NotifyConfInfo();
                                 //Pone media entre usuarios
                                 AddMedia(callId);
                               break;
                            case MDCallState.InUse:                                
                                ManageMedia(thisCall, stateInfo.MediaStatus);
                                //AddMedia(thisCall.CallId);
                                if (thisCall.State != thisCall.OldState)
                                    NotifyConfInfo();
                                break;
                        }
                        break;
                    case CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED:
                        //TODO pending Reintentos para destinos con saco de recursos como RTB
                        //if (stateInfo.LastCode != SipAgent.SIP_OK)
                        //{
                        //    thisCall.Dst._Result = stateInfo.LastCode;
                        //    RetryCall(thisCall);
                        //}
                        switch (_State)
                        {
                            case MDCallState.Idle:
                                _LiveCall.Remove(thisCall);
                                //No se notifica el cambio porque se ha mandado el deleted de la suscripcion
                               break;
                            case MDCallState.Ringing:
                                _LiveCall.Remove(thisCall);
                                if ((thisCall.CallId == _Starter) || (_LiveCall.Count < 2))
                                {
                                    //Quita tono
                                    foreach (int call in _ToneCallingSet.Keys)
                                        _Mixer.Unlink(call);
                                    _ToneCallingSet.Clear(); 
                                    EndMDCall(stateInfo.LastCode);
                                }
                                else
                                    NotifyConfInfo();
                                break;
                            case MDCallState.InUse:
                                _LiveCall.Remove(thisCall);
                                _Mixer.Unlink(thisCall.CallId);
                                if (_LiveCall.Count(x => x.Role == thisCall.Role) == 0)
                                {
                                    //Es el ultimo de los llamantes o llamados. Termino la MD
                                    EndMDCall();
                                }
                                else
                                {
                                    if ((thisCall.CallId == _Starter) && (_State == MDCallState.InUse))
                                    //Cambio el destino de los media, si la llamada MD se mantiene
                                    {
                                        _Starter = _LiveCall.Find(x => x.Role == CallInfo.CallRole.MD_CALLER).CallId;
                                        foreach (CallInfo otherCalled in _LiveCall)
                                            if (otherCalled.Role == CallInfo.CallRole.MD_MEMBER)
                                            {
                                                AddMedia(callId);
                                            }
                                    }
                                    NotifyConfInfo();
                                }
                                break;
                        }
                        break;
                    default:
                        //Do nothing
                        break;
                } // switch
            }  //if
        }
        /// <summary>
        ///Gestiona los media de la llamada teniendo en cuenta el mediaStatus, para los escenarios
        ///de retencion de las llamadas.
        ///Cuando un miembro de la multidestino envia el aparcado, el foco reenvia el aparcado
        ///al resto. Solo se desaparca la llamada cuando el originante de la llamada desaparca
        ///la multidestino. El resto de participantes son pasivos.
        /// </summary>
        /// <param name="newCallId"></param>
        /// <param name="mediaStatus"></param>
        private void ManageMedia (CallInfo newCallId, CORESIP_MediaStatus mediaStatus)
        {
            switch (mediaStatus)
            {
                case CORESIP_MediaStatus.CORESIP_MEDIA_NONE:
                    newCallId.State = newCallId.Role == CallInfo.CallRole.MD_CALLER ? 
                        CallInfo.CallState.InConversation : CallInfo.CallState.OutConversation;
                    foreach (CallInfo call in EstablishedCalls)
                    {
                        _Mixer.Unlink(call.CallId);
                        //Envio el hold al resto de participantes
                        if (call.CallId != newCallId.CallId)
                            SipAgent.HoldCall(call.CallId);
                    }
                    newCallId.State = CallInfo.CallState.Hold;
                    break;
                case CORESIP_MediaStatus.CORESIP_MEDIA_ACTIVE:
                    if (newCallId.State == CallInfo.CallState.Hold)
                    {
                        foreach (CallInfo call in _LiveCall)
                        {
                            //Envio el unhold al resto de participantes
                            if (call.CallId != newCallId.CallId)
                                SipAgent.UnholdCall(call.CallId);
                        }
                    }
                    newCallId.State = newCallId.Role == CallInfo.CallRole.MD_CALLER ? 
                        CallInfo.CallState.InConversation : CallInfo.CallState.OutConversation;
                    AddMedia(newCallId.CallId);
                    break;
                case CORESIP_MediaStatus.CORESIP_MEDIA_LOCAL_HOLD:
                    //Miembros pasivos en la retención de la llamada
                    newCallId.State = CallInfo.CallState.LocalHold;
                    break;
                default:
                    LogInfo<MDCall>("ManageMedia not implemented " + mediaStatus.ToString());
                    break;
            }
        }
        private void AddMedia(int newCallId)
        {            
            foreach (CallInfo call in EstablishedCalls)
            {
                if (call.CallId != newCallId)
                {
                    _Mixer.Link(newCallId, Mixer.UNASSIGNED_PRIORITY, call.CallId, Mixer.UNASSIGNED_PRIORITY);
                    _Mixer.Link(call.CallId, Mixer.UNASSIGNED_PRIORITY, newCallId, Mixer.UNASSIGNED_PRIORITY);
                }
            }
        }

        /// <summary>
        /// Se llama cuando CORESIP ha recibido una suscripcion al evento 'conference'
        /// </summary>
        /// <param name="id">name of MD</param>
        /// <param name="info"></param>
        /// <param name="lenInfo"></param>
        public void OnIncomingSubscribeConf(string id, string info, uint lenInfo)
        {
            if (id == _Name)
            {
                //it't is for me
                Utilities.SipUtilities.SipUriParser sipUri = new Utilities.SipUtilities.SipUriParser(info);
                CallInfo participant =_LiveCall.Find(x => x.Dst._Number == sipUri.User);
                if (participant != null)
                {
                    DestinationTlf participantDst = participant.Dst;
                    if (participantDst != null)
                    {
                        participantDst._Subscribed = true;
                        NotifyConfInfo(participant.CallId);
                    }
                }
                DestinationTlf member = _GroupMembers.Find(x => x._Number == sipUri.User);
                if ((member != null) && member._Subscribed == false)
                    member._Subscribed = true;
            }
        }
        /// <summary>
        /// Termina todas las llamadas vivas de una MD
        /// </summary>
        /// <param name="code" parámetro opcional> codigo de error</param>        
        ///
        private void EndMDCall(int code = -1)
        {
            LogTrace<MDCall>("EndMDCall "+ code.ToString());
            NotifyEndInfo();
            foreach (CallInfo otherCall in _LiveCall)
            {
                if (code == -1)
                    SipAgent.HangupCall(otherCall.CallId);
                else
                    SipAgent.HangupCall(otherCall.CallId, code);
                _Mixer.Unlink(otherCall.CallId);
            }
            //_LiveCall.Clear(); cuando hacer esto por seguridad?
            _State = MDCallState.Idle;
            _Starter = NOBODY;
        }

        /// <summary>
        /// Hace llamadas a todos los miembros del grupo
        /// </summary>
        private void FanOutMDCalls()
        {
            foreach (DestinationTlf dst in _GroupMembers)
            {
                dst.ClearResults();
                //TODO revisar flags y priority
                CORESIP_CallFlags flags = CORESIP_CallFlags.CORESIP_CALL_EXTERNAL_IP;
                int sipCallId = SipAgent.MakeTlfCall(_Name, dst.Uri, null, CORESIP_Priority.CORESIP_PR_NORMAL, flags);
                CallInfo call = new CallInfo(sipCallId, CallInfo.CallRole.MD_MEMBER, dst);
                call.State = CallInfo.CallState.OutGoing;
                _LiveCall.Add(call);
            }
        }

        /// <summary>
        /// Implementa los reintentos de llamada cuando hay una bolsa de recursos comoen RTB
        /// </summary>
        /// <param name="callInfo">llamada que ha dado error</param>
        private void RetryCall(CallInfo callInfo)
        {
            if (callInfo.Dst.SwitchDstFromBag())
            {
                CORESIP_CallFlags flags = CORESIP_CallFlags.CORESIP_CALL_EXTERNAL_IP;
                int sipCallId = SipAgent.MakeTlfCall(_Name, callInfo.Dst.Uri, null, CORESIP_Priority.CORESIP_PR_NORMAL, flags);
                CallInfo call = new CallInfo(sipCallId, CallInfo.CallRole.MD_MEMBER, callInfo.Dst);
                call.State = CallInfo.CallState.OutGoing;
                _LiveCall.Add(call);
            }
        }
        /// <summary>
        /// Temporizador que se utiliza para cancelar las llamadas pendientes en un MD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStopCalling(object sender, ElapsedEventArgs e)
        {
            if (_Disposing)
                return;
            foreach (CallInfo pendingCall in LiveCall)
                if (pendingCall.State == CallInfo.CallState.OutGoing)
                    SipAgent.HangupCall(pendingCall.CallId);
        }
        /// <summary>
        /// Construye y envía el notify con los miembros de la conferencia. 
        /// Se llama cuando alguien se suscribe (1) y cuando se añaden miembros a la conferencia (2).
        /// Tiene un parametro opcional callId. Si viene, se envia el notify al miembro que 
        /// contiene ese callId (1). El comportamiento por defecto es que si no viene el parámetro 
        /// se envia a todos los miembros de la conferencia (2).
        /// </summary>
        /// <param name="callId" parámetro opcional, callId ></param>
        /// <returns> </returns>
        private void NotifyConfInfo(int callId = -1)
        {
            CORESIP_ConfInfo info = new CORESIP_ConfInfo();

            info.Users = new CORESIP_ConfInfo.ConfUser[SipAgent.CORESIP_MAX_CONF_USERS];
            uint j = 0;
            foreach (CallInfo call in EstablishedCalls)
            {
                info.Users[j].Id = call.Dst.Uri;
                info.Users[j].Name = call.Dst._Name;
                if (call.Role == CallInfo.CallRole.MD_CALLER)
                    info.Users[j].Role = "MD_CALLER";
                else if (call.Role == CallInfo.CallRole.MD_MEMBER)
                    info.Users[j].Role = "MD_MEMBER";
                j++;
            }
            info.UsersCount = j;
            if (callId != -1)
                info.Version = _VersionNotify;
            else
                info.Version = ++_VersionNotify;

            SipAgent.SendConfInfoFromAcc(Name, info);
            LogDebug<MDCall>("NotifyConfInfo to " + callId + " participantes: " + j);
        }
        private void NotifyEndInfo() 
        {
            CORESIP_ConfInfo info = new CORESIP_ConfInfo();

            info.Version = ++_VersionNotify;
            _VersionNotify = 0;
            info.Users = new CORESIP_ConfInfo.ConfUser[SipAgent.CORESIP_MAX_CONF_USERS];
            info.State = "deleted";
            info.UsersCount = 0;
            SipAgent.SendConfInfoFromAcc(Name, info);
            LogDebug<MDCall>("NotifyEndConf to miembros: ");
        }

        private IEnumerable<CallInfo> EstablishedCalls
        {
            get
            {
                foreach (CallInfo call in _LiveCall)
                    {
                        if ((call.State == CallInfo.CallState.InConversation) ||
                            (call.State == CallInfo.CallState.OutConversation))
                        {
                            yield return call;
                        }
                    }
            }
        }
    }
}

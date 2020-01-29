#define _HF_GLOBAL_STATUS_
#define _PUBLISH_MNDIS_
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NLog;

using Utilities;
using ProtoBuf;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;

namespace U5ki.RdService
{
    /// <summary>
    /// 
    /// </summary>
    static class RdRegistry 
	{
        public static bool _Master = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="onChannelError"></param>
        /// <param name="onMasterStatusChanged"></param>
        /// <param name="onRsChanged"></param>
        /// <param name="onMsgReceived"></param>
		public static void Init(
            GenericEventHandler<string> onChannelError,
			GenericEventHandler<bool> onMasterStatusChanged, 
            GenericEventHandler<RsChangeInfo> onRsChanged,
			GenericEventHandler<SpreadDataMsg> onMsgReceived)
		{
			_OnChannelError = onChannelError;
			_OnMasterStatusChanged = onMasterStatusChanged;
			_OnRsChanged = onRsChanged;
			_OnMsgReceived = onMsgReceived;

			InitRegistry();
		}

        /// <summary>
        /// 
        /// </summary>
		public static void End()
		{
			if (_Registry != null)
			{
				_Registry.Dispose();
				_Registry = null;
			}
            _DisabledFr.Clear();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="on"></param>
		public static void Activate(bool on)
		{
			if (on)
			{
				_Registry.Join(Identifiers.RdTopic, Identifiers.CfgTopic, Identifiers.TopTopic);
			}
			else
			{
				End();
                /** AGL2014. */
                System.Threading.Thread.Sleep(1000);
				InitRegistry();
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="publish"></param>
		public static void EnablePublish(string fr, bool publish)
		{
			RdSrvFrRs rs;
			string frId = fr.ToUpper();

            if (!_Master)
                return;
			if (_DisabledFr.TryGetValue(frId, out rs))
			{
				_DisabledFr.Remove(frId);

                if (publish && (rs != null) && (_Registry != null))
				{
					_Registry.SetValue<RdSrvFrRs>(Identifiers.RdTopic, fr, rs);
					_Registry.Publish();
				}
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
		public static void DisablePublish(string fr)
		{
			_DisabledFr[fr.ToUpper()] = null;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fr"></param>
        /// <param name="rs"></param>
		public static void Publish(string fr, RdSrvFrRs rs)
		{
			string frId = fr.ToUpper();
            if (!_Master)
                return;

			if (_DisabledFr.ContainsKey(frId))
			{
				if (rs != null)
				{
					_DisabledFr[frId] = rs;
				}
				else
				{
					_DisabledFr.Remove(frId);
                    if (_Registry != null)
                    {
                        _Registry.SetValue<RdSrvFrRs>(Identifiers.RdTopic, fr, rs);
                        _Registry.Publish();
                    }
#if DEBUG
                    // 20161117. AGL. Log de Evento FRECUENCIA.
                    log.Trace("1-Evento estado Frecuencia {0}: Desde {1}/{2}, {3}", frId,
                        (new StackTrace()).GetFrame(2).GetMethod().Name,
                        (new StackTrace()).GetFrame(1).GetMethod().Name,
                        rs==null ? "Poner ASPA" : "Quitar ASPA");                    
#endif
				}
			}
			else
			{
                if (_Registry != null)
                {
                    _Registry.SetValue<RdSrvFrRs>(Identifiers.RdTopic, fr, rs);
                    _Registry.Publish();
                }
#if DEBUG
                // 20161117. AGL. Log de Evento FRECUENCIA.
                log.Trace("2-Evento estado Frecuencia {0}: Desde {1}/{2}, {3}", frId,
                    (new StackTrace()).GetFrame(2).GetMethod().Name,
                    (new StackTrace()).GetFrame(1).GetMethod().Name,
                    rs == null ? "Poner ASPA" : "Quitar ASPA");
#endif
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_Frecuency"></param>
        /// <param name="_FrRs"></param>
        public static void PublishStatusFr(string fr, RdSrvFrRs rs)
        {
            if (!_Master)
                return;

            if (_Registry != null)
            {
                string frId = fr.ToUpper();

                _Registry.SetValue<RdSrvFrRs>(Identifiers.RdTopic, fr, rs);
                _Registry.Publish();
#if DEBUG
            // 20170309. JCAM. Log de Evento ESTADO FRECUENCIA FD.
            if (rs != null)
                log.Trace("3-Evento estado Frecuencia {0}: Desde {1}/{2}, {3}", frId,
                (new StackTrace()).GetFrame(2).GetMethod().Name,
                (new StackTrace()).GetFrame(1).GetMethod().Name,
                rs.FrequencyStatus);
#endif
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rsId"></param>
        /// <param name="txRs"></param>
		public static void PublishTxRs(string rsId, RdSrvTxRs txRs)
		{
            if (!_Master)
                return;

            if (_Registry != null)
            {
                _Registry.SetValue<RdSrvTxRs>(Identifiers.RdTopic, rsId, txRs);
                _Registry.Publish();

#if DEBUG
            // 20161117. AGL. LOG de Evento Recurso Radio.
            log.Warn("Evento estado Recurso RADIO {0}: Desde {1}/{2}, {3}", rsId,
                (new StackTrace()).GetFrame(2).GetMethod().Name, 
                (new StackTrace()).GetFrame(1).GetMethod().Name,
                txRs == null ? "No Disponible" : "Disponible");
#endif
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name=></param>
        /// <param name=></param>
        public static void PublishMaster(string HostId)
        {
            if (_Registry != null)
            {
                _Registry.PublishMaster(HostId, Identifiers.RdMasterTopic);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rsId"></param>
        /// <param name="rxRs"></param>
		public static void PublishRxRs(string rsId, RdSrvRxRs rxRs)
		{
            if (!_Master)
                return;

            if (_Registry != null)
            {
                _Registry.SetValue<RdSrvRxRs>(Identifiers.RdTopic, rsId, rxRs);
                _Registry.Publish();
#if DEBUG
            // 20161117. AGL. LOG de Evento Recurso Radio.
            log.Error("Evento estado Recurso RADIO {0}: Desde {1}/{2}, {3}", rsId,
                (new StackTrace()).GetFrame(2).GetMethod().Name,
                (new StackTrace()).GetFrame(1).GetMethod().Name,
                rxRs == null ? "No Disponible" : "Disponible");
#endif
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="fr"></param>
        /// <param name="tx"></param>
		public static void RespondToFrTxChange(string to, string fr, bool tx)
		{
            if (!_Master)
                return;

            if (_Registry != null)
            {
                FrChangeResponse response = new FrChangeResponse();
                response.Frecuency = fr;
                response.Set = tx;

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, response);
                byte[] data = ms.ToArray();
                _Registry.Channel.Send(Identifiers.FR_TX_CHANGE_RESPONSE_MSG, data, to);
            }
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="fr"></param>
        /// <param name="rx"></param>
		public static void RespondToFrRxChange(string to, string fr, bool rx)
		{
            if (!_Master)
                return;

            if (_Registry != null)
            {
                FrChangeResponse response = new FrChangeResponse();
                response.Frecuency = fr;
                response.Set = rx;

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, response);
                byte[] data = ms.ToArray();

                _Registry.Channel.Send(Identifiers.FR_RX_CHANGE_RESPONSE_MSG, data, to);
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="res"></param>
        /// <param name="men"></param>
        public static void RespondToPrepareSelcal(string to, string fr, bool res, string men)
        {
            if (!_Master)
                return;

            if (_Registry != null)
            {
                SelcalPrepareRsp resp = new SelcalPrepareRsp();
                resp.Frecuency = fr;
                resp.resultado = res;
                resp.Code = men;

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, resp);
                byte[] data = ms.ToArray();

                _Registry.Channel.Send(Identifiers.SELCAL_PREPARE_RSP, data, to);
            }
        }

        public static void RespondToFrHfTxChange(string to, string fr, int tx)
        {
            if (!_Master)
                return;

            if (_Registry != null)
            {
                FrChangeResponse response = new FrChangeResponse();
                response.Frecuency = fr;
                response.Set = tx == 3; // Asignado
                response.Estado = (uint)tx;

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, response);
                byte[] data = ms.ToArray();

                _Registry.Channel.Send(Identifiers.FR_HF_TX_CHANGE_RESPONSE_MSG, data, to);
            }
        }

        public static void RespondToChangingSite(string to, string fr, string alias, int num)
        {
            if (!_Master)
                return;

            if (_Registry != null)
            {
                ChangeSiteRsp response = new ChangeSiteRsp();
                response.Frecuency = fr;
                response.resultado = num > 0;
                response.Alias = alias;

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, response);
                byte[] data = ms.ToArray();

                _Registry.Channel.Send(Identifiers.SITE_CHANGING_RSP, data, to != null ? to : Identifiers.TopTopic);
                //_Registry.Channel.Send(Identifiers.SITE_CHANGING_RSP, data, Identifiers.TopTopic);
            }
        }

#if _HF_GLOBAL_STATUS_
        public static void SendHFStatus(HFStatus std)
        {
            if (!_Master)
                return;

            if (_Registry != null)
                _Registry.Send<HFStatus>(Identifiers.RdTopic, Identifiers.HF_STATUS, std);
        }
#endif
        /** 20180316. MNDISABEDNODES */
        public static void PublishMNDisabledNodes(MNDisabledNodes nodes)
        {
            if (!_Master)
                return;

#if _PUBLISH_MNDIS_
            if (_Registry != null)
            {
                _Registry.SetValue<MNDisabledNodes>(Identifiers.RdTopic, "MNDisabledNodes", nodes);
                _Registry.Publish();
            }
#else
            if (_Registry != null)
                _Registry.Send<MNDisabledNodes>(Identifiers.RdTopic, Identifiers.MNDISABLED_NODES, nodes);
#endif
        }

		#region Private Members
        /// <summary>
        /// 
        /// </summary>
        private static Logger log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		private static Registry _Registry;
        /// <summary>
        /// 
        /// </summary>
		private static GenericEventHandler<string> _OnChannelError;
        /// <summary>
        /// 
        /// </summary>
        private static GenericEventHandler<bool> _OnMasterStatusChanged;
        /// <summary>
        /// 
        /// </summary>
		private static GenericEventHandler<RsChangeInfo> _OnRsChanged;
        /// <summary>
        /// 
        /// </summary>
		private static GenericEventHandler<SpreadDataMsg> _OnMsgReceived;
        /// <summary>
        /// 
        /// </summary>
		private static Dictionary<string, RdSrvFrRs> _DisabledFr = new Dictionary<string, RdSrvFrRs>();

        /// <summary>
        /// 
        /// </summary>
		private static void InitRegistry()
		{
            _Registry = new Registry(Identifiers.RdMasterTopic);

			_Registry.ChannelError += _OnChannelError;
			_Registry.MasterStatusChanged += _OnMasterStatusChanged;
			_Registry.ResourceChanged += _OnRsChanged;
			_Registry.UserMsgReceived += _OnMsgReceived;

			_Registry.SubscribeToMasterTopic(Identifiers.RdMasterTopic);
			_Registry.SubscribeToTopic<Cd40Cfg>(Identifiers.CfgTopic);
			_Registry.SubscribeToTopic<TopRs>(Identifiers.TopTopic);
            _Registry.SubscribeToTopic<SrvMaster>(Identifiers.RdMasterTopic);

#if _PUBLISH_MNDIS_
            _Registry.SubscribeToTopic<MNDisabledNodes>(Identifiers.RdTopic);
#endif
            //_Registry.Join(Identifiers.RdMasterTopic, Identifiers.RdTopic);
            _Registry.Join(Identifiers.RdTopic, Identifiers.CfgTopic, Identifiers.TopTopic, Identifiers.RdMasterTopic);
		}

		#endregion

    }
}

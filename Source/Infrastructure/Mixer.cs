using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NLog;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
	public class Mixer
	{
        private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		public const int RD_REMOTE_PORT_ID = -10;
        /// <summary>
        /// 
        /// </summary>
		public const int UNASSIGNED_PRIORITY = -1;
        /// <summary>
        /// 
        /// </summary>
		public const int LC_PRIORITY = 15;
        /// <summary>
        /// 
        /// </summary>
		public const int RD_PRIORITY = 10;
        /// <summary>
        /// 
        /// </summary>
		public const int RD_INSTRUCTOR_PRIORITY = 9;
        /// <summary>
        /// 
        /// </summary>
		public const int RD_ALUMN_PRIORITY = 8;
        /// <summary>
        /// 
        /// </summary>
		public const int TLF_PRIORITY = 5;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="txPriority"></param>
        /// <param name="dstId"></param>
        /// <param name="rxPriority"></param>
		public void Link(int srcId, int txPriority, int dstId, int rxPriority)
		{
			Port srcPort;
			Port dstPort;

			if (!_Ports.TryGetValue(srcId, out srcPort))
			{
				srcPort = new Port(srcId);
				_Ports[srcId] = srcPort;
			}
			if (!_Ports.TryGetValue(dstId, out dstPort))
			{
				dstPort = new Port(dstId);
				_Ports[dstId] = dstPort;
			}

			if (((srcPort.TxPriority != UNASSIGNED_PRIORITY) && (txPriority > srcPort.TxPriority)) || 
				((dstPort.RxPriority != UNASSIGNED_PRIORITY) && (rxPriority > dstPort.RxPriority)))
			{
				foreach (P2PLink l in _Links)
				{
					if (l.Active &&
						(((l.Src.Id == srcId) && (l.TxPriority != UNASSIGNED_PRIORITY) && (l.TxPriority < txPriority)) ||
						((l.Dst.Id == dstId) && (l.RxPriority != UNASSIGNED_PRIORITY) && (l.RxPriority < rxPriority))))
					{
						if (l.Dst.Id == RD_REMOTE_PORT_ID)
						{
							SipAgent.UnsendToRemote(l.Src.Id);
						}
						else
						{
                            try
                            {
                                SipAgent.MixerUnlink(l.Src.Id, l.Dst.Id);
                            }
                            catch (Exception exc)
                            {
                                string msg = string.Format("ERROR MixerUnLink (link) srcId: {0:X} dstId {1:X}", l.Src.Id, l.Dst.Id);
                                _Logger.Error(msg, exc);
                            }
                        }

						l.Active = false;
					}
				}
			}

			srcPort.TxPriority = Math.Max(srcPort.TxPriority, txPriority);
			dstPort.RxPriority = Math.Max(dstPort.RxPriority, rxPriority);

			P2PLink lk = _Links.Find(delegate(P2PLink l) { return ((l.Src.Id == srcId) && (l.Dst.Id == dstId)); });

			if (lk != null)
			{
				lk.TxPriority = txPriority;
				lk.RxPriority = rxPriority;
			}
			else
			{
				lk = new P2PLink(srcPort, txPriority, dstPort, rxPriority);
				_Links.Add(lk);

				srcPort.TxLinks++;
				dstPort.RxLinks++;
			}

			if (!lk.Active &&
				((txPriority == UNASSIGNED_PRIORITY) || (txPriority == srcPort.TxPriority)) &&
				((rxPriority == UNASSIGNED_PRIORITY) || (rxPriority == dstPort.RxPriority)))
			{
				if (dstId == RD_REMOTE_PORT_ID)
				{
					SipAgent.SendToRemote(srcId, dstPort.TopId, dstPort.RdSrvListenIp, dstPort.RdSrvListenPort);
				}
				else
				{
                    try
                    {
                        SipAgent.MixerLink(srcId, dstId);
                    }
                    catch (Exception exc)
                    {
                        string msg = string.Format("ERROR MixerLink srcId: {0:X} dstId {1:X}", srcId, dstId); 
                        _Logger.Error(msg, exc);
                    }
				}

				lk.Active = true;
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="topId"></param>
        /// <param name="rdSrvListenIp"></param>
        /// <param name="rdSrvListenPort"></param>
		public void Link(int srcId, string topId, string rdSrvListenIp, uint rdSrvListenPort)
		{
			Port dstPort;
			if (!_Ports.TryGetValue(RD_REMOTE_PORT_ID, out dstPort))
			{
				dstPort = new Port(RD_REMOTE_PORT_ID, topId, rdSrvListenIp, rdSrvListenPort);
				_Ports[RD_REMOTE_PORT_ID] = dstPort;
			}

			Link(srcId, RD_PRIORITY, RD_REMOTE_PORT_ID, UNASSIGNED_PRIORITY);
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
		public void Unlink(int srcId, int dstId)
		{
			Unlink(srcId, dstId, true, true);
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
		public void Unlink(int id)
		{
			List<int> txLinksToRemove = new List<int>();
			List<int> rxLinksToRemove = new List<int>();

			foreach (P2PLink lk in _Links)
			{
				if (lk.Src.Id == id)
				{
					txLinksToRemove.Add(lk.Dst.Id);
				}
				else if (lk.Dst.Id == id)
				{
					rxLinksToRemove.Add(lk.Src.Id);
				}
			}

			foreach (int dstId in txLinksToRemove)
			{
				Unlink(id, dstId, false, true);
			}
			foreach (int srcId in rxLinksToRemove)
			{
				Unlink(srcId, id, true, false);
			}

			Debug.Assert(!_Ports.ContainsKey(id));
		}

		#region Private Members

        /// <summary>
        /// 
        /// </summary>
		class Port
		{
            /// <summary>
            /// 
            /// </summary>
			public int Id;
            /// <summary>
            /// 
            /// </summary>
			public int TxPriority = UNASSIGNED_PRIORITY;
            /// <summary>
            /// 
            /// </summary>
			public int RxPriority = UNASSIGNED_PRIORITY;
            /// <summary>
            /// 
            /// </summary>
			public int TxLinks = 0;
            /// <summary>
            /// 
            /// </summary>
			public int RxLinks = 0;
            /// <summary>
            /// 
            /// </summary>
			public string TopId;
            /// <summary>
            /// 
            /// </summary>
			public string RdSrvListenIp;
            /// <summary>
            /// 
            /// </summary>
			public uint RdSrvListenPort;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
			public Port(int id)
			{
				Debug.Assert(id >= 0);
				Id = id;
			}
            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
            /// <param name="topId"></param>
            /// <param name="rdSrvListenIp"></param>
            /// <param name="rdSrvListenPort"></param>
			public Port(int id, string topId, string rdSrvListenIp, uint rdSrvListenPort)
			{
				Debug.Assert(id < 0);
				Id = id;

				TopId = topId;
				RdSrvListenIp = rdSrvListenIp;
				RdSrvListenPort = rdSrvListenPort;
			}
		}

        /// <summary>
        /// 
        /// </summary>
		class P2PLink
		{
			public Port Src;
			public Port Dst;
			public int TxPriority;
			public int RxPriority;
			public bool Active = false;

			public P2PLink(Port src, int txPriority, Port dst, int rxPriority)
			{
				Src = src;
				Dst = dst;
				TxPriority = txPriority;
				RxPriority = rxPriority;
			}
		}

        /// <summary>
        /// 
        /// </summary>
		private Dictionary<int, Port> _Ports = new Dictionary<int, Port>();
        /// <summary>
        /// 
        /// </summary>
		private List<P2PLink> _Links = new List<P2PLink>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <param name="allowTxRecalculate"></param>
        /// <param name="allowRxRecalculate"></param>
		private void Unlink(int srcId, int dstId, bool allowTxRecalculate, bool allowRxRecalculate)
		{
			P2PLink lk = _Links.Find(delegate(P2PLink l) { return ((l.Src.Id == srcId) && (l.Dst.Id == dstId)); });
			if (lk == null)
			{
				return;
			}

			int oldTxPriority = lk.Src.TxPriority;
			int oldRxPriority = lk.Dst.RxPriority;

			bool txRecalculate = allowTxRecalculate && (lk.TxPriority != UNASSIGNED_PRIORITY) && (lk.TxPriority == lk.Src.TxPriority) && (lk.Src.TxLinks > 1);
			bool rxRecalculate = allowRxRecalculate && (lk.RxPriority != UNASSIGNED_PRIORITY) && (lk.RxPriority == lk.Dst.RxPriority) && (lk.Dst.RxLinks > 1);

			if (lk.Active)
			{
				if (lk.Dst.Id == RD_REMOTE_PORT_ID)
				{
					SipAgent.UnsendToRemote(lk.Src.Id);
				}
				else
				{
                    try
                    {
                        SipAgent.MixerUnlink(lk.Src.Id, lk.Dst.Id);
                    }
                    catch (Exception exc)
                    {
                        string msg = string.Format("ERROR MixerUnLink srcId: {0:X} dstId {1:X}", lk.Src.Id, lk.Dst.Id);
                        _Logger.Error(msg, exc);
                    }
                }
			}
			_Links.Remove(lk);

			Debug.Assert(lk.Src.TxLinks > 0);
			Debug.Assert(lk.Dst.RxLinks > 0);
			lk.Src.TxLinks--;
			lk.Dst.RxLinks--;

			if (lk.Src.TxLinks == 0)
			{
				if (lk.Src.RxLinks == 0)
				{
					_Ports.Remove(srcId);
				}
				lk.Src.TxPriority = UNASSIGNED_PRIORITY;
			}
			if (lk.Dst.RxLinks == 0)
			{
				if (lk.Dst.TxLinks == 0)
				{
					_Ports.Remove(dstId);
				}
				lk.Dst.RxPriority = UNASSIGNED_PRIORITY;
			}

			if (txRecalculate)
			{
				lk.Src.TxPriority = UNASSIGNED_PRIORITY;

				foreach (P2PLink l in _Links)
				{
					if (l.Src.Id == srcId)
					{
						lk.Src.TxPriority = Math.Max(lk.Src.TxPriority, l.TxPriority);
					}
				}
			}
			if (rxRecalculate)
			{
				lk.Dst.RxPriority = UNASSIGNED_PRIORITY;

				foreach (P2PLink l in _Links)
				{
					if (l.Dst.Id == dstId)
					{
						lk.Dst.RxPriority = Math.Max(lk.Dst.RxPriority, l.RxPriority);
					}
				}
			}

			if (((lk.Src.TxPriority != UNASSIGNED_PRIORITY) && (lk.Src.TxPriority != oldTxPriority)) ||
				((lk.Dst.RxPriority != UNASSIGNED_PRIORITY) && (lk.Dst.RxPriority != oldRxPriority)))
			{
				foreach (P2PLink l in _Links)
				{
					if (!l.Active && ((l.Src.Id == srcId) || (l.Dst.Id == dstId)) &&
						((l.TxPriority == UNASSIGNED_PRIORITY) || (l.TxPriority == l.Src.TxPriority)) &&
						((l.RxPriority == UNASSIGNED_PRIORITY) || (l.RxPriority == l.Dst.RxPriority)))
					{
						if (l.Dst.Id == RD_REMOTE_PORT_ID)
						{
							SipAgent.SendToRemote(l.Src.Id, l.Dst.TopId, l.Dst.RdSrvListenIp, l.Dst.RdSrvListenPort);
						}
						else
						{
                            try
                            {
							    SipAgent.MixerLink(l.Src.Id, l.Dst.Id);
                            }
                            catch (Exception exc)
                            {
                                string msg = string.Format("ERROR MixerUnLink srcId: {0:X} dstId {1:X}", lk.Src.Id, lk.Dst.Id);
                                _Logger.Error(msg, exc);
                            }
						}

						l.Active = true;
					}
				}
			}
		}

		#endregion
	}
}

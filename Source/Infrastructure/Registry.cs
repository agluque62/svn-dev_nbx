using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using NLog;
using ProtoBuf;
using Utilities;
using U5ki.Infrastructure.Properties;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// Contiene los datos asociados al Evento 'ResourceChanged'.
    /// Se construyen a partir del tratamiento de mensajes recibidos de 'spread'.
    /// 
    /// </summary>
	public class RsChangeInfo
	{
       public enum RsTypeInfo { NoType = 0, PhLine = 1, IcLine = 2, InternalSub = 3, ExternalSub = 4, InternalProxy = 5, InternalAltProxy = 6, ExternalProxy = 7, ExternalAltProxy = 8 }
 
        /// <summary>
        /// 
        /// </summary>
		public string ContainerId;
        /// <summary>
        /// 
        /// </summary>
		public string Topic;
        /// <summary>
        /// de este tipo RsTypeInfo
        /// </summary>
		public string Type;
        /// <summary>
        /// 
        /// </summary>
		public string Id;
        /// <summary>
        /// 
        /// </summary>
		public byte[] Content;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="topic"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="content"></param>
		public RsChangeInfo(string containerId, string topic, string type, string id, byte[] content)
		{
			ContainerId = containerId;
			Topic = topic;
			Type = type;
			Id = id;
			Content = content;
		}
	}

    /// <summary>
    /// 
    /// </summary>
	public class Registry : IDisposable
	{
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<bool> MasterStatusChanged;
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<RsChangeInfo> ResourceChanged;
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<SpreadDataMsg> UserMsgReceived;
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<string> ChannelError
		{
			add { if (_Channel!=null) _Channel.Error += value; }
            remove { if (_Channel != null) _Channel.Error -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
		public string Id
		{
			get { return _Channel!=null ? _Channel.Id : "Registry.Chanel ERROR"; }
		}
        /// <summary>
        /// 
        /// </summary>
		public SpreadChannel Channel
		{
			get { return _Channel; }
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
		public Registry(string name)
		{
            _Channel = new SpreadChannel(name);
            _Channel.DataMsg += OnChannelData;
            _Channel.MembershipMsg += OnChannelMembership;
 		}


        /// <summary>
        /// 
        /// </summary>
		~Registry()
		{
			Dispose(false);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
		public void SubscribeToTopic<T>(string topic) where T : class
		{
			lock (_SubscriberTopicInfo)
			{
				string t = Identifiers.TypeId(typeof(T));
				SubscriberTopicInfo topicInfo;

				if (!_SubscriberTopicInfo.TryGetValue(topic, out topicInfo))
				{
					topicInfo = new SubscriberTopicInfo();
					_SubscriberTopicInfo[topic] = topicInfo;
				}

				Debug.Assert(!topicInfo.SubscriberTypes.Contains(t));

				topicInfo.SubscriberTypes.Add(t);
			}
		}

        /// <summary>
        ///  Inicializa el servicio como slave
        ///  Inicializa la variable que guarda el ultimo master publicado recibido
        /// </summary>
        /// <param name="topic"></param>
		public void SubscribeToMasterTopic(string topic)
		{
			_MasterTopicInfo[topic] = false;
            _OtherPublishedMaster[topic] = NO_OTHER_MASTER_PUB;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topics"></param>
		public void Join(params string[] topics)
		{
            if (_Channel != null) _Channel.Join(topics);
		}

        /// <summary>
        /// Marca un cambio...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="instanceId"></param>
        /// <param name="rs"></param>
		public void SetValue<T>(string topic, string instanceId, T rs) where T : class
		{
			byte[] content = null;

            try
            {
			    if (rs != null)
			    {
                    MemoryStream rsMs = new MemoryStream();
                    Serializer.Serialize(rsMs, rs);
                    content = rsMs.ToArray();

                    if ((Settings.Default.CompressCfg) && (instanceId == Identifiers.CfgRsId))
                    {
                        content = Tools.Compress(content);
                    }
                }
                SetValue(topic, Identifiers.TypeId(typeof(T)), instanceId, content);
            }
            catch (Exception exc)
            {
                _Logger.Error("Exception setting  Cfg {0}", exc.Message);
            }
        }

         /// <summary>
        /// Marca un cambio... 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="type"></param>
        /// <param name="instanceId"></param>
        /// <param name="content"></param>
		public void SetValue(string topic, string type, string instanceId, byte[] content)
		{
			Debug.Assert(!string.IsNullOrEmpty(topic));

			lock (_PublishTopicInfo)
			{
				PublishTopicInfo topicInfo;
				if (!_PublishTopicInfo.TryGetValue(topic, out topicInfo))
				{
					topicInfo = new PublishTopicInfo();
					_PublishTopicInfo[topic] = topicInfo;
				}

				if (string.IsNullOrEmpty(instanceId))
				{
					if (string.IsNullOrEmpty(type))
					{
						foreach (KeyValuePair<string, RsInfo> rs in topicInfo.Resources)
						{
							RsInfo change = new RsInfo();
							change.Type = rs.Value.Type;
							change.Id = rs.Value.Id;

							topicInfo.Changes[rs.Key] = change;
						}
					}
					else
					{
						foreach (KeyValuePair<string, RsInfo> rs in topicInfo.Resources)
						{
							if (rs.Value.Type == type)
							{
								RsInfo change = new RsInfo();
								change.Type = type;
								change.Id = rs.Value.Id;

								topicInfo.Changes[rs.Key] = change;
							}
						}
					}
				}
				else
				{
					RsInfo change = new RsInfo();
					change.Type = type;
					change.Id = instanceId;
					change.Content = content;

					topicInfo.Changes[instanceId + type] = change;
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
		public void Publish()
		{
			Publish(null, true);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
		public void Publish(string ts)
		{
			Publish(ts, true);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="send"></param>
		public void Publish(bool send)
		{
			Publish(null, send);
		}

        /// <summary>
        /// Consolida todos los cambios y opcionalmente 'send=true' los envia...
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="send"></param>
		public void Publish(string ts, bool send)
		{
			lock (_PublishTopicInfo)
			{
				foreach (KeyValuePair<string, PublishTopicInfo> topic in _PublishTopicInfo)
				{
					string topicName = topic.Key;
					PublishTopicInfo topicInfo = topic.Value;

					if (topicInfo.Changes.Count > 0)
					{
						if (send)
						{
							TopicChanges topicChanges = new TopicChanges();
							topicChanges.Topic = topicName;
							topicChanges.PrevTs = topicInfo.LastTs;
							topicChanges.LastTs = topicInfo.IncrementTimestamp(ts);
							topicChanges.Changes.AddRange(topicInfo.Changes.Values);

							MemoryStream ms = new MemoryStream();
							Serializer.Serialize(ms, topicChanges);
							byte[] data = ms.ToArray();

                            if (_Channel != null) _Channel.Send(TOPIC_CHANGES_MSG_ID, data, topicName);
						}

						foreach (KeyValuePair<string, RsInfo> change in topicInfo.Changes)
						{
							if (change.Value.Content != null)
							{
								topicInfo.Resources[change.Key] = change.Value;
							}
							else
							{
								topicInfo.Resources.Remove(change.Key);
							}
						}
						topicInfo.Changes.Clear();
					}
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="messType"></param>
        /// <param name="mess"></param>
		public void Send<T>(string topic, short messType, T mess) where T : class
		{
			MemoryStream ms = new MemoryStream();
			Serializer.Serialize(ms, mess);
			byte[] data = ms.ToArray();

            if (_Channel != null) _Channel.Send(messType, data, topic);
		}
        /// <summary>
        ///  Publica que es el master, al resto de miembros presentes
        /// </summary>
        /// <param name=></param>
        /// <param name=></param>
        public void PublishMaster(String id, String topic)
        {
            SrvMaster change = new SrvMaster();
            change.HostId = id;
            _Logger.Trace("-- Publish Im Master !! topic {0} host {1}", topic, id);

            Send(topic, Identifiers.IM_MASTER_MSG, change);
        }

        #region protected
        #endregion

        #region IDisposable Members
        /// <summary>
        /// 
        /// </summary>
		public void Dispose()
		{
			Dispose(true);
            _Logger.Info("Registry {0}, Disposed.", Id);

			GC.SuppressFinalize(this);
		}

		#endregion

		#region Private Members
        /// <summary>
        /// 
        /// </summary>
		class PublishTopicInfo
		{
			private ulong _DefaultTs = 0;
			private string _LastTs = "0";
			private Dictionary<string, RsInfo> _Changes = new Dictionary<string, RsInfo>();
			private Dictionary<string, RsInfo> _Resources = new Dictionary<string, RsInfo>();

			public string LastTs
			{
				get { return _LastTs; }
			}

			public Dictionary<string, RsInfo> Changes
			{
				get { return _Changes; }
			}

			public Dictionary<string, RsInfo> Resources
			{
				get { return _Resources; }
			}

			public string IncrementTimestamp(string ts)
			{
				_DefaultTs++;
				_LastTs = ts ?? _DefaultTs.ToString();

				return _LastTs;
			}
		}

        /// <summary>
        /// 
        /// </summary>
		class PublisherInfo
		{
			private string _LastTs = "0";
			private Dictionary<string, RsInfo> _ReceivedRs = new Dictionary<string, RsInfo>();

			public string LastTs
			{
				get { return _LastTs; }
				set { _LastTs = value; }
			}

			public Dictionary<string, RsInfo> ReceivedRs
			{
				get { return _ReceivedRs; }
				set { _ReceivedRs = value; }
			}
		}

        /// <summary>
        /// 
        /// </summary>
		class SubscriberTopicInfo
		{
			private Dictionary<string, PublisherInfo> _Publishers = new Dictionary<string, PublisherInfo>();
			private List<string> _SubscriberTypes = new List<string>();

			public Dictionary<string, PublisherInfo> Publishers
			{
				get { return _Publishers; }
			}

			public List<string> SubscriberTypes
			{
				get { return _SubscriberTypes; }
			}
		}

        /// <summary>
        /// 
        /// </summary>
		private const short TOPIC_CONTENTS_MSG_ID = -1;
        /// <summary>
        /// 
        /// </summary>
		private const short TOPIC_CHANGES_MSG_ID = -2;
        /// <summary>
        /// 
        /// </summary>
		private const short TOPIC_ASK_CONTENTS_MSG_ID = -3;
        /// <summary>
        /// 
        /// </summary>
		private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		private SpreadChannel _Channel=null;
        /// <summary>
        /// Mi estado actual de master/slave
        /// </summary>
		private Dictionary<string, bool> _MasterTopicInfo = new Dictionary<string, bool>();
        /// <summary>
        /// Ultimo master publicado y recibido (no puedo ser yo mismo). Contiene el Host Id.
        /// 'None'= NO_OTHER_MASTER_PUB significa que es el valor inicial porque nadie ha publicado 
        /// que es master o que yo soy el master.
        /// </summary>
        private Dictionary<string, String> _OtherPublishedMaster = new Dictionary<string, String>();
        private const String NO_OTHER_MASTER_PUB = "None";

        private Dictionary<string, PublishTopicInfo> _PublishTopicInfo = new Dictionary<string, PublishTopicInfo>();
        /// <summary>
        /// 
        /// </summary>
		private Dictionary<string, SubscriberTopicInfo> _SubscriberTopicInfo = new Dictionary<string, SubscriberTopicInfo>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
                if (_Channel != null) _Channel.Dispose();
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
		private void OnChannelData(object sender, SpreadDataMsg msg)
		{
			try
			{
                switch (msg.Type)
				{
					case TOPIC_ASK_CONTENTS_MSG_ID:
						ProcessTopicAskContentsMsg(msg);
						break;
					case TOPIC_CONTENTS_MSG_ID:
						ProcessTopicContentsMsg(msg);
						break;
					case TOPIC_CHANGES_MSG_ID:
						ProcessTopicChangesMsg(msg);
						break;
                    case Identifiers.IM_MASTER_MSG:
                        ProcessMasterMsg(msg);
                        break;
					default:
						General.SafeLaunchEvent(UserMsgReceived, this, msg);
						break;
				}
			}
			catch (Exception ex)
			{
				if (!_Logger.IsTraceEnabled)
				{
					if (!_Logger.IsDebugEnabled)
					{
						_Logger.Debug("Recibido mensaje de datos {0}", msg);
					}
					_Logger.Trace("Contenido:{0}{1}", Environment.NewLine, new BinToLogString(msg.Data, msg.Length));
				}

				_Logger.Error("ERROR parseando mensaje", ex);
			}
		}

        /// <summary>
        /// Gestiona la entrada y salida de miembros de los grupos (JOIN)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
		private void OnChannelMembership(object sender, SpreadMembershipMsg msg)
		{
            _Logger.Debug("OnChannelMembership {2}. {1} to/from {0}", msg.Topic, msg.Change, msg.MemberChanged);
            try
            {
                EvaluaEstadoMaster(msg);

                switch (msg.Change)
                {
                    /** 
                     * Aparece un miembro nuevo en el grupo.
                     * Se envia toda la informacion asociada a ese nombre grupo, al nuevo miembro...
                     */
                    case MembershipChange.Join:
                        if (msg.MemberChanged != _Channel.Id)
                        {
                            lock (_PublishTopicInfo)
                            {
                                PublishTopicInfo topicInfo = null;
                                if (_PublishTopicInfo.TryGetValue(msg.Topic, out topicInfo))
                                {
                                    byte[] data = GetTopicContents(msg.Topic, topicInfo);
                                    if (data != null)
                                    {
                                        if (_Channel != null)
                                        {
                                            _Channel.Send(TOPIC_CONTENTS_MSG_ID, data, msg.MemberChanged);
                                            _Logger.Debug("Send TOPIC_CONTENTS_MSG_ID. Topic: {0} => {1}", msg.Topic, msg.MemberChanged);
                                        }
                                        else
                                        {
                                            _Logger.Debug("Send GetTopicContents Topic: {0} => {1} Channel == null", msg.Topic, msg.MemberChanged);
                                        }
                                    }
                                    else
                                    {
                                        _Logger.Debug("Join {1} a {0}  GetTopicContents a null", msg.Topic, msg.MemberChanged);
                                    }
                                }
                                else
                                {
                                    _Logger.Debug("Join {1} a {0} No Info en {0}", msg.Topic, msg.MemberChanged);
                                }
                            }
                        }
                        break;

                    /**
                     * Desaparece un miembro del grupo...
                     *  
                     * */
                    case MembershipChange.Leave:
                        lock (_SubscriberTopicInfo)
                        {
                            SubscriberTopicInfo subscriberTopicInfo;
                            if (_SubscriberTopicInfo.TryGetValue(msg.Topic, out subscriberTopicInfo))
                            {
                                PublisherInfo publisherInfo;
                                if (subscriberTopicInfo.Publishers.TryGetValue(msg.MemberChanged, out publisherInfo))
                                {
                                    if ((ResourceChanged != null) && 
                                        ((msg.Topic == Identifiers.TopTopic) ||
                                        //Esto pone ASPAS en las radios cuando se va el último servicio RdService (master/esclavo)
                                         (msg.Topic == Identifiers.RdTopic && subscriberTopicInfo.Publishers.Count == 1)))
                                    {
                                        foreach (RsInfo rs in publisherInfo.ReceivedRs.Values)
                                        {
                                            RsChangeInfo change = new RsChangeInfo(msg.MemberChanged, msg.Topic, rs.Type, rs.Id, null);
                                            ResourceChanged(this, change);

                                            _Logger.Debug("RES => null: {1}: {3}", msg.MemberChanged, msg.Topic, rs.Type, rs.Id);
                                        }
                                    }

                                    subscriberTopicInfo.Publishers.Remove(msg.MemberChanged);
                                }
                            }
                        }
                        break;
                    case MembershipChange.Merge:
                        lock (_PublishTopicInfo)
                        {
                            foreach (KeyValuePair<string, PublishTopicInfo> topic in _PublishTopicInfo)
                            {
                                byte[] data = GetTopicContents(topic.Key, topic.Value);
                                if (data != null)
                                {
                                    if (_Channel != null) _Channel.Send(TOPIC_CONTENTS_MSG_ID, data, topic.Key);
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _Logger.Debug("ERROR en OnChannelMembership ", ex);
              
            }
		}

        /// <summary>
        /// Evalua mi estado master/slave según los mensajes de memberShip recibidos.
        /// -Determina si el master se ha ido
        /// -Publico si soy master si alguien nuevo entra
        /// -Decido si cambio a master, cuando nadie ha publicado, y soy el primero 
        /// -Decido si cambio a slave, cuando hay otro master y yo no soy el primero
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void EvaluaEstadoMaster(SpreadMembershipMsg msg)
        {
            bool masterInfo;
            String otherPublished;
            String myId = Id.Split('#')[2];
            if (!_MasterTopicInfo.TryGetValue(msg.Topic, out masterInfo))
                return;
            switch (msg.Change)
            {
                case MembershipChange.Leave:
                    if (_OtherPublishedMaster.TryGetValue(msg.Topic, out otherPublished))
                    {
                        String[] subStrings = msg.MemberChanged.Split('#');
                        if (subStrings[2] == _OtherPublishedMaster[msg.Topic])
                        {
                            _OtherPublishedMaster[msg.Topic] = NO_OTHER_MASTER_PUB;
                            _Logger.Trace("-- Master Left!! topic {0}", msg.Topic);
                        }
                    }
                    break;
                case MembershipChange.Join:
                    //Publico si soy master, para que el nuevo miembro lo sepa
                    if (_OtherPublishedMaster.TryGetValue(msg.Topic, out otherPublished))
                    {
                        if (masterInfo == true)
                        {
                            //Si hay conflicto de master, solo publico si soy el primero
                            if ((otherPublished == NO_OTHER_MASTER_PUB) ||
                            ((otherPublished != NO_OTHER_MASTER_PUB) && (msg.FirstForMaster == true)))
                            {
                                PublishMaster(myId, msg.Topic);
                                _OtherPublishedMaster[msg.Topic] = NO_OTHER_MASTER_PUB;
                            }
                        }
                    }
                    break;
                default:
                    //do nothing
                    break;
            }

            if (_OtherPublishedMaster.TryGetValue(msg.Topic, out otherPublished))
            {
                if ((otherPublished == NO_OTHER_MASTER_PUB) && (masterInfo == false) && (msg.FirstForMaster == true))
                {
                    _MasterTopicInfo[msg.Topic] = msg.FirstForMaster;
                    _Logger.Trace("-- I become Master !! topic {0}", msg.Topic);
                    PublishMaster(myId, msg.Topic);
                    General.SafeLaunchEvent(MasterStatusChanged, msg.Topic, true);
                }
                else if ((otherPublished != NO_OTHER_MASTER_PUB) && (masterInfo == true) && (msg.FirstForMaster == false))
                {
                    //Conflicto entre dos master: se resuelve con el primero de la lista de precedencia que llega en msg
                    _MasterTopicInfo[msg.Topic] = false;
                    _Logger.Trace("-- I become Slave !! topic {0}", msg.Topic);
                    General.SafeLaunchEvent(MasterStatusChanged, msg.Topic, msg.FirstForMaster);
                }
                // En cualquier otro caso no cambio de estado
            }

        }

        /// <summary>
        /// Procesa un mensaje, en el que se solicita la informacion relativa a un 'topic' / o grupo
        /// </summary>
        /// <param name="msg"></param>
		private void ProcessTopicAskContentsMsg(SpreadDataMsg msg)
		{
			MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
			TopicAskContents ask = Serializer.Deserialize<TopicAskContents>(ms);

			lock (_PublishTopicInfo)
			{
				PublishTopicInfo topicInfo = null;
				TopicContents topicContents = new TopicContents();

				topicContents.Topic = ask.Topic;

				if (_PublishTopicInfo.TryGetValue(ask.Topic, out topicInfo))
				{
					topicContents.LastTs = topicInfo.LastTs;
					topicContents.Resources.AddRange(topicInfo.Resources.Values);
				}
				else
				{
					topicContents.LastTs = "Nothing";
				}

				ms = new MemoryStream();
				Serializer.Serialize(ms, topicContents);

                if (_Channel != null) _Channel.Send(TOPIC_CONTENTS_MSG_ID, ms.ToArray(), msg.From);
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
		private void ProcessTopicContentsMsg(SpreadDataMsg msg)
		{
            if (ResourceChanged != null)
            {
                MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                TopicContents topicContents = Serializer.Deserialize<TopicContents>(ms);

                lock (_SubscriberTopicInfo)
                {
                    SubscriberTopicInfo topicInfo;

                    if (_SubscriberTopicInfo.TryGetValue(topicContents.Topic, out topicInfo))
                    {
                        PublisherInfo publisherInfo;

                        if (!topicInfo.Publishers.TryGetValue(msg.From, out publisherInfo))
                        {
                            publisherInfo = new PublisherInfo();
                            topicInfo.Publishers[msg.From] = publisherInfo;
                        }

                        // Si se recibe este mensaje como consecuencia de que el puesto recupera la conexión de red
                        // será preciso que actualice la informacion aunque el if sea false
                        if (topicContents.LastTs != publisherInfo.LastTs)
                        {
                            publisherInfo.LastTs = topicContents.LastTs;

                            Dictionary<string, RsInfo> rsRemoved = publisherInfo.ReceivedRs;
                            publisherInfo.ReceivedRs = new Dictionary<string, RsInfo>();

                            foreach (RsInfo rs in topicContents.Resources)
                            {
                                Debug.Assert(!string.IsNullOrEmpty(rs.Type));
                                Debug.Assert(!string.IsNullOrEmpty(rs.Id));
                                Debug.Assert(rs.Content != null);

                                if (topicInfo.SubscriberTypes.Contains(rs.Type))
                                {
                                    RsChangeInfo change = new RsChangeInfo(msg.From, topicContents.Topic, rs.Type, rs.Id, rs.Content);
                                    ResourceChanged(this, change);

                                    _Logger.Debug("RES-SET => {4} FROM {0}. {1}: {3}", msg.From, topicContents.Topic, rs.Type, rs.Id, rs.Content == null ? "null" : "VAL");

                                    rs.Content = null;
                                    publisherInfo.ReceivedRs[rs.Id + rs.Type] = rs;
                                    rsRemoved.Remove(rs.Id + rs.Type);
                                }
                                else
                                {
                                    _Logger.Debug("topicInfo.SubscriberTypes.Contains(rs.Type): {0}, {1}", topicContents.Topic, rs.Type);
                                }
                            }
                            foreach (RsInfo rs in rsRemoved.Values)
                            {
                                RsChangeInfo change = new RsChangeInfo(msg.From, topicContents.Topic, rs.Type, rs.Id, null);
                                ResourceChanged(this, change);
                            }
                        }
                        else
                        {
                            _Logger.Debug("topicContents.LastTs != publisherInfo.LastTs: {0}", topicContents.Topic);
                        }
                    }
                    else
                    {
                        _Logger.Debug("No suscrito");
                    }
                }
            }
            else
            {
                _Logger.Debug("");
            }
		}

        /// <summary>
        /// Guarda el host recibido como master
        /// Si hay conflicto de master y no soy el primero, cambio a slave
        /// </summary>
        /// <param name="msg"></param>
        private void ProcessMasterMsg(SpreadDataMsg msg)
        {
            _Logger.Trace("--ProcessMasterMsg from {0}", msg.From);
            MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
            SrvMaster masterMsg = Serializer.Deserialize<SrvMaster>(ms);
            String[] subStrings = msg.From.Split('#');
            _OtherPublishedMaster[subStrings[1]] = masterMsg.HostId;
            if ((_MasterTopicInfo[subStrings[1]] == true) && (msg.FirstForMaster == false))
            {
                //Conflicto entre dos master: se resuelve con la prioridad que llega en msg firstForMaster
                _MasterTopicInfo[subStrings[1]] = false;
                _Logger.Trace("--ProcessMasterMsg: I become Slave !! topic {0}", subStrings[1]);
                General.SafeLaunchEvent(MasterStatusChanged, subStrings[1], msg.FirstForMaster);
            }
            else if ((_MasterTopicInfo[subStrings[1]] == true) && (msg.FirstForMaster == true))
            {
                _Logger.Trace("--ProcessMasterMsg: refresco master !! topic {0}", subStrings[1]);
                General.SafeLaunchEvent(MasterStatusChanged, subStrings[1], msg.FirstForMaster);

            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
		private void ProcessTopicChangesMsg(SpreadDataMsg msg)
		{
			if (ResourceChanged != null)
			{
				MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
				TopicChanges topicChanges = Serializer.Deserialize<TopicChanges>(ms);

				lock (_SubscriberTopicInfo)
				{
					SubscriberTopicInfo topicInfo;

					if (_SubscriberTopicInfo.TryGetValue(topicChanges.Topic, out topicInfo))
					{
						PublisherInfo publisherInfo;

						if (!topicInfo.Publishers.TryGetValue(msg.From, out publisherInfo))
						{
							publisherInfo = new PublisherInfo();
							topicInfo.Publishers[msg.From] = publisherInfo;
						}

						if (topicChanges.LastTs != publisherInfo.LastTs)
						{
							if (topicChanges.PrevTs != publisherInfo.LastTs)
							{
								TopicAskContents topicAskContents = new TopicAskContents();
								topicAskContents.Topic = topicChanges.Topic;

								MemoryStream askMs = new MemoryStream();
								Serializer.Serialize(askMs, topicAskContents);

                                if (_Channel != null) _Channel.Send(TOPIC_ASK_CONTENTS_MSG_ID, askMs.ToArray(), msg.From);
							}
							else
							{
								publisherInfo.LastTs = topicChanges.LastTs;

								foreach (RsInfo rs in topicChanges.Changes)
								{
									if (topicInfo.SubscriberTypes.Contains(rs.Type))
									{
										RsChangeInfo change = new RsChangeInfo(msg.From, topicChanges.Topic, rs.Type, rs.Id, rs.Content);
										ResourceChanged(this, change);

                                        _Logger.Debug("RES-CHG => {4} FROM {0}. {1}: {3}", msg.From, topicChanges.Topic, rs.Type, rs.Id, rs.Content == null ? "null" : "VAL");

										if (rs.Content != null)
										{
											rs.Content = null;
											publisherInfo.ReceivedRs[rs.Id + rs.Type] = rs;
										}
										else
										{
											publisherInfo.ReceivedRs.Remove(rs.Id + rs.Type);
										}
                                    }
								}
							}
						}
					}
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="topicInfo"></param>
        /// <returns></returns>
		private byte[] GetTopicContents(string topic, PublishTopicInfo topicInfo)
		{
			if (topicInfo.Resources.Count > 0)
			{
				TopicContents topicContents = new TopicContents();
				topicContents.Topic = topic;
				topicContents.LastTs = topicInfo.LastTs;
				topicContents.Resources.AddRange(topicInfo.Resources.Values);

				MemoryStream ms = new MemoryStream();
				Serializer.Serialize(ms, topicContents);
				return ms.ToArray();
			}

			return null;
		}

		#endregion
	}
}

//#define _SPREAD_ALL_MEMBERS_
using System;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

using NLog;
using Utilities;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
	public enum MembershipChange { Join, Leave, Merge}
    /// <summary>
    /// 
    /// </summary>
	public enum Qos { Unreliable = 1, Reliable = 2, Fifo = 4, Causal = 8, Agreed = 16, Safe = 32 }

    /// <summary>
    /// 
    /// </summary>
	public class SpreadDataMsg
	{
		public string From;
		public short Type;
		public byte[] Data;
		public int Length;
		public string To;
        public bool FirstForMaster;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <param name="to"></param>
        /// <param name="firstForMaster"> Indica si soy el primero en caso de conflicto de masters
        ///  Solo se utiliza en el tratamiento del mensaje IM_MASTER_MSG</param>
		public SpreadDataMsg(string from, short type, byte[] data, int length, string to, bool firstForMaster = false)
		{
			From = from;
			Type = type;
			Data = data;
			Length = length;
			To = to;
            FirstForMaster = firstForMaster;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
            return string.Format("[From={0}] [Type={1}] [To={2}] [FirstForMaster={3}]", From, Type, To, FirstForMaster);
		}
	}
    /// <summary>
    /// 
    /// </summary>
	public class SpreadMembershipMsg
	{
		public string Topic;
		public bool FirstForMaster;
		public MembershipChange Change;
		public string MemberChanged;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="firstForMaster">Indica si soy el primero en caso de conflicto de masters</param>
        /// <param name="change"></param>
        /// <param name="memberChanged"></param>
        public SpreadMembershipMsg(string topic, bool firstForMaster, MembershipChange change, string memberChanged)
		{
			Topic = topic;
            FirstForMaster = firstForMaster;
			Change = change;
			MemberChanged = memberChanged;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return string.Format("[Topic={0}] [Master={1}] [Change={2}] [MemberChanged={3}]", Topic, FirstForMaster, Change, MemberChanged);
		}
	}
    /// <summary>
    /// 
    /// </summary>
	public class SpreadChannel : IDisposable
	{
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<string> Error;
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<SpreadDataMsg> DataMsg;
        /// <summary>
        /// 
        /// </summary>
		public event GenericEventHandler<SpreadMembershipMsg> MembershipMsg;
        /// <summary>
        /// 
        /// </summary>
		public string Id
		{
			get { return _Id; }
		}
        /// <summary>
        /// 
        /// </summary>
		public bool Connected
		{
			get { return _Connected; }
		}

#if DEBUG_TIME
        /// <summary>
        /// Atributo para realizar medidas de tiempo para debug
        /// </summary>
        public TimeMeasurement timeMeasure = null;
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
		public SpreadChannel(string name)
		{
			Debug.Assert(name.Length <= MAX_PRIVATE_NAME);

            Trace(name, "{0}, New SpreadChannel.", name);
			
            string host = "4803";
			if (Environment.OSVersion.Platform != PlatformID.Unix)
			{
				host += "@localhost";
			}

            group_name id;
			sp_time tm = new sp_time(1, 0);

            int res = SP_connect_timeout(host, name, 0, 1, out _SpreadHandle, out id, tm);
			if (ACCEPT_SESSION != res)
			{
				throw new Exception(string.Format("ERROR {0} conectando al servidor spread. {1}##{2}",res,host,name));
			}

            Trace(name, "{0}::{2}. Conectado en: host={1}.", name, host, id.Name);

            _Name = name;
            _Id = id.Name;
			_Connected = true;

			_ReceiveThread = new Thread(ReceiveThread);
            _ReceiveThread.IsBackground = true;
			_ReceiveThread.Start();
#if _SPREAD_ALL_MEMBERS_
            string[] names = _Id.Split('#');
            _Pict = (names.Length >= 3 ? names[2] : "PICT???").ToUpper();
            try
            {
                prioridades = new SpreadConf().SpreadPriorities();
            }
            catch (Exception x)
            {
                prioridades = new Dictionary<string, int>();
                _Logger.Error("SpreadChannel Exception {0}", x.Message);
            }
#endif
        }
        /// <summary>
        /// 
        /// </summary>
		~SpreadChannel()
		{
			Dispose(false);
            Trace(_Name, "Un Destructor en c#????");
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topics, nombre del grupo"></param>
		public void Join(params string[] topics)
		{
			foreach (string topic in topics)
			{
                _PrecedingMembers[topic] = new List<string>();
                //Creo una lista de miembros presentes en la red para cada grupo
                _PresentMembersInNetwork[topic] = new List<string>();
#if _SPREAD_ALL_MEMBERS_
                _AllMembers[topic] = new List<string>();
#endif

                Debug.Assert(topic.Length < MAX_GROUP_NAME);
				if (SP_join(_SpreadHandle, topic) != 0)
				{
					throw new Exception("ERROR subscribiendose al topic " + topic);
				}

                Trace(_Name, "{0}::{1}<->{2} JOIN",_Name, _Id, topic);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messType"></param>
        /// <param name="mess"></param>
        /// <param name="topic"></param>
		public void Send(short messType, byte[] mess, string topic)
		{
            Send(messType, mess, topic, Qos.Fifo);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messType"></param>
        /// <param name="mess"></param>
        /// <param name="topic"></param>
        /// <param name="quality"></param>
		public void Send(short messType, byte[] mess, string topic, Qos quality)
		{
			group_name[] groups = new group_name[1];
			groups[0].Name = topic;

            Trace(_Name, "SEND {1}-->{2} TIPO {3}: {4}",
                _Name, _Id, topic, messType, BitConverter.ToString(mess, 0, mess.Length > 16 ? 16 : mess.Length));

            int err = SP_multigroup_multicast(_SpreadHandle, (int)quality | SELF_DISCARD, groups.Length, groups, messType, mess.Length, mess);
			if ( err < 0)
			{
				throw new Exception("ERROR enviando mensaje [Type: " + messType + ", Error: " + err + ", LMS: "+ mess.Length + " ]");
            }
#if DEBUG_TIME
            if (timeMeasure != null)
            {
                timeMeasure.StopAndPrint();
                timeMeasure = null;
            }
#endif
        }

		#region IDisposable Members
        /// <summary>
        /// 
        /// </summary>
		public void Dispose()
		{
			Dispose(true);
            _Logger.Info("SpreadChannel {0}, Disposed.", Id);

            GC.SuppressFinalize(this);
            Trace(_Name, "Dispose desde Fuera....");
		}

		#endregion

		#region Dll Interface

        /// <summary>
        /// 
        /// </summary>
		const int MAX_GROUP_NAME = 45;
        /// <summary>
        /// 
        /// </summary>
		const int MAX_PRIVATE_NAME = 10;
        /// <summary>
        /// 
        /// </summary>
		const int ACCEPT_SESSION = 1;
        /// <summary>
        /// 
        /// </summary>
		const int ILLEGAL_MESSAGE = -13;
        /// <summary>
        /// 
        /// </summary>
		const int BUFFER_TOO_SHORT = -15;
        /// <summary>
        /// 
        /// </summary>
		const int GROUPS_TOO_SHORT = -16;
        /// <summary>
        /// 
        /// </summary>
		const int UNRELIABLE_MESS = 0x00000001;
        /// <summary>
        /// 
        /// </summary>
		const int RELIABLE_MESS = 0x00000002;
        /// <summary>
        /// 
        /// </summary>
		const int FIFO_MESS = 0x00000004;
        /// <summary>
        /// 
        /// </summary>
		const int CAUSAL_MESS = 0x00000008;
        /// <summary>
        /// 
        /// </summary>
		const int AGREED_MESS = 0x00000010;
        /// <summary>
        /// 
        /// </summary>
		const int SAFE_MESS = 0x00000020;
        /// <summary>
        /// 
        /// </summary>
		const int REGULAR_MESS = 0x0000003f;
        /// <summary>
        /// 
        /// </summary>
		const int REG_MEMB_MESS = 0x00001000;
        /// <summary>
        /// 
        /// </summary>
		const int TRANSITION_MESS = 0x00002000;
        /// <summary>
        /// 
        /// </summary>
		const int CAUSED_BY_JOIN = 0x00000100;
        /// <summary>
        /// 
        /// </summary>
		const int CAUSED_BY_LEAVE = 0x00000200;
        /// <summary>
        /// 
        /// </summary>
		const int CAUSED_BY_DISCONNECT = 0x00000400;
        /// <summary>
        /// 
        /// </summary>
		const int CAUSED_BY_NETWORK = 0x00000800;
        /// <summary>
        /// 
        /// </summary>
		const int MEMBERSHIP_MESS = 0x00003f00;
        /// <summary>
        /// 
        /// </summary>
		const int REJECT_MESS = 0x00400000;
        /// <summary>
        /// 
        /// </summary>
		const int SELF_DISCARD = 0x00000040;
        /// <summary>
        /// 
        /// </summary>
		const int DROP_RECV = 0x01000000;

        /// <summary>
        /// 
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct sp_time
		{
			public int Sec;
			public int Usec;

			public sp_time(int sec, int usec)
			{
				Sec = sec;
				Usec = usec;
			}
		}
        /// <summary>
        /// 
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct group_name
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_GROUP_NAME)]
			public string Name;
		}
        /// <summary>
        /// 
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct vs_set_info
		{
			public int NumMembers;
			public int MembersOffset;
		}
        /// <summary>
        /// 
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		class membership_info
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] Gid;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_GROUP_NAME)]
			public string ChangedMember;

			public int NumVsSets;
			public vs_set_info MyVsSet;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spread_name"></param>
        /// <param name="private_name"></param>
        /// <param name="priority"></param>
        /// <param name="group_membership"></param>
        /// <param name="mbox"></param>
        /// <param name="private_group"></param>
        /// <param name="time_out"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_connect_timeout(string spread_name, string private_name, int priority,
			int group_membership, out int mbox, [Out] out group_name private_group, sp_time time_out);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_disconnect(int mbox);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <param name="group"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_join(int mbox, string group);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <param name="group"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_leave(int mbox, string group);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <param name="service_type"></param>
        /// <param name="num_groups"></param>
        /// <param name="groups"></param>
        /// <param name="mess_type"></param>
        /// <param name="mess_len"></param>
        /// <param name="mess"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_multigroup_multicast(int mbox, int service_type, int num_groups,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] group_name[] groups, short mess_type, int mess_len,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] mess);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <param name="service_type"></param>
        /// <param name="sender"></param>
        /// <param name="max_groups"></param>
        /// <param name="num_groups"></param>
        /// <param name="groups"></param>
        /// <param name="mess_type"></param>
        /// <param name="endian_mismatch"></param>
        /// <param name="max_mess_len"></param>
        /// <param name="mess"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_receive(int mbox, ref int service_type, [Out] out group_name sender, int max_groups,
			out int num_groups,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] group_name[] groups,
			ref short mess_type, ref int endian_mismatch, int max_mess_len,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] byte[] mess);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memb_mess"></param>
        /// <param name="service_type"></param>
        /// <param name="memb_info"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_get_memb_info(byte[] memb_mess, int service_type, [Out] membership_info memb_info);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memb_mess"></param>
        /// <param name="vs_sets"></param>
        /// <param name="num_vs_sets"></param>
        /// <param name="my_vs_set_index"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_get_vs_sets_info(byte[] memb_mess,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] vs_set_info[] vs_sets,
			int num_vs_sets, out int my_vs_set_index);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memb_mess"></param>
        /// <param name="vs_set"></param>
        /// <param name="members"></param>
        /// <param name="member_names_count"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_get_vs_set_members(byte[] memb_mess, [In] ref vs_set_info vs_set,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] group_name[] members,
			int member_names_count);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mbox"></param>
        /// <returns></returns>
		[DllImport("libspread", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int SP_poll(int mbox);

		#endregion

		#region Private Members
        /// <summary>
        /// 
        /// </summary>
		private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		private bool _Connected = false;
        /// <summary>
        /// 
        /// </summary>
		private Thread _ReceiveThread;
        /// <summary>
        /// 
        /// </summary>
		private int _SpreadHandle;
        /// <summary>
        /// 
        /// </summary>
		private string _Id;
#if _SPREAD_ALL_MEMBERS_
        private string _Pict;
#endif
        /// <summary>
        /// 
        /// </summary>
        private string _Name;
        /// <summary>
        /// Lista que guarda el orden de elementos que me preceden en el spread. 
        /// Se utiliza para dar prioridad al primero de la lista en caso de conflicto entre masters
        /// </summary>
        private Dictionary<string, List<string>> _PrecedingMembers = new Dictionary<string, List<string>>();
        /// <summary>
        /// List of present members of each group, that is updated with join, leave and caused by network
        /// </summary>
        private Dictionary<string, List<string>> _PresentMembersInNetwork = new Dictionary<string, List<string>>();
#if _SPREAD_ALL_MEMBERS_
        private Dictionary<string, List<string>> _AllMembers = new Dictionary<string, List<string>>();
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
		private void Dispose(bool disposing)
		{
			if (_Connected)
			{
				_Connected = false;
				SP_disconnect(_SpreadHandle);
                _Logger.Info("SpreadChannel {0}, SP_disconnect.", Id);
            }

			if (disposing)
			{
				if (_ReceiveThread != null)
				{
					_ReceiveThread.Join();
					_ReceiveThread = null;
                    _Logger.Info("SpreadChannel {0}, _ReceiveThread=null.", Id);
                }
			}

            Trace(_Name, "{0}::{1} SpreadChannel Finalizado...", _Name, _Id);
        }

        /// <summary>
        /// 
        /// </summary>
		private void ReceiveThread()
		{
            
            try
			{
                group_name[] groups = new group_name[200];
                byte[] buffer = new byte[150000];

				while (_Connected)
				{
                    int serviceType = 0;
                    group_name group;
                    int numGroups;
                    short messType = 0;
                    int endianMismatch = 0;

                    // Mas información: http://www.spread.org/docs/spread_docs_4/docs/sp_receive.html
					int ret = SP_receive(_SpreadHandle, ref serviceType, out group, groups.Length, out numGroups, groups,
						ref messType, ref endianMismatch, buffer.Length, buffer);

                    Trace(_Name, "{1}<--{2} MEN. RECIBIDO, numGroups={3}, messType={4}, data={5}", 
                        _Name, _Id, group.Name, numGroups, messType, BitConverter.ToString(buffer, 0, buffer.Length > 16 ? 16 : buffer.Length));
                    
                    if (ret < 0)
					{
						Debug.Assert(ret != GROUPS_TOO_SHORT);
						Debug.Assert(ret != BUFFER_TOO_SHORT);

						if ((ret != ILLEGAL_MESSAGE) && _Connected) 
						{
							throw new Exception("ERROR obteniendo mensaje: " + ret);
						}
					}
					else if ((serviceType & REJECT_MESS) != 0)
					{
						_Logger.Warn("Mensaje rechazado [Type: {0}]", messType);
                        Trace(_Name, "..... Mensaje rechazado [Type: {0}]", messType);
                    }
					else if ((serviceType & REGULAR_MESS) != 0)
					{
                        bool firstForMaster = false;
                        List<string> precedingMemb;
                        if (_PrecedingMembers.TryGetValue(groups[0].Name, out precedingMemb))
                           firstForMaster = (precedingMemb.Count == 0);

                        /** 20180404. AGL. A la clase SpreadDataMsg, no se le puede pasar directamente el buffer de recepcion
                          porque la referencia que se inserta se pasa a otros THREAD via (Thread.Enqueue), y a la vez, queda libre
                          para ser utilizada por este thread en otra recepcion con el consiguiente 'machaque' de datos. */
                        // SpreadDataMsg msg = new SpreadDataMsg(group.Name, messType, buffer, ret, groups[0].Name, firstForMaster);
                        byte[] localbuffer = new byte[ret + 1];
                        Array.Copy(buffer, 0, localbuffer, 0, ret);
                        SpreadDataMsg msg = new SpreadDataMsg(group.Name, messType, localbuffer, ret, groups[0].Name, firstForMaster);

                        Trace(_Name, "..... Decodifica Mensaje de datos: {0}, {1}", msg.Length, BitConverter.ToString(msg.Data, 0, ret > 32 ? 32 : ret));

						General.SafeLaunchEvent(DataMsg, this, msg);
					}
					else if ((serviceType & MEMBERSHIP_MESS) != 0)
                    {
#if _SPREAD_ALL_MEMBERS_
                        membership_info info = new membership_info();
                        // Mas Informacion: http://www.spread.org/docs/spread_docs_4/docs/SP_get_memb_info.html
                        int err = SP_get_memb_info(buffer, serviceType, info);
                        if (err <= 0)
                        {
                            throw new Exception(String.Format("Error {0}, Grupo {1} en SP_get_memb_info...", err, group.Name));
                        }
#endif
                        if ((serviceType & REG_MEMB_MESS) != 0)
                        {
                            /** AGL */
                            Trace(_Name, "..... Decodifica MEMBERSHIP_MESS & REG_MEMB_MESS del Grupo: {0}", group.Name);

                            List<string> precedingMembers = _PrecedingMembers[group.Name];
                            List<string> presentMembers = _PresentMembersInNetwork[group.Name];
#if _SPREAD_ALL_MEMBERS_
                            List<string> actualMembers = new List<string>();
#endif
                            MembershipChange change = MembershipChange.Merge;
                            string changedMember = null;

                            PrintListaRecibida(buffer);

                            if ((serviceType & CAUSED_BY_NETWORK) != 0)
                            {
                                /** AGL */
                                Trace(_Name, "..... Decodifica CAUSED_BY_NETWORK del Grupo: " + group.Name);

                                vs_set_info[] vsSets = new vs_set_info[128];
                                int myVsSetIndex;
          
                                // Mas Informacion: http://www.spread.org/docs/spread_docs_4/docs/sp_get_vs_sets_info.html
                                int numVsSets = SP_get_vs_sets_info(buffer, vsSets, vsSets.Length, out myVsSetIndex);
                                if ((numVsSets <= 0) || (myVsSetIndex >= numVsSets))
                                {
                                    Trace(_Name, "..... Decodifica Mensaje invalido [MEMBERSHIP_CAUSED_BY_MERGE]");
                                    throw new Exception("Mensaje invalido [MEMBERSHIP_CAUSED_BY_MERGE]");
                                }


                                precedingMembers.Clear();
                                Trace(_Name, "precedingMembers.Clear\n");
#if _SPREAD_ALL_MEMBERS_
                                for (int i = 0; i <= numVsSets; i++)
#else
                                for (int i = 0; i <= myVsSetIndex; i++)
#endif
                                {
                                    group_name[] members = new group_name[vsSets[i].NumMembers];
                                    ret = SP_get_vs_set_members(buffer, ref vsSets[i], members, members.Length);
                                    if (ret < 0)
                                    {
                                        Trace(_Name, "VS Set has more then {0} members. Recompile with larger MAX_MEMBERS\n", members.Length);
                                    }

                                    bool _encontrado = false;
                                    foreach (group_name member in members)
                                    {
                                        if ((i == myVsSetIndex) && (member.Name == _Id))
                                        {
                                            _encontrado = true;// break;
                                        }

                                        if (!_encontrado)
                                        {
                                            precedingMembers.Add(member.Name);
                                            Trace(_Name, "precedingMembers.Add({0})\n", member.Name);
                                        }
#if _SPREAD_ALL_MEMBERS_
                                        actualMembers.Add(member.Name);
#endif
                                    }

                                }
                                SearchPresentMembers(buffer, group, vsSets, numVsSets, precedingMembers.Count == 0);
                            }
                            else
                            {
                                Trace(_Name, "..... Decodifica MEMBERSHIP_CAUSED_BY_JOIN/LEAVE/DISCONNECT del Grupo: {0}", group.Name);

#if !_SPREAD_ALL_MEMBERS_
                                membership_info info = new membership_info();

                                // Mas Informacion: http://www.spread.org/docs/spread_docs_4/docs/SP_get_memb_info.html
                                int err = SP_get_memb_info(buffer, serviceType, info);
                                if ( err <= 0)
                                {
                                    throw new Exception(String.Format("Error {0}, Grupo {1} en SP_get_memb_info...", err, group.Name));
                                }
#endif

                                changedMember = info.ChangedMember;
                                if ((serviceType & CAUSED_BY_JOIN) != 0)
                                {
                                    /** AGL */
                                    Trace(_Name, "..... Decodifica MEMBERSHIP_CAUSED_BY_JOIN del Grupo= {0}, Miembro nuevo= {1}",
                                        group.Name, info.ChangedMember);

                                    change = MembershipChange.Join;
                                    if (changedMember == _Id)
                                    {
                                        Debug.Assert(precedingMembers.Count == 0);      // En 'groups' viene la lista actual de Miembros.
                                        Debug.Assert(messType < numGroups);             // messType representa nuestro indice en groups
                                        for (short i = 0; i < numGroups; i++)           // OJO. No habría que resetear la lista (por si acaso).
                                        {
                                            if (i != messType)                          // OJO. Si se intenta tener los que nos preceden, la condicion deberia ser (i < messType) ???
                                            {
                                                precedingMembers.Add(groups[i].Name);
                                                Trace(_Name, "precedingMembers.Add({0})\n", groups[i].Name);
                                            }
                                             //Significa que yo he entrado en la red, tengo que guardar todos los miembros ya presentes en la red
                                            if (!presentMembers.Contains(groups[i].Name))
                                            {
                                                presentMembers.Add(groups[i].Name);
                                                Trace(_Name, "{0} presente J i", groups[i].Name);
                                            }
                                        }
                                    }
                                    //ha entrado un nuevo miembro en la red
                                    else if (!presentMembers.Contains(changedMember))
                                    {
                                        presentMembers.Add(changedMember);
                                        Trace(_Name, "{0} presente J", changedMember);
                                    }
                            }
                                else
                                {
                                    /** AGL */
                                    Trace(_Name, "..... Decodifica MEMBERSHIP_CAUSED_BY_LEAVE OR DISCONNET del Grupo= {0}, Miembro= {1}",
                                        group.Name, info.ChangedMember);

                                    Debug.Assert(changedMember != _Id);

                                    change = MembershipChange.Leave;
                                    precedingMembers.Remove(info.ChangedMember);
                                    Trace(_Name, "precedingMembers.Rem({0})\n", info.ChangedMember);
                                    //ha salido un miembro de la red
                                    if (presentMembers.Contains(changedMember))
                                    {
                                        presentMembers.Remove(changedMember);
                                        Trace(_Name, "{0} no presente L", changedMember);
                                    }

                                }
#if _SPREAD_ALL_MEMBERS_
                                for (short i = 0; i < numGroups; i++)
                                {
                                    actualMembers.Add(groups[i].Name);
                                }
#endif
                            }
#if _SPREAD_ALL_MEMBERS_

                            bool _master = SoyMaster(actualMembers);

                            if (change == MembershipChange.Merge)
                            {
                                List<Pair<MembershipChange, string>> cambios = new List<Pair<MembershipChange,string>>();
                                ProccessGroup(group.Name, actualMembers, ref cambios);
                                foreach (Pair<MembershipChange, string> cambio in cambios)
                                {
                                    SpreadMembershipMsg msg = new SpreadMembershipMsg(group.Name, _master, cambio.First, cambio.Second);
                                    General.SafeLaunchEvent(MembershipMsg, this, msg);
                                    Trace(_Name, "..... EVENT.MembershipMsg Topic: {0}, Master({1}), {2}: {3}", group.Name, msg.Master, cambio.First, cambio.Second);
                                }
                            }
                            else
#endif
                            {
#if _SPREAD_ALL_MEMBERS_
                                SpreadMembershipMsg msg = new SpreadMembershipMsg(group.Name, _master, change, changedMember);
#else

                                SpreadMembershipMsg msg = new SpreadMembershipMsg(group.Name, precedingMembers.Count == 0, change, changedMember);
                                LogMemberGroup(_Name, group.Name, precedingMembers);
#endif
                                General.SafeLaunchEvent(MembershipMsg, this, msg);

                                Trace(_Name, "..... EVENT.MembershipMsg Topic: {0}, FirstForMaster({1}), {2}: {3}",
                                    group.Name, msg.FirstForMaster, msg.Change, msg.MemberChanged);
                            }
#if _SPREAD_ALL_MEMBERS_
                            LogMemberGroup(_Name, group.Name, actualMembers);
                            _AllMembers[group.Name] = actualMembers;
#endif
                        }
                        else
                        {
                            if ((serviceType & TRANSITION_MESS)!=0)
                                Trace(_Name, "..... Decodifica MEMBERSHIP_MESS & TRANSITION_MESS del Grupo: " + group.Name);
                            else
                                Trace(_Name, "..... Decodifica MEMBERSHIP_MESS & SELF-LEAVE del Grupo: " + group.Name);
                        }
                    }
					else
					{
						_Logger.Warn("Recibido mensaje de tipo desconocido [ServiceType: {0}]", serviceType);
                        Trace(_Name, "Recibido mensaje de tipo desconocido [ServiceType: {0}]", serviceType);
                    }
				}
			}
			catch (Exception ex)
			{
				if (_Connected)
				{
					General.SafeLaunchEvent(Error, this, ex.Message);
				}
                _Logger.Error(String.Format("SpreadChannel::ReceiveThread {0}.", _Name), ex);
			}
		}

        private void PrintListaRecibida(byte[] buffer)
        {
            vs_set_info[] vsSets = new vs_set_info[128];
            int myVsSetIndex;

            // Mas Informacion: http://www.spread.org/docs/spread_docs_4/docs/sp_get_vs_sets_info.html
            int numVsSets = SP_get_vs_sets_info(buffer, vsSets, vsSets.Length, out myVsSetIndex);
            //Lista local de los miembros del grupo que saco del mensaje recibido
            for (int i = 0; i < numVsSets; i++)
            {
                Trace(_Name, "-->VS set {0} has {1} members:\n",
                       i, vsSets[i].NumMembers);
                group_name[] members = new group_name[vsSets[i].NumMembers];
                int ret = SP_get_vs_set_members(buffer, ref vsSets[i], members, members.Length);
                for (int j = 0; j < vsSets[i].NumMembers; j++)
                {
                    Trace(_Name, "({1}) {0}", members[j].Name, j);
                }
            }
        }

        /// <summary>
        /// Funcion que actualiza la propiedad de la lista que guarda todos los
        /// miembros presentes en la red en cada grupo en el caso de un mensaje recibido de 
        /// membership - caused by changes in network. 
        /// Envia mensajes de join o leave a Registry, con los cambios detectados
        /// </summary>
        /// <param name= "buffer" mensaje recibido></param>
        /// <param name="topic" nombre del grupo></param> 
        /// <param name="vsSets" array de listas que llegan en el mensaje></param> 
        /// <param name="numVsSets" numero de listas que llegan en el mensaje></param> 
        /// <param name="master" true si debo ser master ></param> 
        private void SearchPresentMembers(byte[] buffer, group_name topic, vs_set_info[] vsSets, int numVsSets, bool master)
        {
            SpreadMembershipMsg msg;
            
            //Lista local de los miembros del grupo que saco del mensaje recibido
            List<String> currentMemberList = new List<String>();
            for (int i = 0; i < numVsSets; i++)
            {
                group_name[] members = new group_name[vsSets[i].NumMembers];
                int ret = SP_get_vs_set_members(buffer, ref vsSets[i], members, members.Length);
                for (int j = 0; j < vsSets[i].NumMembers; j++)
                {
                    currentMemberList.Add(members[j].Name);
                }
            }
            //Copia temporal para poder cambiar la _PresentMembersInNetwork dentro del bucle
            List<string> oldListCopy = new List<string>(_PresentMembersInNetwork[topic.Name]);
            foreach (string elem in oldListCopy)
                if (!currentMemberList.Contains(elem))
                {
                    _PresentMembersInNetwork[topic.Name].Remove(elem);
                    Trace(_Name, "{0} no presente N", elem);
                    msg = new SpreadMembershipMsg(topic.Name, master, MembershipChange.Leave, elem);
                    General.SafeLaunchEvent(MembershipMsg, this, msg);
                }
            foreach (string elem in currentMemberList)
                if (!_PresentMembersInNetwork[topic.Name].Contains(elem))
                {
                    _PresentMembersInNetwork[topic.Name].Add(elem);
                    Trace(_Name, "{0} presente N", elem);
                    msg = new SpreadMembershipMsg(topic.Name, master, MembershipChange.Join, elem);
                    General.SafeLaunchEvent(MembershipMsg, this, msg);
                }
      
        }

		#endregion

        /// <summary>
        /// inci, rd-inci, Cd40CfgReg, Cd40RdSrv, Cd40GwReg
        /// </summary>
        List<string> filtro = new List<string>() {  };
        private void Trace(string name, string formato, params object[] parametros)
        {
            if (filtro.Count==0 || filtro.Contains(name))
            {
                String msg = String.Format(formato, parametros);
                _Logger.Trace("{0}<< {1}",name, msg);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="topic"></param>
        private void LogMemberGroup(string name, string topic, List<string> actuales)
        {
#if _SPREAD_ALL_MEMBERS_
            List<string> anteriores = _AllMembers[topic];
#else
            List<string> anteriores = _PrecedingMembers[topic];
#endif
            StringBuilder sb = new StringBuilder("..... TOPIC <{0}>. ANT: ");
            foreach(string miembro in anteriores)
            {
                sb.Append(miembro + ", ");
            }
            sb.Append("... ACT: ");
            foreach (string miembro in actuales)
            {
                sb.Append(miembro + ", ");
            }
            Trace(name, sb.ToString(), topic);
        }

#if _SPREAD_ALL_MEMBERS_
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="actuales"></param>
        /// <param name="nuevos"></param>
        /// <param name="desaparecidos"></param>
        private void ProccessGroup(string topic, List<string> actuales, ref List<Pair<MembershipChange,string>> cambios)
        {
            List<string> anteriores = _AllMembers[topic];

            cambios.Clear();
            foreach (string member in actuales)
            {
                if (anteriores.Contains(member) == false)
                    cambios.Add(new Pair<MembershipChange,string>(MembershipChange.Join, member));
            }

            foreach (string member in anteriores)
            {
                if (actuales.Contains(member) == false)
                    cambios.Add(new Pair<MembershipChange,string>(MembershipChange.Leave, member));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="actuales"></param>
        /// <returns></returns>
        private Dictionary<string, int> prioridades = new  Dictionary<string, int>()
        {
           {"PICT182",1},
           {"PICT129",2},
           {"PICT127",3}
        };
        private bool SoyMaster(List<string> actuales)
        {
            int _miPrioridad = prioridades.ContainsKey(_Pict) ? prioridades[_Pict] : 0;
            foreach (string member in actuales)
            {
                string[] names = member.Split('#');
                if (names.Length >= 3)
                {
                    string pict = names[2].ToUpper();
                    int _suPrioridad = prioridades.ContainsKey(pict) ? prioridades[pict] : 0;
                    if (_suPrioridad > _miPrioridad)
                        return false;
                }
            }

            return true;
        }
#endif

    }
}
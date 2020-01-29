//#define _TRACEAGENT_
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Timers;

using System.ComponentModel;
using System.Reflection;

using U5ki.Infrastructure.Properties;
using NLog;
using Utilities;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <param name="data"></param>
    /// <param name="len"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void LogCb(int level, string data, int len);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void KaTimeoutCb(int call);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void RdInfoCb(int call, [In] CORESIP_RdInfo info);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
    /// <param name="stateInfo"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void CallStateCb(int call, [In] CORESIP_CallInfo info, [In] CORESIP_CallStateInfo stateInfo);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="call2replace"></param>
    /// <param name="info"></param>
    /// <param name="inInfo"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void CallIncomingCb(int call, int call2replace, [In] CORESIP_CallInfo info, [In] CORESIP_CallInInfo inInfo);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
    /// <param name="transferInfo"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void TransferRequestCb(int call, [In] CORESIP_CallInfo info, [In] CORESIP_CallTransferInfo transferInfo);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="code"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void TransferStatusCb(int call, int code);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="confInfo"></param>
    /// <param name="from"> Uri del origen del Notify </param>
    /// <param name="lenfrom"> Longitud de from </param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void ConfInfoCb(int call, [In] CORESIP_ConfInfo confInfo, string from, uint lenfrom);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="confInfo"></param>
    /// <param name="from"> Uri del origen del Notify </param>
    /// <param name="lenfrom"> Longitud de from </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void ConfInfoAccCb(string accountId, [In] CORESIP_ConfInfo confInfo, string from, uint lenfrom);
    /// <summary>
    /// Callback que se llama cuando se recibe un notify al evento de dialogo
    /// </summary>
    /// <param name="xml_body">body del notify</param>
    /// <param name="length">longitud del body</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void DialogNotifyCb(string xml_body, uint length);

    /// <summary>
    /// Callback que se llama cuando se recibe un mensaje de texto
    /// </summary>
    /// <param name="from">URI of the sender</param>
    /// <param name="to">URI of the destination message</param>
    /// <param name="contact">The Contact URI of the sender, if present.</param>
    /// <param name="mime_type">MIME type of the message.</param>
    /// <param name="body">The message content</param>
    /// 
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void PagerCb(string from_uri, uint from_uri_len,
             string to_uri, uint to_uri_len, string contact_uri, uint contact_uri_len,
             string mime_type, uint mime_type_len, string body, uint body_len);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fromUri"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void OptionsReceiveCb(string fromUri, string callid, int statusCodem, string supported, string allow);

    /// <summary>
    /// Esta funcion se llama cuando se recibe un options del tipo utilizado para la negociacion Call Forward
    /// </summary>
    /// <param name="accId">Account obtenido de la uri To</param>
    /// <param name="from_uri">Uri de la cabecera From</param>
    /// <param name="cfwr_options_type">Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE</param>
    /// <param name="body">Es el cuerpo del mensaje, terminado con el caracter '\0'</param>
    /// <param name="hresp">Manejador necesario para enviar la respuesta
    ///
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CfwrOptReceivedCb(int accId, string from_uri, CORESIP_CFWR_OPT_TYPE cfwr_options_type, string body, uint hresp);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="accId">Account obtenido de la uri To</param>
    /// <param name="from_uri">Uri de la cabecera From</param>
    /// <param name="cfwr_options_type">Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE</param>
    /// <param name="body">Es el cuerpo del mensaje, terminado con el caracter '\0'</param>
    /// <param name="hresp">Manejador necesario para enviar la respuesta
    ///
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CfwrOptReceivedAccCb(string accId, string from_uri, CORESIP_CFWR_OPT_TYPE cfwr_options_type, string body, uint hresp);


    /// <summary>
    /// Esta funcion se llama cuando se recibe la respuesta a un options del tipo utilizado para la negociacion Call Forward
    /// </summary>
    /// <param name="accId">Account obtenido de la uri From</param>
    /// <param name="dstUri">Uri de la cabecera To. Es decir, es la uri del agente que nos envia la respuesta. Finalizado con '\0'</param>
    /// <param name="callid">Call Id recibido. Finalizado con '\0'</param>
    /// <param name="st_code">Code de la respuesta</param>
    /// <param name="cfwr_options_type">Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE</param>
    /// <param name="body">Es el cuerpo del mensaje, terminado con el caracter '\0'.</param>
    ///
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CfwrOptResponseCb(int accId, string dstUri, string callid, int st_code, CORESIP_CFWR_OPT_TYPE cfwr_options_type, string body);

    /// <summary>
    /// Esta funcion se llama cuando se recibe la respuesta a un options del tipo utilizado para la negociacion Call Forward
    /// </summary>
    /// <param name="accId">Account obtenido de la uri From</param>
    /// <param name="dstUri">Uri de la cabecera To. Es decir, es la uri del agente que nos envia la respuesta. Finalizado con '\0'</param>
    /// <param name="callid">Call Id recibido. Finalizado con '\0'</param>
    /// <param name="st_code">Code de la respuesta</param>
    /// <param name="cfwr_options_type">Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE</param>
    /// <param name="body">Es el cuerpo del mensaje, terminado con el caracter '\0'.</param>
    ///
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CfwrOptResponseAccCb(string accId, string dstUri, string callid, int st_code, CORESIP_CFWR_OPT_TYPE cfwr_options_type, string body);

    /// <summary>
    /// Esta funcion se llama cuando se recibe un 302 (Moved Temporally) avisando que hay una redireccion pendiente de la llamada
    /// Para aceptar o rechazar la redireccion, la aplicacion debe llamar a la funcion #CallProccessRedirect
    /// </summary>
    /// <param name="call">Identificador de la llamada</param>
    /// <param name="dstUri">Uri a la que se quiere redirigir la llamada. String terminado en cero.</param>
    ///
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void MovedTemporallyCb(int call, string dstUri);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
    /// <param name="lenInfo"></param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public delegate void InfoReceivedCb(int call, string info, uint lenInfo);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void WG67NotifyCb(IntPtr wg67, CORESIP_WG67Info wg67Info, IntPtr userData);

    /// <summary>
    ///  Received when subscription to conference arrives
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
    /// <param name="lenInfo"></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void IncomingSubscribeConfCb(int call, string from, uint lenInfo);
    /// <summary>
    ///  Received when subscription to conference arrives
    /// </summary>
    /// <param name="call"></param>
    /// <param name="info"></param>
    /// <param name="lenInfo"></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void IncomingSubscribeConfAccCb(string accountId, string from, uint lenInfo);

    /*Callback para recibir notificaciones por la subscripcion de presencia*/
    /*	dst_uri: uri del destino cuyo estado de presencia ha cambiado.
     *	subscription_status: vale 0 la subscripcion al evento no ha tenido exito. 
     *	presence_status: vale 0 si no esta presente. 1 si esta presente.
     */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SubPresCb(string dst_uri, int subscription_status, int presence_status);

    /*Callback para recibir notificaon de fin de reproduccion de un wav. Lo utiliza el ETM*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void FinWavCb(int Code);
	
#if _ED137_
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void UpdateOvrCallMembersCb([In] CORESIP_OvrCallMembers members);
    
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void InfoCRDCb([In] CORESIP_CRD crd);
	
	public enum CORESIP_TypeCrdInfo
	{
		CORESIP_CRD_SET_PARAMETER,
		CORESIP_CRD_RECORD,
		CORESIP_CRD_PAUSE,
		CORESIP_CRD_PTT,
		CORESIP_SQ
	}
#endif

	/// <summary>
    /// 
    /// </summary>
	public enum CORESIP_CallType
	{
        [Description("IA CALL")]
		CORESIP_CALL_IA,
        [Description("MON CALL")]
		CORESIP_CALL_MONITORING,
        [Description("G/G MON CALL")]
		CORESIP_CALL_GG_MONITORING,
        [Description("A/G MON CALL")]
		CORESIP_CALL_AG_MONITORING,
        [Description("DA CALL")]
		CORESIP_CALL_DIA,
		CORESIP_CALL_RD,
		CORESIP_CALL_UNKNOWN
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_Priority
	{
        [Description("Emergencia")]
		CORESIP_PR_EMERGENCY,
        [Description("Urgente")]
		CORESIP_PR_URGENT,
        [Description("Normal")]
		CORESIP_PR_NORMAL,
        [Description("No Urgente")]
		CORESIP_PR_NONURGENT,
		CORESIP_PR_UNKNOWN
	}
    /// <summary>
    /// 
    /// </summary>
	[Flags]
	public enum CORESIP_CallFlags
	{
        [Description("TxRx")]
		CORESIP_CALL_NO_FLAGS = 0,
        [Description("Conference focus")]
        CORESIP_CALL_CONF_FOCUS = 0x1,
        [Description("Coupling")]
		CORESIP_CALL_RD_COUPLING = 0x2,
        [Description("Rx")]
		CORESIP_CALL_RD_RXONLY = 0x4,
        [Description("Tx")]
		CORESIP_CALL_RD_TXONLY = 0x8,
        [Description("Echo Canceller")]
		CORESIP_CALL_EC = 0x10,
        [Description("Through external central IP")]
        CORESIP_CALL_EXTERNAL_IP = 0x20,

        CORESIP_CALL_NINGUNO = 0x0,			//Se refiere a transceptor 
	    CORESIP_CALL_RD_IDLE = 0x12,		
	    CORESIP_CALL_RD_TXRX = 0x14 		
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_CallState
	{
        [Description("Null")]
		CORESIP_CALL_STATE_NULL,					/**< Before INVITE is sent or received  */
        [Description("Calling")]
		CORESIP_CALL_STATE_CALLING,				/**< After INVITE is sent		    */
        [Description("Incoming")]
		CORESIP_CALL_STATE_INCOMING,				/**< After INVITE is received.	    */
        [Description("Early")]
		CORESIP_CALL_STATE_EARLY,					/**< After response with To tag.	    */
        [Description("Connecting")]
		CORESIP_CALL_STATE_CONNECTING,			/**< After 2xx is sent/received.	    */
        [Description("Confirmed")]
		CORESIP_CALL_STATE_CONFIRMED,			/**< After ACK is sent/received.	    */
        [Description("Disconnected")]
		CORESIP_CALL_STATE_DISCONNECTED,		/**< Session is terminated.		    */
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_CallRole
	{
		CORESIP_CALL_ROLE_UAC,
		CORESIP_CALL_ROLE_UCS
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_MediaStatus
	{
		CORESIP_MEDIA_NONE,
		CORESIP_MEDIA_ACTIVE,
		CORESIP_MEDIA_LOCAL_HOLD,
		CORESIP_MEDIA_REMOTE_HOLD,
		CORESIP_MEDIA_ERROR
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_MediaDir
	{
		CORESIP_DIR_NONE,
		CORESIP_DIR_SENDONLY,
		CORESIP_DIR_RECVONLY,
		CORESIP_DIR_SENDRECV
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_PttType
	{
		CORESIP_PTT_OFF,
        [Description("Normal")]
		CORESIP_PTT_NORMAL,
        [Description("Coupling")]
		CORESIP_PTT_COUPLING,
        [Description("Prioritario")]
		CORESIP_PTT_PRIORITY,
        [Description("Emergencia")]
		CORESIP_PTT_EMERGENCY
	}
    /// <summary>
    /// 
    /// </summary>
	public enum CORESIP_SndDevType
	{
		CORESIP_SND_INSTRUCTOR_MHP,
		CORESIP_SND_ALUMN_MHP,
		CORESIP_SND_MAX_IN_DEVICES,
		CORESIP_SND_MAIN_SPEAKERS = CORESIP_SND_MAX_IN_DEVICES,
		CORESIP_SND_LC_SPEAKER,
		CORESIP_SND_RD_SPEAKER,
		CORESIP_SND_INSTRUCTOR_RECORDER,
		CORESIP_SND_ALUMN_RECORDER,
		CORESIP_SND_RADIO_RECORDER,
		CORESIP_SND_LC_RECORDER,
        CORESIP_SND_HF_SPEAKER,
        CORESIP_SND_HF_RECORDER,
		CORESIP_SND_UNKNOWN
	}

    //EDU 20170223
    public enum CORESIP_FREQUENCY_TYPE { Simple = 0, Dual = 1, FD = 2, ME = 3 }         // 0. Normal, 1: 1+1, 2: FD, 3: EM
    public enum CORESIP_CLD_CALCULATE_METHOD { Relative, Absolute }
    public enum CORESIP_PttMuteType
    {
        DESACTIVADO = 0,
        ACTIVADO = 1
    }
    public enum CORESIP_CFWR_OPT_TYPE
    {
        CORESIP_CFWR_OPT_REQUEST,
        CORESIP_CFWR_OPT_RELEASE,
        CORESIP_CFWR_OPT_UPDATE
    }

    public enum CORESIP_REDIRECT_OP
    {
        CORESIP_REDIRECT_REJECT,
        CORESIP_REDIRECT_ACCEPT
    }

    public class WG67Info
	{
		public struct SubscriberInfo
		{
			public ushort PttId;
			public string SubsUri;
		}

		public string DstUri;

		public bool SubscriptionTerminated;
		public uint SubscribersCount;
		public string LastReason;

		public SubscriberInfo[] Subscribers;
	}

#if _ED137_
	  // PlugTest FAA   05/2011
	  // OVR calls
	   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	   public class CORESIP_OvrCallMembers
	   {
		   public short MembersCount;

		   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		   public struct CORESIP_Members
		   {
			   [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
			   public string Member;
			   public int CallId;
			   bool IncommingCall;
		   }

		   [MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgent.CORESIP_MAX_OVR_CALLS_MEMBERS)]
		   public CORESIP_Members[] EstablishedOvrCallMembers;
	   }

       // PlugTest FAA 05/2011
       // Record
       [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
       public class CORESIP_CRD
       {
           public CORESIP_TypeCrdInfo _Info; 
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_CALLREF_LENGTH + 1)]
           public string CallRef;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_CONNREF_LENGTH + 1)]
           public string ConnRef;
           public int Direction;
           public int Priority;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
           public string CallingNr;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
           public string CallerNr;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_TIME_LENGTH + 1)]
           public string SetupTime;

           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
           public string ConnectedNr;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_TIME_LENGTH + 1)]
           public string ConnectedTime;

           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_TIME_LENGTH + 1)]
           public string DisconnectTime;
           public int DisconnectCause;
           public int DisconnectSource;

           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_FRECUENCY_LENGTH + 1)]
           public string FrecuencyId;

           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_TIME_LENGTH + 1)]
           public string Squ;
           [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_TIME_LENGTH + 1)]
           public string Ptt;
       }
#endif
	
	/// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_CallInfo
	{
		public int AccountId;
		public CORESIP_CallType Type;
		public CORESIP_Priority Priority;
		public CORESIP_CallFlags Flags;

        public CORESIP_CallFlags Flags_type;			//UNIFETM: Este campo falta en ULISES. En el ETM lo utiliza para indicar el tipo de agente radio. Se pasa a la callback de onIncommingCall
        //public int SourcePort;						//UNIFETM: Este campo falta en ULISES. En el ETM no se utiliza. Se le asigna valor, pero para nada. Yo lo quitaria en ETM
        public int DestinationPort;						//UNIFETM: Este campo falta en ULISES. Se asigna el valor en onIncomingCall. es un valor que se retorna en la callback. No es de entrada.

#if _VOTER_
        /** 20160608. VOTER */
        public int PreferredCodec = 0;
        public int PreferredBss = 0;
#endif

        //EDU 20170223
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_ZONA_LENGTH + 1)]
        public string Zona;

        public CORESIP_FREQUENCY_TYPE FrequencyType;
        public CORESIP_CLD_CALCULATE_METHOD CLDCalculateMethod;
        public int BssWindows;
        public bool AudioSync;
        public bool AudioInBssWindow;
        public bool NotUnassignable;
        public int cld_supervision_time;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_BSS_LENGTH + 1)]
        public string bss_method;
        public uint porcentajeRSSI;                     //Peso del valor de Qidx del tipo RSSI en el calculo del Qidx final. 0 indica que el calculo es solo interno (centralizado). 10 que el calculo es solo el RSSI. 
	}

    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_CallOutInfo
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
		public string DstUri;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
		public string ReferBy;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_RS_LENGTH + 1)]
		public string RdFr;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_IP_LENGTH + 1)]
		public string RdMcastAddr;
		public uint RdMcastPort;

        //Referente al replaces. Esto no se necesita cuando el DstUri se obtiene de un REFER y ya tiene la info de replaces
	    public bool RequireReplaces;		//Vale true si requiere replaces
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_CALLID_LENGTH + 1)]
	    public string CallIdToReplace;	    //Call id de la llamada a reemplazar
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_TAG_LENGTH + 1)]
	    public string ToTag;				//Tag del To de la llamada a reemplazar
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_TAG_LENGTH + 1)]
	    public string FromTag;				//Tag del From de la llamada a reemplazar
	    public bool EarlyOnly;				//Vale true si se requiere el parametro early-only en el replaces
	}
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_CallInInfo
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string SrcId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_IP_LENGTH + 1)]
		public string SrcIp;
        public uint SrcPort;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string SrcSubId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_RS_LENGTH + 1)]
		public string SrcRs;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string DstId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_IP_LENGTH + 1)]
		public string DstIp;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string DstSubId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_NAME_LENGTH + 1)]
        public string DisplayName;
    }
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_CallTransferInfo
	{
		public IntPtr TxData;
		public IntPtr EvSub;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_RS_LENGTH + 1)]
		public string TsxKey;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
		public string ReferBy;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2 * SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
		public string ReferTo;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string DstId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_IP_LENGTH + 1)]
		public string DstIp;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
		public string DstSubId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_RS_LENGTH + 1)]
		public string DstRs;
	}
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_CallStateInfo
	{
		public CORESIP_CallState State;
		public CORESIP_CallRole Role;

		public int LastCode;										// Util cuando State == PJSIP_INV_STATE_DISCONNECTED
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_REASON_LENGTH + 1)]
		public string LastReason;

		public int LocalFocus;
		public int RemoteFocus;
		public CORESIP_MediaStatus MediaStatus;
		public CORESIP_MediaDir MediaDir;

		// CORESIP_CALL_RD y PJSIP_INV_STATE_CONFIRMED
		public ushort PttId;
		public uint ClkRate;
		public uint ChannelCount;
		public uint BitsPerSample;
		public uint FrameTime;
	}
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_PttInfo
	{
		public CORESIP_PttType PttType;
		public ushort PttId;
        public uint PttMute;
		public uint ClimaxCld;
        public uint Squ;
        public uint RssiQidx;			//Valor de Qidx del tipo RSSI que se envia cuando es un agente simulador de radio y Squ es activo
	}
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_RdInfo
	{
		public CORESIP_PttType PttType;
		public ushort PttId;
        public int PttMute;
		public int Squelch;
		public int Sct;
		
        //EDU 20170224
        public int rx_rtp_port;
        public int rx_qidx;
        public bool rx_selected;
        public int tx_rtp_port;
        public int tx_cld;
        public int tx_owd;
	}
	/// <summary>
	/// 
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_ConfInfo
	{
		public uint Version;
		public uint UsersCount;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_CONF_STATE_LENGTH + 1)]
		public string State;

		public struct ConfUser
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
			public string Id;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
			public string Name;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_USER_ID_LENGTH + 1)]
			public string Role;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_CONF_STATE_LENGTH + 1)]
			public string State;
		}
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgent.CORESIP_MAX_CONF_USERS)]
		public ConfUser[] Users;
	}
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_Callbacks
	{
		public IntPtr UserData;

		public LogCb OnLog;
		public KaTimeoutCb OnKaTimeout;
		public RdInfoCb OnRdInfo;
		public CallStateCb OnCallState;
		public CallIncomingCb OnCallIncoming;
		public TransferRequestCb OnTransferRequest;
		public TransferStatusCb OnTransferStatus;
		public ConfInfoCb OnConfInfo;
		public OptionsReceiveCb OnOptionsReceive ;
		public WG67NotifyCb OnWG67Notify;
		public InfoReceivedCb OnInfoReceived;
        public IncomingSubscribeConfCb OnIncomingSubscribeConf;
        public SubPresCb OnSubPres;
        public FinWavCb OnFinWavCb;
        public DialogNotifyCb OnDialogNotify;
        public PagerCb OnPager;
#if CALLFORWARD
        public CfwrOptReceivedCb OnCfwrOptReceived;
        public CfwrOptResponseCb OnCfwrOptResponse;
        public MovedTemporallyCb OnMovedTemporally;
#endif
#if _ED137_
	// PlugTest FAA 05/2011
		public UpdateOvrCallMembersCb OnUpdateOvrCallMembers; //(CORESIP_EstablishedOvrCallMembers info);
		public InfoCRDCb OnInfoCrd;								//(CORESIP_CRD InfoCrd);
#endif
    }
    /// <summary>
    /// 
    /// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_Params
	{
		public int EnableMonitoring;

		public uint KeepAlivePeriod;
		public uint KeepAliveMultiplier;
	}


	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class CORESIP_WG67Info
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct CORESIP_WG67SubscriberInfo
		{
			public ushort PttId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
			public string SubsUri;
		}

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_URI_LENGTH + 1)]
		public string DstUri;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_REASON_LENGTH + 1)]
		public string LastReason;

		public int SubscriptionTerminated;
		public uint SubscribersCount;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgent.CORESIP_MAX_WG67_SUBSCRIBERS)]
		public CORESIP_WG67SubscriberInfo[] Subscribers;
	}

	/// <summary>
    /// 
    /// </summary>
	public static class SipAgent
	{
		public const int SIP_TRYING = 100;
		public const int SIP_RINGING = 180;
		public const int SIP_QUEUED = 182;
		public const int SIP_INTRUSION_IN_PROGRESS = 183;
		public const int SIP_INTERRUPTION_IN_PROGRESS = 184;
		public const int SIP_INTERRUPTION_END = 185;
		public const int SIP_OK = 200;
		public const int SIP_ACCEPTED = 202;
        public const int SIP_MOVED_TEMPORARILY = 302;
		public const int SIP_BAD_REQUEST = 400;
		public const int SIP_NOT_FOUND = 404;
        public const int SIP_NOT_ACCEPTABLE = 406;
		public const int SIP_REQUEST_TIMEOUT = 408;
		public const int SIP_GONE = 410;
		public const int SIP_TEMPORARILY_UNAVAILABLE = 480;
        public const int SIP_LOOP_DETECTED = 482;
		public const int SIP_BUSY = 486;
		public const int SIP_NOT_ACCEPTABLE_HERE = 488;
		public const int SIP_ERROR = 500;
		public const int SIP_CONGESTION = 503;
        public const int SIP_SERVER_TIMEOUT = 504;
        public const int SIP_DECLINE = 603;
        public const int SIP_UNWANTED = 607;

        /** 20180717. Para los codigos BYE de las sesiones RADIO */
        public const int WG67Reason_KATimeout = 2001;

        private static string UG5K_REC_CONF_FILE = "ug5krec-config.ini";
		#region Dll Interface

		public const int CORESIP_MAX_USER_ID_LENGTH = 100;
		public const int CORESIP_MAX_FILE_PATH_LENGTH = 256;
		public const int CORESIP_MAX_ERROR_INFO_LENGTH = 512;
        public const int CORESIP_MAX_HOSTID_LENGTH = 32;
		public const int CORESIP_MAX_IP_LENGTH = 25;
		public const int CORESIP_MAX_URI_LENGTH = 256;
        public const int CORESIP_MAX_TAG_LENGTH = 256;
		public const int CORESIP_MAX_SOUND_DEVICES = 10;
		public const int CORESIP_MAX_RS_LENGTH = 128;
		public const int CORESIP_MAX_REASON_LENGTH = 128;
		public const int CORESIP_MAX_WG67_SUBSCRIBERS = 25;
		public const int CORESIP_MAX_CODEC_LENGTH = 50;
		public const int CORESIP_MAX_CONF_USERS = 25;
		public const int CORESIP_MAX_CONF_STATE_LENGTH = 25;
        public const int CORESIP_MAX_ZONA_LENGTH = 128;     //EDU 20170223
        public const int CORESIP_MAX_BSS_LENGTH = 32;     //EDU 20170223
        public const int CORESIP_MAX_NAME_LENGTH = 20;    //B. Santamaria 20180206
        public const int CORESIP_MAX_CALLID_LENGTH = 256;    
#if _ED137_
		// PlugTest FAA 05/2011
		public const int CORESIP_MAX_OVR_CALLS_MEMBERS = 10;
		public const int CORESIP_CALLREF_LENGTH = 50;
		public const int CORESIP_CONNREF_LENGTH = 50;
		public const int CORESIP_TIME_LENGTH = 28;
		public const int CORESIP_MAX_FRECUENCY_LENGTH = 7;
#endif

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct CORESIP_Error
		{
			public int Code;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_FILE_PATH_LENGTH + 1)]
			public string File;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_ERROR_INFO_LENGTH + 1)]
			public string Info;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		class CORESIP_SndDeviceInfo
		{
			public CORESIP_SndDevType Type;
			public int OsInDeviceIndex;
			public int OsOutDeviceIndex;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		class CORESIP_RdRxPortInfo
		{
			public uint ClkRate;
			public uint ChannelCount;
			public uint BitsPerSample;
			public uint FrameTime;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_IP_LENGTH + 1)]
			public string Ip;
			public uint Port;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		class CORESIP_Config
		{
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_HOSTID_LENGTH + 1)]
            public string HostId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_IP_LENGTH + 1)]
			public string IpAddress;
			public uint Port;
            public uint RtpPorts;				//Valor por el que empiezan a crearse los puertos RTP

			public CORESIP_Callbacks Cb;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_CODEC_LENGTH + 1)]
			public string DefaultCodec;
			public uint DefaultDelayBufPframes;
			public uint DefaultJBufPframes;
			public uint SndSamplingRate;
			public float RxLevel;
			public float TxLevel;
			public uint LogLevel;

			public uint TsxTout;
			public uint InvProceedingIaTout;
			public uint InvProceedingMonitoringTout;
			public uint InvProceedingDiaTout;
			public uint InvProceedingRdTout;

            /* AGL 20131121. Variables para la configuracion del Cancelador de Eco */
            public uint EchoTail;
            public uint EchoLatency;
            /* FM */

            /// <summary>
            /// JCAM 18/01/2016
            /// Grabación según norma ED-137
            /// </summary>
            public uint RecordingEd137;

            public uint max_calls;		//Máximo número de llamadas que soporta el agente
            public uint Radio_UA;		//Con valor distinto de 0, indica que se comporta como un agente de radio

            /// <summary>
            /// Tiempo para inhibir el envio a Nodebox de eventos de RdInfo tras PTT off propio
            /// Evita que el nodebox reciba falsos SQ de avion provocados por PTT propio
            /// </summary>
            public uint TimeToDiscardRdInfo;

            public uint DIA_TxAttenuation_dB;		//Atenuacion de las llamadas DIA en Tx (Antes de transmistir por RTP). En dB
            public uint IA_TxAttenuation_dB;		//Atenuacion de las llamadas IA en Tx (Antes de transmistir por RTP). En dB
            public uint RD_TxAttenuation_dB;		//Atenuacion del Audio que se transmite hacia el multicas al hacer PTT en el HMI. En dB
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        class CORESIP_Impairments   //Necesario para ETM
        {
	        public int Perdidos;
	        public int Duplicados;
	        public int LatMin;
	        public int LatMax;
        }
       
        enum CORESIP_RecCmdType : int {CORESIP_REC_RESET = 0 // Ordena reiniciar el grabador
        };

        const uint CORESIP_CALL_ID = 0x40000000;
        const uint CORESIP_SNDDEV_ID=0x20000000;
        const uint CORESIP_WAVPLAYER_ID=0x10000000;
        const uint CORESIP_RDRXPORT_ID=0x08000000;
        const uint CORESIP_SNDRXPORT_ID=0x04000000;
        const uint CORESIP_ACC_ID=0x02000000;
        const uint CORESIP_WAVRECORDER_ID=0x01000000;

        const uint CORESIP_ID_TYPE_MASK=0xFF800000;
        const uint CORESIP_ID_MASK = 0x007FFFFF;

#if _VOTER_
        const string coresip = "coresip-voter";
#else
        const string coresip = "coresip";
#endif

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_Init([In] CORESIP_Config info, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_Start(out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern void CORESIP_End();
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_SetLogLevel(uint level, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_SetParams([In] CORESIP_Params info, out CORESIP_Error error);

        /**
         *	CORESIP_CreateAccount. Registra una cuenta SIP en el Módulo. @ref SipAgent::CreateAccount
         *	@param	acc			Puntero a la sip URI que se crea como agente.
         *	@param	defaultAcc	Marca si esta cuenta pasa a ser la Cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateAccount([In] string acc, int defaultAcc, out int accId, out CORESIP_Error error);

        /**
         *	CORESIP_CreateAccountProxyRouting. Registra una cuenta SIP en el Módulo y los paquetes sip se enrutan por el proxy. @ref SipAgent::CreateAccount
         *	@param	acc			Puntero a la sip URI que se crea como agente.
         *	@param	defaultAcc	Marca si esta cuenta pasa a ser la Cuenta por Defecto.
         *	@pa ram	accId		Puntero a el identificador de cuenta asociado.
         *  @param	proxy_ip	Si es distinto de NULL. IP del proxy Donde se quieren enrutar los paquetes.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateAccountProxyRouting([In] string acc, int defaultAcc, out int accId, [In] string proxy_ip, out CORESIP_Error error);

        /**
         *	CreateAccountAndRegisterInProxy. Crea una cuenta y se registra en el SIP proxy. Los paquetes sip se rutean por el SIP proxy también.
         *	@param	acc			Puntero al Numero de Abonado (usuario). NO a la uri.
         *	@param	defaultAcc	Si es diferente a '0', indica que se creará la cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado que retorna.
         *	@param	proxy_ip	IP del proxy.
         *	@param	expire_seg  Tiempo en el que expira el registro en segundos.
         *	@param	username	Si no es necesario autenticación, este parametro será NULL
         *	@param  pass		Password. Si no es necesario autenticación, este parametro será NULL
         *	@param  DisplayName	Display name que va antes de la sip URI, se utiliza para como nombre a mostrar
         *	@param	isfocus		Si el valor es distinto de cero, indica que es Focus, para establecer llamadas multidestino
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateAccountAndRegisterInProxy([In] string acc, int defaultAcc, out int accId, string proxy_ip,
                                                                uint expire_seg, string username, string pass, string displayName, int isfocus, out CORESIP_Error error);


        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_DestroyAccount(int accId, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_AddSndDevice([In] CORESIP_SndDeviceInfo info, out int dev, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateWavPlayer([In] string file, int loop, out int wavPlayer, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_DestroyWavPlayer(int wavPlayer, out CORESIP_Error error);		
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateRdRxPort([In] CORESIP_RdRxPortInfo info, string localIp, out int mcastPort, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int	CORESIP_DestroyRdRxPort(int mcastPort, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateSndRxPort(string id, out int sndRxPort, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_DestroySndRxPort(int sndRxPort, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_BridgeLink(int src, int dst, int on, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_SendToRemote(int dev, int on, string id, string ip, uint port, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_ReceiveFromRemote(string localIp, string mcastIp, uint mcastPort, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_SetVolume(int id, int volume, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_GetVolume(int dev, out int volume, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallMake([In] CORESIP_CallInfo info, [In] CORESIP_CallOutInfo outInfo, out int call, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallHangup(int call, int code, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallAnswer(int call, int code, int addToConference, out CORESIP_Error error);
        /**
         *	CORESIP_CallMovedTemporallyAnswer
         *	@param	call		Identificador de Llamada
         *	@param	dst			Uri del usuario al que la llamada es desviada
         *	@param	reason		Es la razon del desvio. Posibles valores "unconditional", "user-busy", etc.
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallMovedTemporallyAnswer(int call, [In] string dst, [In] string reason, out CORESIP_Error error);

        /**
         *	CORESIP_CallProccessRedirect
         *	Esta funcion debe llamarse despues de recibirse la callback MovedTemporallyCb para 
         *	aceptar o rechazar la redireccion de la llamada.
         *	@param	call		Identificador de Llamada
         *	@param  dstUri		Nueva request uri hacia donde se desvia la llamada. Terminado en '\0', Si se rechaza entonces este parametro se ignora.
         *	@param	op			Opcion (aceptar o rechazar)
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallProccessRedirect(int call, string dstUri, CORESIP_REDIRECT_OP op, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallHold(int call, int hold, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallTransfer(int call, int dstCall, [In] string dst, [In] string displayName, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallPtt(int call, [In] CORESIP_PttInfo info, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallConference(int call, int conf, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallSendConfInfo(int call, [In] CORESIP_ConfInfo info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendConfInfoFromAcc(int accId, [In] CORESIP_ConfInfo info, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendConfInfoToUri(string uri, [In] CORESIP_ConfInfo info, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CallSendInfo(int call, [In] string info, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int	CORESIP_TransferAnswer(string tsxKey, IntPtr txData, IntPtr evSub, int code, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int	CORESIP_TransferNotify(IntPtr evSub, int code, out CORESIP_Error error);

        /**
         *	SendOptionsMsg
         *  Esta función no envia OPTIONS a traves del proxy
         *	@param	dst			Puntero a uri donde enviar OPTIONS
         *  @param	callid		callid que retorna.
         *  @param	isRadio		Si tiene valor distinto de cero el agente se identifica como radio. Si es cero, como telefonia.
         *						Sirve principalmente para poner radio.01 o phone.01 en la cabecera WG67-version
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendOptionsMsg([In] string dst, StringBuilder callid, int isRadio, out CORESIP_Error error);
        //Envía OPTIONS directamente sin pasar por el proxy
        /**
         * SendOptionsCFWD.	...
         * Envia mensaje OPTIONS necesario para la negociacion Call Forward
         * @param	accId				Account de la Coresip que utilizamos.
         * @param	dst					Uri a la que se envia OPTIONS
         * @param	cfwr_options_type	Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE
         * @param	body				Contenido del body (XML). Acabado en '\0'
         * @param	callid				callid que se retorna, acabado en '\0'.
         * @param	by_proxy			TRUE si queremos que se envie a través del proxy. Agregara cabecera route
         * @param	error		Puntero a la Estructura de error
         * @return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendOptionsCFWD(int accId, [In] string dst, CORESIP_CFWR_OPT_TYPE cfwr_options_type, [In] string body, StringBuilder callid, bool by_proxy, out CORESIP_Error error);

        /**
         * CORESIP_SendResponseCFWD.	...
         * Envia la respuesta al options utilizado para la negociacion de call forward
         * @param	st_code				Code de la respuesta. Si no es 200 entonces se ignora el parametro del body
         * @param	body				Contenido del body (XML). Acabado en '\0'
         * @param	hresp				Manejador necesario para enviar la respuesta
         * @param	error		Puntero a la Estructura de error
         * @return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendResponseCFWD(int st_code, [In] string body, uint hresp, out CORESIP_Error error);

        /**
         *	SendOptionsMsg
         *  Esta función envia OPTIONS a traves del proxy
         *	@param	dst			Puntero a uri donde enviar OPTIONS
         *  @param	callid		callid que retorna.
         *  @param	isRadio		Si tiene valor distinto de cero el agente se identifica como radio. Si es cero, como telefonia.
         *						Sirve principalmente para poner radio.01 o phone.01 en la cabecera WG67-version
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendOptionsMsgProxy([In] string dst, StringBuilder callid, int isRadio, out CORESIP_Error error);
                //Envía OPTIONS pasando por el proxy

		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateWavRecorder([In] string file, out int wavPlayer, out CORESIP_Error error);
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_DestroyWavRecorder(int wavPlayer, out CORESIP_Error error);		
		
		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_CreateWG67Subscription(string dst, ref IntPtr wg67, ref CORESIP_Error error);

		[DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
		static extern int CORESIP_DestroyWG67Subscription(IntPtr wg67, ref CORESIP_Error error);

        /** AGL */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void WavRemoteEnd(IntPtr obj);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_Wav2RemoteStart([In] string file, string id, string ip, int port, ref WavRemoteEnd cbend, ref CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_Wav2RemoteEnd(IntPtr obj, ref CORESIP_Error error);

        /* GRABACION VOIP START */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_RdPttEvent(bool on, [In] string freqId, int dev,  out CORESIP_Error error, CORESIP_PttType priority);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_RdSquEvent(bool on, [In] string freqId, [In] string resourceId, [In] string bssMethod, uint bssQidx, out CORESIP_Error error);

        //Metodo para enviar un comando al grabador
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_RecorderCmd(CORESIP_RecCmdType cmd, out CORESIP_Error error);
        /* GRABACION VOIP END */

        /*Funciones para gestion de presencia por subscripcion al evento de presencia*/
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreatePresenceSubscription(string dest_uri, out CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyPresenceSubscription(string dest_uri, out CORESIP_Error error);

        /*Funciones para gestion de subscripcion al evento de conferencia*/
        /**
         *	CORESIP_CreateConferenceSubscription. Crea una subscripcion por evento de conferencia
         *	@param	accId		Identificador del account.
         *  @param  dest_uri.	Uri del destino a monitorizar
         *  @param	by_proxy.   Si true entonces el subscribe se envia a traves del proxy
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateConferenceSubscription(int accId, string dest_uri, bool byProxy, out CORESIP_Error error);

        /**
         *	CORESIP_DestroyConferenceSubscription. Destruye una subscripcion por evento de presencia
         *  @param  dest_uri.	Uri del destino a monitorizar
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyConferenceSubscription(string dest_uri, out CORESIP_Error error);

        /*Funciones para gestion de subscripcion al evento de dialogo*/
        /**
         *	CORESIP_CreateDialogSubscription. Crea una subscripcion por evento de dialogo
         *	@param	accId		Identificador del account.
         *  @param  dest_uri.	Uri del destino a monitorizar
         *  @param	by_proxy.   Si true entonces el subscribe se envia a traves del proxy
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateDialogSubscription(int accId, string dest_uri, bool byProxy, out CORESIP_Error error);

        /**
         *	CORESIP_DestroyDialogSubscription. Destruye una subscripcion por evento de dialogo
         *  @param  dest_uri.	Uri del destino a monitorizar
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyDialogSubscription(string dest_uri, out CORESIP_Error error);

        /**
         * CORESIP_SendInstantMessage. Envia un mensaje instantaneo
         *
         * @param	acc_id		Account ID to be used to send the request.
         * @param	dest_uri	Uri del destino del mensaje. Acabado en 0.
         * @param	text		Texto plano a enviar. Acabado en 0
         * @param	by_proxy	Si es true el mensaje se envia por el proxy
         * @return	Codigo de Error
         *
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendInstantMessage(int acc_id, string dest_uri, string text, bool by_proxy, out CORESIP_Error error);

        /**
         * CORESIP_EchoCancellerLCMic.	...
         * Activa/desactiva cancelador de eco altavoz LC y Microfonos. Sirve para el modo manos libres 
         * Por defecto esta desactivado en la coresip
         * @param	on						true - activa / false - desactiva
         * @return	CORESIP_OK OK, CORESIP_ERROR  error.
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_EchoCancellerLCMic(bool on, out CORESIP_Error error);

        /**
         * CORESIP_SetTipoGRS.	...
         * Funcion para activar en la Coresip un accId para que funcione como una radio 
         * @param	accId           Account de la Coresip
         * @param	Flags           Opciones de la radio
         * @return	CORESIP_OK OK, CORESIP_ERROR  error.
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetTipoGRS(int accId, CORESIP_CallFlags Flag, CORESIP_Error error);

        /**
         * CORESIP_SetImpairments.	...
         * Funcion para activar inperfecciones en la señal de audio que se envia por rtp 
         * @param	call          Call id de la llamada
         * @param	impairments   Define las inperfecciones que queremos que se ejecuten
         * @return	CORESIP_OK OK, CORESIP_ERROR  error.
         */
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
	    static extern int CORESIP_SetImpairments(int call, CORESIP_Impairments impairments, CORESIP_Error error);

		#endregion
		
#if _ED137_
		// PlugTest FAA 05/2011
		/// <summary>
		/// 
		/// </summary>
		public static event UpdateOvrCallMembersCb UpdateOvrCallMembers
		{
			add { _Cb.OnUpdateOvrCallMembers += value; }
			remove { _Cb.OnUpdateOvrCallMembers -= value; }
		}

		// PlugTest FAA 05/2011
		/// <summary>
		/// 
		/// </summary>
		public static event InfoCRDCb InfoCRD
		{
			add { _Cb.OnInfoCrd += value; }
			remove { _Cb.OnInfoCrd -= value; }
		}
#endif		
        
		/// <summary>
        /// 
        /// </summary>
        static InfoReceivedCb OnInfoReceived;
        public static event InfoReceivedCb InfoReceived
		{
			add {/*_Cb.*/OnInfoReceived += value; }
			remove {/*_Cb.*/OnInfoReceived -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static KaTimeoutCb OnKaTimeout;
        public static event KaTimeoutCb KaTimeout
		{
			add { /*_Cb.*/OnKaTimeout += value; }
			remove { /*_Cb.*/OnKaTimeout -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static IncomingSubscribeConfCb OnIncomingSubscribeConf;
        public static event IncomingSubscribeConfCb IncomingSubscribeConf
        {
            add {/*_Cb.*/OnIncomingSubscribeConf += value; }
            remove {/*_Cb.*/OnIncomingSubscribeConf -= value; }
        }

        static IncomingSubscribeConfAccCb OnIncomingSubscribeConfAcc;
        public static event IncomingSubscribeConfAccCb IncomingSubscribeConfAcc
        {
            add {/*_Cb.*/OnIncomingSubscribeConfAcc += value; }
            remove {/*_Cb.*/OnIncomingSubscribeConfAcc -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        static RdInfoCb OnRdInfo;
        public static event RdInfoCb RdInfo
		{
			add { /*_Cb.*/OnRdInfo += value; }
			remove { /*_Cb.*/OnRdInfo -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static CallStateCb OnCallState;
        public static event CallStateCb CallState
		{
			add { /*_Cb.*/OnCallState += value; }
			remove { /*_Cb.*/OnCallState -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static CallIncomingCb OnCallIncoming;
        public static event CallIncomingCb CallIncoming
		{
			add {/*_Cb.*/OnCallIncoming += value; }
			remove {/*_Cb.*/OnCallIncoming -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static TransferRequestCb OnTransferRequest;
        public static event TransferRequestCb TransferRequest
		{
			add {/*_Cb.*/OnTransferRequest += value; }
			remove {/*_Cb.*/OnTransferRequest -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static TransferStatusCb OnTransferStatus;
        public static event TransferStatusCb TransferStatus
		{
			add {/*_Cb.*/OnTransferStatus += value; }
			remove {/*_Cb.*/OnTransferStatus -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        static ConfInfoCb OnConfInfo;
        public static event ConfInfoCb ConfInfo
		{
			add {/*_Cb.*/OnConfInfo += value; }
			remove {/*_Cb.*/OnConfInfo -= value; }
		}

        static ConfInfoAccCb OnConfInfoAcc;
        public static event ConfInfoAccCb ConfInfoAcc
        {
            add {/*_Cb.*/OnConfInfoAcc += value; }
            remove {/*_Cb.*/OnConfInfoAcc -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        static DialogNotifyCb OnDialogNotify;
        public static event DialogNotifyCb DialogNotify
        {
            add {/*_Cb.*/OnDialogNotify += value; }
            remove {/*_Cb.*/OnDialogNotify -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        static PagerCb OnPager;
        public static event PagerCb Pager
        {
            add {/*_Cb.*/OnPager += value; }
            remove {/*_Cb.*/OnPager -= value; }
        }

        static WG67NotifyCb OnWG67Notify;
		public static event WG67NotifyCb WG67Notify
		{
			add {/*_Cb.*/OnWG67Notify += value; }
			remove {/*_Cb.*/OnWG67Notify -= value; }
		}

        /// <summary>
        /// 
        /// </summary>
        public static OptionsReceiveCb OnOptionsReceive;		
        public static event OptionsReceiveCb OptionsReceive
		{
			add { /*_Cb.*/OnOptionsReceive += value; }
			remove { /*_Cb.*/OnOptionsReceive -= value; }
		}
        /// <summary>
        /// 
        /// </summary>
        public static CfwrOptReceivedAccCb OnCfwrOptReceived;
        public static event CfwrOptReceivedAccCb CfwrOptReceived
        {
            add { /*_Cb.*/OnCfwrOptReceived += value; }
            remove { /*_Cb.*/OnCfwrOptReceived -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        //public static CfwrOptResponseCb OnCfwrOptResponse;
        //public static event CfwrOptResponseCb CfwrOptresponse
        //{
        //    add { /*_Cb.*/OnCfwrOptResponse += value; }
        //    remove { /*_Cb.*/OnCfwrOptResponse -= value; }
        //}
        /// <summary>
        /// 
        /// </summary>
        public static CfwrOptResponseAccCb OnCfwrOptResponse;
        public static event CfwrOptResponseAccCb CfwrOptResponse
        {
            add { /*_Cb.*/OnCfwrOptResponse += value; }
            remove { /*_Cb.*/OnCfwrOptResponse -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static MovedTemporallyCb OnMovedTemporally;
        public static event MovedTemporallyCb MovedTemporally
        {
            add { /*_Cb.*/OnMovedTemporally += value; }
            remove { /*_Cb.*/OnMovedTemporally -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        static SubPresCb OnSubPres;
        public static event SubPresCb SubPres
        {
            add {/*_Cb.*/OnSubPres += value; }
            remove {/*_Cb.*/OnSubPres -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId">Identificador del Agente (string libre)</param>
        /// <param name="ip">Direccion IP del Agente para las sesiones SIP.</param>
        /// <param name="port">Puerto UDP del Agente para las sesiones SIP.</param>
        /// <param name="max_calls">Maxima cantidad de llamadas que soporta.</param>
        /// <param name="proxy_ip">Si no es null es la Ip del proxy para rutear los paquetes SIP.</param>
        /** 20180208. */
        static bool IsInitialized = false;
        static bool IsStarted = false;
        public static void Init(string accId, string ip, uint port, uint max_calls = 32, string proxyIP=null)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.Init");
#endif
            /** 20180208 */
            if (!IsInitialized)
            {
                IsInitialized = true;

                _Ip = ip;
                _Port = port;

                /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
                CallbacksInit();

                /** */
                CORESIP_Config cfg = new CORESIP_Config();

                cfg.HostId = accId;     //GRABACION VOIP
                cfg.IpAddress = ip;
                cfg.Port = port;
                cfg.Cb = _Cb;
                cfg.DefaultCodec = Settings.Default.DefaultCodec;
                cfg.DefaultDelayBufPframes = Settings.Default.DefaultDelayBufPframes;
                cfg.DefaultJBufPframes = Settings.Default.DefaultJBufPframes;
                cfg.SndSamplingRate = Settings.Default.SndSamplingRate;
                cfg.RxLevel = Settings.Default.RxLevel;
                cfg.TxLevel = Settings.Default.TxLevel;
                cfg.LogLevel = Settings.Default.SipLogLevel;
                cfg.TsxTout = Settings.Default.TsxTout;
                cfg.InvProceedingIaTout = Settings.Default.InvProceedingIaTout;
                cfg.InvProceedingMonitoringTout = Settings.Default.InvProceedingMonitoringTout;
                cfg.InvProceedingDiaTout = Settings.Default.InvProceedingDiaTout;
                cfg.InvProceedingRdTout = Settings.Default.InvProceedingRdTout;

                // AGL 20131121.
                cfg.EchoTail = Settings.Default.EchoTail;
                cfg.EchoLatency = Settings.Default.EchoLatency;
                // FM           

                /// JCAM 18/01/2016
                /// Grabación según norma ED-137
                cfg.RecordingEd137 = Settings.Default.RecordingEd137;

                cfg.max_calls = max_calls;     // Maximo número de llamadas por defecto en el puesto
                cfg.TimeToDiscardRdInfo = Settings.Default.SQSourceConfirmTime;

                cfg.DIA_TxAttenuation_dB = Settings.Default.DIA_TxAttenuation_dB;
                cfg.IA_TxAttenuation_dB = Settings.Default.IA_TxAttenuation_dB;
                cfg.RD_TxAttenuation_dB = Settings.Default.RD_TxAttenuation_dB;
 
                CORESIP_Error err;
                if (CORESIP_Init(cfg, out err) != 0)
                {
                    throw new Exception(err.Info);
                }

                CORESIP_Params sipParams = new CORESIP_Params();

                sipParams.EnableMonitoring = 0;
                sipParams.KeepAlivePeriod = Settings.Default.KAPeriod;
                sipParams.KeepAliveMultiplier = Settings.Default.KAMultiplier;

                SetParams(sipParams);

                try
                {
                    if (proxyIP == null)
                        //Crea una cuenta que sólo existe cuando no hay configuración
                        SipAgent.CreateAccount(accId);
                    else
                        SipAgent.CreateAccountProxyRouting(accId, proxyIP);
                }
                catch (Exception exc)
                {
                    _Logger.Error("CreateAccount", exc);
                }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.Init");
#endif
            }
		}
        /// <summary>
        /// 
        /// </summary>
		public static void Start()
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.Start");
#endif
            /** 20180208 */
            if (!IsStarted)
            {
                CORESIP_Error err;
                if (CORESIP_Start(out err) != 0)
                {
                    throw new Exception(err.Info);
                }
                IsStarted = true;
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.Start");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
		public static void End()
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.End");
#endif
            if (IsStarted || IsInitialized)
            {

                CORESIP_End();
                _Accounts.Clear();
                IsInitialized = IsStarted = false;
            }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.End");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
		public static void SetLogLevel(LogLevel level)
		{
			uint eqLevel = 0;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SetLogLevel {0}", level.Name);
#endif
            if (level == LogLevel.Fatal)
			{
				eqLevel = 1;
			}
			else if (level == LogLevel.Error)
			{
				eqLevel = 2;
			}
			else if (level == LogLevel.Warn)
			{
				eqLevel = 3;
			}
			else if (level == LogLevel.Info)
			{
				eqLevel = 4;
			}
			else if (level == LogLevel.Debug)
			{
				eqLevel = 5;
			}
			else if (level == LogLevel.Trace)
			{
				eqLevel = 6;
			}
			CORESIP_Error err;
			CORESIP_SetLogLevel(eqLevel, out err);
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SetLogLevel");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
		public static void SetParams(CORESIP_Params cfg)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SetParmas");
#endif
            CORESIP_Error err;
			CORESIP_SetParams(cfg, out err);
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SetParams");
#endif
        }

        /**
         *	CreateAccount. Registra una cuenta SIP en el Módulo. @ref SipAgent::CreateAccount
         *	@param	acc			Puntero al Numero de Abonado (usuario). NO a la uri.
         *	@param	defaultAcc	Marca si esta cuenta pasa a ser la Cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        public static void CreateAccount(string accId)
		{
			int id;
			CORESIP_Error err;
			string sipAcc = string.Format("<sip:{0}@{1}:{2}>", accId, _Ip, _Port);

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateAccount");
#endif
            if (CORESIP_CreateAccount(sipAcc, 0, out id, out err) != 0)
			{
				throw new Exception(err.Info);
			}
			_Accounts[accId] = id;
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateAccount");
#endif
        }

        /**
         *	CORESIP_CreateAccountProxyRouting. Registra una cuenta SIP en el Módulo y los paquetes sip se enrutan por el proxy. @ref SipAgent::CreateAccount
         *	@param	acc			Puntero al Numero de Abonado (usuario). NO a la uri.
         *	@param	defaultAcc	Marca si esta cuenta pasa a ser la Cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado.
         *  @param	proxy_ip	Si es distinto de NULL. IP del proxy Donde se quieren enrutar los paquetes.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        public static void CreateAccountProxyRouting(string accId, string proxyIP)
        {
            int id;
            CORESIP_Error err;
            string sipAcc = string.Format("<sip:{0}@{1}:{2}>", accId, _Ip, _Port);

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateAccount");
#endif
            //Cuenta por defecto
            if (CORESIP_CreateAccountProxyRouting(sipAcc, 1, out id, proxyIP, out err) != 0)
            {
                throw new Exception(err.Info);
            }
            _Accounts[accId] = id;
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateAccount");
#endif
        }

        /**
         *	CreateAccountAndRegisterInProxy. Crea una cuenta y se registra en el SIP proxy. Los paquetes sip se rutean por el SIP proxy también.
         *	@param	acc			Puntero al Numero de Abonado (usuario). NO a la uri.
         *	@param	defaultAcc	Si es diferente a '0', indica que se creará la cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado que retorna.
         *	@param	proxy_ip	IP del proxy.
         *	@param	expire_seg  Tiempo en el que expira el registro en segundos.
         *	@param	username	Si no es necesario autenticación, este parametro será NULL
         *	@param  pass		Password. Si no es necesario autenticación, este parametro será NULL
         *	@param  DisplayName	Display name que va antes de la sip URI, se utiliza para como nombre a mostrar
         *	@param	isfocus		Si el valor es true, indica que es Focus, para establecer llamadas multidestino. por defecto es falso.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        public static void CreateAccountAndRegisterInProxy(string accId, string proxy_ip, uint expire_seg, string username, string pass, string displayName, bool isFocus = false)
        {
            int id;
            CORESIP_Error err;
            int isFocusArg = 0;

            if (isFocus) isFocusArg = 1;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateAccountAndRegisterInProxy");
#endif
            if (CORESIP_CreateAccountAndRegisterInProxy(accId, 0, out id, proxy_ip, expire_seg, username, pass, displayName, isFocusArg, out err) != 0)
            {
                throw new Exception(err.Info);
            }
            _Accounts[accId] = id;
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateAccountAndRegisterInProxy");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        public static void DestroyAccount(string accId)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyAccount");
#endif
            CORESIP_Error err;

            if (CORESIP_DestroyAccount(_Accounts[accId], out err) != 0)
            {
                _Logger.Error("SipAgent.DestroyAccount: " + err.Info);
            }

            _Accounts.Remove(accId);
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyAccount");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
		public static void DestroyAccounts()
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyAccounts");
#endif
            foreach (int id in _Accounts.Values)
			{
				CORESIP_Error err;

				if (CORESIP_DestroyAccount(id, out err) != 0)
				{
                    _Logger.Error("SipAgent.DestroyAccounts: " + err.Info);
				}
			}
			_Accounts.Clear();
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyAccounts");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inDevId"></param>
        /// <param name="outDevId"></param>
        /// <returns></returns>
		public static int AddSndDevice(CORESIP_SndDevType type, int inDevId, int outDevId)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.AddSndDevice {0}, {1}, {2}", type.ToString(), inDevId, outDevId);
#endif
            CORESIP_SndDeviceInfo info = new CORESIP_SndDeviceInfo();

			info.Type = type;
			info.OsInDeviceIndex = inDevId;
			info.OsOutDeviceIndex = outDevId;

			int dev=0;
			CORESIP_Error err;

            if (CORESIP_AddSndDevice(info, out dev, out err) != 0)            
            {            
                throw new Exception(err.Info);                
            }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.AddSndDevice");
#endif
            return dev;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
		public static int CreateWavPlayer(string file, bool loop)
		{
			int wavPlayer;
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateWavPlayer {0}, {1}", file, loop);
#endif

			if (CORESIP_CreateWavPlayer(file, loop ? 1 : 0, out wavPlayer, out err) != 0)
			{
				throw new Exception(err.Info);
			}

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateWavPlayer");
#endif
            return wavPlayer;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wavPlayer"></param>
		public static void DestroyWavPlayer(int wavPlayer)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyWavPlayer {0}", wavPlayer);
#endif

			if (CORESIP_DestroyWavPlayer(wavPlayer, out err) != 0)
			{
                _Logger.Error("SipAgent.DestroyWavPlayer: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyWavPlayer");
#endif
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		public static int CreateWavRecorder(string file)
		{
			int wavRecorder;
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateWavRecorder {0}", file);
#endif

			if (CORESIP_CreateWavRecorder(file, out wavRecorder, out err) != 0)
			{
				throw new Exception(err.Info);
			}

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateWavRecorder");
#endif
            return wavRecorder;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="wavRecorder"></param>
		public static void DestroyWavRecorder(int wavRecorder)
		{
            CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyWavRecorder {0}", wavRecorder);
#endif
            if (CORESIP_DestroyWavRecorder(wavRecorder, out err) != 0)
			{
				_Logger.Error("SipAgent.DestroyWavRecorder: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyWavRecorder");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="localIp"></param>
        /// <returns></returns>
		public static int CreateRdRxPort(RdSrvRxRs rs, string localIp)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateRdRxPort {0}, {1}", rs.ToString(), localIp);
#endif
            CORESIP_RdRxPortInfo info = new CORESIP_RdRxPortInfo();

			info.ClkRate = rs.ClkRate;
			info.ChannelCount = rs.ChannelCount;
			info.BitsPerSample = rs.BitsPerSample;
			info.FrameTime = rs.FrameTime;
			info.Ip = rs.McastIp;
			info.Port = rs.RdRxPort;

			int mcastPort;
			CORESIP_Error err;

			if (CORESIP_CreateRdRxPort(info, localIp, out mcastPort, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateRdRxPort");
#endif

            return mcastPort;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
		public static void DestroyRdRxPort(int port)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyRdRxPort {0}", port);
#endif

			if (CORESIP_DestroyRdRxPort(port, out err) != 0)
			{
                _Logger.Error("SipAgent.DestroyRdrxPort: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyRdRxPort");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
		public static int CreateSndRxPort(string name)
		{
			int sndRxPort;
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateSndRxPort {0}", name);
#endif
            if (CORESIP_CreateSndRxPort(name, out sndRxPort, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateSndRxPort");
#endif
			return sndRxPort;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
		public static void DestroySndRxPort(int port)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroySndRxPort {0}", port);
#endif
            if (CORESIP_DestroySndRxPort(port, out err) != 0)
			{
                _Logger.Error("SipAgent.DestroySndRxPort: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroySndRxPort");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
		public static void MixerLink(int srcId, int dstId)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MixerLink {0}, {1}", srcId, dstId);
#endif
			if (CORESIP_BridgeLink(srcId, dstId, 1, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MixerLink");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
		public static void MixerUnlink(int srcId, int dstId)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MixerUnLink {0},{1}", srcId, dstId);
#endif
            if (CORESIP_BridgeLink(srcId, dstId, 0, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MixerUnLink");
#endif
        }
        /// <summary>
        /// Usado para la transmision del programa HMI hacia el nodebox-master. Cuando se hace PTT.
        /// </summary>
        /// <param name="sndDevId">Identificador de dispositivo asociado a un microfono</param>
        /// <param name="id">Identificador asociado al nodebox-master.</param>
        /// <param name="ip">Direccion mcast en la que escucha el nodebox-master</param>
        /// <param name="port">Puesto UDP asociado al grupo mcast donde escucha el nodebox-master.</param>
		public static void SendToRemote(int sndDevId, string id, string ip, uint port)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendToRemte {0}, {1}, {2}, {3}", sndDevId, id, ip, port);
#endif
            if (CORESIP_SendToRemote(sndDevId, 1, id, ip, port, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendtoRemote");
#endif
        }
        /// <summary>
        /// Usado por el nodebox-master para recibir tramas del HMI....
        /// </summary>
        /// <param name="localIp"></param>
        /// <param name="mcastIp"></param>
        /// <param name="mcastPort"></param>
		public static void ReceiveFromRemote(string localIp, string mcastIp, uint mcastPort)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.ReceiveFromRemote {0}, {1}, {2}", localIp, mcastIp, mcastPort);
#endif
            if (CORESIP_ReceiveFromRemote(localIp, mcastIp, mcastPort, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.ReceiveFromRemote");
#endif
        }
        /// <summary>
        /// Usado por el HMI para 'redireccionar' su transmision. YA no va hacia el nodebox-master. (por ejemplo para cuando utiliza telefonia).
        /// </summary>
        /// <param name="sndDevId">Identificador del dispositivo asociado a un micrófono.</param>
		public static void UnsendToRemote(int sndDevId)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.UnsendToRemote {0}", sndDevId);
#endif
            if (CORESIP_SendToRemote(sndDevId, 0, null, null, 0, out err) != 0)
			{
                _Logger.Error("SipAgent.UnsendToRemote: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.UnsendToRemote");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="volume"></param>
		public static void SetVolume(int id, int volume)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SetVolume {0}, {1}", id, volume);
#endif
            if (CORESIP_SetVolume(id, volume, out err) != 0)
			{
                _Logger.Error("Error SipAgent.SetVolume {0}", err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SetVolume");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
		public static int GetVolume(int id)
		{
			int volume;
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.GetVolume {0}", id);
#endif
            if (CORESIP_GetVolume(id, out volume, out err) != 0)
			{
				throw new Exception(err.Info);
			}

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.GetVolume");
#endif
            return volume;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="referBy"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
		public static int MakeTlfCall(string accId, string dst, string referBy, CORESIP_Priority priority)
		{
            int retorno = MakeTlfCall(accId, dst, referBy, priority, CORESIP_CallFlags.CORESIP_CALL_NO_FLAGS);
            return retorno;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="referBy"></param>
        /// <param name="priority"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
		public static int MakeTlfCall(string accId, string dst, string referBy, CORESIP_Priority priority, CORESIP_CallFlags flags)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MakTlfCall {0}, {1}, {2}, {3}, {4}", accId, dst, referBy, priority, flags);
#endif
            int acc;
			if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
			{
                _Logger.Warn("Llamada con account Id desconocida -1" + accId);
				acc = -1;
			}

			CORESIP_CallInfo info = new CORESIP_CallInfo();
			CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

			info.AccountId = acc;
			info.Type = CORESIP_CallType.CORESIP_CALL_DIA;
			info.Priority = priority;
			info.Flags = flags;

			outInfo.DstUri = dst;
			outInfo.ReferBy = referBy;
            outInfo.RequireReplaces = false;

			int callId;
			CORESIP_Error err;

			if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
			{
				throw new Exception(err.Info);
			}

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MakeTlfCall");
#endif
            return callId;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="referBy"></param>
        /// <param name="priority"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static int MakeTlfCallReplaces(string accId, string dst, CORESIP_Priority priority, CORESIP_CallFlags flags, string callIdReplace,
            string toTag, string fromTag)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MakTlfCall {0}, {1}, {2}, {3}, {4}", accId, dst, referBy, priority, flags);
#endif
            int acc;
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
            {
                _Logger.Warn("Llamada con account Id desconocida -1" + accId);
                acc = -1;
            }

            CORESIP_CallInfo info = new CORESIP_CallInfo();
            CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
            info.Type = CORESIP_CallType.CORESIP_CALL_DIA;
            info.Priority = priority;
            info.Flags = flags;

            outInfo.DstUri = dst;
            outInfo.RequireReplaces = true;
            outInfo.ToTag = fromTag;
            outInfo.FromTag = toTag;
            outInfo.CallIdToReplace = callIdReplace;
            outInfo.EarlyOnly = true;
             int callId;
            CORESIP_Error err;

            if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
            {
                throw new Exception(err.Info);
            }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MakeTlfCall");
#endif
            return callId;
        }        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static int MakeLcCall(string accId, string dst, CORESIP_CallFlags flags)
		{
			int acc;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MakLcCall {0}, {1}", accId, dst);
#endif
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
			{
				acc = -1;
			}

			CORESIP_CallInfo info = new CORESIP_CallInfo();
			CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
			info.Type = CORESIP_CallType.CORESIP_CALL_IA;
			info.Priority = CORESIP_Priority.CORESIP_PR_URGENT;
            info.Flags = flags;

			outInfo.DstUri = dst;

			int callId;
			CORESIP_Error err;

			if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MakeLcCall");
#endif
			return callId;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="frecuency"></param>
        /// <param name="flags"></param>
        /// <param name="mcastIp">Grupo Multicast de Recepcion para los HMI.</param>
        /// <param name="mcastPort">Puerto del grupo asociado al recurso radio.</param>
        /// /// <param name="porcentajeRSSI">Peso del valor de Qidx del tipo RSSI en el calculo del Qidx final. 0 indica que el calculo es solo centralizado. 10 que el calculo es solo el RSSI.</param>
        /// <returns></returns>
		public static int MakeRdCall(string accId, string dst, string frecuency, CORESIP_CallFlags flags, string mcastIp, uint mcastPort,
            CORESIP_Priority prioridad, string Zona, CORESIP_FREQUENCY_TYPE FrequencyType,
            CORESIP_CLD_CALCULATE_METHOD CLDCalculateMethod, int BssWindows, bool AudioSync, bool AudioInBssWindow, bool NotUnassignable,
            int cld_supervision_time, string bss_method, uint porcentajeRSSI = 0)
		{
            int acc;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MakRdCall {0}, {1}, {2}, {3}, {4}, {5}, {6}", accId, dst, frecuency, flags, mcastIp,mcastPort,prioridad);
#endif
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
			{
				acc = -1;
			}

			CORESIP_CallInfo info = new CORESIP_CallInfo();
			CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

			info.AccountId = acc;
			info.Type = CORESIP_CallType.CORESIP_CALL_RD;
            /* AGL*/
            info.Priority = prioridad;  // CORESIP_Priority.CORESIP_PR_EMERGENCY;
			info.Flags = flags;
#if _VOTER_
			/** 20160609 */
            info.PreferredBss = 0;          // Globales.IndexOfPreferredBss;
            info.PreferredCodec = 0;        // Globales.IndexOfPreferredCodec;
#endif
            //EDU 20170223
            info.Zona = Zona;
            info.FrequencyType = FrequencyType;
            info.CLDCalculateMethod = CLDCalculateMethod;
            info.BssWindows = BssWindows;
            info.AudioSync = AudioSync;
            info.AudioInBssWindow = AudioInBssWindow;
            info.NotUnassignable = NotUnassignable;
            info.cld_supervision_time = cld_supervision_time;
            info.bss_method = bss_method;
            info.porcentajeRSSI = porcentajeRSSI;

			outInfo.DstUri = dst;
			outInfo.RdFr = frecuency;
			outInfo.RdMcastAddr = mcastIp;
			outInfo.RdMcastPort = mcastPort;

			int callId = -1;
			CORESIP_Error err;
            try
            {
                if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
                {
                    _Logger.Error("SipAgent.MakeRdCall: [" + outInfo.DstUri + "]" + err.Info);
                }
            }
            catch (Exception excep)
            {
                _Logger.Error("SipAgent.MakeRdCall exception: [" + outInfo.DstUri + "]", excep);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MakeRdCall");
#endif
			return callId;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static int MakeMonitoringCall(string accId, string dst, CORESIP_CallFlags flags)
		{
            //El HMI de ULises solo soporta G/G monitoring
            return MakeMonitoringCall(accId, dst, CORESIP_CallType.CORESIP_CALL_GG_MONITORING, flags);                                    
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int MakeMonitoringCall(string accId, string dst, CORESIP_CallType type, CORESIP_CallFlags flags)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.MakeMonitoringCall {0}, {1}, {2}", accId, dst, type);
#endif
            int acc;
			if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
			{
				acc = -1;
			}

			CORESIP_CallInfo info = new CORESIP_CallInfo();
			CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

			info.AccountId = acc;
			info.Type = type;
			info.Priority = CORESIP_Priority.CORESIP_PR_NONURGENT;  // .CORESIP_PR_NORMAL;
            info.Flags = flags;

			outInfo.DstUri = dst;

			int callId;
			CORESIP_Error err;

			if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.MakeMonitoringCall");
#endif
			return callId;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
		public static void HangupCall(int callId)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.HangupCall {0}", callId);
#endif
            if (CORESIP_CallHangup(callId, 0, out err) != 0)
			{
                _Logger.Error("SipAgent.HangupCall_1: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.HangupCall");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="code"></param>
		public static void HangupCall(int callId, int code)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.HangupCall {0}, {1}", callId, code);
#endif
            if (CORESIP_CallHangup(callId, code, out err) != 0)
			{
                _Logger.Error("SipAgent.HangupCall_2: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.HangupCall");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="response"></param>
		public static void AnswerCall(int callId, int response)
		{
			AnswerCall(callId, response, false);
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="response"></param>
        /// <param name="addToConference"></param>
		public static void AnswerCall(int callId, int response, bool addToConference)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.AnswerCall {0}, {1}, {2}", callId, response, addToConference);
#endif
#if !UNIT_TEST
            if (CORESIP_CallAnswer(callId, response, addToConference ? 1 : 0, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#endif
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.AnswerCall");
#endif
        }
        /// <summary>
        /// Envia una respuesta 302 (Moved Temporally) al INVITE
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="dst"> Uri del destino al que se desvia la llamada </param>
        /// <param name="reason"> Razon del desvio: "unconditional", "user-busy", etc.</param>
		public static void MovedTemporallyAnswerCall(int callId, string dst, string reason)
        {
            CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CORESIP_CallMovedTemporallyAnswer {0}, {1}, {2}", callId, dst, reason);
#endif
#if !UNIT_TEST
            if (CORESIP_CallMovedTemporallyAnswer(callId, dst, reason, out err) != 0)
            {
                throw new Exception(err.Info);
            }
#endif
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CORESIP_CallMovedTemporallyAnswer");
#endif
        }

        /// <summary>
        /// Esta funcion debe llamarse despues de recibirse la callback MovedTemporallyCb para aceptar o rechazar la redireccion de la llamada.
        /// </summary>
        /// <param name="callId">Identificador de Llamada</param>
        /// <param name="dstUri">Nueva request uri hacia donde se desvia la llamada. Si se rechaza entonces este parametro se ignora</param>
        /// <param name="op"> Opcion (aceptar o rechazar) </param>
        public static void CallProccessRedirect(int callId, string dstUri, CORESIP_REDIRECT_OP op)
        {
#if CALLFORWARD
            CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CORESIP_CallProccessRedirect {0}, {1}, {2}", callId, dstUri, op);
#endif
#if !UNIT_TEST
            if (CORESIP_CallProccessRedirect(callId, dstUri, op, out err) != 0)
            {
                throw new Exception(err.Info);
            }
#endif
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CORESIP_CallProccessRedirect");
#endif
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void HoldCall(int callId)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.HoldCall {0}", callId);
#endif
            if (CORESIP_CallHold(callId, 1, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.HoldCall");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
		public static void UnholdCall(int callId)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.UnholdCall {0}", callId);
#endif

			if (CORESIP_CallHold(callId, 0, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.UnholdCall");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="dstCallId"></param>
        /// <param name="dst"></param>
		public static void TransferCall(int callId, int dstCallId, string dst, string displayName)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.TransferCall {0}, {1}, {2}", callId, dstCallId, dst);
#endif
            if (CORESIP_CallTransfer(callId, dstCallId, dst, displayName, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.TransferCall");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pttId"></param>
        /// <param name="pttType"></param>
        public static void PttOn(int callId, ushort pttId, CORESIP_PttType pttType, CORESIP_PttMuteType pttMute)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.PttOn {0}, {1}, {2}", callId, pttId, pttType);
#endif
            CORESIP_PttInfo info = new CORESIP_PttInfo();

			info.PttType = pttType;
			info.PttId = pttId;
            if (pttType == CORESIP_PttType.CORESIP_PTT_OFF)
                info.PttMute = (uint) CORESIP_PttMuteType.DESACTIVADO;
            else
                info.PttMute = (uint) pttMute;
			CORESIP_Error err;

			if (CORESIP_CallPtt(callId, info, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.PttOn");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
		public static void PttOff(int callId)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.PttOff {0}", callId);
#endif
            CORESIP_PttInfo info = new CORESIP_PttInfo();
			info.PttType = CORESIP_PttType.CORESIP_PTT_OFF;
            info.PttMute = (uint)CORESIP_PttMuteType.DESACTIVADO;

			CORESIP_Error err;

			if (CORESIP_CallPtt(callId, info, out err) != 0)
			{
                _Logger.Warn("Error SipAgent.PttOff " + err.Info);
				//throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.PttOff");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
		public static void AddCallToConference(int callId)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.AddCallToConference {0}", callId);
#endif
            if (CORESIP_CallConference(callId, 1, out err) != 0)
			{
				throw new Exception(err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.AddCallToConference");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
		public static void RemoveCallFromConference(int callId)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.RemoveCallFromConference {0}", callId);
#endif
            if (CORESIP_CallConference(callId, 0, out err) != 0)
			{
                _Logger.Error("SipAgent.RemoveCallFromConference: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.RemoveCallFromConference");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="info"></param>
		public static void SendConfInfo(int callId, CORESIP_ConfInfo info)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendConfInfo {0}, {1}", callId, info.Version);
#endif

			if (CORESIP_CallSendConfInfo(callId, info, out err) != 0)
			{
                _Logger.Error("SipAgent.SendConfInfo: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendConfInfo");
#endif
        }
        /**
         * SendConfInfoFromAcc: Envia Notify con la info de la conferencia a todas las subscripciones al evento
         *						de conferencia que tiene un account
         * @param	accid	Account id
         * @param	conf	Info de de conferencia
         * @return	0 si no hay error
         */
        public static void SendConfInfoFromAcc(string accId, CORESIP_ConfInfo info)
        {
            CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendConfInfo {0}, {1}", callId, info.Version);
#endif

            if (CORESIP_SendConfInfoFromAcc(_Accounts[accId], info, out err) != 0)
            {
                _Logger.Error("SipAgent.SendConfInfoFromAcc: " + err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendConfInfo");
#endif
        }


        /**
         * SendConfInfoToUri: Envia Notify con la info de la conferencia a un destino
         * @param	uri	    Uri destino del Notify
         * @param	conf	Info de de conferencia
         * @return	0 si no hay error
         */
        public static void SendConfInfoToUri(string uri, CORESIP_ConfInfo info)
        {
            CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendConfInfo {0}, {1}", callId, info.Version);
#endif
            if (CORESIP_SendConfInfoToUri(uri, info, out err) != 0)
            {             
                _Logger.Error("SipAgent.SendConfInfoToUri: " + err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendConfInfo");
#endif
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="info"></param>
		public static void SendInfo(int callId, string info)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendInfo {0}, {1}", callId, info);
#endif
            if (CORESIP_CallSendInfo(callId, info, out err) != 0)
			{
                _Logger.Error("SipAgent.SendInfo: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendInfo");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst"></param>
        /// 
        /**
         *	SendOptionsMsg
         *  Esta función no envia OPTIONS a traves del proxy
         *	@param	dst			Puntero a uri donde enviar OPTIONS
         *  @param	callid		callid que retorna.
         *  @param	isRadio		Si tiene valor distinto de cero el agente se identifica como radio. Si es cero, como telefonia.
         *						Sirve principalmente para poner radio.01 o phone.01 en la cabecera WG67-version
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
		public static void SendOptionsMsg(string dst, out string callid, bool isRadio)
		{
			CORESIP_Error err;
            StringBuilder callid_ = new StringBuilder(CORESIP_MAX_CALLID_LENGTH + 1);            

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendOptionsMsg {0}", dst);
#endif
            int isrd = (isRadio) ? 1 : 0;
            if (CORESIP_SendOptionsMsg(dst, callid_, isrd, out err) != 0)
			{
				throw new Exception(err.Info);
			}
            callid = callid_.ToString();
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendOptionsMsg");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// 
        /**
         * SendOptionsCFWD.	...
         * Envia mensaje OPTIONS necesario para la negociacion Call Forward
         * @param	accId				Account de la Coresip que utilizamos.
         * @param	dst					Uri a la que se envia OPTIONS
         * @param	cfwr_options_type	Tipo de OPTIONS para la negociacion. Es del tipo CORESIP_CFWR_OPT_TYPE
         * @param	body				Contenido del body (XML). Acabado en '\0'
         * @param	callid				callid que se retorna, acabado en '\0'.
         * @param	by_proxy			TRUE si queremos que se envie a través del proxy. Agregara cabecera route
         */
        public static void SendOptionsCFWD(string accId, string dst, CORESIP_CFWR_OPT_TYPE cfwr_options_type, string body, out string callid, bool by_proxy)
        {

            CORESIP_Error err;
            StringBuilder callid_ = new StringBuilder(CORESIP_MAX_CALLID_LENGTH + 1);
#if CALLFORWARD
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendOptionsCFWD {0}", dst);
#endif
            if (CORESIP_SendOptionsCFWD(_Accounts[accId], dst, cfwr_options_type, body, callid_, by_proxy, out err) != 0)
            {
                //throw new Exception(err.Info);
                _Logger.Error("Error SipAgent.SendOptionsCFWD" + err.Info);
            }
#endif
            callid = callid_.ToString();
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendOptionsCFWD");
#endif

        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /**
         * CORESIP_SendResponseCFWD.	...
         * Envia la respuesta al options utilizado para la negociacion de call forward
         * @param	st_code				Code de la respuesta. Si no es 200 entonces se ignora el parametro del body
         * @param	body				Contenido del body (XML). Acabado en '\0'
         * @param	hresp				Manejador necesario para enviar la respuesta
         */
        public static void SendResponseCFWD(int st_code, string body, uint hresp)
        {
#if CALLFORWARD
            CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendResponseCFWD {0}", dst);
#endif
            if (CORESIP_SendResponseCFWD(st_code, body, hresp, out err) != 0)
            {
                throw new Exception(err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendResponseCFWD");
#endif
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tsxKey"></param>
        /// <param name="txData"></param>
        /// <param name="evSub"></param>
        /// <param name="code"></param>
		public static void TransferAnswer(string tsxKey, IntPtr txData, IntPtr evSub, int code)
		{
			CORESIP_Error err;

#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.TransferAnswer {0}, {1}, {2}, {3}", tsxKey, txData, evSub, code);
#endif
            if (CORESIP_TransferAnswer(tsxKey, txData, evSub, code, out err) != 0)
			{
                _Logger.Error("SipAgent.TransferAnswer: " + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.TransferAnswer");
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evSub"></param>
        /// <param name="code"></param>
		public static void TransferNotify(IntPtr evSub, int code)
		{
			CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.Transfer {0}, {1}", evSub, code);
#endif
            try
            {
                if (CORESIP_TransferNotify(evSub, code, out err) != 0)
                {
                    _Logger.Error("SipAgent.TransferNotify: " + err.Info);
                }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.TransferNotify");
#endif
            }
            catch (Exception exc)
            {
                _Logger.Error("SipAgent.TransferNotify: " + exc.Message);
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst"></param>
		/// <returns></returns>
		public static IntPtr CreateWG67Subscription(string dst)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateWG67Subscription {0}", dst);
#endif
            IntPtr wg67 = IntPtr.Zero;
			CORESIP_Error err = new CORESIP_Error();

			if (CORESIP_CreateWG67Subscription(dst, ref wg67, ref err) != 0)
			{
				_Logger.Error("Error creating WG67 KEY-IN Subscription" + err.Info);
			}

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateWG67Subcription");
#endif
            return wg67;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="wg67"></param>
		public static void DestroyWG67Subscription(IntPtr wg67)
		{
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyWG67Subscription {0}", wg67);
#endif
            CORESIP_Error err = new CORESIP_Error();

			if (CORESIP_DestroyWG67Subscription(wg67, ref err) != 0)
			{
				_Logger.Error("Error destroying WG67 KEY-IN Subscription" + err.Info);
			}
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyWG67Subscription");
#endif
        }

        /**
         *	CreateConferenceSubscription. Crea una subscripcion por evento de conferencia
         *	@param	accId		Identificador del account.
         *  @param  dst.	    Uri del destino al que nos subscribimos
         *  @param	by_proxy.   Si true entonces el subscribe se envia a traves del proxy
         *	@return				Codigo de Error
         */
        public static void CreateConferenceSubscription(string accId, string dst, bool byProxy = true)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateConferenceSubscription {0}", dst);
#endif
            CORESIP_Error err = new CORESIP_Error();

            if (CORESIP_CreateConferenceSubscription(_Accounts[accId], dst, byProxy, out err) != 0)
            {
                _Logger.Error("Error creating Conference Subscription" + err.Info);
            }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateConferenceSubscription");
#endif
            return;
        }

        /**
         *	DestroyConferenceSubscription. Destruye una subscripcion por evento de presencia
         *  @param  dest.	Uri del destino del que nos desubscribimos
         *	@param	error		Puntero a la Estructura de error
         *	@return				Codigo de Error
         */
        public static void DestroyConferenceSubscription(string dst)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyConferenceSubscription {0}", wg67);
#endif
            CORESIP_Error err = new CORESIP_Error();

            if (CORESIP_DestroyConferenceSubscription(dst, out err) != 0)
            {
                _Logger.Error("Error destroying Conference Subscription" + err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyConferenceSubscription");
#endif
        }

        /**
         *	CreateDialogSubscription. Crea una subscripcion por evento de dialogo
         *	@param	accId		Identificador del account.
         *  @param  dst.	    Uri del destino al que nos subscribimos
         *  @param	by_proxy.   Si true entonces el subscribe se envia a traves del proxy
         *	@return				error
         */
        public static bool CreateDialogSubscription(string accId, string dst, bool byProxy = false)
        {
            bool error = false;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.CreateDialogSubscription {0}", dst);
#endif
            CORESIP_Error err = new CORESIP_Error();
            if (CORESIP_CreateDialogSubscription(_Accounts[accId], dst, byProxy, out err) != 0)
            {
                _Logger.Error("Error creating Dialog Subscription" + err.Info);
                error = true;
            }

#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.CreateDialogSubscription");
#endif
            return error;
        }

        /**
         *	DestroyDestroySubscription. Destruye una subscripcion por evento de dialogo
         *  @param  dest.	Uri del destino del que nos desubscribimos
         *	@return				Codigo de Error
         */
        public static void DestroyDialogSubscription(string dst)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.DestroyDialogSubscription {0}", wg67);
#endif
            CORESIP_Error err = new CORESIP_Error();

            if (CORESIP_DestroyDialogSubscription(dst, out err) != 0)
            {
                _Logger.Error("Error destroying Dialog Subscription" + err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyDialogSubscription");
#endif
        }

        /**
          *	SendInstantMessage. Destruye una subscripcion por evento de dialogo
          *  @param  dest.	Uri del destino del que nos desubscribimos
          *	@return				Codigo de Error
          */
        public static void SendInstantMessage(string accId, string dest_uri, string text, bool by_proxy)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.SendInstantMessage {0}", dest_uri);
#endif
            CORESIP_Error error = new CORESIP_Error();
            CORESIP_SendInstantMessage(_Accounts[accId], dest_uri, text, by_proxy, out error);
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.SendInstantMessage");
#endif
        }
        /** AGL */
        public static void Wav2Remote(string file, string id, string ip, int port)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.WavToRemote {0}, {1}, {2}, {3}", file, id, ip, port);
#endif
            CORESIP_Error err = new CORESIP_Error();
            WavRemoteEnd cb = new WavRemoteEnd(Wav2RemoteEnd);
            CORESIP_Wav2RemoteStart(file, id, ip, port, ref cb, ref err);
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.Wav2Remote");
#endif
        }

        /** */
        public static void Wav2RemoteEnd(IntPtr obj)
        {
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.WavToRemoteEnd {0}", obj);
#endif
            CORESIP_Error err = new CORESIP_Error();
            CORESIP_Wav2RemoteEnd(obj, ref err);
            /** Meter un Evento.... */
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.WavToRemoteEnd");
#endif
        }

        // JCAM. 20170324
        /* GRABACION VOIP CFG */
        public static void PictRecordingCfg(string ipRecorder1, string ipRecorder2, int rtspPort)
        {
            CORESIP_Error err;
            bool result = false;

            String fullRecorderFileName = Settings.Default.RecorderServicePath + "\\"+ SipAgent.UG5K_REC_CONF_FILE;
            // Actualizar el fichero INI que maneja el módudo de grabación
            try
            {
                result = Native.Kernel32.WritePrivateProfileString("GENERAL", "DUAL_RECORDER", (!ipRecorder1.Equals("") && !ipRecorder2.Equals("")) ? "1" : "0",
                    fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("GENERAL", "MAX_SESSIONS", "2", fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("SERVICIO", "IP_SERVICIO", "127.0.0.1", fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("SERVICIO", "PORT_IN_SERVICIO", "65003", fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("RTSP", "IP_REC_A", ipRecorder1 ?? string.Empty, fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("RTSP", "IP_REC_B", ipRecorder2 ?? string.Empty, fullRecorderFileName);
                result |= Native.Kernel32.WritePrivateProfileString("RTSP", "PORT_RTSP", rtspPort.ToString(), fullRecorderFileName);
            }
            catch (Exception exc)
            {
                _Logger.Error("Error escribiendo fichero UG5K_REC_CONF_FILE: {0} exception {1} !!!", Marshal.GetLastWin32Error(), exc.Message);
            }
            _Logger.Debug("Modificado fichero {4} IP_REC_A:{0} IP_REC_B:{1} PORT_RTSP:{2} DUAL_RECORDER:{3}", ipRecorder1 ?? string.Empty, ipRecorder2 ?? string.Empty, rtspPort, (ipRecorder1 == null && ipRecorder2 == null) ? "0" : "1", fullRecorderFileName);
            if (result == false)
            {
                _Logger.Error("Error escribiendo fichero UG5K_REC_CONF_FILE: {0} !!!", Marshal.GetLastWin32Error());
            }
            // Notificar al módulo de grabación que ha cambiado la configuración.
            if (CORESIP_RecorderCmd(CORESIP_RecCmdType.CORESIP_REC_RESET, out err) != 0)
            {
                throw new Exception(err.Info);
            }
            _Logger.Debug("Notificado al módulo de grabación reinicio {0}");
        }

        /* GRABACION VOIP START */
        /** */
        public static void RdPttEvent(bool on, string freqId, int dev, uint priority)
        {
            CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.RdPttEvent {0}, {1}", freqId, dev);
#endif
            if (CORESIP_RdPttEvent(on, freqId, dev, out err, (CORESIP_PttType) priority) != 0)
            {
                throw new Exception(err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.RdPttEvent");
#endif
        }

        // JCAM. 20170323
        // RQ.012.04
        public static void RdSquEvent(bool on, string freqId, string qidxResource, string qidxMethod, uint qidxValue)
        {
            CORESIP_Error err;
#if _TRACEAGENT_
            _Logger.Debug("Entrando en SipAgent.RdSquEvent {0} Resource: {1} QidxMethod:{2} QidxValue:{3}", freqId,qidxResource,qidxMethod,qidxValue);
#endif
            if (CORESIP_RdSquEvent(on, freqId, qidxResource, qidxMethod, qidxValue, out err) != 0)
            {
                throw new Exception(err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.RdSquEvent");
#endif
        }

        /** 20180131. AGL. Para la gestion de presencia.... */
        //public static void SetPresenceSubscriptionCallBack(SubPresCb Presence_callback)
        //{
        //    CORESIP_Error err;
        //    if (CORESIP_SetPresenceSubscriptionCallBack(Presence_callback, out err) != 0)
        //    {
        //        throw new Exception(err.Info);
        //    }
        //}
        public static void CreatePresenceSubscription(string dest_uri)
        {
            CORESIP_Error err;
            if (CORESIP_CreatePresenceSubscription(dest_uri, out err) != 0)
            {
                throw new Exception(err.Info);
            }
        }
        public static void DestroyPresenceSubscription(string dest_uri)
        {
            CORESIP_Error err;
            if (CORESIP_DestroyPresenceSubscription(dest_uri, out err) != 0)
            {
                throw new Exception(err.Info);
            }
        }
        /*******************************/


        /* GRABACION VOIP END */

        /**
         * EchoCancellerLCMic.	...
         * Activa/desactiva cancelador de eco altavoz LC y Microfonos. Sirve para el modo manos libres 
         * Por defecto esta desactivado en la CORESIP
         * @param	on						true - activa / false - desactiva         
         */
        public static void EchoCancellerLCMic(bool on)
        {
            CORESIP_Error err;
            if (on != ECHandsFreeState)
            {
                ECHandsFreeState = on;
                if (CORESIP_EchoCancellerLCMic(on, out err) != 0)
                {
                    throw new Exception(err.Info);
                }
            }
        }

#region Private Members
        /// <summary>
        /// 
        /// </summary>
		private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		private static CORESIP_Callbacks _Cb = new CORESIP_Callbacks();
        /// <summary>
        /// 
        /// </summary>
		private static Dictionary<string, int> _Accounts = new Dictionary<string, int>();
        /// <summary>
        /// 
        /// </summary>
		private static string _Ip = null;
        public static string IP
        {
            get { return _Ip; }
        }
        /// <summary>
        /// 
        /// </summary>
		private static uint _Port = 0;

        /// <summary>
        /// Guarda el ultimo estado el ECHandsFree pedido a CORESIP, para evitar llamadas innecesarias
        /// Por defecto tiene el mismo valor que la CORESIP (false)
       /// </summary>
        private static bool ECHandsFreeState = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="data"></param>
        /// <param name="len"></param>
		private static void OnLogCb(int level, string data, int len)
		{
			LogLevel eqLevel = level < 7 ? LogLevel.FromOrdinal(6 - level) : LogLevel.Off;

            //_Logger.Log(eqLevel, data);
            _Logger.Debug(data);
		}

        /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
        private static void CallbacksInit()
        {
            if (Settings.Default.SipLogLevel <= 3)
		        _Cb.OnLog = new LogCb((p1, p2, p3) => 
                {                
                        OnLogCb(p1, p2, p3);
                });
            _Cb.OnKaTimeout = new KaTimeoutCb((p1) =>
            {
                if (OnKaTimeout != null)
                    OnKaTimeout(p1);
            });
            _Cb.OnRdInfo = new RdInfoCb((p1, p2) =>
            {
                if (OnRdInfo != null)
                    OnRdInfo(p1, p2);
            });
            _Cb.OnCallState = new CallStateCb((p1, p2, p3) =>
            {
                if (OnCallState != null)
                    OnCallState(p1, p2, p3);
            });
            _Cb.OnCallIncoming = new CallIncomingCb((p1, p2, p3, p4) =>
            {
                if (OnCallIncoming != null)
                    OnCallIncoming(p1, p2, p3, p4);
            });
            _Cb.OnTransferRequest = new TransferRequestCb((p1, p2, p3) =>
            {
                if (OnTransferRequest != null)
                    OnTransferRequest(p1, p2, p3);
            });
            _Cb.OnTransferStatus = new TransferStatusCb((p1, p2) =>
            {
                if (OnTransferStatus != null)
                    OnTransferStatus(p1, p2);
            });
            _Cb.OnConfInfo = new ConfInfoCb((p1, p2, p3, p4) =>
            {
                //p1 puede ser un callId o el identificador de la cuenta de SIP
                if ((p1 & SipAgent.CORESIP_ID_TYPE_MASK) == SipAgent.CORESIP_ACC_ID)
                {
                    foreach (string accountName in _Accounts.Keys)
                        if (_Accounts[accountName] == p1)
                        {
                            if (OnConfInfoAcc != null)
                                OnConfInfoAcc(accountName, p2, p3, p4);
                            break;
                        }
                }
                else
                if (OnConfInfo != null)
                    OnConfInfo(p1, p2, p3, p4);
            });
            _Cb.OnDialogNotify = new DialogNotifyCb((p1, p2) =>
            {
                if (OnDialogNotify != null)
                    OnDialogNotify(p1, p2);
            });
            _Cb.OnPager = new PagerCb((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10) =>
            {
                if (OnPager != null)
                    OnPager(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
            });
            _Cb.OnOptionsReceive = new OptionsReceiveCb((p1, p2, p3, p4, p5) =>
            {
                if (OnOptionsReceive != null)
                    OnOptionsReceive(p1, p2, p3, p4, p5);
            });
#if CALLFORWARD
            _Cb.OnCfwrOptReceived = new CfwrOptReceivedCb((p1, p2, p3, p4, p5) =>
            {
                foreach (string accountName in _Accounts.Keys)
                    if (_Accounts[accountName] == p1)
                    {
                        if (OnCfwrOptReceived != null)
                            OnCfwrOptReceived(accountName, p2, p3, p4, p5);
                        break;
                    }
            });

            _Cb.OnCfwrOptResponse = new CfwrOptResponseCb((p1, p2, p3, p4, p5, p6) =>
            {
                foreach (string accountName in _Accounts.Keys)
                    if (_Accounts[accountName] == p1)
                    {
                        if (OnCfwrOptResponse != null)
                            OnCfwrOptResponse(accountName,p2, p3, p4, p5, p6);
                        break;
                    }
            });

            _Cb.OnMovedTemporally = new MovedTemporallyCb((p1, p2) =>
            {
                if (OnMovedTemporally != null)
                    OnMovedTemporally(p1, p2);
            });
#endif
            _Cb.OnWG67Notify = new WG67NotifyCb((p1, p2, p3) =>
            {
                if (OnWG67Notify != null)
                    OnWG67Notify(p1, p2, p3);
            });
            _Cb.OnInfoReceived = new InfoReceivedCb((p1, p2, p3) =>
            {
                if (OnInfoReceived != null)
                    OnInfoReceived(p1, p2, p3);
            });
            _Cb.OnIncomingSubscribeConf = new IncomingSubscribeConfCb((p1, p2, p3) =>
            {
                //p1 puede ser un callId o el identificador de la cuenta de SIP
                if ((p1 & SipAgent.CORESIP_ID_TYPE_MASK) == SipAgent.CORESIP_ACC_ID)
                {                        
                    foreach (string accountName in _Accounts.Keys)
                        if (_Accounts[accountName] == p1)
                        {
                            if (OnIncomingSubscribeConfAcc != null)
                                OnIncomingSubscribeConfAcc(accountName, p2, p3);
                            break;
                        }                        
                }
                else
                if (OnIncomingSubscribeConf != null)
                    OnIncomingSubscribeConf(p1, p2, p3);
            });
            _Cb.OnSubPres = new SubPresCb((p1, p2, p3) =>
            {
                if (OnSubPres != null)
                    OnSubPres(p1, p2, p3);
            });
            _Cb.OnFinWavCb = null;
        }
#endregion
	}
}

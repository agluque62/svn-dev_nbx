using System;
using System.Collections.Generic;
using System.Text;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
	public static class Identifiers
	{
        /// <summary>
        /// 
        /// </summary>
		public const string CfgMasterTopic = "Cd40CfgSrv";
        /// <summary>
        /// 
        /// </summary>
		public const string CfgTopic = "Cd40Cfg";
        /// <summary>
        /// 
        /// </summary>
		public const string CfgRsId = "Cd40Cfg";
        /// <summary>
        /// 
        /// </summary>
		public const string RdMasterTopic = "Cd40RdSrv";
        /// <summary>
        /// 
        /// </summary>
		public const string RdTopic = "Cd40Rd";
        /// <summary>
        /// 
        /// </summary>
		public const string GwMasterTopic = "Cd40GwSrv";
        /// <summary>
        /// 
        /// </summary>
		public const string GwTopic = "Cd40Gw";
        /// <summary>
        /// 
        /// </summary>
		public const string TopTopic = "Cd40Top";
        /// <summary>
        /// 
        /// </summary>
        public const string PhoneTopic = "UvkiPhone";
        public const string PhoneMasterTopic = "UvkiPhoSrv";
        /// <summary>
        /// 
        /// </summary>
        public const short FR_RX_CHANGE_ASK_MSG = 1;
        /// <summary>
        /// 
        /// </summary>
		public const short FR_TX_CHANGE_ASK_MSG = 2;
        /// <summary>
        /// 
        /// </summary>
		public const short PTT_CHANGE_ASK_MSG = 3;
        /// <summary>
        /// 
        /// </summary>
		public const short RTX_GROUP_CHANGE_ASK_MSG = 4;
        /// <summary>
        /// 
        /// </summary>
		public const short FR_RX_CHANGE_RESPONSE_MSG = 51;
        /// <summary>
        /// 
        /// </summary>
		public const short FR_TX_CHANGE_RESPONSE_MSG = 52;
        /// <summary>
        /// 
        /// </summary>
		public const short PTT_CHANGE_RESPONSE_MSG = 53;
        /// <summary>
        /// 
        /// </summary>
        public const short FR_HF_TX_CHANGE_RESPONSE_MSG = 54;
        /// <summary>
        /// 
        /// </summary>
		public const short FR_TX_ASSIGNED_MSG = 100;

        /**
         * AGL. 20120706. Para 'sincronizar' las configuraciones por defecto.
         * */
        public const short CFG_SAVE_AS_DEFAULT_MSG = 200;
        public const short CFG_LOAD_DEFAULT = 201;
        public const short CFG_DEL_DEFAULT = 202;

        /**
         * AGL. 20140319. Mensajes para SELCAL.
         * */
        public const short SELCAL_PREPARE = 10;
        public const short SELCAL_PREPARE_RSP = 60;
        public const short SELCAL_SEND_TONES = 65;

        /**
         * AGL. 20141113. Mensajes para el estado Global de HF
         * */
        public const short HF_STATUS = 66;

        /** 20180316. MNDISABEDNODES */
        public const short MNDISABLED_NODES = 67;

        /// <summary>
        /// MENSAJES PARA CAMBIO DE EMPLAZAMIENTO
        /// </summary>
        public const short SITE_CHANGING_MSG = 300;
        public const short SITE_CHANGING_RSP = 301;

        /// <summary>
        /// Mensaje que sirve para publicar al resto, que un servicio es Master
        /// </summary>
        public const short IM_MASTER_MSG = 99;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
		public static string TypeId(Type t)
		{
			return t.Namespace + "." + t.Name;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
		public static Type GetType(string typeId)
		{
			return Type.GetType(typeId);
		}
	}
}

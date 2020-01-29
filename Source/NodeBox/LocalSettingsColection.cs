using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.NodeBox
{
    /// <summary>
    /// 
    /// </summary>
    public class LocalSettingsCollection
    {
        private const string nbxprop_ControlRemoto = "Control Remoto";
        private const string nbxprop_HistBaseOid = "OID Base para Historicos";
        private const string nbxprop_HistCommunity = "Comunidad SNMP para Historicos";
        private const string nbxprop_HistServer = "Direccion IP del Servidor de Historicos";
        private const string nbxprop_PuertoControlRemoto = "Puerto HTTP del Gestor";

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> NodeboxProperties = new Dictionary<string, string>();
        public Dictionary<string, string> RadioServiceProperties = new Dictionary<string, string>();
        public Dictionary<string, string> CfgServiceProperties = new Dictionary<string, string>();
        public Dictionary<string, string> TifxServiceProperties = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            NodeboxProperties.Add(nbxprop_ControlRemoto, Properties.Settings.Default.ControlRemoto.ToString());
            NodeboxProperties.Add(nbxprop_HistBaseOid, Properties.Settings.Default.HistBaseOid);
            NodeboxProperties.Add(nbxprop_HistCommunity, Properties.Settings.Default.HistCommunity);
            NodeboxProperties.Add(nbxprop_HistServer, Properties.Settings.Default.HistServer);
            NodeboxProperties.Add(nbxprop_PuertoControlRemoto, Properties.Settings.Default.PuertoControlRemoto.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        public void NbxPropertieSet(string query)
        {
            /** Quito el ? inicial */
            query.Replace("?", "");
            /** Subtituyo + por espacios */
            query.Replace("+", " ");
            /** Obtengo el comando y el valor */
            string[] val = query.Split('=');
            switch (val[0])
            {
                case nbxprop_ControlRemoto:
                    if (val != null && val.Length > 1 && (val[1].ToUpper() == "TRUE" || val[1].ToUpper() == "FALSE"))
                    {
                        // Properties.Settings.Default.ControlRemoto = val[1].ToUpper() == "TRUE" ? true : false;
                    }
                    break;
                case nbxprop_HistBaseOid:
                    break;
                case nbxprop_HistCommunity:
                    break;
                case nbxprop_HistServer:
                    break;
                case nbxprop_PuertoControlRemoto:
                    break;
            }
              
        }
        
    }
}

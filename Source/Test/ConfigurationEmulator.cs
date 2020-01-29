using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;
using U5ki.RdService.Gears;

namespace Test
{

    /// <summary>
    /// Class that is able to create a fake configuration object.
    /// </summary>
    /// <remarks>
    /// In order to make this class work, we need to manualy modify some of the proto configuration file. 
    /// TODO: Change that.
    /// TODO: Add the "serialize from an xml configuration file" option.
    /// </remarks>
    public class ConfigurationEmulator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Cd40Cfg RdServiceConfigurationGet()
        {
            Cd40Cfg output = new Cd40Cfg();

            // ConfiguracionGeneral
            ConfiguracionSistema configuracionSistema = new ConfiguracionSistema();
            configuracionSistema.PlanAsignacionUsuarios = new List<AsignacionUsuariosTV>();
            output.ConfiguracionGeneral = configuracionSistema;

            // ConfiguracionUsuario
            ConfiguracionUsuario configuracionUsuario = new ConfiguracionUsuario();
            output.ConfiguracionUsuarios.Add(configuracionUsuario);

            CfgUsuario user = new CfgUsuario();
            configuracionUsuario.User = user;

            user.Nombre = "Test";

            // CfgEnlaceExterno
            List<CfgEnlaceExterno> links = new List<CfgEnlaceExterno>();
            configuracionUsuario.RdLinks = links;

            // Frecuency
            links.Add(
                LinkGet("35000", true, Tipo_Frecuencia.UHF));
            links.Add(
                LinkGet("40000", true, Tipo_Frecuencia.UHF));
            links.Add(
                LinkGet("50000", true, Tipo_Frecuencia.UHF));
            links.Add(
                LinkGet("60000", true, Tipo_Frecuencia.VHF));
            links.Add(
                LinkGet("70000", true, Tipo_Frecuencia.VHF));

            // Nodes
            output.Nodes.Add(
                NodeGet("35000", Tipo_Frecuencia.UHF, true, 1));
            output.Nodes.Add(
                NodeGet("40000", Tipo_Frecuencia.UHF, true, 2));
            output.Nodes.Add(
                NodeGet("50000", Tipo_Frecuencia.VHF, true, 3));
            output.Nodes.Add(
                NodeGet("60000", Tipo_Frecuencia.VHF, true, 4));
            output.Nodes.Add(
                NodeGet("70000", Tipo_Frecuencia.VHF, true, 5));

            output.Nodes.Add(
                NodeGet(String.Empty, Tipo_Frecuencia.UHF, false, null,
                    new List<uint[]> { new uint[2] { 3000, 80000 } }));
            output.Nodes.Add(
                NodeGet(String.Empty, Tipo_Frecuencia.VHF, false, null,
                    new List<uint[]> { new uint[2] { 3000, 80000 } }));
            output.Nodes.Add(
                NodeGet(String.Empty, Tipo_Frecuencia.VHF, false, null,
                    new List<uint[]> { new uint[2] { 3000, 80000 } }));

            return output;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Cd40Cfg RdServiceConfigurationSemiRealGet()
        {
            Cd40Cfg output = new Cd40Cfg();

            // ConfiguracionGeneral
            ConfiguracionSistema configuracionSistema = new ConfiguracionSistema();
            configuracionSistema.PlanAsignacionUsuarios = new List<AsignacionUsuariosTV>();
            output.ConfiguracionGeneral = configuracionSistema;

            // ConfiguracionUsuario
            ConfiguracionUsuario configuracionUsuario = new ConfiguracionUsuario();
            output.ConfiguracionUsuarios.Add(configuracionUsuario);

            CfgUsuario user = new CfgUsuario();
            configuracionUsuario.User = user;

            user.Nombre = "Test";

            // CfgEnlaceExterno
            List<CfgEnlaceExterno> links = new List<CfgEnlaceExterno>();
            configuracionUsuario.RdLinks = links;

            // Frecuency
            links.Add(
                LinkGet("119.000", true, Tipo_Frecuencia.VHF));
            links.Add(
                LinkGet("120.300", true, Tipo_Frecuencia.VHF));
            links.Add(
                LinkGet("121.500", true, Tipo_Frecuencia.VHF));
            links.Add(
                LinkGet("121.750", true, Tipo_Frecuencia.VHF));
            links.Add(
                LinkGet("121.900", true, Tipo_Frecuencia.VHF));

            // Nodes
            output.Nodes.Add(
                NodeGet("119.000", Tipo_Frecuencia.VHF, true, 1, null, "192.168.2.204"));
            Globals.Test.Gears.GearsReal.Add("192.168.2.204");
            output.Nodes.Add(
                NodeGet("120.300", Tipo_Frecuencia.VHF, true, 2, null, "192.168.2.205"));
            Globals.Test.Gears.GearsReal.Add("192.168.2.205");
            output.Nodes.Add(
                NodeGet("121.500", Tipo_Frecuencia.VHF, true, 3, null, "192.168.2.206"));
            Globals.Test.Gears.GearsReal.Add("192.168.2.206");
            output.Nodes.Add(
                 NodeGet("121.750", Tipo_Frecuencia.VHF, true, 4, null, "192.168.2.207"));
            Globals.Test.Gears.GearsReal.Add("192.168.2.207");
            output.Nodes.Add(
                 NodeGet("121.900", Tipo_Frecuencia.VHF, true, 5, null, "192.168.2.208"));
            Globals.Test.Gears.GearsReal.Add("192.168.2.208");

            Node tmpNode = NodeGet(String.Empty, Tipo_Frecuencia.VHF, false, null,
                    new List<uint[]> { new uint[2] { 100, 500000000 } }, "192.168.2.209", 160);
            output.Nodes.Add(tmpNode);
            Globals.Test.Gears.GearsReal.Add(tmpNode.IpGestor);

            tmpNode = NodeGet(String.Empty, Tipo_Frecuencia.VHF, false, null,
                    new List<uint[]> { new uint[2] { 100, 500000000 } }, "192.168.2.210", 160);
            output.Nodes.Add(tmpNode);
            Globals.Test.Gears.GearsReal.Add(tmpNode.IpGestor);

            return output;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="supervision"></param>
        /// <param name="frecuencyType"></param>
        /// <returns></returns>
        private CfgEnlaceExterno LinkGet(String frecuency, Boolean supervision, Tipo_Frecuencia frecuencyType)
        {
            CfgEnlaceExterno output = new CfgEnlaceExterno();

            output.Literal = frecuency;
            output.FrecuenciaSintonizada = Convert.ToInt32(frecuency.Replace(".", ""));
            output.SupervisionPortadora = supervision;
            output.TipoFrecuencia = Convert.ToUInt32(frecuencyType);

            return output;
        }

        private Int32 idCount = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="frecuencyType"></param>
        /// <param name="isMaster"></param>
        /// <param name="priority"></param>
        /// <param name="frecuencyRange"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private Node NodeGet(
            String frecuency, 
            Tipo_Frecuencia frecuencyType, 
            Boolean isMaster, 
            Int32? priority,
            IList<uint[]> frecuencyRange = null,
            String ip = null,
            Int32? port = null)
        {
            idCount++;

            Node output = new Node();
            output.Id = "EQ" + idCount.ToString();
            output.SipUri = "sip:192.168.1." + idCount + ":5060";
            output.Oid = "oid";
            output.EsReceptor = true;
            output.EsTransmisor = false;
            output.TipoDeFrecuencia = frecuencyType;
            output.FrecuenciaPrincipal = frecuency;
            output.Prioridad = (uint)Convert.ToInt32(priority);            

            if (null != ip)
                output.IpGestor = ip;
            else
                output.IpGestor = "192.168.2." + idCount.ToString();
            output.SipUri = output.IpGestor;

            if (null != port)
                output.Puerto = (UInt32)port;
            else
                output.Puerto = 161;

            if (isMaster)
            {
                output.TipoDeCanal = Tipo_Canal.Monocanal;
                output.FormaDeTrabajo = Tipo_Formato_Trabajo.Principal;
            }
            else
            {
                output.TipoDeCanal = Tipo_Canal.Multicanal;
                output.FormaDeTrabajo = Tipo_Formato_Trabajo.Reserva;
            }

            if (null == frecuencyRange)
                return output;

            foreach (uint[] range in frecuencyRange)
            {
                HfRangoFrecuencias frecs = new HfRangoFrecuencias();
                frecs.fmin = range[0];
                frecs.fmax = range[1];
                output.Frecs.Add(frecs);
            }

            return output;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="frecuencyType"></param>
        /// <param name="frecuency"></param>
        /// <returns></returns>
        internal BaseGear GearGet(IPAddress ip, Tipo_Frecuencia frecuencyType, String frecuency)
        {
            BaseGear output = new BaseGear(
                NodeGet(
                    frecuency,
                    frecuencyType,
                    true,
                    1),
                null,
                null,
                null,
                null,
                null,
                null);

            output.IP = ip.ToString();
            output.Frecuency = frecuency;

            return output;
        }

    }
}

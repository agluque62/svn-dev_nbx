
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;


using u5ki.PhoneService;

using U5ki.Infrastructure;
using Utilities;
using System.IO;
using ProtoBuf;
using U5ki.NodeBox;
using System.ServiceProcess;
using System.Collections.Generic;

using System.Net;


namespace PhoneServiceUnitTests
{
    [TestClass]
    public class PhoneServiceResourcesUnitTest 
    {
        //Esta clase susituye a nivel estructural a NbxService
        private TestContext testContextInstance;
        private static PhoneService phs = new PhoneService();
       // private static NodeBoxSrv nbx = new NodeBoxSrv();
        public PhoneServiceResourcesUnitTest()  
        {
            //phs = (PhoneService) nbx.PhoneService;
        }
        /// <summary>
        ///Obtiene o establece el contexto de las pruebas que proporciona
        ///información y funcionalidad para la ejecución de pruebas actual.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Atributos de prueba adicionales
        //
        // Puede usar los siguientes atributos adicionales conforme escribe las pruebas:
        //
        // Use ClassInitialize para ejecutar el código antes de ejecutar la primera prueba en la clase
        [ClassInitialize()]
        public static void MyTestInit(TestContext testContext)
        {
            phs.Start();
            SipAgent.Init(
                    "U5KI",
                    "192.168.2.202", //OJO, poner la direccion propia
                    7060, 128);
            SipAgent.Start();
        }

        // Use ClassCleanup para ejecutar el código una vez ejecutadas todas las pruebas en una clase
        // [ClassCleanup()]
        public static void MyClassCleanup() {
            phs.Stop();
            SipAgent.End();
        }
        

        //Usar TestInitialize para ejecutar el código antes de ejecutar cada prueba 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            
        }
        //
        // Use TestCleanup para ejecutar el código una vez ejecutadas todas las pruebas
        [TestCleanup()]
        public void MyTestCleanup()
        {
            
        }

        #endregion
        [TestMethod]
        public void CfgPhoneService()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);
            //Un grupo
            Assert.AreEqual(1, phs.MDCalls.Count);
            MDCall grupo = phs.MDCalls[0];
            Assert.AreEqual("<sip:grupotest1@192.168.2.202:7060>", grupo.Uri);
            List<string> miembros = grupo.GroupMembers;
            Assert.AreEqual(1, miembros.Count);
            Assert.AreEqual<string>("<sip:218001@192.168.2.112:5060>", grupo.GroupMembers[0]);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[0].State);
        }

        [TestMethod]
        //Pruebas de metodo Equals reimplementado en MDCalls
        public void MDEqualTest()
        {
            IPEndPoint ep = new IPEndPoint(123456, 22);
            MDCall callReference = new MDCall("grupo1","192.168.2.3", 5000, ep);
            callReference.AddMember("user", "number", ep);
            ep = new IPEndPoint(123456, 22);
            MDCall callIgual = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callIgual.AddMember("user", "number", ep);
            Assert.IsTrue(callReference.Equals(callIgual));

            //Cambios en los parametros de MDCall
            ep = new IPEndPoint(123456, 22);
            MDCall callDif = new MDCall("grupo", "192.168.2.3", 5000, ep);
            callDif.AddMember("user", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));

            ep = new IPEndPoint(123456, 22);
            callDif = new MDCall("grupo1", "192.168.2.2", 5000, ep);
            callDif.AddMember("user", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));
            ep = new IPEndPoint(123456, 22);
            callDif = new MDCall("grupo1", "192.168.2.3", 5001, ep);
            callDif.AddMember("user", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));
            ep = new IPEndPoint(123457, 22);
            callDif = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callDif.AddMember("user", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));
            ep = new IPEndPoint(123456, 23);
            callDif = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callDif.AddMember("user", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));

            //Cambios en los miembros
            ep = new IPEndPoint(123456, 22);
            callDif = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callDif.AddMember("otroUser", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));

            callDif = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callDif.AddMember("user", "otro", ep);
            Assert.IsFalse(callReference.Equals(callDif));

            callDif = new MDCall("grupo1", "192.168.2.3", 5000, ep);
            callDif.AddMember("user", "number", ep);
            callDif.AddMember("otro", "number", ep);
            Assert.IsFalse(callReference.Equals(callDif));

        }

        [TestMethod]
        //Configuración sin cambios
        public void CfgPhoneServiceSinCambio()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            //Primera configuración
            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Un grupo
            Assert.AreEqual(1, phs.MDCalls.Count);
            MDCall grupo = phs.MDCalls[0];
            Assert.AreEqual("<sip:grupotest1@192.168.2.202:7060>", grupo.Uri);
            List<string> miembros = grupo.GroupMembers;
            Assert.AreEqual(1, miembros.Count);
            Assert.AreEqual<string>("<sip:218001@192.168.2.112:5060>", grupo.GroupMembers[0]);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[0].State);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Repite primera configuración (=sectorización sin cambios)
            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Un grupo
            Assert.AreEqual(1, phs.MDCalls.Count);
            grupo = phs.MDCalls[0];
            Assert.AreEqual("<sip:grupotest1@192.168.2.202:7060>", grupo.Uri);
            miembros = grupo.GroupMembers;
            Assert.AreEqual(1, miembros.Count);
            Assert.AreEqual<string>("<sip:218001@192.168.2.112:5060>", grupo.GroupMembers[0]);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[0].State);

            //No han cambiado los estados de MDCalls
            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
        }

        [TestMethod]
        //Cambio de configuracion
        public void CfgPhoneServiceCambio()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            //Primera configuración
            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);
 
            //Un grupo
            Assert.AreEqual(1, phs.MDCalls.Count);
            MDCall grupo = phs.MDCalls[0];
            Assert.AreEqual("<sip:grupotest1@192.168.2.202:7060>", grupo.Uri);
            List<string> miembros = grupo.GroupMembers;
            Assert.AreEqual(1, miembros.Count);
            Assert.AreEqual<string>("<sip:218001@192.168.2.112:5060>", grupo.GroupMembers[0]);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[0].State);

            //Segunda configuración
            cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionGrupoDosMiembros(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Un grupo
            Assert.AreEqual(1, phs.MDCalls.Count);
            grupo = phs.MDCalls[0];
            Assert.AreEqual("<sip:grupotest2@192.168.2.202:7060>", grupo.Uri);
            miembros = grupo.GroupMembers;
            Assert.AreEqual(2, miembros.Count);
            Assert.AreEqual<string>("<sip:218001@192.168.2.112:5060>", grupo.GroupMembers[0]);
            Assert.AreEqual<string>("<sip:218003@192.168.2.112:5060>", grupo.GroupMembers[1]);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[0].State);
        }

        private static void ConfiguracionGrupoUnMiembro(ref Cd40Cfg cfg)
        {
            //Usuario S2
            ConfiguracionUsuario userCfg = new ConfiguracionUsuario();
            //Tecla de grupo con 1 miembro
            CfgEnlaceInterno link = new CfgEnlaceInterno();
            link.Literal = "grupotest1";
            link.PosicionHMI = 45;
            link.TipoEnlaceInterno = "MD";
            link.Prioridad = 4;
            link.OrigenR2 = "218002";
            CfgRecursoEnlaceInterno rec = new CfgRecursoEnlaceInterno();
            rec.Prefijo = 0;
            rec.NombreRecurso = "218001";
            link.ListaRecursos.Add(rec);
            userCfg.TlfLinks.Add(link);

            //Tecla de AD normal
            CfgEnlaceInterno link2 = new CfgEnlaceInterno();
            link2.Literal = "S1_SCV2";
            link2.PosicionHMI = 44;
            link2.TipoEnlaceInterno = "DA";
            link2.Prioridad = 4;
            link2.OrigenR2 = "218002";
            userCfg.TlfLinks.Add(link2);

            cfg.ConfiguracionUsuarios.Add(userCfg);

            //Usuario S1
            ConfiguracionUsuario userCfg2 = new ConfiguracionUsuario();
            //Tecla de grupo con 1 miembro
            CfgEnlaceInterno link3 = new CfgEnlaceInterno();
            link3.Literal = "grupotest1";
            link3.PosicionHMI = 45;
            link3.TipoEnlaceInterno = "MD";
            link3.Prioridad = 4;
            link3.OrigenR2 = "218001";
            CfgRecursoEnlaceInterno rec2 = new CfgRecursoEnlaceInterno();
            rec2.Prefijo = 0;
            rec2.NombreRecurso = "218001";
            link3.ListaRecursos.Add(rec2);
            userCfg2.TlfLinks.Add(link3);

            //Tecla de AD normal
            CfgEnlaceInterno link4 = new CfgEnlaceInterno();
            link4.Literal = "S2_SCV2";
            link4.PosicionHMI = 44;
            link4.TipoEnlaceInterno = "DA";
            link4.Prioridad = 4;
            link4.OrigenR2 = "218001";
            userCfg2.TlfLinks.Add(link4);

            cfg.ConfiguracionUsuarios.Add(userCfg2);

            //Configuracion de sistema, numeracion de usuarios
            DireccionamientoSIP dirSIP = new DireccionamientoSIP();
            StrNumeroAbonado num = new StrNumeroAbonado();
            num.NumeroAbonado = "218001";
            num.Prefijo = 0;
            dirSIP.IdUsuario = "S1_SCV2";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);

            num = new StrNumeroAbonado();
            num.NumeroAbonado = "218002";
            num.Prefijo = 0;
            dirSIP = new DireccionamientoSIP();
            dirSIP.IdUsuario = "S2_SCV2";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);

            num = new StrNumeroAbonado();
            num.NumeroAbonado = "218003";
            num.Prefijo = 0;
            dirSIP = new DireccionamientoSIP();
            dirSIP.IdUsuario = "S2_SCV3";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);
        }

        private static void ConfiguracionGrupoDosMiembros(ref Cd40Cfg cfg)
        {
            //Usuario S2
            ConfiguracionUsuario userCfg = new ConfiguracionUsuario();
            //Tecla de grupo con 2 miembros
            CfgEnlaceInterno link = new CfgEnlaceInterno();
            link.Literal = "grupotest2";
            link.PosicionHMI = 45;
            link.TipoEnlaceInterno = "MD";
            link.Prioridad = 4;
            link.OrigenR2 = "218002";
            CfgRecursoEnlaceInterno rec = new CfgRecursoEnlaceInterno();
            rec.Prefijo = 0;
            rec.NombreRecurso = "218001";
            link.ListaRecursos.Add(rec);
            rec = new CfgRecursoEnlaceInterno();
            rec.NombreRecurso = "218003";
            link.ListaRecursos.Add(rec);
            userCfg.TlfLinks.Add(link);

            //Tecla de AD normal
            CfgEnlaceInterno link2 = new CfgEnlaceInterno();
            link2.Literal = "S1_SCV2";
            link2.PosicionHMI = 44;
            link2.TipoEnlaceInterno = "DA";
            link2.Prioridad = 4;
            link2.OrigenR2 = "218002";
            userCfg.TlfLinks.Add(link2);

            cfg.ConfiguracionUsuarios.Add(userCfg);

            //Usuario S1
            ConfiguracionUsuario userCfg2 = new ConfiguracionUsuario();
            //Tecla de grupo con 2 miembros
            CfgEnlaceInterno link3 = new CfgEnlaceInterno();
            link3.Literal = "grupotest2";
            link3.PosicionHMI = 45;
            link3.TipoEnlaceInterno = "MD";
            link3.Prioridad = 4;
            link3.OrigenR2 = "218001";
            CfgRecursoEnlaceInterno rec2 = new CfgRecursoEnlaceInterno();
            rec2.Prefijo = 0;
            rec2.NombreRecurso = "218001";
            link3.ListaRecursos.Add(rec2);           
            rec2 = new CfgRecursoEnlaceInterno();
            rec2.NombreRecurso = "218003";
            link3.ListaRecursos.Add(rec2);
            userCfg2.TlfLinks.Add(link3);

            //Tecla de AD normal
            CfgEnlaceInterno link4 = new CfgEnlaceInterno();
            link4.Literal = "S2_SCV2";
            link4.PosicionHMI = 44;
            link4.TipoEnlaceInterno = "DA";
            link4.Prioridad = 4;
            link4.OrigenR2 = "218001";
            userCfg2.TlfLinks.Add(link4);

            cfg.ConfiguracionUsuarios.Add(userCfg2);

            //Usuario S3
            ConfiguracionUsuario userCfg3 = new ConfiguracionUsuario();
            //Tecla de grupo con 2 miembros
            CfgEnlaceInterno link5 = new CfgEnlaceInterno();
            link5.Literal = "grupotest2";
            link5.PosicionHMI = 45;
            link5.TipoEnlaceInterno = "MD";
            link5.Prioridad = 4;
            link5.OrigenR2 = "218003";
            CfgRecursoEnlaceInterno rec3 = new CfgRecursoEnlaceInterno();
            rec3.Prefijo = 0;
            rec3.NombreRecurso = "218001";
            link5.ListaRecursos.Add(rec3);
            rec3 = new CfgRecursoEnlaceInterno();            
            rec3.NombreRecurso = "218003";
            link5.ListaRecursos.Add(rec3);
            userCfg3.TlfLinks.Add(link5);

            //Tecla de AD normal
            CfgEnlaceInterno link6 = new CfgEnlaceInterno();
            link6.Literal = "S2_SCV2";
            link6.PosicionHMI = 44;
            link6.TipoEnlaceInterno = "DA";
            link6.Prioridad = 4;
            link6.OrigenR2 = "218003";
            userCfg3.TlfLinks.Add(link6);

            cfg.ConfiguracionUsuarios.Add(userCfg3);

            //Configuracion de sistema, numeracion de usuarios
            DireccionamientoSIP dirSIP = new DireccionamientoSIP();
            StrNumeroAbonado num = new StrNumeroAbonado();
            num.NumeroAbonado = "218001";
            num.Prefijo = 0;
            dirSIP.IdUsuario = "S1_SCV2";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);

            num = new StrNumeroAbonado();
            num.NumeroAbonado = "218002";
            num.Prefijo = 0;
            dirSIP = new DireccionamientoSIP();
            dirSIP.IdUsuario = "S2_SCV2";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);

            num = new StrNumeroAbonado();
            num.NumeroAbonado = "218003";
            num.Prefijo = 0;
            dirSIP = new DireccionamientoSIP();
            dirSIP.IdUsuario = "S3_SCV2";
            dirSIP.NumerosAbonadoQueAtiende.Add(num);
            cfg.ConfiguracionGeneral.PlanDireccionamientoSIP.Add(dirSIP);
        }
        private void ConfiguraciónProxy(ref Cd40Cfg cfg)
        {
            cfg.Version = "9.9";
            cfg.ConfiguracionGeneral = new ConfiguracionSistema();
            DireccionamientoIP i = new DireccionamientoIP();
            i.IdHost = "SCV2_LABO";
            i.IpRed1 = "192.168.2.112";
            i.TipoHost = Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA;
            i.Interno = true;
            i.Max = 218000;
            i.Min = 218008;
            i.EsCentralIP = true;

            cfg.ConfiguracionGeneral.PlanDireccionamientoIP.Add(i);
         }

        [TestMethod]
        //Establece un llamada MD a un grupo de uno que contesta.
        //Cuelga el llamante
        public void MDEstablish()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId= "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de saliente
            CORESIP_CallStateInfo stInfo= new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada saliente
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);

            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED-1].CallId, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de uno que contesta.
        //Cuelga el llamado
        public void MDEstablish2()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de saliente
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada saliente
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);

            //Cuelga llamado
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED].CallId, info, stInfo);

            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLER].CallId, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de dos miembros y sólo contesta uno
        //Cuelga el llamante
        public void MDEstablish3()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int CALLED2 = 2;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoDosMiembros(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest2";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de salientes
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED2].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Establecimiento llamada saliente, descuelga CALLED
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //espera tiempo y se cancela la llamada no atendida
            System.Threading.Thread.Sleep(500);
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED2].CallId, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);

            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED - 1].CallId, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de dos miembros y contestan dos
        //Cuelga el llamante
        public void MDEstablish4()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int CALLED2 = 2;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoDosMiembros(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest2";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de salientes
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED2].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Establecimiento llamada saliente, descuelga CALLED
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Establecimiento segunda llamada saliente, descuelga CALLED2
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED2].CallId, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);

            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);

            //Cuelgan llamados
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[0].CallId, info, stInfo);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[0].CallId, info, stInfo);

            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de dos miembros y sólo contesta uno
        //entra mas tarde el segundo miembro del grupo
        //Cuelga el llamante
        public void MDEstablish5()
        {
            const int CALLID = 11;
            const int CALLID2 = 12;
            const int CALLER = 0;
            const int CALLED = 1;
            const int CALLED2 = 2;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoDosMiembros(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest2";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de salientes
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED2].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Establecimiento llamada saliente, descuelga CALLED
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //espera tiempo y se cancela la llamada no atendida
            System.Threading.Thread.Sleep(500);
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED2].CallId, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);

            //Entra el segundo miembro del grupo
            info = new CORESIP_CallInfo();
            info.AccountId = 33;
            inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S3_SCV2";
            inInfo.DstId = "grupotest2";
            inInfo.SrcId = "218003";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID2, 0, info, inInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);
            //Establecimiento de llamada
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID2, info, stInfo);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);

            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);

            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[0].CallId, info, stInfo);
            phs.MDCalls[GRUPO1].OnCallState(CALLID2, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de uno que contesta.
        //Entra otro llamante a la MD
        //Cuelga el primer llamante y se mantiene la llamada entre los otros dos
        //Cuelga el llamado
        public void MDEstablish6()
        {
            const int CALLID = 11;
            const int CALLID2 = 12;
            const int CALLER = 0;
            const int CALLED = 1;
            const int CALLED2 = 2;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de saliente
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada saliente
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[0].LiveCall[CALLED].CallId, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);

            //Entra otro llamante a la MD
            inInfo.DisplayName = "S4_SCV4";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId = "218004";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID2, 0, info, inInfo);
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID2, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLED2].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Cuelga el primer llamante y se mantiene la llamda entre los otros dos
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);

            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.InUse, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED-1].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLED2 - 1].State);

            //Cuelga el segundo llamante
            phs.MDCalls[GRUPO1].OnCallState(CALLID2, info, stInfo);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[0].CallId, info, stInfo);

            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }

        [TestMethod]
        //Establece un llamada MD a un grupo de uno. 
        //Antes de que conteste alguien se aborta.
        public void MDAbort()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoUnMiembro(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest1";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de saliente
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);

            //Confirmación de la cancelacion del llamado
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED - 1].CallId, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
        }
        [TestMethod]
        //Establece un llamada MD a un grupo de dos miembros.
        //Antes de que conteste alguien se aborta.
        public void MDAbort2()
        {
            const int CALLID = 11;
            const int CALLER = 0;
            const int CALLED = 1;
            const int CALLED2 = 2;
            const int GRUPO1 = 0;

            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);

            ConfiguracionGrupoDosMiembros(ref cfg);
            phs.ServiceReleaseConfig();
            phs.ServiceSetupConfig(cfg);

            //Llamada entrante
            CORESIP_CallInfo info = new CORESIP_CallInfo();
            info.AccountId = 33;
            CORESIP_CallInInfo inInfo = new CORESIP_CallInInfo();
            inInfo.DisplayName = "S2_SCV2";
            inInfo.DstId = "grupotest2";
            inInfo.SrcId = "218002";
            phs.MDCalls[GRUPO1].OnCallIncoming(CALLID, 0, info, inInfo);

            Assert.AreEqual(1, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_CALLER, phs.MDCalls[GRUPO1].LiveCall[CALLER].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InComing, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);

            //Establecimiento llamada entrante e inicio de salientes
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(3, phs.MDCalls[0].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Ringing, phs.MDCalls[GRUPO1].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallRole.MD_MEMBER, phs.MDCalls[GRUPO1].LiveCall[CALLED2].Role);
            Assert.AreEqual(MDCall.CallInfo.CallState.InConversation, phs.MDCalls[GRUPO1].LiveCall[CALLER].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED].State);
            Assert.AreEqual(MDCall.CallInfo.CallState.OutGoing, phs.MDCalls[GRUPO1].LiveCall[CALLED2].State);

            //Cuelga llamante
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            phs.MDCalls[GRUPO1].OnCallState(CALLID, info, stInfo);
            Assert.AreEqual(2, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);

            //Confirmación de la cancelacion del llamado
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED - 1].CallId, info, stInfo);
            Assert.AreEqual(1, phs.MDCalls[GRUPO1].LiveCall.Count);
            phs.MDCalls[GRUPO1].OnCallState(phs.MDCalls[GRUPO1].LiveCall[CALLED - 1].CallId, info, stInfo);
            Assert.AreEqual(0, phs.MDCalls[GRUPO1].LiveCall.Count);
            Assert.AreEqual(MDCall.MDCallState.Idle, phs.MDCalls[GRUPO1].State);           
        }
    }
}

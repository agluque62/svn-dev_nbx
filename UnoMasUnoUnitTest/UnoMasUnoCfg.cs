using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using U5ki.Infrastructure;
using U5ki.RdService;

namespace UnoMasUnoUnitTest
{
    [TestClass]
    public class UnoMasUnoCfg
    {
        //Esta clase sustituye a nivel estructural a NbxService
        private TestContext testContextInstance;
        private static RdService rdServ = new RdService();
        #region Atributos de prueba adicionales
        //
        // Puede usar los siguientes atributos adicionales conforme escribe las pruebas:
        //
        // Use ClassInitialize para ejecutar el código antes de ejecutar la primera prueba en la clase
        [ClassInitialize()]
        public static void MyTestInit(TestContext testContext)
        {
            rdServ.Start();
            SipAgent.Init(
                    "U5KI",
                    "192.168.2.202", //OJO, poner la direccion propia
                    7060, 128);
            SipAgent.Start();
        }

        // Use ClassCleanup para ejecutar el código una vez ejecutadas todas las pruebas en una clase
        // [ClassCleanup()]
        public static void MyClassCleanup()
        {
            rdServ.Stop();
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
        public void CfgRdSimple()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2,frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund",iRes.ID);
            Assert.AreNotEqual(iRes.SipCallId, -1);

        }

        [TestMethod]
        public void CfgRdIgual()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);

            //Sectorizacion sin cambios
            cfg.Version = "9.2";
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out  frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out  iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);

        }
        [TestMethod]
        public void CfgCambioSimpleADoble()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            //Config Simple y doble
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

            cfg.Version = "9.1";
            //Config doble y doble
            cfg.ConfiguracionUsuarios.Clear();
            ConfiguracionFreqDoble(ref cfg);
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsFalse(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out iRes));
            Assert.IsNull(iRes);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX1_UT@192.168.3.18:5060>0", out iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redunRx", iRes.ID);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

        }
        [TestMethod]
        public void CfgCambioSimpleADobleSinCambioNombre()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            //Config Simple y doble
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

            cfg.Version = "9.1";
            //Config doble y doble
            cfg.ConfiguracionUsuarios.Clear();
            ConfiguracionFreqDoble_2(ref cfg);
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redunRx", iRes.ID);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

        }
        [TestMethod]
        public void CfgCambioDobleASimple()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            //Config doble y doble
            ConfiguracionFreqDoble(ref cfg);

            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX1_UT@192.168.3.18:5060>0", out IRdResource iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redunRx", iRes.ID);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

            //Config Simple y doble
            cfg.Version = "9.1";            
            cfg.ConfiguracionUsuarios.Clear();
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsFalse(frec.RdRs.TryGetValue("<SIP:RX1_UT@192.168.3.18:5060>0", out iRes));
            Assert.IsNull(iRes);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

        }

        [TestMethod]
        public void CfgCambioCGW()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);

            //Reconfiguro: Cambio el recurso tx2 reserva 1+1, de pasarela
            AsignacionRecursosGW equipo = cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Find (x => x.IdRecurso.Equals( "tx2_ut"));
            equipo.IdHost = "CGW2";
            cfg.Version = "9.1";
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            //Assert.AreNotEqual(iRes.SipCallId, -1);


            //Cambio el recurso tx principal 1+1, de pasarela
            equipo = cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Find(x => x.IdRecurso.Equals("tx1_ut"));
            equipo.IdHost = "CGW2";
            cfg.Version = "9.2";
            rdServ.ProcessNewConfig(cfg);
            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out iRes));
            //Assert.AreNotEqual(iRes.SipCallId, -1);

            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.20:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            //Assert.AreNotEqual(iRes.SipCallId, -1);

        }

        [TestMethod]
        public void CaidaSIP()
        {
            Cd40Cfg cfg = new Cd40Cfg();
            ConfiguraciónProxy(ref cfg);
            ConfiguracionEquipos(ref cfg);
            ConfiguracionUsuarios(ref cfg);
            ConfiguracionFreqUnoMasUnoRxSimple(ref cfg);
            rdServ.Master = true;
            rdServ.ProcessNewConfig(cfg);

            Assert.IsTrue(rdServ.Frecuencies.TryGetValue("111.111", out RdFrecuency frec));
            Assert.AreEqual(2, frec.RdRs.Count);
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:RX_UT@192.168.3.18:5060>0", out IRdResource iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResource));
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            CORESIP_CallStateInfo stInfo = new CORESIP_CallStateInfo();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            frec.HandleChangeInCallState(iRes.SipCallId, stInfo, out IRdResource rdResOut);

            //Se conectan los dos recursos de la pareja 1+1
            Assert.IsTrue(frec.RdRs.TryGetValue("<SIP:TX1_UT@192.168.3.18:5060>1", out iRes));
            Assert.IsInstanceOfType(iRes, typeof(RdResourcePair));
            Assert.AreEqual("redund", iRes.ID);
            //Assert.AreNotEqual(iRes.SipCallId, -1);
            RdResource res1 = ((RdResourcePair)iRes).ActiveResource;
            RdResource res2 = ((RdResourcePair)iRes).StandbyResource;

            foreach (RdResource res in iRes.GetListResources())
            {
                frec.HandleChangeInCallState(res.SipCallId, stInfo, out rdResOut);
            }
            Assert.IsFalse(res1.TxMute);
            Assert.IsTrue(res2.TxMute);

            // Se cae el 2 standby -> no hay switch
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            frec.HandleChangeInCallState(res2.SipCallId, stInfo, out rdResOut);
            Assert.IsFalse(res1.TxMute);
            Assert.IsTrue(res2.TxMute);

            //Se conecta el 2 standby -> no hay switch
            frec.RetryFailedConnections();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            frec.HandleChangeInCallState(res2.SipCallId, stInfo, out rdResOut);
            Assert.IsFalse(res1.TxMute);
            Assert.IsTrue(res2.TxMute);

            // Se cae el 1 activo ->  hay switch al 2
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            frec.HandleChangeInCallState(res1.SipCallId, stInfo, out rdResOut);
            Assert.IsFalse(res2.TxMute);
            Assert.IsTrue(res1.TxMute);

            //Se conecta el 1 standby -> no hay switch
            frec.RetryFailedConnections();
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
            frec.HandleChangeInCallState(res1.SipCallId, stInfo, out rdResOut);
            Assert.IsFalse(res2.TxMute);
            Assert.IsTrue(res1.TxMute);

            // Se cae el 2 activo -> hay switch al 1
            stInfo.State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            frec.HandleChangeInCallState(res2.SipCallId, stInfo, out rdResOut);
            Assert.IsFalse(res1.TxMute);
            Assert.IsTrue(res2.TxMute);


        }

        private void ConfiguraciónProxy(ref Cd40Cfg cfg)
        {
            cfg.Version = "9.0";
            cfg.ConfiguracionGeneral = new ConfiguracionSistema();
            DireccionamientoIP i = new DireccionamientoIP();
            i.IdHost = "SCV2_LABO";
            i.IpRed1 = "192.168.3.112";
            i.TipoHost = Tipo_Elemento_HW.TEH_EXTERNO_TELEFONIA;
            i.Interno = true;
            i.Max = 218000;
            i.Min = 218008;
            i.EsCentralIP = true;

            cfg.ConfiguracionGeneral.PlanDireccionamientoIP.Add(i);
        }

        private void ConfiguracionUsuarios(ref Cd40Cfg cfg)
        {
            AsignacionUsuariosTV i = new AsignacionUsuariosTV();
            i.IdHost = "PICT02";
            i.IdUsuario = "S2";
            i.IpGrabador1 = "";
            i.IpGrabador2 = "";
            i.RtspPort = 0;
            cfg.ConfiguracionGeneral.PlanAsignacionUsuarios.Add(i);
        }
        private void ConfiguracionEquipos(ref Cd40Cfg cfg)
        {
            DireccionamientoIP i = new DireccionamientoIP();
            //Pasarela 1
            i.IdHost = "CGW1";
            i.IpRed1 = "192.168.3.18";
            i.IpRed2 = "192.168.3.18";
            i.TipoHost = Tipo_Elemento_HW.TEH_TIFX;
            i.Interno = true;
            i.Max = 228000;
            i.Min = 228008;
            i.EsCentralIP = true;
            cfg.ConfiguracionGeneral.PlanDireccionamientoIP.Add(i);

            //Pasarela 2
            i = new DireccionamientoIP();
            i.IdHost = "CGW2";
            i.IpRed1 = "192.168.3.20";
            i.IpRed2 = "192.168.3.20";
            i.TipoHost = Tipo_Elemento_HW.TEH_TIFX;
            i.Interno = true;
            i.Max = 228000;
            i.Min = 228008;
            i.EsCentralIP = true;

            cfg.ConfiguracionGeneral.PlanDireccionamientoIP.Add(i);
        }


        private static void ConfiguracionFreqUnoMasUnoRxSimple(ref Cd40Cfg cfg)
        {
            //Usuario S2
            ConfiguracionUsuario userCfg = new ConfiguracionUsuario();
            userCfg.User = new CfgUsuario();
            userCfg.User.IdIdentificador = "S2";

            //Tecla 1 frecuencia 1+1
            CfgEnlaceExterno freq = new CfgEnlaceExterno();
            freq.Alias = "";
            freq.AudioPrimerSqBss = true;
            freq.CldSupervisionTime = 10;
            freq.EmplazamientoDefecto = "emplaz";
            freq.Literal = "111.111";
            freq.MetodoCalculoClimax = 0;
            freq.MetodosBssOfrecidos = 0;
            freq.ModoTransmision = Tipo_ModoTransmision.Ninguno;
            freq.PorcentajeRSSI = 0;
            freq.PrioridadSesionSip = 0;
            freq.SincronizaGrupoClimax = true;
            freq.SupervisionPortadora = true;
            freq.TiempoVueltaADefecto = 0;
            freq.TipoFrecuencia = Tipo_Frecuencia.Basica;
            freq.VentanaReposoZonaTxDefecto = 0;
            freq.VentanaSeleccionBss = 100;
            
            CfgRecursoEnlaceExterno rec = new CfgRecursoEnlaceExterno();
            //Principal TX
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.IdRecurso = "tx1_ut";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.RedundanciaIdPareja = "redund";
            rec.RedundanciaRol = "P";
            rec.Tipo = 1;
            freq.ListaRecursos.Add(rec);

            AsignacionRecursosGW equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //Reserva TX
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "tx2_ut";
            rec.RedundanciaRol = "R";
            rec.RedundanciaIdPareja = "redund";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.Tipo = 1;

            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //RX simple
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "rx_ut";
            rec.Tipo = 0;
            rec.RedundanciaIdPareja = "";
            rec.RedundanciaRol = "";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            userCfg.RdLinks.Add(freq);
            cfg.ConfiguracionUsuarios.Add(userCfg);
        }
        //Tiene un ID del Rx diferente de la configuracion simple
        private static void ConfiguracionFreqDoble(ref Cd40Cfg cfg)
        {
            //Usuario S2
            ConfiguracionUsuario userCfg = new ConfiguracionUsuario();
            userCfg.User = new CfgUsuario();
            userCfg.User.IdIdentificador = "S2";

            //Tecla 1 frecuencia 1+1
            CfgEnlaceExterno freq = new CfgEnlaceExterno();
            freq.Alias = "";
            freq.AudioPrimerSqBss = true;
            freq.CldSupervisionTime = 10;
            freq.EmplazamientoDefecto = "emplaz";
            freq.Literal = "111.111";
            freq.MetodoCalculoClimax = 0;
            freq.MetodosBssOfrecidos = 0;
            freq.ModoTransmision = Tipo_ModoTransmision.Ninguno;
            freq.PorcentajeRSSI = 0;
            freq.PrioridadSesionSip = 0;
            freq.SincronizaGrupoClimax = true;
            freq.SupervisionPortadora = true;
            freq.TiempoVueltaADefecto = 0;
            freq.TipoFrecuencia = Tipo_Frecuencia.Basica;
            freq.VentanaReposoZonaTxDefecto = 0;
            freq.VentanaSeleccionBss = 100;

            CfgRecursoEnlaceExterno rec = new CfgRecursoEnlaceExterno();
            //Principal TX
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.IdRecurso = "tx1_ut";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.RedundanciaIdPareja = "redund";
            rec.RedundanciaRol = "P";
            rec.Tipo = 1;
            freq.ListaRecursos.Add(rec);

            AsignacionRecursosGW equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //Reserva TX
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "tx2_ut";
            rec.RedundanciaRol = "R";
            rec.RedundanciaIdPareja = "redund";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.Tipo = 1;

            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //RX Principal
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "rx1_ut";
            rec.Tipo = 0;
            rec.RedundanciaIdPareja = "redunRx";
            rec.RedundanciaRol = "P";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            userCfg.RdLinks.Add(freq);

            //RX Reserva
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "rx2_ut";
            rec.Tipo = 0;
            rec.RedundanciaIdPareja = "redunRx";
            rec.RedundanciaRol = "R";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            userCfg.RdLinks.Add(freq);

            cfg.ConfiguracionUsuarios.Add(userCfg);
        }
        //Mantiene el ID del Rx principal igual al de la configuracion simple
        private static void ConfiguracionFreqDoble_2(ref Cd40Cfg cfg)
        {
            //Usuario S2
            ConfiguracionUsuario userCfg = new ConfiguracionUsuario();
            userCfg.User = new CfgUsuario();
            userCfg.User.IdIdentificador = "S2";

            //Tecla 1 frecuencia 1+1
            CfgEnlaceExterno freq = new CfgEnlaceExterno();
            freq.Alias = "";
            freq.AudioPrimerSqBss = true;
            freq.CldSupervisionTime = 10;
            freq.EmplazamientoDefecto = "emplaz";
            freq.Literal = "111.111";
            freq.MetodoCalculoClimax = 0;
            freq.MetodosBssOfrecidos = 0;
            freq.ModoTransmision = Tipo_ModoTransmision.Ninguno;
            freq.PorcentajeRSSI = 0;
            freq.PrioridadSesionSip = 0;
            freq.SincronizaGrupoClimax = true;
            freq.SupervisionPortadora = true;
            freq.TiempoVueltaADefecto = 0;
            freq.TipoFrecuencia = Tipo_Frecuencia.Basica;
            freq.VentanaReposoZonaTxDefecto = 0;
            freq.VentanaSeleccionBss = 100;

            CfgRecursoEnlaceExterno rec = new CfgRecursoEnlaceExterno();
            //Principal TX
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.IdRecurso = "tx1_ut";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.RedundanciaIdPareja = "redund";
            rec.RedundanciaRol = "P";
            rec.Tipo = 1;
            freq.ListaRecursos.Add(rec);

            AsignacionRecursosGW equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //Reserva TX
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "tx2_ut";
            rec.RedundanciaRol = "R";
            rec.RedundanciaIdPareja = "redund";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            rec.Tipo = 1;

            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            //RX Principal
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "rx_ut";
            rec.Tipo = 0;
            rec.RedundanciaIdPareja = "redunRx";
            rec.RedundanciaRol = "P";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            userCfg.RdLinks.Add(freq);

            //RX Reserva
            rec = new CfgRecursoEnlaceExterno();
            rec.IdRecurso = "rx2_ut";
            rec.Tipo = 0;
            rec.RedundanciaIdPareja = "redunRx";
            rec.RedundanciaRol = "R";
            rec.EnableEventPttSq = true;
            rec.Estado = "S";
            rec.GrsDelay = 10;
            rec.IdEmplazamiento = "emplaz";
            rec.IdMetodoBss = "NUCLEO";
            rec.MetodoBss = 1;
            rec.ModoConfPTT = 0;
            rec.NombreZona = "zona1";
            rec.NumFlujosAudio = 1;
            rec.OffSetFrequency = 0;
            freq.ListaRecursos.Add(rec);

            equipo = new AsignacionRecursosGW();
            equipo.IdHost = "CGW1";
            equipo.IdRecurso = rec.IdRecurso;
            equipo.SipPort = 5060;
            cfg.ConfiguracionGeneral.PlanAsignacionRecursos.Add(equipo);

            userCfg.RdLinks.Add(freq);

            cfg.ConfiguracionUsuarios.Add(userCfg);
        }
    }
}

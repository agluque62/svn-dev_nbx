using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.Diagnostics;

using U5ki.PresenceService;
using U5ki.PresenceService.Interfaces;
using U5ki.PresenceService.Engines;
using U5ki.PresenceService.Agentes;

using U5ki.Infrastructure;
using U5ki.TifxService;
using Utilities;

namespace PresenceServiceUnitTest
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class PresenceServerResourceUnitTest
    {
        [TestMethod]
        public void GetFrameTestMethod()
        {
            PresenceServerResource rs = new PresenceServerResource();
            byte[] frame = rs.Frame;
            Assert.AreEqual(60, frame.Length, "Longitud de Trama Incorrecta...");
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class PresenceServerAgentUnitTest
    {
        [TestMethod]
        public void AgentGetFrameTestMethod()
        {
            /** Genero una trama TIFX */
            PSBaseAgent agent = new PSBkkAgent();
            byte[] frame = agent.Frame;
                                
            ///** La decodifico segun lo hace el servicio... */
            using (MemoryStream ms = new MemoryStream(frame))
            using (CustomBinaryFormatter bf = new CustomBinaryFormatter())
            {
                GwInfo gwInfo = bf.Deserialize<GwInfo>(ms);
                Assert.AreEqual(gwInfo.Type, (uint)AgentType.ForInternalSub, "Decodificacion de Trama incorrecta ");
            }
        }
    }

    [TestClass]
    public class PresenceServerServiceTest
    {
        public static Object DataOfService
        {
            get
            {
                return null;
            }
        }
        protected void SipAgentStart()
        {
            /** 20180208. Inicializa el SipAgent para que pueda se utilizado por diferentes servicios */
            try
            {
                uint sipPort = 6060;
                SipAgent.Init(
                    "TESTING",
                    "192.168.0.129",
                    sipPort, 128);
                SipAgent.Start();
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.Message);
            }
        }
        protected void SipAgentStop()
        {
            try
            {
                /** 20180208. Inicializa el SipAgent para que pueda se utilizado por diferentes servicios */
                Debug.WriteLine("Deteniendo SipAgent.");
                SipAgent.End();
            }
            catch (Exception x)
            {
                Debug.WriteLine("SipAgentStop Exception", x);
            }
            finally
            {
            }
        }

        [TestMethod]
        public void PresenceServerServiceStartStopTest()
        {
            try
            {
                U5kPresService service = new U5kPresService();
                service.Start();

                Assert.IsTrue(service.Status == U5ki.Infrastructure.ServiceStatus.Running, "Error en Rutina de Arranque");

                service.Stop();

                Assert.IsTrue(service.Status == U5ki.Infrastructure.ServiceStatus.Stopped, "Error en Rutina de Parada");
            }
            catch (AssertFailedException x)
            {
                MessageBox.Show(x.Message);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void PresenceServerServiceMasterSlaveTest()
        {
            DialogResult res;
            string Caption = "PresenceServerResourceUnitTest";
            bool MasterSolicitado = false;
            try
            {
                U5kPresService service = new U5kPresService();
                InterProcessEvent ipc = new InterProcessEvent("tifx_master");

                SipAgentStart();
                service.Start();
                Assert.IsTrue(service.Status == U5ki.Infrastructure.ServiceStatus.Running, "Error en Rutina de Arranque");

                do
                {
                    res = MessageBox.Show("Servicio Arrancado.\n\n Modo Solicitado: " + (MasterSolicitado ? "MASTER " : "SLAVE ") + "\n\n" +
                        "Pulse Si para MASTER.\n" +
                        "Pulse No para SLAVE\n" , Caption, MessageBoxButtons.YesNoCancel);
                    switch (res)
                    {
                        case DialogResult.Yes:
                            ipc.Raise<bool>(true);
                            MasterSolicitado = true;
                            break;
                        case DialogResult.No:
                            ipc.Raise<bool>(false);
                            MasterSolicitado = false;
                            break;
                    }

                    /** */
                    List<object> DataOfService = new List<object>();
                    bool result = service.DataGet(U5ki.Infrastructure.ServiceCommands.SrvDbg, ref DataOfService);
                    if (result == true && DataOfService.Count == 1)
                    {
                        File.WriteAllText("StdOfService.json", (string)DataOfService[0]);
                    }

                }
                while (res != DialogResult.Cancel);

                service.Stop();
                SipAgentStop();

                Assert.IsTrue(service.Status == U5ki.Infrastructure.ServiceStatus.Stopped, "Error en Rutina de Parada");
            }
            catch (AssertFailedException x)
            {
                MessageBox.Show(x.Message);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestForNegativeTimespam()
        {
            var future = DateTime.Now + TimeSpan.FromMinutes(10);
            var elapsed = DateTime.Now - future;
            if (elapsed <= TimeSpan.Zero)
                Console.WriteLine($"{elapsed}");
        }

        class SendingControlClass
        {
            static Dictionary<string, bool> SendingControlList = new Dictionary<string, bool>()
            {
                {typeof(PSBkkAgent).Name,false},
                {typeof(PSExternalAgent).Name, false},
                {typeof(PSProxiesAgent).Name, false},            
                /** EXRES Incluir el control de envio del agente de recursos externos de telefonía. */            
                {typeof(PSExternalResourcesAgent).Name, false},
            };
            static public bool DoSend<T>()
            {
                var key = typeof(T).Name;
                if (SendingControlList.ContainsKey(key))
                    return SendingControlList[key];
                return false;
            }
            static public void SendSet<T>(bool val)
            {
                var key = typeof(T).Name;
                if (SendingControlList.ContainsKey(key))
                    SendingControlList[key] = val;
            }

        }

        [TestMethod]
        public void TestingSendingControlClass()
        {
            var res = SendingControlClass.DoSend<PSProxiesAgent>();
        }


    }

}

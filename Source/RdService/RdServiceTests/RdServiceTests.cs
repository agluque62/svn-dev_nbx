using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using U5ki.RdService;
using U5ki.Infrastructure;
using Utilities;

namespace RadioServiceTest
{
    [TestClass]
    public class RdServiceTests
    {
        RdService service = null;
        void PrepareTest()
        {
            ServicesHelpers.IgnoreSpreadChannel = true;

            try
            {
                Debug.WriteLine($"{DateTime.Now}: Preparando Agente Sip...");
                uint sipPort = 8060;
                SipAgent.Init(
                    "TESTING",
                    "192.168.90.48",
                    sipPort, 128);
                SipAgent.Start();

                Debug.WriteLine($"{DateTime.Now}: Arrancado Servicio Radio...");
                service = new RdService();
                service.Cfg = JsonConvert.DeserializeObject<Cd40Cfg>(File.ReadAllText("u5ki.LastCfg.json"));
                service.Start();
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.Message);
            }

        }

        void DisposeTest()
        {
            try
            {
                Debug.WriteLine($"{DateTime.Now}: Deteniendo Servicio Radio.");
                service.Stop();
                service = null;

                Debug.WriteLine($"{DateTime.Now}: Deteniendo Agente Sip");
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
        public void TestMethod1()
        {
            Assert.IsTrue(ServicesHelpers.UnitTestMode);
            
            PrepareTest();


            Task.Delay(TimeSpan.FromSeconds(20)).Wait();

            List<object> sessions = new List<object>();
            if (service?.DataGet(ServiceCommands.RdSessions, ref sessions) == true)
            {
            }

            if (service?.DataGet(ServiceCommands.RdUnoMasUnoData, ref sessions) == true)
            {

            }

            Task.Delay(TimeSpan.FromSeconds(20)).Wait();
            DisposeTest();
        }
    }
}

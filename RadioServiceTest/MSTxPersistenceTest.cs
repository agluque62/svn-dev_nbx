using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

using U5ki.RdService;
using U5ki.Infrastructure;
using Utilities;


namespace RadioServiceTest
{
    [TestClass]
    public class MSTxPersistenceTest
    {
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
        public void TestMethod1()
        {
            var main = new RdResource("pepemain");
            var stby = new RdResource("pepestby");

            SipAgentStart();
            var service = new RdService();
            service.Start();

            Task.Delay(2000).Wait();
            MSTxPersistence.SelectMain(main, stby);

            Task.Delay(2000).Wait();
            service.Stop();
            SipAgentStop();
        }
    }
}

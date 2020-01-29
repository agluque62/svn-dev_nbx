using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

using U5ki.Infrastructure;
using U5ki.PresenceService.Simulation;

namespace PresenceServiceUnitTest
{
    class TestSync : IDisposable
    {
        protected ManualResetEvent mre = new ManualResetEvent(false);
        protected Action<string, int, int> Checking;

        public bool Prepare(Action prepare, Action<string, int, int> checking, int time)
        {
            prepare();
            Checking = checking;
            return mre.WaitOne(time);
        }

        public void Set(string p1="", int p2=0, int p3=0)
        {
            Checking(p1, p2, p3);
            mre.Set();
        }

        public void Dispose() { }

        public string ErrorMsg { get; set; }
    }
    class OptionsReceiveSync : TestSync
    {
        public string Callid { get; set; }
        public int Code { get; set; }
    }
    class PresenceResponseSync : TestSync
    {
        public string From { get; set; }
        public int Subscribed { get; set; }
        public int Present { get; set; }
    }

    [TestClass]
    public class SimulatedProxiesScenarioTest
    {
        OptionsReceiveSync options = null;
        void OptionsReceive(string from, string callid, int code, string support, string allowed)
        {
            if (options != null) options.Set(callid, code);
        }
        PresenceResponseSync presponse = null;
        void PresenceResponse(string uri, int subscription_status, int presence_status)
        {
            if (presponse != null) presponse.Set(uri, subscription_status, presence_status);
        }
        void PrepareTest()
        {
            SimulatedProxiesScenario.Init(OptionsReceive, PresenceResponse);
            Task.Delay(100).Wait();
            
        }
        void DisposeTest()
        {
            SimulatedProxiesScenario.End();
        }
        void ManualSync(string msg)
        {
            MessageBox.Show(msg + "\nPulse tecla para continuar...");
        }

        [TestMethod]
        public void SPSTPrueba1()
        {
            bool result = false;

            PrepareTest();

            options = new OptionsReceiveSync();
            Assert.AreEqual(
                options.Prepare(() =>
                {
                    options.Code = 200;
                    options.Callid = SimulatedProxiesScenario.SendOptionsMsg("<sip:10.12.60.129:6060>");
                }, 
                (callid, code, none) =>
                {
                    if (code != options.Code)
                        options.ErrorMsg = "Codigo de retorno erroneo: " + code.ToString();
                    else if (callid != options.Callid)
                        options.ErrorMsg = "Callid de retorno erroneo: " + callid;
                    else
                        result = true;
                },
                1000), true);

            ManualSync("Options enviado");

            DisposeTest();
            Assert.IsTrue(result, options.ErrorMsg);
        }

        [TestMethod]
        public void SPSTPrueba2()
        {
            PrepareTest();

            presponse = new PresenceResponseSync();
            presponse.Prepare(
                () =>
                {
                    presponse.From = "<sip:318001@10.12.60.129:6060>";
                    presponse.Subscribed = 1;
                    presponse.Present = 1;
                    SimulatedProxiesScenario.CreatePresenceSubscription(presponse.From);
                },
                (from, ss, ps) =>
                {
                    Assert.AreEqual(from, presponse.From);
                    Assert.AreEqual(ss, presponse.Subscribed);
                    Assert.AreEqual(ps, presponse.Present);
                }, 1000);
            ManualSync("Subscripción enviada");


            DisposeTest();
        }

        [TestMethod]
        public void SPSTPrueba3()
        {
            PrepareTest();

            presponse = new PresenceResponseSync();
            presponse.Prepare(
                () =>
                {
                    presponse.From = "<sip:318004@10.12.60.129:6060>";
                    presponse.Subscribed = 1;
                    presponse.Present = 0;
                    SimulatedProxiesScenario.CreatePresenceSubscription(presponse.From);
                },
                (from, ss, ps) =>
                {
                    Assert.AreEqual(from, presponse.From);
                    Assert.AreEqual(ss, presponse.Subscribed);
                    Assert.AreEqual(ps, presponse.Present);
                }, 1000);
            ManualSync("Subscripción enviada");


            DisposeTest();
        }
    }
}

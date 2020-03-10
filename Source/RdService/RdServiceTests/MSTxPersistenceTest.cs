using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

using U5ki.RdService;
using U5ki.Infrastructure;
using Utilities;
using System.Collections.Generic;

namespace RadioServiceTest
{
    class DummyRdResource : IRdResource
    {
        public RdRsType Type => throw new NotImplementedException();
        public bool isRx => throw new NotImplementedException();
        public bool isTx => throw new NotImplementedException();
        public RdRsPttType Ptt => throw new NotImplementedException();
        public ushort PttId => throw new NotImplementedException();
        public bool Squelch => throw new NotImplementedException();
        public bool TxMute { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ID { get; set; }
        public string Uri1 => throw new NotImplementedException();
        public string Uri2 => throw new NotImplementedException();
        public bool ToCheck => throw new NotImplementedException();
        public string Site { get ; set ; }
        public bool SelectedSite { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SipCallId => throw new NotImplementedException();
        public bool MasterMN { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ReplacedMN { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsForbidden { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Connected => throw new NotImplementedException();
        public bool OldSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ActivateResource(string IdResource)
        {
            throw new NotImplementedException();
        }
        public bool Connect()
        {
            throw new NotImplementedException();
        }
        public List<RdResource> GetListResources()
        {
            throw new NotImplementedException();
        }
        public RdResource GetRxSelected()
        {
            throw new NotImplementedException();
        }
        public RdResource GetSimpleResource(int sipCallId)
        {
            throw new NotImplementedException();
        }
        public bool HandleChangeInCallState(int sipCallId, CORESIP_CallStateInfo stateInfo)
        {
            throw new NotImplementedException();
        }
        public void PttOff()
        {
            throw new NotImplementedException();
        }
        public void PttOn(CORESIP_PttType srcPtt)
        {
            throw new NotImplementedException();
        }
        #region IDisposable Support
        private bool disposedValue = false; // Para detectar llamadas redundantes
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: elimine el estado administrado (objetos administrados).
                }
                // TODO: libere los recursos no administrados (objetos no administrados) y reemplace el siguiente finalizador.
                // TODO: configure los campos grandes en nulos.
                disposedValue = true;
            }
        }

        // TODO: reemplace un finalizador solo si el anterior Dispose(bool disposing) tiene código para liberar los recursos no administrados.
        // ~DummyRdResource()
        // {
        //   // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
        //   Dispose(false);
        // }
        // Este código se agrega para implementar correctamente el patrón descartable.
        public void Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
            Dispose(true);
            // TODO: quite la marca de comentario de la siguiente línea si el finalizador se ha reemplazado antes.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public DummyRdResource() { }
    }

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
            var main = new DummyRdResource() { ID = "pepemain", Site = "SITE1" };
            var stby = new DummyRdResource() { ID = "pepestby", Site = "SITE1" };

            SipAgentStart();
            var service = new RdService();
            service.Start();

            Task.Delay(20000).Wait();
            MSTxPersistence.SelectMain(main, stby);
            Task.Delay(10000).Wait();
            Debug.WriteLine($"main is Main {MSTxPersistence.IsMain(main)}, stby is Main {MSTxPersistence.IsMain(stby)}");
            
            MSTxPersistence.SelectMain(stby, main);
            Task.Delay(10000).Wait();
            Debug.WriteLine($"main is Main {MSTxPersistence.IsMain(main)}, stby is Main {MSTxPersistence.IsMain(stby)}");

            service.Stop();
            SipAgentStop();
        }
    }
}

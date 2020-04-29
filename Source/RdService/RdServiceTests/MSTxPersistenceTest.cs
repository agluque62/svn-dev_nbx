using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

using Newtonsoft.Json;

using U5ki.RdService;
using U5ki.Infrastructure;
using Utilities;
using System.Collections.Generic;
using System.Linq;

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
            ServicesHelpers.IgnoreSpreadChannel = true;
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

        protected MSStatus Status2Slave
        {
            get
            {
                var data = new MSStatus();
                data.main_nodes.Add(new MSNodeInfo() { site = "newSite", res = "newTx" });
                data.disabled_nodes.Add(new MSNodeInfo() { site = "newSite", res = "newRx" });
                return data;
            }
        }
        [TestMethod]
        public void TestMethod1()
        {
            SipAgentStart();
            var service = new RdService();
            service.Cfg = JsonConvert.DeserializeObject<Cd40Cfg>(File.ReadAllText("u5ki.LastCfg.json"));
            service.Start();
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();

            MSTxPersistence.GenerateResourceChange<Cd40Cfg>(service.Cfg);
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();

            /** Estructuración de los recursos 1+1 en listas */
            var allMsGrouped = service.MSResources
                .GroupBy(r1 => r1.Frecuency)
                .ToDictionary(g => g.Key, g => g.ToList()
                    .GroupBy(r2=>r2.Site)
                    .ToDictionary(s => s.Key, s =>s.ToList()
                        .GroupBy(r2 => r2.isTx)
                        .ToDictionary(r3 => r3.Key ? "TX" : "RX", r3=>r3.ToList())
                    )
                 );
            /** Los Transmisores*/
            var allMsTxGrouped = service.MSResources
                .GroupBy(r1 => r1.Frecuency)
                .ToDictionary(g => g.Key, g => g.ToList()
                    .GroupBy(r2 => r2.Site)
                    .ToDictionary(s => s.Key, s => s.Where(r2 => r2.isTx).OrderBy(r3 => r3.ID).ToList()
                    )
                 );
            /** Los receptores.*/
            var allMsRxGrouped = service.MSResources
                .GroupBy(r1 => r1.Frecuency)
                .ToDictionary(g => g.Key, g => g.ToList()
                    .GroupBy(r2 => r2.Site)
                    .ToDictionary(s => s.Key, s => s.Where(r2 => r2.isRx).OrderBy(r3 => r3.ID).ToList()
                    )
                 );
            /** Condiciones Inciales. Los primeros TX por orden alfabético son los MAIN */
            allMsTxGrouped.Values.ToList().ForEach(f =>
            {
                f.Values.ToList().ForEach(s =>
                {
                    Assert.AreEqual(s.Count, 2);
                    MSTxPersistence.SelectMain(s.ElementAt(0), s.ElementAt(1));
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                    Assert.IsTrue(MSTxPersistence.IsMain(s.ElementAt(0))==true && MSTxPersistence.IsMain(s.ElementAt(1)) == false);
                    
                    /** Conmuto Todos los TX y los vuelo a dejar Igual */
                    MSTxPersistence.SelectMain(s.ElementAt(1), s.ElementAt(0));
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                    Assert.IsTrue(MSTxPersistence.IsMain(s.ElementAt(1)) == true && MSTxPersistence.IsMain(s.ElementAt(0)) == false);

                    MSTxPersistence.SelectMain(s.ElementAt(0), s.ElementAt(1));
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                    Assert.IsTrue(MSTxPersistence.IsMain(s.ElementAt(0)) == true && MSTxPersistence.IsMain(s.ElementAt(1)) == false);
                });
            });
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();

            allMsRxGrouped.Values.ToList().ForEach(f =>
            {
                f.Values.ToList().ForEach(s =>
                {
                    Assert.IsTrue(s.Count == 2);
                    MSTxPersistence.DisableNode(s.ElementAt(0), true);
                    Assert.IsTrue(MSTxPersistence.IsNodeDisabled(s.ElementAt(0)));
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

                    MSTxPersistence.DisableNode(s.ElementAt(0), false);
                    Assert.IsTrue(MSTxPersistence.IsNodeDisabled(s.ElementAt(0))==false);
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

                    MSTxPersistence.DisableNode(s.ElementAt(1), true);
                    Assert.IsTrue(MSTxPersistence.IsNodeDisabled(s.ElementAt(1)));
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

                    MSTxPersistence.DisableNode(s.ElementAt(1), false);
                    Assert.IsTrue(MSTxPersistence.IsNodeDisabled(s.ElementAt(1)) == false);
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                });
            });


            //Task.Delay(20000).Wait();

            //Task.Delay(TimeSpan.FromSeconds(3)).Wait();
            //MSTxPersistence.GenerateResourceChange<MSStatus>(Status2Slave);

            Task.Delay(TimeSpan.FromSeconds(3)).Wait();
            service.Stop();
            SipAgentStop();
        }
    }
}

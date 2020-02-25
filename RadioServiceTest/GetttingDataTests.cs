using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using U5ki.Infrastructure;
using U5ki.RdService;
using Utilities;


namespace RadioServiceTest
{
    [TestClass]
    public class GetttingDataTests
    {
        private IDictionary<string, RdFrecuency> Frequencies = new Dictionary<string, RdFrecuency>()
        {
        };

        Random Rnd = new Random(Guid.NewGuid().GetHashCode());
        void GeneratePattern()
        {
            var nFrec = Rnd.Next(2, 11);
            for (var frec=0; frec<nFrec; frec++)
            {
                var strFrec = $"123.0{frec}0";
                var Frec = new RdFrecuency(strFrec);
                var nEmpl = Rnd.Next(1, 4);
                for (var empl = 0; empl<nEmpl; empl++)
                {
                    string strSite = $"Site-{empl:D3}";
                    string IdBase = $"_{strFrec}_On_{strSite}";
                    var EsUnoMasUno = Rnd.Next(2) == 1;
                    if (EsUnoMasUno)
                    {
                        var tx1 = new RdResource("Tx1"+IdBase, $"sip:Tx1{IdBase}@10.11.12.13", RdRsType.Tx, strFrec, strSite);
                        var tx2 = new RdResource("Tx2"+IdBase, $"sip:Tx2{IdBase}@10.11.12.13", RdRsType.Tx, strFrec, strSite);
                        var rTxPair = new RdResourcePair(tx1, tx2, null);
                        var rx1 = new RdResource("Rx1"+IdBase, $"sip:Rx1{IdBase}@10.11.12.13", RdRsType.Rx, strFrec, strSite);
                        var rx2 = new RdResource("Rx2"+IdBase, $"sip:Rx2{IdBase}@10.11.12.13", RdRsType.Rx, strFrec, strSite);
                        var rRxPair = new RdResourcePair(rx1, rx2, null);
                        Frec.RdRs["tx"+IdBase] = rTxPair;
                        Frec.RdRs["rx"+IdBase] = rRxPair;
                    }
                    else
                    {
                        Frec.RdRs["tx"+IdBase] = new RdResource("tx"+IdBase, $"sip:tx{IdBase}@10.11.12.13", RdRsType.Tx, strFrec, strSite);
                        Frec.RdRs["rx"+IdBase] = new RdResource("rx"+IdBase, $"sip:rx{IdBase}@10.11.12.13", RdRsType.Rx, strFrec, strSite);
                    }
                }
                Frequencies[strFrec] = Frec;
            }
        }
        [TestMethod]
        public void TestMethod1()
        {
            GeneratePattern();

            var UnoMasUnoFreqs = Frequencies.Values
                .Where(f => f.RdRs.Values.Where(r => r is RdResourcePair).ToList().Count > 0)   // Son Frecuencias 1+1 las que tienen algun RdResourcePair
                .SelectMany(f => f.RdRs.Values)
                .Where(r => r is RdResourcePair)        // De todos los recursos selecciono los pareados.
                .SelectMany(rp =>
                {
                    var resources = (rp as RdResourcePair).GetListResources();
                    resources.ForEach(r => r.SetContainerPair((rp as RdResourcePair)));
                    return resources;                   // Por cada pareado selecciona ambos componentes y marca su container...
                })
                .Select(r => new
                {
                    fr = r.Frecuency,
                    id = r.ID,
                    site = r.Site,
                    tx = r.isTx ? 1 :0,
                    ab = 0,
                    sel = r.GetContainerPair().ActiveResource.Uri1 == r.Uri1 ? 1 : 0,
                    ses = r.Connected ? 1 : 0,
                    uri = r.Uri1
                })
                .ToList();
        }
    }
}

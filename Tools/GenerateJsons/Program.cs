using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

namespace GenerateJsons
{
    static class Extensions
    {
        static readonly Random rndGenerator = new Random(Environment.TickCount);
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rndGenerator.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    class Program
    {
        static readonly Random rndGenerator = new Random(Environment.TickCount);

        static string GenerateCwp(int nCwp)
        {
            var data = new
            {
                lista = new List<object>()
            };
            for (int icwp = 1; icwp < nCwp; icwp++)
            {
                data.lista.Add(new
                {
                    name = $"PICT{icwp:D2}",
                    ip = $"10.12.68.{icwp:D3}",
                    std = rndGenerator.Next(0, 7),
                    panel = rndGenerator.Next(2),
                    jack_exe = rndGenerator.Next(2),
                    jack_ayu = rndGenerator.Next(2),
                    alt_r = rndGenerator.Next(2),
                    alt_t = rndGenerator.Next(2),
                    alt_hf = rndGenerator.Next(2),
                    rec_w = rndGenerator.Next(2),
                    lan1 = rndGenerator.Next(2),
                    lan2 = rndGenerator.Next(2)
                });
            }
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        static object GenerateItemRadio(string Frec, string Site, bool isTx, int AB, bool selected)
        {
            string txOrRx = isTx ? "TX" : "RX";
            string strAB = AB == 0 ? "" : AB == 1 ? "-A" : "-B";
            string Id = $"{txOrRx}{strAB}_{Frec}_On_{Site}";
            return new
            {
                id = Id,
                fr = Frec,
                site = Site,
                tx = isTx ? 1 : 0,
                ab = AB,
                sel= selected ? 1 : 0,
                ses = rndGenerator.Next(2),
                uri = $"sip:{Id}@10.11.12.13"
            };
        }
        static List<object> GenerateEmplRadio(string Frec, int idSite)
        {
            var data = new List<object>();
            var txaSelected = rndGenerator.Next(2) == 1;
            var rxaSelected = rndGenerator.Next(2) == 1;
            string Site = $"Site-{idSite:D3}";

            data.Add(GenerateItemRadio(Frec, Site, true, 1, txaSelected));
            data.Add(GenerateItemRadio(Frec, Site, true, 2, !txaSelected));
            data.Add(GenerateItemRadio(Frec, Site, false, 1, rxaSelected));
            data.Add(GenerateItemRadio(Frec, Site, false, 2, !rxaSelected));
            return data;
        }
        static string GenerateRd11(int nFrec)
        {
            var data = new List<object>();
            int idSite = 1;

            for (int iFrec=0; iFrec<nFrec; iFrec++)
            {
                string strFrec = $"120.{iFrec * 10:D3}";    
                bool fd = rndGenerator.Next(2) == 1;

                if (fd)
                {
                    //
                    int nEmpl = 2 + rndGenerator.Next(2);       // Dos o Tres Ramas.
                    for (int empl=0; empl<nEmpl; empl++)
                    {
                        data.AddRange(GenerateEmplRadio(strFrec, idSite++));
                    }
                }
                else
                {       
                    // Frecuencia SIMPLE.
                    data.AddRange(GenerateEmplRadio(strFrec, idSite++));
                }
            }
            
            data.Shuffle(); // Desordeno los datos.
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        static void Main(string[] args)
        {
            /** Generando Datos de 1+1 */
            File.WriteAllText("rd11.json", GenerateRd11(10)); 
        }
    }
}

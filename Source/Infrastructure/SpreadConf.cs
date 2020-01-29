using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace U5ki.Infrastructure
{
    class SpreadConf
    {
        enum stdLectura { Fuera, EntrandoSegmento, DentroSegmento, EntrandoElemento, Elemento, SaliendoElemento, SaliendoSegmento }

        static Dictionary<string, int> _priorities = new Dictionary<string, int>();

        /// <summary>
        /// 
        /// </summary>
        private static string PathMcast
        {
            get
            {
                if (File.Exists("..\\Ulises5000I-MCast\\U5ki.Mcast.exe"))
                    return "..\\Ulises5000I-MCast\\";
                else if (File.Exists("U5ki.Mcast.exe"))
                    return ".\\";
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, int> SpreadPriorities()
        {
            char[] delimiterChars = { ' ', '{', '}', '\t' };
            int currentPriority = 1;

            /** Ya se ha leido... */
            if (_priorities.Count > 0)
                return _priorities;

            if (!File.Exists(PathMcast + "spread.conf"))
            {
                throw new Exception("No se encuentra el fichero SPREAD.CONF");
            }

            StreamReader fr = new StreamReader(PathMcast + "spread.conf");
            stdLectura std = stdLectura.Fuera;

            while (fr.Peek() >= 0)
            {
                String line = fr.ReadLine();
                if (line.Length != 0 && line.StartsWith("#") == false)
                {
                    switch (std)
                    {
                        case stdLectura.Fuera:
                            if (line.Contains("Spread_Segment"))
                            {
                                std = line.Contains("{")==true ? stdLectura.DentroSegmento : stdLectura.EntrandoSegmento;
                            }
                            break;

                        case stdLectura.EntrandoSegmento:
                            if (line.Contains("{") == true)
                                std = stdLectura.DentroSegmento;
                            break;

                        case stdLectura.DentroSegmento:
                            {
                                string[] partes = line.Split(delimiterChars);
                                foreach (string name in partes)
                                {
                                    if (name.Length != 0)
                                    {
                                        _priorities.Add(name, currentPriority++);
                                        break;
                                    }
                                }
                                std = line.Contains("{") == true ? stdLectura.Elemento : 
                                    line.Contains("}")== true ? stdLectura.Fuera : stdLectura.EntrandoElemento;
                            }
                            break;

                        case stdLectura.EntrandoElemento:
                            if (line.Contains("{") == true)
                                std = stdLectura.Elemento;
                            break;

                        case stdLectura.Elemento:
                            if (line.Contains("}") == true)
                                std = stdLectura.DentroSegmento;
                            break;

                        default:
                            break;

                    }
                }
            }

            /** Escribir en un fichero el resultado*/
            using (StreamWriter file = new StreamWriter("spread-prio.txt"))
            {
                foreach (var entry in _priorities)
                    file.WriteLine("[{0}: {1}]", entry.Key, entry.Value);
            }

            return _priorities;
        }

    }
}

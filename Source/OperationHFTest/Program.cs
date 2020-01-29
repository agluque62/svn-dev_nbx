using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NLog;
using U5ki.Infrastructure;
using U5ki.RdService;

namespace OperationHFTest
{
    class Program
    {
        static List<Tuple<string, string>> Usuarios = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("SECTOR08", "PICT08"),
            new Tuple<string, string>("SECTOR07", "PICT07"),
            new Tuple<string, string>("SECTOR06", "PICT06"),
            new Tuple<string, string>("SECTOR05", "PICT05"),
            new Tuple<string, string>("SECTOR04", "PICT04"),
            new Tuple<string, string>("SECTOR03", "PICT03"),
            new Tuple<string, string>("SECTOR02", "PICT02"),
            new Tuple<string, string>("SECTOR01", "PICT01")
        };
        static int CurrentUser = 0;
        // static List<int> Frecuencies = new List<int>() { 1200000, 2325000, 12456000, 2389000, 5678000, 9876000 };
        static List<RdFrecuency> Frequencies = new List<RdFrecuency>()
        {
            new RdFrecuency() {FrecuenciaSintonizada=1200000, Frecuency="1200"},
            new RdFrecuency() {FrecuenciaSintonizada=2325000, Frecuency="2325"},
            new RdFrecuency() {FrecuenciaSintonizada=12456000,Frecuency="12456"},
            new RdFrecuency() {FrecuenciaSintonizada=2389000, Frecuency="2389"},
            new RdFrecuency() {FrecuenciaSintonizada=5678000, Frecuency="5678"},
            new RdFrecuency() {FrecuenciaSintonizada=9876000, Frecuency="9876"}
        };
        static int CurrentFrequency = 0;

        static RdGestorHF gestor = new RdGestorHF();
        static bool started = false;
        static Task proceso = null;

        static void Main(string[] args)
        {
            LoadTest();
            StartTest();

            
            RdRegistry.RefreshConsole = new Action(() =>
            {
                lock (gestor.Equipos)
                {
                    PrintTitulo();
                    PrintListaEquipos();
                    PrintMenu();
                }
            });

            ConsoleKeyInfo result;
            do
            {
                lock (gestor.Equipos)
                {
                    PrintTitulo();
                    PrintListaEquipos();
                    PrintMenu();
                }
                result = Console.ReadKey(true);
                switch (result.Key)
                {
                    case ConsoleKey.D1:     // Solicitar Tx
                        {
                            RdFrecuency frec = Frequencies.ElementAt(CurrentFrequency);
                            ProcessTxChangeAsk(Usuarios.ElementAt(CurrentUser).Item2,
                                new FrTxChangeAsk() { HostId = Usuarios.ElementAt(CurrentUser).Item1, Frecuency = frec.FrecuenciaSintonizada.ToString(), Tx = true });
                        }
                        break;

                    case ConsoleKey.D2:     // Liberar Tx
                        {
                            RdFrecuency frec = Frequencies.ElementAt(CurrentFrequency);
                            ProcessTxChangeAsk(Usuarios.ElementAt(CurrentUser).Item2,
                                new FrTxChangeAsk() { HostId = Usuarios.ElementAt(CurrentUser).Item1, Frecuency = frec.FrecuenciaSintonizada.ToString(), Tx = false });
                        }
                        break;

                    case ConsoleKey.D3:     // Maniobra SELCAL ON
                        {
                            ProcessSelcalPrepare(Usuarios.ElementAt(CurrentUser).Item2, 
                                new SelcalPrepareMsg() { HostId = Usuarios.ElementAt(CurrentUser).Item1, OnOff = true});
                        }
                        break;
                    case ConsoleKey.D4:     // Maniobra SELCAL OFF
                        {
                            ProcessSelcalPrepare(Usuarios.ElementAt(CurrentUser).Item2,
                                new SelcalPrepareMsg() { HostId = Usuarios.ElementAt(CurrentUser).Item1, OnOff = false });
                        }
                        break;
                    case ConsoleKey.D5:
                        LoadTest();
                        break;

                    case ConsoleKey.U:
                        CurrentUser = CurrentUser < Usuarios.Count - 1 ? CurrentUser + 1 : 0;
                        break;

                    case ConsoleKey.F:
                        CurrentFrequency = CurrentFrequency < Frequencies.Count - 1 ? CurrentFrequency + 1 : 0;
                        break;
                }
            } while (result.Key != ConsoleKey.Escape);

            StopTest();
        }

        /// <summary>
        /// 
        /// </summary>
        static void PrintTitulo()
        {
            Console.Clear();
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Simulador Telemando de Equipos HF. DFNucleo 2017");
            Console.WriteLine("Estado Global del Gestor: {0}", gestor.GlobalStatusPeriodico());
            Console.WriteLine();
            Console.ForegroundColor = last;
        }

        static void PrintListaEquipos()
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Lista de Equipos Simulados");


            List<RdGestorHF.EquipoHF> Equipos = gestor.Equipos;
            int index = 0;
            foreach (RdGestorHF.EquipoHF eq in Equipos)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  [{0,2}]", index++);

                Console.ForegroundColor = eq.Estado == RdGestorHF.EquipoHFStd.stdNoinfo ? ConsoleColor.Gray :
                    eq.Estado == RdGestorHF.EquipoHFStd.stdDisponible ? ConsoleColor.White :
                    eq.Estado == RdGestorHF.EquipoHFStd.stdAsignado ? ConsoleColor.Yellow :
                    eq.Estado == RdGestorHF.EquipoHFStd.stdError ? ConsoleColor.Red :
                    ConsoleColor.Magenta;

                Console.WriteLine(String.Format("{0,10}, {1,15}, {2,8}: {3}", eq.IdEquipo, eq.Estado, eq.Usuario, eq.Frecuencia));

            }

            Console.WriteLine();

            Console.ForegroundColor = last;
        }

        static void PrintMenu()
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("  [U] Usuario: {0}  [F] Frecuencia: {1} ", Usuarios.ElementAt(CurrentUser).Item1, Frequencies.ElementAt(CurrentFrequency).FrecuenciaSintonizada);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Opciones:\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("      [1] Solicitar Transmisor.  [2] Liberar  Transmisor");
            Console.WriteLine("      [3] Maniobra  SELCAL - ON  [4] Maniobra SELCAL - OFF");
            Console.WriteLine("      [5] Recargar...");

            Console.WriteLine();
            Console.WriteLine("  [ESC] Salir");
            Console.WriteLine();

            Console.ForegroundColor = last;
        }

        static bool GetUsuario(ref string user, ref string pict)
        {
            Console.WriteLine();
            int index = 0;
            foreach (Tuple<string, string> userdef in Usuarios)
            {
                Console.Write("[{0, 2}], {1, 8}. ", index++, userdef.Item1);
            }
            Console.WriteLine();
            Console.WriteLine("Seleccione un usuario");
            ConsoleKeyInfo result;
            do
            {
                result = Console.ReadKey(true);
                if (result.Key == ConsoleKey.Escape)
                    return false;
                if (result.KeyChar >= '0' && result.KeyChar <= '9')
                {
                    index = result.KeyChar - '0';
                    if (index < Usuarios.Count)
                    {
                        user = Usuarios.ElementAt(index).Item1;
                        pict = Usuarios.ElementAt(index).Item2;
                        return true;
                    }
                }
            } while (true);
        }

        static RdFrecuency GetFrecuencia()
        {
            Console.WriteLine();
            int index = 0;
            foreach (RdFrecuency item_frec in Frequencies)
            {
                Console.Write("[{0, 2}]: {1, 8}. ", index++, item_frec.FrecuenciaSintonizada);
            }
            Console.WriteLine();
            Console.WriteLine("Seleccione un Frecuencia");
            ConsoleKeyInfo result;
            do
            {
                result = Console.ReadKey(true);
                if (result.Key == ConsoleKey.Escape)
                    return null;
                if (result.KeyChar >= '0' && result.KeyChar <= '9')
                {
                    index = result.KeyChar - '0';
                    if (index < Frequencies.Count)
                    {
                        return Frequencies.ElementAt(index);

                    }
                }
            } while (true);
        }


        static void LoadTest()
        {
            List<HfRangoFrecuencias> rangos = new List<HfRangoFrecuencias>();
            rangos.Add(new HfRangoFrecuencias() { fmin = 1000000, fmax = 50000000 });

            lock (gestor.Equipos)
            {
                gestor.Limpiar();
                foreach (string cfg in Properties.Settings.Default.Equipos)
                {
                    string[] partes = cfg.Split(',');
                    gestor.Equipos.Add(new RdGestorHF.EquipoHF()
                    {
                        IdEquipo = partes[0],
                        SipUri = partes[1],
                        IpRcs = partes[2],
                        Oid = partes[3],
                        Frecs = rangos,
                        Usuario = "",
                        Estado = RdGestorHF.EquipoHFStd.stdNoinfo
                    });
                }
            }
        }

        static void StartTest()
        {
            if (!started)
            {
                started = true;
                proceso = Task.Factory.StartNew(() =>
                {
                    int _HFTimerCount = 5;
                    int _contTimerEvents = 0;
                    int hash = LstEqHash();
                    while (started)
                    {
                        Thread.Sleep(1000);

                        //
                        gestor.CheckFrequency();

                        //
                        if (_HFTimerCount == 0 || ++_contTimerEvents == _HFTimerCount)
                        {
                            _contTimerEvents = 0;
                            gestor.SupervisaEstadoEquipos();
                        }
                        if (hash != LstEqHash())
                        {
                            hash = LstEqHash();
                            lock (gestor.Equipos)
                            {
                                PrintTitulo();
                                PrintListaEquipos();
                                PrintMenu();
                            }
                        }
                    }
                });
            }
        }

        static void StopTest()
        {
            if (started)
            {
                started = false;
                proceso.Wait(2000);
            }
        }

        static int LstEqHash()
        {
            int hash = 0x2D2816FE;

            foreach (RdGestorHF.EquipoHF equipo in gestor.Equipos)
            {
                hash = hash * 31 + ((int)equipo.Estado);
            }

            return hash;
        }

        /// <summary>
        /// Para generar las asignaciones con el mismo modelo de RdService.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        static protected void ProcessTxChangeAsk(string from, FrTxChangeAsk ask)
        {
            AskingThread asking;
            asking.ask = ask;
            asking.from = from;

            Func<string, FrTxChangeAsk, AskingThread> TxChange = ProcessTxChange;
            TxChange.BeginInvoke(from, ask, ProcessedTxChange, TxChange);
        }

        /// <summary>
        /// Thread de Ejecucion....
        /// </summary>
        /// <param name="from"></param>
        /// <param name="ask"></param>
        /// <returns></returns>
        static private AskingThread ProcessTxChange(string from, FrTxChangeAsk ask)
        {
            RdFrecuency rdFr = Frequencies.Where(f => f.FrecuenciaSintonizada.ToString().ToUpper() == ask.Frecuency.ToUpper()).FirstOrDefault();
            AskingThread ret = new AskingThread() { result = -1, from = from, ask = ask };

            if (rdFr != null)
            {
                int prosigue = 0;
                if (rdFr.TipoDeFrecuencia != "HF" || gestor.GlobalStatus() != HFStatusCodes.DISC)
                {
                    prosigue = (ask.Tx == true) ? gestor.AsignarTx(ask.HostId, ref rdFr, from) : gestor.DesasignarTx(ask.HostId, ref rdFr);
                }
                ret.result = prosigue;
            }

            return ret;
        }

        /// <summary>
        /// Thread de Resolucion / Finalizacion...
        /// </summary>
        /// <param name="cookie"></param>
        static private void ProcessedTxChange(IAsyncResult cookie)
        {
            string rdFrData = "";

            try
            {
                var target = (Func<string, FrTxChangeAsk, AskingThread>)cookie.AsyncState;
                AskingThread resultProcess = target.EndInvoke(cookie);
                int prosigue = resultProcess.result;

                if (prosigue != -1)
                {
                    RdFrecuency rdFr = Frequencies.Where(f => f.FrecuenciaSintonizada.ToString().ToUpper() == resultProcess.ask.Frecuency.ToUpper()).FirstOrDefault();
                    if (rdFr != null)
                    {
                        rdFrData = rdFr.FrecuenciaSintonizada.ToString().ToUpper();

                        if (prosigue != (int)RdGestorHF.EquipoHFStd.stdOperationInProgress)
                            RdGestorHF.HFHelper.RespondToFrHfTxChange(resultProcess.from, rdFrData, prosigue);
                        else
                            RdGestorHF.HFHelper.Log(LogLevel.Warn, "Mensaje desechado....", null);
                    }
                    else
                    {
                        RdGestorHF.HFHelper.Log(LogLevel.Error, "No encuentro la Frecuencia en la tabla", null, resultProcess.ask.Frecuency.ToUpper());
                    }
                }
                else
                {
                    RdGestorHF.HFHelper.Log(LogLevel.Error, "Prosige == -1", null, resultProcess.ask.Frecuency.ToUpper());
                }
            }
            catch (Exception x)
            {
                RdGestorHF.HFHelper.Log(LogLevel.Error, x.Message, null, rdFrData);
            }
        }

        static private void ProcessSelcalPrepare(string from, SelcalPrepareMsg msg)
        {
            RdGestorHF.EquipoHF equipo = gestor.Equipos.Where(e => e.Usuario == msg.HostId).FirstOrDefault();
            if (equipo != null)
            {
                string frecuencia = equipo.IDFrecuencia;
                if (frecuencia != null)
                {
                    RdFrecuency rdFr = Frequencies.Where(f => f.FrecuenciaSintonizada.ToString().ToUpper() == frecuencia.ToUpper()).FirstOrDefault();
                    if (rdFr != null)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            bool result = gestor.PrepareSelcal(rdFr, msg.HostId, msg.OnOff, msg.Code);
                            RdGestorHF.HFHelper.RespondToPrepareSelcal(from, frecuencia.ToUpper(), result, result ? msg.Code : "Error");
                        });
                        return;
                    }
                    else
                    {
                        RdGestorHF.HFHelper.Log(LogLevel.Error, "La frecuencia no esta en la tabla", msg.HostId, frecuencia);
                    }
                }
                else
                {
                    RdGestorHF.HFHelper.Log(LogLevel.Error, "No encuentro frecuencia en equipo", msg.HostId, frecuencia);
                }
            }
            else
            {
                RdGestorHF.HFHelper.Log(LogLevel.Error, "No encuentro equuipo asignado a usuario", msg.HostId);
            }

        }

    }
}

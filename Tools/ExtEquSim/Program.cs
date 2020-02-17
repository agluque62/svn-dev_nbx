using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreSipNet;

namespace ExtEquSim
{
    class Program
    {
        static SipAgentNetSettings settings = new SipAgentNetSettings()
        {
            Default = new SipAgentNetSettings.DefaultSettings()
            {
                DefaultCodec = "PCMA",
                DefaultDelayBufPframes = 3,
                DefaultJBufPframes = 4,
                SndSamplingRate = 8000,
                RxLevel = 1,
                TxLevel = 1,
                SipLogLevel = 3,
                TsxTout = 400,
                InvProceedingIaTout = 1000,
                InvProceedingMonitoringTout = 30000,
                InvProceedingDiaTout = 30000,
                InvProceedingRdTout = 1000,
                KAPeriod = 200,
                KAMultiplier = 10
            }
        };
        static AppConfig Cfg { get; set; }

        static void Main(string[] args)
        {
            Helper.Log<Program>("Arranque...");

            AppConfig.Get((cfg, excep) =>
            {
                Cfg = cfg;
            });

            Helper.CommandLineParser(args, (ip, port, users) =>
            {
                Cfg.IP = ip;
                Cfg.PORT = port;
                Cfg.Users = users ?? Cfg.Users;
            });

            Helper.Log<Program>($"Config IP: {Cfg.IP}, Port: {Cfg.PORT}");
            Helper.Log<Program>($"Users: {String.Join(", ", Cfg.Users)}");

            SipAgentNet.Init(settings, "Testing", Cfg.IP, (uint)Cfg.PORT);
            SipAgentNet.Start();
            LoadUsers();

            ConsoleKeyInfo result;
            do
            {

                PrintTitulo();
                PrintListaEquipos();
                PrintMenu();
                result = Console.ReadKey(true);
                switch (result.Key)
                {
                    case ConsoleKey.D1: // Agregar Equipo
                        GetEquipo((equipo) =>
                        {
                            if (Cfg.Users.Contains(equipo) == false)
                            {
                                Cfg.Users.Add(equipo);
                                LoadUsers();
                            }
                        });
                        break;
                    case ConsoleKey.D2: // Eliminar Equipo
                        GetEquipo((equipo) =>
                        {
                            if (Cfg.Users.Contains(equipo) == true)
                            {
                                Cfg.Users.Remove(equipo);
                                LoadUsers();
                            }
                        });
                        break;
                }
            } while (result.Key != ConsoleKey.Escape);


            Console.WriteLine("Descargando Agente Sip...");

            SipAgentNet.DestroyAccounts();
            SipAgentNet.End();

            Cfg.Save();
        }

        static void PrintTitulo()
        {
            Console.Clear();
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"ExtEquSim. Simulador de Actividad de Equipos Externos. Nucleo CC 2020");
            Console.WriteLine($"Agente Escuchando en {Cfg.IP}:{Cfg.PORT}");
            Console.WriteLine();
            Console.ForegroundColor = last;
        }

        static void PrintListaEquipos()
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Lista de Equipos Simulados");

            var users_groups = (from index in Enumerable.Range(0, Cfg.Users.Count)
                        group Cfg.Users[index] by index / 4).ToList();
            users_groups.ForEach(user_group =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                user_group.ToList().ForEach(usuario =>
                {
                    Console.Write($"{usuario,16} ");
                });
                Console.WriteLine();
            });

            Console.WriteLine();
            Console.ForegroundColor = last;
        }

        static void PrintMenu()
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine();
            //Console.WriteLine("  [U] Usuario: {0}  [F] Frecuencia: {1} ", Usuarios.ElementAt(CurrentUser).Item1, Frequencies.ElementAt(CurrentFrequency).FrecuenciaSintonizada);
            //Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Opciones:\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("      [1] Agregar Equipo.  [2] Liberar  Equipo");
            //Console.WriteLine("      [3] Maniobra  SELCAL - ON  [4] Maniobra SELCAL - OFF");
            //Console.WriteLine("      [5] Recargar...");

            Console.WriteLine();
            Console.WriteLine("  [ESC] Salir");
            Console.WriteLine();

            Console.ForegroundColor = last;
        }
        static void GetEquipo(Action<string> notify)
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Introduzca Nombre equipo: ");
            var equipo = Console.ReadLine();
            if (equipo.Length >= 0)
            {
                notify(equipo);
            }
        }

        static void LoadUsers()
        {
            SipAgentNet.DestroyAccounts();

            Cfg.Users.ForEach(u =>
            {
                SipAgentNet.CreateAccount(u);
            });
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using u5ki.RemoteControlService;
using U5ki.CfgService;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;
using U5ki.RdService;
using U5ki.RdService.Gears;

namespace Test
{
    public class Program : BaseCode
    {
        /// <summary>
        /// 
        /// </summary>
        private static RdService _rdService;
        private static CfgService _cfgService;
        private static Cd40Cfg _configuration;
        private static Boolean _clearOnAction = true;
        private static Boolean _returnSemaphore;
        private static Int32 _timerInterval = 1000;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            _configuration = new ConfigurationEmulator().RdServiceConfigurationGet();
            New_Menu_Main();
        }

        #region Menu

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        static void Menu_Title(String title)
        {
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" -- " + title.ToUpper() + " -- ");
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [Is Test Running:  " + Globals.Test.IsTestRunning + "]");
            Console.WriteLine("  [Random Behaviour:  " + Globals.Test.RandomBehaviour + "]");
            Console.WriteLine("  [Remote Control ByPass:  " + Globals.Test.RemoteControlByPass + "]");
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
        }
        /// <summary>
        /// 
        /// </summary>
        static void Menu_Main()
        {
            Globals.Test.IsTestRunning = false;
            Globals.Test.RandomBehaviour = false;
            Globals.Test.RemoteControlByPass = true;

            Boolean loop = true;
            while (loop)
            {
                loop = false;
                if (_clearOnAction)
                    Console.Clear();

                Menu_Title("TEST CONSOLE MAIN MENU");
                Console.WriteLine(" Options: ");
                Console.WriteLine();
                Console.WriteLine("        1: RdService load configuration test. [DO NOT USE IN THIS ENVIRONMENT]");
                Console.WriteLine();
                Console.WriteLine("        2: NMmanager emulation test normal.");
                Console.WriteLine();
                Console.WriteLine("        3: NMmanager emulation test with random behaviour.");
                Console.WriteLine();
                Console.WriteLine("        4: NMmanager emulation test with SemiReal configuration.");
                Console.WriteLine();
                Console.WriteLine("        5: Remote Control test.");
                Console.WriteLine();
                Console.WriteLine("    [Esc]: Exit Application.");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Choose an Option: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();

                Console.Write(" => ");
                ConsoleKeyInfo key = Console.ReadKey();

                if (_clearOnAction)
                    Console.Clear();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        Menu_Exit();
                        break;

                    case ConsoleKey.D1:
                        Test_RdService();
                        break;

                    case ConsoleKey.D2:
                        Test_NMmanager();
                        break;

                    case ConsoleKey.D3:
                        Globals.Test.RandomBehaviour = true;
                        Test_NMmanager();
                        break;

                    case ConsoleKey.D4:
                        _configuration = new ConfigurationEmulator().RdServiceConfigurationSemiRealGet();
                        Test_NMmanager();
                        break;

                    case ConsoleKey.D5:
                        Menu_RC();
                        break;

                    default:
                        loop = true;
                        break;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void Menu_RC()
        {
            Globals.Test.IsTestRunning = false;
            Globals.Test.RandomBehaviour = false;
            Globals.Test.RemoteControlByPass = false;

            Boolean loop = true;
            while (loop)
            {
                loop = false;
                if (_clearOnAction)
                    Console.Clear();

                Menu_Title("REMOTE CONTROL TEST [SNMP]");
                Console.WriteLine(" Options: ");
                Console.WriteLine();
                Console.WriteLine("        1: Frecuency Set.");
                Console.WriteLine();
                Console.WriteLine("        2: Frecuency Get.");
                Console.WriteLine();
                Console.WriteLine("        3: Check.");
                Console.WriteLine();
                Console.WriteLine("    [Esc]: Return to Menu.");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Choose an Option: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();

                Console.Write(" => ");
                ConsoleKeyInfo key = Console.ReadKey();

                if (_clearOnAction)
                    Console.Clear();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        Menu_Main();
                        break;

                    case ConsoleKey.D1:
                        Test_RC_FrecuencySet();
                        break;

                    case ConsoleKey.D2:
                        Test_RC_FrecuencyGet();
                        break;

                    case ConsoleKey.D3:
                        Test_RC_CheckGear();
                        break;

                    default:
                        loop = true;
                        break;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void Menu_Exit()
        {
            if (_clearOnAction)
                Console.Clear();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("    Exiting");

            for (Int32 count = 0; count < 6; count++)
            {
                Console.Write(" .");
                Thread.Sleep(100);
            }

            System.Environment.Exit(0);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static IPAddress IPAddressGet()
        {
            while (true)
            {
                // -----------------------------------
                // IP
                Console.WriteLine();
                Console.WriteLine(" Type destiny IP and press Enter (You can type it without the '192.168.', or just press enter to use '192.168.52.202'): ");
                Console.WriteLine();
                Console.Write(" => ");
                String endIP = Console.ReadLine();

                if (endIP.ToUpper() == "X")
                    return null;

                if (endIP == String.Empty)
                    return IPAddress.Parse("192.168.52.202");

                IPAddress output;
                if (IPAddress.TryParse("192.168." + endIP, out output))
                    return output;
                else if (IPAddress.TryParse(endIP, out output))
                    return output;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static Int32? PortGet()
        {
            while (true)
            {
                // -----------------------------------
                // Port
                Console.WriteLine();
                Console.WriteLine(" Type PORT and press Enter (You can just press enter to use '161'): ");
                Console.WriteLine();
                Console.Write(" => ");
                String portString = Console.ReadLine();

                if (portString.ToUpper() == "X")
                    return null;

                if (portString == String.Empty)
                    return 161;

                Int32 port;
                if (Int32.TryParse(portString, out port))
                    return port;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static String FrecuencyGet()
        {
            while (true)
            {
                // -----------------------------------
                // Frecuency
                Console.WriteLine();
                Console.WriteLine(" Type new Frecuency and press Enter (You can just press Enter to use '125.200.000'): ");
                Console.WriteLine();
                Console.Write(" => ");
                String frecuencyString = Console.ReadLine();

                if (frecuencyString.ToUpper() == "X")
                    return null;

                if (frecuencyString == String.Empty)
                    return "125200000";

                Int32 frecuency;
                if (Int32.TryParse(frecuencyString, out frecuency))
                    return frecuencyString;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="frecuencyType"></param>
        /// <param name="frecuency"></param>
        /// <returns></returns>
        static BaseGear GearGet(IPAddress ip, Tipo_Frecuencia frecuencyType, String frecuency)
        {
            return new ConfigurationEmulator().GearGet(
                ip,
                frecuencyType,
                frecuency);
        }

        #endregion

        #region Test

        /// <summary>
        /// 
        /// </summary>
        static void Test_RdService()
        {
            if (_clearOnAction)
                Console.Clear();

            Menu_Title("Rd Service Test");
            Console.WriteLine(" IMPORTANT: Press [X] to go abort and go back to the main menu.");
            Console.WriteLine();

            // MCast.
            Process.Start(new ProcessStartInfo("U5ki.Mcast.exe", "-console"));

            // Config Service.
            _cfgService = new CfgService();
            _cfgService.Start();

            // RDService.
            _rdService = new RdService();
            _rdService.Start();
            _rdService.OnMasterStatusChanged(null, true);
            _rdService.MNManager.TimerInterval = _timerInterval;

            Int32 count = 0;
            while (!_rdService.Master)
            {
                Thread.Sleep(1000);
                count++;
                Console.WriteLine(" Waiting for service to become Master: " + count + " seconds.");

                if (count > 19)
                {
                    if (_clearOnAction)
                        Console.Clear();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("    Timeout waiting to service to become Master.");
                    Console.WriteLine("    Back to Menu.");
                    Thread.Sleep(5000);
                    Menu_Main();
                    return;
                }
            }

            _rdService.ProcessNewConfig(_configuration);

            Boolean loop = true;
            while (loop)
            {
                loop = false;

                Console.WriteLine();
                Console.Write(" => ");
                ConsoleKeyInfo key = Console.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.X:
                        _rdService.Stop();
                        _rdService = null;
                        Menu_Main();
                        break;

                    default:
                        loop = true;
                        break;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void Test_NMmanager()
        {
            if (_clearOnAction)
                Console.Clear();

            // Sub Menu.
            Menu_Title("NMManager Bypasss Test");

            Globals.Test.IsTestRunning = true;

            _rdService = new RdService();
            _rdService.Start();
            _rdService.OnMasterStatusChanged(null, true);
            _rdService.MNManager.TimerInterval = _timerInterval;

            Int32 count = 0;
            Console.WriteLine(" Waiting for service to become Master.");
            while (!_rdService.Master)
            {
                Thread.Sleep(500);
                count++;

                if (count > 19)
                {
                    if (_clearOnAction)
                        Console.Clear();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("    Timeout waiting to service to become Master.");
                    Console.WriteLine("    Press a key to return to menu.");
                    Console.ReadKey();
                    Menu_Main();
                    return;
                }
            }

            Test_NMmanager_Menu(true, true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="showMenu"></param>
        /// <param name="createConfiguration"></param>
        static void Test_NMmanager_Menu(Boolean showMenu = true, Boolean createConfiguration = false)
        {

            Boolean loop = true;
            while (loop)
            {
                loop = false;

                if (showMenu)
                {
                    if (_clearOnAction)
                        Console.Clear();

                    if (createConfiguration)
                    {
                        createConfiguration = false;

                        Console.WriteLine();
                        Console.WriteLine(" --------------------------------------------------------------------------------------------");
                        Console.WriteLine();
                        Console.WriteLine(" LOADING Configuration . . . ");
                        Console.WriteLine();
                        Console.WriteLine(" --------------------------------------------------------------------------------------------");
                        Console.WriteLine();
                        _rdService.ProcessNewConfig(_configuration);
                    }

                    Console.WriteLine();
                    Console.WriteLine(" --------------------------------------------------------------------------------------------");
                    Console.WriteLine(" Options: ");
                    Console.WriteLine();
                    Console.WriteLine("        0: Clear Window.");
                    Console.WriteLine("        1: Show Gears Status.");
                    Console.WriteLine();
                    Console.WriteLine("        2: Set Gear Ready.");
                    Console.WriteLine("        3: Set Gear Timeout.");
                    Console.WriteLine("        4: Set Gear Fail.");
                    Console.WriteLine("        5: Set Gear Local.");
                    Console.WriteLine();
                    Console.WriteLine("        6: Set ALL Gear Ready.");
                    Console.WriteLine("        7: Set ALL Master Timeout.");
                    Console.WriteLine("        8: Set ALL Master Fail.");
                    Console.WriteLine("        9: Set ALL Master Local.");
                    Console.WriteLine();
                    Console.WriteLine("    [Esc]: Stop Process and back to the Main Menu.");
                    Console.WriteLine(" --------------------------------------------------------------------------------------------");
                    Console.WriteLine();
                    Console.WriteLine(" Choose an Option: ");
                    Console.WriteLine();
                    Console.WriteLine(" --------------------------------------------------------------------------------------------");
                }

                Console.WriteLine();
                Console.Write(" => ");
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        _rdService.Stop();
                        _rdService = null;
                        Menu_Main();
                        break;

                    case ConsoleKey.D0:
                        Test_NMmanager_Menu();
                        break;

                    case ConsoleKey.D1:
                        Test_NMmanager_Show();
                        break;

                    case ConsoleKey.D2:
                        ToolGearSet(GearOperationStatus.OK);
                        break;

                    case ConsoleKey.D3:
                        ToolGearSet(GearOperationStatus.Timeout);
                        break;

                    case ConsoleKey.D4:
                        ToolGearSet(GearOperationStatus.Fail);
                        break;

                    case ConsoleKey.D5:
                        ToolGearSet(GearOperationStatus.Local);
                        break;

                    case ConsoleKey.D6:
                        ToolGearSetReadyAll();
                        Test_NMmanager_Menu();
                        break;

                    case ConsoleKey.D7:
                        ToolGearSetTimeoutMasters();
                        Test_NMmanager_Menu();
                        break;

                    case ConsoleKey.D8:
                        ToolGearSetFailMasters();
                        Test_NMmanager_Menu();
                        break;

                    case ConsoleKey.D9:
                        ToolGearSetLocalMaster();
                        Test_NMmanager_Menu();
                        break;

                    default:
                        loop = true;
                        break;
                }

            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void Test_NMmanager_Show()
        {
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" GEARS STATUS");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            // Cargar los principales
            Console.WriteLine("   MASTERS:");
            foreach (BaseGear gear in _rdService.MNManager.NodePool.Values
                .Where(e => e.WorkingFormat == Tipo_Formato_Trabajo.Principal
                    || e.WorkingFormat == Tipo_Formato_Trabajo.Ambos))
            {
                String console = "     " + gear.ToString(false);
                if (Globals.Test.Gears.GearsReal.Contains(gear.IP))
                    console += " [REAL GEAR]";
                Console.WriteLine(console);
            }

            Console.WriteLine("");

            // Cargar los secundarios
            Console.WriteLine("   SLAVES:");
            foreach (BaseGear gear in _rdService.MNManager.NodePool.Values
                    .Where(e => e.WorkingFormat == Tipo_Formato_Trabajo.Reserva))
            {
                String console = "     " + gear.ToString(false);
                if (Globals.Test.Gears.GearsReal.Contains(gear.IP))
                    console += " [REAL GEAR]";
                Console.WriteLine(console);
            }

            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            if (Globals.Test.Gears.GearsTimeout.Count == 0 && Globals.Test.Gears.GearsFails.Count == 0)
                Console.WriteLine(" All Gears working OK.");

            foreach (String node in Globals.Test.Gears.GearsTimeout)
                Console.WriteLine(" Gear " + node + " is set Timeout.");

            foreach (String node in Globals.Test.Gears.GearsFails)
                Console.WriteLine(" Gear " + node + " is set Fail.");


            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            Test_NMmanager_Menu(false);
        }
        /// <summary>
        /// 
        /// </summary>
        static void Test_RC_FrecuencySet()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Set Frecunecy with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                Menu_Main();
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                Menu_Main();
                return;
            }
            String frecuency = FrecuencyGet();
            if (null == frecuency)
            {
                Menu_Main();
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.ConfigureNode(
                RCConfigurationAction.Assing,
                Tools_AsynResponse,
                GearGet(ip, Tipo_Frecuencia.VHF, frecuency),
                false);

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);

            Menu_RC();
        }
        /// <summary>
        /// 
        /// </summary>
        static void Test_RC_FrecuencyGet()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Get Frecunecy with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                Menu_Main();
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                Menu_Main();
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.FrecuencyGet(
                Tools_AsynResponse_String,
                GearGet(ip, Tipo_Frecuencia.VHF, null));

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);

            Menu_RC();
        }
        /// <summary>
        /// 
        /// </summary>
        static void Test_RC_CheckGear()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Test status on element with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                Menu_RC();
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                Menu_RC();
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.CheckNode(
                Tools_AsynResponse,
                GearGet(ip, Tipo_Frecuencia.VHF, null));

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);

            Menu_RC();
        }

        #endregion

        #region Tools

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        static void ToolGearSet(GearOperationStatus status)
        {
            Boolean loop = true;
            while (loop)
            {
                loop = false;

                if (_clearOnAction)
                    Console.Clear();

                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine(" CHANGE GEAR STATUS TO: " + status);
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine("        -1: To do nothing and return.");

                Int32 count = -1;
                foreach (Node node in _configuration.Nodes)
                {
                    count++;
                    Console.WriteLine("        Press '" + node.Id + "': "
                        + " [" + node.IpGestor + "]"
                        + " [" + node.FrecuenciaPrincipal + "]"
                        + " [" + node.TipoDeFrecuencia + "]"
                        + " [" + node.FormaDeTrabajo + "]");
                }

                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Select a Gear to " + status + " and click ENTER: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();

                Console.Write(" => ");
                String readed = Console.ReadLine();

                // Exit.
                if (readed == "-1")
                {
                    Test_NMmanager_Menu();
                    continue;
                }

                // Check the node.
                String nodeId = _configuration.Nodes.Where(e => e.Id == readed).Select(e => e.Id).FirstOrDefault();
                if (String.IsNullOrEmpty(nodeId))
                {
                    loop = true;
                    continue;
                }

                // Add the node.
                switch (status)
                {
                    case GearOperationStatus.Fail:
                        if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                            Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                        if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                            Globals.Test.Gears.GearsLocal.Remove(nodeId);
                        if (!Globals.Test.Gears.GearsFails.Contains(nodeId))
                            Globals.Test.Gears.GearsFails.Add(nodeId);
                        break;

                    case GearOperationStatus.OK:

                        if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                            Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                        if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                            Globals.Test.Gears.GearsFails.Remove(nodeId);
                        if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                            Globals.Test.Gears.GearsLocal.Remove(nodeId);
                        break;

                    case GearOperationStatus.Timeout:

                        if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                            Globals.Test.Gears.GearsFails.Remove(nodeId);
                        if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                            Globals.Test.Gears.GearsLocal.Remove(nodeId);
                        if (!Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                            Globals.Test.Gears.GearsTimeout.Add(nodeId);
                        break;

                    case GearOperationStatus.Local:
                        if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                            Globals.Test.Gears.GearsFails.Remove(nodeId);
                        if (!Globals.Test.Gears.GearsLocal.Contains(nodeId))
                            Globals.Test.Gears.GearsLocal.Add(nodeId);
                        if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                            Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                        break;
                }
                Test_NMmanager_Menu();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void ToolGearSetReadyAll()
        {
            Globals.Test.Gears.GearsFails.Clear();
            Globals.Test.Gears.GearsTimeout.Clear();
            Globals.Test.Gears.GearsLocal.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        static void ToolGearSetTimeoutMasters()
        {
            foreach (Node node in _configuration.Nodes.Where(
                e => e.FormaDeTrabajo == Tipo_Formato_Trabajo.Principal
                    || e.FormaDeTrabajo == Tipo_Formato_Trabajo.Ambos))
            {
                if (Globals.Test.Gears.GearsFails.Contains(node.Id))
                    Globals.Test.Gears.GearsFails.Remove(node.Id);
                if (Globals.Test.Gears.GearsLocal.Contains(node.Id))
                    Globals.Test.Gears.GearsLocal.Remove(node.Id);
                if (!Globals.Test.Gears.GearsTimeout.Contains(node.Id))
                    Globals.Test.Gears.GearsTimeout.Add(node.Id);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void ToolGearSetFailMasters()
        {
            foreach (Node node in _configuration.Nodes.Where(
                e => e.FormaDeTrabajo == Tipo_Formato_Trabajo.Principal
                    || e.FormaDeTrabajo == Tipo_Formato_Trabajo.Ambos))
            {
                if (Globals.Test.Gears.GearsTimeout.Contains(node.Id))
                    Globals.Test.Gears.GearsTimeout.Remove(node.Id);
                if (Globals.Test.Gears.GearsLocal.Contains(node.Id))
                    Globals.Test.Gears.GearsLocal.Remove(node.Id);
                if (!Globals.Test.Gears.GearsFails.Contains(node.Id))
                    Globals.Test.Gears.GearsFails.Add(node.Id);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static void ToolGearSetLocalMaster()
        {
            foreach (Node node in _configuration.Nodes.Where(
                e => e.FormaDeTrabajo == Tipo_Formato_Trabajo.Principal
                    || e.FormaDeTrabajo == Tipo_Formato_Trabajo.Ambos))
            {
                if (Globals.Test.Gears.GearsTimeout.Contains(node.Id))
                    Globals.Test.Gears.GearsTimeout.Remove(node.Id);
                if (Globals.Test.Gears.GearsFails.Contains(node.Id))
                    Globals.Test.Gears.GearsFails.Remove(node.Id);
                if (!Globals.Test.Gears.GearsLocal.Contains(node.Id))
                    Globals.Test.Gears.GearsLocal.Add(node.Id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        static void Tools_AsynResponse(GearOperationStatus status)
        {
            Console.WriteLine();
            Console.WriteLine(" RESPONSE IS " + status.ToString().ToUpper() + " (Press any Key to return).");
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            Console.Write(" => ");
            Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine(" Returning to Menu . . . ");
            Console.WriteLine();

            _returnSemaphore = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frecuency"></param>
        static void Tools_AsynResponse_String(String frecuency)
        {
            Console.WriteLine();
            Console.WriteLine(" RESPONSE IS " + frecuency + " (Press any Key to return).");
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            Console.Write(" => ");
            Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine(" Returning to Menu . . . ");
            Console.WriteLine();

            _returnSemaphore = false;
        }

        #endregion

        #region New MENU
        /// <summary>
        /// 
        /// </summary>
        static void New_Menu_Main()
        {
            Globals.Test.IsTestRunning = false;
            Globals.Test.RandomBehaviour = false;
            Globals.Test.RemoteControlByPass = true;

            ConsoleKeyInfo key;
            do
            {

                Console.Clear();
                Menu_Title("TEST CONSOLE MAIN MENU");
                Console.WriteLine(" Options: ");
                Console.WriteLine();
                Console.WriteLine("        1: RdService load configuration test. [DO NOT USE IN THIS ENVIRONMENT]");
                Console.WriteLine("        2: NMmanager emulation test normal.");
                Console.WriteLine("        3: NMmanager emulation test with random behaviour.");
                Console.WriteLine("        4: NMmanager emulation test with SemiReal configuration.");
                Console.WriteLine("        5: Remote Control test.");
                Console.WriteLine();
                Console.WriteLine("    [Esc]: Exit Application.");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine(" Choose an Option: ");
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.Write(" => ");

                key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        Menu_Exit();
                        break;

                    case ConsoleKey.D1:
                        Test_RdService();
                        break;

                    case ConsoleKey.D2:
                        New_Test_NMmanager();
                        break;

                    case ConsoleKey.D3:
                        Globals.Test.RandomBehaviour = true;
                        New_Test_NMmanager();
                        break;

                    case ConsoleKey.D4:
                        _configuration = new ConfigurationEmulator().RdServiceConfigurationSemiRealGet();
                        New_Test_NMmanager();
                        break;

                    case ConsoleKey.D5:
                        New_Menu_RC();
                        break;

                    default:
                        break;
                }

            } while (key.Key != ConsoleKey.Escape);

        }

        /// <summary>
        /// 
        /// </summary>
        static void New_Test_NMmanager()
        {
            Console.Clear();

            Globals.Test.IsTestRunning = true;

            _rdService = new RdService();
            _rdService.Start();
            _rdService.OnMasterStatusChanged(null, true);
            _rdService.MNManager.TimerInterval = _timerInterval;
            System.Threading.Thread.Sleep(2500);
            _rdService.ProcessNewConfig(_configuration);

            ConsoleKeyInfo key;
            do
            {
                Console.Clear();
                Menu_Title("NMManager Bypasss Test");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine(" Options: ");
                Console.WriteLine();
                Console.WriteLine("        0: Clear Window.");
                Console.WriteLine("        1: Show Gears Status.");
                Console.WriteLine();
                Console.WriteLine("        2: Set Gear Ready.");
                Console.WriteLine("        3: Set Gear Timeout.");
                Console.WriteLine("        4: Set Gear Fail.");
                Console.WriteLine("        5: Set Gear Local.");
                Console.WriteLine();
                Console.WriteLine("        6: Set ALL Gear Ready.");
                Console.WriteLine("        7: Set ALL Master Timeout.");
                Console.WriteLine("        8: Set ALL Master Fail.");
                Console.WriteLine("        9: Set ALL Master Local.");
                Console.WriteLine();
                Console.WriteLine("    [Esc]: Stop Process and back to the Main Menu.");
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Choose an Option: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.Write(" => ");

                key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        _rdService.Stop();
                        _rdService = null;
                        break;

                    case ConsoleKey.D0:
                        break;

                    case ConsoleKey.D1:
                        New_Test_NMmanager_Show();
                        break;

                    case ConsoleKey.D2:
                        ToolGearSet(GearOperationStatus.OK);
                        break;

                    case ConsoleKey.D3:
                        New_ToolGearSet(GearOperationStatus.Timeout);
                        break;

                    case ConsoleKey.D4:
                        New_ToolGearSet(GearOperationStatus.Fail);
                        break;

                    case ConsoleKey.D5:
                        New_ToolGearSet(GearOperationStatus.Local);
                        break;

                    case ConsoleKey.D6:
                        ToolGearSetReadyAll();
                        break;

                    case ConsoleKey.D7:
                        ToolGearSetTimeoutMasters();
                        break;

                    case ConsoleKey.D8:
                        ToolGearSetFailMasters();
                        break;

                    case ConsoleKey.D9:
                        ToolGearSetLocalMaster();
                        break;

                    default:
                        break;
                }

            } while (key.Key != ConsoleKey.Escape);

        }

        /// <summary>
        /// 
        /// </summary>
        static void New_Test_NMmanager_Show()
        {
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" GEARS STATUS");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            // Cargar los principales
            Console.WriteLine("   MASTERS:");
            foreach (BaseGear gear in _rdService.MNManager.NodePool.Values
                .Where(e => e.WorkingFormat == Tipo_Formato_Trabajo.Principal
                    || e.WorkingFormat == Tipo_Formato_Trabajo.Ambos))
            {
                String console = "     " + gear.ToString(false);
                if (Globals.Test.Gears.GearsReal.Contains(gear.IP))
                    console += " [REAL GEAR]";
                Console.WriteLine(console);
            }

            Console.WriteLine("");

            // Cargar los secundarios
            Console.WriteLine("   SLAVES:");
            foreach (BaseGear gear in _rdService.MNManager.NodePool.Values
                    .Where(e => e.WorkingFormat == Tipo_Formato_Trabajo.Reserva))
            {
                String console = "     " + gear.ToString(false);
                if (Globals.Test.Gears.GearsReal.Contains(gear.IP))
                    console += " [REAL GEAR]";
                Console.WriteLine(console);
            }

            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            if (Globals.Test.Gears.GearsTimeout.Count == 0 && Globals.Test.Gears.GearsFails.Count == 0 && Globals.Test.Gears.GearsLocal.Count==0)
                Console.WriteLine(" All Gears working OK.");

            foreach (String node in Globals.Test.Gears.GearsTimeout)
                Console.WriteLine(" Gear " + node + " is set Timeout.");

            foreach (String node in Globals.Test.Gears.GearsFails)
                Console.WriteLine(" Gear " + node + " is set Fail.");

            foreach (String node in Globals.Test.Gears.GearsLocal)
                Console.WriteLine(" Gear " + node + " is set Local.");

            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();

            Console.WriteLine(" Press any key ");
            Console.Write(" => ");
            Console.ReadKey();
        }
        /// <summary>
        /// 
        /// </summary>
        static void New_Menu_RC()
        {
            Globals.Test.IsTestRunning = false;
            Globals.Test.RandomBehaviour = false;
            Globals.Test.RemoteControlByPass = false;

                        
            ConsoleKeyInfo key;            
            do
            {
                Console.Clear();

                Menu_Title("REMOTE CONTROL TEST [SNMP]");
                Console.WriteLine(" Options: ");
                Console.WriteLine();
                Console.WriteLine("        1: Frecuency Set.");
                Console.WriteLine();
                Console.WriteLine("        2: Frecuency Get.");
                Console.WriteLine();
                Console.WriteLine("        3: Check.");
                Console.WriteLine();
                Console.WriteLine("    [Esc]: Return to Menu.");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Choose an Option: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();

                Console.Write(" => ");
                key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                        New_Test_RC_FrecuencySet();
                        break;

                    case ConsoleKey.D2:
                        New_Test_RC_FrecuencyGet();
                        break;

                    case ConsoleKey.D3:
                        New_Test_RC_CheckGear();
                        break;

                    default:
                        break;
                }
            }            
            while (key.Key != ConsoleKey.Escape);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        static void New_ToolGearSet(GearOperationStatus status)
        {
            String readed = "";
            do
            {
                Console.Clear();

                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine(" CHANGE GEAR STATUS TO: " + status);
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine("        -1: To do nothing and return.");

                Int32 count = -1;
                foreach (Node node in _configuration.Nodes)
                {
                    count++;
                    Console.WriteLine("        Press '" + node.Id + "': "
                        + " [" + node.IpGestor + "]"
                        + " [" + node.FrecuenciaPrincipal + "]"
                        + " [" + node.TipoDeFrecuencia + "]"
                        + " [" + node.FormaDeTrabajo + "]");
                }

                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" Select a Gear to " + status + " and click ENTER: ");
                Console.WriteLine();
                Console.WriteLine(" --------------------------------------------------------------------------------------------");
                Console.WriteLine();
                Console.Write(" => ");

                readed = Console.ReadLine();
                String nodeId = _configuration.Nodes.Where(e => e.Id == readed).Select(e => e.Id).FirstOrDefault();
                if (String.IsNullOrEmpty(nodeId) == false)
                {
                    switch (status)
                    {
                        case GearOperationStatus.Fail:
                            if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                                Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                            if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                                Globals.Test.Gears.GearsLocal.Remove(nodeId);
                            if (!Globals.Test.Gears.GearsFails.Contains(nodeId))
                                Globals.Test.Gears.GearsFails.Add(nodeId);
                            break;

                        case GearOperationStatus.OK:

                            if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                                Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                            if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                                Globals.Test.Gears.GearsFails.Remove(nodeId);
                            if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                                Globals.Test.Gears.GearsLocal.Remove(nodeId);
                            break;

                        case GearOperationStatus.Timeout:

                            if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                                Globals.Test.Gears.GearsFails.Remove(nodeId);
                            if (Globals.Test.Gears.GearsLocal.Contains(nodeId))
                                Globals.Test.Gears.GearsLocal.Remove(nodeId);
                            if (!Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                                Globals.Test.Gears.GearsTimeout.Add(nodeId);
                            break;

                        case GearOperationStatus.Local:
                            if (Globals.Test.Gears.GearsFails.Contains(nodeId))
                                Globals.Test.Gears.GearsFails.Remove(nodeId);
                            if (!Globals.Test.Gears.GearsLocal.Contains(nodeId))
                                Globals.Test.Gears.GearsLocal.Add(nodeId);
                            if (Globals.Test.Gears.GearsTimeout.Contains(nodeId))
                                Globals.Test.Gears.GearsTimeout.Remove(nodeId);
                            break;
                    }
                    return;
                }

            } while (readed != "-1");

        }
        /// <summary>
        /// 
        /// </summary>
        static void New_Test_RC_FrecuencySet()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Set Frecunecy with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                return;
            }
            String frecuency = FrecuencyGet();
            if (null == frecuency)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.ConfigureNode(
                RCConfigurationAction.Assing,
                Tools_AsynResponse,
                GearGet(ip, Tipo_Frecuencia.VHF, frecuency),
                false);

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);
        }
        /// <summary>
        /// 
        /// </summary>
        static void New_Test_RC_FrecuencyGet()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Get Frecunecy with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.FrecuencyGet(
                Tools_AsynResponse_String,
                GearGet(ip, Tipo_Frecuencia.VHF, null));

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);
        }
        /// <summary>
        /// 
        /// </summary>
        static void New_Test_RC_CheckGear()
        {
            _returnSemaphore = true;

            Console.Clear();
            Menu_Title("Test status on element with SNMP");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine(" NOTE: Press [X] any time and press enter to exit.");
            Console.WriteLine(" --------------------------------------------------------------------------------------------");

            IPAddress ip = IPAddressGet();
            if (null == ip)
            {
                return;
            }
            Int32? port = PortGet();
            if (null == port)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Sending Command . . . ");
            Console.WriteLine();

            IRemoteControl RC = new RCRohde4200(Convert.ToInt32(port));
            RC.CheckNode(
                Tools_AsynResponse,
                GearGet(ip, Tipo_Frecuencia.VHF, null));

            // Return.
            while (_returnSemaphore)
                Thread.Sleep(2000);

        }

        #endregion

    }
}

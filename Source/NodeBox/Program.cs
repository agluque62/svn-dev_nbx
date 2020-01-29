using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Windows.Forms;
using System.Diagnostics;

using System.Threading;
using System.Globalization;

using Utilities;



namespace U5ki.NodeBox
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{

			if ((args.Length == 1) && (args[0] == "-install"))
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
				{
					Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));
				}

				NodeBoxInstaller installer = new NodeBoxInstaller();
				Hashtable state = new Hashtable();

				try
				{
					installer.Context = new InstallContext();
					installer.Context.Parameters["assemblypath"] = Application.ExecutablePath;
					installer.Install(state);
				}
				catch (Exception ex)
				{
					installer.Rollback(state);
					Console.WriteLine("ERROR instalando servicio: {0}", ex.Message);
				}
			}
			else if ((args.Length == 1) && (args[0] == "-uninstall"))
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
				{
					Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));
				}

				NodeBoxInstaller installer = new NodeBoxInstaller();

				try
				{
					installer.Context = new InstallContext();
					installer.Uninstall(null);
				}
				catch (Exception ex)
				{
					Console.WriteLine("ERROR desinstalando servicio: {0}", ex.Message);
				}
			}
			else
			{
#if DEBUG
                U5ki.Infrastructure.Code.Globals.Test.IsRCNDFSimuladoRunning = Properties.Settings.Default.MNSimulating;
#endif
				NodeBoxSrv.Run(args);
			}
		}
	}
}
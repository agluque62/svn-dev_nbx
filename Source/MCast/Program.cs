using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Windows.Forms;
using System.Diagnostics;

using Utilities;
using NLog;

namespace U5ki.Mcast
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

				McastInstaller installer = new McastInstaller();
				Hashtable state = new Hashtable();

				try
				{
                    LogManager.GetCurrentClassLogger().Info("Instalando Servicio");
                    installer.Context = new InstallContext();
					installer.Context.Parameters["assemblypath"] = Application.ExecutablePath;
					installer.Install(state);
				}
				catch (Exception ex)
				{
					installer.Rollback(state);
					Console.WriteLine("ERROR instalando servicio: {0}", ex.Message);
                    LogManager.GetCurrentClassLogger().Error("ERROR instalando servicio: {0}", ex.Message);
				}
			}
			else if ((args.Length == 1) && (args[0] == "-uninstall"))
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
				{
					Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));
				}

				McastInstaller installer = new McastInstaller();

				try
				{
                    LogManager.GetCurrentClassLogger().Info("Desinstalando Servicio");
                    installer.Context = new InstallContext();
					installer.Uninstall(null);
				}
				catch (Exception ex)
				{
					Console.WriteLine("ERROR desinstalando servicio: {0}", ex.Message);
                    LogManager.GetCurrentClassLogger().Error("ERROR desinstalando servicio: {0}", ex.Message);
                }
			}
			else
			{
                //LogManager.GetCurrentClassLogger().Info(idioma.translate["Instalando Servicio"]);
				McastSrv.Run(args);
			}
		}
	}
}
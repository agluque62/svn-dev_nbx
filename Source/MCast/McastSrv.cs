#define _TESTING_STDOUT_

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using U5ki.Mcast.Properties;
using Utilities;
using NLog;

namespace U5ki.Mcast
{
    /// <summary>
    /// 
    /// </summary>
	public partial class McastSrv : ServiceBase
	{
        /// <summary>
        /// 
        /// </summary>
		public McastSrv()
		{
			InitializeComponent();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
		public static void Run(string[] args)
		{
			try
			{
				McastSrv server = new McastSrv();

				if ((args.Length == 1) && (args[0] == "-start"))
				{
					using (ServiceController sc = new ServiceController(server.ServiceName))
					{
						sc.Start();
					}
				}
				if ((args.Length == 1) && (args[0] == "-stop"))
				{
					using (ServiceController sc = new ServiceController(server.ServiceName))
					{
						sc.Stop();
					}
				}
				else if ((args.Length == 1) && (args[0] == "-console"))
				{
					server.ServiceMain();
				}
				else if (args.Length == 0)
				{
					ServiceBase.Run(server);
				}
			}
			catch (Exception e)
			{
				_Logger.Fatal("MCAST: " + "ERROR arrancando nodo", e);
			}
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
		protected override void OnStart(string[] args)
		{
			_ServiceMainTh = new Thread(ServiceMain);
            _ServiceMainTh.IsBackground = true;
			_ServiceMainTh.Start();

			base.OnStart(args);
		}
        /// <summary>
        /// 
        /// </summary>
		protected override void OnStop()
		{
			_EndEvent.Set();
			if (_ServiceMainTh != null)
			{
				_ServiceMainTh.Join();
			}

			base.OnStop();
		}

		#region Private Members

        /// <summary>
        /// 
        /// </summary>
		static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		ManualResetEvent _EndEvent = new ManualResetEvent(false);
        /// <summary>
        /// 
        /// </summary>
		string _HostId = Settings.Default.IdHost;
        /// <summary>
        /// 
        /// </summary>
		Process _Process;
        /// <summary>
        /// 
        /// </summary>
		Thread _ServiceMainTh;

        /// <summary>
        /// 
        /// </summary>
		void ServiceMain()
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            
            Environment.CurrentDirectory = Path.GetDirectoryName(Application.ExecutablePath);
			StartMcast();
			_Logger.Info("MCAST: "+"Nodo iniciado");

			while (!_EndEvent.WaitOne(2000, false))
			{
				if ((_Process != null) && _Process.HasExited)
				{
					_Process.Close();
					_Process = null;
				}

                if (_Process == null)
                {
                    StartMcast();
                }
                else
                {
                }
			}

			if (_Process != null)
			{
				if (!_Process.HasExited)
				{
					_Process.Kill();
				}

				_Process.Close();
				_Process = null;
			}
            _Logger.Info("MCAST: " + "Nodo detenido");
		}

        /// <summary>
        /// 
        /// </summary>
		private void StartMcast()
		{
			try
			{
				if (string.IsNullOrEmpty(_HostId))
				{
					_HostId = "PICT";
					List<string> ips = General.GetOperationalV4Ips();

					if (ips.Count > 0)
					{
						string ip = ips[ips.Count - 1];
						_HostId += ip.Substring(ip.LastIndexOf('.') + 1);
					}
				}

#if _TESTING_STDOUT_
                _Process = new Process();
                _Process.StartInfo.FileName = "spread.exe";
                _Process.StartInfo.Arguments = "-n " + _HostId + " -c spread.conf";
                _Process.StartInfo.CreateNoWindow = false;
                _Process.StartInfo.ErrorDialog = false;
                _Process.StartInfo.UseShellExecute = false;

                _Process.StartInfo.RedirectStandardOutput = true;
                //sti.RedirectStandardInput = true;
                //sti.RedirectStandardError = true;


#if _TESTING_STDOUT_PIOMNGR_
                _Process.Start();
                ProcessReadWriteUtils.ProcessIoManager mngr = new ProcessReadWriteUtils.ProcessIoManager(_Process);
                mngr.StderrTextRead += new ProcessReadWriteUtils.StringReadEventHandler((text) =>
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        _Logger.Info("Spread Line: {0}", text);
                    }
                });
                mngr.StartProcessOutputRead();
#else
                _Process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        _Logger.Info("SPREAD: {0}", e.Data);
                    }
                });
                _Process.Start();                
                _Process.BeginOutputReadLine();
#endif

#else
				_Process = new Process();
				_Process.StartInfo.FileName = "spread.exe";
				_Process.StartInfo.Arguments = "-n " + _HostId + " -c spread.conf";
                _Process.StartInfo.CreateNoWindow = false;
                _Process.StartInfo.ErrorDialog = false;
				_Process.StartInfo.UseShellExecute = false;
                _Process.Start();
#endif

                System.Threading.Thread.Sleep(500);
			}
			catch (Exception ex)
			{
				_Process.Close();
				_Process = null;

                _Logger.Error("MCAST: " + "Error arrancando spread.exe", (Exception)ex);
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				if (e.ExceptionObject is Exception)
				{
                    _Logger.Fatal("MCAST: " + "Excepcion no manejada", (Exception)e.ExceptionObject);
				}
			}
			catch (Exception) { }
		}

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _Process.Close();
            _Logger.Info("MCAST: Saliendo...");            
        }

		#endregion
	}
}

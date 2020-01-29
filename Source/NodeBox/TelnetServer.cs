using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
            /**
             * AGL 20120705. Añadir un 'commander' por red
             * */
using NLog;

namespace U5ki.NodeBox
{
    public delegate void SessionCloseHandler(TelnetSession ses);

    class TelnetServer
    {
        public event TelnetCommandHandler TelnetCommand;

        /// <summary>
        /// 
        /// </summary>
        public TelnetServer()
        {
            _listener = new TcpListener(IPAddress.Any, U5ki.NodeBox.Properties.Settings.Default.PuertoControlRemoto);
            _listenertTh = new Thread(new ThreadStart(ListenerThread));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            _listenertTh.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            foreach (TelnetSession ses in sesiones)
            {
                ses.Stop();
            }

            _listener.Server.Close();
            _listenertTh.Abort();
            _listenertTh.Join();
        }

        /// <summary>
        /// 
        /// </summary>
        static Logger _Logger = LogManager.GetCurrentClassLogger();

        private TcpListener _listener;
        private Thread _listenertTh;
        private List<TelnetSession> sesiones = new List<TelnetSession>();

        /// <summary>
        /// 
        /// </summary>
        private void ListenerThread()
        {
            _listener.Start();
            while (_listenertTh.IsAlive)
            {
                try
                {
                    TcpClient cliente = _listener.AcceptTcpClient();
                    TelnetSession sesion = new TelnetSession(cliente);

                    sesion.CloseSesion += OnCloseSesion;
                    sesion.TelnetCommand += TelnetCommand;

                    sesiones.Add(sesion);
                    sesion.Start();
                }
                catch (ThreadAbortException x)
                {
                    _Logger.DebugException("U5ki.NodeBox.TelnetServer.ListenerThread. ", x);
                    break;
                }
                catch (Exception x)
                {
                    _Logger.DebugException("U5ki.NodeBox.TelnetServer.ListenerThread. ", x);
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ses"></param>
        private void OnCloseSesion(TelnetSession ses)
        {
            sesiones.Remove(ses);
            ses.Close();
        }
    }

    public class TelnetSession
    {
        public event SessionCloseHandler CloseSesion;
        public event TelnetCommandHandler TelnetCommand;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sesion"></param>
        public TelnetSession(TcpClient sesion)
        {
            _session = sesion;
            _sessionTh = new Thread(new ThreadStart(ClientThread));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            _sessionTh.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            clientStream.Close();
            _sessionTh.Abort();
            _sessionTh.Join();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            _session.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        static Logger _Logger = LogManager.GetCurrentClassLogger();

        private TcpClient _session;
        private Thread _sessionTh;

        /// <summary>
        /// 
        /// </summary>
        private string prompt = "\r\n>> ";
        // private string notfound = "Comando Desconocido";
        private string strPres = "Cd40 NodeBox. Nucleo CC 2012\r\n\r\nEstado Actual:\r\n\r\n";


        /// <summary>
        /// 
        /// </summary>
        StringBuilder comando = new StringBuilder();
        string respuesta = "";
        NetworkStream clientStream;

        /// <summary>
        /// 
        /// </summary>
        private void ClientThread()
        {
            clientStream = _session.GetStream();
            byte[] input = new byte[1024];
            byte[] output = new byte[1024];
            int bytesRead = 0;

            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            clientStream.Write(encoding.GetBytes(strPres), 0, strPres.Length);
            TelnetCommand("std?", out respuesta);
            clientStream.Write(encoding.GetBytes(respuesta), 0, respuesta.Length);
            clientStream.Write(encoding.GetBytes(prompt), 0, prompt.Length);

            while (_sessionTh.IsAlive)
            {
                try
                {
                    bytesRead = clientStream.Read(input, 0, 1024);
                    if (bytesRead <= 0)
                        throw new Exception("leidos 0 caracteres");

                    for (int index=0; index<bytesRead; index++)
                    {
                        if (Automata((char )input[index]))
                        {
                            string cmd = comando.ToString();
                            switch (cmd)
                            {
                                case "exit":
                                    CloseSesion(this);
                                    return;
                                default:
                                    TelnetCommand(cmd, out respuesta);
                                    clientStream.Write(encoding.GetBytes(respuesta),0,respuesta.Length);
                                    break;
                            }

                            clientStream.Write(encoding.GetBytes(prompt), 0, prompt.Length);
                            comando.Remove(0, comando.Length);
                        }
                    }
                    
                }
                catch (ThreadAbortException x)
                {
                    _Logger.DebugException("U5ki.NodeBox.TelnetSesion.ClienteThread. ", x);
                    break;
                }
                catch (Exception x)
                {
                    _Logger.DebugException("U5ki.NodeBox.TelnetSesion.ClienteThread. ", x);
                    break;
                }
            }

            CloseSesion(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recibido"></param>
        /// <returns></returns>
        enum StdAut { Recibiendo, CrRecibido };
        private StdAut std = StdAut.Recibiendo;
        private bool Automata(char recibido)
        {
            switch (std)
            {
                case StdAut.Recibiendo:
                    if (recibido != '\r')
                        comando.Append(recibido);
                    else
                        std = StdAut.CrRecibido;
                    break;
                
                case StdAut.CrRecibido:
                    if (recibido == '\n')
                    {
                        std = StdAut.Recibiendo;
                        return true;
                    }
                    break;
            }
            return false;
        }

    }
}

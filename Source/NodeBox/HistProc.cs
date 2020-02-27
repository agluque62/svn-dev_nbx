using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;

// using NLog;

using Utilities;
using ProtoBuf;
using U5ki.Infrastructure;

namespace U5ki.NodeBox
{
    /// <summary>
    /// 
    /// </summary>
    public class nbxEvent
    {
        public String fh { get; set; }
        public String ser { get; set; }
        public String ev { get; set; }
        public String par { get; set; }
    }

    public class HistProc : BaseCode
    {
        static Queue<nbxEvent> last_inci = new Queue<nbxEvent>();
        public static List<nbxEvent> LastInci
        {
            get
            {
                lock (last_inci)
                {
                    return last_inci.OrderByDescending(e => e.fh).ToList();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public HistProc()
        {
            _proc = new Thread(new ThreadStart(ProcesoIncidencias)) { IsBackground = true };
            _proc.IsBackground = true;
#if IPC_DEFINED
            _hist = new Queue<NbxHist>();
#else
            // _listener = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1444));
            _listener = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));         // Asignacion automatica de puerto.
            U5kiIncidencias.HistProcPort = ((IPEndPoint)_listener.Client.LocalEndPoint).Port;
#endif        
            last_inci.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Inicializa()
        {
            if (_proc.IsAlive)
                Finaliza();

            _salir = false;
            _proc.Start();      
#if IPC_DEFINED
            _hist.Clear();
#endif

            Thread.Sleep(100);  // Para que entre el proceso...
        }

        /// <summary>
        /// 
        /// </summary>
        public void Finaliza()
        {
            if (_proc.IsAlive)
            {
                _salir = true;
#if IPC_DEFINED
                _hist.Clear();
#else
                _listener.Client.Close();
#endif
                _proc.Join(5000);
            }
        }

        /// <summary>
        /// Datos de Configuracion...
        /// </summary>
        string _oidbase = Properties.Settings.Default.HistBaseOid;
        string _iptrapserver = Properties.Settings.Default.HistServer;
        string _trapcomm = Properties.Settings.Default.HistCommunity;

        /// <summary>
        /// Datos de Estado
        /// </summary>
        bool _salir = false;
        Thread _proc = null;
#if IPC_DEFINED
        Queue<NbxHist> _hist = null;
#else
        UdpClient _listener = null;
#endif
        // private static Logger _Logger = LogManager.GetCurrentClassLogger();
#if IPC_DEFINED
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnHistReceived(object sender, SpreadDataMsg msg)
        {
            lock (_hist)
            {
                try
                {
                    MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                    NbxHist hist = Serializer.Deserialize<NbxHist>(ms);

                    _hist.Enqueue(hist);
                }
                catch (Exception x)
                {
                    _Logger.Error("ProcesoIncidencias.OnHistReceived", x);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ProcesoIncidencias_old()
        {
            try
            {
                InterProcessEvent recv = new InterProcessEvent("inci", OnHistReceived);
                _Logger.Info("Proceso Incidencias Iniciado...");

                while (!_salir)
                {
                    Thread.Sleep(100);
                    lock (_hist)
                    {
                        if (_hist.Count > 0)
                        {
                            try
                            {
                                NbxHist hist = _hist.Dequeue();

                                /** Enviar al servidor. */
                                string oidh = string.Format("{0}.{1}", _oidbase, (int)hist.id);
                                SnmpClient.TrapTo(_iptrapserver, _trapcomm, oidh, hist.descripcion);

                                /** LOG del Historico */
                                DateTime d = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) + new TimeSpan((long)hist.datetime);
                                LogTrace<HistProc>(string.Format("{0}: INCI {1}", d.ToString(), hist.descripcion));

                                // Almacenar la incidencia en la base de datos
                                // AddEvent(hist);
                            }
                            catch (Exception x)
                            {
                                _Logger.Error("Proceso Incidencias", x);
                            }
                        }
                    }
                }

                recv.Dispose();
            }
            catch (Exception x)
            {
                _Logger.Error("Proceso Incidencias", x);
            }
        }
#else
        private void ProcesoIncidencias()
        {                      
            while (!_salir)
            {
                try
                {
                    // IPEndPoint from = new IPEndPoint(IPAddress.Any, 1444);
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, U5kiIncidencias.HistProcPort);
                    Byte[] recibido = _listener.Receive(ref from);
                    SpreadDataMsg msg = new SpreadDataMsg("InterProcessEvent", 1, recibido, recibido.Count(), "InterProcessEvent");

                    MemoryStream ms = new MemoryStream(msg.Data, 0, msg.Length);
                    NbxHist hist = Serializer.Deserialize<NbxHist>(ms);

                    /** Enviar al servidor. */
                    string oidh = string.Format("{0}.{1}", _oidbase, (int)hist.id);
                    
                    // SnmpClient.TrapTo(_iptrapserver, _trapcomm, oidh, hist.descripcion);

                    SnmpClient.TrapFromTo(Properties.Settings.Default.IpPrincipal, _iptrapserver, _trapcomm, oidh, hist.descripcion);

                    /** LOG del Historico */
                    DateTime d = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) + new TimeSpan((long)hist.datetime);
                    LogTrace<HistProc>(string.Format("{0}: INCI {1}", d.ToString(), hist.descripcion));

                    /** Enviar a Ultimas Incidencias */
                    SetLastInci(d, hist);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    break;
                }
                catch (Exception /*x*/)
                {
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hist"></param>
        private void SetLastInci(DateTime dlog, NbxHist hist)
        {
            lock (last_inci)
            {
                /** Formato: IDINCI, IDHW, LOC, DESC */
                String[] formato = hist.descripcion.Split(',');
                String descripcion = formato[1] + (formato.Length > 3 ? (", " + formato[3]) : "");
                String service = formato[2].Substring(0, formato[2].IndexOf('['));
                
                //String descripcion = hist.descripcion.Substring(hist.descripcion.IndexOf(',') + 1);         // Quito el ID
                //String service = descripcion.Substring(0, descripcion.IndexOf('['));                        // Nombre del Servicio
                //descripcion = descripcion.Substring(descripcion.IndexOf(',') + 1);                          // Descripcion

                last_inci.Enqueue(new nbxEvent()
                {
                    fh = dlog.ToString("HH:mm:ss.fff"),
                    ev = his_ids.ContainsKey(hist.id) ? his_ids[hist.id] : hist.id.ToString(),
                    ser = service,
                    par = descripcion
                });

                if (last_inci.Count > 10)
                    last_inci.Dequeue();
            }
        }

        Dictionary<uint, String> his_ids = new Dictionary<uint, string>()
        {
            {(uint)U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR,"NBX. Error Generico"},
            {(uint)U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO,"NBX. Informacion"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ASIGNADO,"HF. Equipo Asignado"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_CONECTADO,"HF. Equipo Disponible"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_DESCONECTADO,"HF. Equipo Desconectado"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_ERROR,"HF. Error en Equipo"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_EQUIPO_LIBERADO,"HF. Liberado Equipo"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_ASIGNACION,"HF. Error en Asignacioin"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_DESASIGNACION,"HF. Error en Desasignacion"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_GENERAL,"HF. Error Generico"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_INTENTO_ASIGNACION_MULTIPLE,"HF. Error Asignacion Multiple"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_HF_ERROR_PREPARACIONSELCAL,"HF. Error Preparando SELCAL"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND,"MN. Operacion Manual"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_ERROR,"MN. Error en Operacion Manual"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_CONFIGURATION_ERROR,"MN. Error de Configuracion"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_NONPRIORITY_ONERROR,"MN. F-RX en ALARMA No Prioritaria"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONERROR,"MN. F-RX en ALARMA"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONMAIN,"MN. F-RX asignado PPAL"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONRSVA,"MN. F-RX asignado RSVA"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_NONPRIORITY_ONERROR,"MN. F-TX en ALARMA No Prioritaria"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONERROR,"MN. F-TX en ALARMA"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONMAIN,"MN. F-TX asignado PPAL"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONRSVA,"MN. F-TX asignado RSVA"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DISP,"MN. Equipo Disponible"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_FAIL,"MN. Equipo en Fallo"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ITF_ERROR,"MN. Fallo ITF Equipo"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR,"MN. Error Generico"},
            {(uint)U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT,"MN. Informacion"},
        };
#endif
    }
}

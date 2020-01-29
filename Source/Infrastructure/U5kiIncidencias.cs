using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.IO;

using ProtoBuf;
using Utilities;

namespace U5ki.Infrastructure
{
    public class U5kiIncidencias : BaseCode
    {

        #region Declarations

        public enum U5kiIncidencia
        {
            IGNORE = -1,

            U5KI_NBX_HF_EQUIPO_CONECTADO=1,
            U5KI_NBX_HF_EQUIPO_ERROR=2,
            U5KI_NBX_HF_EQUIPO_DESCONECTADO=3,
            U5KI_NBX_HF_EQUIPO_ASIGNADO=4,
            U5KI_NBX_HF_EQUIPO_LIBERADO=5,
            U5KI_NBX_HF_ERROR_GENERAL=6,

            U5KI_NBX_HF_ERROR_ASIGNACION=7,
            U5KI_NBX_HF_ERROR_DESASIGNACION=8,
            U5KI_NBX_HF_ERROR_INTENTO_ASIGNACION_MULTIPLE=9,
            U5KI_NBX_HF_ERROR_PREPARACIONSELCAL = 10,

            IGRL_U5KI_NBX_INFO = 50,
            IGRL_U5KI_NBX_ERROR = 51,

                        // 20160921. AGL. Nuevos Historicos para M+N (Segun Especificacion)
            //U5KI_NBX_NM_GEAR_ALLOCATE_ERROR = 3052,             // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_GEAR_ALLOCATE_OK = 3053,                // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_GEAR_DEALLOCATE_ERROR = 3054,           // AGL No Ulizado. Eliminar...
            //U5KI_NBX_NM_GEAR_DEALLOCATE_OK = 3055,              // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_GEAR_LOCAL_MODE_ON = 3056,              // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_GEAR_SOCKET_ERROR = 3057,               // Esto entre otros motivos que pueden darlo, es una conexion Interactiva.
            //                                                    // desde la aplicacion de Gestion.
            //                                                    // AGL Revisado Contenido 20160916
            U5KI_NBX_NM_GEAR_DISP = 3050,
            U5KI_NBX_NM_GEAR_FAIL = 3051,
            U5KI_NBX_NM_GEAR_ITF_ERROR = 3052,

            //U5KI_NBX_NM_FRECUENCY_ALLOCATE_ERROR = 3060,        // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_FRECUENCY_ALLOCATE_OK = 3061,           // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_FRECUENCY_DEALLOCATE_ERROR = 3062,      // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_FRECUENCY_DEALLOCATE_OK = 3063,         // AGL Revisado Contenido 20160916

            U5KI_NBX_NM_FRECUENCY_TX_ONMAIN = 3060,
            U5KI_NBX_NM_FRECUENCY_TX_ONRSVA = 3061,
            U5KI_NBX_NM_FRECUENCY_TX_ONERROR = 3062,
            U5KI_NBX_NM_FRECUENCY_TX_NONPRIORITY_ONERROR = 3063,
            U5KI_NBX_NM_FRECUENCY_RX_ONMAIN = 3064,
            U5KI_NBX_NM_FRECUENCY_RX_ONRSVA = 3065,
            U5KI_NBX_NM_FRECUENCY_RX_ONERROR = 3066,
            U5KI_NBX_NM_FRECUENCY_RX_NONPRIORITY_ONERROR = 3067,

            U5KI_NBX_NM_COMMAND_COMMAND = 3070,                   // AGL Revisado Contenido 20160916
            U5KI_NBX_NM_COMMAND_ERROR = 3071,
            //U5KI_NBX_NM_COMMAND_GEAR_ASSING = 3071,
            //U5KI_NBX_NM_COMMAND_GEAR_UNASSING = 3072,
            //U5KI_NBX_NM_COMMAND_GEAR_TOOGLE = 3073,
            //U5KI_NBX_NM_COMMAND_SERVICE_RESTART = 3074,         // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_COMMAND_SERVICE_TICK_CHANGED = 3075,    // AGL Revisado Contenido 20160916

            //U5KI_NBX_NM_SERVICE_START = 3080,                   // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_SERVICE_STOP = 3081,                    // AGL Revisado Contenido 20160916
            //U5KI_NBX_NM_SERVICE_NEW_CONFIGURATION = 3082,       // AGL Revisado Contenido 20160916

            U5KI_NBX_NM_GENERIC_EVENT = 3080,
            U5KI_NBX_NM_GENERIC_ERROR = 3081,                   // AGL Revisado Contenido 20160916
            U5KI_NBX_NM_CONFIGURATION_ERROR = 3082,             // AGL Revisado Contenido 20160916

            //U5KI_INFO_GENERIC = 3099,                         // AGL Desactivados 20160916
            //U5KI_ERROR_GENERIC = 30100,
        }

        // public static string ConnectionString = Properties.Settings.Default.ConnectionString;
        public static Int32 HistProcPort { get; set; }
#if IPC_DEFINED
        private static InterProcessEvent ipc = null;
#endif

        #endregion

        public static object locker = new object();
        public static int inci_counter = 0;

        public static void Inicializa(string name)
        {
#if IPC_DEFINED
            ipc = new InterProcessEvent(name);
#endif
        }

        public static void Dispose()
        {
#if IPC_DEFINED
            ipc.Dispose();
#endif
        }

        #region Generar Incidencia (Public)

        /// <summary>
        /// Genera una incidencia con el mensaje recibido.
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, string mensaje)
        {
            try
            {
                // Conseguir el tiempo.
                TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                // Construir el objeto a enviar.
                NbxHist hist = new NbxHist();
                hist.datetime = (ulong)span.Ticks;
                hist.id = (uint)tipoDeIncidencia;
                hist.descripcion = mensaje;

                //Thread thread = new Thread(new ParameterizedThreadStart(Send)) { IsBackground = true };
                //thread.Start(hist);
                Enqueue(hist);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR LOGGING] " + ex.Message);
                // Else: Log can NEVER stop the running app.
            }
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object[] parametros)
        {
            GeneraIncidencia(
                tipoDeIncidencia,
                String.Join(",", parametros));
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object parametro1)
        {
            if (null != parametro1)
                GeneraIncidencia(
                    tipoDeIncidencia, parametro1.ToString());
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object parametro1, object parametro2)
        {
            GeneraIncidencia(
                tipoDeIncidencia,
                String.Join(",", new object[] { parametro1, parametro2 } ));
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object parametro1, object parametro2, object parametro3)
        {
            GeneraIncidencia(
                tipoDeIncidencia,
                String.Join(",", new object[] { parametro1, parametro2, parametro3 }));
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object parametro1, object parametro2, object parametro3, object parametro4)
        {
            GeneraIncidencia(
                tipoDeIncidencia,
                String.Join(",", new object[] { parametro1, parametro2, parametro3, parametro4 }));
        }
        /// <summary>
        /// Genera una incidencia parseando los parametros recibidos como cadenas y separandolos con comas (',').
        /// </summary>
        public static void GeneraIncidencia(U5kiIncidencia tipoDeIncidencia, object parametro1, object parametro2, object parametro3, object parametro4, object parametro5)
        {
            GeneraIncidencia(
                tipoDeIncidencia,
                String.Join(",", new object[] { parametro1, parametro2, parametro3, parametro4, parametro5 }));
        }

        #endregion

        #region Handlers

        /// <summary>
        /// Funcion encargada de gestionar el envio de la incidencias.
        /// </summary>
        private static void Send(Object hist)
        {
            if (!(hist is NbxHist))
                throw new NotSupportedException("U5kiIncidencias.Send input object can only be NbxHist type.");

#if IPC_DEFINED
            if (null != ipc)
            {
                lock (locker)
                {
                    ipc.Send<NbxHist>((NbxHist)hist);
                    inci_counter++;
                }
            }
            else
            {
                // throw new NotSupportedException("Enviando Incidencias sin Inicializar el proceso de Intercambio...");
            }
#else
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, (NbxHist)hist);
            byte[] bdata = ms.ToArray();
            // (new UdpClient()).Send(bdata, bdata.Count(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1444));
            (new UdpClient()).Send(bdata, bdata.Count(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), U5kiIncidencias.HistProcPort));
#endif
        }

        private static Queue<NbxHist> historicos = new Queue<NbxHist>();
        private static Thread thHist = new Thread(thProc) { IsBackground = true };
        private static void thProc()
        {
            while (true)
            {
                lock (historicos)
                {
                    if (historicos.Count != 0)
                    {
                        NbxHist hist = historicos.Dequeue();
                        Send(hist);
                    }
                }
                Thread.Sleep(10);
            }
        }

        private static void Enqueue(NbxHist hist)
        {
            if (thHist.IsAlive == false)
                thHist.Start();

            lock (historicos)
            {
                historicos.Enqueue(hist);
            }
        }

        #endregion

    }
}

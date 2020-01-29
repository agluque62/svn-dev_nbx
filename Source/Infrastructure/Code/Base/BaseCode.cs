using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using U5ki.Infrastructure.Properties;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// Objecto inicial del arbol de objetos.
    /// </summary>
    public class BaseCode
    {
        /// <summary>
        /// 20171002. AGL. Para el idioma...
        /// </summary>
        public string idioma
        {
            get
            {
                return Environment.GetEnvironmentVariable("idioma") ?? "es";
            }
        }

        /** 20160905. AGL. Localizacion del Servicio. */
        public String ServiceSite
        {
            get
            {
                return System.Environment.MachineName;
            }
        }

        private static LogLevel _logLevel;
        private static LogLevel _logLevelLocal;

        public BaseCode()
        {
            _logLevel = LogLevel.FromString(Settings.Default.LogLevel);
            _logLevelLocal = LogLevel.FromString(Settings.Default.LogLevelLocal);
        }

        #region Logs

        #region Logs - Base

        /// <summary>
        /// Utiliza esta funcion para escribir en la consola.
        /// </summary>
        public void LogConsole<T>(LogLevel level, String message)
        {
            if (level == LogLevel.Fatal)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (level == LogLevel.Error)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (level == LogLevel.Warn)
                Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" [" + DateTime.Now + "][" + level + "] [" + typeof(T).Name.ToUpper() + "] " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        /// <summary>
        /// Utiliza esta funcion para escribir en el Fichero Log.
        /// </summary>
        private void LogLogger<T>(LogLevel level, String message)
        {
#if DEBUG1
            log_serialize.Log(typeof(T).Name, level, message);
#else
            Logger _logger = LogManager.GetLogger(typeof(T).Name);
            _logger.Log(level, message);
#endif
        }
        /// <summary>
        /// Utiliza esta función para realizar un log, y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        private void Log<T>(LogLevel level, String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            // Origen del Mensaje...
            String msgOrg = String.Format("{0}[{1}]", typeof(T).Name, ServiceSite);

            // Clear the string
            message = message.Replace("'", "").Replace("\"", "");
            // Generar el LOG Local...
            LogLogger<T>(level, (type != U5kiIncidencias.U5kiIncidencia.IGNORE ? "[" + ((Int32)type).ToString() + "] " : "") + msgOrg + ": " + message);

            // Generar el Histórico.
            // El formato será: type,idHw,Origen,mensaje..
            if (level > LogLevel.Debug && type != U5kiIncidencias.U5kiIncidencia.IGNORE && issueMessages != null)
            {                
                //String msgInci = String.Format("{0},{1}", (Int32)type, msgOrg);
                //if (issueMessages != null)
                //{
                //    foreach (string msg in issueMessages)
                //    {
                //        msgInci += ("," + msg.Replace(",",";"));
                //    }
                //}
                //else
                //{
                //    msgInci = String.Format("{0},{1},{2}", (Int32)type, msgOrg, message);
                //}
                String msgInci = String.Format("{0},{1},{2}", (Int32)type, issueMessages[0].ToString().Replace(",", ";"), msgOrg);
                for (int index = 1; index < issueMessages.Count(); index++)
                {
                    msgInci += ("," + issueMessages[index].ToString().Replace(",", ";"));
                }
                U5kiIncidencias.GeneraIncidencia(type, msgInci);
            }
        }
        /// <summary>
        /// Utiliza esta función para realizar un log, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        private void Log<T>(LogLevel level, String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(level, message, type, null);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        private void Log<T>(LogLevel level, String message)
        {
            Log<T>(level, message, U5kiIncidencias.U5kiIncidencia.IGNORE);
        }

        #endregion

        #region Trace

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo TRACE, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogTrace<T>(String message)
        {
            Log<T>(LogLevel.Trace, message, U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo TRACE, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogTrace<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Trace, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo TRACE y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogTrace<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Trace, message, type, issueMessages);
        }

        #endregion

        #region Debug

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo DEBUG, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogDebug<T>(String message)
        {
            Log<T>(LogLevel.Debug, message, U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo DEBUG, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogDebug<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Debug, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo DEBUG y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogDebug<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Debug, message, type, issueMessages);
        }

        #endregion

        #region Info

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo INFO, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogInfo<T>(String message)
        {
            Log<T>(LogLevel.Info, message, U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo INFO, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogInfo<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Info, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo INFO, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogInfo<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Info, message, type, issueMessages);
        }

        #endregion

        #region Warn

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo WARN, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogWarn<T>(String message)
        {
            Log<T>(LogLevel.Warn, message, U5kiIncidencias.U5kiIncidencia.IGNORE);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo WARN, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogWarn<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Warn, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo WARN, y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogWarn<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Warn, message, type, issueMessages);
        }

        #endregion

        #region Error

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogError<T>(Exception ex, String header = "")
        {
            if (!String.IsNullOrEmpty(header))
                header = "[" + header + "] ";
            Log<T>(LogLevel.Error, header + ex.Message/*, U5kiIncidencias.U5kiIncidencia.U5KI_ERROR_GENERIC*/);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogError<T>(String message)
        {
            Log<T>(LogLevel.Error, message/*, U5kiIncidencias.U5kiIncidencia.U5KI_ERROR_GENERIC*/);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogError<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Error, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogError<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Error, message, type, issueMessages);
        }

        #endregion

        #region Fatal

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogFatal<T>(Exception ex)
        {
            Log<T>(LogLevel.Error, ex.Message/*, U5kiIncidencias.U5kiIncidencia.U5KI_ERROR_GENERIC*/);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo FATAL, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogFatal<T>(String message)
        {
            Log<T>(LogLevel.Fatal, message/*, U5kiIncidencias.U5kiIncidencia.U5KI_ERROR_GENERIC*/);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo FATAL, y adicionalmente enviar una incidencia con el string del mensaje literalmente. 
        /// <para>El mensaje NO PUEDE contener comas(',') porque se utilizan como separador.</para>
        /// </summary>
        protected void LogFatal<T>(String message, U5kiIncidencias.U5kiIncidencia type)
        {
            Log<T>(LogLevel.Fatal, message, type);
        }
        /// <summary>
        /// Utiliza esta función para realizar un log de tipo FATAL, y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogFatal<T>(String message, U5kiIncidencias.U5kiIncidencia type, params Object[] issueMessages)
        {
            Log<T>(LogLevel.Fatal, message, type, issueMessages);
        }

        #endregion

        /// <summary>
        /// Utiliza esta función para realizar un log de tipo ERROR, y adicionalmente enviar una incidencia, con mensajes diferentes entre el del Log y el de la incidencia.
        /// La funcion coge cada uno de los parametros y los separa con comas para que el software en el destino los interprete.
        /// </summary>
        protected void LogException<T>(String message, Exception ex, bool bRegistroHistorico /*= true*/)
        {
            message += (" [EXCEPTION ERROR]: " + ex.Message);
            if (null != ex.InnerException)
                message += (" [INNER EXCEPTION ERROR]: " + ex.InnerException.Message);

            /** */
            if (bRegistroHistorico == true)
                Log<T>(LogLevel.Error, message, U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_ERROR, new object[] { message });
            else
                Log<T>(LogLevel.Error, message, U5kiIncidencias.U5kiIncidencia.IGNORE);

            /** */
            LogManager.GetLogger(typeof(T).Name).Error(message + ": ", ex);
        }

        #endregion

        IDictionary<String, Type> _lastExceptions = new Dictionary<String, Type>();
        protected void ExceptionManage<T>(String key, Exception ex, String message, bool bRegistroHistorico = true)
        {
            key = typeof(T).Name + "_" + key;
            if (_lastExceptions.ContainsKey(key)==false || _lastExceptions[key] != ex.GetType())
            {
                LogException<T>(message, ex, bRegistroHistorico);
                _lastExceptions[key] = ex.GetType();
            }
        }
        protected void ExceptionManageInit()
        {
            _lastExceptions.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        protected class ManagedSemaphore
        {
            public ManagedSemaphore(int initialCount, int maxCount, String id)
            {
                _semaphore = new Semaphore(initialCount, maxCount);
                _id = id;
            }
            public void SetName(String id)
            {
                _id = id + "_" + _id;
            }
            public void WaitOne()
            {
                if (_semaphore==null)
                    Throw("ManagedSemaphore no creado: " + Id);
                if (_semaphore.WaitOne(20000)==false)
                    Throw("Timeout en ManagedSemaphore: " + Id);

                owners[Id] = System.Threading.Thread.CurrentThread.Name;    // .ManagedThreadId;
            }
            public void Release()
            {
                if (_semaphore == null)
                    Throw("ManagedSemaphore no creado: " + Id);
                if (_semaphore.WaitOne(0)==true)
                    Throw("ManagedSemaphore. Release fuera de lugar: " + Id);
                _semaphore.Release();
                owners[Id] = "libre";
            }

            protected Semaphore _semaphore = null;
            protected void Throw(string msg)
            {
                Logger _logger = LogManager.GetLogger("ManagedSemaphore");
                _logger.Log(LogLevel.Error, msg);
#if DEBUG
                throw new Exception(msg);
#endif
            }
            protected string _id = null;
            protected string Id 
            {
                get
                {
                    //return (_id ?? "") + "_" + _semaphore.SafeWaitHandle.GetHashCode().ToString();
                    return _id ?? "";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            static protected Dictionary<String, String> owners = new Dictionary<String, String>();
        }
#if DEBUG
        /** Serializa los LOG para DEBUGGER */
        protected static LogSerialize log_serialize = new LogSerialize();
        protected class LogSerialize : IDisposable
        {
            Queue<Tuple<String, LogLevel, String>> cola = new Queue<Tuple<string,LogLevel,string>>();
            public void Log(String slogger, LogLevel level, String msg)
            {
                cola.Enqueue(new Tuple<string, LogLevel, string>(slogger, level, msg));
            }

            bool running = true;
            public LogSerialize()
            {
                Task.Factory.StartNew(() =>
                {
                    while (running)
                    {
                        if (cola.Count > 0)
                        {
                            Tuple<String, LogLevel, String> evento = cola.Dequeue();
                            try
                            {
                                Logger _logger = NLog.LogManager.GetLogger(evento.Item1);
                                _logger.Log(evento.Item2, evento.Item3);
                            }
                            catch(Exception x)
                            {
                            }
                        }
                        Thread.Sleep(10);
                    }
                });
            }

            public void Dispose()
            {
                running = false;
            }

        }
#endif

    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Net;
using U5ki.Infrastructure;
using NLog;

namespace U5ki.NodeBox.WebServer 
{   class WebAppServer : BaseCode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public delegate void wasRestCallBack(HttpListenerContext context, StringBuilder sb);

        #region Public

        public string DefaultUrl { get; set; }
        public string DefaultDir { get; set; }
        public bool HtmlEncode { get; set; }
        public bool Enable { get; set; }
        public string DisableCause { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public WebAppServer()
        {
            SetRequestRootDirectory();
            DefaultUrl = "/index.html";
            DefaultDir = "/appweb";
            HtmlEncode = true;
            Enable = true;
            DisableCause = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultDir"></param>
        /// <param name="defaultUrl"></param>
        public WebAppServer(string defaultDir, string defaultUrl, bool htmlEncode = true, bool enable=true)
        {
            SetRequestRootDirectory();
            DefaultUrl = defaultUrl;
            DefaultDir = defaultDir;
            HtmlEncode = htmlEncode;
            Enable = enable;
            DisableCause = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="cfg"></param>
        public void Start(int port, Dictionary<string, wasRestCallBack> cfg)
        {
            lock (locker)
            {
                if (_listener != null)
                    Stop();

                _cfgRest = cfg;
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://*:" + port.ToString() + "/");

                /** Configurar la Autentificacion */
                _listener.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous;
                _listener.AuthenticationSchemeSelectorDelegate = request =>
                {
                    /** Todas las operaciones No GET se consideran inseguras... Habra que autentificarse */
                    return request.HttpMethod == "GET" ? AuthenticationSchemes.Anonymous : AuthenticationSchemes.Basic;
                };

                _listener.Start();
                _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Stop()
        {
            lock (locker)
            {
                if (_listener != null)
                {
                    _listener.Close();
                    _listener = null;
                    _cfgRest = null;
                }
            }
        }

        #endregion

        #region Protected

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        void GetContextCallback(IAsyncResult result)
        {
            lock (locker)
            {
                //U5kGenericos.SetCurrentCulture();

                if (_listener == null || _listener.IsListening == false)
                    return;

                HttpListenerContext context = _listener.EndGetContext(result);
                logrequest(context);

                try
                {
                    if (Authenticated(context))
                    {
                        string url = context.Request.Url.LocalPath;
                        if (Enable)
                        {
                            if (url == "/") context.Response.Redirect(DefaultUrl);
                            else
                            {
                                wasRestCallBack cb = FindRest(url);
                                if (cb != null)
                                {
                                    StringBuilder sb = new System.Text.StringBuilder();
                                    cb(context, sb);
                                    context.Response.ContentType = FileContentType(".json");
                                    Render(Encode(sb.ToString()), context.Response);
                                }
                                else
                                {
                                    url = DefaultDir + url;
                                    if (url.Length > 1 && File.Exists(url.Substring(1)))
                                    {
                                        /** Es un fichero lo envio... */
                                        string file = url.Substring(1);
                                        string ext = Path.GetExtension(file).ToLowerInvariant();

                                        context.Response.ContentType = FileContentType(ext);
                                        ProcessFile(context.Response, file);
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = 404;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Render(Encode(DisableCause), context.Response);
                            // context.Response.StatusCode = 503;
                            // context.Response.Redirect("/noserver.html");
                            context.Response.ContentType = FileContentType(".html");
                            ProcessFile(context.Response, (DefaultDir + "/disabled.html").Substring(1), "{{cause}}", DisableCause);
                        }
                    }
                }
                catch (Exception x)
                {
                    LogException<WebAppServer>("", x, false);
                    context.Response.StatusCode = 500;
                    // Todo. Render(Encode(x.Message), context.Response);
                }
                finally
                {
                    context.Response.Close();
                    if (_listener != null && _listener.IsListening)
                        _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="file"></param>
        protected void ProcessFile(HttpListenerResponse response, string file, string tag="", string valor="")
        {
            if (tag != "")
            {
                string str = File.ReadAllText(file).Replace(tag, valor);
                byte[] content = Encoding.ASCII.GetBytes(str);
                response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                byte[] content = File.ReadAllBytes(file);
                response.OutputStream.Write(content, 0, content.Length);
            }
            response.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="res"></param>
        protected void Render(string msg, HttpListenerResponse res)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
            res.ContentLength64 = buffer.Length;

            using (System.IO.Stream outputStream = res.OutputStream)
            {
                outputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entrada"></param>
        /// <returns></returns>
        protected string Encode(string entrada)
        {
            if (HtmlEncode == true)
            {
                char[] chars = entrada.ToCharArray();
                StringBuilder result = new StringBuilder(entrada.Length + (int)(entrada.Length * 0.1));

                foreach (char c in chars)
                {
                    int value = Convert.ToInt32(c);
                    if (value > 127)
                        result.AppendFormat("&#{0};", value);
                    else
                        result.Append(c);
                }

                return result.ToString();
            }
            return entrada;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SetRequestRootDirectory()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string rootDirectory = Path.GetDirectoryName(exePath);
            Directory.SetCurrentDirectory(rootDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected wasRestCallBack FindRest(string url)
        {
            if (_cfgRest == null)
                return null;

            if (_cfgRest.ContainsKey(url))
                return _cfgRest[url];

            string[] urlComp = url.Split('/');
            foreach (KeyValuePair<string, wasRestCallBack> item in _cfgRest)
            {
                string[] keyComp = item.Key.Split('/');
                if (keyComp.Count() != urlComp.Count())
                    continue;

                bool encontrado = true;
                for (int index = 0; index < urlComp.Count(); index++)
                {
                    if (urlComp[index] != keyComp[index] && keyComp[index] != "*")
                        encontrado = false;
                }

                if (encontrado == true)
                    return item.Value;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        Dictionary<string, string> _filetypes = new Dictionary<string, string>()
        {
            {".css","text/css"},
            {".jpeg","image/jpg"},
            {".jpg","image/jpg"},
            {".htm","text/html"},
            {".html","text/html"},
            {".ico","image/ico"},
            {".js","text/json"},
            {".json","text/json"},
            {".txt","text/text"},
            {".bmp","image/bmp"}
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        private string FileContentType(string ext)
        {
            if (_filetypes.ContainsKey(ext))
                return _filetypes[ext];
            return "text/text";
        }

        #endregion

        #region Autenticacion
        bool MustAuthenticate = true;
        DateTime SessionExpiredAt = DateTime.Now;
        private bool Authenticated(HttpListenerContext context)
        {
            /** Es una peticion que el Selector ha determinado como 'segura' y no requiere autentificarse */
            if (context.User == null)
            {
                return true;
            }
            else
            {
                if (/*!MustAuthenticate*/ DateTime.Now < SessionExpiredAt)
                {
                    HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
                    if (AuthenticateUser?.Invoke(identity?.Name,identity?.Password)==true)
                    {
                        MustAuthenticate = true;
                        SessionExpiredAt = DateTime.Now;
                        return true;
                    }
                }

                MustAuthenticate = false;
                SessionExpiredAt = DateTime.Now + TimeSpan.FromMinutes(1);
                /** Operador no autentificado. Presenta peticion de Login / Password. */
                /** Para presentar la pantalla de peticion LOGIN / PASSWORD... */
                context.Response.StatusCode = 401;
                context.Response.AddHeader("WWW-Authenticate",
                    "Basic Realm=\"My WebDAV Server\""); // show login dialog
                byte[] message = new UTF8Encoding().GetBytes("Access denied");
                context.Response.ContentLength64 = message.Length;
                context.Response.OutputStream.Write(message, 0, message.Length);
                context.Response.Close();

                return false;
            }
        }
        protected Func<string, string, bool> AuthenticateUser = null;
        #endregion Autentificacion

        #region Testing
        private void logrequest(HttpListenerContext context)
        {
#if DEBUG
            if (context.Request.QueryString.Count > 0)
            {
                NLog.LogManager.GetLogger("Testing").Debug("URL: {0}", context.Request.Url.OriginalString);
                NLog.LogManager.GetLogger("Testing").Debug("Raw URL: {0}", context.Request.RawUrl);

                var array = (from key in context.Request.QueryString.AllKeys
                             from value in context.Request.QueryString.GetValues(key)
                             select string.Format("{0}={1}", key, value)).ToArray();

                NLog.LogManager.GetLogger("Testing").Debug("Query: {0}", String.Join("##",array));
            }
#endif
        }
        #endregion

        #region Private

        HttpListener _listener = null;
        Dictionary<string, wasRestCallBack> _cfgRest = null;
        Object locker = new Object();

        #endregion
    }
}


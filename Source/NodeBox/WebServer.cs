using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Threading;
using System.Net;

using NLog;

namespace U5ki.NodeBox
{
    public enum CmdSupervision {cmdSrvStd, cmdCfgIdAct, cmdCfgLst, cmdCfgDel, cmdCfgAct, cmdCfgSav, cmdHFGet, cmdHFStd, cmdHFLib }

    /// <summary>
    /// 
    /// </summary>
    static public class NbxWebServer  
    {

        static Logger _Logger = LogManager.GetCurrentClassLogger();
        static int nRequest=0;

        static public event WebSrvCommandHandler WebSrvCommand;
        static HttpListener _listener = null;
        
        /** */
        static NodeBoxConfig LocalConfig = new NodeBoxConfig("", Properties.Settings.Default.Idioma);
        // static HMIConfig HmiConfig = new HMIConfig(Properties.Settings.Default.PathFicheroConfigHMI, Properties.Settings.Default.Idioma);
        static HMIConfig HmiConfig = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        static public void Start(int port)
        {
            CultureInfo culture = new CultureInfo(Properties.Settings.Default.Idioma);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + port.ToString() + "/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
        }

        /// <summary>
        /// 
        /// </summary>
        static public void Dispose()
        {
            // _listener.Stop();
            _listener.Close();
            _listener = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        static public void GetContextCallback(IAsyncResult result)
        {
            if (_listener==null || _listener.IsListening == false)
                return;

            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                /** Para el Thread del Callback */
                CultureInfo culture = new CultureInfo(Properties.Settings.Default.Idioma);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                NbxWebServer.nRequest += 1;

                if (request.HttpMethod == "POST")
                {
                    _Logger.Debug("WebServer POST {0}", context.Request.Url.LocalPath);
                }
                else if (request.HttpMethod == "GET")
                {
                    _Logger.Debug("WebServer GET Local Path: '{0}'. Query: '{1}'", context.Request.Url.LocalPath, context.Request.Url.Query);

                    /** Miro si esta solicitando un fichero */
                    string url = context.Request.Url.LocalPath;
                    if (url.Length > 1 && File.Exists(url.Substring(1)))
                    {
                        /** Es un fichero lo envio... */
                        string file = url.Substring(1);

                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".css")
                        {
                            response.ContentType = "text/css";
                            ProcessFile(file, response);
                        }
                        else if (ext == ".jpeg" || ext == ".jpg")
                        {
                            response.ContentType = "image/jpg";
                            ProcessFile(file, response);
                        }
                        else if (ext == ".htm" || ext == ".html")
                        {
                            response.ContentType = "text/html";
                            ProcessFile(file, response);
                        }
                        else if (ext == ".ico")
                        {
                            response.ContentType = "image/ico";
                            ProcessFile(file, response);
                        }
                        else if (ext == ".txt")
                        {
                            response.ContentType = "text/text";
                            ProcessFile(file, response);
                        }
                        else
                        {
                            response.StatusCode = 400;
                            Render(sb.ToString(), response);
                        }

                    }
                    else
                    {
                        /** Es una Peticion. La Proceso */
                        ProcesaPeticion(request, response, sb);
                        Render(Encode(sb.ToString()), response);    // Render(sb.ToString(), response);
                    }
                }
            }
            catch (Exception x)
            {
                response.StatusCode = 500;
                Render(Encode(x.Message), response);

                _Logger.Info("Excepcion en WebServer. GetContextCallback: " + x.Message);
                _Logger.Trace("Excepcion en WebServer. GetContextCallback", x);
                
            }
            finally
            {
                _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <param name="sb"></param>
        private static void ProcesaPeticion(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            /** Apertura de la Página de Respuesta */
            sb.Append(String.Format("<!DOCTYPE html><html><head>"));
            // sb.Append(String.Format("<!DOCTYPE html><html lang='{0}><head>", Properties.Settings.Default.Idioma));
            sb.Append("<link rel='stylesheet' type='text/css' href='U5KIStyle.css'>");
            sb.Append("</head><body>");

            /** Cabecera de Páginas */
            Encabezado(sb);

            switch (request.Url.LocalPath)
            {
                case "/":
                    if (CheckSecurity(request) == true)
                        PaginaInicial(sb);
                    else
                        response.Redirect("./login");
                    break;

                case "/cfg":
                    if (CheckSecurity(request) == true)
                        PaginaConfiguraciones(sb, request);
                    else
                        response.Redirect("./login");
                    break;

                case "/cfgloc":
                    if (CheckSecurity(request) == true)
                        ConfiguracionLocal(sb, request);
                    else
                        response.Redirect("./login");
                    break;

                case "/cfghmi":
                    /**
                    if (CheckSecurity(request) == true)
                        ConfiguracionHmi(sb, request);
                    else
                        response.Redirect("./login");
                     * */
                    sb.Append(String.Format("<p><strong>{0}</strong></p>", Resources.StringResource.Pagina_no_existente));
                    break;

                case "/ghf":
                    if (CheckSecurity(request) == true)
                        EstadoHF(sb, request);
                    else
                        response.Redirect("./login");
                    break;

                case "/rad":
                case "mlog":
                    sb.Append(String.Format("<p><strong>!!! {0} !!!</strong></p>", Resources.StringResource.Pagina_en_Construccion));
                    break;

                case "/login":
                    PideLogin(sb);
                    break;

                case "/form":
                    string[] comandos = request.Url.Query.Split('=');
                    if (comandos.Length > 1  && comandos[0] == "?pwd")
                    {
                        if (comandos[1] == "nodebox")
                        {
                            Cookie login = new Cookie("nbx-login", "login");
                            login.Expires = DateTime.Now.AddSeconds(Properties.Settings.Default.MinutosSesion * 60);

                            response.SetCookie(login);
                            response.Redirect("./");
                        }
                        else
                            response.Redirect("./login");
                    }
                    else if (comandos.Length > 1 && comandos[0]=="?cfgname")
                    {
                        string cfgname = comandos[1];
                        EjecutaComando(CmdSupervision.cmdCfgSav, cfgname, response);
                        response.Redirect("./cfg");
                    }

                    break;

                case "/cfgset-hmi-inf":
                    HmiConfig.PropertieSet(HMIConfig.cSec_Infrastructure, request.Url.Query);
                    response.Redirect("/cfghmi");
                    break;

                case "/cfgset-hmi-cd40":
                    HmiConfig.PropertieSet(HMIConfig.cSec_Cd40Model, request.Url.Query);
                    response.Redirect("/cfghmi");
                    break;

                case "/cfgset-hmi-model":
                    HmiConfig.PropertieSet(HMIConfig.cSec_HmiModel, request.Url.Query);
                    response.Redirect("/cfghmi");
                    break;

                case "/cfgset-hmi-pre":
                    HmiConfig.PropertieSet(HMIConfig.cSec_Premodel, request.Url.Query);
                    response.Redirect("/cfghmi");
                    break;

                case "/cfgset-nbx":
                    LocalConfig.PropertieSet(NodeBoxConfig.cNbxSectionName, request.Url.Query);
                    response.Redirect("/cfgloc");
                    break;

                case "/cfgset-rad":
                    LocalConfig.PropertieSet(NodeBoxConfig.cRadioSectionName, request.Url.Query);
                    response.Redirect("/cfgloc");
                    break;

                case "/cfgset-cfg":
                    LocalConfig.PropertieSet(NodeBoxConfig.cConfigSectionName, request.Url.Query);
                    response.Redirect("/cfgloc");
                    break;

                case "/cfgset-tif":
                    LocalConfig.PropertieSet(NodeBoxConfig.cTifxSectionName, request.Url.Query);
                    response.Redirect("/cfgloc");
                    break;

                case "/cmd":       // Comandos
                    string[] cmd = request.Url.Query.Split('=');
                    if (cmd[0] == "?delc") // Borrar una Preconfiguracion...
                    {
                        EjecutaComando(CmdSupervision.cmdCfgDel, cmd[1], response);
                        response.Redirect("./cfg");
                    }
                    else if (cmd[0] == "?actc") // Activar una Preconfiguracion...
                    {
                        EjecutaComando(CmdSupervision.cmdCfgAct, cmd[1], response);
                        response.Redirect("./cfg");
                    }
                    else if (cmd[0] == "?hflib")
                    {
                        EjecutaComando(CmdSupervision.cmdHFLib, cmd[1], response);
                        response.Redirect("./ghf");
                    }
                    break;

                default:
                    sb.Append(String.Format("<p><strong>{0}</strong></p>", Resources.StringResource.Pagina_no_existente));
                    break;
            }

            /** Pie de Pagina */
            Pie(sb);

            /** Cierre de la Página de Respuesta */
            sb.Append("</body></html>");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static bool CheckSecurity(HttpListenerRequest request)
        {
            /** Chequeo la session */
            CookieCollection cookies = request.Cookies;
            foreach (Cookie ck in cookies)
            {
                if (ck.Name == "nbx-login" && ck.Value == "login")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rq"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static string GetValorCookie(HttpListenerRequest rq, string id)
        {
            foreach (Cookie ck in rq.Cookies)
            {
                if (ck.Name == id)
                    return ck.Value;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        private static void ProcessFile(string file, HttpListenerResponse response)
        {
            byte [] content = File.ReadAllBytes(file);
            response.OutputStream.Write(content, 0, content.Length);
            response.Close();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="res"></param>
        private static void Render(string msg, HttpListenerResponse res)
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
        /// <param name="cmd"></param>
        /// <param name="par"></param>
        /// <param name="res"></param>
        private static void EjecutaComando(CmdSupervision cmd, string par, HttpListenerResponse res)
        {
            string err = "";
            Cookie ck = new Cookie("msg-cfg","");
            if (WebSrvCommand(cmd, par, ref err) == true)
            {
                ck.Value = Resources.StringResource.Comando_ejecutado;
            }
            else
            {
                ck.Value = Resources.StringResource.Error_Ejecucion_orden + err;
            }
            ck.Expires = DateTime.Now.AddSeconds(10);
            res.SetCookie(ck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void Encabezado(StringBuilder sb)
        {
            sb.Append("<table border='0' width='100%'>");
            sb.Append("<tr>");
            sb.Append(String.Format("<td align='left' valign='top'><img src='nucleo-df-new.jpg' alt='DF Nucleo' height='64' width='64'></td>" +
                                    "<td align='center' valign='top'><h2>{1}</h2></td>" +
                                    "<td align='right' valign='top'><p>{0}</p></td>",
                                    DateTime.Now.ToShortDateString(), Resources.StringResource.Ulises_Nodebox));

            sb.Append("</tr>");
            sb.Append("</table>");
            sb.Append("<p></p>");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void Pie(StringBuilder sb)
        {
            sb.Append(String.Format("<div id='pie'>{0}</div>", Resources.StringResource.DFNucleo_Copyright));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void Menu(StringBuilder sb)
        {
            // sb.Append("<menu label='ul' type='toolbar'>");
            // sb.Append("<div id='vmenu'>");

            sb.Append("<ul id=menu width='100 %'>");
            sb.Append(String.Format("<li><a href='./'>{0}</a></li>", Resources.StringResource.Menu_Inicio));
            sb.Append(String.Format("<li><a href='./cfgloc'>{0}</a></li>", Resources.StringResource.Menu_Nodebox_Config));
            /*sb.Append(String.Format("<li><a href='./cfghmi'>{0}</a></li>", Resources.StringResource.Menu_HMI_Config));*/
            sb.Append(String.Format("<li><a href='./cfg'>{0}</a></li>", Resources.StringResource.Menu_Preconfiguraciones));
            sb.Append(String.Format("<li><a href='./ghf'>{0}</a></li>", Resources.StringResource.Menu_EquiposHF));
            sb.Append(String.Format("<li><a href='./logs/logfile.txt' target='_blank'>{0}</a></li>", Resources.StringResource.Menu_FicheroLOG));
            sb.Append("</ul>");

            // sb.Append("</div>");
            // sb.Append("</menu>");
        }

        /// <summary>
        /// 
        /// </summary>
        private static void PaginaInicial(StringBuilder sb)
        {
            string err = "";
           List<string> _cfg = new List<string>();
           List<string> _rad = new List<string>();
           List<string> _tif = new List<string>();

           // Obtengo la Informacion.
           if (WebSrvCommand(CmdSupervision.cmdSrvStd, "Cd40RdService", ref err, _rad)==false)
               return;
           if (WebSrvCommand(CmdSupervision.cmdSrvStd, "Cd40ConfigService", ref err, _cfg) == false)
               return;
           if (WebSrvCommand(CmdSupervision.cmdSrvStd, "Cd40TifxService", ref err, _tif) == false)
               return;

           Menu(sb);

           // Formateo la aplicacion en HTML.
           sb.Append("<table width='80%' border='0' align='center'><tr>");
           sb.Append("<td valign='top'>");

           sb.Append(String.Format("<h3 align='center' >{0}.</h3>", Resources.StringResource.PAG01_Estado_Servicios));

           sb.Append("<table align='center' width='70%' border='0'>");
           sb.Append(String.Format("<tr><th></th><th align='left'>{0}</th><th align='left'>{1}</th><th align='left'>{2}</th></tr>", 
               Resources.StringResource.PAG01_Nombre, Resources.StringResource.PAG01_Estado, Resources.StringResource.PAG01_Modo));
    
           sb.Append("<tr>");
           sb.Append(String.Format("<td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>",
                String.Format("<a href='./cfg'>{0}.</a>", Resources.StringResource.PAG01_Servicio_Configuracion), _cfg[0], _cfg[1], _cfg[2]));
           sb.Append("</tr>");
           sb.Append("<tr>");
           sb.Append(String.Format("<td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>",
                String.Format("<a href='./ghf'>{0}.</a>", Resources.StringResource.PAG01_Servicio_Radio), _rad[0], _rad[1], _rad[2]));
           sb.Append("</tr>");
           sb.Append("<tr>");
           sb.Append(String.Format("<td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>",
                String.Format("<a href='./'>{0}.</a>", Resources.StringResource.PAG01_Servicio_Interfaces), _tif[0], _tif[1], _tif[2]));
           sb.Append("</tr>");
           sb.Append("</table>");

           sb.Append("</td></tr>");
           sb.Append("</table>");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void PaginaConfiguraciones(StringBuilder sb, HttpListenerRequest rq)
        {
            List<string> configs = new List<string>();
            List<string> config_activa = new List<string>(); 
            string err = "";

            /** Obtengo datos a presentar */
            if (WebSrvCommand(CmdSupervision.cmdCfgLst, "", ref err, configs) == false)
                return;
            if (WebSrvCommand(CmdSupervision.cmdCfgIdAct, "", ref err, config_activa) == false)
                return;

            /** Formateo la aplicacion en HTML. */
            Menu(sb);
            sb.Append(String.Format("<h1 align='center'>{0}</h1>", Resources.StringResource.PAG02_Gestion_Configuraciones));

            sb.Append("<table width='80%' border='0' align='center'><tr>");

            sb.Append("<td width='20%' valign='top'>");
            sb.Append(String.Format("<h3 align='center'>{0}</h3>", Resources.StringResource.PAG02_Config_Activa));
            sb.Append(String.Format("<h4 align='right'>{0}</h4>",config_activa[0]));
            sb.Append("<form action='/form' method='GET'>");
            sb.Append(String.Format("<p>{0}</p>", Resources.StringResource.PAG02_Salvar_Como));
            sb.Append("<input type='text' name='cfgname'>");
            sb.Append(String.Format("<input type='submit' value='{0}'>", Resources.StringResource.PAG02_Guardar));
            sb.Append("</form>");
            sb.Append("</td>");

            sb.Append("<td valign='top'>");
            sb.Append(String.Format("<h3 align='center'>{0}.</h3>", Resources.StringResource.PAG02_Lista_Configuraciones));

            if (configs.Count > 1)
            {
                sb.Append("<table align='center' width='80%' border='0'>");

                for (int i = 0; i < configs.Count; i++)
                {
                    sb.Append("<tr>");
                    sb.Append(String.Format("<td>{0}</td>" +
                        "<td><a href='./cmd?delc={0}'>{1}</a></td>" +
                        "<td><a href='./cmd?actc={0}'>{2}</a></td>", configs[i], Resources.StringResource.PAG02_Borrar, Resources.StringResource.PAG02_Activar));
                    sb.Append("</tr>");
                }

                sb.Append("</table>");

                sb.Append("</td></tr>");
                sb.Append("</table>");
                sb.Append(String.Format("<p align='center'>{0}</p>", GetValorCookie(rq, "msg-cfg")));
            }
            else
                sb.Append(configs.Count == 0 ? Resources.StringResource.PAG02_Error : configs[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void PideLogin(StringBuilder sb)
        {
            sb.Append(String.Format("<h3 align = 'center'>{0}</h3>", Resources.StringResource.LOGIN_Identificacion_Seguridad));
            sb.Append(String.Format("<form align = 'center' name='input' action='/form' method='get'>{0}: " +
                      "<input type='Password' name='pwd'>" + 
                      "<input type='submit' value='{1}'>" +
                      "</form>", Resources.StringResource.LOGIN_Clave_Acceso, Resources.StringResource.LOGIN_Envia));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void ConfiguracionLocal(StringBuilder sb, HttpListenerRequest rq)
        {
            /** Formateo la aplicacion en HTML. */
            Menu(sb);
            sb.Append(String.Format("<h1 align='center'>{0}</h1>", Resources.StringResource.PAG03_Configuracion_Local));
            sb.Append("<table width='80%' border='0' align='center'><tr><td>");         
            sb.Append("<ul class='a'>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG03_Nodebox));
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cNbxSectionName))
            {
                sb.Append(String.Format("<li><form action='/cfgset-nbx' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                            prop.Key, prop.Value, Resources.StringResource.PAG03_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG04_Infraestructura));
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cInfraSectionName))
            {
                sb.Append(String.Format("<li><form action='/cfgset-rad' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG03_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG03_Radio));
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cRadioSectionName))
            {
                sb.Append(String.Format("<li><form action='/cfgset-rad' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG03_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG03_Configuracion));
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cConfigSectionName))
            {
                sb.Append(String.Format("<li><form action='/cfgset-cfg' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG03_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG03_Interfaces));
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cTifxSectionName))
            {
                sb.Append(String.Format("<li><form action='/cfgset-tif' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG03_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            sb.Append("</ul>");
            sb.Append("</td></tr></table>");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        private static void ConfiguracionHmi(StringBuilder sb, HttpListenerRequest rq)
        {
            // HmiConfig.Reload(Properties.Settings.Default.PathFicheroConfigHMI, Properties.Settings.Default.Idioma);

            /** Formateo la aplicacion en HTML. */
            Menu(sb);
            sb.Append(String.Format("<h1 align='center'>{0}</h1>", Resources.StringResource.PAG04_Configuracion_HMI));
            sb.Append("<table width='80%' border='0' align='center'><tr><td>");
            sb.Append("<ul class='a'>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG04_Infraestructura));
            foreach (KeyValuePair<string, string> prop in HmiConfig.SectionProperties(HMIConfig.cSec_Infrastructure))
            {
                sb.Append(String.Format("<li><form action='/cfgset-hmi-inf' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>", 
                    prop.Key, prop.Value, Resources.StringResource.PAG04_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG04_ModeloCD40));
            foreach (KeyValuePair<string, string> prop in HmiConfig.SectionProperties(HMIConfig.cSec_Cd40Model))
            {
                sb.Append(String.Format("<li><form action='/cfgset-hmi-cd40' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG04_Cambiar));     // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG04_ModeloHMI));
            foreach (KeyValuePair<string, string> prop in HmiConfig.SectionProperties(HMIConfig.cSec_HmiModel))
            {
                sb.Append(String.Format("<li><form action='/cfgset-hmi-model' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>",
                    prop.Key, prop.Value, Resources.StringResource.PAG04_Cambiar)); // todo...
            }
            sb.Append("	</ul></li>");

            /** */
            sb.Append(String.Format("<li><h3>{0}</h3><ul class='b'>", Resources.StringResource.PAG04_Presentacion));
            foreach (KeyValuePair<string, string> prop in HmiConfig.SectionProperties(HMIConfig.cSec_Premodel))
            {
                sb.Append(String.Format("<li><form action='/cfgset-hmi-pre' method='get'>{0}<input type='text' name='{0}' value='{1}'></input><input type='submit' value='{2}'></input></form></li>", 
                    prop.Key, prop.Value, Resources.StringResource.PAG04_Cambiar));     // todo...
            }
            sb.Append("	</ul></li>");

            sb.Append("</ul>");
            sb.Append("</td></tr></table>");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="rq"></param>
        private static void EstadoHF(StringBuilder sb, HttpListenerRequest rq)
        {
            /** Obtener los Datos */
            List<string> equipos = new List<string>();
            string err = "";
            if (WebSrvCommand(CmdSupervision.cmdHFGet , "", ref err, equipos) == false)
                return;

            /** Formateo la aplicacion en HTML. */
            Menu(sb);

            sb.Append(String.Format("<h1 align='center'>{0}</h1>", Resources.StringResource.PAG05_EstadoEquiposHF));
            sb.Append(String.Format("<table width='80%' border='0' align='center'>" + 
                "<tr><td><h3 align='center'>{0}</h3></td></tr>" +
                "<tr><td align='center'>", Resources.StringResource.PAG05_ListaEquiposHF));

            foreach (string equipo in equipos)
            {
                /** Obtengo Datos del Equipo */
                List<string> estados = new List<string>();
                if (WebSrvCommand(CmdSupervision.cmdHFStd, equipo, ref err, estados) == true)
                {
                    sb.Append(String.Format("<ul class='a'><h4>{1}: {0}</h4><ul class='b'><li><table border='0'>", equipo, Resources.StringResource.PAG05_Equipo));
                    sb.Append(String.Format("<tr><td class='par'>URI:</td><td class='val'>{0}</td></tr>",estados[0]));           // URI
                    sb.Append(String.Format("<tr><td class='par'>RCS-OID:</td><td class='val'>{0}</td></tr>",estados[1]));       // IP+OID
                    sb.Append(String.Format("<tr><td class='par'>{2}:</td><td class='val'>{0}</td><td><a href='./cmd?hflib={1}'>{3}</a></td></tr>",estados[2],equipo,
                            Resources.StringResource.PAG05_Estado, Resources.StringResource.PAG05_Liberar));
                    sb.Append(String.Format("<tr><td class='par'>{1}:</td><td class='val'>{0}</td></tr>",estados[3], Resources.StringResource.PAG05_Usuario));
                    sb.Append(String.Format("<tr><td class='par'>{1}:</td><td class='val'>{0}</td></tr>",estados[4],Resources.StringResource.PAG05_Frecuencia));
                    sb.Append("</table></li></ul></ul>");
                }
            }

            sb.Append("</td></tr></table>");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entrada"></param>
        /// <returns></returns>
        private static string Encode(string entrada)
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

    }

}

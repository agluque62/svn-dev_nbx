//#define _TASK_VERSION_
#define _Authentication_
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Diagnostics;

using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// using NLog;
using U5ki.Infrastructure;
using Translate;


namespace U5ki.NodeBox
{
   
    /// <summary>
    ///     { "cfg": 2, "rad": 2, "ifx": 2, "pbx": 0, "cfg_activa": "...", "nbx_version": "...." }
    /// </summary>
    //class stdGlobal
    //{
    //    public string cfg_activa { get; set; }
    //    public string nbx_version { get; set; }
    //    public int cfg { get; set; }
    //    public int rad { get; set; }
    //    public int ifx { get; set; }
    //    public int pbx { get; set; }
    //    public string mn { get; set; }
    //}

    /// <summary>
    /// { "fecha": "10/01/2015 08:00", "nombre": "Configuracion 1" }
    /// </summary>
    class pcfData
    {
        public string fecha { get; set; }
        public string nombre { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    class margenData
    {
        public int max { get; set; }
        public int min { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    class lparData
    {
        public string nombre { get; set; }
        public string mostrar { get; set; }
        public int tipo { get; set; }
        public List<string> opciones { get; set; }
        public int validar { get; set; }
        public margenData margenes { get; set; }
        public string valor { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    class lparGroup
    {
        public string name { get; set; }
        public List<lparData> par { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    class nbxLocalConfig
    {
        public lparGroup pgn { get; set; }
        public lparGroup pif { get; set; }
        public lparGroup prd { get; set; }
        public lparGroup pcf { get; set; }
        public lparGroup pit { get; set; }
        public lparGroup ppx { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public nbxLocalConfig()
        {
            pgn = new lparGroup();
            pif = new lparGroup();
            prd = new lparGroup();
            pcf = new lparGroup();
            pit = new lparGroup();
            ppx = new lparGroup();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadFromFile1() 
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            NodeBoxConfig LocalConfig = new NodeBoxConfig(Path.GetDirectoryName(exePath), CTranslate.Idioma);

            pgn = new lparGroup();
            pgn.name = "Parametros Generales";
            pgn.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cNbxSectionName))
            {
                pgn.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            pgn.par = pgn.par.OrderBy(o => o.mostrar).ToList();

            pif = new lparGroup();
            pif.name = "Parametros de Infraestructura";
            pif.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cInfraSectionName))
            {
                pif.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            pif.par = pif.par.OrderBy(o => o.mostrar).ToList();

            prd = new lparGroup();
            prd.name = "Parametros de Servicio Radio";
            prd.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cRadioSectionName))
            {
                prd.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            prd.par = prd.par.OrderBy(o => o.mostrar).ToList();

            pcf = new lparGroup();
            pcf.name = "Parametros de Servicio Configuracion";
            pcf.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cConfigSectionName))
            {
                pcf.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            pcf.par = pcf.par.OrderBy(o => o.mostrar).ToList();

            pit = new lparGroup();
            pit.name = "Parametros de Servicio Interfaces";
            pit.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cTifxSectionName))
            {
                pit.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            pit.par = pit.par.OrderBy(o => o.mostrar).ToList();

            ppx = new lparGroup();
            ppx.name = "Parametros de Servicio de PABX";
            ppx.par = new List<lparData>();
            foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(NodeBoxConfig.cPabxSectionName))
            {
                ppx.par.Add(new lparData()
                {
                    nombre = prop.Key,
                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                    tipo = 0,
                    validar = 0,
                    margenes = new margenData() { max = 0, min = 0 },
                    opciones = new List<string>() { "", "" },
                    valor = prop.Value
                });
            }
            ppx.par = ppx.par.OrderBy(o => o.mostrar).ToList();
        }
        /// <summary>
        /// Obsoleta...
        /// </summary>
        public void LoadFromFile_obsoleta() 
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            NodeBoxConfig LocalConfig = new NodeBoxConfig(Path.GetDirectoryName(exePath), CTranslate.Idioma);

            List<Tuple<string, string, lparGroup>> grupos = new List<Tuple<string, string, lparGroup>>() {
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cNbxSectionName, "Parametros Generales", pgn),
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cInfraSectionName, "Parametros de Infraestructura", pif),
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cRadioSectionName, "Parametros de Servicio Radio", prd),
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cConfigSectionName, "Parametros de Servicio Configuracion", pcf),
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cTifxSectionName, "Parametros de Servicio Interfaces", pit),
                new Tuple<string, string, lparGroup> (NodeBoxConfig.cPabxSectionName, "Parametros de Servicio de PABX", ppx)
            };

            try
            {
                foreach (Tuple<string, string, lparGroup> sec in grupos)
                {
                    sec.Item3.name = sec.Item2;
                    sec.Item3.par = new List<lparData>();
                    foreach (KeyValuePair<string, string> prop in LocalConfig.SectionProperties(sec.Item1))
                    {
                        try
                        {
                            if (Properties.Settings.Default.FiltroSettings.Contains(prop.Key) == false)
                            {
                                sec.Item3.par.Add(new lparData()
                                {
                                    nombre = prop.Key,
                                    mostrar = LocalConfig.GetStringDisplay(prop.Key),
                                    tipo = 0,
                                    validar = 0,
                                    margenes = new margenData() { max = 0, min = 0 },
                                    opciones = new List<string>() { "", "" },
                                    valor = prop.Value
                                });
                            }
                        }
                        catch (Exception x)
                        {
                            throw x;
                        }
                    }
                    sec.Item3.par = sec.Item3.par.OrderBy(o => o.mostrar).ToList();
                }
            }
            catch (Exception x)
            {
                throw x;
            }

        }
        
        /// <summary>
        /// Obsoleta
        /// </summary>
        public void SaveToFile_obsoleta()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            NodeBoxConfig LocalConfig = new NodeBoxConfig(Path.GetDirectoryName(exePath), CTranslate.Idioma);

            List<Tuple<string, lparGroup>> grupos = new List<Tuple<string, lparGroup>>() {
                new Tuple<string, lparGroup> (NodeBoxConfig.cNbxSectionName, pgn),
                new Tuple<string, lparGroup> (NodeBoxConfig.cInfraSectionName, pif),
                new Tuple<string, lparGroup> (NodeBoxConfig.cRadioSectionName, prd),
                new Tuple<string, lparGroup> (NodeBoxConfig.cConfigSectionName, pcf),
                new Tuple<string, lparGroup> (NodeBoxConfig.cTifxSectionName, pit),
                new Tuple<string, lparGroup> (NodeBoxConfig.cPabxSectionName, ppx)
            };

            //
            foreach (Tuple<string, lparGroup> grp in grupos)
            {
                foreach (lparData par in grp.Item2.par)
                {
                    LocalConfig.PropertySet(grp.Item1, par.nombre, par.valor);
                }
            }

            LocalConfig.Save();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class nbxLocalConfigExt
    {
        public string fichero { get; set; }
        public nbxLocalConfigExt(bool readFile = false) {
            if (readFile)
            {
                fichero = File.ReadAllText(fileName, Encoding.UTF8);
            }
        }
        public void save(){
            File.WriteAllText(fileName, fichero, Encoding.UTF8);
        }
        protected string fileName
        {
            get
            {
                return NodeBoxSrv.ServerType == ServicesServerTypes.Radio ? @"U5ki.RadioService.exe.config" :
                    NodeBoxSrv.ServerType == ServicesServerTypes.Phone ? @"U5ki.PhoneService.exe.config" : @"U5ki.NodeBox.exe.config";
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class tifxInfo
    {
        public class tifxResInfo
        {
            public string id { get; set; }
            public int ver { get; set; }
            public int std { get; set; }
            public int prio { get; set; }
            public int tp { get; set; }
            /** */
            public string dep { get; set; }
        }
        public string id { get; set; }
        public int tp { get; set; }
        public string ip { get; set; }
        public int ver { get; set; }
        public List<tifxResInfo> res { get; set; }

        public tifxInfo(object gwInfo)
        {
#if DEBUG
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            Type GwInfo = gwInfo.GetType();

            id = GwInfo.GetField("GwId").GetValue(gwInfo) as string;
            ip = GwInfo.GetField("GwIp").GetValue(gwInfo) as string;
            tp = (int)(uint)GwInfo.GetField("Type").GetValue(gwInfo);
            ver = (int )(uint )GwInfo.GetField("Version").GetValue(gwInfo);

            res = new List<tifxResInfo>();
            Array resources = GwInfo.GetField("Resources").GetValue(gwInfo) as Array;
            Type RsInfo = resources.GetType().GetElementType();
            foreach (Object resource in resources)
            {
                string r_id = RsInfo.GetField("RsId").GetValue(resource) as string;
                int r_ver = (int)(uint)RsInfo.GetField("Version").GetValue(resource);
                int r_std = (int)(uint)RsInfo.GetField("State").GetValue(resource);
                int r_prio = (int)(uint)RsInfo.GetField("Priority").GetValue(resource);
                int r_tp = (int)(uint)RsInfo.GetField("Type").GetValue(resource);

                string r_dep = RsInfo.GetField("GwIp").GetValue(resource) as string;

                
                res.Add(new tifxResInfo()
                {
                    id = r_id,
                    ver = r_ver,
                    std = r_std,
                    prio = r_prio,
                    tp = r_tp,
                    dep = r_dep
                });
            }
#if DEBUG
            sw.Stop();
            Console.WriteLine("tifxInfo. {0} ms, {1} ticks. {2} s.", 
                sw.ElapsedMilliseconds, sw.ElapsedTicks,(float)((float)sw.ElapsedTicks/(float)Stopwatch.Frequency));
#endif
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class equipoMNAsigna
    {
        // { equ: item.equ, cmd: 1, frec: frec };
        public string equ { get; set; }
        public string frec { get; set; }
        public int cmd { get; set; }        // 0: Desasignar. 1: Asignar.
    }

    /// <summary>
    /// 
    /// </summary>
    class MNConfiguraTick
    {
        // { equ: item.equ, cmd: 1, frec: frec };
        public String miliseconds { get; set; }
    }

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

    /// <summary>
    /// 
    /// </summary>
    class nbxEventList
    {
        public List<nbxEvent> li { get; set; }
        public int hash { get; set; }
        public string lang { get; set; }

        public nbxEventList()
        {
            li = HistProc.LastInci;
            hash = li.GetHashCode();
            lang = Translate.CTranslate.Idioma;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class nbxPublicData
    {
        //public stdGlobal std = new stdGlobal();
        public List<pcfData> pcf = new List<pcfData>();
        public nbxLocalConfig lcf = new nbxLocalConfig();
        public List<GlobalTypes.radioSessionData> ses = new List<GlobalTypes.radioSessionData>();
        public List<GlobalTypes.equipoMNData> mnd = new List<GlobalTypes.equipoMNData>();
        public List<tifxInfo> tifxs = new List<tifxInfo>();
    }

    /// <summary>
    /// 
    /// </summary>
    class nbxResData
    {
        public string res { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NbxWebServer : BaseCode
    {
        public event WebSrvCommandHandler WebSrvCommand;

        /** Peticiones REST */
        const string rest_url_inci = "inci";
        const string rest_url_std = "std";
        const string rest_url_preconf = "preconf";
        /** Obsoleto */
        const string rest_url_local_config = "lconfig";

        const string rest_url_local_config_ext = "lconfig-ext";
        const string rest_url_radio_sessions = "rdsessions";
        const string rest_url_radio_gestormn = "gestormn";
        const string rest_url_radio_gestormn_habilita = "gestormn/enable";
        const string rest_url_radio_gestormn_asigna = "gestormn/assign";
        const string rest_url_tlf_tifxinfo = "tifxinfo";
        const string rest_url_versiones = "versiones";
        const string rest_url_hf = "rdhf";
        const string rest_url_unomasuno = "rd11";
        const string rest_url_presence = "ps";
        const string rest_url_radio_debug = "rdservice";

        /// <summary>
        /// 
        /// </summary>
        // Logger _Logger = LogManager.GetCurrentClassLogger();
        HttpListener _listener = null;
        nbxPublicData _rtData = null;

        /// <summary>
        /// 
        /// </summary>
        public NbxWebServer()
        {
            SetRequestRootDirectory();
        }

        public NbxWebServer(string workingDirectory)
        {
            Directory.SetCurrentDirectory(workingDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port/*, nbxPublicData rtData*/)
        {
            // TODO. Para el idioma...
            //CultureInfo culture = new CultureInfo(Properties.Settings.Default.Idioma);
            //Thread.CurrentThread.CurrentCulture = culture;
            //Thread.CurrentThread.CurrentUICulture = culture;

            SetRequestRootDirectory();

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + port.ToString() + "/");
#if _Authentication_
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous;
            _listener.AuthenticationSchemeSelectorDelegate = request =>
            {
                /** Todas las operaciones No GET se consideran inseguras... Habra que autentificarse */
                return request.HttpMethod=="GET" ? AuthenticationSchemes.Anonymous : AuthenticationSchemes.Basic;
            };
#endif
            _listener.Start();

            _rtData = new nbxPublicData();

#if _TASK_VERSION_
            Task.Factory.StartNew(() =>
            {

               
            });
#else
            _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
#endif
            
            /** 20180312. Obsoleto */
            // _rtData.lcf.LoadFromFile();
        }

        /// <summary>
        /// 
        /// </summary>
        protected object _listenerLocker = new object();
        public void Dispose()
        {
            lock (_listenerLocker)
            {
                _listener.Close();
                _listener = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        void GetContextCallback(IAsyncResult result)
        {
            lock (_listenerLocker)
            {
                if (_listener == null || _listener.IsListening == false)
                    return;

                HttpListenerContext context;
                HttpListenerRequest request;
                HttpListenerResponse response;

                /** Al cerrar el servicio puede dar una excepcion aqui */
                try
                {
                    context = _listener.EndGetContext(result);
#if _Authentication_
                    if (Authenticated(context) == false)
                    {
                        _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                        return;
                    }
#endif
                    request = context.Request;
                    response = context.Response;
                }
                catch (HttpListenerException x1)
                {
                    LogException<NbxWebServer>("GetContextCallback", x1, false);
                    return;
                }

                /** */
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    if (request.HttpMethod == "POST")
                    {
                        ProcesaPeticionPost(request, response, sb);
                        Render(Encode(sb.ToString()), response);
                    }
                    else if (request.HttpMethod == "PUT")
                    {
                        ProcesaPeticionPut(request, response, sb);
                        Render(Encode(sb.ToString()), response);
                    }
                    else if (request.HttpMethod == "DELETE")
                    {
                        ProcesaPeticionDelete(request, response, sb);
                        Render(Encode(sb.ToString()), response);
                    }
                    else if (request.HttpMethod == "GET")
                    {
                        /** Miro si esta solicitando un fichero de la instalacion */
                        if (context.Request.Url.LocalPath.Length > 1 && File.Exists(context.Request.Url.LocalPath.Substring(1)))
                        {
                            response.ContentType = "text/text";
                            ProcessFile(context.Request.Url.LocalPath.Substring(1), response);
                        }
                        else
                        {
                            string url = "/appweb" + context.Request.Url.LocalPath;
                            /** Miro si esta solicitando un fichero de la web */
                            if (url.Length > 1 && File.Exists(url.Substring(1)))
                            {
                                /** Es un fichero lo envio... */
                                string file = url.Substring(1);
                                string ext = Path.GetExtension(file).ToLowerInvariant();

                                response.ContentType = FileContentType(ext);
                                ProcessFile(file, response);
                            }
                            else
                            {
                                /** Es una Peticion. La Proceso */
                                ProcesaPeticionGet(request, response, sb);
                                Render(Encode(sb.ToString()), response);    // Render(sb.ToString(), response);
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    response.StatusCode = 500;
                    try
                    {
                        Render(Encode(x.Message), response);
                    }
                    catch (InvalidOperationException exc)
                    {
                        LogException<NbxWebServer>("GetContextCallback", exc, false);
                    }
                    finally
                    {
                        LogException<NbxWebServer>("GetContextCallback", x, false);
                    }
                }
                finally
                {
                    if (_listener != null && _listener.IsListening)
                        _listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        private void ProcessFile(string file, HttpListenerResponse response)
        {
            byte[] content = File.ReadAllBytes(file);
            response.OutputStream.Write(content, 0, content.Length);
            response.Close();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <param name="sb"></param>
        private void ProcesaPeticionGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            LogTrace<NbxWebServer>(String.Format("GET {0}", request.Url.LocalPath));

            if (request.Url.LocalPath == "/")
            {
                response.Redirect("/index.html");
            }
            else
            {
                string[] par = request.Url.LocalPath.Split('/');
                if (par.Count() > 1)
                {
                    switch (par[1])
                    {
                        case rest_url_inci:
                            processListaEventos(request, response, sb);
                            break;
                        case rest_url_std:
                            processEstadoGeneral(request, response, sb);
                            break;
                        case rest_url_preconf:
                            processPreconfiguracionesGet(request, response, sb);
                            break;
                        case rest_url_local_config:
                            processLocalConfigGet(request, response, sb);
                            break;
                        case rest_url_local_config_ext:
                            processLocalConfigExtGet(request, response, sb);
                            break;
                        case rest_url_radio_sessions:
                            processRadioSessionsGet(request, response, sb);
                            break;
                        case rest_url_radio_gestormn:
                            processGestorMNDataGet(request, response, sb);
                            break;
                        case rest_url_tlf_tifxinfo:
                            processTifxDataGet(request, response, sb);
                            break;
                        case rest_url_versiones:
                            processVersionesGet(request, response, sb);
                            break;
                        case rest_url_hf:
                            processTxHFGet(request, response, sb);
                            break;
                        /** 20200224. Datos 1+1*/
                        case rest_url_unomasuno:
                            processRdUnoMasUnoGet(request, response, sb);
                            break;

                        case rest_url_presence:
                            processPresenceDataGet(request, response, sb);
                            break;

                        case rest_url_radio_debug:
                            var data = par.Length > 2 ? par[2] : null;
                            processRdServiceDataGet(request, response, sb, data);
                            break;

                        default:
                            sb.Append(webappError(0));
                            response.StatusCode = 404;          // No Implementado...
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void ProcesaPeticionPost(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            LogTrace<NbxWebServer>(String.Format("POST {0}", request.Url.LocalPath));

            if (request.Url.LocalPath.StartsWith("/" + rest_url_preconf))
            {
                processPreconfiguracionesPost(request, response, sb);
            }
            else if (request.Url.LocalPath.StartsWith("/" + rest_url_local_config_ext))
            {
                processLocalConfigExtPost(request, response, sb);
            }
            else if (request.Url.LocalPath.StartsWith("/" + rest_url_local_config))
            {
                processLocalConfigPost(request, response, sb);
            }
            else if (request.Url.LocalPath.StartsWith("/" + rest_url_radio_gestormn_habilita))
            {
                processGestorMNEnable(request, response, sb);
            }
            else if (request.Url.LocalPath.StartsWith("/" + rest_url_radio_gestormn_asigna))
            {
                processGestorMNAsigna(request, response, sb);
            }
            else if (request.Url.LocalPath.StartsWith("/" + rest_url_hf))
            {
                processTxHfRelease(request, response, sb);
            }
            /** 20200224. Mando Uno Mas Uno*/
            else if (request.Url.LocalPath.StartsWith("/"+ rest_url_unomasuno))
            {
                processRdUnoMasUnoActivate(request, response, sb);
            }
            else
            {
                sb.Append(webappError(0));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void ProcesaPeticionPut(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            LogTrace<NbxWebServer>(String.Format("PUT {0}", request.Url.LocalPath));

            if (request.Url.LocalPath.StartsWith("/" + rest_url_preconf))
            {
                processPreconfiguracionesPut(request, response, sb);
            }
            else
            {
                sb.Append(webappError(0));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void ProcesaPeticionDelete(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            
            LogTrace<NbxWebServer>(String.Format("DELETE {0}", request.Url.LocalPath));

            if (request.Url.LocalPath.StartsWith("/" + rest_url_preconf))
            {
                processPreconfiguracionesDelete(request, response, sb);
            }
            if (request.Url.LocalPath.StartsWith("/" + rest_url_radio_gestormn))
            {
                processGestorMNDelete(request, response, sb);
            }
            else
            {
                sb.Append(webappError(0));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="res"></param>
        private void Render(string msg, HttpListenerResponse res)
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
        private string Encode(string entrada)
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

        /// <summary>
        /// 
        /// </summary>
        private void SetRequestRootDirectory()
        {
            // Configurable.

            //string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            //string rootDirectory = Path.Combine(Path.GetDirectoryName(exePath), "appweb");
            //Directory.SetCurrentDirectory(rootDirectory);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processListaEventos(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(new nbxEventList());
            sb.Append(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processEstadoGeneral(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            object data = WebSrvCommand(CmdSupervision.cmdSrvStd, null);
            response.ContentType = "application/json";
            sb.Append(JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processPreconfiguracionesGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            GetListaPreconfiguraciones();
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(_rtData.pcf);
            sb.Append(data);
        }
        /// <summary>
        /// Activar Preconfiguracion
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processPreconfiguracionesPost(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                string strData = reader.ReadToEnd();
                pcfData cfg = JsonConvert.DeserializeObject<pcfData>(strData);
                nbxResData resultado = new nbxResData() { res = "processPreconfiguracionesPost. No se ha podido efectuar la operacion. WebSrvCommand == null" };
                if (WebSrvCommand != null)
                {
                    LogDebug<NbxWebServer>(String.Format("Activando Preconfiguracion : ({0})", cfg.nombre));
                    resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdCfgAct, cfg);
                    /** TODO. Genera Historico */
                }
                sb.Append(JsonConvert.SerializeObject(resultado));
            }
        }

        /// <summary>
        /// Guardar Configuracion activa como ...
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processPreconfiguracionesPut(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                pcfData cfg = JsonConvert.DeserializeObject<pcfData>(reader.ReadToEnd());
                nbxResData resultado = new nbxResData() { res = "processPreconfiguracionesPut. No se ha podido efectuar la operacion. WebSrvCommand == null" };
                if (WebSrvCommand != null)
                {
                    LogDebug<NbxWebServer>(String.Format("Guardando Configuracion Activa como ({0})", cfg.nombre));
                    resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdCfgSav, cfg);
                    /** TODO. Genera Historico */
                }
                sb.Append(JsonConvert.SerializeObject(resultado));
            }
        }

        /// <summary>
        /// Borrar preconfiguracion.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processPreconfiguracionesDelete(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            string cfg_name = Path.GetFileName(request.Url.LocalPath);
            nbxResData resultado = new nbxResData() { res = "processPreconfiguracionesDelete. No se ha podido efectuar la operacion. WebSrvCommand == null" };
            if (WebSrvCommand != null)
            {
                LogDebug<NbxWebServer>(String.Format("Borrando Preconfiguracion {0}", cfg_name));
                resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdCfgDel, cfg_name);
                /** TODO. Genera Historico */
            }
            // sb.Append(JsonConvert.SerializeObject(resultado));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processLocalConfigGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(_rtData.lcf);
            sb.Append(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processLocalConfigExtGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(new nbxLocalConfigExt(true));
            sb.Append(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processLocalConfigPost(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            string text;
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                text = reader.ReadToEnd();
                _rtData.lcf = JsonConvert.DeserializeObject<nbxLocalConfig>(text);
                /** 20180312. Obsoleto */
                // _rtData.lcf.SaveToFile();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processLocalConfigExtPost(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                nbxLocalConfigExt lcf = JsonConvert.DeserializeObject<nbxLocalConfigExt>(reader.ReadToEnd());
                //text = reader.ReadToEnd();
                //_rtData.lcf = JsonConvert.DeserializeObject<nbxLocalConfig>(text);
                //_rtData.lcf.SaveToFile();
                lcf.save();
                sb.Append("{\"res\": \"OK\"}");
                /** TODO. Genera Historico */
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processRadioSessionsGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            GetListaSesiones();
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(_rtData.ses);
            sb.Append(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processGestorMNDataGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            GetListaEquiposMN();
            response.ContentType = "application/json";
            string data = JsonConvert.SerializeObject(_rtData.mnd);
            sb.Append(data);
        }

        /// <summary>
        /// Reset del Servicio
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processGestorMNDelete(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            nbxResData resultado = new nbxResData() { res = "processGestorMNDelete. No se ha podido efectuar la operacion. WebSrvCommand == null" };
            // Reset del Servicio M/N...
            if (WebSrvCommand != null)
            {
                resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdRdMNReset, null);
                /** TODO. Genera Historico */
            }
            else
            {
                LogWarn<NbxWebServer>(String.Format("Reseteando Servicio M+N: {0}", resultado.res),
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR, "WebAppServer", String.Format("Reseteando Servicio M+N: {0}", resultado.res));
            }
        }

        /// <summary>
        /// Habilitar / Deshabilitar Equipo...
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processGestorMNEnable(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                GlobalTypes.equipoMNData equ = JsonConvert.DeserializeObject<GlobalTypes.equipoMNData>(reader.ReadToEnd());
                nbxResData resultado = new nbxResData() { res = "processGestorMNEnable. No se ha podido efectuar la operacion. WebSrvCommand == null" };
                if (WebSrvCommand != null)
                {
                    resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdRdMNEnable, equ);
                    /** TODO. Genera Historico */
                }
                else
                {
                    LogWarn<NbxWebServer>(String.Format("Cambiando el estado de habilitacion en equipo ({0}): {1}", equ.equ, resultado.res),
                         U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR, "WebAppServer", String.Format("Cambiando el estado de habilitacion en equipo ({0}): {1}", equ.equ, resultado.res));
                }
            }
        }

        /// <summary>
        /// Asignar / Desasignar Frecuencia en Equipo...
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processGestorMNAsigna(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                equipoMNAsigna equ = JsonConvert.DeserializeObject<equipoMNAsigna>(reader.ReadToEnd());
                nbxResData resultado = new nbxResData() { res = "processGestorMNAsigna. No se ha podido efectuar la operacion. WebSrvCommand == null" };
                if (WebSrvCommand != null)
                {
                    resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdRdMNAsigna, equ);
                    /** TODO. Genera Historico */
                }
                else
                {
                    LogWarn<NbxWebServer>(String.Format("Asignando el equipo ({0}): {1}", equ.equ, resultado.res),
                         U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, equ.equ, String.Format("Asignando : {0}", resultado.res));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processTxHfRelease(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            response.ContentType = "application/json";
            using (var reader = new StreamReader(request.InputStream,
                                                 request.ContentEncoding))
            {
                GlobalTypes.txHF tx = JsonConvert.DeserializeObject<GlobalTypes.txHF>(reader.ReadToEnd());
                nbxResData resultado = new nbxResData() { res = "processTxHfRelease. No se ha podido efectuar la operacion. WebSrvCommand == null" };
                if (WebSrvCommand != null)
                {
                    resultado = (nbxResData)WebSrvCommand(CmdSupervision.cmdHFLib, tx.id);
                    /** TODO. Genera Historico */
                }
                else
                {
                    LogWarn<NbxWebServer>(String.Format("Tx HF ({0}: {1} Liberando Equipo)", tx.id, resultado.res),
                         U5kiIncidencias.U5kiIncidencia.IGRL_U5KI_NBX_INFO, tx.id);
                }
                response.ContentType = "application/json";
                sb.Append(JsonConvert.SerializeObject(resultado));
            }
        }
        /** 20200224. Mando 1+1 */
        void processRdUnoMasUnoActivate(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="sb"></param>
        private void processTifxDataGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            if (WebSrvCommand != null)
            {
                //// Obtener los datos Reales.
                //_rtData.tifxs = (List<tifxInfo>)WebSrvCommand(CmdSupervision.cmdTlfInfoGet, null);
                //string data = JsonConvert.SerializeObject(_rtData.tifxs);
                //sb.Append(data);
                //response.ContentType = "application/json";
                string info = (string)WebSrvCommand(CmdSupervision.cmdTlfInfoGet, null);
                response.ContentType = "application/json";
                sb.Append(info);
            }
        }

        private void processVersionesGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            if (WebSrvCommand != null)
            {
                sb.Append((new Utilities.VersionDetails("versiones.json")).ToString());
                response.ContentType = "application/json";
            }
        }

        private void processTxHFGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            if (WebSrvCommand != null)
            {
                List<GlobalTypes.txHF> txhf = (List<GlobalTypes.txHF>)WebSrvCommand(CmdSupervision.cmdHFGet, null);
                response.ContentType = "application/json";
                sb.Append(JsonConvert.SerializeObject(txhf));
            }
        }

        private void processRdUnoMasUnoGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            if (WebSrvCommand != null)
            {
                List<GlobalTypes.txHF> txhf = (List<GlobalTypes.txHF>)WebSrvCommand(CmdSupervision.cmdRdUnoMasUnoData, null);
                response.ContentType = "application/json";
                sb.Append(JsonConvert.SerializeObject(txhf));
            }
        }

        private void processPresenceDataGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb)
        {
            if (WebSrvCommand != null)
            {
                string info = (string)WebSrvCommand(CmdSupervision.cmdPresenceInfoGet, null);
                response.ContentType = "application/json";
                sb.Append(info);
            }
        }

        private void processRdServiceDataGet(HttpListenerRequest request, HttpListenerResponse response, StringBuilder sb, object data = null)
        {
            if (WebSrvCommand != null)
            {
                string info = (string)WebSrvCommand(CmdSupervision.cmdRdServiceDebugInfo, data);
                response.ContentType = "application/json";
                sb.Append(info);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nError"></param>
        /// <returns></returns>
        private string webappError(int nError)
        {
            return string.Format("{{\"localError\": {0}}}", nError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //private void GetEstadoGeneral()
        //{
        //    if (WebSrvCommand != null)
        //    {
        //        _rtData.std = (stdGlobal)WebSrvCommand(CmdSupervision.cmdSrvStd, null);
        //        _rtData.std.nbx_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //        // _rtData.std = JsonConvert.DeserializeObject<stdGlobal>(File.ReadAllText(@"./appweb/simulate/std.json"));
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        private void GetListaPreconfiguraciones()
        {
            if (WebSrvCommand != null)
            {
                _rtData.pcf = (List<pcfData>)WebSrvCommand(CmdSupervision.cmdCfgLst, null);
                // _rtData.pcf = JsonConvert.DeserializeObject<List<pcfData>>(File.ReadAllText(@"./appweb/simulate/preconf.json"));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void GetListaSesiones()
        {
            if (WebSrvCommand != null)
            {
                // Obtener los datos Reales.
                // _rtData.ses = (List<radioSessionData>)WebSrvCommand(CmdSupervision.cmdRdSessions, null);
                _rtData.ses = ((List<GlobalTypes.radioSessionData>)WebSrvCommand(CmdSupervision.cmdRdSessions, null)).OrderBy(o => o.std).ThenBy(o=>o.frec).ToList();
                //_rtData.ses.Clear();
                //_rtData.ses = JsonConvert.DeserializeObject<List<radioSessionData>>(File.ReadAllText(@"./appweb/simulate/rdsessions.json"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void GetListaEquiposMN()
        {
            if (WebSrvCommand != null)
            {
                // Obtener los datos Reales...
                _rtData.mnd = (List<GlobalTypes.equipoMNData>)WebSrvCommand(CmdSupervision.cmdRdMNEquip, null);
                _rtData.mnd = _rtData.mnd.OrderBy(e => e.tip).ToList();
                // _rtData.mnd = JsonConvert.DeserializeObject<List<equipoMNData>>(File.ReadAllText(@"./appweb/simulate/gestormn.json"));
            }
        }

#if _Authentication_
        private bool _Operation = false;
        private bool Authenticated(HttpListenerContext context)
        {
            /** Es una peticion que el Selector ha determinado como 'segura' y no requiere autentificarse */
            if (context.User == null)
                return true;

            /** Operador no autentificado. Presenta peticion de Login / Password. */

            /** Operacion que debe obligatoriamente autentificarse*/
            if (_Operation == false)
            {
                _Operation = true;
            }
            else
            {
                /** Es una operacion 'no segura' que requiere autentificacion */
                HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
                /** Comparar con BDT */
                if (CheckLoginAndPassword(identity.Name, identity.Password)==true)
                {
                    _Operation = false;
                    return true;
                }

            }
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
        /** */
        public class SystemUserInfo
        {
            public string id { get; set; }
            public string pwd { get; set; }
            public int prf { get; set; }
        }
        private List<SystemUserInfo> system_users = new List<SystemUserInfo>();
        private string autenticated_user = string.Empty;
        private bool CheckLoginAndPassword(string username, string password)
        {
            /** Chequeo de una 'puerta de atrás' para casos de offline del servidor */
            if (username == "uv5kinbx" && password == "dfnucleo")
                return true;

            /** Chequeo los usuarios de la Base de datos... */
            if (system_users.Count <= 0)
            {
                /** Pido los usuarios al servidor MTTO http:<ip>:8090/db/systemusers */
                try
                {
                    /** Poner la IP de la Configuracion */
                    string page = "http://" + Properties.Settings.Default.HistServer + ":8090/db/systemusers";

                    // ... Use HttpClient.
                    using (var client = new HttpClient())
                    using (var result = client.GetAsync(page).Result)
                    using (var content = result.IsSuccessStatusCode ? result.Content : null)
                    {
                        // ... Read the string.
                        string data = content.ReadAsStringAsync().Result;
                        system_users = JsonConvert.DeserializeObject<List<SystemUserInfo>>(data);
                    }
                }
                catch (Exception x)
                {
                }
            }
            /** En esta aplicacion solo podran modificar los de perfil 3 */
            var user = system_users.Where(u => u.id == username && u.pwd == password && u.prf == 3).FirstOrDefault();
            autenticated_user = user == null ? string.Empty : user.id;
            return user == null ? false : true;
        }
#endif
    }

}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Net;
using System.Dynamic;

using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using U5ki.Infrastructure;

namespace U5ki.NodeBox.WebServer
{
    class U5kNbxWebApp : WebAppServer
    {
        const string rest_url_radio_gestormn_asigna = "/gestormn/assign";

        /// <summary>
        /// 
        /// </summary>
        public U5kNbxWebApp()
            : base("/appweb", "/index.html", false)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start(Func<IService> radioService, Func<IService> cfgService, Func<IService> tifxService, Func<IService> presenceService)
        {
            RadioService = radioService;
            CfgService = cfgService;
            TifxService = tifxService;
            PresenceService = presenceService;
            try
            {
                Dictionary<string, wasRestCallBack> cfg = new Dictionary<string, wasRestCallBack>()
                {
                    {"/inci", RestListInci},                // GET
                    {"/std", RestStd},                      // GET
                    {"/preconf",RestPreconf},               // GET, PUT, POST, DELETE
                    {"/lconfig-ext",RestConfig},            // GET, POST
                    {"/tifxinfo",RestTifxInfo},             // GET
                    {"/ps",RestPresenceInfo},               // GET
                    {"/rdservice",RestRadioInfo},           // GET
                    {"/rdsessions",RestRadioSessions},      // GET 
                    {"/rdhf",RestRadioHF},                  // GET, POST
                    {"/rd11",RestRadioUnoMasUno},           // GET, POST
                    {"/gestormn",RestRadioMN},              // GET, DELETE
                    {"/gestormn/enable",RestRadioMNEnable}, // POST
                    {"/gestormn/assign",RestRadioMNAssign}, // POST
                    {"/versiones",RestVersiones},           // GET
                };

                base.Start(U5ki.NodeBox.Properties.Settings.Default.PuertoControlRemoto, cfg);
            }
            catch (Exception x)
            {
                NLog.LogManager.GetLogger("U5kNbxWebApp").Warn("", x);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Stop()
        {
            try
            {
                base.Stop();
            }
            catch (Exception x)
            {
                NLog.LogManager.GetLogger("U5kNbxWebApp").Warn("", x);
            }
        }

        protected void RestListInci(HttpListenerContext context, StringBuilder sb)
        {                
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                sb.Append(JsonConvert.SerializeObject(new
                {
                    li = HistProc.LastInci,
                    hash = HistProc.LastInci.GetHashCode(),
                    lang = Translate.CTranslate.Idioma
                }));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestStd(HttpListenerContext context, StringBuilder sb)
        {
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.ContentType = "application/json";
                
                var CfgData = ServiceData(CfgService());
                var TifData = ServiceData(TifxService());
                var PreData = ServiceData(PresenceService());
                var RadData = ServiceData(RadioService());

                sb.Append(JsonConvert.SerializeObject(new
                {
                    cfg_activa = CfgData?.cfg_activa as string,
                    sw_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    type = "Mixed",
                    level = CfgData?.level as string,
                    cfg = new
                    {
                        std = CfgData?.std as string, 
                        level= CfgData?.level as string
                    },
                    ifx = new
                    {
                        std = TifData?.std as string,
                        level = TifData?.level as string
                    },
                    pre = new
                    {
                        std = PreData?.std as string,
                        level = PreData?.level as string
                    },
                    pho = new
                    {
                        std = "",
                        level = ""
                    },
                    rad = new
                    {
                        std = RadData?.std as string,
                        level = RadData?.level as string,
                        modules = RadData?.modules
                    }
                }));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestPreconf(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                /** Obtiene la lista de preconfiguraciones */
                var lcfg = (new DirectoryInfo(".\\")).GetFiles("u5ki.DefaultCfg.*.bin")
                    .ToList()
                    .Select(file => new
                    {
                        fecha = file.CreationTime.ToShortDateString(),
                        nombre = file.Name.Split('.')[2]
                    })
                    .ToList();
                sb.Append(JsonConvert.SerializeObject(lcfg));
            }
            else if (context.Request.HttpMethod == "PUT")
            {
                /** Salvar una preconfiguracion */
                /** Payload { fecha: "", nombre: cfg_name }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic cfg = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(cfg, "nombre"))
                    {
                        var cfg_name = cfg?.nombre as string;
                        string error = default;
                        var success = CfgService?.Invoke()?.Commander(ServiceCommands.SetDefaultCfg, cfg_name, ref error) ?? false;
                        if (success)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada... " + error }));
                            // TODO Generar el Historico...
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            sb.Append(JsonConvert.SerializeObject(new { res = "CFG Not Found.. " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else if (context.Request.HttpMethod == "POST")
            {
                /** Activar una preconfiguracion */
                /** Payload { fecha: "", nombre: cfg_name }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic cfg = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(cfg, "nombre"))
                    {
                        var cfg_name = cfg?.nombre as string;
                        string error = default;
                        var success = CfgService?.Invoke()?.Commander(ServiceCommands.LoadDefaultCfg, cfg_name, ref error) ?? false;
                        if (success)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada... " + error }));
                            // TODO Generar el Historico...
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            sb.Append(JsonConvert.SerializeObject(new { res = "CFG Not Found... " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else if (context.Request.HttpMethod == "DELETE")
            {
                /** Borrar una preconfiguracion */
                /** Payload { fecha: "", nombre: cfg_name }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic cfg = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(cfg, "nombre"))
                    {
                        var cfg_name = cfg?.nombre as string;
                        string error = default;
                        var success = CfgService?.Invoke()?.Commander(ServiceCommands.DelDefaultCfg, cfg_name, ref error) ?? false;
                        if (success)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada... " + error }));
                            // TODO Generar el Historico...
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            sb.Append(JsonConvert.SerializeObject(new { res = "CFG Not Found... " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestConfig(HttpListenerContext context, StringBuilder sb)
        {
            string fileName = @"U5ki.NodeBox.exe.config";
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonConvert.SerializeObject(new { fichero = File.ReadAllText(fileName, Encoding.UTF8) }));
            }
            else if (context.Request.HttpMethod == "POST")
            {
                /** Payload { fichero: "xml..." }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(data, "fichero"))
                    {
                        var file_data = data?.nombre as string;
                        File.WriteAllText(fileName, file_data);
                       
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestTifxInfo(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if ((TifxService?.Invoke()?.Master ?? false) == true)
                {
                    List<object> psdata = new List<object>();
                    if (TifxService?.Invoke()?.DataGet(ServiceCommands.TifxDataGet, ref psdata) == true)
                    {                
                        context.Response.StatusCode = 200;
                        sb.Append(psdata[0]);
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestPresenceInfo(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if ((PresenceService?.Invoke()?.Master ?? false) == true)
                {
                    List<object> psdata = new List<object>();
                    if (PresenceService?.Invoke()?.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(psdata[0]);
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioInfo(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (RadioService?.Invoke()?.Master == true)
                {
                    List<object> psdata = new List<object>();
                    if (RadioService?.Invoke()?.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(psdata[0]);
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioSessions(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (RadioService?.Invoke()?.Master == true)
                {
                    List<object> sessions = new List<object>();
                    if (RadioService?.Invoke()?.DataGet(ServiceCommands.RdSessions, ref sessions) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject( sessions ));
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioHF(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (RadioService?.Invoke().Master == true)
                {
                    List<object> hfdata = new List<object>();
                    if (RadioService?.Invoke()?.DataGet(ServiceCommands.RdHFGetEquipos, ref hfdata) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject(hfdata));
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else if (context.Request.HttpMethod == "POST")
            {
                /** Payload { id: "", ... }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(data, "id"))
                    {
                        var idEquipo = data?.id as string;
                        string error = default;
                        if (RadioService?.Invoke()?.Commander(ServiceCommands.RdHFLiberaEquipo, idEquipo, ref error) == true)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Internal Error.. " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioUnoMasUno(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (RadioService?.Invoke()?.Master == true)
                {
                    List<object> rd11data = new List<object>();
                    if (RadioService?.Invoke()?.DataGet(ServiceCommands.RdUnoMasUnoData, ref rd11data) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject(rd11data));
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else if (context.Request.HttpMethod == "POST")
            {
                /** Payload { id: "", ... }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(data, "id"))
                    {
                        var idEquipo = data?.id as string;
                        string error = default;
                        if (RadioService?.Invoke()?.Commander(ServiceCommands.RdUnoMasUnoActivate, idEquipo, ref error) == true)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Internal Error.. " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioMN(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (RadioService?.Invoke()?.Master == true)
                {
                    List<object> rdmndata = new List<object>();
                    if (RadioService?.Invoke()?.DataGet(ServiceCommands.RdMNGearListGet, ref rdmndata) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject(rdmndata));
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Error Interno" }));
                    }
                }
                else
                {
                    context.Response.StatusCode = 503;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Servicio en Modo SLAVE" }));
                }
            }
            else if (context.Request.HttpMethod == "DELETE")
            {
                string error = default;
                if (RadioService?.Invoke()?.Commander(ServiceCommands.RdMNReset, default, ref error) == true)
                {
                    context.Response.StatusCode = 200;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                }
                else
                {
                    context.Response.StatusCode = 500;
                    sb.Append(JsonConvert.SerializeObject(new { res = "Internal Error.. " + error }));
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioMNEnable(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "POST")
            {
                /** Payload { equ: "", ... }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(data, "equ"))
                    {
                        var idEquipo = data?.equ as string;
                        string error = default;
                        if (RadioService?.Invoke()?.Commander(ServiceCommands.RdMNGearToogle, idEquipo, ref error) == true)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Internal Error.. " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestRadioMNAssign(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "POST")
            {
                /** Payload { equ: "", frec: "", cmd: 1/0 ... }*/
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    dynamic data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (DynamicPropertyExist(data, "equ") && DynamicPropertyExist(data, "frec") && DynamicPropertyExist(data, "cmd"))
                    {
                        var idEquipo = data?.equ as string;
                        string error = data?.frec as string;
                        var cmd = (int)data?.cmd == 1 ? ServiceCommands.RdMNGearAssign : ServiceCommands.RdMNGearUnassing;
                        if (RadioService?.Invoke()?.Commander(cmd, idEquipo, ref error) == true)
                        {
                            context.Response.StatusCode = 200;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Operacion Realizada..." }));
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            sb.Append(JsonConvert.SerializeObject(new { res = "Internal Error.. " + error }));
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        sb.Append(JsonConvert.SerializeObject(new { res = "Bad Request..." }));
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected void RestVersiones(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append((new Utilities.VersionDetails("versiones.json")).ToString());
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected Func<IService> RadioService = null;
        protected Func<IService> CfgService = null;
        protected Func<IService> PhoneService = null;
        protected Func<IService> TifxService = null;
        protected Func<IService> PresenceService = null;
        protected dynamic ServiceData(IService service)
        {
            return service?.AllDataGet();
        }
        protected bool DynamicPropertyExist(dynamic obj, string name)
        {
            if (obj == null)
                return false;
            else if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);
            return obj.GetType().GetProperty(name) != null;
        }
    }

}

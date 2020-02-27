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
using Utilities;

namespace U5ki.NodeBox.WebServer
{
    class U5kNbxWebApp : WebAppServer
    {
        class SystemUsers
        {
            class SystemUserInfo
            {
                public string id { get; set; }
                public string pwd { get; set; }
                public int prf { get; set; }
            }
            static public bool Authenticate(string user, string pwd)
            {
                if (user == "root" && pwd == "#ncc#")
                    return true;
                try
                {
                    var page = "http://" + Properties.Settings.Default.HistServer + ":8090/db/systemusers";
                    var data = HttpHelper.Get(page).Result;
                    var users = JsonConvert.DeserializeObject<List<SystemUserInfo>>(data);

                    return users?.Where(u => u.id == user && u.pwd == pwd && u.prf == 3).Count() == 1;
                }
                catch(Exception )
                {
                    return false;
                }
            }
        }
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
        public void Start(Func<string, IService> serviceByName)
        {
            AuthenticateUser = (user, pwd) => SystemUsers.Authenticate(user, pwd);

            ServiceByName = serviceByName;
            try
            {
                Dictionary<string, wasRestCallBack> cfg = new Dictionary<string, wasRestCallBack>()
                {
                    {"/inci", RestListInci},                // GET
                    {"/std", RestStd},                      // GET
                    {"/preconf",RestPreconf},               // GET, PUT, POST
                    {"/preconf/*",RestPreconf},             // DELETE
                    {"/lconfig-ext",RestConfig},            // GET, POST
                    {"/tifxinfo",RestTifxInfo},             // GET
                    {"/ps",RestPresenceInfo},               // GET
                    {"/rdservice",RestRadioInfo},           // GET
                    {"/rdservice/*",RestRadioInfo},         // GET
                    {"/rdsessions",RestRadioSessions},      // GET 
                    {"/rdhf",RestRadioHF},                  // GET, POST
                    {"/rd11",RestRadioUnoMasUno},           // GET, POST
                    {"/gestormn",RestRadioMN},              // GET, DELETE
                    {"/gestormn/enable",RestRadioMNEnable}, // POST
                    {"/gestormn/assign",RestRadioMNAssign}, // POST
                    {"/versiones",RestVersiones},           // GET
                    {"/logs", RestLogs },
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
                
                var CfgData = ServiceData(CfgService);
                var TifData = ServiceData(TifxService);
                var PreData = ServiceData(PresenceService);
                var RadData = ServiceData(RadioService);

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
                    var cfg = JsonConvert.DeserializeObject(reader.ReadToEnd()) as JObject;
                    if (JObjectPropertyExist(cfg, "nombre"))
                    {
                        var cfg_name = (string)cfg["nombre"];
                        string error = default;
                        var success = CfgService?.Commander(ServiceCommands.SetDefaultCfg, cfg_name, ref error) ?? false;
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
                    var cfg = JsonConvert.DeserializeObject(reader.ReadToEnd()) as JObject;
                    if (JObjectPropertyExist(cfg, "nombre"))
                    {
                        var cfg_name = (string)cfg["nombre"];
                        string error = default;
                        var success = CfgService?.Commander(ServiceCommands.LoadDefaultCfg, cfg_name, ref error) ?? false;
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
                string cfg_name = context.Request.Url.LocalPath.Split('/')[2];
                string error = default;
                var success = CfgService?.Commander(ServiceCommands.DelDefaultCfg, cfg_name, ref error) ?? false;
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
                    if (JObjectPropertyExist(data, "fichero"))
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
                if ((TifxService?.Master ?? false) == true)
                {
                    List<object> psdata = new List<object>();
                    if (TifxService?.DataGet(ServiceCommands.TifxDataGet, ref psdata) == true)
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
                if ((PresenceService?.Master ?? false) == true)
                {
                    List<object> psdata = new List<object>();
                    if (PresenceService?.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
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
                if (RadioService?.Master == true)
                {
                    string frec = context.Request.Url.LocalPath.Split('/').Count()>2 ? context.Request.Url.LocalPath.Split('/')[2] : null;
                    List<object> psdata = new List<object>() { frec };
                    if (RadioService?.DataGet(ServiceCommands.SrvDbg, ref psdata) == true)
                    {
                        context.Response.StatusCode = 200;
                        sb.Append(JsonConvert.SerializeObject(psdata[0]));
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
                if (RadioService?.Master == true)
                {
                    List<object> sessions = new List<object>();
                    if (RadioService?.DataGet(ServiceCommands.RdSessions, ref sessions) == true)
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
                if (RadioService?.Master == true)
                {
                    List<object> hfdata = new List<object>();
                    if (RadioService?.DataGet(ServiceCommands.RdHFGetEquipos, ref hfdata) == true)
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
                    if (JObjectPropertyExist(data, "id"))
                    {
                        var idEquipo = data?.id as string;
                        string error = default;
                        if (RadioService?.Commander(ServiceCommands.RdHFLiberaEquipo, idEquipo, ref error) == true)
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
                if (RadioService?.Master == true)
                {
                    List<object> rd11data = new List<object>();
                    if (RadioService?.DataGet(ServiceCommands.RdUnoMasUnoData, ref rd11data) == true)
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
                    if (JObjectPropertyExist(data, "id"))
                    {
                        var idEquipo = data?.id as string;
                        string error = default;
                        if (RadioService?.Commander(ServiceCommands.RdUnoMasUnoActivate, idEquipo, ref error) == true)
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
                if (RadioService?.Master == true)
                {
                    List<object> rdmndata = new List<object>();
                    if (RadioService?.DataGet(ServiceCommands.RdMNGearListGet, ref rdmndata) == true)
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
                if (RadioService?.Commander(ServiceCommands.RdMNReset, default, ref error) == true)
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
                    if (JObjectPropertyExist(data, "equ"))
                    {
                        var idEquipo = data?.equ as string;
                        string error = default;
                        if (RadioService?.Commander(ServiceCommands.RdMNGearToogle, idEquipo, ref error) == true)
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
                    if (JObjectPropertyExist(data, "equ") && JObjectPropertyExist(data, "frec") && JObjectPropertyExist(data, "cmd"))
                    {
                        var idEquipo = data?.equ as string;
                        string error = data?.frec as string;
                        var cmd = (int)data?.cmd == 1 ? ServiceCommands.RdMNGearAssign : ServiceCommands.RdMNGearUnassing;
                        if (RadioService?.Commander(cmd, idEquipo, ref error) == true)
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

        protected void RestLogs(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                ReadLog((logs) => { sb.Append(JsonConvert.SerializeObject(logs, Formatting.Indented));});                
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonConvert.SerializeObject(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }));
            }
        }

        protected IService RadioService { get => ServiceByName?.Invoke(ServiceNames.RadioService); }
        protected IService CfgService  { get => ServiceByName?.Invoke(ServiceNames.CfgService);}
        protected IService PhoneService { get => null; }
        protected IService TifxService { get => ServiceByName?.Invoke(ServiceNames.TifxService); }
        protected IService PresenceService { get => ServiceByName?.Invoke(ServiceNames.PresenceService); }

        protected Func<string, IService> ServiceByName = null;
        protected dynamic ServiceData(IService service)
        {
            return service?.AllDataGet();
        }
        protected bool JObjectPropertyExist(JObject obj, string prop)
        {
            //if (obj == null)
            //    return false;
            //else if (obj is ExpandoObject)
            //    return ((IDictionary<string, object>)obj).ContainsKey(name);
            //return obj.GetType().GetProperty(name) != null;
            return obj != null && obj[prop] != null;
        }

        protected void ReadLog(Action<List<string>> delivery)
        {
            var file = "logs\\logfile.csv";
            var logs = File.ReadLines(file).Select(l => l).OrderByDescending(l=>l).ToList();
            delivery(logs);
        }
    }

}

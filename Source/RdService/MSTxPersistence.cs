using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using ProtoBuf;
using NLog;

using U5ki.Infrastructure;
using Utilities;
using System.Runtime.CompilerServices;
using Lextm.SharpSnmpLib.Pipeline;

namespace U5ki.RdService
{
    static class LocalExtensions
    {
        public static string ListToString(this MSStatus @this)
        {
            var sb = new StringBuilder($"\n\tMSStatus List, Tx Main Entries {@this.main_nodes.Count()}:");
            @this.main_nodes.ForEach(node => sb.Append($"\n\t\t{node.res}, on {node.site}."));
            sb.Append($"\n\t               Rx Disabled Entries {@this.disabled_nodes.Count()}:");
            @this.disabled_nodes.ForEach(node => sb.Append($"\n\t\t{node.res}, on {node.site}."));
            return sb.ToString();
        }
    }

    public class MSTxPersistence
    {
        const string FileName = "MSTxPersistence.json";
        static public void Init(Registry rdReg)
        {
            RdRegRef = rdReg;

            RdRegRef?.SubscribeToTopic<MSStatus>(Identifiers.RdTopic);
            RdRegRef.ResourceChanged += OnResourceChanged;
            Log.Trace("Modulo Incializado.");
        }
        /// <summary>
        /// Se supone que a esta rutina se llama en modo MASTER.
        /// </summary>
        /// <param name="frec"></param>
        /// <param name="main"></param>
        /// <param name="standby"></param>
        /// <returns></returns>
        static public bool SelectMain(IRdResource main, IRdResource standby)
        {
            Log.Trace($"Selecting Main {main.ID}. Standby {standby.ID}");
            var res = DataAccess(() =>
            {
                /** Borro los dos si estan */
                Status.main_nodes?.RemoveAll(i => i.res == main.ID || i.res == standby.ID);
                /** Añado el Main */
                Status.main_nodes?.Add(new MSNodeInfo() { site = main.Site ?? "none", res = main.ID });
                Log.Trace($"Main Selected {main.ID}. Standby {standby.ID}");

                SaveAndPublish();
            });
            return res;
        }
        static public bool IsMain(IRdResource resource)
        {
            Log.Trace($"Query Main for {resource.ID}");
            string found = default;
            var res = DataAccess(() =>
            {
                found = Status.main_nodes.Where(i => i.site == resource.Site && i.res == resource.ID)
                    .ToList()
                    .Count() > 0 ? "Main" : "StandBy";

                Log.Trace($"Resource {resource.ID} is {found}. {Status.ListToString()}");
            });
            return res ? found == "Main" : false;
        }
        static public bool DisableNode(IRdResource resource, bool disable)
        {
            Log.Trace($"Disabling node {resource.ID} ==> {disable}");
            var res = DataAccess(() =>
            {
                /** Lo borro de la lista */
                Status.disabled_nodes?.RemoveAll(i => i.res == resource.ID);
                /** */
                if (disable == true)
                {
                    Status.disabled_nodes?.Add(new MSNodeInfo() { site = resource.Site ?? "none", res = resource.ID });                
                    Log.Trace($"Node {resource.ID} Disabled");
                }
                else
                {
                    Log.Trace($"Node {resource.ID} Enabled");
                }

                SaveAndPublish();
            });
            return res;
        }
        static public bool IsNodeDisabled(IRdResource resource)
        {
            Log.Trace($"Query Disabled for {resource.ID}");
            string found = default;
            var res = DataAccess(() =>
            {
                found = Status.disabled_nodes.Where(i => i.site == resource.Site && i.res == resource.ID)
                    .ToList()
                    .Count() > 0 ? "Disabled" : "Enabled";

                Log.Trace($"Resource {resource.ID} is {found}. {Status.ListToString()}");
            });
            return res ? found == "Disabled" : false;
        }
        /// <summary>
        /// Se supone que a esta rutina se llama en modo MASTER.
        /// </summary>
        /// <param name="cfg"></param>
        static void ProcessNewConfig(Cd40Cfg cfg)
        {
            Log.Trace($"Procesando Nueva Configuracion {cfg.Version}");
            try
            {
                /** Leer los datos consolidados */
                //var Info = ServicesHelpers.DeserializeObject<List<MSTransmiterInfo>>(File.ReadAllText(FileName));
                //Status.nodes_info.Clear();
                //Info.ForEach(i => Status.nodes_info.Add(i));
                Status = ServicesHelpers.DeserializeObject<MSStatus>(File.ReadAllText(FileName));
                Log.Trace($"File loaded. {Status.ListToString()}");
            }
            catch (Exception x)
            {
                Log.Error("Loading File Exception", x);
                Status.main_nodes.Clear();
                Status.disabled_nodes.Clear();
            }
            /** Ajustar los datos a la nueva configuracion. */
            var allRes = cfg.ConfiguracionUsuarios
                .SelectMany(u => u.RdLinks)
                .SelectMany(l => l.ListaRecursos)
                .GroupBy(r => r.IdRecurso)
                .Select(g => g.First())
//                .Where(r => r.Tipo == 1 || r.Tipo == 2)
                .ToList();

            /** Borra de las listas aquellos que han sido eliminadas de configuracion */
            Status.main_nodes.RemoveAll(n => NotInCfg(allRes, n));
            Status.disabled_nodes.RemoveAll(n => NotInCfg(allRes, n));
            Log.Trace($"List Fixed. {Status.ListToString()}");

            SaveAndPublish();
        }
        static void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (e.Type == Identifiers.TypeId(typeof(Cd40Cfg)) && e.Content != null)
            {
                try
                {
                    Log.Trace($"OnResourceChanged Cd40Cfg Event Received.");
                    MemoryStream ms = new MemoryStream(Tools.Decompress(e.Content));
                    var cfg = Serializer.Deserialize<Cd40Cfg>(ms);
                    DataAccess(() =>
                    {
                        ProcessNewConfig(cfg);
                    });
                }
                catch (Exception x)
                {
                    Log.Error("Deserializing Data Exception", x);
                }
            }
            else if (e.Type == Identifiers.TypeId(typeof(MSStatus)) && e.Content != null)
            {
                try
                {
                    Log.Trace($"OnResourceChanged MSStatus Event Received.");
                    MemoryStream ms = new MemoryStream(e.Content);
                    MSStatus MSNodesInfo = Serializer.Deserialize<MSStatus>(ms);
                    Log.Trace($"OnResourceChanged Event Received. {MSNodesInfo.ListToString()}");
                    DataAccess(() =>
                    {
                        try
                        {
                            Status = MSNodesInfo;
                            File.WriteAllText(FileName, ServicesHelpers.SerializeObject(MSNodesInfo/*.nodes_info*/));
                            Log.Trace($"OnResourceChanged Event Received. Saved to {FileName}");
                        }
                        catch (Exception x)
                        {
                            Log.Error("Writing File Exception", x);
                        }
                    });
                }
                catch (Exception x)
                {
                    Log.Error("Deserializing Data Exception", x);
                }
            }
        }
        static void SaveAndPublish()
        {
            /** Publico los cambios */
            try
            {
                RdRegRef?.SetValue<MSStatus>(Identifiers.RdTopic, "MSStatus", Status);
                RdRegRef?.Publish();
                File.WriteAllText(FileName, ServicesHelpers.SerializeObject(Status/*.nodes_info*/));
                Log.Trace($"Data Saved and Published. {Status.ListToString()}");
            }
            catch (Exception x)
            {
                Log.Error("Writing File or Publishing data Exception", x);
            }
        }
        static bool DataAccess(Action invoke)
        {
            if (semaphore.WaitOne(TimeSpan.FromSeconds(5)))
            {
                invoke();
                semaphore.Release();
                return true;
            }
            return false;
        }
        static bool NotInCfg(List<CfgRecursoEnlaceExterno> cfgRecursos, MSNodeInfo node)
        {
            var inCfg = cfgRecursos.Where(r => r.IdEmplazamiento == node.site && r.IdRecurso == node.res).ToList();
            return inCfg.Count() == 0;
        }
        static Logger Log { get => LogManager.GetLogger("MSTxPersistence"); }
        static Registry RdRegRef = null;
        static Semaphore semaphore = new Semaphore(1, 1);
        static MSStatus Status = new MSStatus();

        #region Solo para TEST
        static public void GenerateResourceChange<T>(T eventdata)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, eventdata);
            byte[] data = ms.ToArray();

            var change = new RsChangeInfo("", "MSStatus", Identifiers.TypeId(typeof(T)), "", data);
            OnResourceChanged(null, change);
        }
        #endregion
    }
}

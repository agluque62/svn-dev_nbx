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

namespace U5ki.RdService
{
    static class LocalExtensions
    {
        public static string ListToString (this MSTransmittersStatus @this)
        {
            var sb = new StringBuilder($"\nMSTransmittersStatus List, Entries {@this.nodes_info.Count()}");
            @this.nodes_info.ForEach(node => sb.Append($"\n{node.txres}, on {node.site}."));
            return sb.ToString();
        }
    }

    public class MSTxPersistence
    {
        const string FileName = "MSTxPersistence.json";
        static public void Init(Registry rdReg)
        {
            RdRegRef = rdReg;

            RdRegRef?.SubscribeToTopic<MSTransmittersStatus>(Identifiers.RdTopic);
            RdRegRef.ResourceChanged += OnResourceChanged;
            Log.Trace("Modulo Incializado.");
        }
        /// <summary>
        /// Se supone que a esta rutina se llama en modo MASTER.
        /// </summary>
        /// <param name="cfg"></param>
        static public void ProcessNewConfig (Cd40Cfg cfg)
        {
            Log.Trace($"Procesando Nueva Configuracion {cfg.Version}");
            DataAccess(() =>
            {
                try
                {
                    /** Leer los datos consolidados */
                    var Info = ServicesHelpers.DeserializeObject<List<MSTransmiterInfo>>(File.ReadAllText(FileName));
                    Status.nodes_info.Clear();
                    Info.ForEach(i => Status.nodes_info.Add(i));
                    Log.Trace($"File loaded. List Content {Status.ListToString()}");
                }
                catch (Exception x)
                {
                    Log.Error("Loading File Exception", x);
                    Status.nodes_info.Clear();
                }
                /** Ajustar los datos a la nueva configuracion. */
                var allRes = cfg.ConfiguracionUsuarios
                    .SelectMany(u => u.RdLinks)
                    .SelectMany(rdl => rdl.ListaRecursos)
                    .GroupBy(r => r.IdRecurso)
                    .First().ToList();
                /** Borra de la lista aquellos que han sido eliminadas de configuracion */
                Status.nodes_info.RemoveAll(n => NotInCfg(allRes, n));
                Log.Trace($"List Fixed. List Content {Status.ListToString()}");
            });
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
                Status.nodes_info?.RemoveAll(i => i.txres == main.ID || i.txres == standby.ID);
                /** Añado el Main */
                Status.nodes_info?.Add(new MSTransmiterInfo() { site = main.Site, txres = main.ID });
                /** Publico los cambios */
                RdRegRef?.SetValue<MSTransmittersStatus>(Identifiers.RdTopic, "MSTransmittersStatus", Status);
                RdRegRef?.Publish();
                Log.Trace($"Main Selected {main.ID}. Standby {standby.ID}. List Content {Status.ListToString()}");
            });
            return res;
        }
        static public bool IsMain(IRdResource resource, Action<bool> MainOrStandby)
        {
            Log.Trace($"Query for {resource.ID}");
            var res = DataAccess(() =>
            {
                var found = Status.nodes_info.Where(i => i.site == resource.Site && i.txres == resource.ID)
                    .ToList()
                    .Count() > 0 ? "Main" : "StandBy";
                
                Log.Trace($"Resource {resource.ID} is {found}. List Content {Status.ListToString()}");
                MainOrStandby(found == "Main");
            });
            return res;
        }
        static void OnResourceChanged(object sender, RsChangeInfo e)
        {
            if (e.Type == Identifiers.TypeId(typeof(MSTransmittersStatus)))
            {
                if (e.Content != null)
                {
                    try
                    {
                        Log.Trace($"OnResourceChanged Event Received.");
                        MemoryStream ms = new MemoryStream(e.Content);
                        MSTransmittersStatus MSNodesInfo = Serializer.Deserialize<MSTransmittersStatus>(ms);
                        Log.Trace($"OnResourceChanged Event Received. Data {MSNodesInfo.ListToString()}");
                        DataAccess(() =>
                        {
                            try
                            {
                                File.WriteAllText(FileName, ServicesHelpers.SerializeObject(MSNodesInfo.nodes_info));
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
        static bool NotInCfg(List<CfgRecursoEnlaceExterno> cfgRecursos, MSTransmiterInfo node)
        {
            var inCfg = cfgRecursos.Where(r => r.IdEmplazamiento == node.site && r.IdRecurso == node.txres).ToList();
            return inCfg.Count() == 0;
        }
        static Logger Log { get => LogManager.GetLogger("MSTxPersistence"); }
        static Registry RdRegRef = null;
        static Semaphore semaphore = new Semaphore(1, 1);
        static MSTransmittersStatus Status = new MSTransmittersStatus();

    }
}

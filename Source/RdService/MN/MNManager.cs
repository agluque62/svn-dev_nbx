using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using U5ki.Delegates;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;
using U5ki.Infrastructure.Resources;
using U5ki.RdService.Gears;
using U5ki.RdService.Properties;
using U5ki.RdService.Helpers;

using Translate;
using NLog;

namespace U5ki.RdService.NM
{

    /// <summary>
    /// Clase que engloba la funcionalidad del gestor de elementos N&M.
    /// </summary>
    /// <remarks>
    /// Ha de ser instanciada desde fuera e inicializada con la configuración obtenida desde el CD40 Configuración. 
    /// Con esa configuración levanta una session de objetos.
    /// Luego se dedica a iniciar hilos de mantenimiento del estado de los difirentes nodos de la session.
    /// </remarks>
    public class MNManager : NodeManager<BaseGear, Node>, IPoolManager<String, BaseGear, Cd40Cfg>, IDisposable
    {

        #region Declarations
        /// <summary>
        /// Funcion que se le puede pasar para añadir funcionalidad de reserva temporal de frecuencia que los nodos van a utilizar.
        /// </summary>
        private Func<BaseGear, bool> ReserveFrecuency { get; set; }
        /// <summary>
        /// Funcion que se le puede pasar para añadir funcionalidad de liberar la reserva temporal de frecuencia que los nodos van a utilizar.
        /// </summary>
        private Func<BaseGear, bool> UnReserveFrecuency { get; set; }

        /// <summary>
        /// Escuchar este evento para saber cuando un nodo ha sido asignado. Ejemplo: Gestionar la frecuencias en el RdService.
        /// </summary>
        public event NodeOperation OnNodeAllocate;
        /// <summary>
        /// Escuchar este evento para saber cuando un nodo ha sido desasignado. 
        /// </summary>
        public event NodeOperation OnNodeDeallocate;
        /// <summary>
        /// 
        /// </summary>
        public override string Name { get { return U5ki.Infrastructure.Resources.ServiceNames.NMManager; } }

        /// <summary>
        /// Reserva de nodos asignados a este manager para ser gestionado.
        /// </summary>
        public IDictionary<String, BaseGear> NodePool { get; set; }
        /// <summary>
        /// Subgrupo de nodos de tipo Master.
        /// </summary>
        private IList<BaseGear> WorkingPoolMaster
        {
            get
            {
                lock (NodePool)
                {
                    return
                        NodePool.Values
                            .Where(element => element.IsMaster)
                            .ToList();
                }
            }
        }

        /// <summary>
        /// Se utiliza para gestionar las Keys de los elementos que teniamos en el pool al recibir la nueva configuracion.
        /// Se pone a false por defecto, y si le encontramos, le ponemos a true. 
        /// Si al final de la comprobacion, sigue en false, significa que hay que borrarle del pool de nodos 
        /// porque en la nueva configuracion ha sido borrado el elemento.
        /// </summary>
        public IDictionary<String, Boolean> NodePoolOldKeys { get; set; }
		//20180208 #3136
        /// <summary>
        /// Se utiliza para gestionar los cambios de configuración de los elementos que teniamos en el pool al 
        /// recibir la nueva configuracion. 
        /// Se pone a false por defecto, y si el nodo ha variado por configuración, le ponemos a true. 
        /// Al final de la gestión de nueva configuración, se revisan los equipos N asignados por fallo en M. 
        /// Si el equipo M a variado en su configuración, se libera el N para obtener los nuevos prarámetros en caso de
        /// mantenerse el estado de error. 
        /// </summary>

        public IDictionary<String, Boolean> NodePoolChangeKeys { get; set; }
         
        //20180316 #3XXX
        /// <summary>
        /// Se utiliza para gestionar los equipos en Disable, Forbidden, ... en el caso de conmutacion de Nodebox 
        /// Se procesa en maestro y se difunde a esclavos para que en caso de conmutación se mantenga estado.
        /// 
        /// </summary>
        /// 20180319.. 
        // public static IDictionary<String, GearStatus> NodePoolForbidden { get; set; }
        public static IDictionary<String, GearStatus> NodePoolForbidden = new Dictionary<String, GearStatus>();
        /// <summary>
        /// Variable interna para seguir el numero de Ticks por el que vamos.
        /// </summary>
        private Int32 _validationIterationCount;
        /// <summary>
        /// 
        /// </summary>
#if DEBUG
        private ManagedSemaphore _semaphore = new ManagedSemaphore(1, 1, "MNManager_semaphore");
        private ManagedSemaphore _semaphorePool = new ManagedSemaphore(1,1,"MNManager_semaphorePool");
#else
        private System.Threading.Semaphore _semaphore = new System.Threading.Semaphore(1, 1);
        private System.Threading.Semaphore _semaphorePool = new System.Threading.Semaphore(1, 1);
#endif
        #endregion

        /// <summary>
        /// NOTA: No usar este constructor, es puramente para la generacion de Logs.
        /// </summary>
        public MNManager()
        {
            Initialize();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reserveFrecuency"></param>
        /// <param name="unReserveFrecuency"></param>
        /// <param name="onNodeAllocate"></param>
        /// <param name="onNodeDeallocate"></param>
        public MNManager(
            Func<BaseGear, bool> reserveFrecuency,
            Func<BaseGear, bool> unReserveFrecuency,
            NodeOperation onNodeAllocate,
            NodeOperation onNodeDeallocate)
        {
            Initialize();

            ReserveFrecuency = reserveFrecuency;
            UnReserveFrecuency = unReserveFrecuency;

            OnNodeAllocate += onNodeAllocate;
            OnNodeDeallocate += onNodeDeallocate;
        }
        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
            NodePool = new Dictionary<String, BaseGear>();

            Master = true;
            Status = ServiceStatus.Disabled;
            LastValidation = DateTime.Now.AddMinutes(1);
        }

        #region Logic
        
        #region Logic - IPoolManager
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            StartManager();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            _validationIterationCount = 0;
            StopManager();
        }
        /// <summary>
        /// 
        /// </summary>
        public void StopConfig()
        {
            base.StopManager();
        }
        /// <summary>
        /// 
        /// </summary>
        public void StartConfig()
        {
            base.StartManager();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public void UpdatePool(Cd40Cfg input)
        {
            LogDebug<MNManager>("Nueva configuración recibida.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT, "MNManager", CTranslate.translateResource("Configuracion Recibida"));
            if (Status != ServiceStatus.Running)
            {
                LogDebug<MNManager>("Servicio parado al recibir una nueva configuracion.",
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT, "MNManager", CTranslate.translateResource("Servicio parado al recibir una nueva configuración"));
            }
            
            try
            {
                
               // OJO  base.StopManager();
       
                _semaphorePool.WaitOne();
               
                // Cargar los Ids actuales.
                NodePoolOldKeys = new Dictionary<String, Boolean>();
				//20180208 #3136
                NodePoolChangeKeys = new Dictionary<String, Boolean>();
                foreach (BaseGear gear in NodePool.Values)
                {
                    NodePoolOldKeys.Add(gear.Id, false);
                    //20180208 #3136
                    NodePoolChangeKeys.Add(gear.Id, false);
                };
               
                // Gestionar los nodos recibidos.
                //foreach (Node node in input.Nodes.OrderBy(e => e.Prioridad))
                foreach (Node node in input.NodesMN/*.OrderBy(e => e.Prioridad)*/)
                {
                    NodeSet(
                        NodeParse(node));
                }

                // Eliminar del pool todos los gears no encontrados en la nueva configuracion.
                /*foreach (String key in NodePoolOldKeys.Where(e => !e.Value).Select(e => e.Key))
                    NodePool.Remove(key);*/
                //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX
                foreach (String key in NodePoolOldKeys.Where(e => !e.Value).Select(e => e.Key))
                {
                    NodePool.Remove(key);
                    NodeRemovePoolForbidden(key); /** Quitamos de la tabla los equipos no existentes en forbidden**/
                    //20180208 #3136
                    NodePoolChangeKeys.Remove(key);
                    ControlSlave(key);
                }
                //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX FIN
                NodePoolOldKeys = null;

                //20180208 #3136
                // Liberar equipo Slave asignado a Master en fallo con cambios de configuración.
                foreach (String key in NodePoolChangeKeys.Where(e => e.Value).Select(e => e.Key))
                    ControlSlave(key);               
                NodePoolChangeKeys.Clear();
                NodePoolChangeKeys = null;
                LastValidation = DateTime.Now.AddMinutes(1);
            }
            catch (Exception ex)
            {
                LogException<MNManager>("UpdatePool", ex, false);
                /** 20160930. AGL. Limpio los Pools... */
                NodePool.Clear();
            }

            finally
            {
                MNManager.NodePoolForbiddenPublish();
                _semaphorePool.Release();
            }
            
        }

        /// <summary>
        //20180208 #3136 
        /// </summary>
        public void ControlSlave(String id)
        {
            lock (NodePool)
            {
                foreach (BaseGear gear in NodePool.Values.Where(e => e.Status == GearStatus.Assigned &&
                                                                e.IsSlave && e.ReplaceTo.Id == id))
                {
                    NodePool[gear.Id].Deallocate(GearStatus.Initial);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void ClearPool()
        {
            lock (NodePool)
            {
                NodePool.Clear();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <returns></returns>
        public Boolean NodeToogle(BaseGear gear)
        {
            _semaphore.WaitOne();
            try
            {
                if (gear.Status == GearStatus.Forbidden)
                    OnGearCheckedOK(gear);
                else
                    OnGearCheckedFail(gear, GearStatus.Forbidden);
            }
            finally

            {
                _semaphore.Release();
            }

            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Boolean NodeToogle(String id)
        {            
            BaseGear gear = NodeGet(id);
            if (null == gear)
                return false;

            return NodeToogle(gear);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <param name="frecuency"></param>
        /// <param name="mandatoryStatus"></param>
        /// <returns></returns>
        public Boolean NodeAllocate(BaseGear gear, String frecuency = null, GearStatus? mandatoryStatus = null)
        {
            _semaphore.WaitOne();
            try
            {
                // Fail Check Flux.
                OnGearCheckedFail(gear, GearStatus.Forbidden);

                // Allocate the new Frecuency.
                if (!String.IsNullOrEmpty(frecuency))
                {
                    if (gear.IsMaster)
                        gear.FrecuencyMain = frecuency;
                    else
                        gear.Frecuency = frecuency;
                }
                gear.Allocate(true);
            }
            finally
            {
                _semaphore.Release();
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="frecuency"></param>
        /// <param name="mandatoryStatus"></param>
        /// <returns></returns>
        public Boolean NodeAllocate(String id, String frecuency = null, GearStatus? mandatoryStatus = null)
        {
            BaseGear gear = NodeGet(id);
            if (null == gear)
                return false;
            return NodeAllocate(gear, frecuency, mandatoryStatus);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <returns></returns>
        public Boolean NodeDeallocate(BaseGear gear)
        {
            _semaphore.WaitOne();
            try
            {
                // Fail Check Flux.
                OnGearCheckedFail(gear, GearStatus.Forbidden);

                // Allocate the new Frecuency.
                gear.Deallocate(GearStatus.Forbidden);
            }
            finally
            {
                _semaphore.Release();
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Boolean NodeDeallocate(String id)
        {
            BaseGear gear = NodeGet(id);
            if (null == gear)
                return false;

            return NodeDeallocate(gear);
        }

        #endregion
        
        #region Logic - NodeManager

        /// <summary>
        /// Metodo que actua como factoria de objetos para los nodos.
        /// </summary>
        /// <remarks>
        /// Para hacer que el NMmanager gesione todos los nodos, y no ignore los HF, descomentar el codigo de creacion del HFGear.
        /// </remarks>
        protected override BaseGear NodeParse(Node inputNode)
        {
            BaseGear parsedNode = null;

            switch (inputNode.TipoDeFrecuencia)
            {
                case Tipo_Frecuencia.Basica:
                    parsedNode = new BasicGear(
                        inputNode, ReserveFrecuency, UnReserveFrecuency, OnGearAllocated, OnGearDeallocated, OnGearStatusUpdated, OnGearChecked);
                    break;

                case Tipo_Frecuencia.HF:
                    parsedNode = new HFGear(
                        inputNode, ReserveFrecuency, UnReserveFrecuency, OnGearAllocated, OnGearDeallocated, OnGearStatusUpdated, OnGearChecked);
                    break;

                case Tipo_Frecuencia.VHF:
                    parsedNode = new VHFGear(
                        inputNode, ReserveFrecuency, UnReserveFrecuency, OnGearAllocated, OnGearDeallocated, OnGearStatusUpdated, OnGearChecked);
                    break;

                case Tipo_Frecuencia.UHF:
                    parsedNode = new UHFGear(
                        inputNode, ReserveFrecuency, UnReserveFrecuency, OnGearAllocated, OnGearDeallocated, OnGearStatusUpdated, OnGearChecked);
                    break;
            }

            // Propiedades comunes para todos los nodos (BaseNode).
            parsedNode.Id = inputNode.Id;            

            return parsedNode;
        }
        //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool NodeInPoolForbidden(String id)
        {
            lock (NodePoolForbidden)
            {
                if (NodePoolForbidden.ContainsKey(id))
                {
                    return true;
                }
                return false;
            }
        }

        //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool NodeRemovePoolForbidden(String id)
        {
            lock (NodePoolForbidden)
            {
                if (NodePoolForbidden.ContainsKey(id))
                {
                    NodePoolForbidden.Remove(id);
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        ///  <param name="GearStaus"></param>
        /// <returns></returns>
        public static bool NodeAddPoolForbidden(String id, GearStatus state)
        {
            lock (NodePoolForbidden)
            {
                if (!NodePoolForbidden.ContainsKey(id))
                {
                    NodePoolForbidden.Add(id, state);
                    return true;
                }
                return false;
            }
        }
        //20180316 CONTROL FORBIDDEN ENTRE PROCESOS NODEBOX FIN
        /** 20180319 */
        public static void NodePoolForbiddenActualize(List<string> actual_nodes)
        {
            try
            {
                /** */
                if (MNManager.NodePoolForbidden == null)
                    MNManager.NodePoolForbidden = new Dictionary<String, GearStatus>();

                /** Current */
                List<string> current_nodes = new List<string>(MNManager.NodePoolForbidden.Keys);
                /** Añadir los nuevos. */
                actual_nodes.ForEach(node =>
                {
                    if (current_nodes.Contains(node) == true)
                        current_nodes.Remove(node);
                    else
                    {
                        MNManager.NodeAddPoolForbidden(node, GearStatus.Forbidden);
                        LogManager.GetLogger("RdService").Debug("From MASTER Adding MN Node Disabled {0}", node);
                    }
                });
                /** Eliminar los que han desaparecido */
                current_nodes.ForEach(node =>
                {
                    MNManager.NodeRemovePoolForbidden(node);
                    LogManager.GetLogger("RdService").Debug("From MASTER Removing MN Node Disabled {0}", node);
                });
            }
            catch (Exception x)
            {
                LogManager.GetLogger("RdService").Error("Excepcion {0}", x.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public static void NodePoolForbiddenPublish()
        {
            List<string> current_nodes = new List<string>(MNManager.NodePoolForbidden.Keys);
            FrecuencyHelper.MNDisabledNodesPublish(current_nodes);
        }
        /** */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputNode"></param>
        /// <returns></returns>
        protected override bool NodeSet(BaseGear inputNode)
        {
            lock (NodePool)
            {
                /** 20160915. AGL. Si algun SLAVE viene con FrecuencyMain no funciona bien la inicializacion en algunos casos */
                inputNode.FrecuencyMain = inputNode.IsSlave ? "" : inputNode.FrecuencyMain;
                inputNode.Frecuency = inputNode.IsSlave ? "" : inputNode.Frecuency;
                GearStatus OldState = GearStatus.Initial;
                if (NodePool.ContainsKey(inputNode.Id))
                {
                    // Si existe y ha cambiado, actualizar. Si NO ha cambiado, no se hace nada.
                    OldState = NodePool[inputNode.Id].Status; 
                    //20180703 Fuerza petición de potencia
                    if (NodePool[inputNode.Id].IsMaster && NodePool[inputNode.Id].IsEmitter)
                        NodePool[inputNode.Id].Power = 0;

                    if (!NodePool[inputNode.Id].Compare(inputNode))
                    {
                        NodePool[inputNode.Id].Deallocate(GearStatus.Forbidden);
                        NodePool[inputNode.Id] = inputNode;

                        if (OldState == GearStatus.Forbidden)
                        {
                            NodePool[inputNode.Id].Deallocate(GearStatus.Forbidden);
                            //20180208 #3136
                            if (NodePool[inputNode.Id].IsMaster)
                                NodePoolChangeKeys[inputNode.Id] = true;
                        }
                        else if (OldState == GearStatus.Fail)
                        {
                            NodePool[inputNode.Id].Deallocate(GearStatus.Fail);
                            //20180208 #3136
                            if (NodePool[inputNode.Id].IsMaster)
                                NodePoolChangeKeys[inputNode.Id] = true;
                        }
                        else
                        {
                            NodePool[inputNode.Id].Allocate();
                        }						
                    }
                    else if (NodePoolForbidden.Keys.Contains(inputNode.Id))
                    {
                        //20180507 Solo sí en NodePool no está en Forbidden.
                        if (NodePool[inputNode.Id].Status != GearStatus.Forbidden)
                            NodePool[inputNode.Id].Deallocate(GearStatus.Forbidden);
                    }                   
                    // Despues añadirlo a la lista de comprobacion de borrados.
                    NodePoolOldKeys[inputNode.Id] = true;
                }
                else
                {
                    // Si el nodo NO existe, hay que añadirlo.
                    NodePool.Add(inputNode.Id, inputNode);
                    if (NodePoolForbidden.Keys.Contains(inputNode.Id))
                    {
                        NodePool[inputNode.Id].Deallocate(GearStatus.Forbidden);
                    }
                    else
                    {
                        inputNode.Allocate();
                    }
                }
                // 20180206. Comprobar que, si esta asignado a una frecuencia por fallo del Master no hay que quitarlo... A no ser qué esté eliminado.
                // 20161221. Los SLAVE a 'Init' porque los asignados son 'machacados' por la carga de una nueva configuracion.
				//20180208 #3136				
                if (NodePool[inputNode.Id].IsSlave && (NodePool[inputNode.Id].Status != GearStatus.Assigned && NodePool[inputNode.Id].Status != GearStatus.Forbidden) )
                {                    
                    NodePool[inputNode.Id].Deallocate(GearStatus.Initial);
                }		

                inputNode.LastCfgModification = DateTime.Now;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputNode"></param>
        /// <returns></returns>
        public bool NodeSetForbidden(String Id)
        {
            try
            {
                lock (NodePool)
                {
                    if (NodePool.ContainsKey(Id))
                    {
                        NodePool[Id].Deallocate(GearStatus.Forbidden);
                    }
                    else
                    {
                        LogInfo<MNManager>("Node Not Found CFG",
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT,
                                   "MNManager", CTranslate.translateResource("Forbidden :" + Id));
                    }
                }
            }
            catch (Exception ex)
            {
                LogException<MNManager>("NodeSetForbidden", ex, false);
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputNode"></param>
        /// <returns></returns>
        public bool NodeSetAllocate(String Id)
        {
            try
            {
                lock (NodePool)
                {
                    if (NodePool.ContainsKey(Id))
                    {
                        NodePool[Id].Allocate();
                    }
                    else
                    {
                        LogInfo<MNManager>("Node Not Found CFG",
                            U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT,
                                   "MNManager", CTranslate.translateResource("Allocate :" + Id));
                    }
                }
            }
            catch (Exception ex)
            {
                LogException<MNManager>("NodeSetAllocate", ex, false);
            }
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override BaseGear NodeGet(BaseGear node)
        {
            lock (NodePool)
            {
                if (null != node && NodePool.ContainsKey(node.Id))
                    return NodePool[node.Id];
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected BaseGear NodeGet(String id)
        {
            lock (NodePool)
            {
                if (NodePool.ContainsKey(id))
                    return NodePool[id];
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        private void NodeRemove(BaseGear gear)
        {
            lock (NodePool)
            {
                if (NodePool.ContainsKey(gear.Id))
                {
                    gear.Deallocate(GearStatus.Forbidden);
                    NodePool.Remove(gear.Id);
                }
            }
        }
        /// <summary>
        /// Funcion de inicio del servicio del manager.
        /// </summary>
        protected override void StartManager()
        {
            LogInfo<MNManager>("Servicio Iniciado.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT, "MNManager", CTranslate.translateResource("Iniciado"));
            ExceptionManageInit();
            base.StartManager();
        }
        /// <summary>
        /// Funcion de finalización del servicio del manager, parametrizada por tipo de status de parada.
        /// </summary>
        protected override void StopManager()
        {
            LogInfo<MNManager>("Servicio Detenido.", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_EVENT, "MNManager", CTranslate.translateResource("Servicio detenido."));
            base.StopManager();

            lock (NodePool)
            {
                foreach (BaseGear gear in NodePool.Values)
                {
                    if (gear.Status == GearStatus.Forbidden) //20180709 #3565
                        continue;
                    gear.Deallocate(GearStatus.Initial);
                    //20180703 Se fuerza lectura potencia.
                    if (gear.IsEmitter && gear.IsMaster)
                        gear.Power = 0;
                }
            }
        }

        #endregion

        #region Logic - Handlers

        /// <summary>
        /// Recoge el Elapsed del timer del BaseManager, y lo utiliza para comprobar los nodos. 
        /// Y lanzar la algoritmica de validación de nodos en caso de que haya cambios.
        /// </summary>
        protected override void OnElapsed(object sender, ElapsedEventArgs e)
        {
            Timer tm = (Timer)sender;
            try
            {
                tm.Enabled = false;

                _validationIterationCount++;
                if (_validationIterationCount == Int32.MaxValue)
                    _validationIterationCount = 0;

                if (Status == ServiceStatus.Running)
                {
                    if (!ValidatePool())
                    {
                        LogError<MNManager>("Gestor NM no esta funcionando...", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR, "MNManager", CTranslate.translateResource("Gestor NM no esta funcionando..."));
                    }
                }

            }
            finally
            {
                tm.Enabled = true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <param name="status"></param>
        internal void OnGearAllocated(BaseGear gear, GearOperationStatus status)
        {
            switch (status)
            {
                case GearOperationStatus.OK:
#if DEBUG
                    LogTrace<MNManager>("[GEAR ALLOCATED] " + gear.ToString());
#endif
                    // 20160809. AGL. Acortar el texto...
                    // LogInfo<MNManager>("Equipo Asignado. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ALLOCATE_OK);
                    // 20160921. AGL. Se elimina este histórico.
                    //LogInfo<MNManager>("Equipo Asignado. " + gear.ToString(), 
                    //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ALLOCATE_OK,
                    //    "Equipo: " + gear.Id + ", Frecuencia: " + gear.Frecuency);

                    OnNodeAllocate.Invoke(gear);
                    break;

                // 20160921. AGL. TODO: Para configurar los Nodos por defecto en frecuencias sin cobertura de equipo slave.
                case GearOperationStatus.Fail:
                    OnNodeAllocate.Invoke(gear); // JOI 201709 NEWRDRP Antes comentado
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <param name="status"></param>
        internal void OnGearDeallocated(BaseGear gear, GearOperationStatus status)
        {
#if DEBUG
            LogTrace<MNManager>("[GEAR DEALLOCATE] " + gear.ToString());
#endif
            // 20160809. AGL. Acortar el texto...
            //LogInfo<MNManager>("Equipo liberado. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DEALLOCATE_OK);
            // 20160921. AGL. Se elimina este historico.
            //LogInfo<MNManager>("Equipo liberado. " + gear.ToString(), U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DEALLOCATE_OK,
            //    "Equipo: " + gear.Id);

            OnNodeDeallocate.Invoke(gear);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        internal void OnGearStatusUpdated(BaseGear gear)
        {
#if DEBUG
            LogTrace<MNManager>("[STATUS UPDATED] " + gear.ToString());
#endif
        }
        
        #region Logic - Handlers - Check

        /// <summary>
        /// Handler parar las respuestas asincronas de las validaciones de los equipos fisicos.
        /// Ver Remarks para definicion de logica del Algortimo.
        /// </summary>
        /// <remarks>
        /// Esta funcion es clave para mantener la integridad del pool de nodos y equipos.
        /// Tiene que validar el nuevo estado y en funcion del estado anterior actuar.
        /// 
        ///  - Operation Ok + Estado Assigned: Sin cambios.
        ///  - Operation Ok + Estado Ready: Sin cambios.
        ///  - Operation Ok + Estado Fail: Cambiar estado a Ready. Si es principal hay que buscar al Slave y apagarlo para pasar la responsabilidad a este.
        ///  
        ///  - Operation Ko + Estado Assigned: Error.
        ///  - Operation Ko + Estado Ready: Error.
        ///  - Operation Ko + Estado Fail: Sin cambios.
        ///  
        ///  - Operation Timeout + Estado Assigned: Error.
        ///  - Operation Timeout + Estado Ready: Error.
        ///  - Operation Timeout + Estado Fail: Sin cambios.
        /// 
        /// NOTA: Recordar la prioridad.
        /// </remarks>
        internal void OnGearChecked(BaseGear gear, GearOperationStatus status)
        {
            // NOTE: Ver Remarks!

            if (!OnGearCheckedContinue(gear, status))
                return;

            try
            {
            _semaphore.WaitOne();
              
                switch (status)
                {
                    case GearOperationStatus.OK:
                        OnGearCheckedOK(gear);
                        break;

                    case GearOperationStatus.Fail:
                        OnGearCheckedFail(gear);
                        break;

                    case GearOperationStatus.Timeout:
                        OnGearCheckedTimeout(gear);
                        break;

                    case GearOperationStatus.FailSessionsSip:
                    case GearOperationStatus.FailMasterConfig:
                        OnGearCheckedFail(gear, GearStatus.Forbidden);
                        gear.Deallocate(GearStatus.Forbidden);
                        break;

                    case GearOperationStatus.FailProtocolSNMP:
                        if (gear.IsKOSNMP)
                        {
                            OnGearCheckedFail(gear, GearStatus.Forbidden);
                            gear.Deallocate(GearStatus.Forbidden);
                            gear.IsKOSNMP = false;
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                LogException<MNManager>("OnGearChecked", ex, false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private Boolean OnGearCheckedContinue(BaseGear gear, GearOperationStatus status)
        {
            // 20161117. AGL. Parece ser que Status bloqueaba...
            GearStatus gearStatus = gear.Status;
            switch (status)
            {
                case GearOperationStatus.OK:
                    // Si Ok + Estado Assigned/Ready: Sin cambios.
                    if (gearStatus == GearStatus.Assigned || gearStatus == GearStatus.Ready)
                        return false;
                    break;

                case GearOperationStatus.Fail:
                    // Si Timeout/Ko + Estado Fail: Sin cambios.
                    if (gearStatus == GearStatus.Fail && gear.IsSlave) 
                        return false;
                    if (gearStatus == GearStatus.Fail && gear.IsMaster) 
                    {
                       if (!HaySlaveSinAsignar(gear))
                            return false;
                    }
                    break;

                case GearOperationStatus.Timeout:

                    if (gearStatus == GearStatus.Fail && gear.IsSlave) 
                        return false;
                    if (gearStatus == GearStatus.Fail && gear.IsMaster) 
                    {
                        if (!HaySlaveSinAsignar(gear))
                            return false;
                    }
                    break;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gear"></param>
        /// 
        /// <returns>bool</returns>
        private bool HaySlaveSinAsignar(BaseGear gear)
        {
            gear.OmiteKOsCount += 1;
            if (!gear.BuscaSlave)
                return false;

            gear.OmiteKOsCount = 0;
            if (SlaveAsignadoaOSinReservas(gear))
                return false;
#if DEBUG
            LogError<MNManager>("Gestor NM Hay Slave Sin Asignar ...", U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR, "MNManager",CTranslate.translateResource("Gestor NM Slave sin asignar..."));
#endif
            return true;
        }


        /// <summary>
        /// Algoritmo para agrupar la logica de una operación con resultado OK.
        /// </summary>
        private void OnGearCheckedOK(BaseGear gear)
        {
            if (gear.Status != GearStatus.Forbidden)       
            {
                // Historico de Equipo OK...
                LogInfo<MNManager>("[OPERATION " + GearOperationStatus.OK + "] " + gear.ToString(),
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_DISP, gear.Id);
            }
            else
            {
                // Historico de Equipo OK...
                LogInfo<MNManager>("[OPERATION " + "HABILITAR" + "] " + gear.ToString(),
                    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND, gear.Id, CTranslate.translateResource("Habilitado"));
            }

            // Temporalmente le asignamos el estado Ok que hemos recibido.
            gear.Status = GearStatus.Ready;
            gear.TimeoutsCount = 0;
            gear.KOsCount = 0;
            gear.OmiteKOsCount = 0;

            // El ultimo caso es que es un nodo que estaba caido y se ha levantado, y depende de si es un Master o un Slave su logica es diferente.
            if (gear.WorkingFormat == Tipo_Formato_Trabajo.Principal || gear.WorkingFormat == Tipo_Formato_Trabajo.Ambos)
                OnGearCheckedOKMaster(gear);
            else
                OnGearCheckedOKSlave(gear);
        }
        /// <summary>
        /// Algotirmo de Comprobacion de un Master que se levanta despues de haber estado Ko.
        /// </summary>
        private void OnGearCheckedOKMaster(BaseGear master)
        {
            // Si es principal, significa que puede que haya un slave que estuviera haciendo su funcion mientras el estaba caido.
            // Tenemos que buscar entre los slaves que esten funcionando de reemplazo, liberarle y asignar este. 
            BaseGear slave = NodeSlaveWorkingFind(master);

            // 20160921. Primero trabajamos con el ESCLAVO y Asignamos al MASTER para que los recursos SIP no generen errores.
            // Asignamos al maestro.
            // master.Allocate();

            // Y trabajamos con el esclavo que lo sustituia.
            if (null != slave)
            {
                // Tenemos que hacer deallocate si o si, para forzar el proceso de Allocate desde cero si es necesario.
                slave.Deallocate(GearStatus.Ready);

                // Comprobar si hay un Master caido que no este atendido y este slave pueda atender, en ese caso, asignar este Slave a dicho Master.
                BaseGear masterToReplace = NodeMasterToReplaceFind(slave);
                if (null != masterToReplace) // Found!
                    slave.Allocate(masterToReplace);
            }

            // 20160921. Primero trabajamos con el ESCLAVO y Asignamos al MASTER para que los recursos SIP no generen errores.
            // Asignamos al maestro.
            master.Allocate();
            //20180703 Se fuerza lectura potencia.
            if (master.IsEmitter == true)
                master.Power = 0;
        }
        /// <summary>
        /// Algotirmo de Comprobacion de un Slave que se levanta despues de haber estado Ko.
        /// </summary>
        private void OnGearCheckedOKSlave(BaseGear slave)
        {
            // Puede que haya un Master que necesite ser suplantado.
            BaseGear masterToReplace = NodeMasterToReplaceFind(slave);
            if (null != masterToReplace) // Found!
                slave.Allocate(masterToReplace);
        }

        /// <summary>
        /// Representa el algoritmo de comprobacion comun para cuando hay un Fail o un Timeout de un Equipo.
        /// </summary>
        private void OnGearCheckedKO(BaseGear gearKO, GearStatus deallocateStatus = GearStatus.Fail)
        {
            // Encontrar el elemento que hay que validar para ser reemplazado.
            BaseGear gearToReplace;
            if (gearKO.IsMaster) // Si el KO es Master, hay que buscar un reemplazo para el.
            {
                //20180917 #3698
                if (gearKO.ReplaceBy != null)
                {
                    // Ya esta siendo reemplazado
                    return;
                }
                //20180917 #3698 FIN
                gearToReplace = gearKO;
            }
            else // Si el KO es slave, hay que buscar un reemplazo para el elemento que el Slave reemplazaba (de haberlo).
            {
                if (gearKO.ReplaceTo == null)
                {
                    // 20160921. AGL. Si no esta reemplazando a nadie, no buscamos reemplazo...
                    // Si no está asignado, no lo desasignamos...
                    if (gearKO.Status == GearStatus.Assigned)
                        gearKO.Deallocate(deallocateStatus);
                    else
                        gearKO.Status = deallocateStatus;
                    return;
                }
                gearToReplace = gearKO.ReplaceTo;
            }

            // Encontrar reemplazo para el elemento KO.
            BaseGear replacement = null;
            if (null != gearToReplace)
                replacement = NodeReplacementFind(gearToReplace);
            
            // Ahora trabajamos con el reemplazo.
            if (null == replacement)
            {
                // 20160921. AGL. Historico de FRECUENCIA en ERROR...
                //LogWarn<MNManager>("No se ha encontrado un reemplazo valido para: " + gearKO.ToString(),
                //    U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_ALLOCATE_ERROR,
                //    "Equipo: " + gearKO.Id + "No se ha encontrado un reemplazo valido.");
                LogWarn<MNManager>("No se ha encontrado un reemplazo valido para: " + gearKO.ToString(),
                    gearKO.IsReceptor ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_ONERROR : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_ONERROR,

                    gearKO.Frecuency, "Equipo: " + gearKO.Id + " Emplazamiento: " + gearKO.IdEmplazamiento); 
                    // JOI FREC_DES FIN
                String KOFrecuency = gearKO.Frecuency;
                // 20160921. AGL. Deallocate antes de Allocate (para que los recursos SIP no generen errores...)
                // El nodo pasa a estar Fail en el gestor, y se intenta desconectar.
                gearKO.Deallocate(deallocateStatus);

                // 20160921. AGL. La frecuencia sin cobertura debe quedar asignada al equipo por defecto...
                AllocateDefault(KOFrecuency, gearKO.ResourceType, gearKO.IdEmplazamiento);

            }
            else
            {
                // 20160921. AGL. Deallocate antes de Allocate (para que los recursos SIP no generen errores...)
                // El nodo pasa a estar Fail en el gestor, y se intenta desconectar.
                gearKO.Deallocate(deallocateStatus);

                // Allocate.
                if (replacement.Status == GearStatus.Assigned)     
                {
                    // 20160921. AGL. Se está utilizando como Reemplazo un asignado de menor prioridad...
                    String replacementFrecuency = replacement.Frecuency;
                    LogWarn<MNManager>("Se desasigna por Prioridad: " + replacement.ToString(),
                        replacement.IsReceptor ? U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_RX_NONPRIORITY_ONERROR : U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_FRECUENCY_TX_NONPRIORITY_ONERROR,
                        replacement.Frecuency, "Equipo: " + replacement.Id);

                    replacement.Deallocate(GearStatus.AssignationInProgress);

                    // 20160921. AGL. La frecuencia sin cobertura debe quedar asignada al equipo por defecto...
                    AllocateDefault(replacementFrecuency, replacement.ResourceType, gearKO.IdEmplazamiento);
                 }
                replacement.Status = GearStatus.AssignationInProgress;
                replacement.Allocate(gearToReplace);
            }

            // 20160921. AGL. Deallocate antes de Allocate (para que los recursos SIP no generen errores...)
            // El nodo pasa a estar Fail en el gestor, y se intenta desconectar.
            // gearKO.Deallocate(deallocateStatus);
        }

        /// <summary>
        /// Algoritmo exclusivo de una operación Fail para un equipo. 
        /// Internamente llama a OnGearCheckedKO para completar el algoritmo.
        /// </summary>
        private void OnGearCheckedFail(BaseGear gear, GearStatus deallocateStatus = GearStatus.Fail)
        {
            gear.TimeoutsCount = 0;
            gear.KOsCount += 1;

            if (gear.IsKO || deallocateStatus == GearStatus.Forbidden)
            {
                if (deallocateStatus == GearStatus.Fail)
                {
                    // Historico de Equipo Caido...
                    LogInfo<MNManager>("[OPERATION " + GearOperationStatus.Fail + "] " + gear.ToString(),
                          U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_FAIL, gear.Id);
                }
                else
                {
                    // Historico Equipo Deshabilitado
                    LogInfo<MNManager>("[OPERATION " + "DESHABILITAR" + "] " + gear.ToString(),
                          U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_COMMAND_COMMAND,
                          gear.Id, CTranslate.translateResource("Deshabilitado"));
                }

                OnGearCheckedKO(gear, deallocateStatus);
            }
            else if (!gear.IsKO)
            {
                LogTrace<MNManager>("OnGearCheckedFail adelanta check " + gear.Id);
                gear.Check();
            }

        }

        /// <summary>
        /// Algoritmo exclusivo de un timeout para un equipo. Internamente llama a OnGearCheckedKO para completar el algoritmo.
        /// </summary>
        private void OnGearCheckedTimeout(BaseGear gear, GearStatus deallocateStatus = GearStatus.Fail)
        {
            gear.TimeoutsCount += 1;
            gear.KOsCount = 0;
            gear.OmiteKOsCount = 0; 

            // Historico de Equipo TIMEOUT, Caido o Deshabilitado...
            LogInfo<MNManager>("[OPERATION " + GearOperationStatus.Timeout + "] " + gear.ToString(),
                 U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GEAR_FAIL, gear.Id, " TIMEOUT");

            if (gear.IsTimeout)
                OnGearCheckedKO(gear, deallocateStatus);
            else
            {
                LogTrace<MNManager>("OnGearCheckedTimeout adelanta check " + gear.Id);
                gear.Check();
            }
        }

        #endregion

        #endregion

        #region Logic - Find Nodes

        /// <summary>
        /// Usado para determinar el pool de equipos que se ha de utilizar.
        /// <para>Compruba si el elemento es emisor o receptor, su tipo de frecuencia (Single, HF, VHF, UHF) y la forma de trabajar (Master, Slave).</para>
        /// </summary>
        private IEnumerable<BaseGear> NodePoolSelect(BaseGear node, Tipo_Formato_Trabajo workingFormat = Tipo_Formato_Trabajo.Principal)
        {
            lock (NodePool)
            {
                IEnumerable<BaseGear> output = NodePool.Values
                    .Where(e =>
                        (e.WorkingFormat == Tipo_Formato_Trabajo.Ambos || e.WorkingFormat == workingFormat)
                        && (e.IsEmitter == node.IsEmitter || e.IsReceptor == node.IsReceptor)
                        && (e.FrecuencyType == node.FrecuencyType))
                    .OrderBy(e => e.Priority);
                return output;
            }
        }
        /// <summary>
        /// Usado para encontrar un esclavo que este trabajando en la frecuencia del maestro enviado.
        /// </summary>
        private BaseGear NodeSlaveWorkingFind(BaseGear master)
        {
            BaseGear output = NodePoolSelect(master, Tipo_Formato_Trabajo.Reserva)
                .Where(e =>
                    e.Status == GearStatus.Assigned
                    // JOI FREC_DES
                    //&& e.Frecuency == master.Frecuency)
                    && e.Frecuency == master.Frecuency
                    && e.IdEmplazamiento == master.IdEmplazamiento)
                    // JOI FREC_DES FIN
                .FirstOrDefault();
#if DEBUG
            if (null == output)
                LogTrace<MNManager>("[LOOKING FOR SLAVE] NOT found. " + master.ToString());
            else
                LogTrace<MNManager>("[LOOKING FOR SLAVE] FOUND. [Master: " + master.ToString() + "] [Slave: " + output.ToString() + "]");
#endif
            return output;
        }
        /// <summary>
        /// Usado para encontrar un principal que puede ser reemplazado por el esclavo que recibe.
        /// </summary>
        private BaseGear NodeMasterToReplaceFind(BaseGear slave)
        {
            BaseGear output = NodePoolSelect(slave, Tipo_Formato_Trabajo.Principal)
                .Where(e =>
                    !e.IsAvailable
                    && null == e.ReplaceBy
                    // JOI FREC_DES
                    // && FrecuencyCheck(slave, Convert.ToInt32(e.Frecuency.Replace(".",""))))
 
                    && FrecuencyCheck(slave, Convert.ToInt32(e.Frecuency.Replace(".", "")))
                    && e.IdEmplazamiento == slave.IdEmplazamiento)
                    // JOI FREC_DES FIN
                .FirstOrDefault();
#if DEBUG
            if (null == output)
                LogTrace<MNManager>("[SLAVE LOOKING FOR MASTER] NOT found. [Slave: " + slave.ToString() + "]");
            else
                LogTrace<MNManager>("[SLAVE LOOKING FOR MASTER] FOUND. [Slave: " + slave.ToString() + "] [Master: " + output.ToString() + "]");
#endif
            return output;
        }
        /// <summary>
        /// Usado para encontrar un Nodo por su Id entre todos los nodos.
        /// </summary>
        private BaseGear NodeFindById(String nodeId)
        {
            lock (NodePool)
            {
                return NodePool.Values
                    .Where(e => e.Id == nodeId)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// 20160921. AGL. Para localizar los equipos por defecto de las frecuencias...
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="tp"></param>
        /// <returns></returns>
        // JOI FREC_DES
        // private BaseGear NodeMasterByFrecuency(String frecuency, RdRsType tp)
        private BaseGear NodeMasterByFrecuency(String frecuency, RdRsType tp, String idEmplazamiento)           
        //JOI FREC_DES FIN
        {
            lock (NodePool)
            {
                return NodePool.Values
                    //JOI FREC_DES
                    //.Where(e => e.IsMaster && e.ResourceType==tp && e.FrecuencyMain==frecuency)
                    .Where(e => e.IsMaster && e.ResourceType == tp && e.FrecuencyMain == frecuency && e.IdEmplazamiento == idEmplazamiento)
                    //JOI FREC_DES FIN
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Busca entre los esclavos un reemplazo para el nodo enviado.
        /// </summary>
        /// <param name="input">Recibe el Gear que necesita un reemplazo</param>
        /// <remarks>
        /// Buscar entre los Slave incluidos los "en uso".
        ///     Si se localiza uno vacio se utiliza. 
        ///     Si no hay vacios pero se ha localizado uno ocupado con menos prioridad, se desocupa y se asigna este.
        /// </remarks>
        //private BaseGear NodeReplacementFind(BaseGear input)
//        {
//            if (null == input)
//                return null;

//            BaseGear foundButAssigned = null;

//            // Buscar entre el pool de reservas validas.
//            foreach (BaseGear gear in NodePoolSelect(input, Tipo_Formato_Trabajo.Reserva))
//            {
//                // JOI FREC_DES
//                if (gear.IdEmplazamiento != input.IdEmplazamiento)
//                    continue;
//                // JOI FREC_DES FIN

//                // Si equipo en fallo, siguiente.
//                if (!gear.IsAvailable)
//                    continue;

//                // Distinto del padre
//                if (gear.Id == input.Id)
//                    continue;

//                // Si el equipo no puede soportar esa frecuencia, sal.
//                if (!String.IsNullOrEmpty(input.Frecuency) 
//                        && !FrecuencyCheck(gear, Convert.ToInt32(input.Frecuency.Replace(".", ""))))
//                    continue;
//                if (gear.Status == GearStatus.Fail)
//                {
//                    continue;
//                }

//                // Si frecuencia valida y estado preparado, localizado.
//                if (gear.Status == GearStatus.Ready || gear.Status == GearStatus.Initial)
//                {
//#if DEBUG
//                    LogTrace<MNManager>("[REPLACEMENT FOUND] " + input.ToString() + ": " + gear.ToString());
//#endif
//                    return gear;
//                }

//                // Si hemos encontrado uno valido y esta asignado, pero es mas importante (recordamos que cuanto menos prioridad mas importante), seleccionar temporalmente.
//                if (gear.Status == GearStatus.Assigned && input.Priority < gear.Priority)
//                    foundButAssigned = gear;        
//            }
//#if DEBUG
//            if (null != foundButAssigned)
//                LogTrace<MNManager>("[REPLACEMENT FOUND] " + input.ToString() + ": " + foundButAssigned.ToString());
//            else
//                LogTrace<MNManager>("[REPLACEMENT NOT FOUND] " + input.ToString() + ".");
//#endif
//            return foundButAssigned;            
//        }
        private BaseGear NodeReplacementFind(BaseGear input)
        {
            if (null == input)
                return null;

            BaseGear foundButAssigned = null;
            // Buscar entre el pool de reservas validas.
            foreach (BaseGear gear in NodePoolSelect(input, Tipo_Formato_Trabajo.Reserva))
            {
                // JOI FREC_DES
                if (gear.IdEmplazamiento != input.IdEmplazamiento)
                    continue;
                // JOI FREC_DES FIN

                // Si equipo en fallo, siguiente.
                if (!gear.IsAvailable)
                    continue;

                // Distinto del padre
                if (gear.Id == input.Id)
                    continue;

                // Si el equipo no puede soportar esa frecuencia, sal.
                if (!String.IsNullOrEmpty(input.Frecuency)
                        && !FrecuencyCheck(gear, Convert.ToInt32(input.Frecuency.Replace(".", ""))))
                    continue;
                if (gear.Status == GearStatus.Fail)
                {
                    continue;
                }
                // Si hemos encontrado uno valido y esta asignado, pero es mas importante (recordamos que cuanto menos prioridad mas importante), seleccionar temporalmente.
                if ((gear.Status == GearStatus.Assigned || gear.ReplaceTo != null) && input.Priority < gear.Priority)
                    foundButAssigned = gear;

                // Si frecuencia valida y estado preparado, localizado.
                // No se admite GearStatus.Initial porque no está bien gestionado casos 
                // de N apagado en los arranques de Nodebox
                if (gear.Status == GearStatus.Ready /*|| gear.Status == GearStatus.Initial*/)
                {
#if DEBUG
                    LogTrace<MNManager>("[REPLACEMENT FOUND] " + input.ToString() + ": " + gear.ToString());
#endif
                    return gear;
                }
            }
#if DEBUG
            if (null != foundButAssigned)
                LogTrace<MNManager>("[REPLACEMENT FOUND] " + input.ToString() + ": " + foundButAssigned.ToString());
            else
                LogTrace<MNManager>("[REPLACEMENT NOT FOUND] " + input.ToString() + ".");
#endif
            return foundButAssigned;
        }

        /// <summary>
        /// Busca entre los esclavos si ya esta reemplazado.
        /// </summary>
        /// <param name="input">Recibe el Gear que necesita un reemplazo</param>
        /// <remarks>
        /// Buscar entre los Slave incluidos el que puede estar ya asignado al Master.
        ///     Si se localiza  o input == null retorna true. 
        ///     Si no se localiza pero hay vacios retorna false.
        ///     Se implementa esta función para poder minimizar la incidencia de que habiendo slaves libres
        ///     no se asigne un master en fallo. Apagado Master y Slave en el mismo segundo...
        /// </remarks>
        private bool SlaveAsignadoaOSinReservas(BaseGear input)
        {
            bool HayReservas = false;
            bool bAsignado = false;
            if (null == input)
                return true;

            // Buscar entre el pool de reservas.
            foreach (BaseGear gear in NodePoolSelect(input, Tipo_Formato_Trabajo.Reserva))
            {
                // JOI FREC_DES
                if (gear.IdEmplazamiento != input.IdEmplazamiento)
                    continue;
                // JOI FREC_DES FIN 

                if (gear.Frecuency == input.Frecuency && gear.Status == GearStatus.Assigned)
                {
                    bAsignado = true;
                    break;
                }
                // No se admite GearStatus.Initial porque no está bien gestionado casos 
                // de N apagado en los arranques de Nodebox
                if (gear.Status == GearStatus.Ready /*|| gear.Status == GearStatus.Initial*/)
                {
                   HayReservas = true;
                   break;
                }    
                if ((gear.Status == GearStatus.Assigned) && (gear.Priority > input.Priority))
                {
                    //Hay un reserva asignado a otra frecuencia con menos prioridad
                    HayReservas = true;
                    break;
                }    
            }
            if (bAsignado == true || HayReservas == false)
                return true;

            return false;
        }

        /// <summary>
        /// Busca entre los esclavos si esta ya reemplazado para el nodo enviado.
        /// </summary>
        /// <param name="input">Recibe el Gear que necesita un reemplazo</param>
        /// <remarks>
        /// Buscar entre los Slave incluidos los "en uso".
        ///     Si se localiza uno que lo esta reemplazando retorna true. 
        ///   
        /// </remarks>
        private bool NodeReplacementAssignedToMaster(BaseGear input)
        {
            Boolean bRemplazado = false;
            if (null == input)
                return false;
            // Buscar entre el pool de reservas validas.
            foreach (BaseGear gear in NodePoolSelect(input, Tipo_Formato_Trabajo.Reserva))
            {
                if (gear.ReplaceTo == null || input.ReplaceBy == null)
                    continue;
                if (gear.ReplaceTo.Id == input.Id && 
                    gear.Frecuency == input.Frecuency && 
                    gear.IdEmplazamiento == input.IdEmplazamiento)
                {
                    //Encontrado el esclavo que lo reemplaza
                    bRemplazado = true;
                    break;
                }
            }
            return bRemplazado;
        }

        #endregion

        /// <summary>
        /// Usada para lanzar las peticiones asincronas de validacion de un equipo. Las acciones dependiendo del resultado del mismo se hacen en los Handlers.
        /// Adicionalmente itera por los nodos que han sido modificados y realiza las operaciones con ellos neecesarias.
        /// </summary>
#if DEBUG
        static int tick_count=0;
#endif
        public bool ValidatePool()
        {
            if (Status != ServiceStatus.Running)
                return false;

            System.Threading.Thread.CurrentThread.Name = "ValidatePool";
            try
            {
                _semaphorePool.WaitOne();
                lock (NodePool)
                {
                    // Lanzamos la peticion de obtener el estado real de los equipos fisicos.
#if !_MNMANAGER_V0_
                    foreach (BaseGear gear in NodePool.Values.Where(e => e.Status != GearStatus.Forbidden))
                    {
                        if (gear.CanValidate)
                        {
                            gear.Check();
                        }
                    }
#else
                    if (tick_count % 2 == 0)
                    {
                        foreach (BaseGear gear in NodePool.Values.Where(e => e.Status != GearStatus.Forbidden && e.IsMaster==true))
                        {
                            if (gear.CanValidate)
                            {
                                gear.Check();
                            }
                        }
                    }
                    else
                    {
                        foreach (BaseGear gear in NodePool.Values.Where(e => e.Status != GearStatus.Forbidden && e.IsMaster == false))
                        {
                            if (gear.CanValidate)
                            {
                                gear.Check();
                            }
                        }
                    }

#endif

#if DEBUG
                    if ((++tick_count) % 10 == 0)
                        LogInfo<MNManager>("MNManager-ValidatePool");
#endif
                }

                // Finalizar la validación.
                LastValidation = DateTime.Now;
                return true;
            }
            catch (Exception x)
            {
                LogException<MNManager>("ValidatePool", x, false);
            }
            finally
            {
                _semaphorePool.Release();
            }
            return true;
        }
        /// <summary>
        /// Algoritmo de validacion para un Nodo de una frecuencia.
        /// </summary>
        private bool FrecuencyCheck(BaseGear gearToCheck, int frecuencyToCheck)
        {
            if (null == gearToCheck.FrecuenciesAllowed || gearToCheck.FrecuenciesAllowed.Count == 0)
                return true;

            return (
                gearToCheck.FrecuenciesAllowed
                    .Where(frecuencyRange => 
                        frecuencyToCheck >= frecuencyRange.fmin 
                        && frecuencyToCheck <= frecuencyRange.fmax)
                    .FirstOrDefault() != null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frecuency"></param>
        /// <param name="tipo"></param>
        private void AllocateDefault(String frecuency, RdRsType tipo, String idEmplazamiento)
        {
            BaseGear defaultGear = NodeMasterByFrecuency(frecuency, tipo, idEmplazamiento);
            if (defaultGear == null)
            {
                LogWarn<MNManager>("No se encuentra Equipo por defecto para la frecuencia: " + frecuency + " en " + tipo,
                   U5kiIncidencias.U5kiIncidencia.U5KI_NBX_NM_GENERIC_ERROR, "MNManager",
                   CTranslate.translateResource("No se encuentra equipo por defecto para la frecuencia: " + frecuency + " en " + 
                   (tipo == RdRsType.Rx ? "Recepcion" : "Transmision") + " Emplazamiento: " + idEmplazamiento));

            }
            else
            {
                OnGearAllocated(defaultGear, GearOperationStatus.Fail);
            }
        }


        #endregion

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            ClearPool();
            StopManager();
            base.Dispose();
        }
    }
}

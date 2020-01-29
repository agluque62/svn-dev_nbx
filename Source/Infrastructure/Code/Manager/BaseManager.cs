using System;
using Utilities;

namespace U5ki.Infrastructure
{

    /// <summary>
    /// Clase basica del arbol de herencias de los managers.
    /// </summary>
    public abstract class BaseManager : BaseCode, IDisposable
    {

        #region Declarations

        /// <summary>
        /// El nombre del Manager.
        /// </summary>
        /// <remarks>
        /// Hay que sobreescribir esta propiedad con el nombre concreto desde un archivo de recursos.
        /// </remarks>
        public abstract string Name { get; }

        /// <summary>
        /// Marks the actual status of the element.
        /// Stopped by default.
        /// </summary>
        public ServiceStatus Status { get; set; }
        

        private EventQueue _workingQueue;
        /// <summary>
        /// Utilizada para no bloquear todas las comunicaciones ni el hilo del manager. Simple Queue de acciones.
        /// </summary>
        public EventQueue WorkingQueue 
        { 
            get 
            {
                if (null == _workingQueue)
                    _workingQueue = new EventQueue();
                return _workingQueue; 
            } 
        }
        

        /// <summary>
        /// Gets/Sets si el este Manager es principal. 
        /// </summary>
        /// <remarks>
        /// Propiedad para soportar la funcionalidad de que solo un elemento de la red puede hacer de Master, 
        /// mientras que varios Nodebox pueden estar levantados con uno o varios Managers a la espera.
        /// </remarks>
        public bool Master { get; set; }

        #endregion

        /// <summary>
        /// Funcion de inicio del servicio del manager.
        /// </summary>
        protected virtual void StartManager()
        {
            Status = ServiceStatus.Running;
            WorkingQueue.Start();
        }
        /// <summary>
        /// Funcion de finalización del servicio del manager, parametrizada por tipo de status de parada.
        /// </summary>
        internal virtual void StopManager(ServiceStatus status)
        {
            /** 20180712. AGL. Correccion Error en Servicio Radio SLAVE se LLama WorkingQueue.InternalStop() sin haberlo arrancado lo que provocaba una execpcion */
            bool MustStop = Status == ServiceStatus.Running;

            Status = status;
            if (MustStop) WorkingQueue.InternalStop();
        }
        /// <summary>
        /// Funcion de finalización del servicio del manager.
        /// </summary>
        protected virtual void StopManager()
        {
            StopManager(ServiceStatus.Stopped);
        }

        public virtual void Dispose()
        {
            StopManager(ServiceStatus.Disabled);
        }

    }
}

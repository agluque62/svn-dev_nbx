using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Enums;

namespace U5ki.Infrastructure
{

    /// <summary>
    /// Interface con las llamadas basicas de un Gestor/Manager.
    /// </summary>
    /// <remarks>
    /// La funcionalidad minima de cualquier pool manager tiene que ser a groso modo, la de obtener y asignar elementos al pool.     
    /// </remarks>
    public interface IPoolManager<keyType, nodeType, configurationType>
    {

        /// <summary>
        /// Funcion para hacer iniciar el Pool.
        /// </summary>
        void Start();
        /// <summary>
        /// Funcion para parar el trabajo.
        /// </summary>
        void Stop();

        /// <summary>
        /// Property that will hold the information of the pool. The key.
        /// </summary>
        IDictionary<keyType, nodeType> NodePool { get; set; }

        /// <summary>
        /// Funcion para la validación de una nueva configuración, frente la que esta corriendo, y tomar todas las accciones oportunas.
        /// </summary>
        void UpdatePool(configurationType configuration);

        /// <summary>
        /// Funcion para hacer un borrado del pool.
        /// </summary>
        void ClearPool();
		
		/// <summary>
        /// Funcion para controlar la asignación un Slave a un Master con cambios en configuración y en fallo.
        /// </summary>
        ///
        //20180208 #3136
        void ControlSlave(String id);
        
        /// <summary>
        /// Funcion publica para utilizar como comando remoto para cmabiar el estado de un elemento.
        /// </summary>
        Boolean NodeToogle(String id);
        /// <summary>
        /// Funcion publica para utilizar como comando remoto para activar un elemento de forma manual.
        /// </summary>
        Boolean NodeAllocate(String id, String frecuency = null, GearStatus? mandatoryStatus = null);
        /// <summary>
        /// Funcion publica para utilizar como comando remoto para desactivar un elemento de forma manual.
        /// </summary>
        Boolean NodeDeallocate(String id);        

    }


}

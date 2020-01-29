using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U5ki.Infrastructure
{

    /// <summary>
    /// Clase abstracta que engloba el perfil obligatorio para un manager de nodos.
    /// </summary>
    /// <typeparam name="T">El tipo de nodo que maneja el manager.</typeparam>
    public abstract class NodeManager<TManaged, TInput> : BaseTimeManager
    {

        /// <summary>
        /// Parsea un nodo de tipo Y en tipo T.
        /// </summary>
        protected abstract TManaged NodeParse(TInput node);

        /// <summary>
        /// Función basica de asignación de un nodo existente o nuevo.
        /// </summary>
        protected abstract bool NodeSet(TManaged node);
        /// <summary>
        /// Función basica de optención de información de un nodo de la session.
        /// </summary>
        protected abstract TManaged NodeGet(TManaged node);

    }
}

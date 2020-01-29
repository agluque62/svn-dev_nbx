using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Enums;
using U5ki.Infrastructure;

namespace u5ki.RemoteControlService
{

    /// <summary>
    /// Clase que engloba las funcionalidades publicas del Telemando o Control Remoto.
    /// Funciona como un IService normal, que escucha un puerto interno y utiliza para la comunicación UdpClient.
    /// </summary>
    /// <remarks>
    /// La funcionalidad interna del control remoto basicamente funciona como un Getter/Setter, 
    /// con funcion adicional para hacer mas sencillo el uso del Interfaz.
    /// 
    /// La propia función de Get y Set inicialmente se va a excluir del Interfaz y se va a llevar al sistema de herencia de objetos, para que los metodos sean protected.
    /// 
    /// TODO: En la funciones simples, sustituir el objeto Node por parametros concretos haciendo overload.
    /// </remarks>
    public interface IRemoteControl
    {
        /// <summary>
        /// 20160809. Para Relacionarlos con los GEAR de alto nivel...
        /// </summary>
        string Id { get; set; }

        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        // Funciones Getter simplificadas

        /// <summary>
        /// Funcion que nos devuelve si el nodo esta funcional y funcionando como se espera de el.
        /// </summary>
        void CheckNode(Action<GearOperationStatus> response, BaseNode node);

        /// <summary>
        /// Funcion que nos devuelve si el nodo esta funcional y funcionando como se espera de el.
        /// </summary>
        void FrecuencyGet(Action<String> response, BaseNode node);

        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        // Funciones Setter simplificadas

        /// <summary>
        /// La funcion basica para sintonizar un nodo.
        /// </summary>
        void ConfigureNode(RCConfigurationAction action, Action<GearOperationStatus> response, BaseNode node, Boolean isEmitter, Boolean isMaster);

    }
}

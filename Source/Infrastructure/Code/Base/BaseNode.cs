using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Enums;
using U5ki.Infrastructure.Properties;
using U5ki.Infrastructure.Resources;

namespace U5ki.Infrastructure
{

    /// <summary>
    /// Nodo del pool de objetos que va representar un elemento de comunicación de la red. 
    /// Proporciona el listado de metodos abstractos que cualquier Nodo de la Session de objetos de comunicación debe de tener.
    /// </summary>
    /// <remarks>
    /// Utilizado para crear un pool generico, y operar de forma abstracta con el pool, independientemente del tipo. 
    /// </remarks>
    public class BaseNode : BaseDto
    {
        public string Id { get; set; }

        /// <summary>
        /// Ip donde hay que comunicar con el nodo. Puede ser un gestor intermedio pero ese ha de tener configurado el nodo final.
        /// </summary>
        /// <remarks>
        /// Ip del nodo donde hay que comunicar los SNMP.
        /// Old IpRcs.
        /// </remarks>
        public string IP { get; set; }

        /// <summary>
        /// Representa el puerto a traves del que se hace la comunicación con el equipo. 
        /// </summary>
        /// <remarks>
        /// Por defecto suele ser 161. Pero nos hemos encontrado con que los receptores llevan un 160. 
        /// </remarks>
        public Int32 Port { get; set; }
        //02012017 JOI: control tipo de Equipo; Transmisor/Receptor;
        /// <summary>
        /// Representa el tipo de equipo. 
        /// </summary>
        /// <remarks>
        /// EsTransmisor false (receptor); True (transmisor-Transceptor). 
        /// </remarks>
        public bool IsEmitter { get; set; }
        //02012017 JOI: control tipo de Equipo; Transmisor/Receptor; FIN

        //JOI: CONTROL FRECUENCIA;
        /// <summary>
        /// Representa el tipo de equipo. 
        /// </summary>
        /// <remarks>
        /// EsTransmisor false (receptor); True (transmisor-Transceptor). 
        /// </remarks>
        public bool IsMaster { get; set; }
        //JOI: CONTROL FRECUENCIA; FIN

        /// <summary>
        /// Frecuencia a la que esta operando este nodo en este momento. 
        /// </summary>
        /// <remarks>
        /// Esta propiedad representa la cambiante frecuencia que puede asignarse y se asigna a un nodo. Hay que limpiarla al desasignar el nodo.
        /// </remarks>
        public virtual string Frecuency { get; set; }
        
        /// <summary>
        /// TODO: Set Summary
        /// </summary>
        public GearCarrierOffStatus Offset { get; set; }
        /// <summary>
        /// TODO: Set Summary
        /// </summary>
        public GearChannelSpacings Channeling { get; set; }
        /// <summary>
        /// TODO: Set Summary
        /// </summary>
        public GearModulations Modulation { get; set; }
        /// <summary>
        /// TODO: Set Summary
        /// </summary>
        public GearPowerLevels PowerLevel { get; set; }
        /// <summary>
        /// TODO: Set Summary
        /// </summary>
        public Int32? Power { get; set; }
        
        /// <summary>
        /// Representa la ultima vez que un cambio en la configuracion ha modificado este elemento.
        /// </summary>
        public DateTime LastCfgModification { get; set; }

        /// <summary>
        /// Representa la ultima vez que un cambio en el estado del elemento.
        /// </summary>
        public DateTime LastStatusModification { get; set; }
        
        /// <summary>
        /// Estado actual del equipo. 
        /// </summary>
        public virtual GearStatus Status { get; set; }
        
        /// <summary>
        /// Tipo de equipo fisico que tiene este element.
        /// </summary>
        public RCTypes RemoteControlType { get; set; }      

        public BaseNode() { }
        /// <summary>
        /// Utiliza el objeto Proto de tipo Nodo para llevar las propiedades al Nodo concreto.
        /// </summary>
        public BaseNode(Node input)
        {
            if (null == input)
                LogFatal<BaseNode>(String.Format(Errors.ElementCannotBeNull, "Node input", "BaseNode.BaseNode"));

            Id = input.Id;
            IP = input.IpGestor;
            Frecuency = input.FrecuenciaPrincipal;

            LastStatusModification = DateTime.Now;
            //02012017 JOI: control tipo de Equipo;
            IsEmitter = input.EsTransmisor;
            //JOI: CONTROL FRECUENCIA;
            IsMaster = (input.FormaDeTrabajo == Tipo_Formato_Trabajo.Principal) ? true : false;
        }

    }

}

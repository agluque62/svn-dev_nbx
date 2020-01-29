using System;
using System.Collections.Generic;
using System.Text;

using Utilities;

namespace U5ki.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
	public enum ServiceStatus { Running, Stopped, Disabled }

    /**
     * AGL 20120706. Para la interfaz de control de los servicios
     * */
    /// <summary>
    /// 
    /// </summary>
    public enum ServiceCommands 
    { 
        SetDefaultCfg, 
        LoadDefaultCfg, 
        DelDefaultCfg, 
        ListDefaultCfg, 
        GetDefaultConfigId, 
        RdHFGetEquipos, 
        RdHFGetEstadoEquipo, 
        RdHFLiberaEquipo,
        RdSessions,                 // AGL 20160208. Lista de Sesiones Radio
        RdMNStatus,                 // 20160928. AGL.
        RdMNReset,
        RdMNGearListGet,            //  Lista de Equipo en M+N Gestionados.
        RdMNGearToogle,             //  Habiliar / Deshabilitar Equipo
        RdMNGearAssign,             //  Asignar Equipo.
        RdMNGearUnassing,           //  Desasignar Equipo.
        RdMNValidationTick,         //  Configura el valor del tick de validacion del pool.

        TifxDataGet,

        SrvDbg                      // Generico para pruebas en los servicios....
    }

    /**
     * Fin de la modificacion */

    /// <summary>
    /// 
    /// </summary>
	public interface IService
	{
		string Name { get; }
		ServiceStatus Status { get; }

		void Start();
		void Stop();

        /**
         * AGL 20120706. Para la Interfaz de control de los servicios.
         * */
        bool Master { get; }
        bool Commander(ServiceCommands cmd, string par, ref string err, List<string> resp=null);
        /**
         * Fin de la modificacion */
        /** 20170217. AGL. Nueva interfaz de comandos. Orientada a estructuras definidas en 'Infraestructure' */
        bool DataGet(ServiceCommands cmd, ref List<Object> rsp);
        /** Fin de la Modificacion */
	}
}

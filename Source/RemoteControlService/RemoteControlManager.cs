using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Resources;
using Utilities;

namespace RemoteControl
{

    /// <summary>
    /// Esta es la clase que engloba la funcionalidad principal de control remoto o Telemando.
    /// </summary>
    /// <remarks>
    /// Implementa IRemoteControl, que a su vez implementa IService, 
    /// con lo que la instanciación y lanzamiento del servicio de escucha, y de  se hace automaticamente si se sigue el protocolo de despliegue de IServices.
    /// </remarks>
    public class RemoteControlManager : BaseManager, IRemoteControl
    {

        #region Declarations

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public override string Name { get { return U5ki.Infrastructure.Resources.ServiceNames.RemoteControlService; } }

        #endregion

        public RemoteControlManager()
        {
        }

        #region Logic

        #region Logic - IRemoteControl

        public BaseNode Initialize(Object configuration)
        {
            throw new NotImplementedException();

            //TODO: Recibir por config si es Master o no.
        }

        public bool PingNode(BaseNode node, long timeOutSeconds)
        {
            throw new NotImplementedException("Queda pendiente de definir el funcionamiento de esta parte.");
        }

        public bool ValidateNode(BaseNode node)
        {
            throw new NotImplementedException("Queda pendiente de definir el funcionamiento de esta parte.");
        }

        public bool TuneIn(BaseNode node)
        {
            throw new NotImplementedException("Queda pendiente de definir el funcionamiento de esta parte.");
        }

        #endregion

        #endregion

    }
}

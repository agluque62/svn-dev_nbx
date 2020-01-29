using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code.Base;

namespace u5ki.RemoteControlService
{

    /// <summary>
    /// Esta clase va a ser utilizada en el futuro como una factoria de IRemoteControls, 
    /// cada Nodo tendra una propiedad de configuracion que determinara el tipo de "Telemando" que utiliza. 
    /// 
    /// NOTA: Para la App el tipo se convertira asi en transparente y nos evitamos tener que llevar una gestion manual de RCs.
    /// </summary>
    /// <remarks>
    /// Singletone Pattern cuando se instancie esta clase.
    /// Factory Pattern.
    /// </remarks>
    public class RemoteControlFactory : BaseCode, IBaseFactory<IRemoteControl>
    {

        public IRemoteControl ManufactureOne(string id, RCTypes input, Int32 port)
        {
            switch (input)
            {
                case RCTypes.RCRohde4200:
                    return new RCRohde4200(port) { Id = id };

                case RCTypes.RCJotron7000:
                    return new RCJotron7000(port) { Id = id };

                case RCTypes.RCNDFSimulado:
                    return new RCNDFSimulado() { Id = id };

                default:
                    throw new NotImplementedException("RemoteControlFactory no implementa la creación de este type de elemento: " + input.ToString());
            }
        }
        public IRemoteControl ManufactureOne(BaseDto input)
        {
            if (!(input is BaseNode))
                throw new NotImplementedException("RemoteControlFactory ManufactureOne BaseDto must be BaseNode");

            BaseNode inputParsed = (BaseNode)input;
            
            return ManufactureOne(inputParsed.Id, inputParsed.RemoteControlType, inputParsed.Port);
        }

    }
}

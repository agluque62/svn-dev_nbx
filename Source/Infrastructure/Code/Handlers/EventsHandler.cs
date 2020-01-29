using System;
using U5ki.Delegates;
using U5ki.Enums;
using U5ki.Infrastructure.Exceptions;

namespace U5ki.Infrastructure.Handlers
{
    public class EventsHandler
    {
        
        private static event StringDelegate OnMessage;
        
        #region Event Logic

        private void SuscribeEvent(GlobalEventTypes type, Object suscriber)
        {
            try
            {
                switch (type)
                {
                    case GlobalEventTypes.OnMessage:
                        OnMessage += (StringDelegate)suscriber;
                        break;

                    default:
                        throw new NotImplementedException("Suscribe does not implement this type of event");
                }
            }
            catch (Exception ex)
            {
                if (ex is NotImplementedException)
                    throw;
                throw new EventException(ex);
            }
        }
        public void Suscribe(GlobalEventTypes type, StringDelegate suscriber)
        {
            switch (type)
            {
                case GlobalEventTypes.OnMessage:
                    SuscribeEvent(type, suscriber);
                    break;

                default:
                    throw new NotImplementedException("Suscribe [StringDelegate] does not implement this type of event");
            }
        }

        private Boolean TriggerEvent(GlobalEventTypes type, Object input)
        {
            Boolean output = false;
            try
            {
                switch (type)
                {
                    case GlobalEventTypes.OnMessage:
                        if (null == input)
                            throw new NotImplementedException("Trigger OnMessage input cannot be null");
                        if (null != OnMessage)
                        {
                            OnMessage.Invoke(input.ToString());
                            output = true;
                        }
                        break;

                    default:
                        throw new NotImplementedException("Trigger does not implement this type of event");
                }
            }
            catch (Exception ex)
            {
                if (ex is NotImplementedException)
                    throw;
                throw new EventException(ex);
            }
            return output;
        }
        public Boolean Trigger(GlobalEventTypes type, String input)
        {
            switch (type)
            {
                case GlobalEventTypes.OnMessage:
                    return TriggerEvent(type, input);

                default:
                    throw new NotImplementedException("Suscribe [StringDelegate] does not implement this type of event");
            }
        }

        #endregion

    }
}

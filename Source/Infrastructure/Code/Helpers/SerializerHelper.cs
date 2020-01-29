using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace U5ki.Infrastructure.Helpers
{

    public class SerializerHelper : BaseHelper
    {

        #region Declarations

        private XMLSerializer _xMLSerializer;
        public XMLSerializer XML
        {
            get
            {
                if (null == _xMLSerializer)
                    _xMLSerializer = new XMLSerializer();
                return _xMLSerializer;
            }
        }

        private SNMPSerializer _sNMPSerializer;
        public SNMPSerializer SNMP
        {
            get
            {
                if (null == _sNMPSerializer)
                    _sNMPSerializer = new SNMPSerializer();
                return _sNMPSerializer;
            }
        }

        #endregion

        #region XML -> DTOs

        public class XMLSerializer
        {

            public IList<T> Deserialize<T>(String fileName, String baseAttribute)
            {
                FileStream stream = File.OpenRead(fileName);

                //XmlSerializer xmlSerializer = new XmlSerializer(typeof(Gears));
                //Gears gears = (Gears)xmlSerializer.Deserialize(stream);
                //stream.Close();

                XmlSerializer mySerializer = new XmlSerializer(typeof(T[]), new XmlRootAttribute(baseAttribute));
                T[] elements = (T[])mySerializer.Deserialize(stream);
                stream.Close();

                return elements;
            }

        }

        #endregion

        #region SNMP -> DTOs

        public class SNMPSerializer
        {

            public class OnMessageRecievedDto
            {
                /// <summary>
                /// OID.
                /// </summary>
                public String OID { get; set; }
                /// <summary>
                /// Value of the message recieved.
                /// </summary>
                public Object Value { get; set; }

                /// <summary>
                /// The Sender IP.
                /// </summary>
                public String Sender { get; set; }
                /// <summary>
                /// The status of the recieve message process.
                /// </summary>
                public Boolean Status { get; set; }

                public OnMessageRecievedDto(String oID, Object value)
                {
                    OID = oID;
                    Value = value;
                }
            }

            public OnMessageRecievedDto Deserialize(ISnmpMessage message)
            {
                IList<Variable> variables = message.Variables();

                //if (variables.Count == 0)
                //    return new OnMessageRecievedDto(null, null);

                return new OnMessageRecievedDto(
                    variables[0].Id.ToString(),
                    variables[0].Data);
            }

        }

        #endregion

    }

}

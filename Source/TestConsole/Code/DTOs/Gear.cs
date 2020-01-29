using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using U5ki.Enums;

namespace AdminConsole.Code
{

    public enum GearType
    {
        RCRohde4200 = 1,
        RCJotron7000 = 2
    }

    public enum GearStatusTypes
    {
        Ready = 0, // Remote active.
        Error = 1, // Local. OnError.
        Timeout = 2 // No Cable, No power, Net not working.
    }

    [XmlType("gear")]    
    public class Gear
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("ip")]
        public string Ip { get; set; }

        [XmlElement("port")]
        public string Port { get; set; }

        [XmlElement("type")]
        public GearType Type { get; set; }

        public string Frecuency { get; set; }

        public GearStatusTypes Status { get; set; }

        public override string ToString()
        {
            return " [ID: " + Id + "]"
                + " [Ip: " + Ip + "]"
                + " [Port: " + Port + "]"
                + " [Type: " + Type + "]";
        }
    }

}

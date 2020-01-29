using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace AdminConsole.Code
{
    public class ConfigurationSerializer
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
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Resources;
using System.Globalization;
using System.Threading;
using System.Reflection;

namespace Translate
{
    //VMG 31/10/2017    
    //Clase para traducir los mensajes que se van a insertar en la BBDD
    // 
    public static class CTranslate
    {
        private static string execDirectory = System.IO.Path.GetDirectoryName(
                                                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        private static string languageDirectory = execDirectory.Replace("file:\\", "");//Esto es para quitar el formato URI que no lo acepta
        
        private static CultureInfo ci = null;
        private static bool isOKInitialized = true;
        private static bool isInitialized = false;
        private static string errorMessage = "";
        private static string cInfo = Properties.Settings.Default.language;

        private static Object _lock = new Object();
        /** 
         * Genera los ficheros de recursos a traves del XML
         */
        private static bool generateResources()
        {
            lock (_lock)
            {
                switch (cInfo)
                {
                    case "en":
                        ci = new CultureInfo("en-US");
                        break;
                    case "fr":
                        ci = new CultureInfo("fr-FR");
                        break;
                    default:
                        ci = new CultureInfo("es-ES");
                        break;
                }
                if (!isInitialized)
                {
                    string aux = "";
                    if (cInfo != "es")
                    {
                        string xmlFileName = string.Format("{0}\\data.xml", languageDirectory);
                        ResourceWriter resource = null;
                        XDocument doc = null;

                        if (cInfo == "en")
                            resource = new ResourceWriter(languageDirectory + "/resource.en-US.resources");
                        else if (cInfo == "fr")
                            resource = new ResourceWriter(languageDirectory + "/resource.fr-FR.resources");

                        try
                        {
                            doc = XDocument.Load(xmlFileName);

                            foreach (XElement el in doc.Root.Elements())
                            {
                                foreach (XElement element in el.Elements())
                                {
                                    if (cInfo == "en")
                                    {
                                        if (element.Name.LocalName == "English")
                                        {
                                            aux = el.Attribute("id").Value;
                                            resource.AddResource(el.Attribute("id").Value, element.Value);
                                        }
                                    }
                                    else if (cInfo == "fr")
                                    {
                                        if (element.Name.LocalName == "French")
                                        {
                                            aux = el.Attribute("id").Value;
                                            resource.AddResource(el.Attribute("id").Value, element.Value);
                                        }
                                    }
                                }
                            }
                            resource.Generate();
                            resource.Close();

                        }
                        catch (Exception e)
                        {
                            ci = new CultureInfo("es-ES");
                            isOKInitialized = false;
                            errorMessage = e.Message + " " + aux;
                        }
                    }
                    isInitialized = true;
                }
                return isOKInitialized;
            }
        }

        /**
        *Obtiene la identificacion del idioma configurado 
        */
        public static string getError()
        {
            return errorMessage;
        }

        /**
        *Obtiene la identificacion del idioma configurado 
        */
        public static bool getInitialized()
        {
            return isInitialized;
        }

        /**
         *Traduce el string al idioma configurado
         */
        public static string translateResource(string msg)
        {
            lock (_lock)
            {
                if (!isInitialized)
                {
                    generateResources();
                }

                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;

                if (ci.Name != "es-ES")
                {
                    try
                    {
                        ResourceManager rm = ResourceManager.CreateFileBasedResourceManager("resource", languageDirectory, null);
                        if (rm.GetString(msg) != null)
                            return rm.GetString(msg);
                        else
                            return msg;//Sino encuentra el id devuelve lo que entra
                    }
                    catch (Exception e)
                    {
                        return msg;//Sino encuentra el id devuelve lo que entra
                    }
                }
                return msg;//Devuelve por defecto lo que le ha entrado que se supone que es en castellano
            }
        }

        /**
         *Traduce el string al idioma configurado pasando n parametros  
         */
        public static string translateResource(string msg, params string[] values)
        {
            lock (_lock)
            {
                if (!isInitialized)
                {
                    generateResources();
                }

                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                String translatedMsg = "";
                String formatedMsg = "";

                if (isOKInitialized)
                {
                    if (ci.Name != "es-ES")
                    {
                        try
                        {
                            ResourceManager rm = ResourceManager.CreateFileBasedResourceManager("resource", languageDirectory, null);
                            if (rm.GetString(msg) != null)
                                translatedMsg = rm.GetString(msg);
                            else
                                translatedMsg = msg;//Sino encuentra el id devuelve lo que entra
                        }
                        catch (Exception e)
                        {
                            translatedMsg = msg;//Sino encuentra el id devuelve lo que entra
                        }
                    }
                    else
                        translatedMsg = msg;//Devuelve por defecto lo que le ha entrado que se supone que es en castellano
                }//Sino esta correctamente inicializado, devuelve lo que entra, con parámetros
                else
                    translatedMsg = msg;//Devuelve por defecto lo que le ha entrado que se supone que es en castellano
                for (int i = 0; i < values.Length; i++)
                    formatedMsg = translatedMsg.Replace("{" + i + "}", values[i]);
                return formatedMsg;
            }
        }

        /**
         * 20171117. AGL. Obtiene el lenguaje activado...         * 
         */
        public static string Idioma { get { return cInfo; } }
    }
}

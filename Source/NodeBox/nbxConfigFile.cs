using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Resources;
using System.Reflection;
using System.Globalization;
//using NLog;

namespace U5ki.NodeBox
{
    public class nbxConfigFile
    {
        XmlDocument xdoc;
        string _fileName = string.Empty;
        string _idioma = "es";
        CultureInfo ciIdioma = null;
        // ResourceSet rsIdioma = null;

        /** */
        Dictionary<string, Dictionary<string, string>> _settings = new Dictionary<string, Dictionary<string, string>>();

        #region PUBLIC
        /// <summary>
        /// 
        /// </summary>
        public nbxConfigFile()
        {
            xdoc = new XmlDocument();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public Dictionary<string, string> SectionProperties(string sName)
        {
            try
            {
                return _settings[sName];
            }
            catch (Exception )
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        //public void PropertieSet(string sName, string query)
        //{
        //    string[] val = normalize(query);

        //    if (val != null && val.Length > 1)
        //    {
        //        SetSetting(sName, val[0], val[1]);
        //        Save();
        //        Reload();
        //    }
        //}

        #endregion

        #region PROTECTED

        /// <summary>
        /// 
        /// </summary>
        protected string FileName { get { return _fileName; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        protected void Load(string path, string idioma)
        {
            if (File.Exists(path))
            {

                xdoc.Load(path);

                _fileName = path;
                _idioma = idioma;

                ciIdioma = new CultureInfo(_idioma);
                //rsIdioma = Resources.items_config.ResourceManager.GetResourceSet(ciIdioma, true, true);
                
                //LoadStringDisplay(idioma);
            }
            else
            {
                //Logger _log = LogManager.GetCurrentClassLogger();
                //_log.Info(String.Format("El fichero {0} no existe",path));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void Reload()
        {
            xdoc.RemoveAll();
            if (File.Exists(_fileName))
                xdoc.Load(_fileName);
        }

        /// <summary>
        /// 
        /// </summary>
        protected void AddSettings(string strApl)
        {
            Dictionary<string, string> _section_settings = new Dictionary<string, string>();

            if (xdoc.DocumentElement != null)
            {
                foreach (XmlNode n1 in xdoc.DocumentElement.ChildNodes)
                {
                    if (n1.Name == "applicationSettings")
                    {
                        foreach (XmlNode nApl in n1.ChildNodes)
                        {
                            if (nApl.Name == strApl)
                            {
                                foreach (XmlNode set in nApl.ChildNodes)
                                {
                                    if (set.Name == "setting")
                                    {
                                        XmlAttribute xAttr = set.Attributes["name"];
                                        foreach (XmlNode val in set.ChildNodes)
                                        {
                                            if (val.Name == "value")
                                            {
                                                // string strdisplay = GetStringDisplay(xAttr.Value);

                                                // TODO...
                                                //// _section_settings.Add(xAttr.Value, val.InnerText);
                                                //_section_settings.Add(strdisplay, val.InnerText);

                                                //if (_display2key.ContainsKey(strdisplay)==false)
                                                //    _display2key.Add(strdisplay, xAttr.Value);
                                                _section_settings.Add(xAttr.Value, val.InnerText);
                                            }
                                        }
                                    }
                                }
                                _settings[strApl] = _section_settings;
                            }
                        }                    
                    }
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strApl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        //protected string GetSetting(string strApl, string name)
        //{
        //    XmlNode nodo = Nodo(strApl, name);
        //    if (nodo != null)
        //    {
        //        foreach (XmlNode val in nodo.ChildNodes)
        //        {
        //            if (val.Name == "value")
        //                return val.InnerText;
        //        }
        //    }
        //    return "¡¡¡ERROR!!!";
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="strApl"></param>
        /// <param name="name"></param>
        /// <param name="valor"></param>
        /// <returns></returns>
        //protected bool SetSetting(string strApl, string name, string valor)
        //{
        //    /** Salvarlo en listas locales. */
        //    Dictionary<string, string> _sec_settings = _settings[strApl];
        //    _sec_settings[name] = valor;

        //    /** Salvarlo en el fichero */
        //    XmlNode nodo = Nodo(strApl, name/* TODO _display2key[name]*/);
        //    if (nodo != null)
        //    {
        //        foreach (XmlNode val in nodo.ChildNodes)
        //        {
        //            if (val.Name == "value")
        //            {
        //                val.InnerText=valor;
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strApl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private XmlNode Nodo(string strApl, string name)
        {
            if (xdoc.DocumentElement != null)
            {
                foreach (XmlNode n1 in xdoc.DocumentElement.ChildNodes)
                {
                    if (n1.Name == "applicationSettings")
                    {
                        foreach (XmlNode nApl in n1.ChildNodes)
                        {
                            if (nApl.Name == strApl)
                            {
                                foreach (XmlNode set in nApl.ChildNodes)
                                {
                                    if (set.Name == "setting")
                                    {
                                        XmlAttribute xAttr = set.Attributes["name"];
                                        if (xAttr != null && xAttr.Name == "name" && xAttr.Value == name)
                                            return set;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            xdoc.Save(FileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //protected string[] normalize(string query)
        //{
        //    /** Quito el ? inicial */
        //    query = query.Replace("?", "");

        //    /** Subtituyo + por espacios */
        //    query = query.Replace("+", " ");

        //    /** Obtengo el comando y el valor */
        //    string[] val = query.Split('=');
        //    return val;
        //}

        /// <summary>
        /// 
        /// </summary>
        //protected void LoadStringDisplay(string idioma)
        //{
        //    //CultureInfo culture = new CultureInfo(idioma);
        //    //ResourceSet rs = Properties.Resources.items_config. ..GetResourceSet(culture, true, true);

        //    //    IDictionaryEnumerator id = rs.GetEnumerator();
        //    //    while (id.MoveNext())
        //    //    {
        //    //        try
        //    //        {
        //    //            string _key = (string)id.Key;
        //    //            if (!_key2display.ContainsKey(_key))
        //    //                _key2display[(string)id.Key] = (string)id.Value;
        //    //        }
        //    //        catch (Exception )
        //    //        {
        //    //        }
        //    //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetStringDisplay(string key)
        {
            string val = key; //rsIdioma.GetString(key);
            return val == null ? key : val;
            //return key;
        }

        /// <summary>
        /// 
        /// </summary>
        //System.Resources.ResourceManager Resources
        //{
        //    get
        //    {
        //        return _idioma == "fr" ? Properties.Resources_fr.ResourceManager : Properties.Resources.ResourceManager;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apl"></param>
        /// <param name="name"></param>
        /// <param name="valor"></param>
        /// <returns></returns>
        public bool PropertySet(string apl, string name, string valor)
        {
            XmlNode nodo = Nodo(apl, name);
            if (nodo != null)
            {
                foreach (XmlNode val in nodo.ChildNodes)
                {
                    if (val.Name == "value")
                    {
                        val.InnerText = valor;
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class NodeBoxConfig : nbxConfigFile
    {
        /** Fichero */
        const string cFileName =  "U5ki.NodeBox.exe.config";

        /** Secciones */
        public const string cNbxSectionName    = "U5ki.NodeBox.Properties.Settings";
        public const string cInfraSectionName = "U5ki.Infrastructure.Properties.Settings";
        public const string cRadioSectionName = "U5ki.RdService.Properties.Settings";
        public const string cConfigSectionName = "U5ki.CfgService.Properties.Settings";
        public const string cTifxSectionName   = "U5ki.TifxService.Properties.Settings";
        public const string cPabxSectionName = "U5ki.PabxItfService.Properties.Settings";

        /// <summary>
        /// 
        /// </summary>
        public NodeBoxConfig(string path, string idioma)
            : base()
        {
            string file = Path.Combine(path, cFileName);
            base.Load(file, idioma);

            AddSettings(cNbxSectionName);
            AddSettings(cInfraSectionName);
            AddSettings(cRadioSectionName);
            AddSettings(cConfigSectionName);
            AddSettings(cTifxSectionName);
            AddSettings(cPabxSectionName);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class HMIConfig : nbxConfigFile
    {
        /** Fichero */
        const string cFileName = "hmi.exe.config";

        /** Secciones */
        public const string cSec_Infrastructure = "Cd40.Infrastructure.Properties.Settings";
        public const string cSec_HmiModel = "HMI.Model.Module.Properties.Settings";
        public const string cSec_Cd40Model = "HMI.CD40.Module.Properties.Settings";
        public const string cSec_Premodel = "HMI.Presentation.Twr.Properties.Settings";

        /// <summary>
        /// 
        /// </summary>
        public HMIConfig(string path, string idioma)
            : base()
        {
            string file = Path.Combine(path, cFileName);
            base.Load(file, idioma);

            AddSettings(cSec_Infrastructure);
            AddSettings(cSec_HmiModel);
            AddSettings(cSec_Cd40Model);
            AddSettings(cSec_Premodel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void Reload(string path, string idioma)
        {
            string file = Path.Combine(path, cFileName);
            base.Load(file, idioma);
        }
    }

}

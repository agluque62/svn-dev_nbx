using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NLog;

namespace U5ki.Infrastructure
{
    public class ConfInt
    {
        /// <summary>
        /// 
        /// </summary>
        static Logger _Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        private static string PathMcast
        {
            get
            {
                if (File.Exists("U5ki.Mcast.exe"))
                    return ".\\";
                else if (File.Exists("..\\U5ki.Mcast\\U5ki.Mcast.exe"))
                    return "..\\U5ki.Mcast\\";                
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static string IdMCast
        {
            get
            {
                try
                {
                    Configuration configuration = ConfigurationManager.OpenExeConfiguration(PathMcast + "U5ki.Mcast.exe");

                    ConfigurationSectionGroup grp = configuration.SectionGroups["applicationSettings"];
                    ClientSettingsSection section = (ClientSettingsSection)grp.Sections["U5ki.Mcast.Properties.Settings"];

                    string id = section.Settings.Get("IdHost").Value.ValueXml.InnerText;
                    return id;
                }
                catch (Exception x)
                {
                    _Logger.Info("ConfInt.IdMast. No puedo Obtener el IDMCAST", x);
                    return "none";
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        enum stdLectura { inicio, Segmento }
        private static IPAddress IpFromIdMCAST
        {
            get
            {
                try
                {
                    string id = ConfInt.IdMCast;
                    if (id == "none")
                        throw new Exception("");

                    if (!File.Exists(PathMcast + "spread.conf"))
                        throw new Exception("El fichero spread.conf no existe");

                    StreamReader fr = new StreamReader(PathMcast + "spread.conf");                    
                    stdLectura std = stdLectura.inicio;

                    while (fr.Peek() >= 0)
                    {
                        String line = fr.ReadLine();
                        switch (std)
                        {
                            case stdLectura.inicio:
                                if (line.Contains("Spread_Segment"))
                                    std = stdLectura.Segmento;
                                break;
                            case stdLectura.Segmento:
                                if (line.StartsWith("}"))
                                    throw new Exception("El id de spread no se encuentra en spread.conf");
                                if (line.StartsWith("#"))
                                    break;
                                if (line.Contains(id))
                                {
                                    line = line.Trim(new char[] { ' ', '\t','{','}' });
                                    String[] items = line.Split(new char[] { ' ', '\t' });
                                    foreach (String item in items)
                                    {
                                        IPAddress add;
                                        if (IPAddress.TryParse(item, out add))
                                        {
                                            return add;
                                        }
                                    }
                                    return IPAddress.None;
                                }
                                break;
                            default:
                                break;                          

                        }
                    }

                    return IPAddress.None;
                }
                catch (Exception x)
                {
                    _Logger.Info("ConfInt.IpFromIdMcast. No puedo Obtener IP de Multicast", x);
                    return IPAddress.None;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static IPAddress DireccionBroadSubnet(IPAddress ip)
        {
            NetworkInterface[] nets = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface iface in nets)
            {
                if ((iface.NetworkInterfaceType != NetworkInterfaceType.Loopback) &&
                    (iface.OperationalStatus == OperationalStatus.Up))
                {
                    foreach (UnicastIPAddressInformation ipItem in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (ipItem.Address.AddressFamily == AddressFamily.InterNetwork &&
                            ipItem.Address.ToString() == ip.ToString())
                        {
                            byte [] iplb = ip.GetAddressBytes();
                            byte [] mscb = ipItem.IPv4Mask.GetAddressBytes();
                            byte [] brdb = new byte[iplb.Length];                       
                            for (int i = 0; i< iplb.Length; i++)
                            {
                                brdb[i] = (byte )(iplb[i] | (mscb[i] ^ 255));
                            }

                            IPAddress ipb = new IPAddress(brdb);
                            return ipb;
                        }
                    }
                }
            }
            return IPAddress.Broadcast;
        }



        /// <summary>
        /// 
        /// </summary>
        public static IPAddress IpBroad
        {
            get
            {
                try
                {
                    string pcname = Dns.GetHostName();
                    IPHostEntry ips = Dns.GetHostEntry(pcname);
                    foreach (IPAddress ip in ips.AddressList)
                    {
                        if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal)
                            continue;
                        
                        IPAddress brda = DireccionBroadSubnet(ip);
                        if (brda.Equals(IPAddress.Broadcast))
                            continue;

                        // _Logger.Info("IP: " + ip.ToString() + ", BROAD: " + brda.ToString());
                        return brda;
                    }

                }
                catch (Exception e)
                {
                    _Logger.Info("ConfInt.getIp. No puedo Obtener IP-BROAD", e);
                }
                return IPAddress.Broadcast;
            }
        }

    }
}

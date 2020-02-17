using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

using NLog;

namespace ExtEquSim
{
    public class Helper
    {
        static public void Log<T>(string msg)
        {
            Logger log = LogManager.GetLogger(typeof(T).Name);
            log.Info(msg);
        }

        static public void CommandLineParser(string[] args, Action<string, int, List<string>> Notify)
        {
            if (args.Length >= 1)
            {
                var endpoint = args[0];
                var parts = endpoint.Split(':');
                var ip = parts[0];
                var port = parts.Count() == 2 ? parts[1] : "5060";

                IPAddress Ip;
                int Port;
                if (IPAddress.TryParse(ip, out Ip) == true &&
                    Int32.TryParse(port, out Port) == true)
                {
                    if (args.Length > 1)
                    {
                        var users = args.Skip(1).Take(args.Length - 1);
                        Notify(ip, Port, users.ToList());
                    }
                    else
                    {
                        Notify?.Invoke(ip, Port, null);
                    }
                }
            }
        }

    }
}

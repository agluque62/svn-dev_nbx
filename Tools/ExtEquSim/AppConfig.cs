using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ExtEquSim
{
    public class AppConfig
    {
        public string IP { get; set; }
        public int PORT { get; set; }
        public List<string> Users { get; set; }

        static public void Get(Action<AppConfig, Exception> Notify)
        {
            var cfg = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText("config.json"));
            Notify?.Invoke(cfg, null);
        }

        public void Save()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(this));
        }
    }
}

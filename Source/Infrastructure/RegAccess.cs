using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace U5ki.Infrastructure
{
    public static class RegAccess
    {
        public static string Get(string app, string section, string variable)
        {
            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64);
                Microsoft.Win32.RegistryKey rk = hklm.OpenSubKey(@"SOFTWARE\NucleoDF\Ulises5000\" + app + @"\" + section, false);
                return Convert.ToString(rk.GetValue(variable));
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().FatalException("No existe la clave " + app + @"\" + section + @"\" + variable + " en el registro", e);
                return string.Empty;
            }
        }
    }
}

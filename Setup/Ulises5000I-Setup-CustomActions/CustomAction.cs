using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.Deployment.WindowsInstaller;

//using System.Windows.Forms;

namespace Ulises5000I_Setup_CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomActionTest(Session session)
        {
            session.Log("Begin CustomAction1");
            System.Windows.Forms.MessageBox.Show(String.Format("CustomActionTest <{0}>, {1}, {2}", 
                session["U5ki.MCast.INSTALLFOLDER"],
                session["SPREAD_NUMBER"],
                session["SPREAD_IPBASE"]));
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GenerateSpreadConf(Session session)
        {
            System.IO.StreamWriter conf;
            try
            {
                System.IO.Directory.CreateDirectory(session["U5ki.MCast.INSTALLFOLDER"]);
                int nItems = int.Parse(session["SPREAD_NUMBER"]);
                byte[] ipbase = IPAddress.Parse(session["SPREAD_IPBASE"]).GetAddressBytes();

                /** */
                View lView = session.Database.OpenView("DELETE FROM ComboBox WHERE ComboBox.Property='PICT'");
                lView.Execute();
                lView = session.Database.OpenView("SELECT * FROM ComboBox");
                lView.Execute();

                using (conf = new System.IO.StreamWriter(session["U5ki.MCast.INSTALLFOLDER"] + "spread.conf", false))
                {
                    conf.WriteLine(String.Format("Spread_Segment  239.255.10.50:4803 {{"));

                    for (int item = 0; item < nItems; item++)
                    {
                        IPAddress current = new IPAddress(ipbase);
                        conf.WriteLine(String.Format("\t PICT{0:00} {1} {{", item + 1, current.ToString()));

                        conf.WriteLine(String.Format("\t\tD {0}", current.ToString()));
                        conf.WriteLine(String.Format("\t\tC {0}", current.ToString()));
                        conf.WriteLine(String.Format("\t\tC 127.0.0.1"));
                        conf.WriteLine(String.Format("\t }}"));

                        /** */
                        Record lRecord = session.Database.CreateRecord(4);
                        lRecord.SetString(1, "PICT");
                        lRecord.SetInteger(2, item);
                        lRecord.SetString(3, String.Format("PICT{0:00}", item+1));
                        lRecord.SetString(4, String.Format("PICT{0:00}, {1}", item + 1, current.ToString()));
                        lView.Modify(ViewModifyMode.InsertTemporary, lRecord);

                        ipbase[3]++;
                    }

                    conf.WriteLine(String.Format("}}"));

                    conf.WriteLine("TokenTimeout = 5000");
                    conf.WriteLine("HurryTimeout = 1500");
                    conf.WriteLine("AliveTimeout = 250");
                    conf.WriteLine("JoinTimeout = 250");
                    conf.WriteLine("RepTimeout = 1250");
                    conf.WriteLine("SegTimeout = 500");
                    conf.WriteLine("GatherTimeout = 2500");
                    conf.WriteLine("FormTimeout = 2500");
                        /** 20170903. AGL. Se cambia este parámetro para evitar transitorios en conmutación de NBX... */
                    conf.WriteLine(/*"LookupTimeout = 45000"*/"LookupTimeout = 15000");

                    conf.WriteLine("DebugFlags = { PRINT EXIT }");
                    conf.WriteLine("EventPriority =  INFO");
                    conf.WriteLine("#EventLogFile = testlog.out");
                    conf.WriteLine("EventTimeStamp = \"%Y-%m-%d %H:%M:%S:\"");
                    conf.WriteLine("#EventPreciseTimeStamp");
                    conf.WriteLine("#DebugInitialSequence");
                    conf.WriteLine("DangerousMonitor = true");
                    conf.WriteLine("#SocketPortReuse = AUTO");
                    conf.WriteLine("#MaxSessionMessages = 1000");
                    conf.WriteLine("RuntimeDir = /var/run/spread");
                    conf.WriteLine("DaemonUser = p");
                    conf.WriteLine("DaemonGroup = users");
                    conf.WriteLine("#RequiredAuthMethods = \"   \"");
                    conf.WriteLine("#AllowedAuthMethods = \"NULL\"");

                }
            }
            catch (Exception x)
            {
                session.Log("ERROR in custom action GenerateSpreadConf {0}", x.ToString());
                System.Windows.Forms.MessageBox.Show(String.Format("ERROR in custom action GenerateSpreadConf {0}", x.ToString()));
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CustomActionPict2Ip(Session session)
        {
            session.Log("Begin CustomActionPict2Ip");
            try
            {
                int nItems = int.Parse(session["SPREAD_NUMBER"]);
                byte[] ipbase = IPAddress.Parse(session["SPREAD_IPBASE"]).GetAddressBytes();
                string pict = session["PICT"];

                for (int item = 0; item < nItems; item++)
                {
                    if (pict == String.Format("PICT{0:00}", item + 1))
                    {
                        session["DIRIP"] = (new IPAddress(ipbase)).ToString();
                        return ActionResult.Success;
                    }
                    ipbase[3]++;
                }

            }
            catch (Exception x)
            {
                session.Log("ERROR in custom action CustomActionPict2Ip {0}", x.ToString());
                System.Windows.Forms.MessageBox.Show(String.Format("ERROR in custom action CustomActionPict2Ip {0}", x.ToString()));
                return ActionResult.Failure;
            }
            System.Windows.Forms.MessageBox.Show("ERROR en CustomAction CustomActionPict2Ip: No se ha encontrado una IP");
            return ActionResult.Failure;
        }

    }
}

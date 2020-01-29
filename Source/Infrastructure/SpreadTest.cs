using System;
using System.Collections.Generic;
using System.Text;

using NLog;
using Utilities;

namespace Cd40.Infrastructure
{
    public class SpreadTest
    {
        static Logger _Logger = LogManager.GetCurrentClassLogger();

        SpreadChannel spc1 = new SpreadChannel("SpTest1");
        SpreadChannel spc2 = new SpreadChannel("SpTest2");

        public void Init()
        {
            spc2.DataMsg += OnChannelData;
        
            spc1.Join("pruebas");
            spc2.Join("pruebas");
        }

        private void OnChannelData(object sender, SpreadDataMsg msg)
        {
            try
            {
                switch (msg.Type)
                {
                    case -1000:
                        _Logger.Info("SpreadTest: Recibidos " + msg.Length + " bytes.");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception /*ex*/)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datos"></param>
        public void send(byte[] datos)
        {
            spc1.Send(-1000, datos, "pruebas");
        }

        public void dispose()
        {
            spc1.Dispose();
            spc2.Dispose();
        }
    }
}

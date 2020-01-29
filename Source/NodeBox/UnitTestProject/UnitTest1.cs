using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

using U5ki.NodeBox;
using Utilities;

namespace UnitTestProject
{
    [TestClass]
    public class NbxWebServerUnitTest
    {
        NbxWebServer _webServer = new NbxWebServer("d:\\Datos\\Empresa\\_SharedPrj\\UlisesV5000i-trunk\\ulises-ta\\Sources\\Cd40\\Source\\NodeBox");

        [TestMethod]
        public void NbxWebServerTestMethod1()
        {
            _webServer.WebSrvCommand += (cmd, datain) =>
            {
                return null;
            };
            _webServer.Start(14441);

            MessageBox.Show("WebServerArrancado");

            _webServer.Dispose();
        }
    }

    [TestClass]
    public class SipUtilitiesTest
    {
        [TestMethod]
        public void TestSipUriParser()
        {
            SipUtilities.SipUriParser sipuri = new SipUtilities.SipUriParser("<sip:\"pp\"192.168.0.129:7060>");
            string stdf = sipuri.UlisesFormat;
            
            sipuri = new SipUtilities.SipUriParser("<sip:\"pp\"bb@192.168.0.129:7060>");
            Assert.AreEqual(sipuri.User, "bb");
            Assert.AreEqual(sipuri.Port, 7060);
            Assert.AreEqual(sipuri.Host, "192.168.0.129");
            Assert.AreEqual(sipuri.DisplayName, "pp");

            sipuri = new SipUtilities.SipUriParser("<sip:bb@192.168.0.129:7060>");
            Assert.AreEqual(sipuri.User, "bb");
            Assert.AreEqual(sipuri.Port, 7060);
            Assert.AreEqual(sipuri.Host, "192.168.0.129");
            Assert.AreEqual(sipuri.DisplayName, "");

            sipuri = new SipUtilities.SipUriParser("<sip:\"pp\"bb@192.168.0.129>");
            Assert.AreEqual(sipuri.User, "bb");
            Assert.AreEqual(sipuri.Port, 5060);
            Assert.AreEqual(sipuri.Host, "192.168.0.129");
            Assert.AreEqual(sipuri.DisplayName, "pp");

            sipuri = new SipUtilities.SipUriParser("sip:0682696141@192.168.2.18;cd40rs=RTB-REC");
            Assert.AreEqual(sipuri.User, "0682696141");
            Assert.AreEqual(sipuri.Port, 5060);
            Assert.AreEqual(sipuri.Host, "192.168.2.18");
            Assert.AreEqual(sipuri.DisplayName, "");
            Assert.AreEqual(sipuri.Resource, "RTB-REC");
        }
    }

    [TestClass]
    public class ServicesHelpersTest
    {
        [TestMethod]
        public void VersionsFileAdjustTest_Radio()
        {
            ///** Test Radio */
            ServicesHelpers.VersionsFileAdjust("RadioService", new List<string>() { "CfgService", "RdService", "RemoteControlService" });
        }

        [TestMethod]
        public void VersionsFileAdjustTest_Phone()
        {
            ///** Test Phone */
            ServicesHelpers.VersionsFileAdjust("PhoneService", new List<string>() { "TifxService", "PresenceService", "PhoneService" });
        }

        [TestMethod]
        public void VersionsFileAdjustTest_Mixed()
        {
            ///** Test Mixed */
            ServicesHelpers.VersionsFileAdjust("Nodebox", null);
        }
    }

}

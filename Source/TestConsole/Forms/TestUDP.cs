using AdminConsole.Resources;
using System;
using System.Windows.Forms;
using u5ki.RemoteControlService;
using U5ki.Enums;
using U5ki.Infrastructure;
using U5ki.Infrastructure.Code;

namespace AdminConsole
{
    public partial class TestUDP : Form
    {

        private IRemoteControl _remoteControl;
        /// <summary>
        /// Usado para realizar las conexiones de los nodos.
        /// </summary>
        public IRemoteControl RemoteControl
        {
            get
            {
                if (null == _remoteControl)
                    _remoteControl = 
                        Locals.RemoteControlFactory.ManufactureOne(
                            U5ki.Enums.RCTypes.RCRohde4200, 
                            Convert.ToInt32(test1Port.Text));
                return _remoteControl;
            }
        }

        public TestUDP()
        {
            InitializeComponent();

            InitializeText();
        }

        private void InitializeText()
        {
            this.Text = Messages.SNMPEmulator;
        }

        private void test1Button_Click(object sender, EventArgs e)
        {
            BaseNode node = new BaseNode();
            node.IP = test1Ip.Text;
            node.Port = Convert.ToInt32(test1Port.Text);
            node.Frecuency = test1Frecuency.Text;

            RemoteControl.CheckNode(OnGearOperationStatus, node);
        }

        public void OnGearOperationStatus(GearOperationStatus status)
        {
            Globals.Events.Trigger(
                GlobalEventTypes.OnMessage, 
                status.ToString());
        }

    }
}

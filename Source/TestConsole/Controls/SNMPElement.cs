using AdminConsole.Code;
using AdminConsole.Forms;
using AdminConsole.Resources;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using U5ki.Delegates;
using U5ki.Enums;
using U5ki.Infrastructure.Helpers;
using U5ki.Infrastructure.Servers;

namespace AdminConsole.Controls
{
    /// <summary>
    /// Control de usuario que representa un elemento SNMP en la red. Emula el comportamiento de un equipo que puede recibir ordenes SNMP y responderlas.
    /// </summary>
    /// <remarks>
    /// 
    /// TODO: 
    ///  - Crear un listener con los datos del Gear.
    ///  - Hacer que escuche formatos y devuelva información (Eventos dese el listener).
    /// 
    /// </remarks>
    public partial class SNMPElement : UserControl, ISNMPElement, IDisposable
    {

        public Gear Element { get; set; }

        private SNMPListener _listener;

        private SerializerHelper _serializerHelper;
        public SerializerHelper SerializerHelper
        {
            get
            {
                if (null == _serializerHelper)
                    _serializerHelper = new SerializerHelper();
                return _serializerHelper;
            }
        }

        private Timer SessionTimer = new Timer();

        public SNMPElement()
        {
            InitializeComponent();
            InitializeText();
        }

        #region Initialize

        private void InitializeText()
        {
            LabelId.Text = Keywords.Id + ": ";
            LabelIP.Text = Keywords.IP + ": ";
            LabelPort.Text = Keywords.Port + ": ";
            labelFrecuency.Text = Keywords.Frecuency + ": ";

            ToggleButton.Text = Keywords.Activate + "/" + Keywords.Deactivate;
        }

        public void Initialize(Gear gear)
        {
            Element = gear;

            LabelIdValue.Text = Element.Id;
            LabelIPValue.Text = Element.Ip;
            LabelPortValue.Text = Element.Port.ToString();
            labelFrecuencyValue.Text = Element.Frecuency;
            
            MessageAdd(MessagesSNMP.GearCreated);

            _listener = new SNMPListener(
                Element.Ip, 
                Convert.ToInt32(Element.Port),
                OnMessageRecieved);
            StatusSet(GearStatusTypes.Ready);
            
            InitializeType();

            // Session.
            SessionTimer.Interval = 5000;
            SessionTimer.Tick += SessionTimer_Tick;
            SessionStatus.BackColor = Color.White;
        }

        private void InitializeType()
        {
            switch (Element.Type)
            {
                case GearType.RCRohde4200:
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region Handlers

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            if (_listener.IsRunning)
            {
                _listener.Stop();
                StatusSet(GearStatusTypes.Ready);
            }
            else
            {
                _listener.Start();
                StatusSet(GearStatusTypes.Error);
            }
        }

        private void OrderRecievedList_DoubleClick(object sender, EventArgs e)
        {
            MessagesDetails details = new MessagesDetails();
            details.Initialize(OrderRecievedList.Items, Element.ToString());
            details.Show();
        }

        #endregion

        #region Element Functions

        public void MessageAdd(String message)
        {
            if (this.OrderRecievedList.InvokeRequired)
            {
                StringDelegate invokeDelegate = new StringDelegate(MessageAdd);
                this.Invoke(invokeDelegate, new Object[] { message });
            }
            else
            {
                OrderRecievedList.Items.Insert(0, "[" + DateTime.Now.ToString() + "] " + message);
            }
        }

        private void StatusSet(GearStatusTypes input)
        {
            Element.Status = input;
            if (Element.Status == GearStatusTypes.Ready)
            {
                StatusPanel.BackColor = Color.Green;
                MessageAdd(MessagesSNMP.GearStarted);
            }
            else
            {
                StatusPanel.BackColor = Color.Red;
                MessageAdd(MessagesSNMP.GearStoped);
            }
        }

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            SessionStatus.BackColor = Color.White;
            SessionTimer.Enabled = false;
        }

        #endregion

        #region OnMessageRecieved

        private ErrorCode OnMessageRecieved(ISnmpMessage message, IPEndPoint endPoint)
        {
            SerializerHelper.SNMPSerializer.OnMessageRecievedDto data = 
                SerializerHelper.SNMP.Deserialize(
                    message);

            MessageAdd(
                String.Format(
                    MessagesSNMP.GearMessageRecieved,
                    "[Id: " + data.OID + "] "
                    + "[Value: " + data.Value + "] "));

            return ProcessMessage(data);
        }

        private ErrorCode ProcessMessage(SerializerHelper.SNMPSerializer.OnMessageRecievedDto data)
        {
            switch (Element.Type)
            {
                case GearType.RCRohde4200:
                    return ProcessMessageRCRohde4200(data);

                default:
                    throw new NotImplementedException();
            }
        }

        private ErrorCode ProcessMessageRCRohde4200(SerializerHelper.SNMPSerializer.OnMessageRecievedDto data)
        {
            // RequestSession.
            if (data.OID == u5ki.RemoteControlService.OIDs.RCRohde4200.RequestSession)
            {
                if (Element.Status == GearStatusTypes.Ready)
                {
                    SessionStatus.BackColor = Color.Green;
                    SessionTimer.Enabled = true;
                    return ErrorCode.NoError;
                }
                return ErrorCode.NoAccess;
            }

            throw new NotImplementedException("data.OID pending development in SNMPElement.ProcessMessageRCRohde4200");
        }


        #endregion

        public void Dispose()
        {
            _listener.Dispose();
        }

    }
}

using AdminConsole.Code;
using AdminConsole.Controls;
using AdminConsole.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using U5ki.Enums;
using U5ki.Infrastructure.Code;
using U5ki.Infrastructure.Helpers;

namespace AdminConsole
{
    public partial class SNMPEmulator : Form
    {

        IList<Gear> Gears { get; set; }

        #region Initiliaze

        public SNMPEmulator()
        {
            InitializeComponent();

            Gears = new List<Gear>();

            InitializeText();
        }

        private void InitializeText()
        {
            this.Text = Messages.SNMPEmulator;

            menuClose.Text = Keywords.Close;
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach (Control control in ElementList.Controls)
                if (control is SNMPElement)
                    ((SNMPElement)control).Dispose();
        }

        #region Menu Handlers
        
        private void menuClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "XML Files (.xml)|*.xml";
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Gears = new SerializerHelper().XML.Deserialize<Gear>(dialog.FileName, "gears");
                InitializeGears();
            }
        }

        #endregion

        #region Gears

        private void InitializeGears()
        {
            ElementList.Controls.Clear();

            if (null == Gears)
            {
                Globals.Events.Trigger(GlobalEventTypes.OnMessage, MessagesSNMP.GearsEmpty);
                return;
            }
            if (Gears.Count == 0)
            {
                Globals.Events.Trigger(GlobalEventTypes.OnMessage, MessagesSNMP.GearsEmpty);
                return;
            }

            foreach (Gear gear in Gears)
                InitializeGear(gear);

            Globals.Events.Trigger(GlobalEventTypes.OnMessage, MessagesSNMP.GearsLoaded);
        }

        private void InitializeGear(Gear gear)
        {
            SNMPElement control = new SNMPElement();
            control.Initialize(gear);
            ElementList.Controls.Add(control);
        }

        #endregion

    }
}

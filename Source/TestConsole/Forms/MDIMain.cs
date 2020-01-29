using AdminConsole.Resources;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using U5ki.Delegates;
using U5ki.Enums;
using U5ki.Infrastructure.Code;

namespace AdminConsole
{
    public partial class MDIMain : Form
    {

        private int childFormNumber = 0;

        private IDictionary<String, Form> Forms { get; set; }
        
        public MDIMain()
        {
            InitializeComponent();
            Forms = new Dictionary<String, Form>();

            InitializeText();
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            Globals.Events.Suscribe(GlobalEventTypes.OnMessage, OnMessage);
        }

        private void InitializeText()
        {
            this.Text = Messages.Title;
            GeneralStatus.Text = String.Empty;

            menuSNMPEmulator.Text = Messages.SNMPEmulator;
            menuMNEmulator.Text = Messages.MNEmulator;

            menuTestUDP.Text = Messages.TestUDP;
        }

        #region Handlers

        #region Handlers - Menus

        private String _menuSNMPEmulatorKey = "SNMPEmulator";
        private void menuSNMPEmulator_Click(object sender, EventArgs e)
        {
            InitializeSubForm(_menuSNMPEmulatorKey, typeof(SNMPEmulator), menuSNMPEmulator_Close);
        }
        private void menuSNMPEmulator_Close(object sender, EventArgs e)
        {
            CloseSubForm(_menuSNMPEmulatorKey);
        }

        private String _menuMNEmulatorKey = "MNEmulator";
        private void menuMNEmulator_Click(object sender, EventArgs e)
        {
            InitializeSubForm(_menuMNEmulatorKey, typeof(MNEmulator), menuMNEmulator_Close);
        }
        private void menuMNEmulator_Close(object sender, EventArgs e)
        {
            CloseSubForm(_menuMNEmulatorKey);
        }

        private String _menuTestUDPKey = "TestUDP";
        private void menuTestUDP_Click(object sender, EventArgs e)
        {
            InitializeSubForm(_menuTestUDPKey, typeof(TestUDP), menuTestUDP_Close);
        }
        private void menuTestUDP_Close(object sender, EventArgs e)
        {
            CloseSubForm(_menuTestUDPKey);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IList<Form> forms = new List<Form>(Forms.Values);
            foreach (Form form in forms)
                form.Close();
            forms.Clear();

            this.Close();
        }

        #endregion

        private void InitializeSubForm(String key, Type formType, FormClosedEventHandler closeHandler)
        {
            if (Forms.ContainsKey(key))
            {
                Forms[key].Select();
                return;
            }

            Forms.Add(
                key,
                CreateShowForm(
                    (Form)Activator.CreateInstance(formType),
                    closeHandler,
                    key));
        }
        private void CloseSubForm(String key)
        {
            if (Forms.ContainsKey(key))
                Forms.Remove(key);
        }
        private Form CreateShowForm(Form input, FormClosedEventHandler closeHandler, String title)
        {
            input.Text = title;
            input.MdiParent = this;
            input.Text = "Ventana " + childFormNumber++;
            input.WindowState = FormWindowState.Maximized;
            input.Show();
            input.FormClosed += closeHandler;
            this.Width = this.Width + 1;
            return input;
        }

        public void OnMessage(String message)
        {
            if (this.InvokeRequired)
            {
                StringDelegate invokeDelegate = new StringDelegate(OnMessage);
                this.Invoke(invokeDelegate, new Object[] { message });
            }
            else
            {
                if (!String.IsNullOrEmpty(GeneralStatus.Text))
                    GeneralStatus.Text += " ### ";
                GeneralStatus.Text += message;
                StatusTimer.Enabled = true;
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            GeneralStatus.Text = String.Empty;
            StatusTimer.Enabled = false;
        }

        #endregion




    }
}

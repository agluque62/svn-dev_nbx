using AdminConsole.Resources;
using System;
using System.Windows.Forms;

namespace AdminConsole
{
    public partial class MNEmulator : Form
    {

        public MNEmulator()
        {
            InitializeComponent();

            InitializeText();
        }

        private void InitializeText()
        {
            this.Text = Messages.MNEmulator;

            menuClose.Text = Keywords.Close;
        }

        #region Menu Handlers
        
        private void menuClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
        }

        #endregion

    }
}

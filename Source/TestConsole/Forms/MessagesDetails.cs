using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdminConsole.Forms
{
    public partial class MessagesDetails : Form
    {
        public MessagesDetails()
        {
            InitializeComponent();
        }

        public void Initialize(ListBox.ObjectCollection items, String header = "")
        {
            if (!String.IsNullOrEmpty(header))
            {
                OrderRecievedList.Items.Add("");
                OrderRecievedList.Items.Add(header);
                OrderRecievedList.Items.Add("");
            }

            foreach (Object control in items)
                OrderRecievedList.Items.Add(" " + control);
        }

        private void OrderRecievedList_DoubleClick(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            
            foreach (Object control in OrderRecievedList.Items)
                builder.Append(control + Environment.NewLine);

            Clipboard.SetText(builder.ToString());
            MessageBox.Show("Se ha copiado en el Portapapeles el contenido.");
        }
    }
}

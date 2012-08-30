using System;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public sealed partial class AddItemPoolDialog : Form
    {
        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        public string ItemPoolName { get { return tbName.Text.Trim(); } }

        public AddItemPoolDialog()
        {
            InitializeComponent();
        }

        private void AddItemPoolDialogLoad(object sender, EventArgs e)
        {
            tbName.Text = string.Empty;
            tbName.Focus();
        }

        private void OkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void CancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void NameChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = !string.IsNullOrWhiteSpace(tbName.Text) && ItemPoolManager.GetItemPool(tbName.Text.Trim()) == null;
        }
    }
}

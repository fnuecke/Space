using System;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public sealed partial class AddAttributePoolDialog : Form
    {
        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        public string AttributePoolName { get { return tbName.Text.Trim(); } }

        public AddAttributePoolDialog()
        {
            InitializeComponent();
        }

        private void AddAttributePoolDialogLoad(object sender, EventArgs e)
        {
            tbName.Text = string.Empty;
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
            btnOK.Enabled = !string.IsNullOrWhiteSpace(tbName.Text) && AttributePoolManager.GetAttributePool(tbName.Text.Trim()) == null;
        }
    }
}

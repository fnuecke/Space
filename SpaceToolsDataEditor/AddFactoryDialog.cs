using System;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public sealed partial class AddFactoryDialog : Form
    {
        /// <summary>
        /// Gets the type of the factory.
        /// </summary>
        public Type FactoryType { get { return (Type)cbType.SelectedItem; } }

        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        public string FactoryName { get { return tbName.Text.Trim(); } }

        public AddFactoryDialog()
        {
            InitializeComponent();

            cbType.DisplayMember = "Name";

            cbType.BeginUpdate();
            foreach (var type in FactoryManager.GetFactoryTypes())
            {
                cbType.Items.Add(type);
            }
            cbType.EndUpdate();
        }

        private void AddFactoryDialogLoad(object sender, EventArgs e)
        {
            if (cbType.Items.Count > 0 && cbType.SelectedIndex < 0)
            {
                cbType.SelectedIndex = 0;
            }
            tbName.Text = string.Empty;

            cbType.Focus();
        }

        private void OkClick(object sender, EventArgs e)
        {
            tbName.Text = tbName.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void CancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void NameChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = !string.IsNullOrWhiteSpace(tbName.Text) && !FactoryManager.HasFactory(tbName.Text.Trim());
        }
    }
}

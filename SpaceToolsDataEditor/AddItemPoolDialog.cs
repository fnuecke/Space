using System;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class AddItemPoolDialog : Form
    {
        /// <summary>
        /// Gets the type of the factory.
        /// </summary>
        public Type ItemPoolType { get { return (Type)cbType.SelectedItem; } }

        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        public string FactoryName { get { return tbName.Text; } }

        public AddItemPoolDialog()
        {
            InitializeComponent();

            cbType.BeginUpdate();

            cbType.Items.Add(new ItemPool().GetType());
            cbType.EndUpdate();
        }

        private void AddItemPoolDialogLoad(object sender, EventArgs e)
        {
            if (cbType.Items.Count > 0)
            {
                cbType.SelectedIndex = 0;
            }
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
            btnOK.Enabled = !string.IsNullOrWhiteSpace(tbName.Text);
        }
    }
}

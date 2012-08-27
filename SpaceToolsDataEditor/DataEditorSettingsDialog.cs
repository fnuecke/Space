using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditorSettingsDialog : Form
    {
        public DataEditorSettingsDialog()
        {
            InitializeComponent();

            pgSettings.SelectedObject = DataEditorSettingsProxy.Default;
        }

        private void BtnOkClick(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BtnSearchClick(object sender, System.EventArgs e)
        {

        }
    }
}

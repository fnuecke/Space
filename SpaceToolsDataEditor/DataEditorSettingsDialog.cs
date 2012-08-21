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
    }
}

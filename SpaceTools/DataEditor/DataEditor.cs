using System;
using System.IO;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor : Form
    {
        private readonly FolderBrowserDialog _openDialog = new FolderBrowserDialog();

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            _openDialog.ShowNewFolderButton = false;

            if (string.IsNullOrWhiteSpace(Settings.Default.LastOpenedFolder))
            {
                _openDialog.SelectedPath = Application.StartupPath;
            }
            else
            {
                _openDialog.SelectedPath = Settings.Default.LastOpenedFolder;
                if (Settings.Default.AutoLoad)
                {
                    LoadFactories(_openDialog.SelectedPath);
                }
            }

            DesktopBounds = Settings.Default.WindowBounds;

            new Serialization.SpaceAttributeModifierConstraintSerializer();
            new Serialization.SpaceAttributeModifierSerializer();
        }

        private void ExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SaveClick(object sender, EventArgs e)
        {

        }

        private void LoadClick(object sender, EventArgs e)
        {
            if (_openDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            if (!Directory.Exists(_openDialog.SelectedPath))
            {
                return;
            }

            Settings.Default.LastOpenedFolder = _openDialog.SelectedPath;
            Settings.Default.Save();

            LoadFactories(_openDialog.SelectedPath);
        }

        private void FactorySelected(object sender, TreeViewEventArgs e)
        {
            SelectFactory(e.Node.Name);
        }

        private void DataEditorClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.WindowBounds = DesktopBounds;
            Settings.Default.Save();
        }
    }
}

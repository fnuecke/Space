using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor : Form
    {
        private readonly CommonOpenFileDialog _openDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            EnsurePathExists = true,
            EnsureValidNames = true,
            InitialDirectory = Application.StartupPath,
            Title = "Select folder with factory XMLs"
        };

        private readonly DataEditorSettingsDialog _settingsDialog = new DataEditorSettingsDialog();

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            if (!string.IsNullOrWhiteSpace(DataEditorSettings.Default.LastOpenedFolder))
            {
                _openDialog.InitialDirectory = DataEditorSettings.Default.LastOpenedFolder;
                if (DataEditorSettings.Default.AutoLoad)
                {
                    LoadFactories(_openDialog.InitialDirectory);
                }
            }

            DesktopBounds = DataEditorSettings.Default.WindowBounds;

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
            if (_openDialog.ShowDialog(Handle) != CommonFileDialogResult.Ok)
            {
                return;
            }
            if (!Directory.Exists(_openDialog.FileName))
            {
                return;
            }

            DataEditorSettings.Default.LastOpenedFolder = _openDialog.FileName;
            DataEditorSettings.Default.Save();

            LoadFactories(_openDialog.FileName);
        }

        private void FactorySelected(object sender, TreeViewEventArgs e)
        {
            SelectFactory(e.Node.Name);
        }

        private void DataEditorClosing(object sender, FormClosingEventArgs e)
        {
            DataEditorSettings.Default.WindowBounds = DesktopBounds;
            DataEditorSettings.Default.Save();
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            _settingsDialog.ShowDialog(this);
        }

        private void PropertiesSelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            // Clear preview.
            var oldBackground = pbPreview.BackgroundImage;
            var oldImage = pbPreview.Image;
            pbPreview.BackgroundImage = null;
            pbPreview.Image = null;
            if (oldBackground != null)
            {
                oldBackground.Dispose();
            }
            if (oldImage != null)
            {
                oldImage.Dispose();
            }

            // Stop if nothing is selected.
            if (pgProperties.SelectedObject == null ||
                e.NewSelection == null ||
                e.NewSelection.PropertyDescriptor == null)
            {
                return;
            }

            // Figure out what to show. If an image asset is selected we
            // simply show that.
            if (e.NewSelection.PropertyDescriptor.Attributes[typeof(ImageAssetAttribute)] != null)
            {
                // OK, render that image (or try to).
                var fullPath = (string)e.NewSelection.Value;
                if (fullPath == null)
                {
                    return;
                }

                // Make all forward slashes backslashes for the following split.
                fullPath = fullPath.Replace('/', '\\');

                var filePath = ContentProjectManager.GetFileForTextureAsset(fullPath);
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    // We got it. Set as the new image.
                    pbPreview.ImageLocation = filePath;
                }
                return;
            }

            var objectType = pgProperties.SelectedObject.GetType();
            if (objectType == typeof(ItemFactory))
            {
                
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditorSettingsDialog : Form
    {
        private readonly CommonOpenFileDialog _openDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            EnsurePathExists = true,
            EnsureValidNames = true,
            InitialDirectory = Application.StartupPath,
            Title = "Select base directory"
        };

        public DataEditorSettingsDialog()
        {
            InitializeComponent();

            pgSettings.SelectedObject = DataEditorSettingsProxy.Default;

            var settings = DataEditorSettings.Default;
            if (!string.IsNullOrWhiteSpace(settings.LastOpenedFolder))
            {
                _openDialog.InitialDirectory = settings.LastOpenedFolder;
            }
        }

        private void BtnOkClick(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BtnSearchClick(object sender, System.EventArgs e)
        {
            if (_openDialog.ShowDialog(Handle) != CommonFileDialogResult.Ok)
            {
                return;
            }
            if (!Directory.Exists(_openDialog.FileName))
            {
                return;
            }

            var projects = new HashSet<string>(DataEditorSettings.Default.ContentProjects.Replace('/', '\\').Split(new[] {'\n'}));

            Cursor = Cursors.WaitCursor;
            foreach (var file in GetFiles(_openDialog.FileName, "*.contentproj"))
            {
                projects.Add(file.Replace('/', '\\'));
            }
            Cursor = Cursors.Default;

            DataEditorSettings.Default.ContentProjects = string.Join("\n", projects);
        }

        static IEnumerable<string> GetFiles(string path, string filter)
        {
            var paths = new Stack<string>();
            paths.Push(path);
            while (paths.Count > 0)
            {
                path = paths.Pop();
                try
                {
                    Directory.EnumerateDirectories(path).ToList().ForEach(paths.Push);
                }
                catch
                {
                }

                string[] files = null;
                try
                {

                    files = Directory.GetFiles(path, filter);
                }
                catch
                {
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}

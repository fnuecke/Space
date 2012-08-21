using System.ComponentModel;
using System.Drawing.Design;
using System.Text;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Intermediate class to get custom editor for strings.
    /// </summary>
    [Editor(typeof(ContentProjectFileNameEditor), typeof(UITypeEditor))]
    [DefaultValue("")]
    internal sealed class ContentProjectPath
    {
        public string Path = "";

        public static implicit operator string(ContentProjectPath path)
        {
            return path.Path;
        }

        public static implicit operator ContentProjectPath(string path)
        {
            return new ContentProjectPath {Path = path};
        }

        public override string ToString()
        {
            return Path;
        }
    }

    /// <summary>
    /// File chooser to open content projects. We perform some extra logic to navigate
    /// to the place of the value that is being edited.
    /// </summary>
    internal sealed class ContentProjectFileNameEditor : System.Windows.Forms.Design.FileNameEditor
    {
        private OpenFileDialog _dialog;

        private string _lastPath;

        protected override void InitializeDialog(OpenFileDialog openFileDialog)
        {
            base.InitializeDialog(openFileDialog);

            _dialog = openFileDialog;
            _dialog.Filter = @"XNA Content Project (*.contentproj)|*.contentproj";
            _dialog.ValidateNames = true;
            _dialog.CheckFileExists = true;

            if (!string.IsNullOrWhiteSpace(_lastPath) && _lastPath.Contains("\\"))
            {
                _dialog.InitialDirectory = _lastPath.Substring(0, _lastPath.LastIndexOf('\\'));
                _dialog.FileName = _lastPath.Substring(_lastPath.LastIndexOf('\\') + 1);
            }
            else if (string.IsNullOrWhiteSpace(DataEditorSettings.Default.LastOpenedFolder))
            {
                _dialog.InitialDirectory = Application.StartupPath;
            }
            else
            {
                _dialog.InitialDirectory = DataEditorSettings.Default.LastOpenedFolder;
            }
        }

        public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
            var oldPath = value is string ? (string)value : value is ContentProjectPath ? ((ContentProjectPath)value).Path : null;
            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                _lastPath = oldPath;
                if (_dialog != null && oldPath.Contains("\\"))
                {
                    _dialog.InitialDirectory = oldPath.Substring(0, oldPath.LastIndexOf('\\'));
                    _dialog.FileName = oldPath.Substring(oldPath.LastIndexOf('\\') + 1);
                }
            }
            return new ContentProjectPath {Path = (string)base.EditValue(context, provider, value)};
        }
    }

    internal static class ContentProjectPathStringExtensions
    {
        public static string Join(this ContentProjectPath[] paths, string separator)
        {
            if (paths == null || paths.Length < 1)
            {
                return string.Empty;
            }
            var sb = new StringBuilder(paths[0].Path);
            for (var i = 1; i < paths.Length; i++)
            {
                sb.Append(separator);
                sb.Append(paths[i].Path);
            }
            return sb.ToString();
        }
    }
}

using System.ComponentModel;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Proxy for actual settings used in settings property grid.
    /// </summary>
    internal sealed class DataEditorSettingsProxy
    {
        /// <summary>
        /// The default instance of the proxy.
        /// </summary>
        public static DataEditorSettingsProxy Default
        {
            get { return Instance; }
        }
        private static readonly DataEditorSettingsProxy Instance = new DataEditorSettingsProxy();

        /// <summary>
        /// Auto loading of last opened factories.
        /// </summary>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Determines whether the last opened folder should be opened again upon the next start of the application.")]
        public bool AutoLoad
        {
            get { return DataEditorSettings.Default.AutoLoad; }
            set { DataEditorSettings.Default.AutoLoad = value; }
        }

        /// <summary>
        /// Content root directory as set in game content manager.
        /// </summary>
        [DefaultValue("data")]
        [Category("Content")]
        [Description("The content root directory as set in the content manager used in the game.")]
        public string ContentRootDirectory
        {
            get { return DataEditorSettings.Default.ContentRootDirectory; }
            set { DataEditorSettings.Default.ContentRootDirectory = value; }
        }

        /// <summary>
        /// Referenced content projects.
        /// </summary>
        [Category("Content")]
        [Description("A list of folders in which to look for image assets.")]
        public ContentProjectPath[] ContentProjects
        {
            get
            {
                var rawValue = DataEditorSettings.Default.ContentProjects;
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    return new ContentProjectPath[0];
                }
                var rawPaths = rawValue.Replace('/', '\\').Split(new[] { '\n' });
                var paths = new ContentProjectPath[rawPaths.Length];
                for (var i = 0; i < rawPaths.Length; i++)
                {
                    paths[i] = new ContentProjectPath { Path = rawPaths[i] };
                }
                return paths;
            }
            set
            {
                DataEditorSettings.Default.ContentProjects = value.Join("\n");
                ContentProjectManager.Reload();
            }
        }
    }
}

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;

namespace Space
{
    /// <summary>
    /// All the game settings that can be changed and saved. Also provides
    /// utility methods for saving to and loading from XML files.
    /// </summary>
    public class Settings
    {
        #region Display

        /// <summary>
        /// The horizontal display resolution to use.
        /// </summary>
        public int ScreenWidth = 1280;

        /// <summary>
        /// The vertical display resolution to use.
        /// </summary>
        public int ScreenHeight = 720;

        /// <summary>
        /// Run full screen mode or not.
        /// </summary>
        public bool Fullscreen = false;

        /// <summary>
        /// Whether to enable post processing effects.
        /// </summary>
        public bool PostProcessing = true;

        #endregion

        #region Interface

        /// <summary>
        /// The locale to use for localized content.
        /// </summary>
        public string Language = "en";

        #endregion

        #region Miscellaneous

        /// <summary>
        /// The Name of the Player
        /// </summary>
        public string PlayerName = "Player";

        /// <summary>
        /// Address of the last server we tried to connect to.
        /// </summary>
        public string LastServerAddress = "127.0.0.1";

        /// <summary>
        /// Autoexec file, contains console commands to automatically execute
        /// after joining a game.
        /// </summary>
        public string AutoexecFilename = "autoexec.cfg";

        #endregion

        #region Key bindings

        /// <summary>
        /// The key that opens the in-game console.
        /// </summary>
        public Keys ConsoleKey = Keys.OemTilde;

        /// <summary>
        /// Key to accelerate upwards.
        /// </summary>
        public Keys MoveUp = Keys.W;

        /// <summary>
        /// Key to accelerate downwards.
        /// </summary>
        public Keys MoveDown = Keys.S;

        /// <summary>
        /// Key to accelerate left.
        /// </summary>
        public Keys MoveLeft = Keys.A;

        /// <summary>
        /// Key to accelerate right.
        /// </summary>
        public Keys MoveRight = Keys.D;

        /// <summary>
        /// Key to toggle the stabilizer.
        /// </summary>
        public Keys Stabilizer = Keys.CapsLock;

        #endregion

        #region Save / Load / Singleton

        /// <summary>
        /// Save all current values to a file with the given name.
        /// </summary>
        /// <param name="filename">the path to the file to save the settings to.</param>
        public static void Save(string filename)
        {
            try
            {
                using (Stream stream = File.Create(filename))
                {
                    // Produce minimal XML, so strip away the <?xml ... ?> header.
                    using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                    {
                        // And strip away namespaces, we don't want those.
                        new XmlSerializer(typeof(Settings)).Serialize(writer, _instance, new XmlSerializerNamespaces(new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") })));
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        /// <summary>
        /// Load settings from a file. This will invalidate any old references
        /// to <c>Settings.Instance</c>. It will also overwrite <em>all</em> settings,
        /// not just the ones loaded from the file. For all fields not set, the default
        /// values will be restored.
        /// </summary>
        /// <param name="filename">the path to the file to load the settings from.</param>
        public static void Load(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (Stream stream = File.OpenRead(filename))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                        _instance = (Settings)serializer.Deserialize(stream);
                    }
                }
                catch (IOException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        /// <summary>
        /// The singleton instance of the settings class.
        /// </summary>
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                }
                return _instance;
            }
        }

        /// <summary>
        /// The actual current instance.
        /// </summary>
        private static Settings _instance;

        /// <summary>
        /// Singleton enforcement.
        /// </summary>
        private Settings()
        {
        }

        #endregion
    }
}

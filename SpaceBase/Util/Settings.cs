using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Data;
using Space.Input;

namespace Space.Util
{
    /// <summary>
    /// All the game settings that can be changed and saved. Also provides
    /// utility methods for saving to and loading from XML files.
    /// </summary>
    public sealed class Settings
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// This determines how fast the actual game logic is updated. It is measured in
        /// ticks per second, i.e. how many update cycles to drive per second.
        /// </summary>
        public const float TicksPerSecond = 20f;

        #endregion

        #region Display

        /// <summary>
        /// The horizontal display resolution to use.
        /// </summary>
        [ScriptAccess("ScreenResolution", Options = new object[]
            {
                "640x480",
                "800x600",
                "1024x600",
                "1024x768",
                "1152x864",
                "1280x720",
                "1280x768",
                "1280x800",
                "1280x960",
                "1280x1024",
                "1360x768",
                "1366x768",
                "1400x1050",
                "1440x900",
                "1600x900",
                "1600x1200",
                "1680x1050",
                "1920x1080",
                "1920x1200",
                "2048x1152",
                "2560x1440",
                "2560x1600"
            })]
        public Point ScreenResolution = new Point(1280, 720);

        /// <summary>
        /// Run full screen mode or not.
        /// </summary>
        [ScriptAccess("Fullscreen")]
        public bool Fullscreen;

        /// <summary>
        /// Whether to enable post processing effects.
        /// </summary>
        public bool PostProcessing = true;

        /// <summary>
        /// The scaling of the GUI.
        /// </summary>
        [ScriptAccess("GuiScale", MinValue = 0.25, MaxValue = 2.0)]
        public float GuiScale = 1.0f;

        /// <summary>
        /// Compute the distance displayed in radar icons as the distance of
        /// the object to the screen edge instead of to the player ship.
        /// </summary>
        [ScriptAccess("RadarDistanceType")]
        public bool RadarDistanceFromBorder = true;

        #endregion

        #region Interface

        /// <summary>
        /// The locale to use for localized content.
        /// </summary>
        [ScriptAccess("Language", Options = new object[] {"en", "de"})]
        public string Language = "en";

        #endregion

        #region Player

        /// <summary>
        /// The Name of the Player
        /// </summary>
        [ScriptAccess("PlayerName")]
        public string PlayerName = "Player";

        /// <summary>
        /// The name of the profile currently in use.
        /// </summary>
        [ScriptAccess("Profile")]
        public string CurrentProfileName = string.Empty;

        /// <summary>
        /// The actual profile currently in use.
        /// </summary>
        [XmlIgnore]
        public IProfile CurrentProfile;

        #endregion

        #region Miscellaneous

        /// <summary>
        /// Relative or absolute path to the folder to store profiles in.
        /// </summary>
        public string ProfileFolder = "save";

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

        #region Input

        /// <summary>
        /// Input bindings for game control as set by the player.
        /// </summary>
        [ScriptAccess("GameBindings")]
        public InputBindings<GameCommand> GameBindings = new InputBindings<GameCommand>();

        /// <summary>
        /// Input bindings for gamepad axis control as set by the player.
        /// </summary>
        [ScriptAccess("AxisBindings")]
        public InputBindings<GamePadCommand> AxisBindings = new InputBindings<GamePadCommand>();

        /// <summary>
        /// Whether to toggle stabilizer functionality or keep it active only
        /// while the key is pressed.
        /// </summary>
        [ScriptAccess("StabilizeToggles")]
        public bool StabilizeToggles = true;

        /// <summary>
        /// Whether to toggle shield functionality or keep it active only
        /// while the key is pressed.
        /// </summary>
        [ScriptAccess("ShieldToggles")]
        public bool ShieldToggles;

        /// <summary>
        /// Whether to use a game pad, if attached, for input.
        /// </summary>
        [ScriptAccess("UseGamepad")]
        public bool EnableGamepad = false;

        /// <summary>
        /// Epsilon value below which to ignore axis values (to compensate for
        /// construction based inaccuracies in game pads).
        /// </summary>
        public float GamePadDetectionEpsilon = 0.15f;

        /// <summary>
        /// Invert the horizontal acceleration axis.
        /// </summary>
        public bool InvertGamepadAccelerationAxisX;

        /// <summary>
        /// Invert the vertical acceleration axis.
        /// </summary>
        public bool InvertGamepadAccelerationAxisY = true;

        /// <summary>
        /// Invert the horizontal look axis.
        /// </summary>
        public bool InvertGamepadLookAxisX;

        /// <summary>
        /// Invert the vertical look axis.
        /// </summary>
        public bool InvertGamepadLookAxisY = true;

        #region Update Methods

        /// <summary>
        /// Restores the default input bindings for ingame commands.
        /// </summary>
        public void SetDefaultGameBindings()
        {
            GameBindings.Clear();
            GameBindings.Add(GameCommand.Up, Keys.W);
            GameBindings.Add(GameCommand.Up, Keys.Up);
            GameBindings.Add(GameCommand.Down, Keys.S);
            GameBindings.Add(GameCommand.Down, Keys.Down);
            GameBindings.Add(GameCommand.Left, Keys.A);
            GameBindings.Add(GameCommand.Left, Keys.Left);
            GameBindings.Add(GameCommand.Right, Keys.D);
            GameBindings.Add(GameCommand.Right, Keys.Right);
            GameBindings.Add(GameCommand.Stabilize, Keys.LeftShift);
            GameBindings.Add(GameCommand.Stabilize, Keys.RightShift);
            GameBindings.Add(GameCommand.Stabilize, Buttons.LeftShoulder);
            GameBindings.Add(GameCommand.ZoomIn, MouseWheel.Up);
            GameBindings.Add(GameCommand.ZoomIn, Buttons.LeftTrigger);
            GameBindings.Add(GameCommand.ZoomOut, MouseWheel.Down);
            GameBindings.Add(GameCommand.ZoomOut, Buttons.RightTrigger);

            GameBindings.Add(GameCommand.Shoot, MouseButtons.Left);
            GameBindings.Add(GameCommand.Shoot, Buttons.RightShoulder);
            GameBindings.Add(GameCommand.Use, Keys.E);
            GameBindings.Add(GameCommand.Use, Keys.Enter);
            GameBindings.Add(GameCommand.Use, Buttons.A);
            GameBindings.Add(GameCommand.Shield, Keys.Space);
            GameBindings.Add(GameCommand.Shield, Buttons.B);
            GameBindings.Add(GameCommand.PickUp, Keys.F);
            GameBindings.Add(GameCommand.PickUp, Buttons.X);

            GameBindings.Add(GameCommand.Back, Keys.Back);
            GameBindings.Add(GameCommand.Back, Keys.Escape);
            GameBindings.Add(GameCommand.Menu, Keys.Pause);
            GameBindings.Add(GameCommand.Menu, Keys.F10);
            GameBindings.Add(GameCommand.Inventory, Keys.I);
            GameBindings.Add(GameCommand.Character, Keys.C);

            GameBindings.Add(GameCommand.ToggleGraphs, Keys.F1);
            GameBindings.Add(GameCommand.Console, Keys.OemTilde);

            GameBindings.Add(GameCommand.TestCommand, Keys.T);
        }

        /// <summary>
        /// Restores the default input bindings for gamepad axii.
        /// </summary>
        public void SetDefaultAxisBindings()
        {
            AxisBindings.Clear();
            AxisBindings.Add(GamePadCommand.AccelerateX, ExtendedAxes.X);
            AxisBindings.Add(GamePadCommand.AccelerateY, ExtendedAxes.Y);
            AxisBindings.Add(GamePadCommand.LookX, ExtendedAxes.RotationX);
            AxisBindings.Add(GamePadCommand.LookY, ExtendedAxes.RotationY);

            // For Logitech Rumblepad
            AxisBindings.Add(GamePadCommand.LookX, ExtendedAxes.Z);
            AxisBindings.Add(GamePadCommand.LookY, ExtendedAxes.RotationZ);
        }

        #endregion

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
                // Produce minimal XML, so strip away the <?xml ... ?> header.
                using (var writer = XmlWriter.Create(File.Create(filename), new XmlWriterSettings {OmitXmlDeclaration = true, Indent = true}))
                {
                    // And strip away namespaces, we don't want those.
                    new XmlSerializer(typeof(Settings)).Serialize(writer, _instance, new XmlSerializerNamespaces(new XmlSerializerNamespaces(new[] {new XmlQualifiedName("", "")})));
                }
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Could not save settings.", ex);
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
            if (!File.Exists(filename))
            {
                Logger.Info("No existing settings found, using defaults.");
                return;
            }
            try
            {
                using (Stream stream = File.OpenRead(filename))
                {
                    var serializer = new XmlSerializer(typeof(Settings));
                    _instance = (Settings)serializer.Deserialize(stream);
                }
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Could not load settings.", ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.ErrorException("Could not load settings.", ex);
            }
        }

        /// <summary>
        /// The singleton instance of the settings class.
        /// </summary>
        public static Settings Instance
        {
            get { return _instance ?? (_instance = new Settings()); }
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
            SetDefaultGameBindings();
            SetDefaultAxisBindings();
        }

        #endregion
    }
}

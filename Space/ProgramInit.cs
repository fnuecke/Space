using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Space.ComponentSystem.Systems;
using Space.Control;
using Space.Simulation.Commands;
using Space.Util;

namespace Space
{
    /// <summary>
    /// Initialization of any components/objects we need while the game is running.
    /// </summary>
    internal partial class Program
    {
        /// <summary>
        /// Called after the Game and GraphicsDevice are created, but before LoadContent.
        /// </summary>
        protected override void Initialize()
        {
            // Initialize the console as soon as possible.
            InitializeConsole();

            // Initialize localization. Anything after this loaded via the content
            // manager will be localized.
            InitializeLocalization();

            // Set up input to allow interaction with the game.
            InitializeInput();

            base.Initialize();
        }

        /// <summary>
        /// Initialize the localization by figuring out which to use, either by getting
        /// it from the settings, or by falling back to the default one instead.
        /// </summary>
        private void InitializeLocalization()
        {
            // Get locale for localized content.
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(Settings.Instance.Language);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.InvariantCulture;
                Settings.Instance.Language = culture.Name;
            }

            // Set up resources.
            GuiStrings.Culture = culture;
            AttributeNames.Culture = culture;
            AttributePrefixes.Culture = culture;
            ItemDescriptions.Culture = culture;
            ItemNames.Culture = culture;
            QualityNames.Culture = culture;

            // Set up content loader.
            ((LocalizedContentManager)Content).Culture = culture;
        }

        /// <summary>
        /// Initialize input logic.
        /// </summary>
        private void InitializeInput()
        {
            // Initialize input.
            _inputManager = new InputManager(Services, Window.Handle);
            Components.Add(_inputManager);
            Services.AddService(typeof(InputManager), _inputManager);

            // Create the input handler that converts input to ingame commands.
            _input = new InputHandler(this);
            Components.Add(_input);
        }

        /// <summary>
        /// Initialize the console, adding commands and making the logger write to it.
        /// </summary>
        private void InitializeConsole()
        {
            // Create the console and add it as a component.
            _console = new GameConsole(this);
            Components.Add(_console);

            // We do this in the input handler.
            _console.Hotkey = Keys.None;

            // Add a logging target that'll write to our console.
            _consoleLoggerTarget = new GameConsoleTarget(this, NLog.LogLevel.Trace);

            _console.AddCommand(new[] { "fullscreen", "fs" },
                args => GraphicsDeviceManager.ToggleFullScreen(),
                "Toggles fullscreen mode.");

            _console.AddCommand("search",
                args => _client.Controller.Session.Search(),
                "Search for games available on the local subnet.");
            _console.AddCommand("connect",
                args => _client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[1]), 7777), Settings.Instance.PlayerName, Settings.Instance.CurrentProfile),
                "Joins a game at the given host.",
                "connect <host> - join the host with the given host name or IP.");
            _console.AddCommand("leave",
                args => DisposeClient(),
                "Leave the current game.");

            // Register debug commands.
            InitializeConsoleForDebug();

            // Say hi.
            _console.WriteLine("Console initialized. Type 'help' for available commands.");
        }

        /// <summary>
        /// Debug commands for the console, that won't be available in release builds.
        /// </summary>
        [Conditional("DEBUG")]
        private void InitializeConsoleForDebug()
        {
            // Default handler to interpret everything that is not a command
            // as a script.
            _console.SetDefaultCommandHandler(
                command =>
                {
                    if (_client != null)
                    {
                        _client.Controller.PushLocalCommand(new ScriptCommand(command));
                    }
                    else
                    {
                        _console.WriteLine("Unknown command.");
                    }
                });

            // Add hints for auto completion to also complete python methods.
            _console.AddAutoCompletionLookup(SpaceCommandHandler.GetGlobalNames);

            _console.AddCommand("d_ai",
                args => SetDebugRenderSystemEnabled<DebugAIRenderSystem>(args[1]),
                "Enables rendering debug information on AI ships.",
                "d_ai 1|0 - set whether to enabled rendering AI debug info.");

            _console.AddCommand("d_renderindex",
                args =>
                {
                    int index;
                    if (!int.TryParse(args[1], out index))
                    {
                        switch (args[1])
                        {
                            case "c":
                            case "collision":
                            case "collidable":
                            case "collidables":
                                _indexGroupMask = CollisionSystem.IndexGroupMask;
                                break;
                            case "d":
                            case "detector":
                            case "detectable":
                            case "detectables":
                                _indexGroupMask = DetectableSystem.IndexGroupMask;
                                break;
                            case "g":
                            case "grav":
                            case "gravitation":
                                _indexGroupMask = GravitationSystem.IndexGroupMask;
                                break;
                            case "s":
                            case "sound":
                            case "sounds":
                                _indexGroupMask = SoundSystem.IndexGroupMask;
                                break;
                        }
                    }
                    else if (index > 64)
                    {
                        _console.WriteLine("Invalid index, must be smaller or equal to 64.");
                    }
                    else
                    {
                        _indexGroupMask = 1ul << index;
                    }
                },
                "Enables rendering of the index with the given index.",
                "d_renderindex <index> - render the cells of the specified index.");

            _console.AddCommand("d_speed",
                args =>
                {
                    _server.Controller.TargetSpeed = float.Parse(args[1]);
                },
                "Sets the target gamespeed.",
                "d_speed <x> - set the target game speed to the specified value.");

            _console.AddCommand("r_collbounds",
                args => SetDebugRenderSystemEnabled<DebugCollisionBoundsRenderSystem>(args[1]),
                "Sets whether to render collision bounds of objects.",
                "r_collbounds 1|0 - set whether to render collision bounds.");

            _console.AddCommand("r_entityid",
                args => SetDebugRenderSystemEnabled<DebugEntityIdRenderSystem>(args[1]),
                "Sets whether to render entitiy ids at entity position.",
                "r_entityid 1|0 - set whether to render entity ids.");

            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            _console.LineWritten += (sender, e) => Console.WriteLine(((LineWrittenEventArgs)e).Message);
        }

        private void SetDebugRenderSystemEnabled<T>(string value) where T : AbstractSystem, IDrawingSystem
        {
            switch (value)
            {
                case "1":
                case "on":
                case "true":
                case "yes":
                    _client.GetSystem<T>().Enabled = true;
                    break;
                default:
                    _client.GetSystem<T>().Enabled = false;
                    break;
            }
        }
    }
}

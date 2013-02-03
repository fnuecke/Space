using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Space.ComponentSystem.Systems;
using Space.Control;
using Space.Simulation.Commands;
using Space.Util;

namespace Space
{
    /// <summary>Initialization of any components/objects we need while the game is running.</summary>
    internal partial class Program
    {
        /// <summary>Called after the Game and GraphicsDevice are created, but before LoadContent.</summary>
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
        ///     Initialize the localization by figuring out which to use, either by getting it from the settings, or by
        ///     falling back to the default one instead.
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
            ((LocalizedContentManager) Content).Culture = culture;
        }

        /// <summary>Initialize input logic.</summary>
        private void InitializeInput()
        {
            // Initialize input.
            _inputManager = new InputManager(Services, Window.Handle);
            Components.Add(_inputManager);
            Services.AddService(typeof (InputManager), _inputManager);

            // Create the input handler that converts input to ingame commands.
            _input = new InputHandler(this);
            Components.Add(_input);
        }

        /// <summary>Initialize the console, adding commands and making the logger write to it.</summary>
        private void InitializeConsole()
        {
            // Create the console and add it as a component.
            _console = new GameConsole(this);
            Components.Add(_console);

            // We do this in the input handler.
            _console.Hotkey = Keys.None;

            // Add a logging target that'll write to our console.
            _consoleLoggerTarget = new GameConsoleTarget(this, NLog.LogLevel.Trace);

            _console.AddCommand(
                new[] {"fullscreen", "fs"},
                args => GraphicsDeviceManager.ToggleFullScreen(),
                "Toggles fullscreen mode.");

            _console.AddCommand(
                "search",
                args => _client.Controller.Session.Search(),
                "Search for games available on the local subnet.");
            _console.AddCommand(
                "connect",
                args =>
                _client.Controller.Session.Join(
                    new IPEndPoint(IPAddress.Parse(args[1]), 7777),
                    Settings.Instance.PlayerName,
                    Settings.Instance.CurrentProfile),
                "Joins a game at the given host.",
                "connect <host> - join the host with the given host name or IP.");
            _console.AddCommand(
                "leave",
                args => DisposeClient(),
                "Leave the current game.");

            // Register debug commands.
            InitializeConsoleForDebug();

            // Say hi.
            _console.WriteLine("Console initialized. Type 'help' for available commands.");
        }

        /// <summary>Debug commands for the console, that won't be available in release builds.</summary>
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
            _console.AddAutoCompletionLookup(() => _client != null ? _client.GetSystem<ScriptSystem>().GlobalNames : Enumerable.Empty<string>());
            
            _console.AddCommand(
                "d_dump",
                args =>
                {
                    const string filename = "dump_{0}_{1}.txt";
                    var id = DateTime.UtcNow.Ticks.ToString("D");
                    if (args.Length > 0)
                    {
                        id = args[1];
                    }

                    while (_client.Controller.Simulation.CurrentFrame < _server.Controller.Simulation.CurrentFrame)
                    {
                        _client.Controller.Update(1f / 60f);
                    }
                    while (_server.Controller.Simulation.CurrentFrame < _client.Controller.Simulation.CurrentFrame)
                    {
                        _server.Controller.Update(1f / 60f);
                    }

                    if (_client != null)
                    {
                        using (var w = new StreamWriter(string.Format(filename, id, "client")))
                        {
                            w.Write("Simulation = ");
                            w.Dump(_client.Controller.Simulation);
                        }
                    }
                    if (_server != null)
                    {
                        using (var w = new StreamWriter(string.Format(filename, id, "server")))
                        {
                            w.Write("Simulation = ");
                            w.Dump(_server.Controller.Simulation);
                        }
                    }
                },
                "Writes a dump of the current game state to a file. If a name is omitted",
                "one will be chosen at random.",
                "d_dump <filename> - writes the game state dump to the specified file.");

            _console.AddCommand(
                "d_pause",
                args =>
                {
                    var paused = ParseBool(args[1]);
                    if (_client != null)
                    {
                        _client.Paused = paused;
                    }
                    if (_server != null)
                    {
                        _server.Paused = paused;
                    }
                },
                "Sets whether to pause simulation updating. If enabled, sessions will still",
                "be updated, the actual simulation however will not.",
                "d_pause 1|0 - sets whether to pause the simulation or not.");

            _console.AddCommand(
                "d_speed",
                args => { _server.Controller.TargetSpeed = float.Parse(args[1]); },
                "Sets the target gamespeed.",
                "d_speed <x> - set the target game speed to the specified value.");

            _console.AddCommand(
                "d_step",
                args =>
                {
                    var updates = args.Length > 0 ? int.Parse(args[1]) : 1;
                    if (_client != null)
                    {
                        for (var i = 0; i < updates; i++)
                        {
                            _client.Controller.Update(1000f / Settings.TicksPerSecond);
                        }
                    }
                    if (_server != null)
                    {
                        for (var i = 0; i < updates; i++)
                        {
                            _server.Controller.Update(1000f / Settings.TicksPerSecond);
                        }
                    }
                },
                "Performs a single update for the server and client if they exist.",
                "step <frames> - applies the specified number of updates.");

            _console.AddCommand(
                "r_ai",
                args =>
                {
                    _client.GetSystem<DebugAIRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables rendering debug information on AI ships.",
                "r_ai 1|0 - set whether to enabled rendering AI debug info.");
            
            _console.AddCommand(
                "r_background",
                args =>
                {
                    _client.GetSystem<BackgroundRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables background rendering.",
                "r_background 1|0 - set whether to render the background.");
            
            _console.AddCommand(
                "r_entity",
                args =>
                {
                    _client.GetSystem<DebugEntityIdRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Sets whether to render entity info at entity position.",
                "r_entity 1|0 - set whether to render entity info.");

            _console.AddCommand(
                "r_interpolate",
                args =>
                {
                    _client.GetSystem<InterpolationSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables position and angle interpolation for rendering.",
                "r_interpolate 1|0 - set whether to interpolate positions and angles.");
            
            _console.AddCommand(
                "r_pfx",
                args =>
                {
                    _client.GetSystem<ParticleEffectSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables particle effect rendering.",
                "r_pfx 1|0 - set whether to render particle effects.");
            
            _console.AddCommand(
                "r_postprocessing",
                args =>
                {
                    _client.GetSystem<TextureRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables post-processing effects such as bloom.",
                "r_postprocessing 1|0 - set whether to apply post-processing effects.");
            
            _console.AddCommand(
                "r_physics",
                args =>
                {
                    var physics = _client.GetSystem<DebugPhysicsRenderSystem>();
                    var enable = ParseBool(args[2]);
                    foreach (var c in args[1])
                    {
                        switch (c)
                        {
                            case 'f':
                                physics.RenderFixtures = enable;
                                break;
                            case 'b':
                                physics.RenderFixtureBounds = enable;
                                break;
                            case 'c':
                                physics.RenderContactPoints = enable;
                                break;
                            case 'm':
                                physics.RenderCenterOfMass = enable;
                                break;
                            case 'n':
                                physics.RenderContactNormals = enable;
                                break;
                            case 'i':
                                physics.RenderContactPointNormalImpulse = enable;
                                break;
                            case 'j':
                                physics.RenderJoints = enable;
                                break;
                        }
                    }
                },
                "Sets for which parts of the physics simulation to render debug representations for.",
                "r_physics [fbcmni]+ 1|0");
            
            _console.AddCommand(
                "r_radar",
                args =>
                {
                    _client.GetSystem<RadarRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables radar rendering.",
                "r_radar 1|0 - set whether to render the radar.");
            
            _console.AddCommand(
                "r_sound",
                args =>
                {
                    _client.GetSystem<SoundSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables sound.",
                "r_sound 1|0 - set whether to play sound.");
            
            _console.AddCommand(
                "r_textures",
                args =>
                {
                    _client.GetSystem<TextureRenderSystem>().Enabled = ParseBool(args[1]);
                },
                "Enables or disables texture rendering.",
                "r_textures 1|0 - set whether to render textures.");
            
            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            _console.LineWritten += (sender, e) => Console.WriteLine(((LineWrittenEventArgs) e).Message);
        }

        private static bool ParseBool(string value)
        {
            switch (value)
            {
                case "1":
                case "on":
                case "true":
                case "yes":
                    return true;
                default:
                    return false;
            }
        }
    }
}
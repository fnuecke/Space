using System;
using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Input;
using Engine.Util;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using NLog;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Control;
using Space.Data;
using Space.View;

namespace Space
{
    /// <summary>
    /// Main class, sets up services and basic components.
    /// </summary>
    public class Spaaace : Microsoft.Xna.Framework.Game
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string SettingsFile = "config.xml";

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameConsole console;

        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;

        public Spaaace()
        {
            logger.Info("Starting up program...");

            // Load settings. Save on exit.
            Settings.Load(SettingsFile);
            Exiting += (object sender, EventArgs e) =>
            {
                logger.Info("Shutting down program...");
                Settings.Save(SettingsFile);
            };

            // Set up display.
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Settings.Instance.ScreenWidth;
            graphics.PreferredBackBufferHeight = Settings.Instance.ScreenHeight;
            graphics.IsFullScreen = Settings.Instance.Fullscreen;
            
            // We really want to do this, because it keeps the game from running at one billion
            // frames per second -- which sounds fun, but isn't, because game states won't update
            // properly anymore (because elapsed time since last step will always appear to be zero).
            graphics.SynchronizeWithVerticalRetrace = true;

            // XNAs fixed time step implementation doesn't suit us, to be gentle.
            // So we let it be dynamic and adjust for it as necessary, leading
            // to almost no desyncs at all! Yay!
            this.IsFixedTimeStep = false;

            // Create our own, localized content manager.
            Content = new LocalizedContentManager(Services);

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
            Strings.Culture = culture;
            ((LocalizedContentManager)Content).Culture = culture;

            // Remember to keep this in sync with the content project.
            Content.RootDirectory = "data";

            // Some window settings.
            Window.Title = "Space. The Game. Seriously.";
            IsMouseVisible = true;

            // Add some more utility components.
            Components.Add(new KeyboardInputManager(this));
            Components.Add(new MouseInputManager(this));

            console = new GameConsole(this);
            console.Hotkey = Settings.Instance.ConsoleKey;
            Components.Add(console);

            // Create the screen manager component.
            var screenManager = new ScreenManager(this);
            Components.Add(screenManager);

            // Activate the first screens.
            screenManager.AddScreen(new BackgroundScreen());
            screenManager.AddScreen(new MainMenuScreen());

            // Add a logging target that'll write to our console.
            new GameConsoleTarget(this, LogLevel.Debug);

            console.AddCommand(new[] { "fullscreen", "fs" }, args =>
            {
                graphics.ToggleFullScreen();
            },
                "Toggles fullscreen mode.");

            // Copy everything written to our game console to the actual console,
            // too, so we can inspect it out of game, copy stuff or read it after
            // the game has crashed.
            console.LineWritten += delegate(object sender, EventArgs e)
            {
                Console.WriteLine(((LineWrittenEventArgs)e).Message);
            };
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(typeof(SpriteBatch), spriteBatch);

            console.SpriteBatch = spriteBatch;
            console.Font = Content.Load<SpriteFont>("Fonts/ConsoleFont");

            console.WriteLine("Game Console. Type 'help' for available commands.");

            // Set up audio stuff.

            audioEngine = new AudioEngine("data/Audio/SpaceAudio.xgs");
            waveBank = new WaveBank(audioEngine, "data/Audio/Wave Bank.xwb");
            soundBank = new SoundBank(audioEngine, "data/Audio/Sound Bank.xsb");

            audioEngine.Update();

            Services.AddService(typeof(SoundBank), soundBank);

#if DEBUG
            arrow = Content.Load<Texture2D>("Textures/arrow");
#endif
        }

        protected override void Update(GameTime gameTime)
        {
            audioEngine.Update();

            base.Update(gameTime);
        }

#if DEBUG
        private Texture2D arrow;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            spriteBatch.Begin();

            string info = String.Format("FPS: {0:f} | Slow: {1}",
                System.Math.Ceiling(1 / (float)gameTime.ElapsedGameTime.TotalSeconds), gameTime.IsRunningSlowly);
            var infoPosition = new Vector2(GraphicsDevice.Viewport.Width - 10 - console.Font.MeasureString(info).X, 10);

            spriteBatch.DrawString(console.Font, info, infoPosition, Color.White);

            spriteBatch.End();

            foreach (var component in Components)
            {
                if (component is GameClient)
                {
                    var client = (GameClient)component;
                    var session = client.Controller.Session;
                    var entityManager = client.Controller.Simulation.EntityManager;
                    var systemManager = entityManager.SystemManager;
                    

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(GraphicsDevice.Viewport.Width - 230, GraphicsDevice.Viewport.Height - 140);
                    var sessionOffset = new Vector2(GraphicsDevice.Viewport.Width - 360, GraphicsDevice.Viewport.Height - 140);

                    SessionInfo.Draw("Client", session, sessionOffset, console.Font, spriteBatch);
                    //NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);

                    // Draw planet arrows and stuff.
                    if (session.ConnectionState == Engine.Session.ClientState.Connected)
                    {
                        var avatar = systemManager.GetSystem<AvatarSystem>().GetAvatar(session.LocalPlayer.Number);
                        if (avatar != null)
                        {
                            spriteBatch.Begin();

                            var cellX = ((int)avatar.GetComponent<Transform>().Translation.X) >> CellSystem.CellSizeShiftAmount;
                            var cellY = ((int)avatar.GetComponent<Transform>().Translation.Y) >> CellSystem.CellSizeShiftAmount;
                            var x = avatar.GetComponent<Transform>().Translation.X;
                            var y = avatar.GetComponent<Transform>().Translation.Y;
                            var id = ((ulong)cellX << 32) | (uint)cellY;

                            var universe = systemManager.GetSystem<UniversalSystem>();
                            var count = 0;
                            if (universe != null)
                            {
                                var list = universe.GetSystemList(id);

                                spriteBatch.DrawString(console.Font, "Cellx: " + cellX + " CellY: " + cellY + "posx: " + x + " PosY" + y, new Vector2(20, 20), Color.White);
                                var screensize =
                                            Math.Sqrt(Math.Pow(GraphicsDevice.Viewport.Width / 2.0, 2) +
                                                        Math.Pow(GraphicsDevice.Viewport.Height / 2.0, 2));
                                foreach (var i in list)
                                {
                                    var entity = entityManager.GetEntity(i);
                                    if (entity != null && entity.GetComponent<Transform>() != null)
                                    {

                                        var color = Color.Teal;
                                        switch (entity.GetComponent<AstronomicBody>().Type)
                                        {
                                            case AstronomicBodyType.Sun:
                                                color = Color.Yellow;
                                                break;
                                            case AstronomicBodyType.Planet:
                                                color = Color.Blue;
                                                break;
                                            case AstronomicBodyType.Moon:
                                                color = Color.Gray;
                                                break;
                                        }
                                        var position = entity.GetComponent<Transform>().Translation;

                                        var distX = Math.Abs((double)position.X - (double)x);
                                        var distY = Math.Abs((double)position.Y - (double)y);
                                        var distance = Math.Sqrt(Math.Pow((double)position.Y - (double)y, 2) +
                                                        Math.Pow((double)position.X - (double)x, 2));
                                        count++;
                                        var phi = Math.Atan2((double)position.Y - (double)y, (double)position.X - (double)x);
                                        var arrowPos = new Vector2(GraphicsDevice.Viewport.Width / 2.0f,
                                                                    GraphicsDevice.Viewport.Height / 2.0f);
                                        arrowPos.X += GraphicsDevice.Viewport.Height / 2.0f * (float)Math.Cos(phi);

                                        arrowPos.Y += GraphicsDevice.Viewport.Height / 2.0f * (float)Math.Sin(phi);
                                        //Console.WriteLine(arrowPos);
                                        var size = 40 / distance;
                                        if (distX > GraphicsDevice.Viewport.Width / 2.0 || distY > GraphicsDevice.Viewport.Height / 2.0)
                                            spriteBatch.Draw(arrow, arrowPos, null, color, (float)phi, new Vector2(arrow.Width / 2.0f, arrow.Height / 2.0f), (float)size,
                                                            SpriteEffects.None, 1);
                                        spriteBatch.DrawString(console.Font, "Position: " + position + "phi:" + phi + " Distance: " + distance + "size: " + size, new Vector2(20, count * 20 + 20), Color.White);

                                        //spriteBatch.Draw(rocketTexture, rocketPosition, null, players[currentPlayer].Color, rocketAngle, new Vector2(42, 240), 0.1f, SpriteEffects.None, 1);

                                    }

                                }
                                spriteBatch.DrawString(console.Font, "Count: " + count, new Vector2(20, count * 20 + 40), Color.White);
                            }

                            var health = avatar.GetComponent<Health>();
                            if (health != null)
                            {
                                spriteBatch.DrawString(console.Font, "Health: " + health.Value, new Vector2(20, count * 20 + 100), Color.White);
                            }
                            var energy = avatar.GetComponent<Energy>();
                            if (energy != null)
                            {
                                spriteBatch.DrawString(console.Font, "Energy: " + energy.Value, new Vector2(20, count * 20 + 120), Color.White);
                            }
                            spriteBatch.End();
                        }
                    }
                }
                else if (component is GameServer)
                {
                    var server = (GameServer)component;
                    var session = server.Controller.Session;

                    // Draw session info and netgraph.
                    var ngOffset = new Vector2(150, GraphicsDevice.Viewport.Height - 140);
                    var sessionOffset = new Vector2(10, GraphicsDevice.Viewport.Height - 140);

                    SessionInfo.Draw("Server", session, sessionOffset, console.Font, spriteBatch);
                    //NetGraph.Draw(protocol.Information, ngOffset, font, spriteBatch);
                }
            }
        }
#endif
    }
}

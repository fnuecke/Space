using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Simulation;

namespace Space.Control
{
    public class GameClient : DrawableGameComponent
    {
        #region Logger
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties
        
        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public SimpleClientController<PlayerInfo> Controller { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        ///  Command emitter used to get player input.
        /// </summary>
        private InputCommandEmitter _emitter;

        private SpriteBatch SpriteBatch;
        private Texture2D arrow;
        private SpriteFont font;
        #endregion

        public GameClient(Game game)
            : base(game)
        {
            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            SpriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            arrow = game.Content.Load<Texture2D>("Textures/arrow");
            font = game.Content.Load<SpriteFont>("Fonts/ConsoleFont");
            // Create our client controller.
            Controller = new SimpleClientController<PlayerInfo>(GameCommandHandler.HandleCommand);

            // Register for events.
            Controller.Session.PlayerJoined += HandlePlayerJoined;
            Controller.Session.PlayerLeft += HandlePlayerLeft;

            // Add all systems we need in our game.
            Controller.Simulation.EntityManager.SystemManager.AddSystems(
                new[]
                {
                    new DefaultLogicSystem(),
                    new ShipControlSystem(),
                    new AvatarSystem(),
                    new CellSystem(),
                    new PlayerCenteredSoundSystem(soundBank, Controller.Session),
                    new PlayerCenteredRenderSystem(SpriteBatch, game.Content, Controller.Session)
                                .AddComponent(new Background("Textures/stars")),
                    new UniversalSystem(game.Content.Load<WorldConstaints>("Data/world"))
                });

            // Create our input command emitter, which is used to grab user
            // input and convert it into commands that can be injected into our
            // simulation.
            _emitter = new InputCommandEmitter(game, Controller.Session, Controller.Simulation);
            Controller.AddEmitter(_emitter);
            Game.Components.Add(_emitter);

            // Draw underneath menus etc.
            DrawOrder = -50;
        }

        protected override void Dispose(bool disposing)
        {
            Controller.Session.PlayerJoined -= HandlePlayerJoined;
            Controller.Session.PlayerLeft -= HandlePlayerLeft;

            Controller.RemoveEmitter(_emitter);

            Game.Components.Remove(_emitter);

            _emitter.Dispose();

            Controller.Dispose();

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            Controller.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Controller.Draw();
            
            SpriteBatch.Begin();

            try
            {
                var entityManager = Controller.Simulation.EntityManager;
                var systemManager = entityManager.SystemManager;
                var avatar = systemManager.GetSystem<AvatarSystem>().GetAvatar(Controller.Session.LocalPlayer.Number);
                var cellSize = systemManager.GetSystem<CellSystem>().CellSize;
                var cellX = ((int)avatar.GetComponent<Transform>().Translation.X)/cellSize;
                var cellY = ((int)avatar.GetComponent<Transform>().Translation.Y) /cellSize;
                var x = avatar.GetComponent<Transform>().Translation.X;
                var y = avatar.GetComponent<Transform>().Translation.Y;
                var id = ((ulong)cellX << 32) | (uint)cellY;

                var list = systemManager.GetSystem<UniversalSystem>().GetSystemList(id);
                SpriteBatch.DrawString(font, "Cellx: " + cellX + " CellY: " + cellY + "posx: " + x + " PosY" + y, new Vector2(20, 20), Color.White);
                var count = 0;
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
                       
                        var distX = Math.Abs((double) position.X - (double) x);
                        var distY = Math.Abs((double)position.Y - (double)y);
                        var distance = Math.Sqrt(Math.Pow((double)position.Y - (double)y, 2) +
                                      Math.Pow((double)position.X - (double)x, 2));
                        count++;
                        var phi = Math.Atan2((double)position.Y - (double)y, (double)position.X - (double)x);
                        var arrowPos = new Vector2(GraphicsDevice.Viewport.Width/2.0f,
                                                   GraphicsDevice.Viewport.Height/2.0f);
                        arrowPos.X += GraphicsDevice.Viewport.Height/2.0f*(float) Math.Cos(phi);

                        arrowPos.Y += GraphicsDevice.Viewport.Height / 2.0f * (float)Math.Sin(phi);
                        //Console.WriteLine(arrowPos);
                        var size = 20/distance;
                        if (distX > GraphicsDevice.Viewport.Width / 2.0||distY>GraphicsDevice.Viewport.Height/2.0)
                            SpriteBatch.Draw(arrow, arrowPos, null, color, (float)phi, new Vector2(arrow.Width / 2.0f, arrow.Height / 2.0f), (float)size,
                                         SpriteEffects.None,1);
                        SpriteBatch.DrawString(font, "Position: " + position + "phi:" + phi+" Distance: "+distance+ "size: "+size, new Vector2(20, count * 20+20), Color.White);

                        //spriteBatch.Draw(rocketTexture, rocketPosition, null, players[currentPlayer].Color, rocketAngle, new Vector2(42, 240), 0.1f, SpriteEffects.None, 1);

                    }

                }
                SpriteBatch.DrawString(font, "Count: " + count, new Vector2(20, count * 20 + 40), Color.White);

                string activeSystems = "Active cells: ";
                foreach (var item in Controller.Simulation.EntityManager.SystemManager.GetSystem<CellSystem>().ActiveSystems)
                {
                    activeSystems += item.Item1 + ":" + item.Item2 + "  ";
                }
                SpriteBatch.DrawString(font, activeSystems, new Vector2(20, count * 20 + 80), Color.White);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //nothing
            }
            //sensor end
            SpriteBatch.End();
        }

        /// <summary>
        /// Got info about an open game.
        /// </summary>
        protected void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;

            var info = args.Data.ReadString();
            logger.Debug("Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers);
        }

        /// <summary>
        /// Got info that a new player joined the game.
        /// </summary>
        protected void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            logger.Debug("{0} joined.", args.Player);
        }

        /// <summary>
        /// Got information that a player has left the game.
        /// </summary>
        protected void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            logger.Debug("{0} left.", args.Player);
        }
    }
}

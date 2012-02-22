using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Session;
using Space.Simulation.Commands;
using Space.Util;

namespace Space.Control
{
    /// <summary>
    /// Utility class for creating new game server and client instances.
    /// </summary>
    public static class ControllerFactory
    {
        /// <summary>
        /// Creates a new game server.
        /// </summary>
        /// <param name="game">The game to create the game for.</param>
        /// <returns>A new server.</returns>
        public static ISimulationController<IServerSession> CreateServer(Game game)
        {
            // Create actual controller.
            var controller = new SimpleServerController<Profile>(7777, 12, SpaceCommandHandler.HandleCommand);

            // Add all systems we need in our game as a server.
            AddCommonSystems(controller.Simulation.Manager);
            AddRPGSystems(controller.Simulation.Manager);
            AddSpaceServerSystems(controller.Simulation.Manager, game);

            // Done.
            return controller;
        }

        /// <summary>
        /// Creates a new client that can be used to join remote games.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        /// <returns>A new client.</returns>
        public static IClientController<FrameCommand> CreateRemoteClient(Game game)
        {
            // Create actual controller.
            var controller = new SimpleClientController<Profile>(SpaceCommandHandler.HandleCommand);

            // Needed by some systems. Add all systems we need in our game as a client.
            AddCommonSystems(controller.Simulation.Manager);
            AddRPGSystems(controller.Simulation.Manager);
            AddSpaceServerSystems(controller.Simulation.Manager, game);
            AddSpaceClientSystems(controller.Simulation.Manager, game, controller.Session);

            // Done.
            return controller;
        }

        /// <summary>
        /// Creates a new client that will automatically connect to the given
        /// local server, and reuse the server's game state.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        /// <param name="server">The server to couple the client with.</param>
        /// <returns>A new client.</returns>
        public static IClientController<FrameCommand> CreateLocalClient(Game game, ISimulationController<IServerSession> server)
        {
            // Create actual controller.
            var controller = new ThinClientController<Profile>(server, Settings.Instance.PlayerName, (Profile)Settings.Instance.CurrentProfile);

            // Check if the server has all the services we need (enough to
            // check for one, because we only add all at once -- here).
            if (server.Simulation.Manager.GetSystem<CameraCenteredTextureRenderSystem>() == null)
            {
                // Needed by some systems. Add all systems we need in
                // *addition* to the ones the server already has.
                AddSpaceClientSystems(server.Simulation.Manager, game, controller.Session);
            }
            
            // Done.
            return controller;
        }

        private static void AddCommonSystems(IManager manager)
        {
            manager.AddSystems(
                new AbstractSystem[]
                {
                    new AccelerationSystem(),
                    new AvatarSystem(),
                    new CollisionSystem(1024),
                    new EllipsePathSystem(),
                    new ExpirationSystem(),
                    new FrictionSystem(),
                    new IndexSystem(),
                    new SpinSystem(),
                    new VelocitySystem()
                });
        }

        private static void AddRPGSystems(IManager manager)
        {
            manager.AddSystems(
                new AbstractSystem[]
                {
                    new CharacterSystem<AttributeType>(),
                    new InventorySystem(), 
                    new StatusEffectSystem()
                });
        }

        private static void AddSpaceServerSystems(IManager manager, Game game)
        {
            manager.AddSystems(
                new AbstractSystem[]
                {
                    new AISystem(),
                    new CellSystem(),
                    new CollisionDamageSystem(),
                    new DeathSystem(),
                    new DetectableSystem(game.Content),
                    new DropSystem(game.Content),
                    new GravitationSystem(),
                    new PlayerMassSystem(),
                    new RegeneratingValueSystem(),
                    new ShipControlSystem(),
                    new ShipInfoSystem(),
                    new ShipSpawnSystem(),
                    new SpaceUsablesSystem(),
                    new UniverseSystem(game.Content.Load<WorldConstraints>("Data/world")),
                    new WeaponControlSystem()
                });
        }

        private static void AddSpaceClientSystems(IManager manager, Game game, IClientSession session)
        {
            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            var graphicsDevice = ((Spaaace)game).GraphicsDeviceManager;

            manager.AddSystems(
                new AbstractSystem[]
                {
                    new CameraCenteredTextureRenderSystem(game, spriteBatch), 
                    new CameraSystem(game, session),
                    new ParticleEffectSystem(game, graphicsDevice),
                    new PlanetRenderSystem(game), 
                    new PlayerCenteredSoundSystem(soundBank, session), 
                    new SunRenderSystem(game, spriteBatch)
                });
        }
    }
}

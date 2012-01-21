using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Session;
using Space.Simulation.Commands;

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
            var controller = new SimpleServerController<PlayerData>(7777, 12, SpaceCommandHandler.HandleCommand);

            // Add all systems we need in our game as a server.
            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new IComponentSystem[]
                {
                    new DefaultLogicSystem(),
                    new IndexSystem(),
                    new CollisionSystem(1024),
                    new AvatarSystem(),
                    new CellSystem(),

                    new UniverseSystem(game.Content.Load<WorldConstraints>("Data/world")),
                    new ShipsSpawnSystem(game.Content)
                });

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
            var controller = new SimpleClientController<PlayerData>(SpaceCommandHandler.HandleCommand);

            // Needed by some systems.
            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

            // Add all systems we need in our game as a client.
            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new IComponentSystem[]
                {
                    new DefaultLogicSystem(),
                    new IndexSystem(),
                    new CollisionSystem(1024),
                    new AvatarSystem(),
                    new CellSystem(),

                    new UniverseSystem(game.Content.Load<WorldConstraints>("Data/world")),
                    new ShipsSpawnSystem(game.Content),

                    new PlayerCenteredSoundSystem(soundBank, controller.Session),
                    new PlayerCenteredRenderSystem(game, spriteBatch, ((Spaaace)game).GraphicsDeviceManager, controller.Session)
                });

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
            // The join data.
            string playerName = Settings.Instance.PlayerName;
            PlayerData playerData = new PlayerData();
            playerData.Ship = game.Content.Load<ShipData[]>("Data/ships")[0];

            // Create actual controller.
            var controller = new ThinClientController<PlayerData>(server, playerName, playerData);

            // Check if the server has all the services we need (enough to
            // check for one, because we only add all at once -- here).
            if (server.Simulation.EntityManager.SystemManager.GetSystem<PlayerCenteredRenderSystem>() == null)
            {
                // Needed by some systems.
                var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
                var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

                // Add all systems we need in *addition* to the ones the server
                // already has.
                server.Simulation.EntityManager.SystemManager.AddSystems(
                    new IComponentSystem[] {
                        new PlayerCenteredSoundSystem(soundBank, controller.Session),
                        new PlayerCenteredRenderSystem(game, spriteBatch, ((Spaaace)game).GraphicsDeviceManager, controller.Session)
                    });
            }
            
            // Done.
            return controller;
        }
    }
}

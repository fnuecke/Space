using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Constraints;
using Space.ComponentSystem.Systems;
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
            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new ISystem[]
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
            var controller = new SimpleClientController<Profile>(SpaceCommandHandler.HandleCommand);

            // Needed by some systems. Add all systems we need in our game as a client.
            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new ISystem[]
                {
                    new DefaultLogicSystem(),
                    new IndexSystem(),
                    new CollisionSystem(1024),
                    new AvatarSystem(),
                    new CellSystem(),

                    //new UniverseSystem(game.Content.Load<WorldConstraints>("Data/world")),
                    //new ShipsSpawnSystem(game.Content),

                    new PlayerCenteredRenderSystem(game,
                        (SpriteBatch)game.Services.GetService(typeof(SpriteBatch)),
                        ((Spaaace)game).GraphicsDeviceManager, controller.Session)
                });

            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            if (soundBank != null)
            {
                controller.Simulation.EntityManager.SystemManager.AddSystem(
                    new PlayerCenteredSoundSystem(soundBank, controller.Session));
            }

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
            if (server.Simulation.EntityManager.SystemManager.GetSystem<PlayerCenteredRenderSystem>() == null)
            {
                // Needed by some systems. Add all systems we need in
                // *addition* to the ones the server already has.
                server.Simulation.EntityManager.SystemManager.AddSystem(
                    new PlayerCenteredRenderSystem(game,
                        (SpriteBatch)game.Services.GetService(typeof(SpriteBatch)),
                        ((Spaaace)game).GraphicsDeviceManager, controller.Session));

                var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
                if (soundBank != null)
                {
                    server.Simulation.EntityManager.SystemManager.AddSystem(
                        new PlayerCenteredSoundSystem(soundBank, controller.Session));
                }
            }
            
            // Done.
            return controller;
        }
    }
}

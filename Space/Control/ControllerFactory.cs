using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;
using Space.Session;

namespace Space.Control
{
    public static class ControllerFactory
    {
        public static ISimulationController<IServerSession> CreateServer(Game game)
        {
            var controller = new SimpleServerController(new HybridServerSession<PlayerData>(7777, 12));

            var simulation = new DefaultSimulation();
            simulation.Command += GameCommandHandler.HandleCommand;
            ((TSS)controller.Simulation).Initialize(simulation);

            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new IComponentSystem[]
                {
                    new DefaultLogicSystem(),
                    new IndexSystem(),
                    new CollisionSystem(128),
                    new AvatarSystem(),
                    new CellSystem(),

                    new ShipControlSystem(),
                    new UniversalSystem(game.Content.Load<WorldConstraints>("Data/world"))
                });

            return controller;
        }

        public static IClientController<IFrameCommand> CreateRemoteClient(Game game)
        {
            var controller = new SimpleClientController<PlayerData>(GameCommandHandler.HandleCommand);

            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

            // Add all systems we need in our game.
            controller.Simulation.EntityManager.SystemManager.AddSystems(
                new IComponentSystem[]
                {
                    new DefaultLogicSystem(),
                    new IndexSystem(),
                    new CollisionSystem(128),
                    new AvatarSystem(),
                    new CellSystem(),

                    new ShipControlSystem(),
                    new UniversalSystem(game.Content.Load<WorldConstraints>("Data/world")),

                    new PlayerCenteredSoundSystem(soundBank, controller.Session),
                    new PlayerCenteredRenderSystem(spriteBatch, game.Content, controller.Session)
                                .AddComponent(new Background("Textures/stars"))
                });

            return controller;
        }

        public static IClientController<IFrameCommand> CreateLocalClient(Game game, ISimulationController<IServerSession> server)
        {
            PlayerData info = new PlayerData();
            info.Ship = game.Content.Load<ShipData[]>("Data/ships")[0];

            var controller = new ThinClientController<PlayerData>(server, Settings.Instance.PlayerName, info);

            if (server.Simulation.EntityManager.SystemManager.GetSystem<PlayerCenteredRenderSystem>() == null)
            {
                var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
                var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

                server.Simulation.EntityManager.SystemManager.AddSystems(
                    new[] {
                        new PlayerCenteredSoundSystem(soundBank, controller.Session),
                        new PlayerCenteredRenderSystem(spriteBatch, game.Content, controller.Session)
                                    .AddComponent(new Background("Textures/stars"))
                    });
            }

            return controller;
        }
    }
}

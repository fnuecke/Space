using System;
using System.Diagnostics;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Systems;
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
using Space.Util;

namespace Space.Control
{
    /// <summary>
    /// Utility class for creating new game server and client instances.
    /// </summary>
    internal static class ControllerFactory
    {
        /// <summary>
        /// Creates a new game server.
        /// </summary>
        /// <param name="game">The game to create the game for.</param>
        /// <param name="purelyLocal">Whether to create a purely local game (single player).</param>
        /// <returns>A new server.</returns>
        public static ISimulationController<IServerSession> CreateServer(Game game, bool purelyLocal = false)
        {
            // Create actual controller.
            var controller = new SimpleServerController<Profile>(7777, purelyLocal ? 1 : 8, SpaceCommandHandler.HandleCommand);

            // Add all systems we need in our game as a server.
            AddSpaceServerSystems(controller.Simulation.Manager);

            // Done.
            return controller;
        }

        /// <summary>
        /// Creates a new client that can be used to join remote games.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        /// <returns>A new client.</returns>
        public static IClientController<FrameCommand> CreateRemoteClient(Program game)
        {
            // Create actual controller.
            var controller = new SimpleClientController<Profile>(SpaceCommandHandler.HandleCommand);

            // Needed by some systems. Add all systems we need in our game as a client.
            AddSpaceServerSystems(controller.Simulation.Manager);
            AddSpaceClientSystems(controller.Simulation.Manager, game, controller.Session, controller);

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
        public static IClientController<FrameCommand> CreateLocalClient(Program game, ISimulationController<IServerSession> server)
        {
            // Create actual controller.
            var controller = new ThinClientController<Profile>(server, Settings.Instance.PlayerName, (Profile)Settings.Instance.CurrentProfile);

            // Check if the server has all the services we need (enough to
            // check for one, because we only add all at once -- here).
            if (server.Simulation.Manager.GetSystem(CameraSystem.TypeId) == null)
            {
                // Needed by some systems. Add all systems we need in
                // *addition* to the ones the server already has.
                AddSpaceClientSystems(server.Simulation.Manager, game, controller.Session, server);
            }

            // Done.
            return controller;
        }

        /// <summary>
        /// Adds systems used by the server and the client.
        /// </summary>
        /// <param name="manager">The manager.</param>
        private static void AddSpaceServerSystems(IManager manager)
        {
            manager.AddSystems(
                new AbstractSystem[]
                {
                    // IMPORTANT: Systems have to be updated in a specific order
                    // to function as intended. Mess this up, and you can cause
                    // terrible, terrible damage!

                    // ---- Informational and passive things ----- //

                    // The index system will update its indexes when the translation
                    // of an object changes, and remembers which movements triggered
                    // actual tree updates. It also marks entities as changed when
                    // they're added or change index groups. We update this first
                    // to reset the list of changed entities.
                    new IndexSystem(16, 64),

                    // Purely reactive/informational systems (only do stuff when
                    // receiving messages, if at all).
                    new AvatarSystem(),
                    new ShipInfoSystem(),

                    // These may be manipulated/triggered via player commands.
                    new CharacterSystem<AttributeType>(),
                    new PlayerMassSystem(),
                    new SpaceUsablesSystem(),
                    
                    // These are also purely reactive, but they handle chained removals
                    // so they're here for context (i.e. they remove items if the owner
                    // is removed, or the item from the owner if the item is removed).
                    new InventorySystem(),
                    new ItemSlotSystem(),
                    // The following systems will react to equipment changes, to adjust
                    // how a ship is rendered.
                    new ItemEffectSystem(),

                    // ----- Stuff that updates positions of things ----- //

                    // Friction has to be updated before acceleration is, to allow
                    // maximum speed to be reached at the end of one cycle.
                    new FrictionSystem(),

                    // Apply gravitation before ship control, to allow it to
                    // compensate for the gravitation.
                    new GravitationSystem(),
                    // Ship control must come first, but after stuff like gravitation,
                    // to be able to compute the stabilizer acceleration.
                    new ShipControlSystem(),

                    // Acceleration must come after ship control and gravitation, because
                    // those use the system as the accumulator.
                    new AccelerationSystem(),

                    // Velocity must come after acceleration, so that all other forces
                    // already have been applied (gravitation).
                    new VelocitySystem(),
                    new SpinSystem(),
                    new EllipsePathSystem(),
                    
                    // Update position after everything that might want to update it
                    // has run. This will apply those changes.
                    new TranslationSystem(),
                    
                    // ----- Stuff that creates new things ----- //

                    // Handle player respawns before the cell system update, as this
                    // might move the player somewhere out of the current live cells.
                    new RespawnSystem(),
                    // Check which cells are active after updating positions. This system
                    // may also remove entities if they are now out of bounds. But it'll
                    // also trigger new stuff via the spawn systems below.
                    new CellSystem(),
                    
                    // These two are purely reactive, and trigger on on cells becoming
                    // active. The first one is responsible for spawning "static" stuff
                    // in a cell. This includes stardust, but also moving elements such
                    // as planets. Update our universe before our spawn system, to give
                    // it a chance to generate cell information.
                    new UniverseSystem(),
                    // This one spawns ships in and stuff in a now populated new cell.
                    new ShipSpawnSystem(),
                    
                    // Run weapon control after velocity, to spawn projectiles at the
                    // correct position.
                    new WeaponControlSystem(),

                    // This system is purely reactive, and will trigger on entity death
                    // from whatever cause (debuffs, normally). Having it here will lead
                    // to drops mostly appear in the next update cycle, but no-one will
                    // know ;)
                    new DropSystem(),

                    // ----- Stuff that removes things ----- //

                    // Check for collisions after positions have been updated.
                    new CollisionSystem(),
                    // Collision damage is mainly reactive to collisions, but let's keep
                    // it here for context. Note that it also has it's own update, in
                    // which it updates damager cooldowns.
                    new CollisionAttributeEffectSystem(),
                    // Systems that apply effects, damage, handle on-hit effects etc.
                    new DirectDamageApplyingSystem(),
                    new OverTimeDamageApplyingSystem(),
                    new FreezeOnHitSystem(),
                    
                    // Apply any status effects at this point. Shield system first, to
                    // consume possibly regenerated energy -- so as not to block with
                    // the tiny regenerated value (which would apply the full shield
                    // armor rating...)
                    new ShieldSystem(),
                    // Apply damage after shield system update (after energy consumption).
                    new DamageSystem(),

                    // Remove expired status effects.
                    new StatusEffectSystem(),
                    // Handle deaths granting experience.
                    new ExperienceSystem(),
                    
                    // ----- Stuff that removes things ----- //

                    // Update this system after updating the cell system, to
                    // make sure we give cells a chance to 'activate' before
                    // checking if there are entities inside them.
                    new DeathSystem(),
                    // Get rid of expired components. This is similar to our death system
                    // as it's one of the few systems that actually remove stuff, so we
                    // keep it here, for context.
                    new ExpirationSystem(),
                    
                    // Energy should be update after it was used, to give it a chance
                    // to regenerate (e.g. if we're using less than we produce this
                    // avoids always displaying slightly less than max). Same for health.
                    new RegeneratingValueSystem(),
                    
                    // ----- Special stuff ----- //

                    // AI should react after everything else had its turn.
                    new AISystem()

                    // ----- For reference: rendering ----- //
                });
        }

        /// <summary>
        /// Adds systems only used by the client.
        /// </summary>
        /// <typeparam name="TSession">The type of the session.</typeparam>
        /// <param name="manager">The manager.</param>
        /// <param name="game">The game.</param>
        /// <param name="session">The session.</param>
        /// <param name="controller">The controller.</param>
        private static void AddSpaceClientSystems<TSession>(IManager manager, Program game, IClientSession session, ISimulationController<TSession> controller) where TSession : ISession
        {
            var audioEngine = (AudioEngine)game.Services.GetService(typeof(AudioEngine));
            var soundBank = (SoundBank)game.Services.GetService(typeof(SoundBank));
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            var simulationFps = new Func<float>(() => controller.ActualSpeed * Settings.TicksPerSecond);

            manager.AddSystems(
                new AbstractSystem[]
                {
                    // Load textures for detectables before trying to render radar.
                    new DetectableSystem(game.Content),

                    // Provides interpolation of objects in view space. This uses the camera
                    // for the viewport, but the camera uses it for its own position (based
                    // on the avatar position). It's not so bad if we use the viewport of the
                    // previous frame, but it's noticable if the avatar is no longer at the
                    // center, so we do it this way around.
                    new CameraCenteredInterpolationSystem(game.GraphicsDevice, simulationFps),

                    // Update camera first, as it determines what to render.
                    new CameraSystem(game.GraphicsDevice, game.Services, session),

                    // Handle sound.
                    new CameraCenteredSoundSystem(soundBank, audioEngine.GetGlobalVariable("MaxAudibleDistance"), session),
                    
                    // Biome system triggers background changes and stuff.
                    new BiomeSystem(session),

                    // Draw background behind everything else.
                    new CameraCenteredBackgroundSystem(game.Content, spriteBatch),

                    // Mind the order: orbits below planets below suns below normal
                    // objects below particle effects below radar.
                    new OrbitRenderSystem(game.Content, spriteBatch, session),
                    new PlanetRenderSystem(game.Content, game.GraphicsDevice),
                    new SunRenderSystem(game.Content, game.GraphicsDevice, spriteBatch),
                    new CameraCenteredTextureRenderSystem(game.Content, spriteBatch),
                    new CameraCenteredParticleEffectSystem(game.Content, game.GraphicsDeviceManager, simulationFps),
                    new ShieldRenderSystem(game.Content, game.GraphicsDevice),
                    new RadarRenderSystem(game.Content, spriteBatch, session)
                });

            // Add some systems for debug overlays.
            AddDebugSystems(manager, game);
        }

        /// <summary>
        /// Adds systems that are purely for debugging purposes (display additional
        /// information).
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="game">The game.</param>
        [Conditional("DEBUG")]
        private static void AddDebugSystems(IManager manager, Game game)
        {
            var spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

            manager.AddSystems(
                new AbstractSystem[]
                {
                    new DebugCollisionBoundsRenderSystem(game.Content, game.GraphicsDevice),
                    new DebugEntityIdRenderSystem(game.Content, spriteBatch)
                });
        }
    }
}

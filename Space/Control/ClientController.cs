using System;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class ClientController : AbstractTssClient
    {
        #region Constructor

        /// <summary>
        /// Creates a new game client, ready to connect to an open game.
        /// </summary>
        /// <param name="game"></param>
        public ClientController(Game game, IClientSession session)
            : base(game, session)
        {
            var simulation = new DefaultSimulation();
            simulation.Command += GameCommandHandler.HandleCommand;
            tss.Initialize(simulation);

            tss.EntityManager.SystemManager.AddSystem(new PhysicsSystem())
                .AddSystem(new ShipControlSystem())
                .AddSystem(new AvatarSystem())
                .AddSystem(new PlayerCenteredRenderSystem((SpriteBatch)game.Services.GetService(typeof(SpriteBatch)), game.Content, Session)
                            .AddComponent(new Background("Textures/stars")));
        }

        #endregion

        #region Events

        /// <summary>
        /// Got a server response to our request to join it.
        /// </summary>
        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            // OK, we were allowed to join, invalidate our simulation to request the current state.
            tss.Invalidate();
        }

        #endregion

        #region Debugging stuff

        internal void DEBUG_InvalidateSimulation()
        {
            tss.Invalidate();
        }

        #endregion
    }
}

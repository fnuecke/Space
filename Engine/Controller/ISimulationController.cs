﻿using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for controllers managing a game state.
    /// </summary>
    public interface ISimulationController<TSession> : IController<TSession>
        where TSession : ISession
    {
        /// <summary>
        /// The underlying simulation controlled by this controller.
        /// </summary>
        ISimulation Simulation { get; }
    }
}

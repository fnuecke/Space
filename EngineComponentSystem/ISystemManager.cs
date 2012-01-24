using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Interface to component system managers, which hold multiple systems
    /// which may communicate with each other via the manager.
    /// </summary>
    public interface ISystemManager : ICopyable<ISystemManager>, IPacketizable, IHashable
    {
        #region Properties
        
        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        ReadOnlyCollection<ISystem> Systems { get; }

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        IEntityManager EntityManager { get; set; }

        #endregion

        #region Logic

        /// <summary>
        /// Update all subsystems.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(long frame);

        /// <summary>
        /// Draw all subsystems.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Draw(GameTime gameTime, long frame);

        #endregion

        #region Systems
        
        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        ISystemManager AddSystem(ISystem system);

        /// <summary>
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        void AddSystems(IEnumerable<ISystem> systems);

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        void RemoveSystem(ISystem system);

        /// <summary>
        /// Get the first system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The first system of the given type, or <c>null</c> if no such system exits.</returns>
        T GetSystem<T>() where T : ISystem;

        #endregion

        #region Messaging
        
        /// <summary>
        /// Send a message to all systems of this system manager.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessage<T>(ref T message) where T : struct;

        #endregion
    }
}

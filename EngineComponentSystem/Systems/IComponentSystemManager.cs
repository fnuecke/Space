using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface to component system managers, which hold multiple systems
    /// which may communicate with each other via the manager.
    /// </summary>
    public interface IComponentSystemManager : ICopyable<IComponentSystemManager>, IPacketizable, IHashable
    {
        #region Properties
        
        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        ReadOnlyCollection<IComponentSystem> Systems { get; }

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

        #region Components
        
        /// <summary>
        /// Add the component to supported subsystems.
        /// </summary>
        /// <param name="component">The component to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        IComponentSystemManager AddComponent(AbstractComponent component);

        /// <summary>
        /// Removes the component from supported subsystems.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        void RemoveComponent(AbstractComponent component);

        /// <summary>
        /// Remove all components from all subsystems.
        /// </summary>
        void ClearComponents();

        #endregion

        #region Systems
        
        /// <summary>
        /// Add the system to this manager.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This component system manager, for chaining.</returns>
        IComponentSystemManager AddSystem(IComponentSystem system);

        /// <summary>
        /// Add multiple systems to this manager.
        /// </summary>
        /// <param name="systems">The systems to add.</param>
        void AddSystems(IEnumerable<IComponentSystem> systems);

        /// <summary>
        /// Removes the system from this manager.
        /// </summary>
        /// <param name="system">The system to remove.</param>
        void RemoveSystem(IComponentSystem system);

        /// <summary>
        /// Get the first system of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The first system of the given type, or <c>null</c> if no such system exits.</returns>
        T GetSystem<T>() where T : IComponentSystem;

        #endregion

        #region Messaging
        
        /// <summary>
        /// Send a message to all systems of this component system manager.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessageToSystems<T>(ref T message) where T : struct;

        /// <summary>
        /// Send a message to all components in all component system managers.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessageToComponents<T>(ref T message) where T : struct;

        #endregion
    }
}

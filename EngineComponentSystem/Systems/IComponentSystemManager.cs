using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface to component system managers, which hold multiple systems
    /// which may communicate with each other via the manager.
    /// </summary>
    public interface IComponentSystemManager : ICloneable, IPacketizable, IHashable
    {
        /// <summary>
        /// A list of registered subsystems.
        /// </summary>
        ReadOnlyCollection<IComponentSystem> Systems { get; }

        /// <summary>
        /// The component system manager used together with this entity manager.
        /// </summary>
        IEntityManager EntityManager { get; set; }

        /// <summary>
        /// Update all subsystems.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(long frame);

        /// <summary>
        /// Draw all subsystems.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Draw(long frame);

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
        
        /// <summary>
        /// Send a message to all systems of this component system manager.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendMessage(ValueType message);
    }
}

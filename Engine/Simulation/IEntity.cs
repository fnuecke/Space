using System;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal functionality of a world entity that can be used in simulations
    /// and updated via the component system. The entity must know its components
    /// and delegate the <c>Update</c> call to its components.
    /// </summary>
    public interface IEntity : IPacketizable, ICloneable, IHashable
    {
        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        long UID { get; }

        /// <summary>
        /// A list of all of this entities components.
        /// </summary>
        ReadOnlyCollection<IComponent> Components { get; }
    }
}

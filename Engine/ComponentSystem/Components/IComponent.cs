using System;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    public interface IComponent : ICloneable, IPacketizable, IHashable
    {
        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        IEntity Entity { get; set; }

        /// <summary>
        /// Update the component with the specified parameterization.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        void Update(object parameterization);

        /// <summary>
        /// Test whether the component supports the specified parameterization type.
        /// </summary>
        /// <param name="parameterizationType">The parameterization type to check.</param>
        /// <returns>Whether the type is supported.</returns>
        bool SupportsParameterization(Type parameterizationType);

        /// <summary>
        /// Inform a component of a message that was sent by a component of
        /// the entity the component belongs to.
        /// 
        /// <para>
        /// Note that components will also receive the messages they send themselves.
        /// </para>
        /// </summary>
        /// <param name="message">The sent message.</param>
        void HandleMessage(object message);
    }
}

using System;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    public interface IComponent : ICloneable, IPacketizable, IHashable
    {
        /// <summary>
        /// Unique ID in the context of its entity. This means there can be
        /// multiple components with the same id, but no two components with
        /// the same id attached to the same entity.
        /// </summary>
        int UID { get; set; }

        /// <summary>
        /// This determines in which order this component will be updated.
        /// Components with higher values will be updated later.
        /// </summary>
        int UpdateOrder { get; set; }

        /// <summary>
        /// This determines in which order this component will be drawn.
        /// Components with higher values will be drawn later.
        /// </summary>
        int DrawOrder { get; set; }

        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        IEntity Entity { get; set; }

        /// <summary>
        /// Whether the component is enabled or not. Disabled components will
        /// not have their <c>Update()</c> method called.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Update the component with the specified parameterization.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        void Update(object parameterization);

        /// <summary>
        /// Draw the component with the specified parameterization.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        void Draw(object parameterization);

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
        void HandleMessage(ValueType message);
    }
}

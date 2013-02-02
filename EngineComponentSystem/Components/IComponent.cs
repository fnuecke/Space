﻿using System;

namespace Engine.ComponentSystem.Components
{
    /// <summary>Used to allow other interfaces for components.</summary>
    public interface IComponent : IComparable<IComponent>
    {
        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        int GetTypeId();

        /// <summary>The manager the component lives in.</summary>
        IManager Manager { get; }

        /// <summary>
        ///     Unique ID in the context of its entity. This means there can be multiple components with the same id, but no
        ///     two components with the same id attached to the same entity.
        /// </summary>
        int Id { get; }

        /// <summary>Gets the entity this component belongs to.</summary>
        int Entity { get; }

        /// <summary>
        ///     Whether the component is enabled or not. Disabled components will not be handled in the component's system's
        ///     <c>Update()</c> method.
        /// </summary>
        bool Enabled { get; set; }
    }
}
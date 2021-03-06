﻿using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>Determines how much experience an entity is worth if destroyed.</summary>
    public sealed class ExperiencePoints : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>How many experience this entity is worth when killed.</summary>
        public int Value;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified experience point value.</summary>
        /// <param name="value">The value.</param>
        public ExperiencePoints Initialize(int value)
        {
            Value = value;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0;
        }

        #endregion
    }
}
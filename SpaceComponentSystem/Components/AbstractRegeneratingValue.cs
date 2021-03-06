﻿using System;
using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>Base class for modules that represent regenerating values.</summary>
    public abstract class AbstractRegeneratingValue : Component
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

        #region Properties

        /// <summary>The current value.</summary>
        public float Value { get; set; }

        #endregion

        #region Fields

        /// <summary>The maximum value.</summary>
        public float MaxValue;

        /// <summary>The amount the value is regenerated per tick.</summary>
        public float Regeneration;

        /// <summary>The timeout in ticks to wait after the last reducing change, before applying regeneration again.</summary>
        public int Timeout;

        /// <summary>Time to wait before triggering regeneration again, in ticks.</summary>
        internal int TimeToWait;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified timeout.</summary>
        /// <param name="timeout">The timeout.</param>
        public AbstractRegeneratingValue Initialize(int timeout)
        {
            Timeout = timeout;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            MaxValue = 0;
            Regeneration = 0;
            Timeout = 0;
            TimeToWait = 0;
            Value = 0;
        }

        #endregion

        #region Accessors

        /// <summary>Sets the value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="causingEntity">The entity that caused the value change.</param>
        public virtual void SetValue(float value, int causingEntity = 0)
        {
            if (value < Value)
            {
                TimeToWait = Timeout;
            }
            Value = Math.Max(0, Math.Min(MaxValue, value));
        }

        #endregion

        #region Logic

        /// <summary>Recomputes the maximum value and regeneration speed.</summary>
        internal abstract void RecomputeValues();

        #endregion
    }
}
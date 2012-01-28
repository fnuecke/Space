using System;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents the energy available on a entity.
    /// </summary>
    public sealed class Energy : AbstractRegeneratingValue
    {
        #region Constructor

        /// <summary>
        /// Creates a new energy component.
        /// </summary>
        /// <param name="timeout">The number of ticks to wait after using
        /// energy before starting to regenerate energy again.</param>
        public Energy(int timeout)
            : base(timeout)
        {
        }

        public Energy()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Test for change in equipment.
        /// </summary>
        /// <param name="message">Handles module added / removed messages.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is CharacterStatsInvalidated)
            {
                RecomputeValues();
            }
            else if (message is ItemAdded)
            {
                var added = (ItemAdded)(ValueType)message;
                if (added.Item.GetComponent<Reactor>() != null)
                {
                    RecomputeValues();
                }
            }
            else if (message is ItemRemoved)
            {
                var removed = (ItemRemoved)(ValueType)message;
                if (removed.Item.GetComponent<Reactor>() != null)
                {
                    RecomputeValues();
                }
            }
        }

        private void RecomputeValues()
        {
            // Recompute our values.
            var character = Entity.GetComponent<Character<AttributeType>>();
            var equipment = Entity.GetComponent<Equipment>();

            // Rebuild base energy and regeneration values.
            MaxValue = 0;
            Regeneration = 0;
            for (int i = 0; i < equipment.GetSlotCount<Reactor>(); i++)
            {
                var reactor = equipment.GetItem<Reactor>(i);
                MaxValue += reactor.GetComponent<Reactor>().Energy;
                Regeneration += reactor.GetComponent<Reactor>().EnergyRegeneration;
            }

            // Apply bonuses.
            MaxValue = character.GetValue(AttributeType.Energy, MaxValue);
            Regeneration = character.GetValue(AttributeType.EnergyRegeneration, Regeneration);

            // Adjust current energy so it does not exceed our new maximum.
            if (Value > MaxValue)
            {
                Value = MaxValue;
            }
        }

        #endregion
    }
}

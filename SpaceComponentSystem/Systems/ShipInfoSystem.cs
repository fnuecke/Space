using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Keeps the ship information facade up-to-date.
    /// </summary>
    public sealed class ShipInfoSystem : AbstractSystem
    {
        #region Logic

        /// <summary>
        /// Handles a message. Updates speed and acceleration when modules
        /// change.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void Receive<T>(ref T message)
        {
            if (message is CharacterStatsInvalidated)
            {
                var entity = ((CharacterStatsInvalidated)(ValueType)message).Entity;
                var shipInfo = Manager.GetComponent<ShipInfo>(entity);

                // Get ship modules.
                var character = Manager.GetComponent<Character<AttributeType>>(entity);

                // Get the mass of the ship and return it.
                shipInfo.Mass = character.GetValue(AttributeType.Mass);

                // Recompute cached values.
                shipInfo.MaxAcceleration = character.GetValue(AttributeType.AccelerationForce) / shipInfo.Mass;
                shipInfo.MaxSpeed = float.PositiveInfinity;

                // Maximum speed.
                var friction = Manager.GetComponent<Friction>(entity);
                if (friction != null)
                {
                    shipInfo.MaxSpeed = shipInfo.MaxAcceleration / friction.Value;
                }

                // Figure out the overall range of our radar system.
                shipInfo.RadarRange = character.GetValue(AttributeType.SensorRange);

                // TODO: compute actual range
                shipInfo.WeaponRange = 1000;
            }
        }

        #endregion
    }
}

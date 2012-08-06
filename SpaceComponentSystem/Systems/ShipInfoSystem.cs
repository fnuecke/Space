using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Keeps the ship information facade up-to-date.
    /// </summary>
    public sealed class ShipInfoSystem : AbstractSystem, IMessagingSystem
    {
        #region Logic

        /// <summary>
        /// Handles a message. Updates speed and acceleration when modules
        /// change.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public void Receive<T>(ref T message) where T : struct
        {
            if (message is CharacterStatsInvalidated)
            {
                var entity = ((CharacterStatsInvalidated)(ValueType)message).Entity;
                var shipInfo = ((ShipInfo)Manager.GetComponent(entity, ShipInfo.TypeId));

                // Get ship modules.
                var character = ((Character<AttributeType>)Manager.GetComponent(entity, Character<AttributeType>.TypeId));

                // Get the mass of the ship and return it.
                shipInfo.Mass = character.GetValue(AttributeType.Mass);

                // Recompute cached values.
                shipInfo.MaxAcceleration = character.GetValue(AttributeType.AccelerationForce) / (shipInfo.Mass * Settings.TicksPerSecond);
                shipInfo.MaxSpeed = float.PositiveInfinity;

                // Maximum speed.
                var friction = ((Friction)Manager.GetComponent(entity, Friction.TypeId));
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

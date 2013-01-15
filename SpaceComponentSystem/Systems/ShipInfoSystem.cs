using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Keeps the ship information facade up-to-date.</summary>
    public sealed class ShipInfoSystem : AbstractSystem, IMessagingSystem
    {
        #region Logic

        /// <summary>Handles a message. Updates speed and acceleration when modules change.</summary>
        /// <param name="message">The message to handle.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as CharacterStatsInvalidated?;
            if (cm == null)
            {
                return;
            }

            var entity = cm.Value.Entity;
            var shipInfo = ((ShipInfo) Manager.GetComponent(entity, ShipInfo.TypeId));
            if (shipInfo == null)
            {
                // Skip if there's no ship info here (other entities with attributes,
                // such as damagers -- e.g. suns).
                return;
            }

            // Get ship modules.
            var attributes =
                (Attributes<AttributeType>) Manager.GetComponent(entity, Attributes<AttributeType>.TypeId);

            // Get the mass of the ship and return it.
            shipInfo.Mass = attributes.GetValue(AttributeType.Mass);

            // Recompute cached values.
            shipInfo.MaxAcceleration = attributes.GetValue(AttributeType.AccelerationForce) /
                                       (shipInfo.Mass * Settings.TicksPerSecond);
            shipInfo.MaxSpeed = float.PositiveInfinity;

            // Maximum speed.
            var friction = ((Friction) Manager.GetComponent(entity, Friction.TypeId));
            if (friction != null)
            {
                shipInfo.MaxSpeed = shipInfo.MaxAcceleration / friction.Value;
            }

            // Figure out the overall range of our radar system.
            shipInfo.RadarRange = attributes.GetValue(AttributeType.SensorRange);

            // TODO: compute actual range
            shipInfo.WeaponRange = 1000;
        }

        #endregion
    }
}
using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.Data.Constraints;

namespace Space.ComponentSystem.Entities
{
    public static class ItemFactory
    {
        /// <summary>
        /// Creates a new sensor based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the sensor on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new sensor based on the specified blueprint.</returns>
        public static Entity CreateSensor(SensorConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as a sensor.
            result.AddComponent(new Sensor(blueprint.SampleRange(random)));
            
            return result;
        }

        /// <summary>
        /// Creates a new armor based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the armor on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new armor based on the specified blueprint.</returns>
        public static Entity CreateArmor(ArmorConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as armor.
            result.AddComponent(
                new Armor(blueprint.SampleArmor(random)));

            return result;
        }

        /// <summary>
        /// Creates a new reactor based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the reactor on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new reactor based on the specified blueprint.</returns>
        public static Entity CreateReactor(ReactorConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as a reactor.
            result.AddComponent(
                new Reactor(blueprint.SampleEnergy(random),
                            blueprint.SampleEnergyRegeneration(random) / 60f));

            return result;
        }

        /// <summary>
        /// Creates a new shield based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the shield on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new shield based on the specified blueprint.</returns>
        public static Entity CreateShield(ShieldConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as a shield.
            result.AddComponent(new Shield());

            return result;
        }

        /// <summary>
        /// Creates a new thruster based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the thruster on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new thruster based on the specified blueprint.</returns>
        public static Entity CreateThruster(ThrusterConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as a thruster.
            result.AddComponent(
                new Thruster(blueprint.SampleAccelerationForce(random),
                             blueprint.SampleRotationForce(random),
                             blueprint.SampleEnergyConsumption(random) / 60f));

            return result;
        }

        /// <summary>
        /// Creates a new weapon based on the specified blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to base the weapon on.</param>
        /// <param name="random">The randomizer to use to sample values.</param>
        /// <returns>A new weapon based on the specified blueprint.</returns>
        public static Entity CreateWeapon(WeaponConstraints blueprint, IUniformRandom random)
        {
            var result = new Entity();

            // Mark it as a weapon.
            result.AddComponent(
                new Weapon(blueprint.Texture,
                    blueprint.Sound,
                    (int)(blueprint.SampleCooldown(random) * 60),
                    blueprint.SampleEnergyConsumption(random),
                    blueprint.SampleDamage(random),
                    blueprint.Projectiles));

            return result;
        }
    }
}

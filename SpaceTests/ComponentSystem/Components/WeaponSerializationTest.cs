using System.Collections.Generic;
using System.Linq;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace SpaceTests.ComponentSystem.Components
{
    public class WeaponSerializationTest : AbstractSpaceItemSerializationTest<Weapon>
    {
        /// <summary>
        /// Generates a list of instances to test. The validity of the
        /// serialization is tested using the objects hash. This should at
        /// least return one instance per initializer.
        /// </summary>
        /// <returns>A list of instances to test with.</returns>
        protected override IEnumerable<Weapon> NewInstances()
        {
            return new[]
                   {
                       new Weapon(),
                       (Weapon)new Weapon().Initialize("name1", "icon1"),
                       (Weapon)new Weapon().Initialize("name2", "icon2", ItemQuality.Common),
                       new Weapon().Initialize("name3", "icon3", ItemQuality.Rare, "model", "sound", 1.5f, 2.5f, 3.5f, new[]
                                                          {
                                                              new ProjectileFactory
                                                              {
                                                                  AccelerationForce = new Interval<float>(1, 2),
                                                                  CanBeShot = true,
                                                                  CollisionRadius = 4,
                                                                  Effect = "qwe",
                                                                  Friction = 3,
                                                                  InitialDirection = new Interval<float>(2, 7),
                                                                  InitialRotation = new Interval<float>(3, 6),
                                                                  InitialVelocity = new Interval<float>(4, 5),
                                                                  Model = "sdf",
                                                                  TimeToLive = 5
                                                              }
                                                          })
                   };
        }

        /// <summary>
        /// Returns a list of methods that change a value of an instance so
        /// that its new hash value should be different.
        /// </summary>
        protected override IEnumerable<ValueChanger> GetValueChangers()
        {
            return new ValueChanger[]
                   {
                       instance => instance.Cooldown += 10,
                       instance => instance.EnergyConsumption += 10,
                       instance => instance.Damage += 10,
                       instance => instance.ModelName += "b",
                       instance => instance.Sound += "b",
                       instance => instance.Projectiles = new[]
                                                          {
                                                              new ProjectileFactory
                                                              {
                                                                  AccelerationForce = new Interval<float>(0, 1),
                                                                  CanBeShot = false,
                                                                  CollisionRadius = 5,
                                                                  Effect = "asd",
                                                                  Friction = 1,
                                                                  InitialDirection = new Interval<float>(0, 1),
                                                                  InitialRotation = new Interval<float>(0, 1),
                                                                  InitialVelocity = new Interval<float>(0, 1),
                                                                  Model = "zxc",
                                                                  TimeToLive = 10
                                                              }
                                                          }
                   }.Concat(base.GetValueChangers());
        }
    }
}

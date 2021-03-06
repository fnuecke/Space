﻿using System.Collections.Generic;
using System.Linq;
using Engine.Math;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace SpaceTests.ComponentSystem.Components
{
    public sealed class WeaponSerializationTest : AbstractSpaceItemSerializationTest<Weapon>
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
                (Weapon)
                new Weapon().Initialize("name2", "icon2", ItemQuality.Common, ItemSlotSize.Large, Vector2.Zero, false),
                (Weapon)new Weapon().Initialize("sound", null, new[]
                                                {
                                                    new ProjectileFactory
                                                    {
                                                        AccelerationForce = new FloatInterval(1, 2),
                                                        CanBeShot = true,
                                                        CollisionRadius = 4,
                                                        Effect = "qwe",
                                                        Friction = 3,
                                                        InitialDirection = new FloatInterval(2, 7),
                                                        InitialVelocity = new FloatInterval(4, 5),
                                                        Model = "sdf",
                                                        TimeToLive = 5
                                                    }
                                                }).Initialize("name3", "icon3", ItemQuality.Rare, ItemSlotSize.Small,
                                                              Vector2.Zero, false)
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
                instance => instance.Sound += "b",
                instance => instance.Projectiles = new[]
                {
                    new ProjectileFactory
                    {
                        AccelerationForce = new FloatInterval(0, 1),
                        CanBeShot = false,
                        CollisionRadius = 5,
                        Effect = "asd",
                        Friction = 1,
                        InitialDirection = new FloatInterval(0, 1),
                        InitialVelocity = new FloatInterval(0, 1),
                        Model = "zxc",
                        TimeToLive = 10
                    }
                }
            }.Concat(base.GetValueChangers());
        }
    }
}

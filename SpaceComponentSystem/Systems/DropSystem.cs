﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Manages item drops by reacting to death events.</summary>
    public sealed class DropSystem : AbstractSystem
    {
        #region Constants
        
        /// <summary>Store interface type id for performance.</summary>
        private static readonly int DrawableTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<IDrawable>();

        #endregion

        #region Fields

        /// <summary>
        ///     List of drops to sample when we update. This is accumulated from death events, to allow thread safe sampling
        ///     in one go.
        /// </summary>
        [CopyIgnore, PacketizeIgnore]
        private List<Tuple<string, FarPosition>> _dropsToSample = new List<Tuple<string, FarPosition>>();

        #endregion

        #region Logic

        /// <summary>Removes entities that died this frame from the manager.</summary>
        [MessageCallback]
        public void OnUpdate(Update message)
        {
            // Remove dead entities (getting out of bounds).
            foreach (var drop in _dropsToSample)
            {
                SampleDrop(drop.Item1, drop.Item2);
            }
            _dropsToSample.Clear();
        }

        /// <summary>
        ///     Queues a drops for one or more items from the specified item pool at the specified position. The actual drop
        ///     will be performed when the drop system updates.
        /// </summary>
        /// <param name="poolName">The name of the item pool to sample from.</param>
        /// <param name="position">The position at which to drop the items.</param>
        [PublicAPI]
        public void Drop(string poolName, ref FarPosition position)
        {
            lock (_dropsToSample)
            {
                _dropsToSample.Add(Tuple.Create(poolName, position));
            }
        }

        /// <summary>Performs the actual drop sampling.</summary>
        /// <param name="poolName">The name of the item pool to sample from.</param>
        /// <param name="position">The position at which to drop the items.</param>
        private void SampleDrop(string poolName, FarPosition position)
        {
            // Get the actual item pool to pull items from.
            var pool = FactoryLibrary.GetItemPool(poolName);

            // Get the list of possible drops.
            var dropInfo = new List<ItemPool.DropInfo>(pool.Items);

            // Randomizer used for sampling of items. Seed it based on the item
            // pool and the drop position, to get a deterministic result for
            // each drop, regardless in which order they happen.
            var hasher = new Hasher();
            hasher.Write(poolName);
            hasher.Write(position);
            var random = new MersenneTwister(hasher.Value);

            // And shuffle it. This is important, to give each entry an equal
            // chance to be picked. Otherwise the first few entries have a much
            // better chance, because we stop after reaching a certain number
            // of items.
            for (var i = dropInfo.Count; i > 1; i--)
            {
                // Pick random element to swap.
                var j = random.NextInt32(i); // 0 <= j <= i - 1
                // Swap.
                var tmp = dropInfo[j];
                dropInfo[j] = dropInfo[i - 1];
                dropInfo[i - 1] = tmp;
            }

            var dropCount = 0;
            for (int i = 0, j = dropInfo.Count; i < j; i++)
            {
                var item = dropInfo[i];

                // Give the item a chance to be dropped.
                if (item.Probability > random.NextDouble())
                {
                    // Random roll succeeded, drop the item.
                    var entity = FactoryLibrary.SampleItem(Manager, item.ItemName, position, random);

                    // Make the item visible (per default items are hidden).
                    foreach (IDrawable drawable in Manager.GetComponents(entity, DrawableTypeId))
                    {
                        drawable.Enabled = true;
                    }

                    // Did a drop, check if we're done.
                    dropCount++;
                    if (dropCount == pool.MaxDrops)
                    {
                        break;
                    }
                }
            }
        }
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        [MessageCallback]
        public void OnEntityDied(EntityDied message)
        {
            var entity = message.KilledEntity;

            var drops = ((Drops) Manager.GetComponent(entity, Drops.TypeId));
            if (drops != null)
            {
                var translation = ((ITransform) Manager.GetComponent(entity, TransformTypeId)).Position;
                Drop(drops.ItemPool, ref translation);
            }
        }

        #endregion

        #region Copying

        /// <summary>
        ///     Servers as a copy constructor that returns a new instance of the same type that is freshly initialized.
        ///     <para>This takes care of duplicating reference types to a new copy of that type (e.g. collections).</para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (DropSystem) base.NewInstance();

            copy._dropsToSample = new List<Tuple<string, FarPosition>>();

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            System.Diagnostics.Debug.Assert(
                _dropsToSample.Count == 0, "Drop system got drop requests after its update, but before copying.");

            base.CopyInto(into);
        }

        #endregion
    }
}
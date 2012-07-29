﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages Item Drops
    /// </summary>
    public sealed class DropSystem : AbstractComponentSystem<Drops>
    {
        #region Fields

        /// <summary>
        /// A List of all item pools mapped by their ID.
        /// </summary>
        private Dictionary<string, ItemPool> _itemPools = new Dictionary<string, ItemPool>();

        #endregion

        #region Single allocation

        /// <summary>
        /// Reusable list for sampling items to drop.
        /// </summary>
        private List<ItemPool.DropInfo> _reusableDropInfo = new List<ItemPool.DropInfo>();

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new Drop System with the given ContentManager
        /// </summary>
        /// <param name="content"></param>
        public DropSystem(ContentManager content)
        {
            foreach (var itemPool in content.Load<ItemPool[]>("Data/Items"))
            {
                _itemPools.Add(itemPool.Name, itemPool);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drops one or more Items from the given Item Pool  on the given Position
        /// </summary>
        /// <param name="poolName">The name of the item pool to sample from.</param>
        /// <param name="position">The position at which to drop the items.</param>
        public void Drop(string poolName, ref Vector2 position)
        {
            // Get the actual item pool to pull items from.
            var pool = _itemPools[poolName];

            // Get the list of possible drops.
            _reusableDropInfo.AddRange(pool.Items);
            
            // Randomizer used for sampling of items. Seed it based on the item
            // pool and the drop position, to get a deterministic result for
            // each drop, regardless in which order they happen.
            var hasher = new Hasher();
            hasher.Put(poolName);
            hasher.Put(position);
            var random = new MersenneTwister((ulong)hasher.Value);

            // And shuffle it. This is important, to give each entry an equal
            // chance to be picked. Otherwise the first few entries have a much
            // better chance, because we stop after reaching a certain number
            // of items.
            for (var i = _reusableDropInfo.Count; i > 1; i--)
            {
                // Pick random element to swap.
                var j = random.NextInt32(i); // 0 <= j <= i - 1
                // Swap.
                var tmp = _reusableDropInfo[j];
                _reusableDropInfo[j] = _reusableDropInfo[i - 1];
                _reusableDropInfo[i - 1] = tmp;
            }

            var dropCount = 0;
            for (int i = 0, j = _reusableDropInfo.Count; i < j; i++)
            {
                var item = _reusableDropInfo[i];

                // Give the item a chance to be dropped.
                if (item.Probability > random.NextDouble())
                {
                    // Random roll succeeded, drop the item.
                    FactoryLibrary.SampleItem(Manager, item.ItemName, position, random);

                    // Did a drop, check if we're done.
                    dropCount++;
                    if (dropCount == pool.MaxDrops)
                    {
                        break;
                    }
                }
            }

            // Clear up for next run.
            _reusableDropInfo.Clear();
        }

        /// <summary>
        /// Drop items when entities die.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is EntityDied)
            {
                var entity = ((EntityDied)(ValueType)message).Entity;

                var drops = ((Drops)Manager.GetComponent(entity, Drops.TypeId));
                if (drops != null)
                {
                    var translation = ((Transform)Manager.GetComponent(entity, Transform.TypeId)).Translation;
                    Drop(drops.ItemPool, ref translation);
                }
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (DropSystem)base.NewInstance();

            copy._itemPools = new Dictionary<string, ItemPool>();
            copy._reusableDropInfo = new List<ItemPool.DropInfo>();

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);
            
            var copy = (DropSystem)into;

            // Copy for shuffling.
            copy._itemPools.Clear();
            foreach (var item in _itemPools)
            {
                copy._itemPools.Add(item.Key, item.Value);
            }
        }

        #endregion
    }
}

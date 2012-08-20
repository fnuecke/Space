using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages item drops by reacting to death events.
    /// </summary>
    public sealed class DropSystem : AbstractComponentSystem<Drops>, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// A list of all item pools mapped by their ID. This should be treated
        /// as read-only after construction.
        /// </summary>
        private Dictionary<string, ItemPool> _itemPools = new Dictionary<string, ItemPool>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DropSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager used to load drop tables.</param>
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
        public void Drop(string poolName, ref FarPosition position)
        {
            // Get the actual item pool to pull items from.
            var pool = _itemPools[poolName];

            // Get the list of possible drops.
            var dropInfo = new List<ItemPool.DropInfo>(pool.Items);
            
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

                    // Make the item visible.
                    var renderer = ((TextureRenderer)Manager.GetComponent(entity, TextureRenderer.TypeId));
                    if (renderer != null)
                    {
                        renderer.Enabled = true;
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

        /// <summary>
        /// Drop items when entities die.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
        {
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

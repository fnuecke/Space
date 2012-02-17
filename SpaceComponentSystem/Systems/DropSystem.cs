using System.Collections.Generic;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages Item Drops
    /// </summary>
    public class DropSystem : AbstractSystem
    {
        #region Fields

        /// <summary>
        /// A List of all Item Pools Mapped to the ID
        /// </summary>
        private Dictionary<string, ItemPool> _itemPools = new Dictionary<string, ItemPool>();

        /// <summary>
        /// Randomizer used for sampling of items.
        /// </summary>
        private MersenneTwister _random;

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
            _random = new MersenneTwister();
            foreach (var itemPool in content.Load<ItemPool[]>("Data/Items"))
            {
                _itemPools.Add(itemPool.Name, itemPool);
            }

            // We need to synchronize our randomizer.
            ShouldSynchronize = true;
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

            // And shuffle it. This is important, to give each entry an equal
            // chance to be picked. Otherwise the first few entries have a much
            // better chance, because we stop after reaching a certain number
            // of items.
            for (int i = _reusableDropInfo.Count; i > 1; i--)
            {
                // Pick random element to swap.
                int j = _random.NextInt32(i); // 0 <= j <= i-1
                // Swap.
                var tmp = _reusableDropInfo[j];
                _reusableDropInfo[j] = _reusableDropInfo[i - 1];
                _reusableDropInfo[i - 1] = tmp;
            }

            int dropCount = 0;
            foreach (var item in _reusableDropInfo)
            {
                // Give the item a chance to be dropped.
                if (item.Probability > _random.NextDouble())
                {
                    // Random roll succeeded, drop the item.
                    Manager.EntityManager.AddEntity(FactoryLibrary.SampleItem(item.ItemName, position, _random));

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
        #endregion

        #region Serilization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(_random);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _random = packet.ReadPacketizableInto<MersenneTwister>(_random);
        }

        #endregion

        #region Copying

        public override ISystem DeepCopy(ISystem into)
        {
            var copy = (DropSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._itemPools.Clear();
                foreach (var item in _itemPools)
                {
                    copy._itemPools[item.Key] = item.Value;
                }
                copy._random = _random.DeepCopy(copy._random);
            }
            else
            {
                copy._itemPools = new Dictionary<string, ItemPool>(_itemPools);
                copy._random = _random.DeepCopy();
                copy._reusableDropInfo = new List<ItemPool.DropInfo>();
            }

            return copy;
        }

        #endregion
    }
}

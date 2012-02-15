using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories;
using Microsoft.Xna.Framework;
using Engine.Util;

namespace Space.ComponentSystem.Systems
{
    //Manages Item Drops
    public class DropSystem: AbstractSystem
    {
        #region Fields
        
        /// <summary>
        /// A List of all Item Pools Mapped to the ID
        /// </summary>
        private readonly Dictionary<string, ItemPool> _itemPools = new Dictionary<string, ItemPool>();

        private MersenneTwister _random;
        #endregion

        #region Constructor        
        /// <summary>
        /// Create a new Drop System with the given ContentManager
        /// </summary>
        /// <param name="content"></param>
        public DropSystem(ContentManager content)
        {
            _random = new MersenneTwister();
            foreach (var itemPool in content.Load<ItemPool[]>("data/Items"))
            {
                _itemPools.Add(itemPool.Name, itemPool);
            }
        }
        #endregion

        #region Logic
        /// <summary>
        /// Drops one or more Items from the given Item Pool  on the given Position
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="position"></param>
        public void Drop(string poolName, ref Vector2 position)
        {
            var itempool = _itemPools[poolName];
            var dropCount = 0;
            foreach (var item in itempool.Items)
            {
                if (item.Dropchance > _random.NextDouble())
                {
                    Manager.EntityManager.AddEntity(FactoryLibrary.SampleItem(item.ItemName, position, _random));
                    dropCount++;
                    if (dropCount == itempool.MaxDrops)
                        break;
                }
            }

        }
        #endregion


        #region Serilization

        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(_random);
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);
            _random = packet.ReadPacketizableInto<MersenneTwister>(_random);
        }
        #endregion
    }
}

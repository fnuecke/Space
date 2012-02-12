using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    public class SpaceInventory : AbstractComponent{
        #region Fields
        private ItemHolder[] _items;
        #endregion

        #region Constructor
        
        public SpaceInventory()
        {
            _items = new ItemHolder[25];
        }
        public SpaceInventory(int capacity)
        {
            _items = new ItemHolder[capacity];
        }
        #endregion

        #region Logic
        public bool AddItem(Entity item)
        {
            //todo check if item can be stacked somewhere else
            for (int i = 0; i < _items.Length;i++ )
            {
                var slot = _items[i];
                if (slot == null)
                {
                    _items[i] = new ItemHolder(item.UID);
                    return true;
                }
            }
            return false;
        }
        public void AddItem(Entity item, int index)
        {
            if (_items[index] == null)
                _items[index] = new ItemHolder();
            _items[index].AddItem(item.UID);
        }
        public void AddItem(int item, int index)
        {
            if (_items[index] == null)
                _items[index] = new ItemHolder();
            _items[index].AddItem(item);
        }
        public Entity this[int index]
        {
            get
            {
                if (_items[index] == null)
                    return null;
                return Entity.Manager.GetEntity(_items[index].GetItem());
            }
            set
            {
                if (_items[index] == null)
                    _items[index] = new ItemHolder(value.UID);
                else
                _items[index].SetItem(value.UID);
            }
        }
        public void Add(Entity item)
        {

        }
        public int ItemCount()
        {
            var count = 0;
            foreach (var item in _items)
                if (item != null)
                    count++;
            return count;
        }
        public int Count()
        {
            return _items.Length;
        }

        public void RemoveAt(int index)
        {
            //TODO check if there is a element left
            _items[index] = null;
        }

        public List<Entity> GetAll(int index)
        {
            List<Entity> entitys = new List<Entity>();
            if(_items[index] != null)
            foreach (var item in _items[index].GetItems()){
             entitys.Add(Entity.Manager.GetEntity(item));
            }
            return entitys;
            
        }
        #endregion

        public void Swap(int previousId, int i)
        {
            if (previousId == -1 || i == -1)
            {
                return;
            }
            var helper = _items[i];
            _items[i] = _items[previousId];
            _items[previousId] = helper;
        }

        #region Copying
        
        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_items.Length);
            for (int i = 0; i < _items.Length; i++)
            {
                packet.Write(_items[i]);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            int numItems = packet.ReadInt32();
            _items = new ItemHolder[numItems];
            for (int i = 0; i < numItems; i++)
            {
                _items[i] = packet.ReadPacketizable<ItemHolder>();
            }
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (SpaceInventory)base.DeepCopy(into);

            if (copy == into)
            {
                copy._items = new ItemHolder[_items.Length];
                for (int i = 0; i < _items.Length; i++)
                {
                    copy._items[i] = _items[i];
                }
            }
            else
            {
                copy._items = new ItemHolder[_items.Length];
                for (int i = 0; i < _items.Length; i++)
                {
                    copy._items[i] = _items[i];
                }
            }

            return copy;
        }

        #endregion
    }
}

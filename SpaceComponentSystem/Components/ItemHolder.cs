using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    class ItemHolder : IPacketizable
    {
        private List<int> _data = new List<int>();

        public ItemHolder()
        {

        }
        public ItemHolder(int item)
        {
            _data.Add(item);
        }

        public int GetItem()
        {
            if (_data.Count > 0)
                return _data[0];
            return 0;
        }
        public List<int> GetItems()
        {
            return _data;
        }
        public void SetItem(int item)
        {
            _data.Clear();
            _data.Add(item);
        }
        public void AddItem(int item)
        {
            _data.Add(item);
        }
        public void AddItem(List<int> items)
        {
            _data.AddRange(items);
        }

        public Packet Packetize(Packet packet)
        {
            packet.Write(_data.Count);
            for (var i = 0; i < _data.Count; i++)
            {
                packet.Write(_data[i]);
            }
            return packet;
        }
        public  void Depacketize(Packet packet)
        {
            int numItems = packet.ReadInt32();
            for (int i = 0; i < numItems; i++)
            {
                _data.Add(packet.ReadInt32());
            }
        }
    }
}

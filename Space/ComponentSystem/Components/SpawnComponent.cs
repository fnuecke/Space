using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.ComponentSystem.Systems.Messages;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    public class AiInfo
    {
        #region Fields

        public Vector2 SpawnPoint;
        public int RespawnTime;
        public AiComponent.AiCommand AiCommand;
        public Factions Faction;
        #endregion
        #region Constructor
        public AiInfo(Vector2 spawnPoint, int respawnTime, Factions faction, AiComponent.AiCommand command)
        {
            SpawnPoint = spawnPoint;
            RespawnTime = respawnTime;
            AiCommand = command;
            Faction = faction;
        }
        public AiInfo()
        {

        }
        #endregion

        #region Hash/Copy



        public IComponentSystem DeepCopy(IComponentSystem into)
        {

            return into;
        }

        public Packet Packetize(Packet packet)
        {
            packet.Write(SpawnPoint)
                .Write(RespawnTime)
                ;
            return packet;
        }

        public void Depacketize(Packet packet)
        {
            SpawnPoint = packet.ReadVector2();
            RespawnTime = packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {


            hasher.Put(BitConverter.GetBytes(SpawnPoint.X));
            hasher.Put(BitConverter.GetBytes(SpawnPoint.Y));
            hasher.Put(BitConverter.GetBytes(RespawnTime));
        }
        #endregion
    }
    class SpawnComponent : AbstractComponent
    {

        private List<int> _targets = new List<int>();
        public override void HandleMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {

            }
            else if (message is EntityRemoved)
            {
                var entity = (EntityRemoved)(ValueType)message;
                _targets.Remove(entity.EntityUid);
            }
        }

        public override void Update(object parameterization)
        {
            foreach (var target in _targets)
            {
                //EntityFactory.CreateAIShip()
            }
        }
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }
    }
}

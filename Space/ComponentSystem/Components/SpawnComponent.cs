using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    sealed class AiInfo : ICopyable<AiInfo>
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

        public AiInfo DeepCopy()
        {
            return DeepCopy(null);
        }

        public AiInfo DeepCopy(AiInfo into)
        {
            var copy = into ?? (AiInfo)MemberwiseClone();

            if (copy == into)
            {
                copy.SpawnPoint = SpawnPoint;
                copy.RespawnTime = RespawnTime;
                copy.AiCommand = AiCommand;
                copy.Faction = Faction;
            }

            return copy;
        }

        public Packet Packetize(Packet packet)
        {
            return packet.Write(SpawnPoint)
                .Write(RespawnTime)
                .Write(AiCommand)
                .Write((int)Faction);
        }

        public void Depacketize(Packet packet)
        {
            SpawnPoint = packet.ReadVector2();
            RespawnTime = packet.ReadInt32();
            AiCommand = packet.ReadPacketizableInto(AiCommand);
            Faction = (Factions)packet.ReadInt32();
        }

        public void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(SpawnPoint.X));
            hasher.Put(BitConverter.GetBytes(SpawnPoint.Y));
            hasher.Put(BitConverter.GetBytes(RespawnTime));
        }

        #endregion
    }

    sealed class SpawnComponent : AbstractComponent
    {
        private List<int> _targets = new List<int>();

        public override void HandleMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {

            }
            else if (message is EntityRemoved)
            {
                var info = (EntityRemoved)(ValueType)message;
                _targets.Remove(info.Entity.UID);
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

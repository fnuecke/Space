using System;
using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command<T> : ICommand<T>
        where T : struct
    {
        #region Properties

        public bool IsTentative { get; set; }

        public int Player { get; set; }

        public T Type { get; private set; }

        #endregion

        #region Constructor

        protected Command()
        {
        }

        protected Command(T type)
        {
            Type = type;
        }

        #endregion

        #region Serialization

        public virtual void Packetize(Packet packet)
        {
            packet.Write(IsTentative);
            packet.Write(Player);
            packet.Write(Enum.GetName(typeof(T), Type));
        }

        public virtual void Depacketize(Packet packet)
        {
            IsTentative = packet.ReadBoolean();
            Player = packet.ReadInt32();
            Type = (T)Enum.Parse(typeof(T), packet.ReadString());
        }

        #endregion
    }
}

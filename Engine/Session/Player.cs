using System;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// This class is used to represent a single player in a Session.
    /// </summary>
    public sealed class Player<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        /// <summary>
        /// The player's number in the game he's in.
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// The name of the player.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Some arbitrary data associated with the player.
        /// </summary>
        public TPlayerData Data { get; set; }

        internal Player(int number, string name, TPlayerData data)
        {
            this.Number = number;
            this.Name = name;
            this.Data = data;
        }

        public override bool Equals(object obj)
        {
            if (obj is Player<TPlayerData>)
            {
                return ((Player<TPlayerData>)obj).Number == this.Number;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Number.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("Player {0} ({1})", Number, Name);
        }
    }
}

using System;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// This class is used to represent a single player in a Session.
    /// </summary>
    public sealed class Player<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
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
        /// The average ping to this player.
        /// </summary>
        public int Ping { get { return pingGetter(); } }

        /// <summary>
        /// Some arbitrary data associated with the player.
        /// </summary>
        public TPlayerData Data { get; set; }

        /// <summary>
        /// The session this player belongs to.
        /// </summary>
        private Func<int> pingGetter;

        internal Player(int number, string name, TPlayerData data, Func<int> pingGetter)
        {
            this.Number = number;
            this.Name = name;
            this.Data = data;
            this.pingGetter = pingGetter;
        }

        public override bool Equals(object obj)
        {
            if (obj is Player<TPlayerData, TPacketizerContext>)
            {
                return ((Player<TPlayerData, TPacketizerContext>)obj).Number == this.Number;
            }
            return false;
        }

        public override string ToString()
        {
            return String.Format("Player {0} ({1})", Number, Name);
        }
    }
}

using System;

namespace Engine.Session
{
    /// <summary>
    /// This class is used to represent a single player in a Session.
    /// </summary>
    public class Player
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
        public byte[] Data { get; set; }

        public Player(int number, string name, byte[] data)
        {
            this.Number = number;
            this.Name = name;
            this.Data = data;
        }

        public override string ToString()
        {
            return String.Format("Player {0} ({1})", Number, Name);
        }

    }
}

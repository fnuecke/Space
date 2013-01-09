using System;

namespace Engine.Session
{
    /// <summary>This class is used to represent a single player in a Session.</summary>
    public sealed class Player
    {
        /// <summary>The player's number in the game he's in.</summary>
        public readonly int Number;

        /// <summary>The name of the player.</summary>
        public readonly string Name;

        /// <summary>Some arbitrary data associated with the player.</summary>
        public readonly object Data;

        internal Player(int number, string name, object data)
        {
            Number = number;
            Name = name;
            Data = data;
        }

        public override bool Equals(object obj)
        {
            var player = obj as Player;
            return player != null && (player).Number == Number;
        }

        public override int GetHashCode()
        {
            return Number.GetHashCode();
        }
    }
}
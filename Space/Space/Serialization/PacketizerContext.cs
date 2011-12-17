using System.Collections.Generic;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SpaceData;

namespace Space.Serialization
{
    public class PacketizerContext : IPacketizerContext
    {
        public Game game;

        /// <summary>
        /// The session the packetizer context is bound to.
        /// </summary>
        public ISession Session { get; set; }

        public Dictionary<string, ShipData> shipData = new Dictionary<string, ShipData>();

        public Dictionary<string, Texture2D> shipTextures = new Dictionary<string, Texture2D>();

        public Dictionary<string, SoundEffect> shipSounds = new Dictionary<string, SoundEffect>();

        public Dictionary<string, WeaponData> weaponData = new Dictionary<string, WeaponData>();

        public Dictionary<string, Texture2D> weaponTextures = new Dictionary<string, Texture2D>();

        public Dictionary<string, SoundEffect> weaponsSounds = new Dictionary<string, SoundEffect>();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

using System.Collections.Generic;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceData;

namespace Space.Model
{
    public class PacketizerContext : IPacketizerContext<PlayerInfo, PacketizerContext>
    {
        public Game game;

        /// <summary>
        /// The session the packetizer context is bound to.
        /// </summary>
        public ISession<PlayerInfo, PacketizerContext> Session { get; set; }

        public Dictionary<string, ShipData> shipData = new Dictionary<string, ShipData>();

        public Dictionary<string, Texture2D> shipTextures = new Dictionary<string, Texture2D>();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

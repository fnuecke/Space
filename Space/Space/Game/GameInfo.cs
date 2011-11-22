using System.Text;
using System.Net;

namespace Space.Game
{
    class GameInfo
    {

        public static GameInfo Deserializer(IPEndPoint host, byte[] data)
        {
            var result = new GameInfo();
            result.Host = host;
            result.Name = Encoding.UTF8.GetString(data);
            return result;
        }

        public static byte[] Serializer(GameInfo gameInfo)
        {
            return Encoding.UTF8.GetBytes(gameInfo.Name);
        }

        /// <summary>
        /// Only set for info on games not hosted by self.
        /// </summary>
        public IPEndPoint Host { get; private set; }

        /// <summary>
        /// The name of the game.
        /// </summary>
        public string Name { get; set; }

    }
}

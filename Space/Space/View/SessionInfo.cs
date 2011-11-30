using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Model;

namespace Space.View
{
    static class SessionInfo
    {
        public static void Draw(string title, ISession<PlayerInfo, PacketizerContext> session, Vector2 offset, SpriteFont font, SpriteBatch spriteBatch)
        {
            string sessionInfo = title + " (" + session.NumPlayers + "/" + session.MaxPlayers + ")";
            for (int i = 0; i < session.MaxPlayers; ++i)
            {
                var player = session.GetPlayer(i);
                if (player != null)
                {
                    sessionInfo += "\n#" + player.Number + ": " + player.Name + " [" + player.Ping + "]";
                }
            }

            var sessionInfoMeasure = font.MeasureString(sessionInfo);
            var sessionInfoPosition = offset;

            spriteBatch.Begin();
            spriteBatch.DrawString(font, sessionInfo, sessionInfoPosition, Color.White);
            spriteBatch.End();
        }
    }
}

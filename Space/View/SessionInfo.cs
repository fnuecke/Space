using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.View
{
    internal static class SessionInfo
    {
        public static void Draw(
            string title, ISession session, Vector2 offset, SpriteFont font, SpriteBatch spriteBatch)
        {
            var sessionInfo = title + " (" + session.PlayerCount + "/" + session.MaxPlayers + ")";
            foreach (var player in session.AllPlayers)
            {
                sessionInfo += "\n#" + player.Number + ": " + player.Name;
            }

            var sessionInfoMeasure = font.MeasureString(sessionInfo);
            var sessionInfoPosition = offset;

            spriteBatch.Begin();
            spriteBatch.DrawString(font, sessionInfo, sessionInfoPosition, Color.White);
            spriteBatch.End();
        }
    }
}
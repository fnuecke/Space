using Engine.Session;

namespace Engine.Controller
{
    public sealed class SimpleServerController : AbstractTssServer
    {
        #region Constructor

        public SimpleServerController(IServerSession session)
            : base(session)
        {
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Session.Update();
            
            base.Update(gameTime);
        }

        #endregion
    }
}

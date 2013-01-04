using Engine.Physics.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests.Tests
{
    internal sealed class VerticalStack : AbstractTest
    {
        private int _bullet;

        protected override void Create()
        {
            {
                Manager.AddEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));
                Manager.AddEdge(new Vector2(20.0f, 0.0f), new Vector2(20.0f, 20.0f));
            }

            var xs = new[] {0.0f, -10.0f, -5.0f, 5.0f, 10.0f};

            const int count = 5;
            const int rowCount = 16;
            for (var j = 0; j < count; ++j)
            {
                for (var i = 0; i < rowCount; ++i)
                {
                    Manager.AddRectangle(width: 1, height: 1,
                                         type: Body.BodyType.Dynamic,
                                         worldPosition: new WorldPoint(xs[j], 0.752f + 1.54f * i),
                                         density: 1, friction: 0.3f);
                }
            }

            _bullet = 0;
        }
        
        public override void  OnKeyDown(Keys key)
        {
            base.OnKeyDown(key);
            switch (key)
            {
                case Keys.OemComma:
                    if (_bullet != 0)
                    {
                        Manager.RemoveEntity(_bullet);
                        _bullet = 0;
                    }
                    var bullet = Manager.AddCircle(0.25f,
                                                   type: Body.BodyType.Dynamic,
                                                   worldPosition: new WorldPoint(-31, 5),
                                                   isBullet: true,
                                                   density: 20, restitution: 0.05f);

                    bullet.LinearVelocity = new Vector2(400, 0);
                    _bullet = bullet.Entity;
                    break;
            }
        }

        protected override void Step()
        {
            DrawString("Press: (,) to launch a bullet.");
        }
    }
}

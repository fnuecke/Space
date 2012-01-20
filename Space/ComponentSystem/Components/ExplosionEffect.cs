using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Space.ComponentSystem.Components
{
    class ExplosionEffect:Effect
    {
        public ExplosionEffect()
            :base("Effects/BasicExplosion")
        {
            
        }

        public override void Update(object parameterization)
        {
            base.Update(parameterization);
            if (_effect[0] != null && _isDrawingInstance)
            {
                Emitting = false;
            }
        }
    }
}

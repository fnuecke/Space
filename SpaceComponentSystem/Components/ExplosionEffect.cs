namespace Space.ComponentSystem.Components
{
    sealed class ExplosionEffect : Effect
    {
        public ExplosionEffect(float scale = 0)
            : base("Effects/BasicExplosion")
        {
            Scale = scale;
        }

        public ExplosionEffect()
        {
        }

        public override void Update(object parameterization)
        {
            base.Update(parameterization);
            if (_effect[0] != null)
            {
                if (Emitting && _effect[0].ActiveParticlesCount > 0)
                {
                    Emitting = false;
                }
                else if (!Emitting && _effect[0].ActiveParticlesCount == 0)
                {
                    Entity.Manager.RemoveEntity(Entity);
                }
            }
        }
    }
}

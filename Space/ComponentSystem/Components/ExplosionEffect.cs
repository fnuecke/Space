namespace Space.ComponentSystem.Components
{
    sealed class ExplosionEffect : Effect
    {
        public ExplosionEffect()
            : base("Effects/BasicExplosion")
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

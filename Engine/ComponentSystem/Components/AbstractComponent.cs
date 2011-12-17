namespace Engine.ComponentSystem.Components
{
    public abstract class AbstractComponent : IComponent
    {
        public virtual void Update(object parameterization)
        {
        }

        public virtual bool SupportsParameterization(object parameterization)
        {
            return false;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public abstract void Packetize(Serialization.Packet packet);

        public abstract void Depacketize(Serialization.Packet packet);

        public abstract void Hash(Util.Hasher hasher);
    }
}

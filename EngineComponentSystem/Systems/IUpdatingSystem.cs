namespace Engine.ComponentSystem.Systems
{
    /// <summary>Interface for logic implementing systems.</summary>
    public interface IUpdatingSystem
    {
        /// <summary>Updates the system.</summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        void Update(long frame);
    }
}
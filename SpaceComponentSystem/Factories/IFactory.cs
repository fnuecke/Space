namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Interface for constraints (for lookup).
    /// </summary>
    public interface IFactory
    {
        /// <summary>
        /// The unique name of the object type.
        /// </summary>
        string Name { get; set; }
    }
}

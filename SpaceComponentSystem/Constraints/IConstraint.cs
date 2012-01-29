namespace Space.ComponentSystem.Constraints
{
    /// <summary>
    /// Interface for constraints (for lookup).
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        /// The unique name of the constraint.
        /// </summary>
        string Name { get; }
    }
}

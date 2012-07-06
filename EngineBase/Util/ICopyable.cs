namespace Engine.Util
{
    /// <summary>
    /// Interface for objects supporting deep copies.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    public interface ICopyable<T>
    {
        /// <summary>
        /// Creates a new copy of the same type as the object.
        /// </summary>
        /// <returns>The copy.</returns>
        T DeepCopy();

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        T DeepCopy(T into);
    }
}

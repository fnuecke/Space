namespace Engine.Util
{
    /// <summary>
    /// Interface for objects of which a hash should be computable.
    /// </summary>
    public interface IHashable
    {
        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        void Hash(Hasher hasher);
    }
}

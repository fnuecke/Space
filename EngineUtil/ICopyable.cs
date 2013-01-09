namespace Engine.Util
{
    /// <summary>Interface for objects supporting deep copies.</summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    public interface ICopyable<T> where T : class
    {
        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        T NewInstance();

        /// <summary>Creates a deep copy of the object, reusing the given object.</summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        void CopyInto(T into);
    }
}
namespace Space.Data
{
    /// <summary>
    /// Possible player classes.
    /// </summary>
    public enum PlayerClassType
    {
        Fighter,

        /// <summary>
        /// Default player class. This is used as a fall-back while loading,
        /// to gracefully handle corrupt profile data.
        /// </summary>
        Default = Fighter
    }
}

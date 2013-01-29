using System.Collections.Generic;

namespace Engine.Random
{
    public static class ListExtensions
    {
        /// <summary>Shuffles the specified list using the Fisher-Yates shuffle.</summary>
        /// <typeparam name="T">The type of element stored in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="random">The random number generator to use for shuffling. If none is specified, a new one will be created.</param>
        public static void Shuffle<T>(this IList<T> list, IUniformRandom random = null)
        {
            list.Shuffle(0, list.Count, random);
        }

        /// <summary>
        ///     Shuffles the specified list using the Fisher-Yates shuffle. This only shuffles a segment in a list, as opposed
        ///     to shuffling the complete list.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="offset">The offset at which to start shuffling.</param>
        /// <param name="length">The length of the interval to shuffle.</param>
        /// <param name="random">The random number generator to use for shuffling. If none is specified, a new one will be created.</param>
        public static void Shuffle<T>(this IList<T> list, int offset, int length, IUniformRandom random = null)
        {
            random = random ?? new MersenneTwister();
            for (var i = offset + length - 1; i > offset; --i)
            {
                var j = random.NextInt32(i + 1);
                var t = list[j];
                list[j] = list[i];
                list[i] = t;
            }
        }
    }
}
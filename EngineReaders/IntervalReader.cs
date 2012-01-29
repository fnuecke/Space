using System;
using Engine.Util;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    public abstract class AbstractIntervalReader<T> : ContentTypeReader<Interval<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        protected override Interval<T> Read(ContentReader input, Interval<T> existingInstance)
        {
            existingInstance = existingInstance ?? new Interval<T>();

            T low = Read(input);
            T high = Read(input);

            existingInstance.SetTo(low, high);

            return existingInstance;
        }

        protected abstract T Read(ContentReader input);
    }

    public sealed class DoubleIntervalReader : AbstractIntervalReader<double>
    {
        protected override double Read(ContentReader input)
        {
            return input.ReadDouble();
        }
    }

    public sealed class FloatIntervalReader : AbstractIntervalReader<float>
    {
        protected override float Read(ContentReader input)
        {
            return input.ReadSingle();
        }
    }

    public sealed class Int32IntervalReader : AbstractIntervalReader<int>
    {
        protected override int Read(ContentReader input)
        {
            return input.ReadInt32();
        }
    }
}

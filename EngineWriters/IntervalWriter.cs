using System;
using Engine.Util;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Engine.Serialization
{
    public abstract class AbstractIntervalWriter<T> : ContentTypeWriter<Interval<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        protected override void Write(ContentWriter output, Interval<T> value)
        {
            Write(output, value.Low);
            Write(output, value.High);
        }

        protected abstract void Write(ContentWriter output, T value);

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(Interval<T>).AssemblyQualifiedName;
        }
    }

    [ContentTypeWriter]
    public sealed class DoubleIntervalWriter : AbstractIntervalWriter<double>
    {
        protected override void Write(ContentWriter output, double value)
        {
            output.Write(value);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(DoubleIntervalReader).AssemblyQualifiedName;
        }
    }

    [ContentTypeWriter]
    public sealed class FloatIntervalWriter : AbstractIntervalWriter<float>
    {
        protected override void Write(ContentWriter output, float value)
        {
            output.Write(value);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FloatIntervalReader).AssemblyQualifiedName;
        }
    }

    [ContentTypeWriter]
    public sealed class Int32IntervalWriter : AbstractIntervalWriter<int>
    {
        protected override void Write(ContentWriter output, int value)
        {
            output.Write(value);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(Int32IntervalReader).AssemblyQualifiedName;
        }
    }
}

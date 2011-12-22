using System;
using System.Text.RegularExpressions;
using Engine.Data;
using Engine.Math;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    public abstract class AbstractEntityAttributeSerializer<TAttribute> : ContentTypeSerializer<EntityAttribute<TAttribute>>
        where TAttribute : struct
    {
        
        private static readonly Regex AttributePattern = new Regex(@"^\s*(?<type>[\+\-])?(?<value>[0-9]*(\.[0-9]+)?)(?<percentual>%)?\s+(?<class>\w+)\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        protected override void Serialize(IntermediateWriter output, EntityAttribute<TAttribute> value, ContentSerializerAttribute format)
        {
            switch (value.ComputationType)
            {
                case EntityAttributeComputationType.Additive:
                    output.Xml.WriteValue(value.Value.DoubleValue.ToString() + " " + value.Type.ToString());
                    break;
                case EntityAttributeComputationType.Multiplicative:
                    output.Xml.WriteValue((value.Value.DoubleValue - 1).ToString() + "% " + value.Type.ToString());
                    break;
            }
        }

        protected override EntityAttribute<TAttribute> Deserialize(IntermediateReader input, ContentSerializerAttribute format, EntityAttribute<TAttribute> existingInstance)
        {
            // Parse the content.
            var match = AttributePattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                if (existingInstance == null)
                {
                    existingInstance = new EntityAttribute<TAttribute>();
                }

                // Pattern was OK, get the enum.
                existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), match.Groups["class"].Value);

                // Now get the numeric value for the attribute.
                var value = Fixed.Create(double.Parse(match.Groups["value"].Value));

                // Check if it's a negative value.
                var type = match.Groups["type"];
                if (type.Success && type.Value.Equals("-"))
                {
                    value = -value;
                }

                // Check if it's a percentual value, and if so set mode to multiplicative.
                var percentual = match.Groups["percentual"];
                if (percentual.Success)
                {
                    value = ((Fixed)1) + value;
                    existingInstance.ComputationType = EntityAttributeComputationType.Multiplicative;
                }
                else
                {
                    existingInstance.ComputationType = EntityAttributeComputationType.Additive;
                }

                // Set final value in our instance.
                existingInstance.Value = value;

                return existingInstance;
            }
            else
            {
                throw new ArgumentException("input");
            }
        }
    }

    /// <summary>
    /// This is for writing data back in binary format.
    /// </summary>
    public abstract class AbstractEntityAttributeWriter<TAttribute> : ContentTypeWriter<EntityAttribute<TAttribute>>
        where TAttribute : struct
    {
        protected override void Write(ContentWriter output, EntityAttribute<TAttribute> value)
        {
            output.Write(Enum.GetName(typeof(TAttribute), value.Type));
            output.Write((byte)value.ComputationType);
            output.Write(value.Value.DoubleValue);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(EntityAttribute<TAttribute>).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(EntityAttributeReader<TAttribute>).AssemblyQualifiedName;
        }
    }
}
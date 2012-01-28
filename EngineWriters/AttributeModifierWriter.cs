using System;
using System.Text.RegularExpressions;
using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    public abstract class AbstractAttributeModifierSerializer<TAttribute> : ContentTypeSerializer<AttributeModifier<TAttribute>>
        where TAttribute : struct
    {

        private static readonly Regex AttributePattern = new Regex(@"
            ^\s*            # Complete line, ignore leading whitespace.
            (?<value>       # Read the actual value, which must be a number.
                -?[0-9]+
                (
                    \.[0-9]+    # Optionally a floating point value.
                )?
            )
            (?<percentual>  # Check if it's a percentual value.
                %
            )?
            \s+             # Separate attribute type with whitespace.
            (?<type>        # Read the attribute type.
                \w+
            )
            \s*$    # Skip trailing whitespace",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        protected override void Serialize(IntermediateWriter output, AttributeModifier<TAttribute> value, ContentSerializerAttribute format)
        {
            switch (value.ComputationType)
            {
                case AttributeComputationType.Additive:
                    output.Xml.WriteValue(value.Value.ToString() + " " + Enum.GetName(typeof(TAttribute), value.Type));
                    break;
                case AttributeComputationType.Multiplicative:
                    output.Xml.WriteValue(((value.Value - 1) * 100).ToString() + "% " + Enum.GetName(typeof(TAttribute), value.Type));
                    break;
            }
        }

        protected override AttributeModifier<TAttribute> Deserialize(IntermediateReader input, ContentSerializerAttribute format, AttributeModifier<TAttribute> existingInstance)
        {
            // Parse the content.
            var match = AttributePattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                existingInstance = existingInstance ?? new AttributeModifier<TAttribute>();

                // Pattern was OK, get the enum.
                existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), match.Groups["type"].Value);

                // Now get the numeric value for the attribute.
                var value = float.Parse(match.Groups["value"].Value);

                // Check if it's a percentual value, and if so set mode to multiplicative.
                var percentual = match.Groups["percentual"];
                if (percentual.Success)
                {
                    value = (value / 100f) + 1;
                    existingInstance.ComputationType = AttributeComputationType.Multiplicative;
                }
                else
                {
                    existingInstance.ComputationType = AttributeComputationType.Additive;
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
    public abstract class AbstractAttributeModifierWriter<TAttribute> : ContentTypeWriter<AttributeModifier<TAttribute>>
        where TAttribute : struct
    {
        protected override void Write(ContentWriter output, AttributeModifier<TAttribute> value)
        {
            output.Write(Enum.GetName(typeof(TAttribute), value.Type));
            output.Write((byte)value.ComputationType);
            output.Write(value.Value);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(AttributeModifier<TAttribute>).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(AttributeModifierReader<TAttribute>).AssemblyQualifiedName;
        }
    }
}
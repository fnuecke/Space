using System;
using System.Text.RegularExpressions;
using Engine.Data;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading actual XML files in the content project.
    /// </summary>
    public abstract class AbstractEntityAttributeSerializer<TAttribute> : ContentTypeSerializer<ModuleAttribute<TAttribute>>
        where TAttribute : struct
    {
        
        private static readonly Regex AttributePattern = new Regex(@"^\s*(?<type>[\+\-])?(?<value>[0-9]*(\.[0-9]+)?)(?<percentual>%)?\s+(?<class>\w+)\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        protected override void Serialize(IntermediateWriter output, ModuleAttribute<TAttribute> value, ContentSerializerAttribute format)
        {
            switch (value.ComputationType)
            {
                case ModuleAttributeComputationType.Additive:
                    output.Xml.WriteValue(value.Value.ToString() + " " + value.Type.ToString());
                    break;
                case ModuleAttributeComputationType.Multiplicative:
                    output.Xml.WriteValue((value.Value - 1).ToString() + "% " + value.Type.ToString());
                    break;
            }
        }

        protected override ModuleAttribute<TAttribute> Deserialize(IntermediateReader input, ContentSerializerAttribute format, ModuleAttribute<TAttribute> existingInstance)
        {
            // Parse the content.
            var match = AttributePattern.Match(input.Xml.ReadContentAsString());
            if (match.Success)
            {
                if (existingInstance == null)
                {
                    existingInstance = new ModuleAttribute<TAttribute>();
                }

                // Pattern was OK, get the enum.
                existingInstance.Type = (TAttribute)Enum.Parse(typeof(TAttribute), match.Groups["class"].Value);

                // Now get the numeric value for the attribute.
                var value = float.Parse(match.Groups["value"].Value);

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
                    value = 1 + value;
                    existingInstance.ComputationType = ModuleAttributeComputationType.Multiplicative;
                }
                else
                {
                    existingInstance.ComputationType = ModuleAttributeComputationType.Additive;
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
    public abstract class AbstractEntityAttributeWriter<TAttribute> : ContentTypeWriter<ModuleAttribute<TAttribute>>
        where TAttribute : struct
    {
        protected override void Write(ContentWriter output, ModuleAttribute<TAttribute> value)
        {
            output.Write(Enum.GetName(typeof(TAttribute), value.Type));
            output.Write((byte)value.ComputationType);
            output.Write(value.Value);
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(ModuleAttribute<TAttribute>).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(EntityAttributeReader<TAttribute>).AssemblyQualifiedName;
        }
    }
}
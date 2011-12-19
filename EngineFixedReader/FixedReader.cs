using Engine.Math;
using Microsoft.Xna.Framework.Content;

namespace Engine.Serialization
{
    /// <summary>
    /// This is for reading data in binary format written with the <c>FixedWriter</c>.
    /// </summary>
    public class FixedReader : ContentTypeReader<Fixed>
    {
        protected override Fixed Read(ContentReader input, Fixed existingInstance)
        {
            if (input != null)
            {
                return Fixed.Create(input.ReadDouble());
            }
            else
            {
                return Fixed.Create(0);
            }
        }
    }
}
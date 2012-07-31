// Adjust this as necessary, it just have to share a compatible
// interface with the XNA types.
using TRectangle = Engine.Math.RectangleF;

namespace Tests.MichaelCoyle
{
    /// <summary>
    /// An interface that defines and object with a rectangle
    /// </summary>
    public interface IHasRect
    {
        TRectangle Rectangle { get; }
    }
}

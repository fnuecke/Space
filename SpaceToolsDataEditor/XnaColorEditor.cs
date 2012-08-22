using System;
using System.Drawing.Design;

namespace Space.Tools.DataEditor
{
    public sealed class XnaColorEditor : ColorEditor
    {
        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (value is Microsoft.Xna.Framework.Color)
            {
                var xnaColor = (Microsoft.Xna.Framework.Color)value;
                var color = System.Drawing.Color.FromArgb(xnaColor.A, xnaColor.R, xnaColor.G, xnaColor.B);
                var result = (System.Drawing.Color)base.EditValue(context, provider, color);
                return new Microsoft.Xna.Framework.Color
                {
                    A = result.A,
                    R = result.R,
                    G = result.G,
                    B = result.B,
                };
            }
            return base.EditValue(context, provider, value);
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is Microsoft.Xna.Framework.Color)
            {
                var xnaColor = (Microsoft.Xna.Framework.Color)e.Value;
                base.PaintValue(new PaintValueEventArgs(e.Context, System.Drawing.Color.FromArgb(xnaColor.A, xnaColor.R, xnaColor.G, xnaColor.B), e.Graphics, e.Bounds));
            }
            else
            {
                base.PaintValue(e);
            }
        }
    }
}

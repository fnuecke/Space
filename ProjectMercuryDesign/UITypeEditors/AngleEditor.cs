/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Design.UITypeEditors
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using MathHelper = Microsoft.Xna.Framework.MathHelper;

    public class AngleEditor : UITypeEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AngleEditor"/> class.
        /// </summary>
        public AngleEditor() { }

        /// <summary>
        /// Gets the editor style used by the <see cref="M:System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)"/> method.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that can be used to gain additional context information.</param>
        /// <returns>
        /// A <see cref="T:System.Drawing.Design.UITypeEditorEditStyle"/> value that indicates the style of editor used by the <see cref="M:System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)"/> method. If the <see cref="T:System.Drawing.Design.UITypeEditor"/> does not support this method, then <see cref="M:System.Drawing.Design.UITypeEditor.GetEditStyle"/> will return <see cref="F:System.Drawing.Design.UITypeEditorEditStyle.None"/>.
        /// </returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        /// <summary>
        /// Edits the specified object's value using the editor style indicated by the <see cref="M:System.Drawing.Design.UITypeEditor.GetEditStyle"/> method.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that can be used to gain additional context information.</param>
        /// <param name="provider">An <see cref="T:System.IServiceProvider"/> that this editor can use to obtain services.</param>
        /// <param name="value">The object to edit.</param>
        /// <returns>
        /// The new value of the object. If the value of the object has not changed, this should return the same object it was passed.
        /// </returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (value.GetType() != typeof(float))
                return value;

            var editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            if (editorService != null)
            {
                float angleDegrees = MathHelper.ToDegrees((float)value);

                AngleControl control = new AngleControl(angleDegrees);

                editorService.DropDownControl(control);

                float angleRadians = MathHelper.ToRadians(control.Angle);

                return angleRadians;
            }

            return value;
        }

        /// <summary>
        /// Paints a representation of the value of an object using the specified <see cref="T:System.Drawing.Design.PaintValueEventArgs"/>.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Drawing.Design.PaintValueEventArgs"/> that indicates what to paint and where to paint it.</param>
        public override void PaintValue(PaintValueEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

            Point center = new Point
            {
                X = e.Bounds.Width / 2,
                Y = e.Bounds.Height / 2
            };

            var backgroundBrush = Brushes.SteelBlue;
            var circleBrush = Brushes.White;
            var centerBrush = Brushes.Black;
            var anglePen = new Pen(Brushes.Red, 1f);

            // Fill background and ellipse and center point...
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
            e.Graphics.FillEllipse(circleBrush, e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
            e.Graphics.FillEllipse(centerBrush, center.X + e.Bounds.X - 1, center.Y + e.Bounds.Y - 1, 3, 3);

            // Draw line along the current Angle...
            float radians = (float)e.Value;

            e.Graphics.DrawLine(anglePen, center.X + e.Bounds.X, center.Y + e.Bounds.Y,
                e.Bounds.X + (center.X + (int)((float)center.X * Calculator.Cos(radians))),
                e.Bounds.Y + (center.Y + (int)((float)center.Y * Calculator.Sin(radians))));
        }

        /// <summary>
        /// Indicates whether the specified context supports painting a representation of an object's value within the specified context.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that can be used to gain additional context information.</param>
        /// <returns>
        /// true if <see cref="M:System.Drawing.Design.UITypeEditor.PaintValue(System.Object,System.Drawing.Graphics,System.Drawing.Rectangle)"/> is implemented; otherwise, false.
        /// </returns>
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private class AngleControl : UserControl
        {
            /// <summary>
            /// Gets or sets the angle.
            /// </summary>
            /// <value>The angle.</value>
            public float Angle { get; set; }

            /// <summary>
            /// 
            /// </summary>
            private int Rotation = 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="AngleControl"/> class.
            /// </summary>
            /// <param name="initialAngle">The initial angle.</param>
            public AngleControl(float initialAngle)
            {
                this.Angle = initialAngle;

                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            }

            /// <summary>
            /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
            /// </summary>
            /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

                // Set Angle origin point at center of control.
                int originX = (this.Width / 2);
                int originY = (this.Height / 2);

                // Fill background and ellipse and center point.
                e.Graphics.FillRectangle(Brushes.SteelBlue, 0, 0, this.Width, this.Height);
                e.Graphics.FillEllipse(Brushes.White, 1, 1, this.Width - 3, this.Height - 3);
                e.Graphics.FillEllipse(Brushes.SlateBlue, originX - 2, originY - 2, 5, 5);

                // Draw Angle markers.
                int startangle = (270 - this.Rotation) % 360;
                
                e.Graphics.DrawString(startangle.ToString(), new Font("Tahoma", 8), Brushes.WhiteSmoke, (this.Width / 2) - 10, 10);
                
                startangle = (startangle + 90) % 360;
                
                e.Graphics.DrawString(startangle.ToString(), new Font("Tahoma", 8), Brushes.WhiteSmoke, this.Width - 18, (this.Height / 2) - 6);
                
                startangle = (startangle + 90) % 360;
                
                e.Graphics.DrawString(startangle.ToString(), new Font("Tahoma", 8), Brushes.WhiteSmoke, (this.Width / 2) - 6, this.Height - 18);
                
                startangle = (startangle + 90) % 360;
                
                e.Graphics.DrawString(startangle.ToString(), new Font("Tahoma", 8), Brushes.WhiteSmoke, 10, (this.Height / 2) - 6);

                // Draw line along the current Angle.         
                float radians = ((((this.Angle + this.Rotation) + 360f) % 360f) * Calculator.Pi) / 180f;
                
                e.Graphics.DrawLine(new Pen(Brushes.Red, 1), originX, originY,
                    originX + (int)((float)originX * (float)Math.Cos(radians)),
                    originY + (int)((float)originY * (float)Math.Sin(radians)));

                // Output Angle information.
                e.Graphics.DrawString("Angle: " + this.Angle.ToString("F4"), new Font("Tahoma", 8), Brushes.WhiteSmoke, this.Width - 84, 2);
            }

            /// <summary>
            /// Raises the <see cref="E:System.Windows.Forms.Control.MouseDown"/> event.
            /// </summary>
            /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
            protected override void OnMouseDown(MouseEventArgs e)
            {
                this.UpdateAngle(e.X, e.Y);

                this.Refresh();
            }

            /// <summary>
            /// Raises the <see cref="E:System.Windows.Forms.Control.MouseMove"/> event.
            /// </summary>
            /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    this.UpdateAngle(e.X, e.Y);

                this.Refresh();
            }

            /// <summary>
            /// Updates the angle.
            /// </summary>
            /// <param name="mx">The mx.</param>
            /// <param name="my">My.</param>
            private void UpdateAngle(int mx, int my)
            {
                // Translate y coordinate input to GetAngle method to correct for ellipsoid distortion...
                float widthToHeightRatio = (float)this.Width / (float)this.Height;
                
                int tmy;
                
                if (my == 0)
                    tmy = my;
                
                else if (my < this.Height / 2)
                    tmy = (this.Height / 2) - (int)(((this.Height / 2) - my) * widthToHeightRatio);
                
                else
                    tmy = (this.Height / 2) + (int)((float)(my - (this.Height / 2)) * widthToHeightRatio);

                // Retrieve updated Angle based on rise over run.
                this.Angle = (this.GetAngle(this.Width / 2, this.Height / 2, mx, tmy) - this.Rotation) % 360;
            }

            /// <summary>
            /// Gets the angle.
            /// </summary>
            /// <param name="x1">The x1.</param>
            /// <param name="y1">The y1.</param>
            /// <param name="x2">The x2.</param>
            /// <param name="y2">The y2.</param>
            /// <returns></returns>
            private float GetAngle(int x1, int y1, int x2, int y2)
            {
                float degrees;

                // Avoid divide by zero run values...
                if (x2 - x1 == 0)
                {
                    if (y2 > y1)
                        degrees = 90;
                    
                    else
                        degrees = 270;
                }
                else
                {
                    // Calculate Angle from offset...
                    float riseoverrun = (float)(y2 - y1) / (float)(x2 - x1);
                    
                    float radians = Calculator.Atan(riseoverrun);
                    
                    degrees = radians * ((float)180 / Calculator.Pi);

                    // Handle quadrant specific transformations..
                    if ((x2 - x1) < 0 || (y2 - y1) < 0)
                        degrees += 180;
                    
                    if ((x2 - x1) > 0 && (y2 - y1) < 0)
                        degrees -= 180;
                    
                    if (degrees < 0)
                        degrees += 360;
                }

                return degrees;
            }
        }
    }
}

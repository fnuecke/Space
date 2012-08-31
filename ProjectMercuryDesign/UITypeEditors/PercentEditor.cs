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

    public class PercentEditor : UITypeEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AngleEditor"/> class.
        /// </summary>
        public PercentEditor() { }

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
                PercentControl control = new PercentControl((float)value, editorService);

                editorService.DropDownControl(control);

                return control.Value;
            }

            return value;
        }

        /// <summary>
        /// Paints a representation of the value of an object using the specified <see cref="T:System.Drawing.Design.PaintValueEventArgs"/>.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Drawing.Design.PaintValueEventArgs"/> that indicates what to paint and where to paint it.</param>
        public override void PaintValue(PaintValueEventArgs e)
        {
            Brush backgroundBrush = new SolidBrush(Color.FromArgb((int)((float)e.Value * 255f), Color.White));

            e.Graphics.FillRectangle(Brushes.Black, e.Bounds);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
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

        private class PercentControl : UserControl
        {
            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The angle.</value>
            public float Value { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="AngleControl"/> class.
            /// </summary>
            /// <param name="initialAngle">The initial angle.</param>
            public PercentControl(float initialAngle, IWindowsFormsEditorService edService)
            {
                this.Value = initialAngle;

                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

                TrackBar trackBar = new TrackBar
                {
                    Dock = DockStyle.Right,
                    LargeChange = 25,
                    Maximum = 100,
                    Minimum = 0,
                    Orientation = Orientation.Vertical,
                    SmallChange = 5,
                    TickFrequency = 10,
                    TickStyle = TickStyle.Both,
                    Value = (int)(initialAngle * 100f)
                };

                base.Controls.Add(trackBar);

                trackBar.ValueChanged += (s, ea) => this.Value = trackBar.Value / 100f;
                
                trackBar.MouseUp += (s, ea) => edService.CloseDropDown();
            }

            /// <summary>
            /// </summary>
            /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
            protected override void OnParentChanged(EventArgs e)
            {
                if (this.Parent != null)
                {
                    this.Parent.Width = 64;
                }
            }
        }
    }
}

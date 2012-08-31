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
    using System.Drawing.Design;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using Microsoft.Xna.Framework;

    //YES I'm British OK!
    using Colour = System.Drawing.Color;

    public class VectorColourEditor : ColorEditor
    {
        /// <summary>
        /// Edits the given object value using the editor style provided by the <see cref="M:System.Drawing.Design.ColorEditor.GetEditStyle(System.ComponentModel.ITypeDescriptorContext)"/> method.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that can be used to gain additional context information.</param>
        /// <param name="provider">An <see cref="T:System.IServiceProvider"/> through which editing services may be obtained.</param>
        /// <param name="value">An instance of the value being edited.</param>
        /// <returns>
        /// The new value of the object. If the value of the object has not changed, this should return the same object it was passed.
        /// </returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var xnaColour = (Vector3)value;

            if (provider != null)
            {
                var edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc == null)
                    return value;

                this.StartColourPicker(edSvc, Colour.FromArgb(1, (int)(xnaColour.X * 255f),
                                                                 (int)(xnaColour.Y * 255f),
                                                                 (int)(xnaColour.Z * 255f)));
                edSvc.DropDownControl(this.ColourPicker);

                if ((this.ColorPickerValue != null) && (((Colour)this.ColorPickerValue) != Colour.Empty))
                {
                    var chosenColour = (Colour)this.ColorPickerValue;

                    value = new Vector3
                    {
                        X = chosenColour.R / 255f,
                        Y = chosenColour.G / 255f,
                        Z = chosenColour.B / 255f
                    };
                }

                this.EndColourPicker();
            }

            return value;
        }

        /// <summary>
        /// Paints a representative value of the given object to the provided canvas.
        /// </summary>
        /// <param name="e">What to paint and where to paint it.</param>
        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is Vector3)
            {
                var colour = (Vector3)e.Value;

                var displayColor = Colour.FromArgb((int)(colour.X * 255f),
                                                   (int)(colour.Y * 255f),
                                                   (int)(colour.Z * 255f));

                using (var brush = new SolidBrush(displayColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }
        }

        /// <summary>
        /// Ensures the colour picker.
        /// </summary>
        private void EnsureColourPicker()
        {
            if (this.ColourPicker == null)
            {
                var uiType = typeof(ColorEditor).GetNestedType("ColorUI", BindingFlags.NonPublic);

                var ui = Activator.CreateInstance(uiType, new object[] { this });

                this.ColourPicker = (Control)ui;
            }
        }

        /// <summary>
        /// Gets or sets the colour picker.
        /// </summary>
        /// <value>The colour picker.</value>
        private Control ColourPicker
        {
            get
            {
                var field = typeof(ColorEditor).GetField("colorUI", BindingFlags.Instance |
                                                                    BindingFlags.NonPublic);
                return (Control)field.GetValue(this);
            }
            set
            {
                var field = typeof(ColorEditor).GetField("colorUI", BindingFlags.Instance |
                                                                    BindingFlags.NonPublic);
                field.SetValue(this, value);
            }
        }

        /// <summary>
        /// Gets the color picker value.
        /// </summary>
        /// <value>The color picker value.</value>
        private object ColorPickerValue
        {
            get
            {
                var valueProperty = this.ColourPicker.GetType().GetProperty("Value");

                return valueProperty.GetValue(this.ColourPicker, null);
            }
        }

        /// <summary>
        /// Starts the colour picker.
        /// </summary>
        /// <param name="edSvc">The ed SVC.</param>
        /// <param name="value">The value.</param>
        private void StartColourPicker(IWindowsFormsEditorService edSvc, object value)
        {
            this.EnsureColourPicker();

            var startMethod = this.ColourPicker.GetType().GetMethod("Start");

            startMethod.Invoke(this.ColourPicker, new object[] { edSvc, value });
        }

        /// <summary>
        /// Ends the colour picker.
        /// </summary>
        private void EndColourPicker()
        {
            var endMethod = this.ColourPicker.GetType().GetMethod("End");

            endMethod.Invoke(this.ColourPicker, null);
        }
    }
}
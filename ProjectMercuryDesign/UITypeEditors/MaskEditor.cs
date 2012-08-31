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
    using System.IO;
    using System.Windows.Forms;

    public class MaskEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg" })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FileInfo fileInfo = new FileInfo(dialog.FileName);

                    if (fileInfo.Exists)
                    {
                        Bitmap selectedImage = Image.FromFile(fileInfo.FullName) as Bitmap;

                        byte[][] mask = new byte[selectedImage.Width][];

                        for (int i = 0; i < mask.Length; i++)
                            mask[i] = new byte[selectedImage.Height];

                        for (int x = 0; x < selectedImage.Width; x++)
                        {
                            for (int y = 0; y < selectedImage.Height; y++)
                            {
                                Color color = selectedImage.GetPixel(x, y);

                                int sum = color.R + color.G + color.B;

                                mask[x][y] = (byte)(sum / 3);
                            }
                        }

                        return mask;
                    }
                }
            }

            return value;
        }
    }
}
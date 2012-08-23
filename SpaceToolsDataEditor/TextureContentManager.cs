using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Tools.DataEditor
{
    sealed class TextureContentManager : ContentManager
    {
        private TextureImporter _importer = new TextureImporter();

        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        public TextureContentManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override T Load<T>(string assetName)
        {
            if (typeof(T) != typeof(Texture2D))
            {
                return default(T);
            }
            if (_textures.ContainsKey(assetName))
            {
                return (T)(object)_textures[assetName];
            }
            using (var img = Image.FromFile(ContentProjectManager.GetFileForTextureAsset(assetName)))
            {
                var bmp = new Bitmap(img);
                var data = new uint[bmp.Width * bmp.Height];
                for (var i = 0; i != bmp.Width; ++i)
                {
                    for (var j = 0; j != bmp.Height; ++j)
                    {
                        var pixel = bmp.GetPixel(i, j);
                        data[i + j * bmp.Width] =
                            Microsoft.Xna.Framework.Color.FromNonPremultiplied(pixel.R, pixel.G, pixel.B, pixel.A).
                                PackedValue;
                    }
                }
                var g = (IGraphicsDeviceService)ServiceProvider.GetService(typeof(IGraphicsDeviceService));
                if (g != null)
                {
                    var t = new Texture2D(g.GraphicsDevice, img.Width, img.Height);
                    _textures.Add(assetName, t);
                    return (T)(object)t;
                }
                else
                {
                    throw new InvalidOperationException("Must wait with loading until graphics device is initialized.");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Tools.DataEditor
{
    sealed class TextureContentManager : ContentManager
    {
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
            var g = (IGraphicsDeviceService)ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            if (_textures.ContainsKey(assetName))
            {
                return (T)(object)_textures[assetName];
            }
            using (var img = Image.FromFile(ContentProjectManager.GetFileForTextureAsset(assetName)))
            {
                var bmp = new Bitmap(img);
                var data = new uint[bmp.Width * bmp.Height];
                for (var y = 0; y != bmp.Height; ++y)
                {
                    for (var x = 0; x != bmp.Width; ++x)
                    {
                        var pixel = bmp.GetPixel(y, x);
                        data[y + x * bmp.Width] =
                            Microsoft.Xna.Framework.Color.FromNonPremultiplied(pixel.R, pixel.G, pixel.B, pixel.A).
                                PackedValue;
                    }
                }
                if (g != null)
                {
                    var t = new Texture2D(g.GraphicsDevice, img.Width, img.Height);
                    t.SetData(data);
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

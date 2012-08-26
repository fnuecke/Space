using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury;

namespace Space.Tools.DataEditor
{
    sealed class PlainContentManager : ContentManager
    {
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        private readonly Dictionary<string, Effect> _shaders = new Dictionary<string, Effect>();

        public PlainContentManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override T Load<T>(string assetName)
        {
                var g = (IGraphicsDeviceService)ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            if (typeof(T) == typeof(Texture2D))
            {
                if (_textures.ContainsKey(assetName))
                {
                    return (T)(object)_textures[assetName];
                }

                using (var img = Image.FromFile(ContentProjectManager.GetTexturePath(assetName)))
                {
                    var bmp = new Bitmap(img);
                    var data = new uint[bmp.Width * bmp.Height];
                    for (var y = 0; y < bmp.Height; ++y)
                    {
                        for (var x = 0; x < bmp.Width; ++x)
                        {
                            var pixel = bmp.GetPixel(x, y);
                            data[x + y * bmp.Width] =
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
            else if (typeof(T) == typeof(Effect))
            {
                if (_shaders.ContainsKey(assetName))
                {
                    return (T)(object)_shaders[assetName];
                }

                var shaderPath = ContentProjectManager.GetShaderPath(assetName);
                if (shaderPath != null)
                {
                    using (var file = File.OpenText(shaderPath))
                    {
                        var sourceCode = file.ReadToEnd();

                        var effectSource = new EffectContent
                        {
                            EffectCode = sourceCode,
                            Identity = new ContentIdentity {SourceFilename = assetName}
                        };
                        var processor = new EffectProcessor();
                        var compiledEffect = processor.Process(effectSource, new DummyProcessorContext());
                        var effect = new Effect(g.GraphicsDevice, compiledEffect.GetEffectCode());
                        _shaders.Add(assetName, effect);
                        return (T)(object)effect;
                    }
                }
            }
            else if (typeof(T) == typeof(ParticleEffect))
            {
                using (var xmlReader = XmlReader.Create(ContentProjectManager.GetEffectPath(assetName)))
                {
                    var effect = IntermediateSerializer.Deserialize<ParticleEffect>(xmlReader, null);
                    effect.Initialise();
                    effect.LoadContent(this);
                    return (T)(object)effect;
                }
            }
            return default(T);
        }

        class DummyProcessorContext : ContentProcessorContext
        {
            public override TargetPlatform TargetPlatform { get { return TargetPlatform.Windows; } }
            public override GraphicsProfile TargetProfile { get { return GraphicsProfile.Reach; } }
            public override string BuildConfiguration { get { return string.Empty; } }
            public override string IntermediateDirectory { get { return string.Empty; } }
            public override string OutputDirectory { get { return string.Empty; } }
            public override string OutputFilename { get { return string.Empty; } }

            public override OpaqueDataDictionary Parameters { get { return parameters; } }
            OpaqueDataDictionary parameters = new OpaqueDataDictionary();

            public override ContentBuildLogger Logger { get { return logger; } }
            ContentBuildLogger logger = new MyLogger();

            public override void AddDependency(string filename) { }
            public override void AddOutputFile(string filename) { }

            public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters) { throw new NotImplementedException(); }
            public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName) { throw new NotImplementedException(); }
            public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName, string assetName) { throw new NotImplementedException(); }
        }

        class MyLogger : ContentBuildLogger
        {
            public override void LogMessage(string message, params object[] messageArgs) { }
            public override void LogImportantMessage(string message, params object[] messageArgs) { }
            public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs) { }
        }
    }
}

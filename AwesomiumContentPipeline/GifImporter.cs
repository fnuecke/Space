using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Awesomium.ContentPipeline
{
    /// <summary>
    /// Importer that keeps files intact. Its only purpose is to allow treating
    /// files of any format, even if they are handled somewhere else (in our
    /// case image data for Awesomium), through XNAs content pipeline.
    /// </summary>
    [ContentImporter(".gif", ".jpg", ".jpeg", ".png", DisplayName = "Raw Data Importer", DefaultProcessor = "PassThroughProcessor")]
    public class GifImporter : ContentImporter<byte[]>
    {
        public override byte[] Import(string filename, ContentImporterContext context)
        {
            return File.ReadAllBytes(filename);
        }
    }
}

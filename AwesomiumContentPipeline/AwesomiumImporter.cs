using Microsoft.Xna.Framework.Content.Pipeline;

namespace Awesomium.ContentPipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to import a file from disk into the specified type, TImport.
    /// 
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentImporter(".xhtml", ".html", ".css", ".js", DefaultProcessor = "PassThroughProcessor", DisplayName = "HTML / CSS - Awesomium")]
    public class AwesomiumImporter : ContentImporter<string>
    {
        public override string Import(string filename, ContentImporterContext context)
        {
            return System.IO.File.ReadAllText(filename);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using ProjectMercury;
using Xap;

namespace Space.Tools.DataEditor
{
    internal static class ContentProjectManager
    {
        /// <summary>
        /// All texture assets known from referenced content projects, and
        /// the mapping of the asset names to the files on the file system.
        /// </summary>
        private static readonly Dictionary<string, string> TextureAssets = new Dictionary<string, string>();

        /// <summary>
        /// All particle effect assets known from referenced content projects.
        /// </summary>
        private static readonly Dictionary<string, string> EffectAssets = new Dictionary<string, string>();

        /// <summary>
        /// All shader assets known from referenced content projects.
        /// </summary>
        private static readonly Dictionary<string, string> ShaderAssets = new Dictionary<string, string>();

        /// <summary>
        /// All sound assets known from referenced content projects.
        /// </summary>
        private static readonly List<string> SoundAssets = new List<string>();

        /// <summary>
        /// Perform initial load when used.
        /// </summary>
        static ContentProjectManager()
        {
            Reload();
        }

        /// <summary>
        /// Gets an enumerator over all known texture asset names.
        /// </summary>
        public static IEnumerable<string> TextureAssetNames
        {
            get { return TextureAssets.Keys; }
        }
        /// <summary>
        /// Gets an enumerator over all known texture asset names.
        /// </summary>
        public static IEnumerable<string> SoundAssetNames
        {
            get { return SoundAssets.AsEnumerable(); }
        }
        /// <summary>
        /// Gets an enumerator over all known effect asset names.
        /// </summary>
        public static IEnumerable<string> EffectAssetNames
        {
            get { return EffectAssets.Keys; }
        }

        /// <summary>
        /// Reload all content projects defined in the settings.
        /// </summary>
        public static void Reload()
        {
            // Forget what we knew.
            TextureAssets.Clear();
            EffectAssets.Clear();
            ShaderAssets.Clear();
            SoundAssets.Clear();

            // Get "final" content root, as used after compilation. We strip this from content
            // project's individual root paths.
            var contentRoot = DataEditorSettings.Default.ContentRootDirectory.Replace('\\', '/');

            // Loop all set content projects.
            foreach (string projectPath in DataEditorSettingsProxy.Default.ContentProjects)
            {
                // Skip invalid entries.
                if (string.IsNullOrWhiteSpace(projectPath))
                {
                    continue;
                }

                try
                {
                    // Load the project's XML description.
                    var xml = XElement.Load(projectPath);

                    // Get the namespace. We could hardcode this, but eh...
                    var xmlns = xml.Attribute("xmlns");
                    XNamespace ns = xmlns != null ? xmlns.Value : "";

                    // Get relative root path for this content project. We need to prepend this to
                    // all our asset names.
                    if (!xml.Elements(ns + "PropertyGroup").Elements(ns + "ContentRootDirectory").Any())
                    {
                        continue;
                    }
                    var rootPath = xml.Elements(ns + "PropertyGroup").Elements(ns + "ContentRootDirectory").First().Value.Replace('\\', '/');

                    // Strip global root.
                    if (rootPath.StartsWith(contentRoot))
                    {
                        rootPath = rootPath.Substring(contentRoot.Length);
                        if (rootPath.Length > 0 && rootPath[0] == '/')
                        {
                            rootPath = rootPath.Substring(1);
                        }
                        if (rootPath.Length > 0)
                        {
                            rootPath += '/';
                        }
                    }

                    // Base path we prepend to asset file paths, being that to the project minus it's file name.
                    var basePath = projectPath.Contains('/') ? projectPath.Substring(0, projectPath.LastIndexOf('/') + 1) : (projectPath + '/');

                    // Find all usable assets in the content project.
                    foreach (var texture in from asset in xml.Elements(ns + "ItemGroup").Elements(ns + "Compile")
                                            where
                                                asset.Elements(ns + "Importer").Any() && asset.Elements(ns + "Importer").First().Value.Equals("TextureImporter") &&
                                                asset.Elements(ns + "Processor").Any() && asset.Elements(ns + "Processor").First().Value.Equals("TextureProcessor")
                                            select asset)
                    {
                        // Get path to asset on disk.
                        var include = texture.Attribute("Include");
                        if (include == null)
                        {
                            return;
                        }
                        var assetPath = include.Value.Replace('\\', '/');

                        // Extract the relative path, which we need to prepend to the asset name.
                        var relativeAssetPath = assetPath.Contains('/') ? assetPath.Substring(0, assetPath.LastIndexOf('/') + 1) : "";

                        // Prepend it with the base path.
                        assetPath = basePath + assetPath;

                        // Build our complete asset name.
                        if (!texture.Elements(ns + "Name").Any())
                        {
                            continue;
                        }
                        var assetName = rootPath + relativeAssetPath + texture.Elements(ns + "Name").First().Value;

                        // Store the asset in our lookup table.
                        TextureAssets.Add(assetName.Trim(), assetPath.Trim());
                    }

                    // Find all usable particle effect assets in the content project.
                    foreach (var effect in from asset in xml.Elements(ns + "ItemGroup").Elements(ns + "Compile")
                                            where
                                                asset.Elements(ns + "Importer").Any() && asset.Elements(ns + "Importer").First().Value.Equals("XmlImporter") &&
                                                asset.Elements(ns + "Processor").Any() && asset.Elements(ns + "Processor").First().Value.Equals("PassThroughProcessor")
                                            select asset)
                    {
                        // Get path to asset on disk.
                        var include = effect.Attribute("Include");
                        if (include == null)
                        {
                            return;
                        }
                        var assetPath = include.Value.Replace('\\', '/');

                        // Extract the relative path, which we need to prepend to the asset name.
                        var relativeAssetPath = assetPath.Contains('/') ? assetPath.Substring(0, assetPath.LastIndexOf('/') + 1) : "";

                        // Prepend it with the base path.
                        assetPath = basePath + assetPath;

                        // Take a peek, to make sure it's an effect asset.
                        try
                        {
                            using (var xmlReader = XmlReader.Create(assetPath))
                            {
                                IntermediateSerializer.Deserialize<ParticleEffect>(xmlReader, null);
                            }
                        }
                        catch
                        {
                            continue;
                        }

                        // Build our complete asset name.
                        if (!effect.Elements(ns + "Name").Any())
                        {
                            continue;
                        }
                        var assetName = rootPath + relativeAssetPath + effect.Elements(ns + "Name").First().Value;

                        // Store the asset in our lookup table.
                        EffectAssets.Add(assetName.Trim(), assetPath.Trim());
                    }

                    // Find all usable effect assets in the content project.
                    foreach (var shader in from asset in xml.Elements(ns + "ItemGroup").Elements(ns + "Compile")
                                            where
                                                asset.Elements(ns + "Importer").Any() && asset.Elements(ns + "Importer").First().Value.Equals("EffectImporter") &&
                                                asset.Elements(ns + "Processor").Any() && asset.Elements(ns + "Processor").First().Value.Equals("EffectProcessor")
                                            select asset)
                    {
                        // Get path to asset on disk.
                        var include = shader.Attribute("Include");
                        if (include == null)
                        {
                            return;
                        }
                        var assetPath = include.Value.Replace('\\', '/');

                        // Extract the relative path, which we need to prepend to the asset name.
                        var relativeAssetPath = assetPath.Contains('/') ? assetPath.Substring(0, assetPath.LastIndexOf('/') + 1) : "";

                        // Prepend it with the base path.
                        assetPath = basePath + assetPath;

                        // Build our complete asset name.
                        if (!shader.Elements(ns + "Name").Any())
                        {
                            continue;
                        }
                        var assetName = rootPath + relativeAssetPath + shader.Elements(ns + "Name").First().Value;

                        // Store the asset in our lookup table.
                        ShaderAssets.Add(assetName.Trim(), assetPath.Trim());
                    }

                    // Find all usable assets in the content project.
                    foreach (var sound in from asset in xml.Elements(ns + "ItemGroup").Elements(ns + "Compile")
                                            where
                                                asset.Elements(ns + "Importer").Any() && asset.Elements(ns + "Importer").First().Value.Equals("XactImporter") &&
                                                asset.Elements(ns + "Processor").Any() && asset.Elements(ns + "Processor").First().Value.Equals("XactProcessor")
                                            select asset)
                    {
                        // Get path to asset on disk.
                        var include = sound.Attribute("Include");
                        if (include == null)
                        {
                            return;
                        }
                        var assetPath = include.Value.Replace('\\', '/');

                        // Extract the relative path, which we need to prepend to the asset name.
                        var relativeAssetPath = assetPath.Contains('/') ? assetPath.Substring(0, assetPath.LastIndexOf('/') + 1) : "";

                        // Prepend it with the base path.
                        assetPath = basePath + assetPath;

                        
                        // Build our complete asset name.
                        if (!sound.Elements(ns + "Name").Any())
                        {
                            continue;
                        }
                        
                        var proj = new Project();
                        proj.Parse( File.ReadAllLines(assetPath.Trim()) );

                        foreach (var soundbank in proj.m_soundBanks)
                        {
                            foreach (var cue in soundbank.m_cues)
                            {
                                SoundAssets.Add(cue.m_name);
                            }
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show("Error loading content project " + projectPath + ":\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show("Error loading content project " + projectPath + ":\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (XmlException ex)
                {
                    MessageBox.Show("Error loading content project " + projectPath + ":\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Error loading content project " + projectPath + ":\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                }
            }
        }

        /// <summary>
        /// Try to resolve an asset name to a path pointing to the assets file on disk.
        /// </summary>
        /// <param name="assetName">The asset to look up.</param>
        /// <returns>The path to the asset's file.</returns>
        public static string GetTexturePath(string assetName)
        {
            string result;
            TextureAssets.TryGetValue((assetName ?? "").Replace('\\', '/'), out result);
            return result;
        }

        /// <summary>
        /// Determines whether a texture asset with the specified name is known.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <returns>
        ///   <c>true</c> if the texture asset is known; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasTextureAsset(string assetName)
        {
            return TextureAssets.ContainsKey((assetName ?? "").Replace('\\', '/'));
        }

        /// <summary>
        /// Determines whether a texture asset with the specified name is known.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <returns>
        ///   <c>true</c> if the texture asset is known; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasEffectAsset(string assetName)
        {
            return EffectAssets.ContainsKey((assetName ?? "").Replace('\\', '/'));
        }

        /// <summary>
        /// Determines whether a texture asset with the specified name is known.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <returns>
        ///   <c>true</c> if the texture asset is known; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasShaderAsset(string assetName)
        {
            return ShaderAssets.ContainsKey((assetName ?? "").Replace('\\', '/'));
        }

        /// <summary>
        /// Try to resolve an asset name to a path pointing to the assets file on disk.
        /// </summary>
        /// <param name="assetName">The asset to look up.</param>
        /// <returns>The path to the asset's file.</returns>
        public static string GetEffectPath(string assetName)
        {
            string result;
            EffectAssets.TryGetValue((assetName ?? "").Replace('\\', '/'), out result);
            return result;
        }

        /// <summary>
        /// Try to resolve an asset name to a path pointing to the assets file on disk.
        /// </summary>
        /// <param name="assetName">The asset to look up.</param>
        /// <returns>The path to the asset's file.</returns>
        public static string GetShaderPath(string assetName)
        {
            string result;
            ShaderAssets.TryGetValue((assetName ?? "").Replace('\\', '/'), out result);
            return result;
        }
    }
}

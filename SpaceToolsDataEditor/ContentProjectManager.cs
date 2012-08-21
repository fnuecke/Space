﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

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
        /// Reload all content projects defined in the settings.
        /// </summary>
        public static void Reload()
        {
            // Forget what we knew.
            TextureAssets.Clear();

            // Get "final" content root, as used after compilation. We strip this from content
            // project's individual root paths.
            var contentRoot = DataEditorSettings.Default.ContentRootDirectory.Replace('/', '\\');

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
                    var rootPath = xml.Elements(ns + "PropertyGroup").Elements(ns + "ContentRootDirectory").First().Value.Replace('/', '\\');

                    // Strip global root.
                    if (rootPath.StartsWith(contentRoot))
                    {
                        rootPath = rootPath.Substring(contentRoot.Length);
                        if (rootPath.Length > 0 && rootPath[0] == '\\')
                        {
                            rootPath = rootPath.Substring(1);
                        }
                        if (rootPath.Length > 0)
                        {
                            rootPath += '\\';
                        }
                    }

                    // Base path we prepend to asset file paths, being that to the project minus it's file name.
                    var basePath = projectPath.Contains('\\') ? projectPath.Substring(0, projectPath.LastIndexOf('\\') + 1) : (projectPath + '\\');

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
                        var assetPath = include.Value.Replace('/', '\\');

                        // Extract the relative path, which we need to prepend to the asset name.
                        var relativeAssetPath = assetPath.Contains('\\') ? assetPath.Substring(0, assetPath.LastIndexOf('\\') + 1) : "";

                        // Prepend it with the base path.
                        assetPath = basePath + assetPath;

                        // Build our complete asset name.
                        if (!texture.Elements(ns + "Name").Any())
                        {
                            continue;
                        }
                        var assetName = rootPath + relativeAssetPath + texture.Elements(ns + "Name").First().Value;

                        // Store the asset in our lookup table.
                        TextureAssets.Add(assetName, assetPath);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show(@"Error loading content project " + projectPath + @":\n" + ex, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show(@"Error loading content project " + projectPath + @":\n" + ex, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (XmlException ex)
                {
                    MessageBox.Show(@"Error loading content project " + projectPath + @":\n" + ex, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(@"Error loading content project " + projectPath + @":\n" + ex, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                }
            }
        }

        /// <summary>
        /// Try to resolve an asset name to a path pointing to the assets file on disk.
        /// </summary>
        /// <param name="assetName">The asset to look up.</param>
        /// <returns>The path to the asset's file.</returns>
        public static string GetFileForTextureAsset(string assetName)
        {
            string result;
            TextureAssets.TryGetValue(assetName.Replace('/', '\\'), out result);
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
            return TextureAssets.ContainsKey(assetName.Replace('/', '\\'));
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace Engine.Util
{
    /// <summary>
    /// Implements a localized content manager, that tries to load assets for
    /// its set culture, before falling back to an invariant version.
    /// </summary>
    public class LocalizedContentManager : ContentManager
    {
        #region Properties
        
        /// <summary>
        /// The culture used in this manager to determine the actual files to
        /// load when loading.
        /// </summary>
        public CultureInfo Culture { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Used for locking to support multi-threaded loading.
        /// </summary>
        private object _lock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new localized content manager for the specified culture.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="culture"></param>
        public LocalizedContentManager(IServiceProvider service)
            : base(service)
        {
            // Default to the invariant culture.
            this.Culture = CultureInfo.InvariantCulture;
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Loads an asset that has been processed by the Content Pipeline.
        /// </summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">Asset name, relative to the loader root
        /// directory, and not including the .xnb extension.</param>
        /// <returns>The loaded asset. Repeated calls to load the same asset
        /// will return the same object instance.</returns>
        public override T Load<T>(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }

            // Try the fully localized variant (xx-YY).
            var localizedAssetName = assetName + "." + Culture.Name;
            if (File.Exists(Path.Combine(RootDirectory, GetCleanPath(localizedAssetName) + ".xnb")))
            {
                lock (_lock)
                {
                    return base.Load<T>(localizedAssetName);
                }
            }

            // Try language only (xx).
            localizedAssetName = assetName + "." + Culture.TwoLetterISOLanguageName;
            if (File.Exists(Path.Combine(RootDirectory, GetCleanPath(localizedAssetName) + ".xnb")))
            {
                lock (_lock)
                {
                    return base.Load<T>(localizedAssetName);
                }
            }

            // Use invariant version (no language extension).
            lock (_lock)
            {
                return base.Load<T>(assetName);
            }
        }

        #endregion

        #region Copy-Pasta of non-public XNA stuff -.-

        protected static string GetCleanPath(string path)
        {
            path = path.Replace('/', '\\');
            path = path.Replace(@"\.\", @"\");
            while (path.StartsWith(@".\"))
            {
                path = path.Substring(@".\".Length);
            }
            while (path.EndsWith(@"\."))
            {
                if (path.Length > @"\.".Length)
                {
                    path = path.Substring(0, path.Length - @"\.".Length);
                }
                else
                {
                    path = @"\";
                }
            }
            int startIndex = 1;
            while (startIndex < path.Length)
            {
                startIndex = path.IndexOf(@"\..\", startIndex);
                if (startIndex < 0)
                {
                    break;
                }
                startIndex = CollapseParentDirectory(ref path, startIndex, @"\..\".Length);
            }
            if (path.EndsWith(@"\.."))
            {
                startIndex = path.Length - @"\..".Length;
                if (startIndex > 0)
                {
                    CollapseParentDirectory(ref path, startIndex, @"\..".Length);
                }
            }
            if (path == ".")
            {
                path = string.Empty;
            }
            return path;
        }

        private static int CollapseParentDirectory(ref string path, int position, int removeLength)
        {
            int startIndex = path.LastIndexOf('\\', position - 1) + 1;
            path = path.Remove(startIndex, (position - startIndex) + removeLength);
            return System.Math.Max(startIndex - 1, 1);
        }

        #endregion
    }
}

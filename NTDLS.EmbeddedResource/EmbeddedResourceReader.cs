using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NTDLS.EmbeddedResource
{
    /// <summary>
    /// Used to read EmbeddedResources from assemblies.
    /// </summary>
    public static class EmbeddedResourceReader
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        /// <summary>
        /// Strips the Byte Order Mark (BOM) from the beginning of a string if it exists.
        /// This is useful for ensuring that text loaded from embedded resources does not contain unexpected BOM characters,
        /// which can cause issues when we assume the text is in a specific encoding (like UTF-8) without BOM.
        /// </summary>
        /// <param name="text">The text from which to remove the BOM.</param>
        /// <returns>The text without the BOM if it was present; otherwise, the original text.</returns>
        private static string StripBom(string text)
        {
            // U+FEFF is the BOM character; only strip if it's literally that codepoint
            if (text.Length > 0 && text[0] == '\uFEFF')
                return text[1..];
            return text;
        }

        /// <summary>
        /// Loads the text content of an embedded resource from the specified resource path.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies for the resource and uses an internal cache
        /// to improve performance on repeated calls. If the resource is not found, an exception is thrown rather than
        /// returning null.</remarks>
        /// <param name="resourcePath">The path to the embedded resource to load. The path is case-insensitive and should use slash-separated</param>
        /// <param name="encoding"> Optional parameter to specify the encoding of the embedded resource. If not provided, UTF-8 encoding is used by default.</param>
        /// <returns>A string containing the text content of the embedded resource if found.</returns>
        /// <exception cref="Exception">Thrown if the embedded resource cannot be found at the specified path.</exception>
        public static string LoadText(string resourcePath, Encoding? encoding = null)
        {
            var bytes = LoadBytes(resourcePath);
            var text = (encoding ?? Encoding.UTF8).GetString(bytes);
            if(encoding == null)
            {
                text = StripBom(text);
            }
            return text;
        }

        /// <summary>
        /// Loads the text content of an embedded resource from the specified resource path then formats it using the provided parameters.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies for the resource and uses an internal cache
        /// to improve performance on repeated calls. If the resource is not found, an exception is thrown rather than
        /// returning null.</remarks>
        /// <param name="resourcePath">The path to the embedded resource to load. The path is case-insensitive and should use slash-separated</param>
        /// <param name="param">An array of objects to format the text content of the embedded resource. The formatting is performed using string.Format semantics.</param>
        /// <param name="encoding"> Optional parameter to specify the encoding of the embedded resource. If not provided, UTF-8 encoding is used by default.</param>
        /// <returns>A string containing the text content of the embedded resource if found.</returns>
        /// <exception cref="Exception">Thrown if the embedded resource cannot be found at the specified path.</exception>
        public static string Format(string resourcePath, object[] param, Encoding? encoding = null)
        {
            var bytes = LoadBytes(resourcePath);
            var text = (encoding ?? Encoding.UTF8).GetString(bytes);
            if(encoding == null)
            {
                text = StripBom(text);
            }

            return string.Format(text, param);
        }

        /// <summary>
        /// Retrieves the embedded resource as a byte array from the specified resource path.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies for the resource and uses an internal cache
        /// to improve performance on repeated calls. The resource path is normalized for lookup.</remarks>
        /// <param name="resourcePath">The path to the embedded resource to load. The path is case-insensitive and may use either '/' or '\' as
        /// separators.</param>
        /// <returns>A byte array containing the contents of the embedded resource. Returns the cached value if available.</returns>
        /// <exception cref="Exception">Thrown if the embedded resource cannot be found at the specified path.</exception>
        public static byte[] LoadBytes(string resourcePath)
        {
            string cacheKey = $":{resourcePath.ToLowerInvariant()}".Replace('.', ':').Replace('\\', ':').Replace('/', ':');

            if (_cache.Get(cacheKey) is byte[] cachedResourceBytes)
            {
                return cachedResourceBytes;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var resourceBytes = SearchAssembly(assembly, cacheKey, resourcePath);
                if (resourceBytes != null)
                {
                    return resourceBytes;
                }
            }

            throw new Exception($"The embedded resource could not be found after enumeration: '{resourcePath}'");
        }

        /// <summary>
        /// Enumerates the names of embedded resources located under the specified "directory" (a namespace/folder
        /// prefix, e.g. "TextFiles" or "TextFiles/SubFolder") whose file name matches the given wildcard pattern.
        /// </summary>
        /// <remarks>The method searches all loaded assemblies. Directory matching is case-insensitive and matches
        /// on the trailing segment(s) of the resource's namespace, so a partial (suffix) directory path is
        /// sufficient. The returned names are slash-separated paths that can be passed directly to <see
        /// cref="LoadBytes"/>, <see cref="LoadText"/>, or <see cref="Format"/>.</remarks>
        /// <param name="directoryPath">The "directory" (namespace prefix) to enumerate resources from, e.g. "TextFiles". Pass an empty string to
        /// match resources with no folder prefix.</param>
        /// <param name="searchPattern">A wildcard search pattern (supporting '*' and '?') to match against the resource file name. Defaults to "*"
        /// which matches all files.</param>
        /// <returns>A collection of matching resource paths, in slash-separated form.</returns>
        public static IEnumerable<string> EnumerateResourceNames(string directoryPath, string searchPattern = "*")
        {
            static string NormalizeDirectory(string path)
                => path.Replace('\\', '/').Trim('/').ToLowerInvariant();

            var normalizedDirectory = NormalizeDirectory(directoryPath);
            var regexPattern = "^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            var fileNameRegex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var results = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string[] resourceNames;
                try
                {
                    resourceNames = assembly.GetManifestResourceNames();
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                foreach (var rawResourceName in resourceNames)
                {
                    var friendlyPath = ConvertResourceNameToPath(rawResourceName);
                    var lastSlashIndex = friendlyPath.LastIndexOf('/');
                    var directoryPart = lastSlashIndex >= 0 ? friendlyPath[..lastSlashIndex] : string.Empty;
                    var fileNamePart = lastSlashIndex >= 0 ? friendlyPath[(lastSlashIndex + 1)..] : friendlyPath;

                    var normalizedDirectoryPart = NormalizeDirectory(directoryPart);

                    var directoryMatches = normalizedDirectory.Length == 0
                        ? normalizedDirectoryPart.Length == 0
                        : normalizedDirectoryPart.Equals(normalizedDirectory, StringComparison.InvariantCultureIgnoreCase)
                            || normalizedDirectoryPart.EndsWith('/' + normalizedDirectory, StringComparison.InvariantCultureIgnoreCase);

                    if (directoryMatches && fileNameRegex.IsMatch(fileNamePart))
                    {
                        results.Add(friendlyPath);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Converts a dotted embedded resource name (e.g. "MyProject.TextFiles.TestResource.txt") into a
        /// slash-separated friendly path (e.g. "MyProject/TextFiles/TestResource.txt"), treating the final
        /// dot-delimited segment as the file extension.
        /// </summary>
        private static string ConvertResourceNameToPath(string resourceName)
        {
            var lastDotIndex = resourceName.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                return resourceName;
            }

            var namePart = resourceName[..lastDotIndex].Replace('.', '/');
            var extensionPart = resourceName[(lastDotIndex + 1)..];

            return $"{namePart}.{extensionPart}";
        }

        /// <summary>
        /// Searches the given assembly for a file.
        /// </summary>
        private static byte[]? SearchAssembly(Assembly assembly, string resourceCacheKey, string resourceName)
        {
            var assemblyCacheKey = $"EmbeddedResources:SearchAssembly:{assembly.FullName}";

            var allResourceNames = _cache.Get(assemblyCacheKey) as List<string>;
            if (allResourceNames == null)
            {
                allResourceNames = assembly.GetManifestResourceNames().Select(o => $":{o}".Replace('.', ':')).ToList();
                _cache.Set(assemblyCacheKey, allResourceNames, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });
            }

            if (allResourceNames.Count > 0)
            {
                var resource = allResourceNames.Where(o => o.EndsWith(resourceCacheKey, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (resource.Count > 1)
                {
                    throw new Exception($"Ambiguous resource name: [{resourceName}].");
                }
                else if (resource.Count == 0)
                {
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(resource.Single().Replace(':', '.').Trim(['.']))
                    ?? throw new InvalidOperationException($"Resource not found: [{resourceName}].");

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] bytes = ms.ToArray();

                _cache.Set(resourceCacheKey, bytes, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });

                return bytes;
            }

            return null;
        }
    }
}

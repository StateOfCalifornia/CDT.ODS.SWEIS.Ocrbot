using CDT.OCRBot.Domain.Interfaces;

namespace CDT.OCRBot.Infrastructure.Pdf
{
    /// <summary>
    /// Provides O(1) tag lookup performance using indexed cache
    /// </summary>
    public class TagLookupCache
    {
        private readonly Dictionary<string, Dictionary<string, object>> _cache;
        private readonly IAppLogger _logger;

        /// <summary>
        /// Initializes a new tag lookup cache with the provided tags
        /// </summary>
        /// <param name="tags">List of tag dictionaries to index</param>
        /// <param name="logger">Logger instance</param>
        public TagLookupCache(List<Dictionary<string, object>> tags, IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new Dictionary<string, Dictionary<string, object>>();

            if (tags == null || tags.Count == 0)
            {
                _logger.LogDebug("TagLookupCache initialized with empty tags");
                return;
            }

            // Build index: "pageNumber|normalizedContent" -> tag
            int indexed = 0;
            foreach (var tag in tags)
            {
                if (tag == null)
                    continue;

                // Extract page number and content
                if (!tag.ContainsKey("page_number") || !tag.ContainsKey("content"))
                    continue;

                var pageNumber = tag["page_number"];
                var content = tag["content"];

                if (pageNumber == null || content == null)
                    continue;

                string contentStr = content.ToString() ?? string.Empty;
                string normalizedContent = NormalizeContent(contentStr);

                // Create composite key
                string key = $"{pageNumber}|{normalizedContent}";

                // Add to cache (handle duplicates by keeping first occurrence)
                if (!_cache.ContainsKey(key))
                {
                    _cache[key] = tag;
                    indexed++;
                }
            }

            _logger.LogDebug($"TagLookupCache initialized: indexed {indexed} tags out of {tags.Count} total");
        }

        /// <summary>
        /// Finds a tag matching the given page number and content
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="content">Content text</param>
        /// <returns>Matching tag dictionary or null if not found</returns>
        public Dictionary<string, object>? FindTag(int pageNumber, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            string normalizedContent = NormalizeContent(content);
            string key = $"{pageNumber}|{normalizedContent}";

            return _cache.TryGetValue(key, out var tag) ? tag : null;
        }

        /// <summary>
        /// Gets the total number of indexed tags
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Normalizes content for consistent matching
        /// </summary>
        private string NormalizeContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Normalize whitespace and case
            return string.Join(" ", content.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();
        }
    }
}




using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Projects;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Projects
{
    /// <summary>
    /// Service for handling source file type replacements
    /// </summary>
    public class SourceFileService : ISourceFileService
    {
        private readonly ITypeManager _typeManager;
        private readonly ILogger<SourceFileService> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeManager">Type manager for type resolution</param>
        /// <param name="logger">Logger</param>
        public SourceFileService(ITypeManager typeManager, ILogger<SourceFileService> logger)
        {
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public List<TypeReplacement> ProcessTypeReplacements(SourceFile sourceFile)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));

            if (string.IsNullOrEmpty(sourceFile.Content))
                return new List<TypeReplacement>();

            var result = ReplaceTypes(sourceFile.Content);
            sourceFile.ProcessedContent = result.processedContent;
            sourceFile.TypeReplacements = result.replacements;
            sourceFile.ProcessedLines = sourceFile.ProcessedContent.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            ).ToList();

            return result.replacements;
        }

        /// <inheritdoc/>
        public (string processedContent, List<TypeReplacement> replacements) ReplaceTypes(string content)
        {
            if (string.IsNullOrEmpty(content))
                return (content, new List<TypeReplacement>());

            var replacements = new List<TypeReplacement>();
            string processedContent = content;

            // Get all type mappings from TypeManager
            var typeMappings = _typeManager.GetAllTypeMappings();

            // Process each mapping
            foreach (var mapping in typeMappings)
            {
                string userType = mapping.Key;
                string baseType = mapping.Value.BaseType;

                // Use regex to find all occurrences with word boundaries
                var regex = new Regex($@"\b{userType}\b");
                var matches = regex.Matches(content);

                // Convert matches to list for processing
                var replacementsList = new List<TypeReplacement>();
                foreach (Match match in matches)
                {
                    // Calculate line and column information for original text
                    int lineNumber = GetLineNumberFromPosition(content, match.Index);
                    int lineStartPosition = GetLineStartPosition(content, lineNumber);
                    int startColumn = match.Index - lineStartPosition;
                    int endColumn = startColumn + match.Length;

                    replacementsList.Add(new TypeReplacement
                    {
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        LineNumber = lineNumber,
                        StartColumn = startColumn,
                        EndColumn = endColumn,
                        OriginalText = match.Value,
                        ReplacementText = baseType
                    });
                }

                // Sort by position in descending order to replace from bottom to top
                replacementsList.Sort((a, b) => b.StartPosition.CompareTo(a.StartPosition));

                // Apply replacements
                int positionShift = 0;
                foreach (var replacement in replacementsList)
                {
                    // Calculate new position after previous replacements
                    int newStartPos = replacement.StartPosition + positionShift;

                    // Remove old text and insert new text
                    processedContent = processedContent.Remove(newStartPos, replacement.OriginalText.Length)
                                               .Insert(newStartPos, replacement.ReplacementText);

                    // Calculate position shift
                    int replacementDiff = replacement.ReplacementText.Length - replacement.OriginalText.Length;
                    positionShift += replacementDiff;

                    // Calculate positions for the replacement text
                    replacement.ReplacedStartPosition = newStartPos;
                    replacement.ReplacedEndPosition = newStartPos + replacement.ReplacementText.Length;

                    // Update line and column for replaced text
                    replacement.ReplacedLineNumber = GetLineNumberFromPosition(processedContent, replacement.ReplacedStartPosition);
                    int replacedLineStartPos = GetLineStartPosition(processedContent, replacement.ReplacedLineNumber);
                    replacement.ReplacedStartColumn = replacement.ReplacedStartPosition - replacedLineStartPos;
                    replacement.ReplacedEndColumn = replacement.ReplacedStartColumn + replacement.ReplacementText.Length;

                    replacements.Add(replacement);
                }
            }

            return (processedContent, replacements);
        }

        /// <inheritdoc/>
        public List<TypeOccurrence> FindTypeOccurrences(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<TypeOccurrence>();

            var occurrences = new List<TypeOccurrence>();
            var typeMappings = _typeManager.GetAllTypeMappings();

            foreach (var mapping in typeMappings)
            {
                string userType = mapping.Key;
                string baseType = mapping.Value.BaseType;

                var regex = new Regex($@"\b{userType}\b");
                var matches = regex.Matches(content);

                foreach (Match match in matches)
                {
                    // Calculate line and column information
                    int lineNumber = GetLineNumberFromPosition(content, match.Index);
                    int lineStartPosition = GetLineStartPosition(content, lineNumber);
                    int startColumn = match.Index - lineStartPosition;

                    occurrences.Add(new TypeOccurrence
                    {
                        UserType = userType,
                        BaseType = baseType,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        LineNumber = lineNumber,
                        ColumnNumber = startColumn,
                        Context = ExtractContext(content, match.Index, 50)
                    });
                }
            }

            // Sort by position
            occurrences.Sort((a, b) => a.StartPosition.CompareTo(b.StartPosition));

            return occurrences;
        }

        /// <inheritdoc/>
        public string ApplyReplacements(string content, IEnumerable<TypeReplacement> replacements)
        {
            if (string.IsNullOrEmpty(content) || replacements == null || !replacements.Any())
                return content;

            string result = content;

            // Sort replacements from bottom to top to preserve positions
            var sortedReplacements = replacements.OrderByDescending(r => r.StartPosition).ToList();

            foreach (var replacement in sortedReplacements)
            {
                // Ensure positions are valid
                if (replacement.StartPosition >= 0 &&
                    replacement.StartPosition < result.Length &&
                    replacement.EndPosition <= result.Length &&
                    replacement.EndPosition > replacement.StartPosition)
                {
                    // Apply replacement
                    result = result.Remove(replacement.StartPosition, replacement.OriginalText.Length)
                                  .Insert(replacement.StartPosition, replacement.ReplacementText);
                }
                else
                {
                    _logger.LogWarning($"Invalid replacement position: {replacement.StartPosition}-{replacement.EndPosition} in content of length {result.Length}");
                }
            }

            return result;
        }

        /// <summary>
        /// Extract context around a position in content
        /// </summary>
        private string ExtractContext(string content, int position, int radius)
        {
            int start = Math.Max(0, position - radius);
            int end = Math.Min(content.Length, position + radius);
            int length = end - start;

            return content.Substring(start, length);
        }

        /// <summary>
        /// Get line number from position in content (1-based)
        /// </summary>
        private int GetLineNumberFromPosition(string content, int position)
        {
            if (string.IsNullOrEmpty(content) || position < 0 || position >= content.Length)
                return 1;

            string contentUpToPosition = content.Substring(0, position);
            return contentUpToPosition.Count(c => c == '\n') + 1;
        }

        /// <summary>
        /// Get the start position of a line (0-based)
        /// </summary>
        private int GetLineStartPosition(string content, int lineNumber)
        {
            if (string.IsNullOrEmpty(content) || lineNumber <= 1)
                return 0;

            int newlineCount = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    newlineCount++;
                    if (newlineCount == lineNumber - 1)
                        return i + 1;
                }
            }

            return 0;
        }
    }
}

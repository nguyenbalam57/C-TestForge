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
        private readonly string[] _newlineDelimiters = new[] { "\r\n", "\r", "\n" };

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

            _logger.LogDebug($"Processing type replacements for file: {sourceFile.FileName}");

            // Store original line positions for accurate line mapping
            var lineStartPositions = CalculateLineStartPositions(sourceFile.Content);

            var result = ReplaceTypes(sourceFile.Content, lineStartPositions);
            sourceFile.ProcessedContent = result.processedContent;
            sourceFile.TypeReplacements = result.replacements;

            // Recalculate processed lines carefully to ensure correct line breaks
            sourceFile.ProcessedLines = SplitIntoLines(sourceFile.ProcessedContent);

            _logger.LogDebug($"Completed replacements. Found {result.replacements.Count} type replacements");

            return result.replacements;
        }

        /// <inheritdoc/>
        public (string processedContent, List<TypeReplacement> replacements) ReplaceTypes(
            string content,
            List<int> lineStartPositions = null)
        {
            if (string.IsNullOrEmpty(content))
                return (content, new List<TypeReplacement>());

            // Calculate line start positions if not provided
            if (lineStartPositions == null)
            {
                lineStartPositions = CalculateLineStartPositions(content);
            }

            var replacements = new List<TypeReplacement>();
            string processedContent = content;

            // Get all type mappings from TypeManager
            var typeMappings = _typeManager.GetAllTypeMappings();
            _logger.LogDebug($"Found {typeMappings.Count} type mappings to process");

            // Build a combined dictionary of all replacements to avoid conflicts
            var allReplacements = new List<TypeReplacement>();

            // First, collect all replacements from all type mappings
            foreach (var mapping in typeMappings)
            {
                string userType = mapping.Key;
                string baseType = mapping.Value.BaseType;

                // Use regex to find all occurrences with word boundaries
                var regex = new Regex($@"\b{userType}\b");
                var matches = regex.Matches(content);

                _logger.LogDebug($"Found {matches.Count} occurrences of type '{userType}'");

                // Convert matches to replacements
                foreach (Match match in matches)
                {
                    // Calculate line and column information for original text
                    var (lineNumber, startColumn) = GetLineAndColumnFromPosition(
                        match.Index, lineStartPositions);
                    int endColumn = startColumn + match.Length;

                    var replacement = new TypeReplacement
                    {
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        LineNumber = lineNumber,
                        StartColumn = startColumn,
                        EndColumn = endColumn,
                        OriginalText = match.Value,
                        ReplacementText = baseType
                    };

                    allReplacements.Add(replacement);

                    _logger.LogTrace($"Type replacement: {replacement.OriginalText} -> {replacement.ReplacementText} " +
                        $"at line {replacement.LineNumber}, columns {replacement.StartColumn}-{replacement.EndColumn}");
                }
            }

            // Check for overlapping replacements and resolve conflicts
            allReplacements = ResolveReplacementConflicts(allReplacements);

            // Sort by position in descending order to replace from bottom to top
            allReplacements.Sort((a, b) => b.StartPosition.CompareTo(a.StartPosition));

            // Apply replacements
            var processedContentBuilder = new StringBuilder(processedContent);
            var processedLineStartPositions = new List<int>(lineStartPositions);

            foreach (var replacement in allReplacements)
            {
                try
                {
                    // Apply replacement
                    processedContentBuilder.Remove(
                        replacement.StartPosition,
                        replacement.OriginalText.Length);

                    processedContentBuilder.Insert(
                        replacement.StartPosition,
                        replacement.ReplacementText);

                    // Calculate position shift
                    int replacementDiff = replacement.ReplacementText.Length - replacement.OriginalText.Length;

                    // Update processed line start positions after this replacement point
                    UpdateLineStartPositionsAfterReplacement(
                        processedLineStartPositions,
                        replacement.StartPosition,
                        replacementDiff);

                    // Calculate positions for the replacement text
                    replacement.ReplacedStartPosition = replacement.StartPosition;
                    replacement.ReplacedEndPosition = replacement.StartPosition + replacement.ReplacementText.Length;

                    // Update line and column for replaced text
                    var (replacedLineNumber, replacedStartColumn) = GetLineAndColumnFromPosition(
                        replacement.ReplacedStartPosition, processedLineStartPositions);

                    replacement.ReplacedLineNumber = replacedLineNumber;
                    replacement.ReplacedStartColumn = replacedStartColumn;
                    replacement.ReplacedEndColumn = replacedStartColumn + replacement.ReplacementText.Length;

                    replacements.Add(replacement);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Error applying replacement {replacement.OriginalText} -> {replacement.ReplacementText} " +
                        $"at position {replacement.StartPosition}");
                }
            }

            // Verify the processed content
            string finalProcessedContent = processedContentBuilder.ToString();

            // Final validation check
            if (ValidateProcessedContent(content, finalProcessedContent, replacements))
            {
                _logger.LogDebug("Processed content validation successful");
            }
            else
            {
                _logger.LogWarning("Processed content validation failed - some replacements may be incorrect");
            }

            return (finalProcessedContent, replacements);
        }

        /// <inheritdoc/>
        public List<TypeOccurrence> FindTypeOccurrences(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<TypeOccurrence>();

            var lineStartPositions = CalculateLineStartPositions(content);
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
                    var (lineNumber, columnNumber) = GetLineAndColumnFromPosition(
                        match.Index, lineStartPositions);

                    occurrences.Add(new TypeOccurrence
                    {
                        UserType = userType,
                        BaseType = baseType,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        LineNumber = lineNumber,
                        ColumnNumber = columnNumber,
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

            // Use StringBuilder for better performance with multiple replacements
            var result = new StringBuilder(content);

            // Sort replacements from bottom to top to preserve positions
            var sortedReplacements = replacements.OrderByDescending(r => r.StartPosition).ToList();

            foreach (var replacement in sortedReplacements)
            {
                try
                {
                    // Ensure positions are valid
                    if (replacement.StartPosition >= 0 &&
                        replacement.StartPosition < result.Length &&
                        replacement.EndPosition <= result.Length &&
                        replacement.EndPosition > replacement.StartPosition)
                    {
                        // Apply replacement
                        result.Remove(
                            replacement.StartPosition,
                            replacement.OriginalText.Length);

                        result.Insert(
                            replacement.StartPosition,
                            replacement.ReplacementText);
                    }
                    else
                    {
                        _logger.LogWarning(
                            $"Invalid replacement position: {replacement.StartPosition}-{replacement.EndPosition} " +
                            $"in content of length {result.Length}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Error applying replacement at position {replacement.StartPosition}: {ex.Message}");
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Split content into lines properly handling all newline variants
        /// </summary>
        private List<string> SplitIntoLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            return content.Split(_newlineDelimiters, StringSplitOptions.None).ToList();
        }

        /// <summary>
        /// Calculate start positions for each line in content
        /// </summary>
        private List<int> CalculateLineStartPositions(string content)
        {
            var positions = new List<int> { 0 }; // First line starts at position 0

            if (string.IsNullOrEmpty(content))
                return positions;

            // Handle different newline types (\r\n, \r, \n)
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    positions.Add(i + 1);
                }
                else if (content[i] == '\r')
                {
                    // Check for \r\n sequence
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++; // Skip the \n part
                    }
                    positions.Add(i + 1);
                }
            }

            return positions;
        }

        /// <summary>
        /// Update line start positions after a replacement
        /// </summary>
        private void UpdateLineStartPositionsAfterReplacement(
            List<int> lineStartPositions,
            int replacementPosition,
            int shift)
        {
            // Update all line start positions that come after the replacement
            for (int i = 0; i < lineStartPositions.Count; i++)
            {
                if (lineStartPositions[i] > replacementPosition)
                {
                    lineStartPositions[i] += shift;
                }
            }
        }

        /// <summary>
        /// Get line and column from position using pre-calculated line start positions
        /// </summary>
        private (int lineNumber, int column) GetLineAndColumnFromPosition(
            int position,
            List<int> lineStartPositions)
        {
            // Find the last line start position that's less than or equal to the given position
            int lineIndex = lineStartPositions.FindLastIndex(pos => pos <= position);

            if (lineIndex < 0)
                return (1, 0);  // Default to first line, first column

            int lineNumber = lineIndex + 1;  // Line numbers are 1-based
            int column = position - lineStartPositions[lineIndex];

            return (lineNumber, column);
        }

        /// <summary>
        /// Resolve conflicts between overlapping replacements
        /// </summary>
        private List<TypeReplacement> ResolveReplacementConflicts(List<TypeReplacement> replacements)
        {
            if (replacements.Count <= 1)
                return replacements;

            // Sort by start position
            var sortedReplacements = replacements.OrderBy(r => r.StartPosition).ToList();
            var resolvedReplacements = new List<TypeReplacement>();

            for (int i = 0; i < sortedReplacements.Count; i++)
            {
                var current = sortedReplacements[i];
                bool hasConflict = false;

                // Check for conflicts with already resolved replacements
                foreach (var resolved in resolvedReplacements)
                {
                    // Check if current replacement overlaps with any resolved replacement
                    if (current.StartPosition < resolved.EndPosition &&
                        current.EndPosition > resolved.StartPosition)
                    {
                        hasConflict = true;
                        _logger.LogWarning(
                            $"Conflicting replacements: '{current.OriginalText}' at position {current.StartPosition} " +
                            $"conflicts with '{resolved.OriginalText}' at position {resolved.StartPosition}");
                        break;
                    }
                }

                if (!hasConflict)
                {
                    resolvedReplacements.Add(current);
                }
            }

            return resolvedReplacements;
        }

        /// <summary>
        /// Validate the processed content against the original with the applied replacements
        /// </summary>
        private bool ValidateProcessedContent(
            string originalContent,
            string processedContent,
            List<TypeReplacement> replacements)
        {
            // Apply the replacements one by one to a copy of the original content
            var validationBuilder = new StringBuilder(originalContent);

            // Sort by position in descending order (bottom to top)
            var sortedReplacements = replacements
                .OrderByDescending(r => r.StartPosition)
                .ToList();

            foreach (var replacement in sortedReplacements)
            {
                try
                {
                    if (replacement.StartPosition >= 0 &&
                        replacement.StartPosition < validationBuilder.Length &&
                        replacement.StartPosition + replacement.OriginalText.Length <= validationBuilder.Length)
                    {
                        validationBuilder.Remove(
                            replacement.StartPosition,
                            replacement.OriginalText.Length);

                        validationBuilder.Insert(
                            replacement.StartPosition,
                            replacement.ReplacementText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Validation error at position {replacement.StartPosition}: {ex.Message}");
                    return false;
                }
            }

            // Compare the validation result with the processed content
            string validationResult = validationBuilder.ToString();

            if (validationResult != processedContent)
            {
                _logger.LogError("Validation failed: processed content does not match expected result");
                // Log the first difference
                for (int i = 0; i < Math.Min(validationResult.Length, processedContent.Length); i++)
                {
                    if (validationResult[i] != processedContent[i])
                    {
                        _logger.LogError(
                            $"First difference at position {i}: " +
                            $"Expected '{validationResult[i]}', got '{processedContent[i]}'");
                        break;
                    }
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extract context around a position in content
        /// </summary>
        private string ExtractContext(string content, int position, int radius)
        {
            if (string.IsNullOrEmpty(content) || position < 0 || position >= content.Length)
                return string.Empty;

            int start = Math.Max(0, position - radius);
            int end = Math.Min(content.Length, position + radius);
            int length = end - start;

            return content.Substring(start, length);
        }
    }
}

using C_TestForge.Models.Base;
using C_TestForge.Models.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace C_TestForge.Models.Projects
{
    /// <summary>
    /// Represents a source file
    /// </summary>
    public class SourceFile : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Path to the source file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the source file
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Content of the source file
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Processed content after type replacements (added for use with SourceFileService)
        /// </summary>
        public string ProcessedContent { get; set; } = string.Empty;

        /// <summary>
        /// Lines of the source file
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary>
        /// Processed lines after type replacements (added for use with SourceFileService)
        /// </summary>
        public List<string> ProcessedLines { get; set; } = new List<string>();

        /// <summary>
        /// Hash of the content for change detection
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// Type of the source file
        /// </summary>
        public SourceFileType FileType { get; set; }

        /// <summary>
        /// Dictionary of includes in the source file
        /// </summary>
        public Dictionary<string, string> Includes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Last modified time of the file
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Parse result for this source file
        /// </summary>
        [JsonIgnore]
        public ParseResult ParseResult { get; set; } = new ParseResult();

        /// <summary>
        /// Whether the file has been modified since the last save
        /// </summary>
        [JsonIgnore]
        public bool IsDirty { get; set; }

        /// <summary>
        /// Type replacements (added for use with SourceFileService)
        /// </summary>
        public List<TypeReplacement> TypeReplacements { get; set; } = new List<TypeReplacement>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public SourceFile()
        {
            Id = Guid.NewGuid().ToString();
            Lines = new List<string>();
            ProcessedLines = new List<string>();
            Includes = new Dictionary<string, string>();
            TypeReplacements = new List<TypeReplacement>();
        }

        /// <summary>
        /// Get a string representation of the source file
        /// </summary>
        public override string ToString()
        {
            return $"{FileName} ({FileType})";
        }

        /// <summary>
        /// Update the content of the source file
        /// </summary>
        public void UpdateContent(string newContent)
        {
            Content = newContent;
            Lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            IsDirty = true;
        }

        /// <summary>
        /// Update a specific line in the source file
        /// </summary>
        public void UpdateLine(int lineNumber, string newContent)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber));
            }
            Lines[lineNumber - 1] = newContent;
            Content = string.Join(Environment.NewLine, Lines);
            IsDirty = true;
        }

        /// <summary>
        /// Create a clone of the source file
        /// </summary>
        public SourceFile Clone()
        {
            return new SourceFile
            {
                Id = Id,
                FilePath = FilePath,
                FileName = FileName,
                Content = Content,
                ProcessedContent = ProcessedContent,
                Lines = Lines != null ? new List<string>(Lines) : new List<string>(),
                ProcessedLines = ProcessedLines != null ? new List<string>(ProcessedLines) : new List<string>(),
                ContentHash = ContentHash,
                FileType = FileType,
                Includes = Includes != null ? new Dictionary<string, string>(Includes) : new Dictionary<string, string>(),
                LastModified = LastModified,
                IsDirty = IsDirty,
                TypeReplacements = TypeReplacements != null ?
                    TypeReplacements.Select(r => r.Clone()).ToList() : new List<TypeReplacement>()
            };
        }
    }

    /// <summary>
    /// Represents a type replacement in the source code
    /// </summary>
    public class TypeReplacement
    {
        /// <summary>
        /// Start position of the original text in the entire content
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// End position of the original text in the entire content
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Line number where the original text occurs (1-based)
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Start column in the line for original text (0-based)
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// End column in the line for original text (0-based)
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// Start position of the replacement text in the processed content
        /// </summary>
        public int ReplacedStartPosition { get; set; }

        /// <summary>
        /// End position of the replacement text in the processed content
        /// </summary>
        public int ReplacedEndPosition { get; set; }

        /// <summary>
        /// Line number where the replacement text occurs (1-based)
        /// </summary>
        public int ReplacedLineNumber { get; set; }

        /// <summary>
        /// Start column in the line for replacement text (0-based)
        /// </summary>
        public int ReplacedStartColumn { get; set; }

        /// <summary>
        /// End column in the line for replacement text (0-based)
        /// </summary>
        public int ReplacedEndColumn { get; set; }

        /// <summary>
        /// Original text before replacement
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// Replacement text
        /// </summary>
        public string ReplacementText { get; set; } = string.Empty;

        /// <summary>
        /// Clone the type replacement
        /// </summary>
        public TypeReplacement Clone()
        {
            return new TypeReplacement
            {
                StartPosition = StartPosition,
                EndPosition = EndPosition,
                LineNumber = LineNumber,
                StartColumn = StartColumn,
                EndColumn = EndColumn,
                ReplacedStartPosition = ReplacedStartPosition,
                ReplacedEndPosition = ReplacedEndPosition,
                ReplacedLineNumber = ReplacedLineNumber,
                ReplacedStartColumn = ReplacedStartColumn,
                ReplacedEndColumn = ReplacedEndColumn,
                OriginalText = OriginalText,
                ReplacementText = ReplacementText
            };
        }
    }

    /// <summary>
    /// Represents an occurrence of a type in source code
    /// </summary>
    public class TypeOccurrence
    {
        /// <summary>
        /// User-defined type (e.g., UINT32)
        /// </summary>
        public string UserType { get; set; } = string.Empty;

        /// <summary>
        /// Base type (e.g., unsigned int)
        /// </summary>
        public string BaseType { get; set; } = string.Empty;

        /// <summary>
        /// Start position in content
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// End position in content
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Line number (1-based)
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number (0-based)
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Context around the occurrence
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }
}
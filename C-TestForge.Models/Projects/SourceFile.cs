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
        /// Lines of the source file
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();

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
                Lines = Lines != null ? new List<string>(Lines) : new List<string>(),
                ContentHash = ContentHash,
                FileType = FileType,
                Includes = Includes != null ? new Dictionary<string, string>(Includes) : new Dictionary<string, string>(),
                LastModified = LastModified,
                IsDirty = IsDirty
            };
        }
    }
}
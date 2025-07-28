namespace C_TestForge.Models.CodeAnalysis.Coverage
{
    /// <summary>
    /// Represents an uncovered code area
    /// </summary>
    public class UncoveredCodeArea
    {
        /// <summary>
        /// Gets or sets the area type
        /// </summary>
        public UncoveredAreaType AreaType { get; set; }

        /// <summary>
        /// Gets or sets the line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the end line number (for multi-line areas)
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        public CodeCoverageSeverity Severity { get; set; }
    }
}
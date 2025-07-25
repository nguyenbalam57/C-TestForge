namespace C_TestForge.Models
{
    /// <summary>
    /// Stores complexity metrics for a function
    /// </summary>
    public class FunctionComplexity
    {
        /// <summary>
        /// Gets or sets the cyclomatic complexity
        /// </summary>
        public int CyclomaticComplexity { get; set; }

        /// <summary>
        /// Gets or sets the number of lines in the function
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Gets or sets the number of statements in the function
        /// </summary>
        public int StatementCount { get; set; }

        /// <summary>
        /// Gets or sets the nesting depth of the function
        /// </summary>
        public int NestingDepth { get; set; }

        /// <summary>
        /// Gets or sets the maintainability index
        /// </summary>
        public double MaintainabilityIndex { get; set; }

        /// <summary>
        /// Creates a deep copy of this complexity object
        /// </summary>
        /// <returns>A new FunctionComplexity instance with the same properties</returns>
        public FunctionComplexity Clone()
        {
            return new FunctionComplexity
            {
                CyclomaticComplexity = this.CyclomaticComplexity,
                LineCount = this.LineCount,
                StatementCount = this.StatementCount,
                NestingDepth = this.NestingDepth,
                MaintainabilityIndex = this.MaintainabilityIndex
            };
        }
    }
}

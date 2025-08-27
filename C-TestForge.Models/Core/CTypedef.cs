using C_TestForge.Models.Base;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a C typedef
    /// </summary>
    public class CTypedef : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// The original type that this typedef aliases
        /// </summary>
        public string OriginalType { get; set; } = string.Empty;

        /// <summary>
        /// The aliased name (new type name)
        /// </summary>
        public string AliasName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this typedef creates a pointer type
        /// </summary>
        public bool IsPointerType { get; set; }

        /// <summary>
        /// Whether this typedef creates an array type
        /// </summary>
        public bool IsArrayType { get; set; }

        /// <summary>
        /// Whether this typedef creates a function pointer type
        /// </summary>
        public bool IsFunctionPointer { get; set; }

        /// <summary>
        /// Function signature if this is a function pointer typedef
        /// </summary>
        public CFunctionSignature FunctionSignature { get; set; }

        /// <summary>
        /// Documentation for the typedef
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Usage count of this typedef
        /// </summary>
        public int UsageCount { get; set; }

        // ISymbol implementation
        string ISymbol.Type => "Typedef";

        public override string ToString()
        {
            return $"typedef {OriginalType} {Name}";
        }

        public CTypedef Clone()
        {
            return new CTypedef
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                OriginalType = OriginalType,
                AliasName = AliasName,
                IsPointerType = IsPointerType,
                IsArrayType = IsArrayType,
                IsFunctionPointer = IsFunctionPointer,
                FunctionSignature = FunctionSignature?.Clone(),
                Documentation = Documentation,
                UsageCount = UsageCount
            };
        }
    }
    /// <summary>
    /// Represents a constraint on a typedef
    /// </summary>
    public class TypedefConstraint
    {
        /// <summary>
        /// Name of the typedef this constraint applies to
        /// </summary>
        public string TypedefName { get; set; } = string.Empty;

        /// <summary>
        /// Type of constraint
        /// </summary>
        public ConstraintType Type { get; set; }

        /// <summary>
        /// Minimum value for range constraints
        /// </summary>
        public string MinValue { get; set; } = string.Empty;

        /// <summary>
        /// Maximum value for range constraints
        /// </summary>
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>
        /// Allowed values for enumeration constraints
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Custom constraint expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Source of the constraint (e.g., "Type constraint", "Comment analysis")
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes or description
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents usage analysis results for a typedef
    /// </summary>
    public class TypedefUsageAnalysis
    {
        /// <summary>
        /// Name of the typedef being analyzed
        /// </summary>
        public string TypedefName { get; set; } = string.Empty;

        /// <summary>
        /// Total usage count across all functions
        /// </summary>
        public int TotalUsageCount { get; set; }

        /// <summary>
        /// Functions that use this typedef
        /// </summary>
        public List<string> UsedByFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Functions that declare variables of this typedef
        /// </summary>
        public List<string> DeclarationFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Functions that pass this typedef as parameters
        /// </summary>
        public List<string> ParameterFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Functions that return this typedef
        /// </summary>
        public List<string> ReturnTypeFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Common usage patterns found
        /// </summary>
        public List<TypedefUsagePattern> UsagePatterns { get; set; } = new List<TypedefUsagePattern>();

        /// <summary>
        /// Analysis timestamp
        /// </summary>
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a usage pattern for a typedef
    /// </summary>
    public class TypedefUsagePattern
    {
        /// <summary>
        /// Type of usage pattern
        /// </summary>
        public TypedefUsageType UsageType { get; set; }

        /// <summary>
        /// Description of the pattern
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Frequency of this pattern
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Examples of this pattern
        /// </summary>
        public List<string> Examples { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents the resolution of a typedef chain
    /// </summary>
    public class TypedefResolution
    {
        /// <summary>
        /// Original typedef name
        /// </summary>
        public string OriginalTypedef { get; set; } = string.Empty;

        /// <summary>
        /// Ultimate underlying type
        /// </summary>
        public string UltimateType { get; set; } = string.Empty;

        /// <summary>
        /// Chain of typedefs leading to the ultimate type
        /// </summary>
        public List<string> ResolutionChain { get; set; } = new List<string>();

        /// <summary>
        /// Depth of the typedef chain
        /// </summary>
        public int ChainDepth { get; set; }

        /// <summary>
        /// Whether the resolution was successful
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Any issues encountered during resolution
        /// </summary>
        public string ResolutionError { get; set; } = string.Empty;

        /// <summary>
        /// Whether the ultimate type is a function pointer
        /// </summary>
        public bool IsFunctionPointer { get; set; }

        /// <summary>
        /// Whether the ultimate type is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Whether the ultimate type is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Whether the ultimate type is a primitive type
        /// </summary>
        public bool IsPrimitive { get; set; }

        /// <summary>
        /// Whether the ultimate type is a custom/user-defined type
        /// </summary>
        public bool IsCustomType { get; set; }
    }

    /// <summary>
    /// Represents a validation issue with a typedef
    /// </summary>
    public class TypedefValidationIssue
    {
        /// <summary>
        /// Name of the typedef with the issue
        /// </summary>
        public string TypedefName { get; set; } = string.Empty;

        /// <summary>
        /// Type of validation issue
        /// </summary>
        public TypedefIssueType IssueType { get; set; }

        /// <summary>
        /// Severity of the issue
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Suggested fix for the issue
        /// </summary>
        public string SuggestedFix { get; set; } = string.Empty;

        /// <summary>
        /// Location information
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Line number where the issue occurs
        /// </summary>
        public int LineNumber { get; set; }
    }

}

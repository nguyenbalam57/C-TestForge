using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using C_TestForge.Models.Core;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Result of parsing a C source file with comprehensive analysis data
    /// </summary>
    public class ParseResult
    {
        #region Basic Parse Data

        /// <summary>
        /// List of preprocessor definitions found in the source file
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of variables found in the source file
        /// </summary>
        public List<CVariable> Variables { get; set; } = new List<CVariable>();

        /// <summary>
        /// List of functions found in the source file
        /// </summary>
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        /// <summary>
        /// List of structures found in the source file
        /// </summary>
        public List<CStruct> Structures { get; set; } = new List<CStruct>();

        /// <summary>
        /// List of unions found in the source file
        /// </summary>
        public List<CUnion> Unions { get; set; } = new List<CUnion>();

        /// <summary>
        /// List of enumerations found in the source file
        /// </summary>
        public List<CEnum> Enumerations { get; set; } = new List<CEnum>();

        /// <summary>
        /// List of typedefs found in the source file
        /// </summary>
        public List<CTypedef> Typedefs { get; set; } = new List<CTypedef>();

        /// <summary>
        /// List of conditional directives found in the source file
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of include directives found in the source file
        /// </summary>
        public List<CInclude> Includes { get; set; } = new List<CInclude>();

        /// <summary>
        /// List of global constants found in the source file
        /// </summary>
        public List<CConstant> Constants { get; set; } = new List<CConstant>();

        #endregion

        #region Relationships and Dependencies

        /// <summary>
        /// List of function relationships found in the analysis
        /// </summary>
        public List<FunctionRelationship> FunctionRelationships { get; set; } = new List<FunctionRelationship>();

        /// <summary>
        /// List of variable constraints found in the analysis
        /// </summary>
        public List<VariableConstraint> VariableConstraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// List of type dependencies between structures, unions, and typedefs
        /// </summary>
        public List<TypeDependency> TypeDependencies { get; set; } = new List<TypeDependency>();

        /// <summary>
        /// List of symbol references found in the source
        /// </summary>
        public List<SymbolReference> SymbolReferences { get; set; } = new List<SymbolReference>();

        /// <summary>
        /// Call graph representing function call relationships
        /// </summary>
        public CallGraph CallGraph { get; set; } = new CallGraph();

        #endregion

        #region Parse Status and Metrics

        /// <summary>
        /// List of errors that occurred during parsing
        /// </summary>
        public List<ParseError> ParseErrors { get; set; } = new List<ParseError>();

        /// <summary>
        /// List of warnings generated during parsing
        /// </summary>
        public List<ParseWarning> ParseWarnings { get; set; } = new List<ParseWarning>();

        /// <summary>
        /// Parse start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Parse end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total parse duration
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Whether parsing completed successfully
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Whether the parse result is complete
        /// </summary>
        public bool IsComplete { get; set; } = true;

        /// <summary>
        /// Source file that was parsed
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Hash of the parsed content for validation
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// Version of the parser used
        /// </summary>
        public string ParserVersion { get; set; } = "1.0";

        /// <summary>
        /// Clang version used for parsing
        /// </summary>
        public string ClangVersion { get; set; } = string.Empty;

        #endregion

        #region Parse Statistics

        /// <summary>
        /// Statistics about the parsing process
        /// </summary>
        public ParseStatistics Statistics { get; set; } = new ParseStatistics();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets whether the parse result contains critical errors
        /// </summary>
        [JsonIgnore]
        public bool HasCriticalErrors => ParseErrors.Any(e => e.Severity == ErrorSeverity.Critical);

        /// <summary>
        /// Gets whether the parse result contains any errors
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => ParseErrors.Any(e => e.Severity >= ErrorSeverity.Error);

        /// <summary>
        /// Gets whether the parse result contains any warnings
        /// </summary>
        [JsonIgnore]
        public bool HasWarnings => ParseErrors.Any(e => e.Severity == ErrorSeverity.Warning) || ParseWarnings.Count > 0;

        /// <summary>
        /// Gets total count of all parsed elements
        /// </summary>
        [JsonIgnore]
        public int TotalElementCount => Functions.Count + Variables.Count + Structures.Count +
                                       Unions.Count + Enumerations.Count + Typedefs.Count +
                                       Definitions.Count + Constants.Count;

        /// <summary>
        /// Gets all global symbols (functions, variables, constants)
        /// </summary>
        [JsonIgnore]
        public List<ISymbol> GlobalSymbols
        {
            get
            {
                var symbols = new List<ISymbol>();
                symbols.AddRange(Functions.Cast<ISymbol>());
                symbols.AddRange(Variables.Where(v => v.IsGlobal).Cast<ISymbol>());
                symbols.AddRange(Constants.Cast<ISymbol>());
                return symbols.OrderBy(s => s.Name).ToList();
            }
        }

        /// <summary>
        /// Gets complexity metrics for the parsed code
        /// </summary>
        [JsonIgnore]
        public CodeComplexity ComplexityMetrics
        {
            get
            {
                return new CodeComplexity
                {
                    CyclomaticComplexity = Functions.Sum(f => f.CyclomaticComplexity),
                    TotalFunctions = Functions.Count,
                    TotalVariables = Variables.Count,
                    TotalStructures = Structures.Count,
                    AverageFunctionLength = Functions.Count > 0 ? Functions.Average(f => f.LineCount) : 0,
                    MaxFunctionComplexity = Functions.Count > 0 ? Functions.Max(f => f.CyclomaticComplexity) : 0
                };
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get variable by name
        /// </summary>
        public CVariable GetVariable(string name)
        {
            return Variables.FirstOrDefault(v => v.Name == name);
        }

        /// <summary>
        /// Get function by name
        /// </summary>
        public CFunction GetFunction(string name)
        {
            return Functions.FirstOrDefault(f => f.Name == name);
        }

        /// <summary>
        /// Get definition by name
        /// </summary>
        public CDefinition GetDefinition(string name)
        {
            return Definitions.FirstOrDefault(d => d.Name == name);
        }

        /// <summary>
        /// Get structure by name
        /// </summary>
        public CStruct GetStructure(string name)
        {
            return Structures.FirstOrDefault(s => s.Name == name);
        }

        /// <summary>
        /// Get union by name
        /// </summary>
        public CUnion GetUnion(string name)
        {
            return Unions.FirstOrDefault(u => u.Name == name);
        }

        /// <summary>
        /// Get enumeration by name
        /// </summary>
        public CEnum GetEnumeration(string name)
        {
            return Enumerations.FirstOrDefault(e => e.Name == name);
        }

        /// <summary>
        /// Get typedef by name
        /// </summary>
        public CTypedef GetTypedef(string name)
        {
            return Typedefs.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Get constant by name
        /// </summary>
        public CConstant GetConstant(string name)
        {
            return Constants.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Get functions that call the specified function
        /// </summary>
        public List<CFunction> GetCallers(string functionName)
        {
            var callerNames = FunctionRelationships
                .Where(r => r.CalleeName == functionName)
                .Select(r => r.CallerName)
                .ToList();

            return Functions
                .Where(f => callerNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Get functions called by the specified function
        /// </summary>
        public List<CFunction> GetCallees(string functionName)
        {
            var calleeNames = FunctionRelationships
                .Where(r => r.CallerName == functionName)
                .Select(r => r.CalleeName)
                .ToList();

            return Functions
                .Where(f => calleeNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Get all symbols by name pattern
        /// </summary>
        public List<ISymbol> FindSymbols(string pattern, bool useRegex = false)
        {
            var allSymbols = new List<ISymbol>();
            allSymbols.AddRange(Functions.Cast<ISymbol>());
            allSymbols.AddRange(Variables.Cast<ISymbol>());
            allSymbols.AddRange(Constants.Cast<ISymbol>());
            allSymbols.AddRange(Structures.Cast<ISymbol>());
            allSymbols.AddRange(Unions.Cast<ISymbol>());
            allSymbols.AddRange(Enumerations.Cast<ISymbol>());
            allSymbols.AddRange(Typedefs.Cast<ISymbol>());

            if (useRegex)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return allSymbols.Where(s => regex.IsMatch(s.Name)).ToList();
                }
                catch
                {
                    // Fall back to simple string matching
                    return allSymbols.Where(s => s.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return allSymbols.Where(s => s.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Get all functions that use a specific variable
        /// </summary>
        public List<CFunction> GetFunctionsUsingVariable(string variableName)
        {
            return Functions
                .Where(f => f.UsedVariables != null && f.UsedVariables.Contains(variableName))
                .ToList();
        }

        /// <summary>
        /// Get all variables used within a specific function
        /// </summary>
        public List<CVariable> GetVariablesUsedByFunction(string functionName)
        {
            var function = GetFunction(functionName);
            if (function?.UsedVariables == null)
                return new List<CVariable>();

            return Variables
                .Where(v => function.UsedVariables.Contains(v.Name))
                .ToList();
        }

        #endregion

        #region Merge and Transform Methods

        /// <summary>
        /// Merges another parse result into this one
        /// </summary>
        public void Merge(ParseResult other)
        {
            if (other == null)
                return;

            // Merge collections
            Definitions.AddRange(other.Definitions);
            Variables.AddRange(other.Variables);
            Functions.AddRange(other.Functions);
            Structures.AddRange(other.Structures);
            Unions.AddRange(other.Unions);
            Enumerations.AddRange(other.Enumerations);
            Typedefs.AddRange(other.Typedefs);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            Includes.AddRange(other.Includes);
            Constants.AddRange(other.Constants);
            FunctionRelationships.AddRange(other.FunctionRelationships);
            ParseErrors.AddRange(other.ParseErrors);
            ParseWarnings.AddRange(other.ParseWarnings);
            VariableConstraints.AddRange(other.VariableConstraints);
            TypeDependencies.AddRange(other.TypeDependencies);
            SymbolReferences.AddRange(other.SymbolReferences);

            // Merge statistics
            Statistics.Merge(other.Statistics);

            // Update flags
            IsSuccess = IsSuccess && other.IsSuccess;
            IsComplete = IsComplete && other.IsComplete;
        }

        /// <summary>
        /// Create a filtered copy containing only specified element types
        /// </summary>
        public ParseResult Filter(ParseElementFilter filter)
        {
            var filtered = new ParseResult
            {
                StartTime = StartTime,
                EndTime = EndTime,
                SourceFilePath = SourceFilePath,
                ContentHash = ContentHash,
                ParserVersion = ParserVersion,
                ClangVersion = ClangVersion,
                IsSuccess = IsSuccess,
                IsComplete = IsComplete
            };

            if (filter.IncludeFunctions) filtered.Functions.AddRange(Functions);
            if (filter.IncludeVariables) filtered.Variables.AddRange(Variables);
            if (filter.IncludeStructures) filtered.Structures.AddRange(Structures);
            if (filter.IncludeUnions) filtered.Unions.AddRange(Unions);
            if (filter.IncludeEnums) filtered.Enumerations.AddRange(Enumerations);
            if (filter.IncludeTypedefs) filtered.Typedefs.AddRange(Typedefs);
            if (filter.IncludeDefinitions) filtered.Definitions.AddRange(Definitions);
            if (filter.IncludeConstants) filtered.Constants.AddRange(Constants);
            if (filter.IncludeErrors) filtered.ParseErrors.AddRange(ParseErrors);
            if (filter.IncludeWarnings) filtered.ParseWarnings.AddRange(ParseWarnings);

            return filtered;
        }

        /// <summary>
        /// Validate the consistency of the parse result
        /// </summary>
        public List<string> Validate()
        {
            var issues = new List<string>();

            // Check for duplicate names in same scope
            var functionNames = Functions.Select(f => f.Name).ToList();
            var duplicateFunctions = functionNames.GroupBy(n => n).Where(g => g.Count() > 1);
            foreach (var dup in duplicateFunctions)
            {
                issues.Add($"Duplicate function name: {dup.Key}");
            }

            // Check for unresolved references
            foreach (var reference in SymbolReferences)
            {
                if (!reference.IsResolved)
                {
                    issues.Add($"Unresolved symbol reference: {reference.Name} at line {reference.LineNumber}");
                }
            }

            // Check function call relationships consistency
            foreach (var relationship in FunctionRelationships)
            {
                if (GetFunction(relationship.CallerName) == null)
                {
                    issues.Add($"Function relationship references unknown caller: {relationship.CallerName}");
                }
                if (GetFunction(relationship.CalleeName) == null)
                {
                    issues.Add($"Function relationship references unknown callee: {relationship.CalleeName}");
                }
            }

            return issues;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParseResult()
        {
            StartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with source file path
        /// </summary>
        public ParseResult(string sourceFilePath) : this()
        {
            SourceFilePath = sourceFilePath;
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// String representation of parse result
        /// </summary>
        public override string ToString()
        {
            var status = IsSuccess ? "Success" : "Failed";
            var elements = TotalElementCount;
            var errors = ParseErrors.Count;
            var warnings = ParseWarnings.Count;

            return $"ParseResult: {status} - {elements} elements, {errors} errors, {warnings} warnings";
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Filter options for creating filtered parse results
    /// </summary>
    public class ParseElementFilter
    {
        public bool IncludeFunctions { get; set; } = true;
        public bool IncludeVariables { get; set; } = true;
        public bool IncludeStructures { get; set; } = true;
        public bool IncludeUnions { get; set; } = true;
        public bool IncludeEnums { get; set; } = true;
        public bool IncludeTypedefs { get; set; } = true;
        public bool IncludeDefinitions { get; set; } = true;
        public bool IncludeConstants { get; set; } = true;
        public bool IncludeErrors { get; set; } = true;
        public bool IncludeWarnings { get; set; } = true;
    }

    /// <summary>
    /// Parse statistics
    /// </summary>
    public class ParseStatistics
    {
        public int LinesProcessed { get; set; }
        public int TokensProcessed { get; set; }
        public int ASTNodesCreated { get; set; }
        public int SymbolsResolved { get; set; }
        public int SymbolsUnresolved { get; set; }
        public long MemoryUsed { get; set; }
        public TimeSpan PreprocessingTime { get; set; }
        public TimeSpan ParsingTime { get; set; }
        public TimeSpan AnalysisTime { get; set; }

        public void Merge(ParseStatistics other)
        {
            if (other == null) return;

            LinesProcessed += other.LinesProcessed;
            TokensProcessed += other.TokensProcessed;
            ASTNodesCreated += other.ASTNodesCreated;
            SymbolsResolved += other.SymbolsResolved;
            SymbolsUnresolved += other.SymbolsUnresolved;
            MemoryUsed += other.MemoryUsed;
            PreprocessingTime += other.PreprocessingTime;
            ParsingTime += other.ParsingTime;
            AnalysisTime += other.AnalysisTime;
        }
    }

    /// <summary>
    /// Code complexity metrics
    /// </summary>
    public class CodeComplexity
    {
        public int CyclomaticComplexity { get; set; }
        public int TotalFunctions { get; set; }
        public int TotalVariables { get; set; }
        public int TotalStructures { get; set; }
        public double AverageFunctionLength { get; set; }
        public int MaxFunctionComplexity { get; set; }
        public double ComplexityDensity => TotalFunctions > 0 ? (double)CyclomaticComplexity / TotalFunctions : 0;
    }

    /// <summary>
    /// Parse warning information
    /// </summary>
    public class ParseWarning
    {
        public string Message { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Type dependency information
    /// </summary>
    public class TypeDependency
    {
        public string DependentType { get; set; } = string.Empty;
        public string DependsOnType { get; set; } = string.Empty;
        public DependencyType Type { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Symbol reference information
    /// </summary>
    public class SymbolReference
    {
        public string Name { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public SymbolType Type { get; set; }
        public bool IsResolved { get; set; }
        public string ResolvedSymbol { get; set; } = string.Empty;
    }

    /// <summary>
    /// Call graph representation
    /// </summary>
    public class CallGraph
    {
        public List<CallGraphNode> Nodes { get; set; } = new List<CallGraphNode>();
        public List<CallGraphEdge> Edges { get; set; } = new List<CallGraphEdge>();

        public CallGraphNode GetNode(string functionName)
        {
            return Nodes.FirstOrDefault(n => n.FunctionName == functionName);
        }

        public List<string> GetCallChain(string from, string to)
        {
            // Implementation would use graph traversal to find call chain
            return new List<string>();
        }
    }

    /// <summary>
    /// Call graph node
    /// </summary>
    public class CallGraphNode
    {
        public string FunctionName { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public bool IsEntryPoint { get; set; }
        public bool IsLeaf { get; set; }
    }

    /// <summary>
    /// Call graph edge
    /// </summary>
    public class CallGraphEdge
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public List<int> CallLines { get; set; } = new List<int>();
    }

    /// <summary>
    /// Dependency type enumeration
    /// </summary>
    public enum DependencyType
    {
        Inheritance,
        Composition,
        Association,
        Usage,
        TypeDefinition
    }

    /// <summary>
    /// Symbol type enumeration
    /// </summary>
    public enum SymbolType
    {
        Function,
        Variable,
        Type,
        Macro,
        Constant
    }

    /// <summary>
    /// Generic symbol interface
    /// </summary>
    public interface ISymbol
    {
        string Name { get; }
        int LineNumber { get; }
        int ColumnNumber { get; }
        string Type { get; }
    }

    #endregion
}
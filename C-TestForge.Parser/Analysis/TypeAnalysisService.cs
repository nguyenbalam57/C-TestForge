using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Analysis
{
    // <summary>
    /// Implementation of the typedef analysis service
    /// </summary>
    public class TypeAnalysisService : ITypeAnalysisService
    {
        private readonly ILogger<TypeAnalysisService> _logger;
        private readonly ITypeManager _typeManager;

        /// <summary>
        /// Constructor for TypeAnalysisService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="typeManager">Type manager for type resolution</param>
        public TypeAnalysisService(
            ILogger<TypeAnalysisService> logger,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractTypedef(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_TypedefDecl)
                {
                    return;
                }

                string typedefName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting typedef: {typedefName}");

                // Get typedef location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Get the underlying type
                var underlyingType = cursor.TypedefDeclUnderlyingType;
                string originalTypeName = underlyingType.Spelling.ToString();

                // Analyze the underlying type
                bool isPointerType = IsPointerType(underlyingType);
                bool isArrayType = IsArrayType(underlyingType);
                bool isFunctionPointer = IsFunctionPointerType(underlyingType);

                // Extract function signature if it's a function pointer
                CFunctionSignature functionSignature = null;
                if (isFunctionPointer)
                {
                    functionSignature = ExtractFunctionSignature(underlyingType);
                }

                // Extract documentation from comments
                string documentation = ExtractDocumentation(cursor);

                // Create typedef object
                var typedef = new CTypedef
                {
                    Name = typedefName,
                    AliasName = typedefName, // In C, typedef name is the alias
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    OriginalType = CleanTypeName(originalTypeName),
                    IsPointerType = isPointerType,
                    IsArrayType = isArrayType,
                    IsFunctionPointer = isFunctionPointer,
                    FunctionSignature = functionSignature,
                    Documentation = documentation,
                    UsageCount = 0 // Will be calculated later
                };

                // Add to result
                result.Typedefs.Add(typedef);

                _logger.LogDebug($"Extracted typedef: {typedefName} -> {originalTypeName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting typedef: {ex.Message}");
                return;
            }
        }

        /// <inheritdoc/>
        public async Task<List<TypedefConstraint>> AnalyzeTypedefsAsync(
            List<CTypedef> typedefs,
            List<CFunction> functions,
            List<CStruct> structures,
            List<CEnum> enumerations)
        {
            _logger.LogInformation($"Analyzing {typedefs.Count} typedefs");

            var constraints = new List<TypedefConstraint>();

            try
            {
                // Analyze each typedef
                foreach (var typedef in typedefs)
                {
                    _logger.LogDebug($"Analyzing typedef: {typedef.Name}");

                    // Add constraints based on underlying type
                    var typeConstraints = ExtractUnderlyingTypeConstraints(typedef);
                    constraints.AddRange(typeConstraints);

                    // Calculate usage count
                    typedef.UsageCount = CalculateUsageCount(typedef, functions);

                    // Analyze function pointer constraints
                    if (typedef.IsFunctionPointer && typedef.FunctionSignature != null)
                    {
                        var functionConstraints = ExtractFunctionPointerConstraints(typedef);
                        constraints.AddRange(functionConstraints);
                    }

                    // Look for related structures/enums
                    var relationshipConstraints = ExtractRelationshipConstraints(typedef, structures, enumerations);
                    constraints.AddRange(relationshipConstraints);
                }

                // Analyze typedef chains and dependencies
                foreach (var typedef in typedefs)
                {
                    var chainConstraints = AnalyzeTypedefChain(typedef, typedefs);
                    constraints.AddRange(chainConstraints);
                }

                _logger.LogInformation($"Extracted {constraints.Count} typedef constraints");
                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing typedefs: {ex.Message}");
                return constraints;
            }
        }

        /// <inheritdoc/>
        public async Task<List<TypedefConstraint>> ExtractConstraintsAsync(CTypedef typedef, SourceFile sourceFile)
        {
            _logger.LogInformation($"Extracting constraints for typedef {typedef.Name}");

            var constraints = new List<TypedefConstraint>();

            try
            {
                // Add basic type constraints
                constraints.AddRange(ExtractUnderlyingTypeConstraints(typedef));

                // Extract constraints from source code comments
                var commentConstraints = await ExtractConstraintsFromCommentsAsync(typedef, sourceFile);
                constraints.AddRange(commentConstraints);

                // Extract constraints from usage patterns
                var patternConstraints = await ExtractConstraintsFromPatternsAsync(typedef, sourceFile);
                constraints.AddRange(patternConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for typedef {typedef.Name}");

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for typedef {typedef.Name}: {ex.Message}");
                return new List<TypedefConstraint>();
            }
        }

        /// <inheritdoc/>
        public async Task<TypedefUsageAnalysis> AnalyzeTypedefUsageAsync(CTypedef typedef, List<CFunction> functions)
        {
            _logger.LogInformation($"Analyzing usage for typedef {typedef.Name}");

            var analysis = new TypedefUsageAnalysis
            {
                TypedefName = typedef.Name
            };

            try
            {
                foreach (var function in functions)
                {
                    // Check if function uses this typedef in parameters
                    if (function.Parameters != null)
                    {
                        foreach (var param in function.Parameters)
                        {
                            if (IsTypedefUsed(param.ParameterType, typedef.Name))
                            {
                                if (!analysis.ParameterFunctions.Contains(function.Name))
                                {
                                    analysis.ParameterFunctions.Add(function.Name);
                                    analysis.UsedByFunctions.Add(function.Name);
                                }
                                analysis.TotalUsageCount++;
                            }
                        }
                    }

                    // Check if function returns this typedef
                    if (IsTypedefUsed(function.ReturnType, typedef.Name))
                    {
                        if (!analysis.ReturnTypeFunctions.Contains(function.Name))
                        {
                            analysis.ReturnTypeFunctions.Add(function.Name);
                            analysis.UsedByFunctions.Add(function.Name);
                        }
                        analysis.TotalUsageCount++;
                    }

                    // Check if function declares variables of this typedef
                    if (function.LocalVariables != null)
                    {
                        foreach (var variable in function.LocalVariables)
                        {
                            if (IsTypedefUsed(variable.TypeName, typedef.Name))
                            {
                                if (!analysis.DeclarationFunctions.Contains(function.Name))
                                {
                                    analysis.DeclarationFunctions.Add(function.Name);
                                    analysis.UsedByFunctions.Add(function.Name);
                                }
                                analysis.TotalUsageCount++;
                            }
                        }
                    }

                    // Analyze function body for usage patterns
                    if (!string.IsNullOrEmpty(function.Body))
                    {
                        var bodyPatterns = ExtractUsagePatternsFromCode(typedef.Name, function.Body);
                        analysis.UsagePatterns.AddRange(bodyPatterns);
                    }
                }

                // Remove duplicates and consolidate patterns
                analysis.UsedByFunctions = analysis.UsedByFunctions.Distinct().ToList();
                analysis.UsagePatterns = ConsolidateUsagePatterns(analysis.UsagePatterns);

                _logger.LogInformation($"Typedef {typedef.Name} used {analysis.TotalUsageCount} times across {analysis.UsedByFunctions.Count} functions");

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing usage for typedef {typedef.Name}: {ex.Message}");
                return analysis;
            }
        }

        /// <inheritdoc/>
        public TypedefResolution ResolveTypedefChain(CTypedef typedef, List<CTypedef> allTypedefs)
        {
            _logger.LogDebug($"Resolving typedef chain for {typedef.Name}");

            var resolution = new TypedefResolution
            {
                OriginalTypedef = typedef.Name,
                ResolutionChain = new List<string> { typedef.Name }
            };

            try
            {
                var visited = new HashSet<string>();
                var current = typedef;
                visited.Add(current.Name);

                while (current != null)
                {
                    // Check if the original type is another typedef
                    var nextTypedef = allTypedefs.FirstOrDefault(t => t.Name == current.OriginalType);

                    if (nextTypedef == null)
                    {
                        // Found the ultimate type
                        resolution.UltimateType = current.OriginalType;
                        resolution.IsResolved = true;
                        break;
                    }

                    // Check for circular reference
                    if (visited.Contains(nextTypedef.Name))
                    {
                        resolution.ResolutionError = $"Circular reference detected: {string.Join(" -> ", visited)} -> {nextTypedef.Name}";
                        resolution.IsResolved = false;
                        break;
                    }

                    visited.Add(nextTypedef.Name);
                    resolution.ResolutionChain.Add(nextTypedef.Name);
                    current = nextTypedef;
                }

                resolution.ChainDepth = resolution.ResolutionChain.Count - 1;

                // Analyze the ultimate type if resolved
                if (resolution.IsResolved)
                {
                    AnalyzeUltimateType(resolution, typedef, allTypedefs);
                }

                return resolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving typedef chain for {typedef.Name}: {ex.Message}");
                resolution.ResolutionError = ex.Message;
                resolution.IsResolved = false;
                return resolution;
            }
        }

        /// <inheritdoc/>
        public List<TypedefValidationIssue> ValidateTypedefs(List<CTypedef> typedefs)
        {
            _logger.LogInformation($"Validating {typedefs.Count} typedefs");

            var issues = new List<TypedefValidationIssue>();

            try
            {
                // Check for duplicate names
                var duplicates = typedefs.GroupBy(t => t.Name)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => g.Key);

                foreach (var duplicate in duplicates)
                {
                    var duplicateTypedefs = typedefs.Where(t => t.Name == duplicate).ToList();
                    foreach (var typedef in duplicateTypedefs)
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.DuplicateDefinition,
                            Severity = IssueSeverity.Error,
                            Description = $"Typedef '{typedef.Name}' is defined multiple times",
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Use unique names for each typedef or combine definitions"
                        });
                    }
                }

                // Check for unused typedefs
                foreach (var typedef in typedefs)
                {
                    if (typedef.UsageCount == 0)
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.UnusedTypedef,
                            Severity = IssueSeverity.Warning,
                            Description = $"Typedef '{typedef.Name}' is defined but never used",
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Remove unused typedef or add usage documentation"
                        });
                    }
                }

                // Check for circular references
                foreach (var typedef in typedefs)
                {
                    var resolution = ResolveTypedefChain(typedef, typedefs);
                    if (!resolution.IsResolved && resolution.ResolutionError.Contains("Circular reference"))
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.CircularReference,
                            Severity = IssueSeverity.Error,
                            Description = resolution.ResolutionError,
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Break circular dependency by using forward declarations or restructuring types"
                        });
                    }
                }

                // Check naming conventions
                foreach (var typedef in typedefs)
                {
                    if (!IsValidNamingConvention(typedef.Name))
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.NamingConvention,
                            Severity = IssueSeverity.Info,
                            Description = $"Typedef '{typedef.Name}' doesn't follow naming conventions",
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Consider using conventional naming patterns (e.g., PascalCase, suffix with '_t')"
                        });
                    }
                }

                // Check for missing documentation
                foreach (var typedef in typedefs)
                {
                    if (string.IsNullOrWhiteSpace(typedef.Documentation))
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.MissingDocumentation,
                            Severity = IssueSeverity.Info,
                            Description = $"Typedef '{typedef.Name}' lacks documentation",
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Add documentation comments explaining the purpose and usage of this typedef"
                        });
                    }
                }

                // Check for complex typedef chains
                foreach (var typedef in typedefs)
                {
                    var resolution = ResolveTypedefChain(typedef, typedefs);
                    if (resolution.IsResolved && resolution.ChainDepth > 3)
                    {
                        issues.Add(new TypedefValidationIssue
                        {
                            TypedefName = typedef.Name,
                            IssueType = TypedefIssueType.ComplexChain,
                            Severity = IssueSeverity.Warning,
                            Description = $"Typedef '{typedef.Name}' has a complex resolution chain (depth: {resolution.ChainDepth})",
                            Location = $"{typedef.SourceFile}:{typedef.LineNumber}",
                            LineNumber = typedef.LineNumber,
                            SuggestedFix = "Consider simplifying the typedef chain for better readability"
                        });
                    }
                }

                _logger.LogInformation($"Found {issues.Count} validation issues");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating typedefs: {ex.Message}");
                return issues;
            }
        }

        /// <summary>
        /// Determines if a type is a pointer type
        /// </summary>
        private bool IsPointerType(CXType type)
        {
            try
            {
                return type.kind == CXTypeKind.CXType_Pointer;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a type is an array type
        /// </summary>
        private bool IsArrayType(CXType type)
        {
            try
            {
                return type.kind == CXTypeKind.CXType_ConstantArray ||
                       type.kind == CXTypeKind.CXType_IncompleteArray;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a type is a function pointer type
        /// </summary>
        private bool IsFunctionPointerType(CXType type)
        {
            try
            {
                // Check if it's a pointer to a function
                if (type.kind == CXTypeKind.CXType_Pointer)
                {
                    var pointee = type.PointeeType;
                    return pointee.kind == CXTypeKind.CXType_FunctionProto ||
                           pointee.kind == CXTypeKind.CXType_FunctionNoProto;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts function signature from a function pointer type
        /// </summary>
        private CFunctionSignature ExtractFunctionSignature(CXType type)
        {
            try
            {
                if (!IsFunctionPointerType(type))
                    return null;

                var pointee = type.PointeeType;
                var signature = new CFunctionSignature();

                // Extract return type
                signature.ReturnType = pointee.ResultType.Spelling.ToString();

                // Extract parameters
                int numArgs = pointee.NumArgTypes;
                for (int i = 0; i < numArgs; i++)
                {
                    var argType = pointee.GetArgType((uint)i);
                    signature.ParameterTypes.Add(argType.Spelling.ToString());
                }

                // Check if variadic
                signature.IsVariadic = pointee.IsFunctionTypeVariadic;

                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract function signature: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Extracts documentation from cursor comments
        /// </summary>
        private unsafe string ExtractDocumentation(CXCursor cursor)
        {
            try
            {
                // Try to get raw comment text first
                var rawComment = cursor.RawCommentText;
                if (!string.IsNullOrEmpty(rawComment.ToString()))
                {
                    return CleanCommentText(rawComment.ToString());
                }

                // Alternative: try to get brief comment
                var briefComment = cursor.BriefCommentText;
                if (!string.IsNullOrEmpty(briefComment.ToString()))
                {
                    return CleanCommentText(briefComment.ToString());
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Cleans comment text by removing comment markers
        /// </summary>
        private string CleanCommentText(string commentText)
        {
            if (string.IsNullOrEmpty(commentText))
                return string.Empty;

            try
            {
                // Remove common comment markers
                string cleaned = commentText
                    .Replace("/**", "")
                    .Replace("*/", "")
                    .Replace("/*", "")
                    .Replace("///", "")
                    .Replace("//", "");

                // Remove leading asterisks from each line
                var lines = cleaned.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var cleanedLines = lines.Select(line =>
                {
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("* "))
                        return trimmed.Substring(2);
                    else if (trimmed.StartsWith("*"))
                        return trimmed.Substring(1);
                    return trimmed;
                });

                return string.Join(" ", cleanedLines).Trim();
            }
            catch
            {
                return commentText;
            }
        }

        /// <summary>
        /// Cleans and normalizes type names
        /// </summary>
        private string CleanTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return string.Empty;

            // Remove extra spaces and normalize
            return Regex.Replace(typeName.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Extracts constraints based on the underlying type
        /// </summary>
        private List<TypedefConstraint> ExtractUnderlyingTypeConstraints(CTypedef typedef)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                // If it's a function pointer, add function pointer constraints
                if (typedef.IsFunctionPointer)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = "function_pointer != NULL",
                        Source = "Function pointer constraint",
                        Description = "Function pointer must be valid (non-NULL) before use"
                    });
                }

                // If it's a pointer type, add pointer constraints
                if (typedef.IsPointerType)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = "ptr != NULL",
                        Source = "Pointer type constraint",
                        Description = "Pointer must be valid (non-NULL) before dereferencing"
                    });
                }

                // Add constraints based on the original type
                var underlyingConstraints = ExtractConstraintsFromTypeName(typedef.OriginalType, typedef.Name);
                constraints.AddRange(underlyingConstraints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting underlying type constraints for typedef {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts constraints based on type name patterns
        /// </summary>
        private List<TypedefConstraint> ExtractConstraintsFromTypeName(string typeName, string typedefName)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                // Handle common integer types
                if (Regex.IsMatch(typeName, @"\bint\b"))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedefName,
                        Type = ConstraintType.Range,
                        MinValue = "-2147483648",
                        MaxValue = "2147483647",
                        Source = $"Underlying type: {typeName}",
                        Description = "Standard integer range constraints"
                    });
                }
                else if (Regex.IsMatch(typeName, @"\bunsigned\s+int\b"))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedefName,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "4294967295",
                        Source = $"Underlying type: {typeName}",
                        Description = "Unsigned integer range constraints"
                    });
                }
                else if (Regex.IsMatch(typeName, @"\bchar\b"))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedefName,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "255",
                        Source = $"Underlying type: {typeName}",
                        Description = "Character type range constraints"
                    });
                }
                else if (Regex.IsMatch(typeName, @"\bshort\b"))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedefName,
                        Type = ConstraintType.Range,
                        MinValue = "-32768",
                        MaxValue = "32767",
                        Source = $"Underlying type: {typeName}",
                        Description = "Short integer range constraints"
                    });
                }
                else if (Regex.IsMatch(typeName, @"\blong\b"))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedefName,
                        Type = ConstraintType.Range,
                        MinValue = IntPtr.Size == 8 ? "-9223372036854775808" : "-2147483648",
                        MaxValue = IntPtr.Size == 8 ? "9223372036854775807" : "2147483647",
                        Source = $"Underlying type: {typeName}",
                        Description = "Long integer range constraints (platform dependent)"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting constraints from type name {typeName}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Calculates usage count of a typedef across functions
        /// </summary>
        private int CalculateUsageCount(CTypedef typedef, List<CFunction> functions)
        {
            int count = 0;

            try
            {
                foreach (var function in functions)
                {
                    // Check parameters
                    if (function.Parameters != null)
                    {
                        count += function.Parameters.Count(p => IsTypedefUsed(p.ParameterType, typedef.Name));
                    }

                    // Check return type
                    if (IsTypedefUsed(function.ReturnType, typedef.Name))
                    {
                        count++;
                    }

                    // Check local variables
                    if (function.LocalVariables != null)
                    {
                        count += function.LocalVariables.Count(v => IsTypedefUsed(v.TypeName, typedef.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error calculating usage count for typedef {typedef.Name}: {ex.Message}");
            }

            return count;
        }

        /// <summary>
        /// Extracts function pointer specific constraints
        /// </summary>
        private List<TypedefConstraint> ExtractFunctionPointerConstraints(CTypedef typedef)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                if (typedef.FunctionSignature == null)
                    return constraints;

                var sig = typedef.FunctionSignature;

                // Add parameter count constraint
                constraints.Add(new TypedefConstraint
                {
                    TypedefName = typedef.Name,
                    Type = ConstraintType.Custom,
                    Expression = $"parameter_count == {sig.ParameterTypes.Count}",
                    Source = "Function pointer signature",
                    Description = $"Function must accept exactly {sig.ParameterTypes.Count} parameters"
                });

                // Add return type constraint
                if (!string.IsNullOrEmpty(sig.ReturnType))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = $"return_type == {sig.ReturnType}",
                        Source = "Function pointer signature",
                        Description = $"Function must return type: {sig.ReturnType}"
                    });
                }

                // Add variadic constraint
                if (sig.IsVariadic)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = "supports_variadic_args",
                        Source = "Function pointer signature",
                        Description = "Function must support variable number of arguments"
                    });
                }

                // Add calling convention constraint
                if (!string.IsNullOrEmpty(sig.CallingConvention))
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = $"calling_convention == {sig.CallingConvention}",
                        Source = "Function pointer signature",
                        Description = $"Function must use calling convention: {sig.CallingConvention}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function pointer constraints for typedef {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts relationship constraints with structures and enumerations
        /// </summary>
        private List<TypedefConstraint> ExtractRelationshipConstraints(CTypedef typedef, List<CStruct> structures, List<CEnum> enumerations)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                // Check if typedef is based on a structure
                var relatedStruct = structures.FirstOrDefault(s =>
                    typedef.OriginalType.Contains(s.Name) ||
                    typedef.OriginalType.Contains($"struct {s.Name}"));

                if (relatedStruct != null)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = $"based_on_struct_{relatedStruct.Name}",
                        Source = $"Related to struct: {relatedStruct.Name}",
                        Description = $"Typedef is based on structure {relatedStruct.Name}"
                    });
                }

                // Check if typedef is based on an enumeration
                var relatedEnum = enumerations.FirstOrDefault(e =>
                    typedef.OriginalType.Contains(e.Name) ||
                    typedef.OriginalType.Contains($"enum {e.Name}"));

                if (relatedEnum != null && relatedEnum.Values != null)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = relatedEnum.Values.Select(v => v.Name).ToList(),
                        Source = $"Related to enum: {relatedEnum.Name}",
                        Description = $"Typedef is based on enumeration {relatedEnum.Name}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting relationship constraints for typedef {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Analyzes typedef chain for circular references and dependencies
        /// </summary>
        private List<TypedefConstraint> AnalyzeTypedefChain(CTypedef typedef, List<CTypedef> allTypedefs)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                var resolution = ResolveTypedefChain(typedef, allTypedefs);

                if (!resolution.IsResolved)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = "unresolved_typedef_chain",
                        Source = "Typedef chain analysis",
                        Description = resolution.ResolutionError
                    });
                }
                else if (resolution.ChainDepth > 1)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = $"typedef_chain_depth == {resolution.ChainDepth}",
                        Source = "Typedef chain analysis",
                        Description = $"Typedef resolves through {resolution.ChainDepth} levels: {string.Join(" -> ", resolution.ResolutionChain)}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing typedef chain for {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts constraints from source code comments
        /// </summary>
        private async Task<List<TypedefConstraint>> ExtractConstraintsFromCommentsAsync(CTypedef typedef, SourceFile sourceFile)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                if (sourceFile?.Lines == null || sourceFile.Lines.Count == 0)
                    return constraints;

                // Look for comments around the typedef declaration
                int startLine = Math.Max(0, typedef.LineNumber - 5);
                int endLine = Math.Min(sourceFile.Lines.Count - 1, typedef.LineNumber + 5);

                for (int i = startLine; i <= endLine; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Look for range comments
                    var rangeMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Range|Valid range|Value range):\s*(-?\d+(?:\.\d+)?)\s*(?:to|-)\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                    if (rangeMatch.Success)
                    {
                        constraints.Add(new TypedefConstraint
                        {
                            TypedefName = typedef.Name,
                            Type = ConstraintType.Range,
                            MinValue = rangeMatch.Groups[1].Value,
                            MaxValue = rangeMatch.Groups[2].Value,
                            Source = $"Comment at line {i + 1}",
                            Description = "Range constraint from documentation"
                        });
                    }

                    // Look for enumeration comments
                    var enumMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Valid values|Allowed|Values):\s*([\w\d\s,]+)", RegexOptions.IgnoreCase);
                    if (enumMatch.Success)
                    {
                        var values = enumMatch.Groups[1].Value
                            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => v.Trim())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .ToList();

                        if (values.Count > 0)
                        {
                            constraints.Add(new TypedefConstraint
                            {
                                TypedefName = typedef.Name,
                                Type = ConstraintType.Enumeration,
                                AllowedValues = values,
                                Source = $"Comment at line {i + 1}",
                                Description = "Allowed values from documentation"
                            });
                        }
                    }

                    // Look for usage notes
                    var usageMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Usage|Note|Warning):\s*(.+)", RegexOptions.IgnoreCase);
                    if (usageMatch.Success)
                    {
                        constraints.Add(new TypedefConstraint
                        {
                            TypedefName = typedef.Name,
                            Type = ConstraintType.Custom,
                            Expression = "usage_note",
                            Source = $"Comment at line {i + 1}",
                            Description = usageMatch.Groups[1].Value.Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from comments for typedef {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts constraints from usage patterns in code
        /// </summary>
        private async Task<List<TypedefConstraint>> ExtractConstraintsFromPatternsAsync(CTypedef typedef, SourceFile sourceFile)
        {
            var constraints = new List<TypedefConstraint>();

            try
            {
                if (sourceFile?.Content == null)
                    return constraints;

                string content = sourceFile.Content;

                // Look for assignment patterns
                var assignmentPattern = new Regex($@"{Regex.Escape(typedef.Name)}\s+\w+\s*=\s*([^;]+);");
                var assignmentMatches = assignmentPattern.Matches(content);

                var assignedValues = new List<string>();
                foreach (Match match in assignmentMatches)
                {
                    string value = match.Groups[1].Value.Trim();
                    if (Regex.IsMatch(value, @"^[0-9]+$|^[0-9]*\.[0-9]+$|^'.'$"))
                    {
                        assignedValues.Add(value);
                    }
                }

                if (assignedValues.Count > 0)
                {
                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = assignedValues.Distinct().ToList(),
                        Source = "Code assignment patterns",
                        Description = "Values found in assignment statements"
                    });
                }

                // Look for comparison patterns
                var comparisonPattern = new Regex($@"if\s*\(\s*\w+\s*(==|!=|<|>|<=|>=)\s*([^&|)]+)\s*\).*{Regex.Escape(typedef.Name)}");
                var comparisonMatches = comparisonPattern.Matches(content);

                foreach (Match match in comparisonMatches)
                {
                    string op = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    constraints.Add(new TypedefConstraint
                    {
                        TypedefName = typedef.Name,
                        Type = ConstraintType.Custom,
                        Expression = $"comparison_pattern: {op} {value}",
                        Source = "Code comparison patterns",
                        Description = $"Found comparison pattern using operator {op} with value {value}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from patterns for typedef {typedef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Checks if a typedef is used in a type name
        /// </summary>
        private bool IsTypedefUsed(string typeName, string typedefName)
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(typedefName))
                return false;

            // Simple exact match
            if (typeName == typedefName)
                return true;

            // Check for typedef usage in pointer types, arrays, etc.
            var pattern = $@"\b{Regex.Escape(typedefName)}\b";
            return Regex.IsMatch(typeName, pattern);
        }

        /// <summary>
        /// Extracts usage patterns from function code
        /// </summary>
        private List<TypedefUsagePattern> ExtractUsagePatternsFromCode(string typedefName, string code)
        {
            var patterns = new List<TypedefUsagePattern>();

            try
            {
                var escapedName = Regex.Escape(typedefName);

                // Variable declarations
                var declPattern = new Regex($@"{escapedName}\s+(\w+)");
                var declMatches = declPattern.Matches(code);
                if (declMatches.Count > 0)
                {
                    patterns.Add(new TypedefUsagePattern
                    {
                        UsageType = TypedefUsageType.VariableDeclaration,
                        Description = "Variable declaration using typedef",
                        Frequency = declMatches.Count,
                        Examples = declMatches.Cast<Match>().Take(3).Select(m => m.Value).ToList()
                    });
                }

                // Type casts
                var castPattern = new Regex($@"\(\s*{escapedName}\s*\)");
                var castMatches = castPattern.Matches(code);
                if (castMatches.Count > 0)
                {
                    patterns.Add(new TypedefUsagePattern
                    {
                        UsageType = TypedefUsageType.TypeCast,
                        Description = "Type casting to typedef",
                        Frequency = castMatches.Count,
                        Examples = castMatches.Cast<Match>().Take(3).Select(m => m.Value).ToList()
                    });
                }

                // Sizeof operations
                var sizeofPattern = new Regex($@"sizeof\s*\(\s*{escapedName}\s*\)");
                var sizeofMatches = sizeofPattern.Matches(code);
                if (sizeofMatches.Count > 0)
                {
                    patterns.Add(new TypedefUsagePattern
                    {
                        UsageType = TypedefUsageType.SizeofOperand,
                        Description = "Sizeof operation with typedef",
                        Frequency = sizeofMatches.Count,
                        Examples = sizeofMatches.Cast<Match>().Take(3).Select(m => m.Value).ToList()
                    });
                }

                // Pointer declarations
                var ptrPattern = new Regex($@"{escapedName}\s*\*+\s*(\w+)");
                var ptrMatches = ptrPattern.Matches(code);
                if (ptrMatches.Count > 0)
                {
                    patterns.Add(new TypedefUsagePattern
                    {
                        UsageType = TypedefUsageType.PointerTarget,
                        Description = "Pointer to typedef",
                        Frequency = ptrMatches.Count,
                        Examples = ptrMatches.Cast<Match>().Take(3).Select(m => m.Value).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting usage patterns for typedef {typedefName}: {ex.Message}");
            }

            return patterns;
        }

        /// <summary>
        /// Consolidates usage patterns by removing duplicates and merging similar patterns
        /// </summary>
        private List<TypedefUsagePattern> ConsolidateUsagePatterns(List<TypedefUsagePattern> patterns)
        {
            try
            {
                var consolidated = new List<TypedefUsagePattern>();
                var grouped = patterns.GroupBy(p => p.UsageType);

                foreach (var group in grouped)
                {
                    var merged = new TypedefUsagePattern
                    {
                        UsageType = group.Key,
                        Description = group.First().Description,
                        Frequency = group.Sum(p => p.Frequency),
                        Examples = group.SelectMany(p => p.Examples).Distinct().Take(5).ToList()
                    };
                    consolidated.Add(merged);
                }

                return consolidated.OrderByDescending(p => p.Frequency).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error consolidating usage patterns: {ex.Message}");
                return patterns;
            }
        }

        /// <summary>
        /// Analyzes the ultimate type after resolving typedef chain
        /// </summary>
        private void AnalyzeUltimateType(TypedefResolution resolution, CTypedef originalTypedef, List<CTypedef> allTypedefs)
        {
            try
            {
                string ultimateType = resolution.UltimateType;

                // Check if it's a primitive type
                resolution.IsPrimitive = IsBasicType(ultimateType);

                // Check if it's a pointer
                resolution.IsPointer = ultimateType.Contains("*") || originalTypedef.IsPointerType;

                // Check if it's an array
                resolution.IsArray = ultimateType.Contains("[") || originalTypedef.IsArrayType;

                // Check if it's a function pointer
                resolution.IsFunctionPointer = originalTypedef.IsFunctionPointer;

                // Check if it's a custom type
                resolution.IsCustomType = !resolution.IsPrimitive &&
                                        !ultimateType.StartsWith("struct ") &&
                                        !ultimateType.StartsWith("enum ") &&
                                        !ultimateType.StartsWith("union ");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error analyzing ultimate type {resolution.UltimateType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a type name represents a basic/primitive type
        /// </summary>
        private bool IsBasicType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            var basicTypes = new[]
            {
                "void", "char", "signed char", "unsigned char",
                "short", "signed short", "unsigned short",
                "int", "signed int", "unsigned int",
                "long", "signed long", "unsigned long",
                "long long", "signed long long", "unsigned long long",
                "float", "double", "long double",
                "_Bool", "bool"
            };

            string cleanType = typeName.Trim().ToLower();
            return basicTypes.Contains(cleanType) ||
                   basicTypes.Any(bt => cleanType.StartsWith(bt + " ") || cleanType.EndsWith(" " + bt));
        }

        /// <summary>
        /// Validates naming convention for typedef
        /// </summary>
        private bool IsValidNamingConvention(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Common conventions:
            // 1. PascalCase (MyType)
            // 2. snake_case_t (my_type_t)  
            // 3. UPPER_CASE_T (MY_TYPE_T)
            // 4. camelCase (myType)

            // Check for PascalCase
            if (Regex.IsMatch(name, @"^[A-Z][a-zA-Z0-9]*$"))
                return true;

            // Check for snake_case with optional _t suffix
            if (Regex.IsMatch(name, @"^[a-z][a-z0-9_]*(_t)?$"))
                return true;

            // Check for UPPER_CASE with optional _T suffix
            if (Regex.IsMatch(name, @"^[A-Z][A-Z0-9_]*(_T)?$"))
                return true;

            // Check for camelCase
            if (Regex.IsMatch(name, @"^[a-z][a-zA-Z0-9]*$"))
                return true;

            // Allow some flexibility for legacy code
            return name.Length >= 2 && char.IsLetter(name[0]);
        }
    }
}

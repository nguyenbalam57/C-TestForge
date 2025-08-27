using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Core.SupportingClasses.Unions;
using C_TestForge.Models.Parse;
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
    /// <summary>
    /// Implementation of union analysis and extraction service
    /// </summary>
    public class UnionAnalysisExtractor : IUnionAnalysisExtractor
    {
        private readonly ILogger<UnionAnalysisExtractor> _logger;
        private readonly ITypeManager _typeManager;

        private static readonly Regex UnionNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        private static readonly Regex MemberNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        private static readonly HashSet<string> SystemTypes = new HashSet<string>
        {
            "char", "short", "int", "long", "float", "double", "void",
            "signed", "unsigned", "const", "volatile", "static", "extern",
            "register", "auto", "typedef", "struct", "union", "enum",
            "size_t", "ptrdiff_t", "wchar_t", "FILE", "time_t"
        };

        private static readonly HashSet<string> PackingAttributes = new HashSet<string>
        {
            "__attribute__", "__packed__", "packed", "#pragma pack"
        };

        // Type size mapping for memory layout calculation
        private static readonly Dictionary<string, int> TypeSizes = new Dictionary<string, int>
        {
            {"char", 1}, {"signed char", 1}, {"unsigned char", 1},
            {"short", 2}, {"short int", 2}, {"signed short", 2}, {"signed short int", 2},
            {"unsigned short", 2}, {"unsigned short int", 2},
            {"int", 4}, {"signed", 4}, {"signed int", 4}, {"unsigned", 4}, {"unsigned int", 4},
            {"long", 8}, {"long int", 8}, {"signed long", 8}, {"signed long int", 8},
            {"unsigned long", 8}, {"unsigned long int", 8},
            {"long long", 8}, {"long long int", 8}, {"signed long long", 8}, {"signed long long int", 8},
            {"unsigned long long", 8}, {"unsigned long long int", 8},
            {"float", 4}, {"double", 8}, {"long double", 16},
            {"void*", 8}, {"size_t", 8}, {"ptrdiff_t", 8}
        };

        /// <summary>
        /// Constructor for UnionAnalysisExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="typeManager">Type manager for type resolution</param>
        public UnionAnalysisExtractor(
            ILogger<UnionAnalysisExtractor> logger,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractUnionDefinition(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_UnionDecl)
                {
                    return;
                }

                string unionName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting union definition: {unionName}");

                // Get union location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Create union object
                var unionDef = new CUnion
                {
                    Name = string.IsNullOrEmpty(unionName) ? $"__anonymous_union_{line}" : unionName,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsAnonymous = string.IsNullOrEmpty(unionName),
                    IsForwardDeclaration = IsForwardDeclaration(cursor)
                };

                // Extract documentation
                unionDef.Documentation = ExtractUnionDocumentation(cursor);

                // Extract attributes
                unionDef.Attributes = ExtractUnionAttributes(cursor, unionDef);

                // Extract members if not a forward declaration
                if (!unionDef.IsForwardDeclaration)
                {
                    ExtractUnionMembers(cursor, unionDef);

                    // Calculate memory layout
                    var memoryLayout = CalculateMemoryLayout(unionDef);
                    unionDef.Size = memoryLayout.TotalSize;
                    unionDef.Alignment = memoryLayout.Alignment;
                }

                // Validate union definition
                var validationErrors = ValidateUnionDefinition(unionDef);
                result.ParseErrors.AddRange(validationErrors);

                // Add to result
                if (result.Unions == null)
                {
                    result.Unions = new List<CUnion>();
                }
                result.Unions.Add(unionDef);

                // Update statistics
                result.Statistics.SymbolsResolved++;

                _logger.LogDebug($"Successfully extracted union: {unionDef.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting union definition: {ex.Message}");
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                result.ParseErrors.Add(new ParseError
                {
                    Message = $"Failed to extract union definition: {ex.Message}",
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    Severity = ErrorSeverity.Warning,
                });
            }
        }

        /// <inheritdoc/>
        public async Task<List<UnionDependency>> AnalyzeUnionDependenciesAsync(
            List<CUnion> unions,
            ParseResult result)
        {
            _logger.LogInformation($"Analyzing dependencies for {unions.Count} union definitions");

            var dependencies = new List<UnionDependency>();

            try
            {
                foreach (var unionDef in unions)
                {
                    _logger.LogDebug($"Analyzing dependencies for union: {unionDef.Name}");

                    // Find direct dependencies
                    var directDeps = FindDirectDependencies(unionDef, unions);

                    foreach (var dep in directDeps)
                    {
                        dependencies.Add(new UnionDependency
                        {
                            UnionName = unionDef.Name,
                            DependsOn = dep,
                            DependencyType = UnionDependencyType.Direct,
                            LineNumber = unionDef.LineNumber
                        });
                    }

                    // Analyze circular dependencies
                    var circularDeps = DetectCircularDependencies(unionDef, unions);
                    foreach (var circularDep in circularDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Circular dependency detected: {unionDef.Name} <-> {circularDep}",
                            LineNumber = unionDef.LineNumber,
                            Category = "Union Dependencies",
                            Code = "UNION_CIRCULAR"
                        });
                    }

                    // Check for undefined dependencies
                    var undefinedDeps = FindUndefinedDependencies(unionDef, unions);
                    foreach (var undefinedDep in undefinedDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Union '{unionDef.Name}' references undefined union '{undefinedDep}'",
                            LineNumber = unionDef.LineNumber,
                            Category = "Undefined References",
                            Code = "UNION_UNDEFINED"
                        });
                    }
                }

                _logger.LogInformation($"Found {dependencies.Count} union dependencies");
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing union dependencies: {ex.Message}");
                return dependencies;
            }
        }

        /// <inheritdoc/>
        public List<ParseError> ValidateUnionDefinition(CUnion unionDef)
        {
            var errors = new List<ParseError>();

            try
            {
                if (unionDef == null)
                {
                    errors.Add(CreateValidationError("Null union definition", 0, 0));
                    return errors;
                }

                _logger.LogDebug($"Validating union definition: {unionDef.Name}");

                // Validate name
                ValidateUnionName(unionDef, errors);

                // Validate members
                ValidateUnionMembers(unionDef, errors);

                // Check for naming conventions
                ValidateNamingConventions(unionDef, errors);

                // Check for memory layout issues
                ValidateMemoryLayout(unionDef, errors);

                // Check for potential issues with bit fields
                ValidateBitFields(unionDef, errors);

                // Check for union-specific issues
                ValidateUnionSpecificIssues(unionDef, errors);

                _logger.LogDebug($"Validation completed for union: {unionDef.Name}, found {errors.Count} issues");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating union definition {unionDef?.Name}: {ex.Message}");
                errors.Add(CreateValidationError($"Validation error: {ex.Message}",
                    unionDef?.LineNumber ?? 0, unionDef?.ColumnNumber ?? 0));
            }

            return errors;
        }

        /// <inheritdoc/>
        public async Task<List<UnionConstraint>> ExtractUnionConstraintsAsync(
            CUnion unionDef,
            string sourceCode)
        {
            _logger.LogInformation($"Extracting constraints for union {unionDef.Name}");

            var constraints = new List<UnionConstraint>();

            try
            {
                // Extract usage patterns
                var usagePatterns = await ExtractUsagePatternsAsync(unionDef, sourceCode);
                constraints.AddRange(usagePatterns);

                // Extract size constraints
                var sizeConstraints = ExtractSizeConstraints(unionDef);
                constraints.AddRange(sizeConstraints);

                // Extract alignment constraints
                var alignmentConstraints = ExtractAlignmentConstraints(unionDef);
                constraints.AddRange(alignmentConstraints);

                // Extract type safety constraints
                var typeSafetyConstraints = ExtractTypeSafetyConstraints(unionDef);
                constraints.AddRange(typeSafetyConstraints);

                // Extract memory access constraints
                var memoryAccessConstraints = ExtractMemoryAccessConstraints(unionDef);
                constraints.AddRange(memoryAccessConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for union {unionDef.Name}");
                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for union {unionDef.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <inheritdoc/>
        public UnionMemoryLayout CalculateMemoryLayout(CUnion unionDef)
        {
            _logger.LogDebug($"Calculating memory layout for union: {unionDef.Name}");

            var layout = new UnionMemoryLayout
            {
                UnionName = unionDef.Name,
                Alignment = 1,
                TotalSize = 0
            };

            try
            {
                int maxSize = 0;
                int maxAlignment = 1;
                string largestMember = "";
                bool hasBitFields = false;

                foreach (var member in unionDef.Members)
                {
                    var memberLayout = new UnionMemberLayout
                    {
                        MemberName = member.Name,
                        DataType = member.DataType,
                        IsBitField = member.IsBitfield,
                        BitWidth = member.BitfieldWidth,
                        Offset = 0 // All union members start at offset 0
                    };

                    if (member.IsBitfield)
                    {
                        hasBitFields = true;
                        memberLayout.BitOffset = 0;
                        memberLayout.Size = (member.BitfieldWidth + 7) / 8; // Round up to bytes
                        memberLayout.Alignment = 1;
                    }
                    else
                    {
                        // Calculate member alignment and size
                        int memberAlignment = CalculateMemberAlignment(member);
                        int memberSize = CalculateMemberSize(member);

                        memberLayout.Alignment = memberAlignment;
                        memberLayout.Size = memberSize;

                        maxAlignment = Math.Max(maxAlignment, memberAlignment);

                        if (memberSize > maxSize)
                        {
                            maxSize = memberSize;
                            largestMember = member.Name;

                            // Mark previous members as not largest
                            foreach (var prevLayout in layout.MemberLayouts)
                            {
                                prevLayout.IsLargestMember = false;
                            }
                            memberLayout.IsLargestMember = true;
                        }
                    }

                    layout.MemberLayouts.Add(memberLayout);
                }

                // Union size is the size of the largest member
                layout.TotalSize = maxSize;
                layout.Alignment = maxAlignment;
                layout.LargestMember = largestMember;
                layout.HasBitFields = hasBitFields;

                // Apply alignment padding to total size
                if (layout.TotalSize % layout.Alignment != 0)
                {
                    layout.TotalSize = ((layout.TotalSize + layout.Alignment - 1) / layout.Alignment) * layout.Alignment;
                }

                _logger.LogDebug($"Calculated layout for {unionDef.Name}: Size={layout.TotalSize}, Alignment={layout.Alignment}, Largest={largestMember}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating memory layout for union {unionDef.Name}: {ex.Message}");
                layout.TotalSize = 0;
                layout.Alignment = 1;
            }

            return layout;
        }

        #region Private Helper Methods

        /// <summary>
        /// Check if cursor represents a forward declaration
        /// </summary>
        private unsafe bool IsForwardDeclaration(CXCursor cursor)
        {
            try
            {
                var definition = cursor.Definition;
                return definition.IsNull || cursor.Equals(definition);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extract union documentation from cursor
        /// </summary>
        private unsafe string ExtractUnionDocumentation(CXCursor cursor)
        {
            try
            {
                var comment = cursor.ParsedComment;
                if (comment.Kind == CXCommentKind.CXComment_Null)
                    return string.Empty;

                return comment.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract union documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract union attributes
        /// </summary>
        private unsafe List<CUnionAttribute> ExtractUnionAttributes(CXCursor cursor, CUnion unionDef)
        {
            var attributes = new List<CUnionAttribute>();

            try
            {
                // Visit children to find attributes
                cursor.VisitChildren((childCursor, parent, clientData) =>
                {
                    if (childCursor.Kind == CXCursorKind.CXCursor_UnexposedAttr)
                    {
                        var attrText = childCursor.Spelling.ToString();
                        if (!string.IsNullOrEmpty(attrText))
                        {
                            var attribute = ParseUnionAttribute(attrText);
                            if (attribute != null)
                            {
                                attributes.Add(attribute);
                            }
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting union attributes: {ex.Message}");
            }

            return attributes;
        }

        /// <summary>
        /// Extract union members from cursor
        /// </summary>
        private unsafe void ExtractUnionMembers(CXCursor cursor, CUnion unionDef)
        {
            try
            {
                cursor.VisitChildren((childCursor, parent, clientData) =>
                {
                    if (childCursor.Kind == CXCursorKind.CXCursor_FieldDecl)
                    {
                        var member = ExtractMemberFromCursor(childCursor);
                        if (member != null)
                        {
                            unionDef.Members.Add(member);
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting union members: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract member information from cursor
        /// </summary>
        private unsafe CUnionMember ExtractMemberFromCursor(CXCursor cursor)
        {
            try
            {
                var memberName = cursor.Spelling.ToString();
                var memberType = cursor.Type;

                // Get member location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);

                var member = new CUnionMember
                {
                    Name = memberName,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    DataType = memberType.Spelling.ToString(),
                    IsAnonymous = string.IsNullOrEmpty(memberName),
                    IsPointer = memberType.kind == CXTypeKind.CXType_Pointer,
                    IsArray = memberType.kind == CXTypeKind.CXType_ConstantArray,
                    Size = CalculateMemberSize(memberType),
                    Alignment = CalculateMemberAlignment(memberType)
                };

                // Extract bitfield information
                if (cursor.IsBitField)
                {
                    member.IsBitfield = true;
                    member.BitfieldWidth = cursor.FieldDeclBitWidth;
                }

                // Extract array size if applicable
                if (member.IsArray)
                {
                    member.ArraySize = (int)memberType.ArraySize;
                }

                return member;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting member from cursor: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse attribute string into CUnionAttribute
        /// </summary>
        private CUnionAttribute ParseUnionAttribute(string attrText)
        {
            try
            {
                var match = Regex.Match(attrText, @"(\w+)(?:\((.*?)\))?");
                if (match.Success)
                {
                    var attribute = new CUnionAttribute
                    {
                        AttributeName = match.Groups[1].Value,
                        RawText = attrText
                    };

                    if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        var paramString = match.Groups[2].Value;
                        attribute.Parameters = paramString.Split(',')
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrEmpty(p))
                            .ToList();
                    }

                    return attribute;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing union attribute '{attrText}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Calculate member size in bytes
        /// </summary>
        private int CalculateMemberSize(CUnionMember member)
        {
            try
            {
                if (member.IsPointer)
                    return 8; // Assuming 64-bit pointers

                string baseType = member.DataType.Replace("const", "").Replace("volatile", "").Trim();

                if (TypeSizes.ContainsKey(baseType))
                {
                    int baseSize = TypeSizes[baseType];
                    return member.IsArray ? baseSize * member.ArraySize : baseSize;
                }

                // Default size for unknown types
                return 4;
            }
            catch
            {
                return 4; // Default fallback
            }
        }

        /// <summary>
        /// Calculate member size from Clang type
        /// </summary>
        private unsafe int CalculateMemberSize(CXType memberType)
        {
            try
            {
                long size = memberType.SizeOf;
                return size > 0 ? (int)size : 4;
            }
            catch
            {
                return 4;
            }
        }

        /// <summary>
        /// Calculate member alignment
        /// </summary>
        private int CalculateMemberAlignment(CUnionMember member)
        {
            try
            {
                if (member.IsPointer)
                    return 8; // Pointer alignment

                string baseType = member.DataType.Replace("const", "").Replace("volatile", "").Trim();

                // Alignment typically matches size for basic types, up to 8 bytes
                if (TypeSizes.ContainsKey(baseType))
                {
                    return Math.Min(TypeSizes[baseType], 8);
                }

                return 4; // Default alignment
            }
            catch
            {
                return 4;
            }
        }

        /// <summary>
        /// Calculate member alignment from Clang type
        /// </summary>
        private unsafe int CalculateMemberAlignment(CXType memberType)
        {
            try
            {
                long alignment = memberType.AlignOf;
                return alignment > 0 ? (int)alignment : 4;
            }
            catch
            {
                return 4;
            }
        }

        /// <summary>
        /// Find direct dependencies of a union
        /// </summary>
        private List<string> FindDirectDependencies(CUnion unionDef, List<CUnion> allUnions)
        {
            var dependencies = new HashSet<string>();

            foreach (var member in unionDef.Members)
            {
                string memberType = member.DataType.Replace("struct", "").Replace("union", "").Trim();
                if (allUnions.Any(u => u.Name == memberType))
                {
                    dependencies.Add(memberType);
                }
            }

            return dependencies.ToList();
        }

        /// <summary>
        /// Detect circular dependencies
        /// </summary>
        private List<string> DetectCircularDependencies(CUnion unionDef, List<CUnion> allUnions)
        {
            var circularDeps = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            DetectCircularDependenciesRecursive(unionDef.Name, allUnions, visited, recursionStack, circularDeps);

            return circularDeps;
        }

        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private void DetectCircularDependenciesRecursive(string unionName, List<CUnion> allUnions,
            HashSet<string> visited, HashSet<string> recursionStack, List<string> circularDeps)
        {
            visited.Add(unionName);
            recursionStack.Add(unionName);

            var union = allUnions.FirstOrDefault(u => u.Name == unionName);
            if (union != null)
            {
                var dependencies = FindDirectDependencies(union, allUnions);

                foreach (var dep in dependencies)
                {
                    if (!visited.Contains(dep))
                    {
                        DetectCircularDependenciesRecursive(dep, allUnions, visited, recursionStack, circularDeps);
                    }
                    else if (recursionStack.Contains(dep))
                    {
                        circularDeps.Add(dep);
                    }
                }
            }

            recursionStack.Remove(unionName);
        }

        /// <summary>
        /// Find undefined dependencies
        /// </summary>
        private List<string> FindUndefinedDependencies(CUnion unionDef, List<CUnion> allUnions)
        {
            var undefinedDeps = new List<string>();
            var definedUnions = allUnions.Select(u => u.Name).ToHashSet();

            foreach (var member in unionDef.Members)
            {
                string memberType = member.DataType.Replace("struct", "").Replace("union", "").Trim();

                // Check if it looks like a union type but isn't defined
                if (memberType.Contains("union") && !definedUnions.Contains(memberType) && !SystemTypes.Contains(memberType))
                {
                    undefinedDeps.Add(memberType);
                }
            }

            return undefinedDeps;
        }

        /// <summary>
        /// Validate union name
        /// </summary>
        private void ValidateUnionName(CUnion unionDef, List<ParseError> errors)
        {
            if (string.IsNullOrEmpty(unionDef.Name))
            {
                errors.Add(CreateValidationError("Union has empty name", unionDef.LineNumber, unionDef.ColumnNumber));
                return;
            }

            if (!UnionNameRegex.IsMatch(unionDef.Name))
            {
                errors.Add(CreateValidationError($"Invalid union name '{unionDef.Name}' - must be valid C identifier",
                    unionDef.LineNumber, unionDef.ColumnNumber));
            }
        }

        /// <summary>
        /// Validate union members
        /// </summary>
        private void ValidateUnionMembers(CUnion unionDef, List<ParseError> errors)
        {
            if (unionDef.Members == null || unionDef.Members.Count == 0)
            {
                if (!unionDef.IsForwardDeclaration)
                {
                    errors.Add(CreateValidationError($"Union '{unionDef.Name}' has no members",
                        unionDef.LineNumber, unionDef.ColumnNumber));
                }
                return;
            }

            var memberNames = new HashSet<string>();
            foreach (var member in unionDef.Members)
            {
                // Check for duplicate member names
                if (!member.IsAnonymous && memberNames.Contains(member.Name))
                {
                    errors.Add(CreateValidationError($"Duplicate member name '{member.Name}' in union '{unionDef.Name}'",
                        member.LineNumber, member.ColumnNumber));
                }
                memberNames.Add(member.Name);

                // Validate member name
                if (!member.IsAnonymous && !MemberNameRegex.IsMatch(member.Name))
                {
                    errors.Add(CreateValidationError($"Invalid member name '{member.Name}' - must be valid C identifier",
                        member.LineNumber, member.ColumnNumber));
                }
            }
        }

        /// <summary>
        /// Validate naming conventions
        /// </summary>
        private void ValidateNamingConventions(CUnion unionDef, List<ParseError> errors)
        {
            // Check for common naming convention violations
            if (!unionDef.IsAnonymous && char.IsLower(unionDef.Name[0]))
            {
                errors.Add(CreateValidationError($"Union '{unionDef.Name}' should start with uppercase letter (convention)",
                    unionDef.LineNumber, unionDef.ColumnNumber, ErrorSeverity.Info));
            }
        }

        /// <summary>
        /// Validate memory layout
        /// </summary>
        private void ValidateMemoryLayout(CUnion unionDef, List<ParseError> errors)
        {
            if (unionDef.Size <= 0)
            {
                errors.Add(CreateValidationError($"Union '{unionDef.Name}' has invalid size: {unionDef.Size}",
                    unionDef.LineNumber, unionDef.ColumnNumber));
            }

            if (unionDef.Alignment <= 0 || (unionDef.Alignment & (unionDef.Alignment - 1)) != 0)
            {
                errors.Add(CreateValidationError($"Union '{unionDef.Name}' has invalid alignment: {unionDef.Alignment}",
                    unionDef.LineNumber, unionDef.ColumnNumber));
            }
        }

        /// <summary>
        /// Validate bit fields
        /// </summary>
        private void ValidateBitFields(CUnion unionDef, List<ParseError> errors)
        {
            var bitFieldMembers = unionDef.Members.Where(m => m.IsBitfield).ToList();

            foreach (var member in bitFieldMembers)
            {
                if (member.BitfieldWidth <= 0 || member.BitfieldWidth > 64)
                {
                    errors.Add(CreateValidationError($"Invalid bitfield width {member.BitfieldWidth} for member '{member.Name}'",
                        member.LineNumber, member.ColumnNumber));
                }
            }
        }

        /// <summary>
        /// Validate union-specific issues
        /// </summary>
        private void ValidateUnionSpecificIssues(CUnion unionDef, List<ParseError> errors)
        {
            // Check for potentially unsafe union usage
            var hasPointers = unionDef.Members.Any(m => m.IsPointer);
            var hasNonPointers = unionDef.Members.Any(m => !m.IsPointer);

            if (hasPointers && hasNonPointers)
            {
                errors.Add(CreateValidationError($"Union '{unionDef.Name}' mixes pointers and non-pointers - potential type safety issue",
                    unionDef.LineNumber, unionDef.ColumnNumber, ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Extract usage patterns from source code
        /// </summary>
        private async Task<List<UnionConstraint>> ExtractUsagePatternsAsync(CUnion unionDef, string sourceCode)
        {
            var constraints = new List<UnionConstraint>();

            try
            {
                // Look for usage patterns in the source code
                var unionUsagePattern = new Regex($@"\b{Regex.Escape(unionDef.Name)}\b", RegexOptions.IgnoreCase);
                var matches = unionUsagePattern.Matches(sourceCode);

                if (matches.Count > 0)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.UsagePattern,
                        Value = $"Used {matches.Count} times in source code",
                        Source = "Source Code Analysis",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Check for common unsafe patterns
                var unsafePatterns = new[]
                {
                    $@"memcpy\s*\([^)]*{Regex.Escape(unionDef.Name)}",
                    $@"memset\s*\([^)]*{Regex.Escape(unionDef.Name)}",
                    $@"\({Regex.Escape(unionDef.Name)}\s*\*\)\s*malloc"
                };

                foreach (var pattern in unsafePatterns)
                {
                    var unsafeMatches = Regex.Matches(sourceCode, pattern, RegexOptions.IgnoreCase);
                    if (unsafeMatches.Count > 0)
                    {
                        constraints.Add(new UnionConstraint
                        {
                            UnionName = unionDef.Name,
                            ConstraintType = UnionConstraintType.TypeSafety,
                            Value = "Potentially unsafe memory operations detected",
                            Source = "Pattern Analysis",
                            Severity = ConstraintSeverity.Warning
                        });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting usage patterns for union {unionDef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract size constraints
        /// </summary>
        private List<UnionConstraint> ExtractSizeConstraints(CUnion unionDef)
        {
            var constraints = new List<UnionConstraint>();

            try
            {
                // Check for size-related attributes
                foreach (var attr in unionDef.Attributes)
                {
                    if (attr.AttributeName.Contains("packed") || attr.AttributeName.Contains("align"))
                    {
                        constraints.Add(new UnionConstraint
                        {
                            UnionName = unionDef.Name,
                            ConstraintType = UnionConstraintType.PackingConstraint,
                            Value = attr.AttributeName,
                            Source = "Attribute",
                            Severity = ConstraintSeverity.Info
                        });
                    }
                }

                // Size constraint based on largest member
                if (unionDef.Size > 0)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.SizeConstraint,
                        Value = $"Size: {unionDef.Size} bytes",
                        Source = "Memory Layout",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Warn about very large unions
                if (unionDef.Size > 1024)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.SizeConstraint,
                        Value = "Large union size may impact performance",
                        Source = "Size Analysis",
                        Severity = ConstraintSeverity.Warning
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting size constraints for union {unionDef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract alignment constraints
        /// </summary>
        private List<UnionConstraint> ExtractAlignmentConstraints(CUnion unionDef)
        {
            var constraints = new List<UnionConstraint>();

            try
            {
                if (unionDef.Alignment > 0)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.AlignmentConstraint,
                        Value = $"Alignment: {unionDef.Alignment} bytes",
                        Source = "Memory Layout",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Check for non-standard alignment requirements
                if (unionDef.Alignment > 8)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.AlignmentConstraint,
                        Value = "Non-standard alignment requirement",
                        Source = "Alignment Analysis",
                        Severity = ConstraintSeverity.Warning
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting alignment constraints for union {unionDef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract type safety constraints
        /// </summary>
        private List<UnionConstraint> ExtractTypeSafetyConstraints(CUnion unionDef)
        {
            var constraints = new List<UnionConstraint>();

            try
            {
                // Check for mixed pointer/non-pointer members
                bool hasPointers = unionDef.Members.Any(m => m.IsPointer);
                bool hasNonPointers = unionDef.Members.Any(m => !m.IsPointer);

                if (hasPointers && hasNonPointers)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.TypeSafety,
                        Value = "Mixed pointer and non-pointer members",
                        Source = "Type Safety Analysis",
                        Severity = ConstraintSeverity.Warning
                    });
                }

                // Check for members with different sizes
                var memberSizes = unionDef.Members.Where(m => m.Size > 0).Select(m => m.Size).Distinct().ToList();
                if (memberSizes.Count > 2)
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.TypeSafety,
                        Value = "Members have significantly different sizes",
                        Source = "Size Analysis",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Member count constraint
                constraints.Add(new UnionConstraint
                {
                    UnionName = unionDef.Name,
                    ConstraintType = UnionConstraintType.MemberCount,
                    Value = $"{unionDef.Members.Count} members",
                    Source = "Structure Analysis",
                    Severity = ConstraintSeverity.Info
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting type safety constraints for union {unionDef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract memory access constraints
        /// </summary>
        private List<UnionConstraint> ExtractMemoryAccessConstraints(CUnion unionDef)
        {
            var constraints = new List<UnionConstraint>();

            try
            {
                // Check for volatile members
                if (unionDef.Members.Any(m => m.IsVolatile))
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.MemoryAccess,
                        Value = "Contains volatile members",
                        Source = "Volatile Analysis",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Check for const members
                if (unionDef.Members.Any(m => m.IsConst))
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.MemoryAccess,
                        Value = "Contains const members",
                        Source = "Const Analysis",
                        Severity = ConstraintSeverity.Info
                    });
                }

                // Check for bitfield members
                if (unionDef.Members.Any(m => m.IsBitfield))
                {
                    constraints.Add(new UnionConstraint
                    {
                        UnionName = unionDef.Name,
                        ConstraintType = UnionConstraintType.MemoryAccess,
                        Value = "Contains bitfield members",
                        Source = "Bitfield Analysis",
                        Severity = ConstraintSeverity.Warning
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting memory access constraints for union {unionDef.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Create a validation error
        /// </summary>
        private ParseError CreateValidationError(string message, int lineNumber, int columnNumber,
            ErrorSeverity severity = ErrorSeverity.Error)
        {
            return new ParseError
            {
                Message = message,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                Severity = severity
            };
        }

        #endregion
    }
}
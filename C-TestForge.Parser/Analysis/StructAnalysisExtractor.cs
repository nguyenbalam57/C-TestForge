using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Core.SupportingClasses.Structs;
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
    /// Implementation of struct analysis and extraction service
    /// </summary>
    public class StructAnalysisExtractor : IStructAnalysisExtractor
    {
        private readonly ILogger<StructAnalysisExtractor> _logger;
        private readonly ITypeManager _typeManager;

        private static readonly Regex StructNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        private static readonly Regex FieldNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$",
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
        /// Constructor for StructAnalysisExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="typeManager">Type manager for type resolution</param>
        public StructAnalysisExtractor(
            ILogger<StructAnalysisExtractor> logger,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractStructDefinition(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_StructDecl)
                {
                    return;
                }

                string structName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting struct definition: {structName}");

                // Get struct location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Create struct object
                var structDef = new CStruct
                {
                    Name = string.IsNullOrEmpty(structName) ? $"__anonymous_struct_{line}" : structName,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsAnonymous = string.IsNullOrEmpty(structName),
                    IsForwardDeclaration = IsForwardDeclaration(cursor)
                };

                // Extract documentation
                structDef.Documentation = ExtractStructDocumentation(cursor);

                // Extract attributes
                structDef.Attributes = ExtractStructAttributes(cursor);

                // Check if packed
                structDef.IsPacked = IsPackedStruct(cursor, structDef.Attributes);

                // Extract fields if not a forward declaration
                if (!structDef.IsForwardDeclaration)
                {
                    ExtractStructFields(cursor, structDef);

                    // Calculate memory layout
                    var memoryLayout = CalculateMemoryLayout(structDef);
                    structDef.Size = memoryLayout.TotalSize;
                    structDef.Alignment = memoryLayout.Alignment;

                    // Analyze dependencies
                    structDef.Dependencies = AnalyzeStructDependencies(structDef, result.Structures);
                }

                // Validate struct definition
                var validationErrors = ValidateStructDefinition(structDef);
                result.ParseErrors.AddRange(validationErrors);

                // Add to result
                result.Structures.Add(structDef);

                // Update statistics
                result.Statistics.SymbolsResolved++;

                _logger.LogDebug($"Successfully extracted struct: {structDef.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting struct definition: {ex.Message}");
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                result.ParseErrors.Add(new ParseError
                {
                    Message = $"Failed to extract struct definition: {ex.Message}",
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    Severity = ErrorSeverity.Warning,
                });
            }
        }

        /// <inheritdoc/>
        public async Task<List<StructDependency>> AnalyzeStructDependenciesAsync(
            List<CStruct> structs,
            ParseResult result)
        {
            _logger.LogInformation($"Analyzing dependencies for {structs.Count} struct definitions");

            var dependencies = new List<StructDependency>();

            try
            {
                foreach (var structDef in structs)
                {
                    _logger.LogDebug($"Analyzing dependencies for struct: {structDef.Name}");

                    // Find direct dependencies
                    var directDeps = FindDirectDependencies(structDef, structs);

                    foreach (var dep in directDeps)
                    {
                        dependencies.Add(new StructDependency
                        {
                            StructName = structDef.Name,
                            DependsOn = dep,
                            DependencyType = StructDependencyType.Direct,
                            LineNumber = structDef.LineNumber
                        });
                    }

                    // Analyze circular dependencies
                    var circularDeps = DetectCircularDependencies(structDef, structs);
                    foreach (var circularDep in circularDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Circular dependency detected: {structDef.Name} <-> {circularDep}",
                            LineNumber = structDef.LineNumber,
                            Category = "Struct Dependencies",
                            Code = "STRUCT_CIRCULAR"
                        });
                    }

                    // Check for undefined dependencies
                    var undefinedDeps = FindUndefinedDependencies(structDef, structs);
                    foreach (var undefinedDep in undefinedDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Struct '{structDef.Name}' references undefined struct '{undefinedDep}'",
                            LineNumber = structDef.LineNumber,
                            Category = "Undefined References",
                            Code = "STRUCT_UNDEFINED"
                        });
                    }
                }

                _logger.LogInformation($"Found {dependencies.Count} struct dependencies");
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing struct dependencies: {ex.Message}");
                return dependencies;
            }
        }

        /// <inheritdoc/>
        public List<ParseError> ValidateStructDefinition(CStruct structDef)
        {
            var errors = new List<ParseError>();

            try
            {
                if (structDef == null)
                {
                    errors.Add(CreateValidationError("Null struct definition", 0, 0));
                    return errors;
                }

                _logger.LogDebug($"Validating struct definition: {structDef.Name}");

                // Validate name
                ValidateStructName(structDef, errors);

                // Validate fields
                ValidateStructFields(structDef, errors);

                // Check for naming conventions
                ValidateNamingConventions(structDef, errors);

                // Check for memory layout issues
                ValidateMemoryLayout(structDef, errors);

                // Check for potential issues with bit fields
                ValidateBitFields(structDef, errors);

                _logger.LogDebug($"Validation completed for struct: {structDef.Name}, found {errors.Count} issues");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating struct definition {structDef?.Name}: {ex.Message}");
                errors.Add(CreateValidationError($"Validation error: {ex.Message}",
                    structDef?.LineNumber ?? 0, structDef?.ColumnNumber ?? 0));
            }

            return errors;
        }

        /// <inheritdoc/>
        public async Task<List<StructConstraint>> ExtractStructConstraintsAsync(
            CStruct structDef,
            string sourceCode)
        {
            _logger.LogInformation($"Extracting constraints for struct {structDef.Name}");

            var constraints = new List<StructConstraint>();

            try
            {
                // Extract usage patterns
                var usagePatterns = await ExtractUsagePatternsAsync(structDef, sourceCode);
                constraints.AddRange(usagePatterns);

                // Extract size constraints
                var sizeConstraints = ExtractSizeConstraints(structDef);
                constraints.AddRange(sizeConstraints);

                // Extract alignment constraints
                var alignmentConstraints = ExtractAlignmentConstraints(structDef);
                constraints.AddRange(alignmentConstraints);

                // Extract packing constraints
                var packingConstraints = ExtractPackingConstraints(structDef);
                constraints.AddRange(packingConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for struct {structDef.Name}");
                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for struct {structDef.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <inheritdoc/>
        public StructMemoryLayout CalculateMemoryLayout(CStruct structDef)
        {
            _logger.LogDebug($"Calculating memory layout for struct: {structDef.Name}");

            var layout = new StructMemoryLayout
            {
                StructName = structDef.Name,
                Alignment = 1
            };

            try
            {
                int currentOffset = 0;
                int maxAlignment = 1;
                bool hasBitFields = false;

                foreach (var field in structDef.Fields)
                {
                    var fieldLayout = new FieldLayout
                    {
                        FieldName = field.Name,
                        IsBitField = field.IsBitField,
                        BitWidth = field.BitWidth
                    };

                    if (field.IsBitField)
                    {
                        hasBitFields = true;
                        // Simplified bit field layout calculation
                        fieldLayout.BitOffset = currentOffset * 8; // Convert to bits
                        fieldLayout.Offset = currentOffset;
                        fieldLayout.Size = (field.BitWidth + 7) / 8; // Round up to bytes
                    }
                    else
                    {
                        // Calculate field alignment
                        int fieldAlignment = CalculateFieldAlignment(field);
                        maxAlignment = Math.Max(maxAlignment, fieldAlignment);

                        // Add padding before field if necessary
                        int padding = (fieldAlignment - (currentOffset % fieldAlignment)) % fieldAlignment;
                        currentOffset += padding;

                        fieldLayout.Offset = currentOffset;
                        fieldLayout.Size = CalculateFieldSize(field);
                        currentOffset += fieldLayout.Size;
                    }

                    layout.FieldLayouts.Add(fieldLayout);
                }

                // Apply struct packing rules
                if (!structDef.IsPacked)
                {
                    layout.Alignment = maxAlignment;
                    // Add final padding to align struct size to its alignment
                    int finalPadding = (maxAlignment - (currentOffset % maxAlignment)) % maxAlignment;
                    layout.PaddingBytes = finalPadding;
                    layout.TotalSize = currentOffset + finalPadding;
                }
                else
                {
                    layout.Alignment = 1;
                    layout.TotalSize = currentOffset;
                }

                layout.HasBitFields = hasBitFields;

                _logger.LogDebug($"Calculated layout for {structDef.Name}: Size={layout.TotalSize}, Alignment={layout.Alignment}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating memory layout for struct {structDef.Name}: {ex.Message}");
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
        /// Extract struct documentation from cursor
        /// </summary>
        private unsafe string ExtractStructDocumentation(CXCursor cursor)
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
                _logger.LogWarning($"Could not extract struct documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract struct attributes
        /// </summary>
        private unsafe List<CStructAttribute> ExtractStructAttributes(CXCursor cursor)
        {
            var attributes = new List<CStructAttribute>();

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
                            var attribute = ParseAttribute(attrText);
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
                _logger.LogWarning($"Error extracting struct attributes: {ex.Message}");
            }

            return attributes;
        }

        /// <summary>
        /// Parse attribute string into CStructAttribute
        /// </summary>
        private CStructAttribute ParseAttribute(string attrText)
        {
            try
            {
                var match = Regex.Match(attrText, @"(\w+)(?:\((.*?)\))?");
                if (match.Success)
                {
                    var attribute = new CStructAttribute
                    {
                        Name = match.Groups[1].Value
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
                _logger.LogWarning($"Error parsing attribute '{attrText}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Check if struct is packed
        /// </summary>
        private bool IsPackedStruct(CXCursor cursor, List<CStructAttribute> attributes)
        {
            try
            {
                // Check attributes for packing indicators
                foreach (var attr in attributes)
                {
                    if (PackingAttributes.Contains(attr.Name) ||
                        attr.Name.Contains("pack") ||
                        attr.Name.Contains("packed"))
                    {
                        return true;
                    }
                }

                // Additional checks could be added here for pragma pack detection
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error checking packed struct: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extract struct fields using VisitChildren
        /// </summary>
        private unsafe void ExtractStructFields(CXCursor cursor, CStruct structDef)
        {
            try
            {
                cursor.VisitChildren((childCursor, parent, clientData) =>
                {
                    if (childCursor.Kind == CXCursorKind.CXCursor_FieldDecl)
                    {
                        var field = ExtractFieldFromCursor(childCursor);
                        if (field != null)
                        {
                            structDef.Fields.Add(field);
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting struct fields for {structDef.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract field from cursor
        /// </summary>
        private unsafe CStructField ExtractFieldFromCursor(CXCursor cursor)
        {
            try
            {
                string fieldName = cursor.Spelling.ToString();
                var fieldType = cursor.Type;

                // Get field location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                var field = new CStructField
                {
                    Name = fieldName,
                    FieldType = fieldType.Spelling.ToString(),
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    Size = (int)fieldType.SizeOf,
                    IsConst = fieldType.IsConstQualified,
                    IsVolatile = fieldType.IsVolatileQualified,
                    IsPointer = fieldType.kind == CXTypeKind.CXType_Pointer,
                    IsArray = fieldType.kind == CXTypeKind.CXType_ConstantArray
                };

                // Check for bit field
                if (cursor.IsBitField)
                {
                    field.IsBitField = true;
                    field.BitWidth = cursor.FieldDeclBitWidth;
                }

                // Extract array dimensions if it's an array
                if (field.IsArray)
                {
                    field.ArrayDimensions = ExtractArrayDimensions(fieldType);
                }

                // Extract default value if present
                ExtractFieldDefaultValue(cursor, field);

                return field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting field from cursor: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract default value for field
        /// </summary>
        private unsafe void ExtractFieldDefaultValue(CXCursor cursor, CStructField field)
        {
            try
            {
                string defaultValue = null;

                cursor.VisitChildren((child, parent, clientData) =>
                {
                    switch (child.Kind)
                    {
                        case CXCursorKind.CXCursor_IntegerLiteral:
                        case CXCursorKind.CXCursor_FloatingLiteral:
                        case CXCursorKind.CXCursor_StringLiteral:
                        case CXCursorKind.CXCursor_CharacterLiteral:
                            defaultValue = GetLiteralValue(child);
                            return CXChildVisitResult.CXChildVisit_Break;
                        case CXCursorKind.CXCursor_InitListExpr:
                            defaultValue = ExtractInitListValue(child);
                            return CXChildVisitResult.CXChildVisit_Break;
                        default:
                            return CXChildVisitResult.CXChildVisit_Continue;
                    }
                }, default(CXClientData));

                // Store default value in documentation or comments since CStructField doesn't have DefaultValue property
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    // Could be stored in a future property or handled differently
                    _logger.LogDebug($"Found default value for field {field.Name}: {defaultValue}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting default value for field {field.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get literal value from cursor - simplified version
        /// </summary>
        private unsafe string GetLiteralValue(CXCursor cursor)
        {
            try
            {
                // For most cases, we can get the value from cursor spelling
                var spelling = cursor.Spelling.ToString();
                if (!string.IsNullOrEmpty(spelling))
                {
                    return spelling;
                }

                // Alternative: try to use libclang API directly if available
                // This is a fallback that doesn't rely on tokenization
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting literal value: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract initialization list value
        /// </summary>
        private unsafe string ExtractInitListValue(CXCursor cursor)
        {
            try
            {
                var sb = new StringBuilder("{");
                var values = new List<string>();

                cursor.VisitChildren((child, parent, clientData) =>
                {
                    var value = GetLiteralValue(child);
                    if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));

                sb.Append(string.Join(", ", values));
                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting init list value: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract array dimensions from type
        /// </summary>
        private List<int> ExtractArrayDimensions(CXType arrayType)
        {
            var dimensions = new List<int>();

            try
            {
                var currentType = arrayType;
                while (currentType.kind == CXTypeKind.CXType_ConstantArray)
                {
                    long size = currentType.ArraySize;
                    dimensions.Add((int)size);
                    currentType = currentType.ArrayElementType;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting array dimensions: {ex.Message}");
            }

            return dimensions;
        }

        /// <summary>
        /// Analyze struct dependencies
        /// </summary>
        private List<string> AnalyzeStructDependencies(CStruct structDef, List<CStruct> allStructs)
        {
            var dependencies = new HashSet<string>();

            try
            {
                foreach (var field in structDef.Fields)
                {
                    // Remove pointer/array indicators to get base type
                    string baseType = ExtractBaseTypeName(field.FieldType);

                    // Check if this type is another struct
                    if (allStructs.Any(s => s.Name == baseType) && baseType != structDef.Name)
                    {
                        dependencies.Add(baseType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing dependencies for struct {structDef.Name}: {ex.Message}");
            }

            return dependencies.ToList();
        }

        /// <summary>
        /// Extract base type name from complex type string
        /// </summary>
        private string ExtractBaseTypeName(string fieldType)
        {
            try
            {
                // Remove common qualifiers and modifiers
                string cleanType = fieldType
                    .Replace("const", "")
                    .Replace("volatile", "")
                    .Replace("struct", "")
                    .Replace("*", "")
                    .Replace("[", "")
                    .Replace("]", "")
                    .Trim();

                // Extract the first identifier
                var match = Regex.Match(cleanType, @"([A-Za-z_][A-Za-z0-9_]*)");
                return match.Success ? match.Groups[1].Value : cleanType;
            }
            catch
            {
                return fieldType;
            }
        }

        /// <summary>
        /// Find direct dependencies of a struct
        /// </summary>
        private List<string> FindDirectDependencies(CStruct structDef, List<CStruct> allStructs)
        {
            var dependencies = new List<string>();

            try
            {
                if (structDef.Dependencies != null)
                {
                    dependencies.AddRange(structDef.Dependencies);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding dependencies for struct {structDef.Name}: {ex.Message}");
            }

            return dependencies;
        }

        /// <summary>
        /// Detect circular dependencies
        /// </summary>
        private List<string> DetectCircularDependencies(CStruct structDef, List<CStruct> allStructs)
        {
            var circularDeps = new List<string>();

            try
            {
                var visited = new HashSet<string>();
                var currentPath = new HashSet<string>();

                DetectCircularDependenciesRecursive(structDef.Name, allStructs, visited, currentPath, circularDeps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting circular dependencies for struct {structDef.Name}: {ex.Message}");
            }

            return circularDeps;
        }

        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private void DetectCircularDependenciesRecursive(
            string structName,
            List<CStruct> allStructs,
            HashSet<string> visited,
            HashSet<string> currentPath,
            List<string> circularDeps)
        {
            if (currentPath.Contains(structName))
            {
                if (!circularDeps.Contains(structName))
                {
                    circularDeps.Add(structName);
                }
                return;
            }

            if (visited.Contains(structName))
                return;

            visited.Add(structName);
            currentPath.Add(structName);

            var structDef = allStructs.FirstOrDefault(s => s.Name == structName);
            if (structDef?.Dependencies != null)
            {
                foreach (var dependency in structDef.Dependencies)
                {
                    DetectCircularDependenciesRecursive(dependency, allStructs, visited, currentPath, circularDeps);
                }
            }

            currentPath.Remove(structName);
        }

        /// <summary>
        /// Find undefined dependencies
        /// </summary>
        private List<string> FindUndefinedDependencies(CStruct structDef, List<CStruct> allStructs)
        {
            var undefinedDeps = new List<string>();

            try
            {
                if (structDef.Dependencies != null)
                {
                    foreach (var dependency in structDef.Dependencies)
                    {
                        if (!allStructs.Any(s => s.Name == dependency) && !SystemTypes.Contains(dependency))
                        {
                            undefinedDeps.Add(dependency);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding undefined dependencies for struct {structDef.Name}: {ex.Message}");
            }

            return undefinedDeps;
        }

        /// <summary>
        /// Validate struct name
        /// </summary>
        private void ValidateStructName(CStruct structDef, List<ParseError> errors)
        {
            try
            {
                if (string.IsNullOrEmpty(structDef.Name))
                {
                    errors.Add(CreateValidationError("Struct name cannot be empty",
                        structDef.LineNumber, structDef.ColumnNumber));
                    return;
                }

                if (!StructNameRegex.IsMatch(structDef.Name))
                {
                    errors.Add(CreateValidationError($"Invalid struct name '{structDef.Name}'. Must start with letter or underscore.",
                        structDef.LineNumber, structDef.ColumnNumber));
                }

                if (SystemTypes.Contains(structDef.Name))
                {
                    errors.Add(CreateValidationError($"Struct name '{structDef.Name}' conflicts with system type",
                        structDef.LineNumber, structDef.ColumnNumber, ErrorSeverity.Warning));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error validating struct name: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate struct fields
        /// </summary>
        private void ValidateStructFields(CStruct structDef, List<ParseError> errors)
        {
            try
            {
                if (structDef.Fields == null || structDef.Fields.Count == 0)
                {
                    if (!structDef.IsForwardDeclaration)
                    {
                        errors.Add(CreateValidationError($"Struct '{structDef.Name}' has no fields",
                            structDef.LineNumber, structDef.ColumnNumber, ErrorSeverity.Warning));
                    }
                    return;
                }

                var fieldNames = new HashSet<string>();
                foreach (var field in structDef.Fields)
                {
                    // Check for duplicate field names
                    if (fieldNames.Contains(field.Name))
                    {
                        errors.Add(CreateValidationError($"Duplicate field name '{field.Name}' in struct '{structDef.Name}'",
                            field.LineNumber, field.ColumnNumber));
                    }
                    else
                    {
                        fieldNames.Add(field.Name);
                    }

                    // Validate field name
                    if (!FieldNameRegex.IsMatch(field.Name))
                    {
                        errors.Add(CreateValidationError($"Invalid field name '{field.Name}' in struct '{structDef.Name}'",
                            field.LineNumber, field.ColumnNumber));
                    }

                    // Validate field type
                    if (string.IsNullOrEmpty(field.FieldType))
                    {
                        errors.Add(CreateValidationError($"Field '{field.Name}' has no type specified",
                            field.LineNumber, field.ColumnNumber));
                    }

                    // Validate bit field constraints
                    if (field.IsBitField)
                    {
                        if (field.BitWidth <= 0 || field.BitWidth > 64)
                        {
                            errors.Add(CreateValidationError($"Invalid bit width {field.BitWidth} for field '{field.Name}'",
                                field.LineNumber, field.ColumnNumber));
                        }
                    }

                    // Validate array dimensions
                    if (field.IsArray && field.ArrayDimensions != null)
                    {
                        foreach (var dimension in field.ArrayDimensions)
                        {
                            if (dimension <= 0)
                            {
                                errors.Add(CreateValidationError($"Invalid array dimension {dimension} for field '{field.Name}'",
                                    field.LineNumber, field.ColumnNumber));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error validating struct fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate naming conventions
        /// </summary>
        private void ValidateNamingConventions(CStruct structDef, List<ParseError> errors)
        {
            try
            {
                // Check if struct name follows common conventions
                if (!structDef.IsAnonymous)
                {
                    // Warn about all lowercase names (common convention is PascalCase or snake_case)
                    if (structDef.Name.All(char.IsLower))
                    {
                        errors.Add(CreateValidationError($"Struct name '{structDef.Name}' should follow naming convention (PascalCase or snake_case)",
                            structDef.LineNumber, structDef.ColumnNumber, ErrorSeverity.Info));
                    }
                }

                // Check field naming conventions
                foreach (var field in structDef.Fields)
                {
                    // Check for Hungarian notation (discouraged in modern C)
                    var hungarianPrefixes = new[] { "sz", "str", "p", "lp", "n", "i", "dw", "ul" };
                    if (hungarianPrefixes.Any(prefix => field.Name.StartsWith(prefix) && field.Name.Length > prefix.Length && char.IsUpper(field.Name[prefix.Length])))
                    {
                        errors.Add(CreateValidationError($"Field '{field.Name}' uses Hungarian notation which is discouraged",
                            field.LineNumber, field.ColumnNumber, ErrorSeverity.Info));
                    }

                    // Check for very long field names
                    if (field.Name.Length > 50)
                    {
                        errors.Add(CreateValidationError($"Field name '{field.Name}' is very long ({field.Name.Length} characters)",
                            field.LineNumber, field.ColumnNumber, ErrorSeverity.Info));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error validating naming conventions: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate memory layout
        /// </summary>
        private void ValidateMemoryLayout(CStruct structDef, List<ParseError> errors)
        {
            try
            {
                if (structDef.Fields == null || structDef.Fields.Count == 0)
                    return;

                // Check for potential alignment issues
                var layout = CalculateMemoryLayout(structDef);

                // Warn about very large structs
                if (layout.TotalSize > 1024) // 1KB
                {
                    errors.Add(CreateValidationError($"Struct '{structDef.Name}' is very large ({layout.TotalSize} bytes)",
                        structDef.LineNumber, structDef.ColumnNumber, ErrorSeverity.Info));
                }

                // Check for inefficient field ordering (larger fields should come first for better packing)
                for (int i = 0; i < structDef.Fields.Count - 1; i++)
                {
                    var currentField = structDef.Fields[i];
                    var nextField = structDef.Fields[i + 1];

                    if (!currentField.IsBitField && !nextField.IsBitField)
                    {
                        int currentSize = CalculateFieldSize(currentField);
                        int nextSize = CalculateFieldSize(nextField);

                        if (currentSize < nextSize && nextSize >= 4) // Only warn for significant size differences
                        {
                            errors.Add(CreateValidationError($"Field ordering in struct '{structDef.Name}' may cause padding. Consider placing larger fields first.",
                                currentField.LineNumber, currentField.ColumnNumber, ErrorSeverity.Info));
                            break; // Only warn once per struct
                        }
                    }
                }

                // Warn about excessive padding
                if (layout.PaddingBytes > layout.TotalSize / 4) // More than 25% padding
                {
                    errors.Add(CreateValidationError($"Struct '{structDef.Name}' has excessive padding ({layout.PaddingBytes} bytes). Consider reordering fields.",
                        structDef.LineNumber, structDef.ColumnNumber, ErrorSeverity.Info));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error validating memory layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate bit fields
        /// </summary>
        private void ValidateBitFields(CStruct structDef, List<ParseError> errors)
        {
            try
            {
                var bitFields = structDef.Fields.Where(f => f.IsBitField).ToList();
                if (bitFields.Count == 0)
                    return;

                // Group consecutive bit fields to check for boundary issues
                var bitFieldGroups = new List<List<CStructField>>();
                List<CStructField> currentGroup = null;

                foreach (var field in structDef.Fields)
                {
                    if (field.IsBitField)
                    {
                        if (currentGroup == null)
                        {
                            currentGroup = new List<CStructField>();
                            bitFieldGroups.Add(currentGroup);
                        }
                        currentGroup.Add(field);
                    }
                    else
                    {
                        currentGroup = null;
                    }
                }

                // Validate each bit field group
                foreach (var group in bitFieldGroups)
                {
                    int totalBits = group.Sum(f => f.BitWidth);

                    // Warn about bit fields spanning multiple storage units
                    if (totalBits > 32) // Assuming 32-bit storage units
                    {
                        var firstField = group.First();
                        errors.Add(CreateValidationError($"Bit field group starting at '{firstField.Name}' spans multiple storage units ({totalBits} bits)",
                            firstField.LineNumber, firstField.ColumnNumber, ErrorSeverity.Info));
                    }

                    // Check for zero-width bit fields in the middle of a group
                    for (int i = 1; i < group.Count - 1; i++)
                    {
                        if (group[i].BitWidth == 0)
                        {
                            errors.Add(CreateValidationError($"Zero-width bit field '{group[i].Name}' in middle of bit field group",
                                group[i].LineNumber, group[i].ColumnNumber, ErrorSeverity.Warning));
                        }
                    }
                }

                // Check for mixed signed/unsigned bit fields
                var signedBitFields = bitFields.Where(f => f.FieldType.Contains("signed") ||
                    (f.FieldType.Contains("int") && !f.FieldType.Contains("unsigned"))).ToList();
                var unsignedBitFields = bitFields.Where(f => f.FieldType.Contains("unsigned")).ToList();

                if (signedBitFields.Any() && unsignedBitFields.Any())
                {
                    var firstField = bitFields.First();
                    errors.Add(CreateValidationError($"Struct '{structDef.Name}' mixes signed and unsigned bit fields",
                        firstField.LineNumber, firstField.ColumnNumber, ErrorSeverity.Info));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error validating bit fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract usage patterns from source code
        /// </summary>
        private async Task<List<StructConstraint>> ExtractUsagePatternsAsync(CStruct structDef, string sourceCode)
        {
            var constraints = new List<StructConstraint>();

            try
            {
                await Task.Run(() =>
                {
                    // Look for malloc/free patterns
                    var mallocPattern = new Regex($@"malloc\s*\(\s*sizeof\s*\(\s*{Regex.Escape(structDef.Name)}\s*\)", RegexOptions.IgnoreCase);
                    if (mallocPattern.IsMatch(sourceCode))
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.UsagePattern,
                            Value = "DynamicAllocation",
                            Source = "malloc pattern analysis"
                        });
                    }

                    // Look for stack allocation patterns
                    var stackPattern = new Regex($@"{Regex.Escape(structDef.Name)}\s+\w+\s*[;=]", RegexOptions.IgnoreCase);
                    if (stackPattern.IsMatch(sourceCode))
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.UsagePattern,
                            Value = "StackAllocation",
                            Source = "stack allocation pattern analysis"
                        });
                    }

                    // Look for array usage patterns
                    var arrayPattern = new Regex($@"{Regex.Escape(structDef.Name)}\s+\w+\s*\[\s*\d+\s*\]", RegexOptions.IgnoreCase);
                    if (arrayPattern.IsMatch(sourceCode))
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.UsagePattern,
                            Value = "ArrayUsage",
                            Source = "array pattern analysis"
                        });
                    }

                    // Look for pointer usage
                    var pointerPattern = new Regex($@"{Regex.Escape(structDef.Name)}\s*\*", RegexOptions.IgnoreCase);
                    if (pointerPattern.IsMatch(sourceCode))
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.UsagePattern,
                            Value = "PointerUsage",
                            Source = "pointer pattern analysis"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting usage patterns: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract size constraints
        /// </summary>
        private List<StructConstraint> ExtractSizeConstraints(CStruct structDef)
        {
            var constraints = new List<StructConstraint>();

            try
            {
                if (structDef.Size > 0)
                {
                    if (structDef.Size > 1024) // Large struct
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.SizeConstraint,
                            Value = $"LargeSize:{structDef.Size}",
                            Source = "size analysis"
                        });
                    }
                    else if (structDef.Size < 4) // Very small struct
                    {
                        constraints.Add(new StructConstraint
                        {
                            StructName = structDef.Name,
                            ConstraintType = StructConstraintType.SizeConstraint,
                            Value = $"SmallSize:{structDef.Size}",
                            Source = "size analysis"
                        });
                    }
                }

                // Check for empty structs
                if (structDef.Fields?.Count == 0)
                {
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.FieldCount,
                        Value = "EmptyStruct:0",
                        Source = "field count analysis"
                    });
                }
                else if (structDef.Fields != null)
                {
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.FieldCount,
                        Value = $"FieldCount:{structDef.Fields.Count}",
                        Source = "field count analysis"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting size constraints: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract alignment constraints
        /// </summary>
        private List<StructConstraint> ExtractAlignmentConstraints(CStruct structDef)
        {
            var constraints = new List<StructConstraint>();

            try
            {
                if (structDef.Alignment > 8)
                {
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.AlignmentConstraint,
                        Value = $"HighAlignment:{structDef.Alignment}",
                        Source = "alignment analysis"
                    });
                }

                // Check for potential alignment issues with bit fields
                if (structDef.Fields?.Any(f => f.IsBitField) == true)
                {
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.BitFieldConstraint,
                        Value = "ContainsBitFields",
                        Source = "bit field analysis"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting alignment constraints: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract packing constraints
        /// </summary>
        private List<StructConstraint> ExtractPackingConstraints(CStruct structDef)
        {
            var constraints = new List<StructConstraint>();

            try
            {
                if (structDef.IsPacked)
                {
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.PackingConstraint,
                        Value = "IsPacked",
                        Source = "packing analysis"
                    });

                    // Packed structs may have performance implications
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.PackingConstraint,
                        Value = "PackedPerformanceWarning",
                        Source = "packing performance analysis"
                    });
                }

                // Check for potential packing issues
                if (structDef.Attributes?.Any(a => a.Name.Contains("pack")) == true)
                {
                    var packAttr = structDef.Attributes.First(a => a.Name.Contains("pack"));
                    constraints.Add(new StructConstraint
                    {
                        StructName = structDef.Name,
                        ConstraintType = StructConstraintType.PackingConstraint,
                        Value = $"ExplicitPacking:{packAttr.Name}",
                        Source = "attribute analysis"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting packing constraints: {ex.Message}");
            }

            return constraints;
        }
        /// <summary>
        /// Calculate field alignment
        /// </summary>
        private int CalculateFieldAlignment(CStructField field)
        {
            try
            {
                if (field.IsBitField)
                {
                    return 1; // Bit fields typically don't add alignment requirements
                }

                if (field.IsPointer)
                {
                    return IntPtr.Size; // Pointer alignment (4 bytes on 32-bit, 8 bytes on 64-bit)
                }

                // Get base type for arrays
                string baseType = field.FieldType;
                if (field.IsArray)
                {
                    baseType = ExtractBaseTypeName(field.FieldType);
                }

                // Look up in type sizes dictionary
                if (TypeSizes.TryGetValue(baseType.ToLower(), out int size))
                {
                    return Math.Min(size, IntPtr.Size); // Alignment is typically size or pointer size, whichever is smaller
                }

                // For unknown types, assume pointer-sized alignment
                return IntPtr.Size;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error calculating field alignment for {field.Name}: {ex.Message}");
                return 1; // Default to byte alignment
            }
        }

        /// <summary>
        /// Calculate field size
        /// </summary>
        private int CalculateFieldSize(CStructField field)
        {
            try
            {
                if (field.IsBitField)
                {
                    return (field.BitWidth + 7) / 8; // Round up to bytes
                }

                if (field.IsPointer)
                {
                    return IntPtr.Size;
                }

                if (field.IsArray && field.ArrayDimensions != null)
                {
                    string baseType = ExtractBaseTypeName(field.FieldType);
                    int baseSize = GetBaseTypeSize(baseType);
                    int totalElements = field.ArrayDimensions.Aggregate(1, (a, b) => a * b);
                    return baseSize * totalElements;
                }

                // Use the size from ClangSharp if available
                if (field.Size > 0)
                {
                    return field.Size;
                }

                // Fallback to type lookup
                return GetBaseTypeSize(field.FieldType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error calculating field size for {field.Name}: {ex.Message}");
                return 4; // Default size
            }
        }

        /// <summary>
        /// Get base type size
        /// </summary>
        private int GetBaseTypeSize(string typeName)
        {
            try
            {
                string cleanType = typeName.ToLower()
                    .Replace("const", "")
                    .Replace("volatile", "")
                    .Replace("struct", "")
                    .Trim();

                if (TypeSizes.TryGetValue(cleanType, out int size))
                {
                    return size;
                }

                // Default sizes for common patterns
                if (cleanType.Contains("char")) return 1;
                if (cleanType.Contains("short")) return 2;
                if (cleanType.Contains("long long")) return 8;
                if (cleanType.Contains("long")) return 8;
                if (cleanType.Contains("int")) return 4;
                if (cleanType.Contains("float")) return 4;
                if (cleanType.Contains("double")) return 8;

                // Default for unknown types
                return 4;
            }
            catch
            {
                return 4; // Default size
            }
        }

        /// <summary>
        /// Create validation error
        /// </summary>
        private ParseError CreateValidationError(string message, int line, int column,
            ErrorSeverity severity = ErrorSeverity.Error)
        {
            return new ParseError
            {
                Message = message,
                LineNumber = line,
                ColumnNumber = column,
                Severity = severity
            };
        }

        #endregion
    }
}
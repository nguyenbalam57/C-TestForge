using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.CodeAnalysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the function analysis service
    /// </summary>
    public class FunctionAnalysisService : IFunctionAnalysisService
    {
        private readonly ILogger<FunctionAnalysisService> _logger;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IFileService _fileService;

        // Cache for function signatures to avoid duplicate processing
        private readonly Dictionary<string, CFunction> _functionCache = new Dictionary<string, CFunction>();

        /// <summary>
        /// Constructor for FunctionAnalysisService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sourceCodeService">Source code service</param>
        /// <param name="fileService">File service</param>
        public FunctionAnalysisService(
            ILogger<FunctionAnalysisService> logger,
            ISourceCodeService sourceCodeService,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public unsafe void ExtractFunction(CXCursor cursor, SourceFile sourceFile)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_FunctionDecl)
                {
                    return ;
                }

                string functionName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting function: {functionName}");

                // Check cache first
                string cacheKey = $"{functionName}_{cursor.Location.GetHashCode()}";
                if (_functionCache.TryGetValue(cacheKey, out var cachedFunction))
                {
                    sourceFile.ParseResult.Functions.Add(cachedFunction.Clone());

                    return;
                }

                // Get function location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceName = file != null ? Path.GetFileName(file.Name.ToString()) : null;

                // Get function extent for end position
                var extent = cursor.Extent;
                uint endLine, endColumn, endOffset;
                extent.End.GetFileLocation(out file, out endLine, out endColumn, out endOffset);

                // Get return type
                var returnType = cursor.ResultType;
                string returnTypeName = returnType.Spelling.ToString();

                // Check function attributes using storage class
                bool isStatic = IsStaticFunction(cursor);
                bool isInline = IsInlineFunction(cursor);
                bool isExternal = IsExternalFunction(cursor);
                bool isVariadic = IsVariadicFunction(cursor);

                // Get function parameters
                List<CParameter> parameters = ExtractParameters(cursor);

                // Extract function body
                string body = ExtractFunctionBody(cursor, sourceFile.Content);
                bool isDeclarationOnly = string.IsNullOrWhiteSpace(body) || !body.Contains("{");

                // Extract called functions and used variables
                var (calledFunctions, usedVariables) = ExtractFunctionCallsAndVariables(body);

                // Extract local variables from function body
                var localVariables = ExtractLocalVariables(body, (int)line);

                // Extract function documentation
                string documentation = ExtractFunctionDocumentation(cursor);

                // Extract attributes
                var attributes = ExtractFunctionAttributes(cursor);

                // Calculate cyclomatic complexity
                int cyclomaticComplexity = CalculateCyclomaticComplexity(body);

                // Create function object
                var function = new CFunction
                {
                    Name = functionName,
                    ReturnType = returnTypeName,
                    Parameters = parameters,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    StartLineNumber = (int)line,
                    EndLineNumber = (int)endLine,
                    SourceFile = sourceName,
                    IsStatic = isStatic,
                    IsInline = isInline,
                    IsExternal = isExternal,
                    IsVariadic = isVariadic,
                    Linkage = DetermineFunctionLinkage(isStatic, isExternal),
                    Body = body,
                    CalledFunctions = calledFunctions,
                    UsedVariables = usedVariables,
                    LocalVariables = localVariables,
                    Attributes = attributes,
                    Documentation = documentation,
                    CyclomaticComplexity = cyclomaticComplexity,
                    IsDeclarationOnly = isDeclarationOnly
                };

                // Cache the function
                _functionCache[cacheKey] = function.Clone();

                sourceFile.ParseResult.Functions.Add(function);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function: {ex.Message}");
                return ;
            }
        }

        /// <summary>
        /// Checks if a function has static storage class
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>True if the function is static</returns>
        private unsafe bool IsStaticFunction(CXCursor cursor)
        {
            // Check storage class using CX_StorageClass enum
            var storageClass = clang.Cursor_getStorageClass(cursor);
            return storageClass == CX_StorageClass.CX_SC_Static;
        }

        /// <summary>
        /// Checks if a function is declared as inline
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>True if the function is inline</returns>
        private unsafe bool IsInlineFunction(CXCursor cursor)
        {
            // First, check if the function has the inline keyword by examining tokens
            bool isInline = false;
            uint numTokens = 0;
            CXToken* tokens = null;

            try
            {
                // Get tokens for the function declaration
                clang.tokenize(cursor.TranslationUnit, cursor.Extent, &tokens, &numTokens);

                // Look for "inline" token
                for (uint i = 0; i < numTokens; i++)
                {
                    var token = tokens[i];
                    var spelling = clang.getTokenSpelling(cursor.TranslationUnit, token).ToString();
                    if (spelling == "inline")
                    {
                        isInline = true;
                        break;
                    }
                }
            }
            finally
            {
                // Free token memory
                if (tokens != null && numTokens > 0)
                {
                    clang.disposeTokens(cursor.TranslationUnit, tokens, numTokens);
                }
            }

            return isInline;
        }

        /// <summary>
        /// Checks if a function is declared as extern
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>True if the function is extern</returns>
        private unsafe bool IsExternalFunction(CXCursor cursor)
        {
            // Check storage class
            var storageClass = clang.Cursor_getStorageClass(cursor);
            return storageClass == CX_StorageClass.CX_SC_Extern;
        }

        /// <summary>
        /// Checks if a function is variadic (has variable arguments)
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>True if the function is variadic</returns>
        private unsafe bool IsVariadicFunction(CXCursor cursor)
        {
            var functionType = cursor.Type;
            return clang.isFunctionTypeVariadic(functionType) != 0;
        }

        /// <summary>
        /// Determines function linkage based on storage class
        /// </summary>
        /// <param name="isStatic">Whether function is static</param>
        /// <param name="isExternal">Whether function is external</param>
        /// <returns>Function linkage</returns>
        private FunctionLinkage DetermineFunctionLinkage(bool isStatic, bool isExternal)
        {
            if (isStatic)
                return FunctionLinkage.Internal;
            else if (isExternal)
                return FunctionLinkage.External;
            else
                return FunctionLinkage.External; // Default for C functions
        }

        /// <inheritdoc/>
        public async Task<List<FunctionRelationship>> AnalyzeFunctionRelationshipsAsync(List<CFunction> functions)
        {
            _logger.LogInformation($"Analyzing relationships between {functions.Count} functions");

            var relationships = new List<FunctionRelationship>();

            try
            {
                // Build a dictionary of functions for quick lookup
                var functionDict = functions.ToDictionary(f => f.Name, f => f);

                // Analyze each function for calls to other functions
                foreach (var function in functions)
                {
                    _logger.LogDebug($"Analyzing function calls for: {function.Name}");

                    foreach (var calledFunctionName in function.CalledFunctions)
                    {
                        // Check if the called function is in our list
                        if (functionDict.TryGetValue(calledFunctionName, out var calledFunction))
                        {
                            // Create a relationship
                            var relationship = new FunctionRelationship
                            {
                                CallerName = function.Name,
                                CalleeName = calledFunctionName,
                                SourceFile = function.SourceFile,
                                LineNumber = function.LineNumber
                            };

                            // Check if this relationship already exists
                            if (!relationships.Any(r =>
                                r.CallerName == relationship.CallerName &&
                                r.CalleeName == relationship.CalleeName))
                            {
                                relationships.Add(relationship);
                            }
                        }
                    }
                }

                _logger.LogInformation($"Found {relationships.Count} function relationships");

                return relationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing function relationships: {ex.Message}");
                return relationships;
            }
        }

        /// <inheritdoc/>
        public async Task<ControlFlowGraph> ExtractControlFlowGraphAsync(CFunction function)
        {
            _logger.LogInformation($"Extracting control flow graph for function: {function.Name}");

            var graph = new ControlFlowGraph
            {
                FunctionName = function.Name
            };

            try
            {
                if (string.IsNullOrEmpty(function.Body))
                {
                    return graph;
                }

                // Split the function body into lines for analysis
                string[] lines = function.Body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Create nodes for control structures using a more sophisticated approach
                var nodeStack = new Stack<ControlFlowNode>();
                int nodeId = 0;

                // Entry node
                var entryNode = new ControlFlowNode
                {
                    Id = $"node_{nodeId++}",
                    NodeType = "Entry",
                    LineNumber = function.LineNumber,
                    Code = $"Entry: {function.Name}"
                };
                graph.Nodes.Add(entryNode);
                nodeStack.Push(entryNode);

                // Exit node (will be added at the end)
                var exitNode = new ControlFlowNode
                {
                    Id = $"node_{nodeId++}",
                    NodeType = "Exit",
                    LineNumber = function.EndLineNumber,
                    Code = $"Exit: {function.Name}"
                };

                // Track current node and branching context
                var currentNode = entryNode;
                var branchStack = new Stack<(ControlFlowNode node, string branchType)>();

                // Process function body with better control flow detection
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (string.IsNullOrWhiteSpace(line) || IsComment(line))
                    {
                        continue; // Skip comments and empty lines
                    }

                    var newNode = ProcessControlFlowLine(line, function.LineNumber + i, ref nodeId);
                    if (newNode != null)
                    {
                        graph.Nodes.Add(newNode);

                        // Add appropriate edges based on node type
                        AddControlFlowEdge(graph, currentNode, newNode, DetermineEdgeType(newNode.NodeType));

                        // Handle branching structures
                        HandleBranchingLogic(graph, newNode, branchStack, ref nodeId);

                        currentNode = newNode;
                    }
                }

                // Connect to exit node if not already connected
                if (!graph.Edges.Any(e => e.TargetId == exitNode.Id))
                {
                    graph.Nodes.Add(exitNode);
                    AddControlFlowEdge(graph, currentNode, exitNode, "Unconditional");
                }

                _logger.LogInformation($"Generated control flow graph with {graph.Nodes.Count} nodes and {graph.Edges.Count} edges");

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting control flow graph for function {function.Name}: {ex.Message}");
                return graph;
            }
        }

        /// <inheritdoc/>
        public async Task AnalyzeFunctionComplexityAsync(CFunction function, SourceFile sourceFile)
        {
            _logger.LogInformation($"Analyzing complexity for function: {function.Name}");

            try
            {
                if (string.IsNullOrEmpty(function.Body))
                {
                    return;
                }

                // Calculate cyclomatic complexity (already done during extraction)
                int complexity = function.CyclomaticComplexity;
                _logger.LogDebug($"Cyclomatic complexity for function {function.Name}: {complexity}");

                // Calculate nesting depth
                int maxNestingDepth = CalculateMaxNestingDepth(function.Body);
                _logger.LogDebug($"Maximum nesting depth for function {function.Name}: {maxNestingDepth}");

                // Calculate lines of code
                int linesOfCode = CountLinesOfCode(function.Body);
                _logger.LogDebug($"Lines of code for function {function.Name}: {linesOfCode}");

                // Calculate parameter count
                int parameterCount = function.Parameters?.Count ?? 0;
                _logger.LogDebug($"Parameter count for function {function.Name}: {parameterCount}");

                // Store complexity metrics in function or return them
                // This could be extended to return a FunctionComplexity object

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing complexity for function {function.Name}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, List<CVariable>>> AnalyzeFunctionVariableUsageAsync(CFunction function, List<CVariable> allVariables)
        {
            _logger.LogInformation($"Analyzing variable usage for function: {function.Name}");

            var variableUsage = new Dictionary<string, List<CVariable>>();

            try
            {
                if (string.IsNullOrEmpty(function.Body))
                {
                    return variableUsage;
                }

                // Get all variables that might be used in this function
                var potentialVariables = allVariables
                    .Where(v => v.Scope == VariableScope.Global || v.Scope == VariableScope.Static)
                    .ToList();

                // Add function parameters as variables
                var parameterVariables = function.Parameters?.Select(p => new CVariable
                {
                    Name = p.Name,
                    TypeName = p.ParameterType,
                    Scope = VariableScope.Parameter,
                    IsConst = p.IsConst,
                    IsPointer = p.IsPointer,
                    PointerDepth = p.PointerDepth,
                    IsArray = p.IsArray,
                    LineNumber = p.LineNumber,
                    SourceFile = p.SourceFile
                }).ToList() ?? new List<CVariable>();

                potentialVariables.AddRange(parameterVariables);

                // Add local variables
                potentialVariables.AddRange(function.LocalVariables);

                // Create a dictionary for quick lookup
                var variableDict = potentialVariables.ToDictionary(v => v.Name, v => v);

                // Analyze body for variable usage
                foreach (var varName in function.UsedVariables)
                {
                    if (variableDict.TryGetValue(varName, out var variable))
                    {
                        string usageType = DetermineVariableUsageType(function.Body, varName);

                        if (!variableUsage.ContainsKey(usageType))
                        {
                            variableUsage[usageType] = new List<CVariable>();
                        }

                        variableUsage[usageType].Add(variable);
                    }
                }

                _logger.LogInformation($"Analyzed usage for {function.UsedVariables.Count} variables in function {function.Name}");

                return variableUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing variable usage for function {function.Name}: {ex.Message}");
                return variableUsage;
            }
        }

        /// <summary>
        /// Extracts parameters from a function cursor
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>List of function parameters</returns>
        private unsafe List<CParameter> ExtractParameters(CXCursor cursor)
        {
            var parameters = new List<CParameter>();

            try
            {
                // Get the number of parameters
                int numParams = clang.Cursor_getNumArguments(cursor);

                for (uint i = 0; i < numParams; i++)
                {
                    var paramCursor = clang.Cursor_getArgument(cursor, i);
                    string paramName = paramCursor.Spelling.ToString();

                    // Get parameter location
                    CXFile file;
                    uint line, column, offset;
                    paramCursor.Location.GetFileLocation(out file, out line, out column, out offset);
                    string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : null;

                    // Get parameter type
                    var type = paramCursor.Type;
                    string typeName = type.Spelling.ToString();

                    // Check type properties
                    bool isConst = typeName.Contains("const");
                    bool isVolatile = typeName.Contains("volatile");
                    bool isPointer = type.kind == CXTypeKind.CXType_Pointer;
                    bool isArray = type.kind == CXTypeKind.CXType_ConstantArray ||
                                  type.kind == CXTypeKind.CXType_VariableArray ||
                                  type.kind == CXTypeKind.CXType_IncompleteArray;

                    int pointerDepth = 0;
                    var currentType = type;
                    while (currentType.kind == CXTypeKind.CXType_Pointer)
                    {
                        pointerDepth++;
                        currentType = clang.getPointeeType(currentType);
                    }

                    parameters.Add(new CParameter
                    {
                        Name = string.IsNullOrEmpty(paramName) ? $"param{i}" : paramName,
                        ParameterType = typeName,
                        LineNumber = (int)line,
                        ColumnNumber = (int)column,
                        SourceFile = sourceFile,
                        IsConst = isConst,
                        IsVolatile = isVolatile,
                        IsPointer = isPointer,
                        PointerDepth = pointerDepth,
                        IsArray = isArray
                    });
                }

                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting parameters: {ex.Message}");
                return parameters;
            }
        }

        /// <summary>
        /// Extracts the function body from a cursor
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <param name="sourceCode">Complete source code</param>
        /// <returns>Function body as a string</returns>
        private string ExtractFunctionBody(CXCursor cursor, string sourceCode)
        {
            try
            {
                // Get function extent
                var extent = cursor.Extent;
                CXFile file;
                uint startLine, startColumn, startOffset;
                extent.Start.GetFileLocation(out file, out startLine, out startColumn, out startOffset);

                uint endLine, endColumn, endOffset;
                extent.End.GetFileLocation(out file, out endLine, out endColumn, out endOffset);

                // Try offset-based extraction first
                if (startOffset < sourceCode.Length && endOffset <= sourceCode.Length && endOffset > startOffset)
                {
                    int approxLength = (int)(endOffset - startOffset);
                    string approximateFunction = sourceCode.Substring((int)startOffset, approxLength);

                    // Find opening brace
                    int openBraceIndex = approximateFunction.IndexOf('{');
                    if (openBraceIndex >= 0)
                    {
                        // Find matching closing brace
                        int braceCount = 1;
                        int closeBraceIndex = openBraceIndex + 1;

                        while (braceCount > 0 && closeBraceIndex < approximateFunction.Length)
                        {
                            char c = approximateFunction[closeBraceIndex];
                            if (c == '{')
                                braceCount++;
                            else if (c == '}')
                                braceCount--;

                            closeBraceIndex++;
                        }

                        if (braceCount == 0)
                        {
                            return approximateFunction.Substring(openBraceIndex, closeBraceIndex - openBraceIndex);
                        }
                    }
                }

                // Fallback to line-based extraction
                return ExtractFunctionBodyByLines(sourceCode, (int)startLine, (int)endLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function body: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts function body using line-based approach
        /// </summary>
        /// <param name="sourceCode">Complete source code</param>
        /// <param name="startLine">Start line number</param>
        /// <param name="endLine">End line number</param>
        /// <returns>Function body</returns>
        private string ExtractFunctionBodyByLines(string sourceCode, int startLine, int endLine)
        {
            try
            {
                string[] lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (startLine < 1 || endLine > lines.Length || startLine > endLine)
                {
                    _logger.LogWarning($"Invalid line numbers: start={startLine}, end={endLine}, total lines={lines.Length}");
                    return string.Empty;
                }

                // Find opening brace
                int openBraceLineIndex = -1;
                int openBracePos = -1;

                for (int i = startLine - 1; i < Math.Min(endLine, lines.Length); i++)
                {
                    int bracePos = lines[i].IndexOf('{');
                    if (bracePos >= 0)
                    {
                        openBraceLineIndex = i;
                        openBracePos = bracePos;
                        break;
                    }
                }

                if (openBraceLineIndex < 0)
                {
                    // This might be a function declaration without body
                    return string.Empty;
                }

                // Find matching closing brace
                int closingBraceLineIndex = -1;
                int closingBracePos = -1;
                int braceCount = 1;

                for (int i = openBraceLineIndex; i < Math.Min(endLine, lines.Length); i++)
                {
                    string line = lines[i];
                    int startPos = (i == openBraceLineIndex) ? openBracePos + 1 : 0;

                    for (int j = startPos; j < line.Length; j++)
                    {
                        if (line[j] == '{')
                        {
                            braceCount++;
                        }
                        else if (line[j] == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                closingBraceLineIndex = i;
                                closingBracePos = j;
                                break;
                            }
                        }
                    }

                    if (closingBraceLineIndex >= 0)
                        break;
                }

                if (closingBraceLineIndex < 0)
                {
                    _logger.LogWarning($"No matching closing brace found for function at line {startLine}");
                    return string.Empty;
                }

                // Build body from lines
                var bodyLines = new List<string>();

                // Add first line from opening brace position
                bodyLines.Add(lines[openBraceLineIndex].Substring(openBracePos));

                // Add middle lines
                for (int i = openBraceLineIndex + 1; i < closingBraceLineIndex; i++)
                {
                    bodyLines.Add(lines[i]);
                }

                // Add last line up to closing brace
                if (closingBraceLineIndex > openBraceLineIndex)
                {
                    bodyLines.Add(lines[closingBraceLineIndex].Substring(0, closingBracePos + 1));
                }

                return string.Join(Environment.NewLine, bodyLines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in line-based body extraction: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts function calls and variable uses from function body
        /// </summary>
        /// <param name="body">Function body</param>
        /// <returns>Tuple of (called functions, used variables)</returns>
        private (List<string> CalledFunctions, List<string> UsedVariables) ExtractFunctionCallsAndVariables(string body)
        {
            var calledFunctions = new List<string>();
            var usedVariables = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return (calledFunctions, usedVariables);
                }

                // Remove comments to avoid false matches
                string cleanedBody = RemoveComments(body);

                // Extract function calls with improved pattern
                var functionCallPattern = new Regex(@"(?<![a-zA-Z_])([a-zA-Z_]\w*)\s*\(", RegexOptions.Compiled);
                var functionMatches = functionCallPattern.Matches(cleanedBody);

                foreach (Match match in functionMatches)
                {
                    string functionName = match.Groups[1].Value;

                    if (!IsKeywordOrBuiltinFunction(functionName) && !calledFunctions.Contains(functionName))
                    {
                        calledFunctions.Add(functionName);
                    }
                }

                // Extract variable uses with improved pattern
                var variablePattern = new Regex(@"(?<![a-zA-Z_])([a-zA-Z_]\w*)(?!\s*\()", RegexOptions.Compiled);
                var variableMatches = variablePattern.Matches(cleanedBody);

                foreach (Match match in variableMatches)
                {
                    string variableName = match.Groups[1].Value;

                    if (!IsKeywordOrLiteral(variableName) && !usedVariables.Contains(variableName))
                    {
                        usedVariables.Add(variableName);
                    }
                }

                return (calledFunctions, usedVariables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function calls and variables: {ex.Message}");
                return (calledFunctions, usedVariables);
            }
        }

        /// <summary>
        /// Extracts local variables from function body
        /// </summary>
        /// <param name="body">Function body</param>
        /// <param name="startLine">Starting line number</param>
        /// <returns>List of local variables</returns>
        private List<CVariable> ExtractLocalVariables(string body, int startLine)
        {
            var localVariables = new List<CVariable>();

            try
            {
                if (string.IsNullOrEmpty(body))
                    return localVariables;

                // Simple pattern to match variable declarations
                // This is a basic implementation - a real parser would use AST
                var declarationPattern = new Regex(
                    @"^\s*((?:const\s+|volatile\s+|static\s+)*)\s*([a-zA-Z_]\w*(?:\s*\*)*)\s+([a-zA-Z_]\w*(?:\[[^\]]*\])*)\s*(?:=\s*([^;]+))?\s*;",
                    RegexOptions.Multiline);

                string[] lines = body.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var matches = declarationPattern.Matches(lines[i]);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            string modifiers = match.Groups[1].Value.Trim();
                            string typeName = match.Groups[2].Value.Trim();
                            string varName = match.Groups[3].Value.Trim();
                            string defaultValue = match.Groups[4].Success ? match.Groups[4].Value.Trim() : "";

                            // Parse array dimensions
                            bool isArray = varName.Contains('[');
                            if (isArray)
                            {
                                int bracketIndex = varName.IndexOf('[');
                                varName = varName.Substring(0, bracketIndex);
                            }

                            // Count pointer depth
                            int pointerDepth = typeName.Count(c => c == '*');
                            typeName = typeName.Replace("*", "").Trim();

                            var variable = new CVariable
                            {
                                Name = varName,
                                TypeName = typeName,
                                Scope = VariableScope.Local,
                                LineNumber = startLine + i,
                                IsConst = modifiers.Contains("const"),
                                IsVolatile = modifiers.Contains("volatile"),
                                IsPointer = pointerDepth > 0,
                                PointerDepth = pointerDepth,
                                IsArray = isArray,
                                DefaultValue = defaultValue,
                                StorageClass = modifiers.Contains("static") ? StorageClass.Static : StorageClass.Auto
                            };

                            localVariables.Add(variable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting local variables: {ex.Message}");
            }

            return localVariables;
        }

        /// <summary>
        /// Extracts function documentation from comments above the function
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>Documentation string</returns>
        private unsafe string ExtractFunctionDocumentation(CXCursor cursor)
        {
            try
            {
                var comment = clang.Cursor_getParsedComment(cursor);
                if (comment.Kind != CXCommentKind.CXComment_Null)
                {
                    var commentString = clang.FullComment_getAsHTML(comment);
                    return commentString.ToString();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts function attributes
        /// </summary>
        /// <param name="cursor">Function cursor</param>
        /// <returns>List of function attributes</returns>
        private unsafe List<CFunctionAttribute> ExtractFunctionAttributes(CXCursor cursor)
        {
            var attributes = new List<CFunctionAttribute>();

            try
            {
                // Create a visitor delegate that matches the expected unmanaged function pointer signature
                delegate* unmanaged[Cdecl]<CXCursor, CXCursor, void*, CXChildVisitResult> visitor = &VisitAttributeChildren;
                var context = new AttributeVisitorContext { Attributes = attributes };

                // Pin the context for the duration of the visit
                var contextHandle = GCHandle.Alloc(context, GCHandleType.Pinned);
                try
                {
                    clang.visitChildren(cursor, visitor, new CXClientData(contextHandle.AddrOfPinnedObject()));
                }
                finally
                {
                    contextHandle.Free();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function attributes: {ex.Message}");
            }

            return attributes;
        }

        /// <summary>
        /// Context structure for attribute visitor
        /// </summary>
        private class AttributeVisitorContext
        {
            public List<CFunctionAttribute> Attributes { get; set; }
        }

        /// <summary>
        /// Unmanaged visitor function for extracting attributes
        /// </summary>
        /// <param name="cursor">Current cursor</param>
        /// <param name="parent">Parent cursor</param>
        /// <param name="clientData">Client data pointer</param>
        /// <returns>Visit result</returns>
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe CXChildVisitResult VisitAttributeChildren(CXCursor cursor, CXCursor parent, void* clientData)
        {
            try
            {
                if (clientData != null)
                {
                    var handle = GCHandle.FromIntPtr((IntPtr)clientData);
                    if (handle.IsAllocated && handle.Target is AttributeVisitorContext context)
                    {
                        if (cursor.Kind == CXCursorKind.CXCursor_AnnotateAttr ||
                            cursor.Kind == CXCursorKind.CXCursor_AlignedAttr ||
                            cursor.Kind == CXCursorKind.CXCursor_PackedAttr)
                        {
                            var attrName = cursor.Spelling.ToString();
                            if (!string.IsNullOrEmpty(attrName))
                            {
                                context.Attributes.Add(new CFunctionAttribute { Name = attrName });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Cannot throw exceptions from unmanaged code
                // Silently continue
            }

            return CXChildVisitResult.CXChildVisit_Continue;
        }

        /// <summary>
        /// Removes comments from source code
        /// </summary>
        /// <param name="code">Source code</param>
        /// <returns>Code without comments</returns>
        private string RemoveComments(string code)
        {
            try
            {
                // Remove single-line comments
                code = Regex.Replace(code, @"//.*?(?=\r?\n|$)", "", RegexOptions.Multiline);

                // Remove multi-line comments
                code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);

                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing comments: {ex.Message}");
                return code;
            }
        }

        /// <summary>
        /// Checks if a line is a comment
        /// </summary>
        /// <param name="line">Line to check</param>
        /// <returns>True if the line is a comment</returns>
        private bool IsComment(string line)
        {
            return line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*");
        }

        /// <summary>
        /// Processes a line for control flow analysis
        /// </summary>
        /// <param name="line">Line of code</param>
        /// <param name="lineNumber">Line number</param>
        /// <param name="nodeId">Node ID counter</param>
        /// <returns>Control flow node if applicable</returns>
        private ControlFlowNode ProcessControlFlowLine(string line, int lineNumber, ref int nodeId)
        {
            try
            {
                if (line.StartsWith("if ") || line.StartsWith("if("))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Condition",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("else ") || line == "else")
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Condition",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("for ") || line.StartsWith("for("))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Loop",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("while ") || line.StartsWith("while("))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Loop",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("do ") || line == "do")
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Loop",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("switch ") || line.StartsWith("switch("))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Switch",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("case ") || line.StartsWith("default:"))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Case",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("return ") || line == "return;")
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Return",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("break;") || line == "break")
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Break",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("continue;") || line == "continue")
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Continue",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.StartsWith("goto "))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Goto",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }
                else if (line.Contains("(") && !line.StartsWith("{") && !line.StartsWith("}") && !string.IsNullOrWhiteSpace(line))
                {
                    return new ControlFlowNode
                    {
                        Id = $"node_{nodeId++}",
                        NodeType = "Statement",
                        LineNumber = lineNumber,
                        Code = line
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing control flow line: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a control flow edge to the graph
        /// </summary>
        /// <param name="graph">Control flow graph</param>
        /// <param name="source">Source node</param>
        /// <param name="target">Target node</param>
        /// <param name="edgeType">Edge type</param>
        private void AddControlFlowEdge(ControlFlowGraph graph, ControlFlowNode source, ControlFlowNode target, string edgeType)
        {
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = $"edge_{graph.Edges.Count}",
                SourceId = source.Id,
                TargetId = target.Id,
                EdgeType = edgeType
            });
        }

        /// <summary>
        /// Determines edge type based on node type
        /// </summary>
        /// <param name="nodeType">Node type</param>
        /// <returns>Edge type</returns>
        private string DetermineEdgeType(string nodeType)
        {
            switch (nodeType)
            {
                case "Condition":
                    return "True";
                case "Loop":
                    return "Loop";
                case "Return":
                    return "Return";
                case "Break":
                    return "Break";
                case "Continue":
                    return "Continue";
                default:
                    return "Unconditional";
            }
        }

        /// <summary>
        /// Handles branching logic for control flow
        /// </summary>
        /// <param name="graph">Control flow graph</param>
        /// <param name="node">Current node</param>
        /// <param name="branchStack">Stack for tracking branches</param>
        /// <param name="nodeId">Node ID counter</param>
        private void HandleBranchingLogic(ControlFlowGraph graph, ControlFlowNode node,
            Stack<(ControlFlowNode node, string branchType)> branchStack, ref int nodeId)
        {
            try
            {
                switch (node.NodeType)
                {
                    case "Condition":
                        if (node.Code.StartsWith("if"))
                        {
                            branchStack.Push((node, "if"));
                        }
                        break;

                    case "Loop":
                        branchStack.Push((node, "loop"));
                        break;

                    case "Switch":
                        branchStack.Push((node, "switch"));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling branching logic: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines the variable type from a Clang type
        /// </summary>
        /// <param name="type">Clang type</param>
        /// <returns>Variable type</returns>
        private VariableType DetermineVariableType(CXType type)
        {
            switch (type.kind)
            {
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_VariableArray:
                case CXTypeKind.CXType_IncompleteArray:
                case CXTypeKind.CXType_DependentSizedArray:
                    return VariableType.Array;

                case CXTypeKind.CXType_Pointer:
                    return VariableType.Pointer;

                case CXTypeKind.CXType_Record:
                    return type.Spelling.ToString().Contains("struct") ?
                        VariableType.Struct : VariableType.Union;

                case CXTypeKind.CXType_Enum:
                    return VariableType.Enum;

                case CXTypeKind.CXType_Float:
                case CXTypeKind.CXType_Double:
                case CXTypeKind.CXType_LongDouble:
                    return VariableType.Float;

                case CXTypeKind.CXType_Bool:
                    return VariableType.Bool;

                case CXTypeKind.CXType_Char_S:
                case CXTypeKind.CXType_Char_U:
                case CXTypeKind.CXType_SChar:
                case CXTypeKind.CXType_UChar:
                    return VariableType.Char;

                default:
                    if (type.kind >= CXTypeKind.CXType_Short && type.kind <= CXTypeKind.CXType_ULongLong)
                    {
                        return VariableType.Integer;
                    }
                    return VariableType.Unknown;
            }
        }

        /// <summary>
        /// Checks if a string is a C keyword or built-in function
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <returns>True if the word is a C keyword or built-in function</returns>
        private bool IsKeywordOrBuiltinFunction(string word)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // C keywords
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "sizeof", "typedef", "struct",
                "union", "enum", "void", "char", "short", "int", "long", "float",
                "double", "signed", "unsigned", "const", "volatile", "auto", "register",
                "static", "extern", "inline", "restrict", "_Bool", "_Complex", "_Imaginary",
                
                // Common standard library functions
                "printf", "scanf", "sprintf", "sscanf", "fprintf", "fscanf",
                "malloc", "calloc", "realloc", "free", "memset", "memcpy", "memmove", "memcmp",
                "strcpy", "strncpy", "strcat", "strncat", "strlen", "strcmp", "strncmp",
                "strchr", "strrchr", "strstr", "strtok",
                "fopen", "fclose", "fread", "fwrite", "fseek", "ftell", "rewind",
                "getchar", "putchar", "gets", "puts", "fgets", "fputs",
                "atoi", "atof", "atol", "strtol", "strtoul", "strtod",
                "abs", "labs", "div", "ldiv", "rand", "srand",
                "exit", "atexit", "system", "getenv"
            };

            return keywords.Contains(word);
        }

        /// <summary>
        /// Checks if a string is a C keyword or literal
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <returns>True if the word is a C keyword or literal</returns>
        private bool IsKeywordOrLiteral(string word)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // C keywords
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "sizeof", "typedef", "struct",
                "union", "enum", "void", "char", "short", "int", "long", "float",
                "double", "signed", "unsigned", "const", "volatile", "auto", "register",
                "static", "extern", "inline", "restrict", "_Bool", "_Complex", "_Imaginary",
                
                // Literals and special values
                "true", "false", "NULL", "__func__", "__FILE__", "__LINE__", "__DATE__", "__TIME__"
            };

            // Check if it's a numeric literal
            if (Regex.IsMatch(word, @"^\d+(\.\d+)?([eE][+-]?\d+)?[flFL]?$"))
                return true;

            // Check if it's a character or string literal indicator
            if (word.StartsWith("\"") || word.StartsWith("'"))
                return true;

            return keywords.Contains(word);
        }

        /// <summary>
        /// Calculates the cyclomatic complexity of a function
        /// </summary>
        /// <param name="body">Function body</param>
        /// <returns>Cyclomatic complexity</returns>
        private int CalculateCyclomaticComplexity(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return 1; // Minimum complexity
                }

                // Remove comments and strings to avoid false matches
                string cleanedBody = RemoveCommentsAndStrings(body);

                int complexity = 1; // Base complexity

                // Count decision structures
                complexity += CountPatternOccurrences(cleanedBody, @"\bif\b");
                complexity += CountPatternOccurrences(cleanedBody, @"\bfor\b");
                complexity += CountPatternOccurrences(cleanedBody, @"\bwhile\b");
                complexity += CountPatternOccurrences(cleanedBody, @"\bcase\b");
                complexity += CountPatternOccurrences(cleanedBody, @"\bcatch\b");

                // Count logical operators (each adds one path)
                complexity += CountPatternOccurrences(cleanedBody, @"&&");
                complexity += CountPatternOccurrences(cleanedBody, @"\|\|");
                complexity += CountPatternOccurrences(cleanedBody, @"\?"); // Ternary operator

                return complexity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating cyclomatic complexity: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Removes comments and string literals from code
        /// </summary>
        /// <param name="code">Source code</param>
        /// <returns>Cleaned code</returns>
        private string RemoveCommentsAndStrings(string code)
        {
            try
            {
                // Remove string literals
                code = Regex.Replace(code, @"""(?:[^""\\]|\\.)*""", "\"\"", RegexOptions.Multiline);
                code = Regex.Replace(code, @"'(?:[^'\\]|\\.)*'", "''", RegexOptions.Multiline);

                // Remove comments
                code = RemoveComments(code);

                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing comments and strings: {ex.Message}");
                return code;
            }
        }

        /// <summary>
        /// Counts occurrences of a regex pattern
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <param name="pattern">Regex pattern</param>
        /// <returns>Number of matches</returns>
        private int CountPatternOccurrences(string text, string pattern)
        {
            try
            {
                return Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error counting pattern occurrences: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calculates the maximum nesting depth of a function
        /// </summary>
        /// <param name="body">Function body</param>
        /// <returns>Maximum nesting depth</returns>
        private int CalculateMaxNestingDepth(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return 0;
                }

                int maxDepth = 0;
                int currentDepth = 0;
                bool inString = false;
                bool inChar = false;
                bool inSingleLineComment = false;
                bool inMultiLineComment = false;

                for (int i = 0; i < body.Length; i++)
                {
                    char c = body[i];
                    char nextChar = (i + 1 < body.Length) ? body[i + 1] : '\0';

                    // Handle string and character literals
                    if (!inSingleLineComment && !inMultiLineComment)
                    {
                        if (c == '"' && !inChar && (i == 0 || body[i - 1] != '\\'))
                        {
                            inString = !inString;
                            continue;
                        }

                        if (c == '\'' && !inString && (i == 0 || body[i - 1] != '\\'))
                        {
                            inChar = !inChar;
                            continue;
                        }
                    }

                    // Handle comments
                    if (!inString && !inChar)
                    {
                        if (c == '/' && nextChar == '/')
                        {
                            inSingleLineComment = true;
                            i++; // Skip next character
                            continue;
                        }

                        if (c == '/' && nextChar == '*')
                        {
                            inMultiLineComment = true;
                            i++; // Skip next character
                            continue;
                        }

                        if (inMultiLineComment && c == '*' && nextChar == '/')
                        {
                            inMultiLineComment = false;
                            i++; // Skip next character
                            continue;
                        }

                        if (inSingleLineComment && (c == '\n' || c == '\r'))
                        {
                            inSingleLineComment = false;
                            continue;
                        }
                    }

                    // Count braces only if not in string, char, or comment
                    if (!inString && !inChar && !inSingleLineComment && !inMultiLineComment)
                    {
                        if (c == '{')
                        {
                            currentDepth++;
                            maxDepth = Math.Max(maxDepth, currentDepth);
                        }
                        else if (c == '}')
                        {
                            currentDepth--;
                        }
                    }
                }

                return maxDepth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating nesting depth: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Counts the lines of code in a function body
        /// </summary>
        /// <param name="body">Function body</param>
        /// <returns>Number of lines of code (excluding comments and blank lines)</returns>
        private int CountLinesOfCode(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return 0;
                }

                string[] lines = body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int count = 0;
                bool inMultiLineComment = false;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    // Handle multi-line comments
                    if (trimmed.StartsWith("/*"))
                        inMultiLineComment = true;

                    if (inMultiLineComment)
                    {
                        if (trimmed.EndsWith("*/"))
                            inMultiLineComment = false;
                        continue;
                    }

                    // Skip single-line comments
                    if (trimmed.StartsWith("//"))
                        continue;

                    // Skip lines with only braces
                    if (trimmed == "{" || trimmed == "}")
                        continue;

                    count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error counting lines of code: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Determines how a variable is used in a function
        /// </summary>
        /// <param name="body">Function body</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>Usage type: "read", "write", or "read/write"</returns>
        private string DetermineVariableUsageType(string body, string variableName)
        {
            try
            {
                if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(variableName))
                {
                    return "unknown";
                }

                string cleanedBody = RemoveCommentsAndStrings(body);
                string escapedVarName = Regex.Escape(variableName);

                // Check for writes (assignments and modifications)
                var writePatterns = new[]
                {
                    $@"\b{escapedVarName}\s*=(?!=)", // Assignment (but not ==)
                    $@"\b{escapedVarName}\s*\+=",    // Compound assignments
                    $@"\b{escapedVarName}\s*-=",
                    $@"\b{escapedVarName}\s*\*=",
                    $@"\b{escapedVarName}\s*/=",
                    $@"\b{escapedVarName}\s*%=",
                    $@"\b{escapedVarName}\s*<<=",
                    $@"\b{escapedVarName}\s*>>=",
                    $@"\b{escapedVarName}\s*&=",
                    $@"\b{escapedVarName}\s*\|=",
                    $@"\b{escapedVarName}\s*\^=",
                    $@"\b{escapedVarName}\+\+",      // Post-increment
                    $@"\b{escapedVarName}--",        // Post-decrement
                    $@"\+\+{escapedVarName}\b",      // Pre-increment
                    $@"--{escapedVarName}\b"         // Pre-decrement
                };

                bool isWritten = writePatterns.Any(pattern => Regex.IsMatch(cleanedBody, pattern));

                // Check for reads (variable usage in expressions)
                var readPattern = $@"\b{escapedVarName}\b";
                bool isRead = Regex.IsMatch(cleanedBody, readPattern);

                // If found in write context, check if it's also read
                if (isWritten && isRead)
                {
                    // More sophisticated check: exclude pure assignments from read
                    var pureAssignmentPattern = $@"\b{escapedVarName}\s*=\s*[^=]";
                    string tempBody = Regex.Replace(cleanedBody, pureAssignmentPattern, "");
                    bool hasOtherReads = Regex.IsMatch(tempBody, readPattern);

                    return hasOtherReads ? "read/write" : "write";
                }
                else if (isRead)
                {
                    return "read";
                }
                else if (isWritten)
                {
                    return "write";
                }
                else
                {
                    return "unknown";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error determining variable usage type: {ex.Message}");
                return "unknown";
            }
        }

        /// <summary>
        /// Clears the function cache
        /// </summary>
        public void ClearCache()
        {
            _functionCache.Clear();
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <returns>Number of cached functions</returns>
        public int GetCacheSize()
        {
            return _functionCache.Count;
        }
    }
}
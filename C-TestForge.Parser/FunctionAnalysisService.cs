using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public unsafe CFunction ExtractFunction(CXCursor cursor)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_FunctionDecl)
                {
                    return null;
                }

                string functionName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting function: {functionName}");

                // Get function location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : null;

                // Get return type
                var returnType = cursor.ResultType;
                string returnTypeName = returnType.Spelling.ToString();

                // Check function attributes using storage class
                bool isStatic = IsStaticFunction(cursor);
                bool isInline = IsInlineFunction(cursor);
                bool isExternal = IsExternalFunction(cursor);

                // Get function parameters
                List<CVariable> parameters = ExtractParameters(cursor);

                // Extract function body
                string body = ExtractFunctionBody(cursor);

                // Extract called functions and used variables
                var (calledFunctions, usedVariables) = ExtractFunctionCallsAndVariables(body);

                // Create function object
                var function = new CFunction
                {
                    Name = functionName,
                    ReturnType = returnTypeName,
                    Parameters = parameters,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsStatic = isStatic,
                    IsInline = isInline,
                    IsExternal = isExternal,
                    Body = body,
                    CalledFunctions = calledFunctions,
                    UsedVariables = usedVariables
                };

                return function;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function: {ex.Message}");
                return null;
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

            // Free token memory
            if (tokens != null && numTokens > 0)
            {
                clang.disposeTokens(cursor.TranslationUnit, tokens, numTokens);
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

                // Create nodes for control structures (if, else, for, while, switch, etc.)
                // This is a simplified approach; a real implementation would use the AST
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

                // Exit node
                var exitNode = new ControlFlowNode
                {
                    Id = $"node_{nodeId++}",
                    NodeType = "Exit",
                    LineNumber = function.LineNumber + lines.Length,
                    Code = $"Exit: {function.Name}"
                };

                // Process function body
                var currentNode = entryNode;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*"))
                    {
                        continue; // Skip comments and empty lines
                    }

                    // Detect control structures
                    if (line.StartsWith("if ") || line.StartsWith("if("))
                    {
                        // If statement
                        var ifNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Condition",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(ifNode);

                        // Add edge from current node to if node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = ifNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = ifNode;
                    }
                    else if (line.StartsWith("else ") || line == "else")
                    {
                        // Else statement
                        var elseNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Condition",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(elseNode);

                        // Add edge from previous node to else node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = elseNode.Id,
                            EdgeType = "False"
                        });

                        currentNode = elseNode;
                    }
                    else if (line.StartsWith("for ") || line.StartsWith("for("))
                    {
                        // For loop
                        var forNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Loop",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(forNode);

                        // Add edge from current node to for node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = forNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = forNode;
                    }
                    else if (line.StartsWith("while ") || line.StartsWith("while("))
                    {
                        // While loop
                        var whileNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Loop",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(whileNode);

                        // Add edge from current node to while node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = whileNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = whileNode;
                    }
                    else if (line.StartsWith("switch ") || line.StartsWith("switch("))
                    {
                        // Switch statement
                        var switchNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Switch",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(switchNode);

                        // Add edge from current node to switch node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = switchNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = switchNode;
                    }
                    else if (line.StartsWith("case ") || line.StartsWith("default:"))
                    {
                        // Case or default in switch
                        var caseNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Case",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(caseNode);

                        // Add edge from current node to case node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = caseNode.Id,
                            EdgeType = "Case"
                        });

                        currentNode = caseNode;
                    }
                    else if (line.StartsWith("return ") || line == "return;")
                    {
                        // Return statement
                        var returnNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Return",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(returnNode);

                        // Add edge from current node to return node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = returnNode.Id,
                            EdgeType = "Unconditional"
                        });

                        // Add edge from return node to exit node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = returnNode.Id,
                            TargetId = exitNode.Id,
                            EdgeType = "Return"
                        });

                        currentNode = returnNode;
                    }
                    else if (line.StartsWith("break;") || line == "break")
                    {
                        // Break statement
                        var breakNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Break",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(breakNode);

                        // Add edge from current node to break node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = breakNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = breakNode;
                    }
                    else if (line.StartsWith("continue;") || line == "continue")
                    {
                        // Continue statement
                        var continueNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Continue",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(continueNode);

                        // Add edge from current node to continue node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = continueNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = continueNode;
                    }
                    else if (line.Contains("(") && !line.StartsWith("{") && !line.StartsWith("}"))
                    {
                        // Likely a function call or statement
                        var statementNode = new ControlFlowNode
                        {
                            Id = $"node_{nodeId++}",
                            NodeType = "Statement",
                            LineNumber = function.LineNumber + i,
                            Code = line
                        };
                        graph.Nodes.Add(statementNode);

                        // Add edge from current node to statement node
                        graph.Edges.Add(new ControlFlowEdge
                        {
                            Id = $"edge_{graph.Edges.Count}",
                            SourceId = currentNode.Id,
                            TargetId = statementNode.Id,
                            EdgeType = "Unconditional"
                        });

                        currentNode = statementNode;
                    }
                }

                // Add exit node if not already connected
                if (!graph.Edges.Any(e => e.TargetId == exitNode.Id))
                {
                    graph.Nodes.Add(exitNode);

                    // Add edge from last node to exit node
                    graph.Edges.Add(new ControlFlowEdge
                    {
                        Id = $"edge_{graph.Edges.Count}",
                        SourceId = currentNode.Id,
                        TargetId = exitNode.Id,
                        EdgeType = "Unconditional"
                    });
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

                // Calculate cyclomatic complexity
                int complexity = CalculateCyclomaticComplexity(function.Body);
                _logger.LogDebug($"Cyclomatic complexity for function {function.Name}: {complexity}");

                // Calculate nesting depth
                int maxNestingDepth = CalculateMaxNestingDepth(function.Body);
                _logger.LogDebug($"Maximum nesting depth for function {function.Name}: {maxNestingDepth}");

                // Calculate lines of code
                int linesOfCode = CountLinesOfCode(function.Body);
                _logger.LogDebug($"Lines of code for function {function.Name}: {linesOfCode}");

                // Additional metrics could be added here
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
                    .Where(v => v.Scope == VariableScope.Global || v.Scope == VariableScope.Rom || v.Scope == VariableScope.Static)
                    .ToList();

                // Add function parameters
                potentialVariables.AddRange(function.Parameters);

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
        private unsafe List<CVariable> ExtractParameters(CXCursor cursor)
        {
            var parameters = new List<CVariable>();

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

                    // Determine variable type
                    VariableType variableType = DetermineVariableType(type);

                    // Check attributes
                    bool isConst = typeName.Contains("const");

                    parameters.Add(new CVariable
                    {
                        Name = paramName,
                        TypeName = typeName,
                        VariableType = variableType,
                        Scope = VariableScope.Parameter,
                        LineNumber = (int)line,
                        ColumnNumber = (int)column,
                        SourceFile = sourceFile,
                        IsConst = isConst
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
        /// <returns>Function body as a string</returns>
        private string ExtractFunctionBody(CXCursor cursor)
        {
            try
            {
                var extent = cursor.Extent;
                string fullText = extent.ToString();

                // Find the opening brace
                int openBrace = fullText.IndexOf('{');
                if (openBrace < 0)
                {
                    // No body, might be a declaration
                    return string.Empty;
                }

                // Find the closing brace (matching the opening one)
                int closeBrace = -1;
                int braceCount = 0;

                for (int i = openBrace; i < fullText.Length; i++)
                {
                    if (fullText[i] == '{')
                    {
                        braceCount++;
                    }
                    else if (fullText[i] == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            closeBrace = i;
                            break;
                        }
                    }
                }

                if (closeBrace < 0)
                {
                    // Could not find matching closing brace
                    return string.Empty;
                }

                // Extract the body (including braces)
                return fullText.Substring(openBrace, closeBrace - openBrace + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function body: {ex.Message}");
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

                // Extract function calls
                // This is a simplified approach that looks for identifiers followed by parentheses
                var functionCallPattern = new Regex(@"(\w+)\s*\(");
                var functionMatches = functionCallPattern.Matches(body);

                foreach (Match match in functionMatches)
                {
                    string functionName = match.Groups[1].Value;

                    // Skip C keywords and standard library functions
                    if (!IsKeywordOrBuiltinFunction(functionName) && !calledFunctions.Contains(functionName))
                    {
                        calledFunctions.Add(functionName);
                    }
                }

                // Extract variable uses
                // This is a simplified approach that might pick up false positives
                var variablePattern = new Regex(@"([a-zA-Z_]\w*)\b(?!\s*\()");
                var variableMatches = variablePattern.Matches(body);

                foreach (Match match in variableMatches)
                {
                    string variableName = match.Groups[1].Value;

                    // Skip C keywords and literals
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
                    // Handle integer types
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
        /// <returns>True if the word is a C keyword or built-in function, false otherwise</returns>
        private bool IsKeywordOrBuiltinFunction(string word)
        {
            // List of common C keywords and built-in functions
            string[] keywords = new[]
            {
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "sizeof", "typedef", "struct",
                "union", "enum", "void", "char", "short", "int", "long", "float",
                "double", "signed", "unsigned", "const", "volatile", "auto", "register",
                "static", "extern", "printf", "scanf", "malloc", "free", "memset", "memcpy",
                "strcpy", "strlen", "strcmp", "strcat", "fopen", "fclose", "fread", "fwrite"
            };

            return keywords.Contains(word);
        }

        /// <summary>
        /// Checks if a string is a C keyword or literal
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <returns>True if the word is a C keyword or literal, false otherwise</returns>
        private bool IsKeywordOrLiteral(string word)
        {
            // List of common C keywords and literals
            string[] keywords = new[]
            {
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "sizeof", "typedef", "struct",
                "union", "enum", "void", "char", "short", "int", "long", "float",
                "double", "signed", "unsigned", "const", "volatile", "auto", "register",
                "static", "extern", "true", "false", "NULL"
            };

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

                // Count the number of decision points (if, for, while, case, etc.) and logical operators (&&, ||)
                int complexity = 1; // Base complexity

                // Count decision structures
                complexity += Regex.Matches(body, @"\bif\b").Count;
                complexity += Regex.Matches(body, @"\bfor\b").Count;
                complexity += Regex.Matches(body, @"\bwhile\b").Count;
                complexity += Regex.Matches(body, @"\bcase\b").Count;
                complexity += Regex.Matches(body, @"\bcatch\b").Count;

                // Count logical operators
                complexity += Regex.Matches(body, @"&&").Count;
                complexity += Regex.Matches(body, @"\|\|").Count;
                complexity += Regex.Matches(body, @"\?").Count; // Ternary operator

                return complexity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating cyclomatic complexity: {ex.Message}");
                return 1;
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

                foreach (char c in body)
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

                // Split into lines
                string[] lines = body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Count non-empty, non-comment lines
                int count = 0;
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) &&
                        !trimmed.StartsWith("//") &&
                        !trimmed.StartsWith("/*") &&
                        !trimmed.EndsWith("*/"))
                    {
                        count++;
                    }
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
                if (string.IsNullOrEmpty(body))
                {
                    return "unknown";
                }

                // Check for writes (assignments)
                bool isWritten = Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*\+=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*-=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*\*=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*/=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*%=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*<<=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*>>=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*&=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*\|=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*\^=") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\+\+") ||
                                Regex.IsMatch(body, $@"{Regex.Escape(variableName)}--") ||
                                Regex.IsMatch(body, $@"\+\+{Regex.Escape(variableName)}") ||
                                Regex.IsMatch(body, $@"--{Regex.Escape(variableName)}");

                // Check for reads (uses)
                bool isRead = Regex.IsMatch(body, $@"\b{Regex.Escape(variableName)}\b") &&
                             !Regex.IsMatch(body, $@"{Regex.Escape(variableName)}\s*=\s*[^=]"); // Exclude simple assignments

                if (isRead && isWritten)
                {
                    return "read/write";
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
    }
}
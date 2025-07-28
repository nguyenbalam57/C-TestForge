using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClangSharp;
using ClangSharp.Interop;
using C_TestForge.Core;
using C_TestForge.Models;
using Microsoft.Extensions.Logging;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Solver;

namespace C_TestForge.Parser
{
    #region FunctionAnalysisService Implementation

    /// <summary>
    /// Implementation of the function analysis service
    /// </summary>
    public class FunctionAnalysisService : IFunctionAnalysisService
    {
        private readonly ILogger<FunctionAnalysisService> _logger;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IFileService _fileService;

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
        public CFunction ExtractFunction(CXCursor cursor)
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
                var location = cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out _);
                string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : null;

                // Get function return type
                var type = cursor.Type;
                var returnType = type.GetResultType().Spelling.ToString();

                // Check function attributes
                bool isStatic = false;
                bool isInline = false;
                bool isExternal = false;

                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_StorageClass)
                    {
                        string storage = child.Spelling.ToString();
                        if (storage == "static")
                        {
                            isStatic = true;
                        }
                        else if (storage == "extern")
                        {
                            isExternal = true;
                        }
                    }
                    else if (child.Kind == CXCursorKind.CXCursor_InlineAttr)
                    {
                        isInline = true;
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, IntPtr.Zero);

                // Create function object
                var function = new CFunction
                {
                    Name = functionName,
                    ReturnType = returnType,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsStatic = isStatic,
                    IsInline = isInline,
                    IsExternal = isExternal,
                    Parameters = new List<CVariable>(),
                    CalledFunctions = new List<string>(),
                    UsedVariables = new List<string>()
                };

                // Extract parameters
                uint paramCount = cursor.NumArguments;
                for (uint i = 0; i < paramCount; i++)
                {
                    var paramCursor = cursor.GetArgument(i);
                    var parameter = ExtractParameter(paramCursor);
                    if (parameter != null)
                    {
                        function.Parameters.Add(parameter);
                    }
                }

                // Extract function body if available
                if (cursor.HasChildren)
                {
                    ExtractFunctionBody(cursor, function);
                }

                return function;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting function from cursor: {cursor.Spelling}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<FunctionRelationship>> AnalyzeFunctionRelationshipsAsync(List<CFunction> functions)
        {
            try
            {
                _logger.LogInformation($"Analyzing relationships between {functions.Count} functions");

                var relationships = new List<FunctionRelationship>();

                // Build a dictionary for quick lookup
                var functionDict = functions.ToDictionary(f => f.Name, f => f);

                // Analyze each function's relationships
                foreach (var function in functions)
                {
                    _logger.LogDebug($"Analyzing relationships for function: {function.Name}");

                    // Find called functions
                    foreach (var calledFunctionName in function.CalledFunctions)
                    {
                        if (functionDict.TryGetValue(calledFunctionName, out var calledFunction))
                        {
                            _logger.LogDebug($"Function {function.Name} calls {calledFunctionName}");

                            var relationship = new FunctionRelationship
                            {
                                CallerName = function.Name,
                                CalleeName = calledFunctionName,
                                LineNumber = function.LineNumber, // Ideally, we would find the exact line number of the call
                                SourceFile = function.SourceFile
                            };

                            relationships.Add(relationship);
                        }
                    }
                }

                // Identify complex relationships (e.g., recursive calls, mutual recursion)
                IdentifyComplexRelationships(relationships, functions);

                _logger.LogInformation($"Identified {relationships.Count} function relationships");

                return relationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing function relationships");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ControlFlowGraph> ExtractControlFlowGraphAsync(CFunction function, SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Extracting control flow graph for function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                // Create a new control flow graph
                var graph = new ControlFlowGraph
                {
                    FunctionName = function.Name,
                    Nodes = new List<ControlFlowNode>(),
                    Edges = new List<ControlFlowEdge>()
                };

                // Extract the function body lines
                var functionBody = ExtractFunctionBodyLines(function, sourceFile);

                if (functionBody.Count == 0)
                {
                    _logger.LogWarning($"Could not extract function body for {function.Name}");
                    return graph;
                }

                // Create the entry node
                var entryNode = new ControlFlowNode
                {
                    Id = Guid.NewGuid().ToString(),
                    NodeType = "Entry",
                    LineNumber = function.LineNumber,
                    Code = $"{function.ReturnType} {function.Name}(...)"
                };

                graph.Nodes.Add(entryNode);

                // Create nodes for each statement in the function body
                await ProcessFunctionBodyAsync(functionBody, function.LineNumber, graph, entryNode.Id);

                _logger.LogInformation($"Extracted control flow graph with {graph.Nodes.Count} nodes and {graph.Edges.Count} edges for function: {function.Name}");

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting control flow graph for function: {function.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionComplexity> AnalyzeFunctionComplexityAsync(CFunction function, SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Analyzing complexity of function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                // Extract the function body lines
                var functionBody = ExtractFunctionBodyLines(function, sourceFile);

                if (functionBody.Count == 0)
                {
                    _logger.LogWarning($"Could not extract function body for {function.Name}");
                    return new FunctionComplexity
                    {
                        FunctionName = function.Name,
                        CyclomaticComplexity = 1,
                        LinesOfCode = 0,
                        ParameterCount = function.Parameters.Count,
                        NestingDepth = 0,
                        StatementCount = 0,
                        ConditionCount = 0
                    };
                }

                // Calculate complexity metrics
                int cyclomaticComplexity = CalculateCyclomaticComplexity(functionBody);
                int nestingDepth = CalculateNestingDepth(functionBody);
                int statementCount = CalculateStatementCount(functionBody);
                int conditionCount = CalculateConditionCount(functionBody);

                var complexity = new FunctionComplexity
                {
                    FunctionName = function.Name,
                    CyclomaticComplexity = cyclomaticComplexity,
                    LinesOfCode = functionBody.Count,
                    ParameterCount = function.Parameters.Count,
                    NestingDepth = nestingDepth,
                    StatementCount = statementCount,
                    ConditionCount = conditionCount
                };

                _logger.LogInformation($"Analyzed complexity of function: {function.Name}, Cyclomatic Complexity: {cyclomaticComplexity}, Nesting Depth: {nestingDepth}");

                return complexity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing complexity of function: {function.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CVariable>> AnalyzeFunctionVariableUsageAsync(CFunction function, List<CVariable> allVariables)
        {
            try
            {
                _logger.LogInformation($"Analyzing variable usage in function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (allVariables == null)
                {
                    throw new ArgumentNullException(nameof(allVariables));
                }

                var usedVariables = new List<CVariable>();

                // Check each used variable name
                foreach (var variableName in function.UsedVariables)
                {
                    // Find the variable in the allVariables list
                    var variable = allVariables.FirstOrDefault(v => v.Name == variableName);

                    if (variable != null)
                    {
                        usedVariables.Add(variable);

                        // Add the function to the variable's UsedByFunctions list
                        if (!variable.UsedByFunctions.Contains(function.Name))
                        {
                            variable.UsedByFunctions.Add(function.Name);
                        }
                    }
                }

                // Also add the function parameters
                foreach (var parameter in function.Parameters)
                {
                    if (!usedVariables.Any(v => v.Name == parameter.Name))
                    {
                        usedVariables.Add(parameter);

                        // Add the function to the parameter's UsedByFunctions list
                        if (!parameter.UsedByFunctions.Contains(function.Name))
                        {
                            parameter.UsedByFunctions.Add(function.Name);
                        }
                    }
                }

                _logger.LogInformation($"Found {usedVariables.Count} variables used in function: {function.Name}");

                return usedVariables;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing variable usage in function: {function.Name}");
                throw;
            }
        }

        private CVariable ExtractParameter(CXCursor cursor)
        {
            if (cursor.Kind != CXCursorKind.CXCursor_ParmDecl)
            {
                return null;
            }

            string paramName = cursor.Spelling.ToString();
            var type = cursor.Type;
            string typeName = type.Spelling.ToString();

            // Get parameter location
            var location = cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out _);
            string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : null;

            // Determine variable type
            VariableType variableType = DetermineVariableType(type);

            // Check attributes
            bool isConst = typeName.Contains("const");

            return new CVariable
            {
                Name = paramName,
                TypeName = typeName,
                VariableType = variableType,
                Scope = VariableScope.Parameter,
                LineNumber = (int)line,
                ColumnNumber = (int)column,
                SourceFile = sourceFile,
                IsConst = isConst
            };
        }

        private VariableType DetermineVariableType(CXType type)
        {
            switch (type.Kind)
            {
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_VariableArray:
                case CXTypeKind.CXType_IncompleteArray:
                case CXTypeKind.CXType_DependentSizedArray:
                    return VariableType.Array;

                case CXTypeKind.CXType_Pointer:
                    return VariableType.Pointer;

                case CXTypeKind.CXType_Record:
                    return type.Spelling.ToString().Contains("struct") ? VariableType.Struct : VariableType.Union;

                case CXTypeKind.CXType_Enum:
                    return VariableType.Enum;

                default:
                    return VariableType.Primitive;
            }
        }

        private void ExtractFunctionBody(CXCursor cursor, CFunction function)
        {
            // Find the function body
            CXCursor bodyStmt = default;

            cursor.VisitChildren((child, parent, clientData) =>
            {
                if (child.Kind == CXCursorKind.CXCursor_CompoundStmt)
                {
                    bodyStmt = child;
                    return CXChildVisitResult.CXChildVisit_Break;
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, IntPtr.Zero);

            if (bodyStmt.Kind != CXCursorKind.CXCursor_CompoundStmt)
            {
                return;
            }

            // Extract the source code of the body
            var extent = bodyStmt.Extent;
            var startLocation = extent.Start.GetFileLocation(out var startFile, out uint startLine, out uint startColumn, out _);
            var endLocation = extent.End.GetFileLocation(out var endFile, out uint endLine, out uint endColumn, out _);

            if (startFile != null && endFile != null && startFile.Name.ToString() == endFile.Name.ToString())
            {
                function.Body = $"// Function body from line {startLine} to {endLine}";
            }

            // Find called functions and used variables
            var usedVariables = new HashSet<string>();
            var calledFunctions = new HashSet<string>();

            bodyStmt.VisitChildren((child, parent, clientData) =>
            {
                if (child.Kind == CXCursorKind.CXCursor_DeclRefExpr)
                {
                    var referencedCursor = child.Referenced;
                    if (referencedCursor.Kind == CXCursorKind.CXCursor_VarDecl)
                    {
                        usedVariables.Add(referencedCursor.Spelling.ToString());
                    }
                    else if (referencedCursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        calledFunctions.Add(referencedCursor.Spelling.ToString());
                    }
                }

                return CXChildVisitResult.CXChildVisit_Recurse;
            }, IntPtr.Zero);

            function.UsedVariables = usedVariables.ToList();
            function.CalledFunctions = calledFunctions.ToList();
        }

        private void IdentifyComplexRelationships(List<FunctionRelationship> relationships, List<CFunction> functions)
        {
            // Find recursive calls
            foreach (var function in functions)
            {
                if (function.CalledFunctions.Contains(function.Name))
                {
                    _logger.LogDebug($"Recursive call detected in function: {function.Name}");
                }
            }

            // Find mutual recursion
            foreach (var functionA in functions)
            {
                foreach (var functionB in functions)
                {
                    if (functionA.Name != functionB.Name &&
                        functionA.CalledFunctions.Contains(functionB.Name) &&
                        functionB.CalledFunctions.Contains(functionA.Name))
                    {
                        _logger.LogDebug($"Mutual recursion detected between functions: {functionA.Name} and {functionB.Name}");
                    }
                }
            }
        }

        private List<string> ExtractFunctionBodyLines(CFunction function, SourceFile sourceFile)
        {
            var bodyLines = new List<string>();

            // Find the function in the source file
            bool foundFunction = false;
            int braceCount = 0;

            for (int i = function.LineNumber - 1; i < sourceFile.Lines.Count; i++)
            {
                string line = sourceFile.Lines[i];

                if (!foundFunction)
                {
                    // Look for the function declaration
                    if (line.Contains(function.Name) && (line.Contains('(') || line.Contains(')')))
                    {
                        foundFunction = true;
                    }

                    continue;
                }

                // Count braces to find the function body
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j] == '{')
                    {
                        braceCount++;

                        if (braceCount == 1)
                        {
                            // Start of function body
                            bodyLines.Add(line.Substring(j));
                            break;
                        }
                    }
                    else if (line[j] == '}')
                    {
                        braceCount--;

                        if (braceCount == 0)
                        {
                            // End of function body
                            bodyLines.Add(line.Substring(0, j + 1));
                            return bodyLines;
                        }
                    }
                }

                if (braceCount > 0 && (!bodyLines.Contains(line)))
                {
                    bodyLines.Add(line);
                }
            }

            return bodyLines;
        }

        private async Task ProcessFunctionBodyAsync(List<string> functionBody, int baseLineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            // Process each line in the function body
            for (int i = 0; i < functionBody.Count; i++)
            {
                string line = functionBody[i].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*"))
                {
                    continue;
                }

                // Check for control flow statements
                if (line.StartsWith("if") || line.Contains(" if "))
                {
                    await ProcessIfStatementAsync(functionBody, i, baseLineNumber, graph, currentNodeId);
                }
                else if (line.StartsWith("for") || line.Contains(" for "))
                {
                    await ProcessLoopStatementAsync(functionBody, i, "for", baseLineNumber, graph, currentNodeId);
                }
                else if (line.StartsWith("while") || line.Contains(" while "))
                {
                    await ProcessLoopStatementAsync(functionBody, i, "while", baseLineNumber, graph, currentNodeId);
                }
                else if (line.StartsWith("do") || line.Contains(" do "))
                {
                    await ProcessDoWhileStatementAsync(functionBody, i, baseLineNumber, graph, currentNodeId);
                }
                else if (line.StartsWith("switch") || line.Contains(" switch "))
                {
                    await ProcessSwitchStatementAsync(functionBody, i, baseLineNumber, graph, currentNodeId);
                }
                else if (line.StartsWith("return") || line.Contains(" return "))
                {
                    await ProcessReturnStatementAsync(line, baseLineNumber + i, graph, currentNodeId);
                }
                else
                {
                    // Regular statement
                    var statementNode = new ControlFlowNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        NodeType = "Statement",
                        LineNumber = baseLineNumber + i,
                        Code = line
                    };

                    graph.Nodes.Add(statementNode);

                    // Add edge from current node to this statement
                    graph.Edges.Add(new ControlFlowEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = currentNodeId,
                        TargetId = statementNode.Id,
                        EdgeType = "Sequential",
                        SourceFile = null // Would need to get this from somewhere
                    });

                    // Update current node
                    currentNodeId = statementNode.Id;
                }
            }

            // Add an exit node if there isn't a return statement
            if (!graph.Nodes.Any(n => n.NodeType == "Return"))
            {
                var exitNode = new ControlFlowNode
                {
                    Id = Guid.NewGuid().ToString(),
                    NodeType = "Exit",
                    LineNumber = baseLineNumber + functionBody.Count,
                    Code = "// Function exit"
                };

                graph.Nodes.Add(exitNode);

                // Add edge from current node to exit
                graph.Edges.Add(new ControlFlowEdge
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = currentNodeId,
                    TargetId = exitNode.Id,
                    EdgeType = "Sequential",
                    SourceFile = null // Would need to get this from somewhere
                });
            }

            await Task.CompletedTask;
        }

        private async Task ProcessIfStatementAsync(List<string> functionBody, int lineIndex, int baseLineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            string line = functionBody[lineIndex].Trim();

            // Extract the condition
            string condition = ExtractCondition(line, "if");

            // Create a condition node
            var conditionNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "Condition",
                LineNumber = baseLineNumber + lineIndex,
                Code = $"if ({condition})"
            };

            graph.Nodes.Add(conditionNode);

            // Add edge from current node to condition
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = currentNodeId,
                TargetId = conditionNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the 'then' branch
            var thenBodyLines = ExtractBlock(functionBody, lineIndex);

            // Create a node for the 'then' branch
            var thenNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "ThenBranch",
                LineNumber = baseLineNumber + lineIndex + 1,
                Code = "// Then branch"
            };

            graph.Nodes.Add(thenNode);

            // Add edge from condition to 'then' branch
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = conditionNode.Id,
                TargetId = thenNode.Id,
                EdgeType = "True",
                Condition = condition,
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the 'then' branch recursively
            await ProcessFunctionBodyAsync(thenBodyLines, baseLineNumber + lineIndex + 1, graph, thenNode.Id);

            // Check for 'else' branch
            int elseIndex = FindElseBranch(functionBody, lineIndex + thenBodyLines.Count);

            if (elseIndex >= 0)
            {
                // Process the 'else' branch
                var elseBodyLines = ExtractBlock(functionBody, elseIndex);

                // Create a node for the 'else' branch
                var elseNode = new ControlFlowNode
                {
                    Id = Guid.NewGuid().ToString(),
                    NodeType = "ElseBranch",
                    LineNumber = baseLineNumber + elseIndex + 1,
                    Code = "// Else branch"
                };

                graph.Nodes.Add(elseNode);

                // Add edge from condition to 'else' branch
                graph.Edges.Add(new ControlFlowEdge
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = conditionNode.Id,
                    TargetId = elseNode.Id,
                    EdgeType = "False",
                    Condition = $"!({condition})",
                    SourceFile = null // Would need to get this from somewhere
                });

                // Process the 'else' branch recursively
                await ProcessFunctionBodyAsync(elseBodyLines, baseLineNumber + elseIndex + 1, graph, elseNode.Id);
            }
        }

        private async Task ProcessLoopStatementAsync(List<string> functionBody, int lineIndex, string loopType, int baseLineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            string line = functionBody[lineIndex].Trim();

            // Extract the condition
            string condition = ExtractCondition(line, loopType);

            // Create a loop node
            var loopNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = loopType == "for" ? "ForLoop" : "WhileLoop",
                LineNumber = baseLineNumber + lineIndex,
                Code = $"{loopType} ({condition})"
            };

            graph.Nodes.Add(loopNode);

            // Add edge from current node to loop
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = currentNodeId,
                TargetId = loopNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the loop body
            var loopBodyLines = ExtractBlock(functionBody, lineIndex);

            // Create a node for the loop body
            var loopBodyNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "LoopBody",
                LineNumber = baseLineNumber + lineIndex + 1,
                Code = "// Loop body"
            };

            graph.Nodes.Add(loopBodyNode);

            // Add edge from loop to loop body
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = loopNode.Id,
                TargetId = loopBodyNode.Id,
                EdgeType = "True",
                Condition = condition,
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the loop body recursively
            await ProcessFunctionBodyAsync(loopBodyLines, baseLineNumber + lineIndex + 1, graph, loopBodyNode.Id);

            // Add edge from loop body back to loop
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = loopBodyNode.Id,
                TargetId = loopNode.Id,
                EdgeType = "Loop",
                SourceFile = null // Would need to get this from somewhere
            });

            // Create a node for the loop exit
            var loopExitNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "LoopExit",
                LineNumber = baseLineNumber + lineIndex + loopBodyLines.Count,
                Code = "// Loop exit"
            };

            graph.Nodes.Add(loopExitNode);

            // Add edge from loop to loop exit
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = loopNode.Id,
                TargetId = loopExitNode.Id,
                EdgeType = "False",
                Condition = $"!({condition})",
                SourceFile = null // Would need to get this from somewhere
            });

            // Update current node
            currentNodeId = loopExitNode.Id;
        }

        private async Task ProcessDoWhileStatementAsync(List<string> functionBody, int lineIndex, int baseLineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            // Create a do-while node
            var doWhileNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "DoWhileLoop",
                LineNumber = baseLineNumber + lineIndex,
                Code = "do"
            };

            graph.Nodes.Add(doWhileNode);

            // Add edge from current node to do-while
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = currentNodeId,
                TargetId = doWhileNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the do-while body
            var loopBodyLines = ExtractBlock(functionBody, lineIndex);

            // Process the loop body recursively
            await ProcessFunctionBodyAsync(loopBodyLines, baseLineNumber + lineIndex + 1, graph, doWhileNode.Id);

            // Find the while condition
            int whileIndex = FindWhileCondition(functionBody, lineIndex + loopBodyLines.Count);
            string condition = "true"; // Default if not found

            if (whileIndex >= 0)
            {
                string line = functionBody[whileIndex].Trim();
                condition = ExtractCondition(line, "while");
            }

            // Create a condition node for the while part
            var conditionNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "Condition",
                LineNumber = baseLineNumber + whileIndex,
                Code = $"while ({condition})"
            };

            graph.Nodes.Add(conditionNode);

            // Add edge from loop body to condition
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = doWhileNode.Id,
                TargetId = conditionNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Add edge from condition back to do-while if true
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = conditionNode.Id,
                TargetId = doWhileNode.Id,
                EdgeType = "True",
                Condition = condition,
                SourceFile = null // Would need to get this from somewhere
            });

            // Create a node for the loop exit
            var loopExitNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "LoopExit",
                LineNumber = baseLineNumber + whileIndex + 1,
                Code = "// Loop exit"
            };

            graph.Nodes.Add(loopExitNode);

            // Add edge from condition to loop exit if false
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = conditionNode.Id,
                TargetId = loopExitNode.Id,
                EdgeType = "False",
                Condition = $"!({condition})",
                SourceFile = null // Would need to get this from somewhere
            });

            // Update current node
            currentNodeId = loopExitNode.Id;
        }

        private async Task ProcessSwitchStatementAsync(List<string> functionBody, int lineIndex, int baseLineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            string line = functionBody[lineIndex].Trim();

            // Extract the switch expression
            string switchExpr = ExtractCondition(line, "switch");

            // Create a switch node
            var switchNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "Switch",
                LineNumber = baseLineNumber + lineIndex,
                Code = $"switch ({switchExpr})"
            };

            graph.Nodes.Add(switchNode);

            // Add edge from current node to switch
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = currentNodeId,
                TargetId = switchNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Extract the switch body
            var switchBodyLines = ExtractBlock(functionBody, lineIndex);

            // Create a simple merged case body for now
            // A more sophisticated implementation would separate each case
            var caseBodyNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "SwitchBody",
                LineNumber = baseLineNumber + lineIndex + 1,
                Code = "// Switch body"
            };

            graph.Nodes.Add(caseBodyNode);

            // Add edge from switch to case body
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = switchNode.Id,
                TargetId = caseBodyNode.Id,
                EdgeType = "Switch",
                Condition = switchExpr,
                SourceFile = null // Would need to get this from somewhere
            });

            // Process the switch body recursively
            await ProcessFunctionBodyAsync(switchBodyLines, baseLineNumber + lineIndex + 1, graph, caseBodyNode.Id);

            // Create a node for the switch exit
            var switchExitNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "SwitchExit",
                LineNumber = baseLineNumber + lineIndex + switchBodyLines.Count,
                Code = "// Switch exit"
            };

            graph.Nodes.Add(switchExitNode);

            // Add edge from case body to switch exit
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = caseBodyNode.Id,
                TargetId = switchExitNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            // Update current node
            currentNodeId = switchExitNode.Id;
        }

        private async Task ProcessReturnStatementAsync(string line, int lineNumber, ControlFlowGraph graph, string currentNodeId)
        {
            // Extract the return value
            string returnValue = null;
            if (line.Contains("return"))
            {
                int index = line.IndexOf("return") + "return".Length;
                returnValue = line.Substring(index).Trim();

                // Remove trailing semicolon
                if (returnValue.EndsWith(";"))
                {
                    returnValue = returnValue.Substring(0, returnValue.Length - 1).Trim();
                }
            }

            // Create a return node
            var returnNode = new ControlFlowNode
            {
                Id = Guid.NewGuid().ToString(),
                NodeType = "Return",
                LineNumber = lineNumber,
                Code = returnValue != null ? $"return {returnValue}" : "return"
            };

            graph.Nodes.Add(returnNode);

            // Add edge from current node to return
            graph.Edges.Add(new ControlFlowEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = currentNodeId,
                TargetId = returnNode.Id,
                EdgeType = "Sequential",
                SourceFile = null // Would need to get this from somewhere
            });

            await Task.CompletedTask;
        }

        private string ExtractCondition(string line, string keyword)
        {
            int startIndex = line.IndexOf(keyword) + keyword.Length;
            int openParenIndex = line.IndexOf('(', startIndex);

            if (openParenIndex < 0)
            {
                return "";
            }

            int closeParenIndex = FindMatchingCloseParen(line, openParenIndex);

            if (closeParenIndex < 0)
            {
                return "";
            }

            return line.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
        }

        private int FindMatchingCloseParen(string line, int openParenIndex)
        {
            int parenCount = 1;

            for (int i = openParenIndex + 1; i < line.Length; i++)
            {
                if (line[i] == '(')
                {
                    parenCount++;
                }
                else if (line[i] == ')')
                {
                    parenCount--;

                    if (parenCount == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private List<string> ExtractBlock(List<string> functionBody, int startLineIndex)
        {
            // Skip to the opening brace
            int i = startLineIndex;
            while (i < functionBody.Count && !functionBody[i].Contains('{'))
            {
                i++;
            }

            if (i >= functionBody.Count)
            {
                // No opening brace found, assume the next line is the body
                if (startLineIndex + 1 < functionBody.Count)
                {
                    return new List<string> { functionBody[startLineIndex + 1] };
                }

                return new List<string>();
            }

            // Found opening brace, extract the block
            int braceCount = 0;
            int startIndex = i;

            for (int j = i; j < functionBody.Count; j++)
            {
                string line = functionBody[j];

                for (int k = 0; k < line.Length; k++)
                {
                    if (line[k] == '{')
                    {
                        braceCount++;
                    }
                    else if (line[k] == '}')
                    {
                        braceCount--;

                        if (braceCount == 0)
                        {
                            // Found matching closing brace
                            return functionBody.GetRange(startIndex, j - startIndex + 1);
                        }
                    }
                }
            }

            // No matching closing brace found
            return functionBody.GetRange(startIndex, functionBody.Count - startIndex);
        }

        private int FindElseBranch(List<string> functionBody, int startLineIndex)
        {
            for (int i = startLineIndex; i < functionBody.Count; i++)
            {
                string line = functionBody[i].Trim();

                if (line.StartsWith("else") || line == "else")
                {
                    return i;
                }

                // If we find any non-whitespace line that's not 'else',
                // then there's no else branch
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return -1;
                }
            }

            return -1;
        }

        private int FindWhileCondition(List<string> functionBody, int startLineIndex)
        {
            for (int i = startLineIndex; i < functionBody.Count; i++)
            {
                string line = functionBody[i].Trim();

                if (line.StartsWith("while") || line.Contains(" while "))
                {
                    return i;
                }
            }

            return -1;
        }

        private int CalculateCyclomaticComplexity(List<string> functionBody)
        {
            // Start with 1 for the entry point
            int complexity = 1;

            foreach (string line in functionBody)
            {
                // Count branching statements
                if (line.Contains("if") || line.Contains("else if") ||
                    line.Contains("while") || line.Contains("for") ||
                    line.Contains("case") || line.Contains("catch") ||
                    line.Contains("&&") || line.Contains("||"))
                {
                    complexity++;
                }
            }

            return complexity;
        }

        private int CalculateNestingDepth(List<string> functionBody)
        {
            int maxDepth = 0;
            int currentDepth = 0;

            foreach (string line in functionBody)
            {
                foreach (char c in line)
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

        private int CalculateStatementCount(List<string> functionBody)
        {
            int count = 0;

            foreach (string line in functionBody)
            {
                string trimmedLine = line.Trim();

                // Skip empty lines, comments, and control structures
                if (string.IsNullOrWhiteSpace(trimmedLine) ||
                    trimmedLine.StartsWith("//") ||
                    trimmedLine.StartsWith("/*") ||
                    trimmedLine.StartsWith("*/") ||
                    trimmedLine.StartsWith("{") ||
                    trimmedLine.StartsWith("}") ||
                    trimmedLine.StartsWith("if") ||
                    trimmedLine.StartsWith("else") ||
                    trimmedLine.StartsWith("for") ||
                    trimmedLine.StartsWith("while") ||
                    trimmedLine.StartsWith("do") ||
                    trimmedLine.StartsWith("switch") ||
                    trimmedLine.StartsWith("case") ||
                    trimmedLine.StartsWith("default"))
                {
                    continue;
                }

                // Count statements (lines ending with semicolon)
                if (trimmedLine.EndsWith(";"))
                {
                    count++;
                }
            }

            return count;
        }

        private int CalculateConditionCount(List<string> functionBody)
        {
            int count = 0;

            foreach (string line in functionBody)
            {
                string trimmedLine = line.Trim();

                // Count if, else if, while, for, switch, case statements
                if (trimmedLine.StartsWith("if") ||
                    trimmedLine.StartsWith("else if") ||
                    trimmedLine.StartsWith("while") ||
                    trimmedLine.StartsWith("for") ||
                    trimmedLine.StartsWith("switch") ||
                    trimmedLine.StartsWith("case"))
                {
                    count++;
                }

                // Count logical operators within conditions
                int andCount = CountOccurrences(trimmedLine, "&&");
                int orCount = CountOccurrences(trimmedLine, "||");

                count += andCount + orCount;
            }

            return count;
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(pattern, index)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }
    }

    #endregion
}
